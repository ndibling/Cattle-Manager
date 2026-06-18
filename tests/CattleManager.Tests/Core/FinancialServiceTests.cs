using CattleManager.Core.Models;
using CattleManager.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CattleManager.Tests.Core;

public class FinancialServiceTests
{
    private static FinancialService MakeSut(
        ITransactionRepository? transactions = null,
        IAssetRepository? assets = null,
        ILoanRepository? loans = null,
        IBudgetRepository? budget = null,
        IAnimalRepository? animals = null,
        IAppSettingsRepository? settings = null) => new(
            transactions ?? new Mock<ITransactionRepository>().Object,
            assets       ?? new Mock<IAssetRepository>().Object,
            loans        ?? new Mock<ILoanRepository>().Object,
            budget       ?? new Mock<IBudgetRepository>().Object,
            animals      ?? new Mock<IAnimalRepository>().Object,
            settings     ?? new Mock<IAppSettingsRepository>().Object);

    private static AssetDto MakeAsset(
        decimal purchasePrice = 10_000m,
        decimal salvageValue = 1_000m,
        int usefulLifeYears = 5,
        DepreciationMethod method = DepreciationMethod.StraightLine,
        DateTime? purchaseDate = null) => new()
    {
        AssetName = "Test Asset",
        PurchasePrice = purchasePrice,
        SalvageValue = salvageValue,
        UsefulLifeYears = usefulLifeYears,
        DepreciationMethod = method,
        PurchaseDate = purchaseDate ?? DateTime.Today.AddYears(-1)
    };

    private static LoanDto MakeLoan(
        decimal principal = 100_000m,
        decimal annualRate = 0.06m,
        decimal payment = 1_110m,
        PaymentFrequency frequency = PaymentFrequency.Monthly,
        DateTime? startDate = null) => new()
    {
        LenderName = "Test Bank",
        OriginalPrincipal = principal,
        InterestRate = annualRate,
        PaymentAmount = payment,
        PaymentFrequency = frequency,
        StartDate = startDate ?? new DateTime(2023, 1, 1),
        IsActive = true
    };

    // --- StraightLine depreciation ---

    [Fact]
    public void CalculateAnnualDepreciation_StraightLine_YearOne_ReturnsDepreciableBaseOverLife()
    {
        var sut = MakeSut();
        var asset = MakeAsset(purchasePrice: 10_000m, salvageValue: 1_000m, usefulLifeYears: 9);

        var result = sut.CalculateAnnualDepreciation(asset, year: 1);

        result.Should().Be(1_000m); // (10000 - 1000) / 9
    }

    [Fact]
    public void CalculateAnnualDepreciation_StraightLine_SameEachYear()
    {
        var sut = MakeSut();
        var asset = MakeAsset(purchasePrice: 10_000m, salvageValue: 0m, usefulLifeYears: 5);

        var year1 = sut.CalculateAnnualDepreciation(asset, 1);
        var year3 = sut.CalculateAnnualDepreciation(asset, 3);
        var year5 = sut.CalculateAnnualDepreciation(asset, 5);

        year1.Should().Be(year3).And.Be(year5);
    }

    [Fact]
    public void CalculateAnnualDepreciation_StraightLine_BeyondUsefulLife_ReturnsZero()
    {
        var sut = MakeSut();
        var asset = MakeAsset(usefulLifeYears: 5);

        var result = sut.CalculateAnnualDepreciation(asset, year: 6);

        result.Should().Be(0m);
    }

    // --- Section 179 depreciation ---

    [Fact]
    public void CalculateAnnualDepreciation_Section179_YearOne_ReturnsFullPurchasePrice()
    {
        var sut = MakeSut();
        var asset = MakeAsset(purchasePrice: 42_000m, method: DepreciationMethod.Section179);

        var result = sut.CalculateAnnualDepreciation(asset, year: 1);

        result.Should().Be(42_000m);
    }

    [Fact]
    public void CalculateAnnualDepreciation_Section179_YearTwo_ReturnsZero()
    {
        var sut = MakeSut();
        var asset = MakeAsset(purchasePrice: 42_000m, method: DepreciationMethod.Section179);

        var result = sut.CalculateAnnualDepreciation(asset, year: 2);

        result.Should().Be(0m);
    }

    // --- 150% DB depreciation ---

    [Fact]
    public void CalculateAnnualDepreciation_DB150_YearOne_IsHigherThanStraightLine()
    {
        var sut = MakeSut();
        var sl = MakeAsset(purchasePrice: 10_000m, salvageValue: 0m, usefulLifeYears: 5, method: DepreciationMethod.StraightLine);
        var db = MakeAsset(purchasePrice: 10_000m, salvageValue: 0m, usefulLifeYears: 5, method: DepreciationMethod.DB150);

        var slY1 = sut.CalculateAnnualDepreciation(sl, 1);
        var dbY1 = sut.CalculateAnnualDepreciation(db, 1);

        dbY1.Should().BeGreaterThan(slY1);
    }

    [Fact]
    public void CalculateAnnualDepreciation_DB150_TotalNeverExceedsDepreciableBase()
    {
        var sut = MakeSut();
        var asset = MakeAsset(purchasePrice: 10_000m, salvageValue: 1_000m, usefulLifeYears: 5, method: DepreciationMethod.DB150);

        var total = Enumerable.Range(1, 7).Sum(y => sut.CalculateAnnualDepreciation(asset, y));

        total.Should().BeLessThanOrEqualTo(asset.PurchasePrice - asset.SalvageValue);
    }

    // --- Loan balance ---

    [Fact]
    public void CalculateLoanBalance_BeforeStart_ReturnsOriginalPrincipal()
    {
        var sut = MakeSut();
        var loan = MakeLoan(principal: 100_000m, startDate: new DateTime(2024, 1, 1));

        var result = sut.CalculateLoanBalance(loan, new DateTime(2023, 12, 31));

        result.Should().Be(100_000m);
    }

    [Fact]
    public void CalculateLoanBalance_OnStartDate_ReturnsOriginalPrincipal()
    {
        var sut = MakeSut();
        var start = new DateTime(2023, 1, 1);
        var loan = MakeLoan(principal: 100_000m, startDate: start);

        var result = sut.CalculateLoanBalance(loan, start);

        result.Should().Be(100_000m);
    }

    [Fact]
    public void CalculateLoanBalance_AfterOneYear_BalanceReduces()
    {
        var sut = MakeSut();
        var start = new DateTime(2023, 1, 1);
        // Standard 30yr mortgage-style loan: $100k at 6% monthly
        var loan = MakeLoan(principal: 100_000m, annualRate: 0.06m, payment: 599.55m,
            frequency: PaymentFrequency.Monthly, startDate: start);

        var balance = sut.CalculateLoanBalance(loan, start.AddYears(1));

        balance.Should().BeLessThan(100_000m).And.BeGreaterThan(0m);
    }

    [Fact]
    public void CalculateLoanBalance_ZeroInterest_ReducesByPaymentsOnly()
    {
        var sut = MakeSut();
        var start = new DateTime(2023, 1, 1);
        var loan = MakeLoan(principal: 12_000m, annualRate: 0m, payment: 1_000m,
            frequency: PaymentFrequency.Monthly, startDate: start);

        var balance = sut.CalculateLoanBalance(loan, start.AddMonths(3));

        balance.Should().Be(9_000m);
    }

    // --- P&L ---

    [Fact]
    public async Task GetProfitAndLossAsync_WithIncomeAndExpenses_ReturnsCorrectNet()
    {
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 12, 31);

        var txMock = new Mock<ITransactionRepository>();
        txMock.Setup(r => r.GetByDateRangeAsync(from, to)).ReturnsAsync(new List<TransactionDto>
        {
            new() { TransactionType = TransactionType.Income,  Category = "LivestockSales", Date = new DateTime(2025,3,1), Amount = 5_000m },
            new() { TransactionType = TransactionType.Income,  Category = "HayCropSales",  Date = new DateTime(2025,6,1), Amount = 1_200m },
            new() { TransactionType = TransactionType.Expense, Category = "FeedHay",       Date = new DateTime(2025,2,1), Amount = 2_400m },
            new() { TransactionType = TransactionType.Expense, Category = "FuelOil",       Date = new DateTime(2025,4,1), Amount = 600m },
        });

        var loanMock = new Mock<ILoanRepository>();
        loanMock.Setup(r => r.GetAllPaymentsInRangeAsync(from, to)).ReturnsAsync([]);

        var assetMock = new Mock<IAssetRepository>();
        assetMock.Setup(r => r.GetActiveAsync()).ReturnsAsync([]);

        var budgetMock = new Mock<IBudgetRepository>();
        budgetMock.Setup(r => r.GetByFiscalYearAsync(It.IsAny<int>())).ReturnsAsync([]);

        var sut = MakeSut(txMock.Object, assetMock.Object, loanMock.Object, budgetMock.Object);

        var pl = await sut.GetProfitAndLossAsync(from, to);

        pl.GrossRevenue.Should().Be(6_200m);
        pl.TotalExpenses.Should().Be(3_000m);
        pl.InterestExpense.Should().Be(0m);
        pl.DepreciationTotal.Should().Be(0m);
        pl.NetFarmIncome.Should().Be(3_200m);
        pl.IncomeByCategory.Should().HaveCount(2);
        pl.ExpensesByCategory.Should().HaveCount(2);
    }
}
