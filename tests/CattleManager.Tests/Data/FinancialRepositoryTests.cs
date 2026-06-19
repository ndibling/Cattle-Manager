using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CattleManager.Data;
using CattleManager.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CattleManager.Tests.Data;

public class FinancialRepositoryTests : IDisposable
{
    private readonly CattleDbContext _db;
    private readonly TransactionRepository _transactions;
    private readonly AssetRepository _assets;
    private readonly LoanRepository _loans;
    private readonly BudgetRepository _budget;

    public FinancialRepositoryTests()
    {
        var opts = new DbContextOptionsBuilder<CattleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CattleDbContext(opts);
        _db.Database.EnsureCreated();

        _transactions = new TransactionRepository(_db);
        _assets       = new AssetRepository(_db);
        _loans        = new LoanRepository(_db);
        _budget       = new BudgetRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static TransactionDto MakeTx(
        TransactionType type = TransactionType.Expense,
        string category = "FeedHay",
        decimal amount = 500m,
        DateTime? date = null,
        bool sample = false) => new()
    {
        TransactionType = type,
        Category        = category,
        Date            = date ?? new DateTime(2025, 6, 1),
        Amount          = amount,
        Description     = "Test transaction",
        IsSampleData    = sample,
    };

    private static AssetDto MakeAsset(bool disposed = false, bool sample = false) => new()
    {
        AssetName          = "Test Tractor",
        Category           = AssetCategory.MachineryEquipment,
        PurchaseDate       = new DateTime(2020, 1, 1),
        PurchasePrice      = 40_000m,
        DepreciationMethod = DepreciationMethod.StraightLine,
        UsefulLifeYears    = 10,
        SalvageValue       = 5_000m,
        DisposedDate       = disposed ? DateTime.Today : null,
        IsSampleData       = sample,
    };

    private static LoanDto MakeLoan(bool active = true, bool sample = false) => new()
    {
        LenderName        = "First Ag Bank",
        LoanType          = LoanType.EquipmentLoan,
        OriginalPrincipal = 80_000m,
        InterestRate      = 0.065m,
        StartDate         = new DateTime(2023, 1, 1),
        PaymentFrequency  = PaymentFrequency.Monthly,
        PaymentAmount     = 900m,
        IsActive          = active,
        IsSampleData      = sample,
    };

    // ── TransactionRepository ────────────────────────────────────────────────

    [Fact]
    public async Task Transaction_Add_AssignsId()
    {
        var tx = await _transactions.AddAsync(MakeTx());
        tx.TransactionId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Transaction_GetAll_ReturnsAdded()
    {
        await _transactions.AddAsync(MakeTx(amount: 100m));
        await _transactions.AddAsync(MakeTx(amount: 200m));

        var all = await _transactions.GetAllAsync();

        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task Transaction_GetByDateRange_FiltersCorrectly()
    {
        await _transactions.AddAsync(MakeTx(date: new DateTime(2025, 3, 1)));
        await _transactions.AddAsync(MakeTx(date: new DateTime(2025, 7, 1)));
        await _transactions.AddAsync(MakeTx(date: new DateTime(2025, 11, 1)));

        var from = new DateTime(2025, 1, 1);
        var to   = new DateTime(2025, 9, 30);
        var results = await _transactions.GetByDateRangeAsync(from, to);

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(t => t.Date.Should().BeOnOrAfter(from).And.BeOnOrBefore(to));
    }

    [Fact]
    public async Task Transaction_GetByType_FiltersCorrectly()
    {
        await _transactions.AddAsync(MakeTx(TransactionType.Income,  "LivestockSales"));
        await _transactions.AddAsync(MakeTx(TransactionType.Expense, "FeedHay"));
        await _transactions.AddAsync(MakeTx(TransactionType.Expense, "FuelOil"));

        var expenses = await _transactions.GetByTypeAsync(TransactionType.Expense);
        var income   = await _transactions.GetByTypeAsync(TransactionType.Income);

        expenses.Should().HaveCount(2);
        income.Should().HaveCount(1);
    }

    [Fact]
    public async Task Transaction_Update_PersistsChanges()
    {
        var tx = await _transactions.AddAsync(MakeTx(amount: 100m));
        tx.Amount = 250m;
        tx.Description = "Updated";

        await _transactions.UpdateAsync(tx);
        var all = await _transactions.GetAllAsync();

        all.Single().Amount.Should().Be(250m);
        all.Single().Description.Should().Be("Updated");
    }

    [Fact]
    public async Task Transaction_Delete_RemovesRecord()
    {
        var tx = await _transactions.AddAsync(MakeTx());

        await _transactions.DeleteAsync(tx.TransactionId);
        var all = await _transactions.GetAllAsync();

        all.Should().BeEmpty();
    }

    [Fact]
    public async Task Transaction_DeleteSampleData_OnlyRemovesSamples()
    {
        await _transactions.AddAsync(MakeTx(sample: true));
        await _transactions.AddAsync(MakeTx(sample: true));
        await _transactions.AddAsync(MakeTx(sample: false));

        await _transactions.DeleteSampleDataAsync();
        var all = await _transactions.GetAllAsync();

        all.Should().HaveCount(1);
        all.Single().IsSampleData.Should().BeFalse();
    }

    // ── AssetRepository ──────────────────────────────────────────────────────

    [Fact]
    public async Task Asset_Add_AssignsId()
    {
        var asset = await _assets.AddAsync(MakeAsset());
        asset.AssetId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Asset_GetAll_IncludesDisposed()
    {
        await _assets.AddAsync(MakeAsset(disposed: false));
        await _assets.AddAsync(MakeAsset(disposed: true));

        var all = await _assets.GetAllAsync();

        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task Asset_GetActive_ExcludesDisposed()
    {
        await _assets.AddAsync(MakeAsset(disposed: false));
        await _assets.AddAsync(MakeAsset(disposed: true));

        var active = await _assets.GetActiveAsync();

        active.Should().HaveCount(1);
        active.Single().DisposedDate.Should().BeNull();
    }

    [Fact]
    public async Task Asset_Update_PersistsChanges()
    {
        var asset = await _assets.AddAsync(MakeAsset());
        asset.AssetName    = "Updated Tractor";
        asset.PurchasePrice = 45_000m;

        await _assets.UpdateAsync(asset);
        var all = await _assets.GetAllAsync();

        all.Single().AssetName.Should().Be("Updated Tractor");
        all.Single().PurchasePrice.Should().Be(45_000m);
    }

    [Fact]
    public async Task Asset_Delete_RemovesRecord()
    {
        var asset = await _assets.AddAsync(MakeAsset());

        await _assets.DeleteAsync(asset.AssetId);
        var all = await _assets.GetAllAsync();

        all.Should().BeEmpty();
    }

    [Fact]
    public async Task Asset_DeleteSampleData_OnlyRemovesSamples()
    {
        await _assets.AddAsync(MakeAsset(sample: true));
        await _assets.AddAsync(MakeAsset(sample: false));

        await _assets.DeleteSampleDataAsync();
        var all = await _assets.GetAllAsync();

        all.Should().HaveCount(1);
        all.Single().IsSampleData.Should().BeFalse();
    }

    // ── LoanRepository ───────────────────────────────────────────────────────

    [Fact]
    public async Task Loan_Add_AssignsId()
    {
        var loan = await _loans.AddAsync(MakeLoan());
        loan.LoanId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Loan_GetAll_ReturnsBothActiveAndInactive()
    {
        await _loans.AddAsync(MakeLoan(active: true));
        await _loans.AddAsync(MakeLoan(active: false));

        var all = await _loans.GetAllAsync();

        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task Loan_GetActive_ExcludesInactive()
    {
        await _loans.AddAsync(MakeLoan(active: true));
        await _loans.AddAsync(MakeLoan(active: false));

        var active = await _loans.GetActiveAsync();

        active.Should().HaveCount(1);
        active.Single().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Loan_Update_PersistsChanges()
    {
        var loan = await _loans.AddAsync(MakeLoan());
        loan.LenderName = "New Lender";
        loan.IsActive   = false;

        await _loans.UpdateAsync(loan);
        var all = await _loans.GetAllAsync();

        all.Single().LenderName.Should().Be("New Lender");
        all.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Loan_Delete_RemovesRecord()
    {
        var loan = await _loans.AddAsync(MakeLoan());

        await _loans.DeleteAsync(loan.LoanId);
        var all = await _loans.GetAllAsync();

        all.Should().BeEmpty();
    }

    [Fact]
    public async Task LoanPayment_AddAndGet_RoundTrips()
    {
        var loan = await _loans.AddAsync(MakeLoan());

        var payment = await _loans.AddPaymentAsync(new LoanPaymentDto
        {
            LoanId           = loan.LoanId,
            PaymentDate      = new DateTime(2025, 2, 1),
            TotalPayment     = 900m,
            PrincipalPortion = 400m,
            InterestPortion  = 500m,
            RemainingBalance = 79_600m,
        });

        payment.PaymentId.Should().BeGreaterThan(0);

        var payments = await _loans.GetPaymentsAsync(loan.LoanId);
        payments.Should().HaveCount(1);
        payments.Single().TotalPayment.Should().Be(900m);
    }

    [Fact]
    public async Task LoanPayment_Delete_RemovesPayment()
    {
        var loan    = await _loans.AddAsync(MakeLoan());
        var payment = await _loans.AddPaymentAsync(new LoanPaymentDto
        {
            LoanId = loan.LoanId, PaymentDate = DateTime.Today,
            TotalPayment = 900m, PrincipalPortion = 400m,
            InterestPortion = 500m, RemainingBalance = 79_600m,
        });

        await _loans.DeletePaymentAsync(payment.PaymentId);
        var payments = await _loans.GetPaymentsAsync(loan.LoanId);

        payments.Should().BeEmpty();
    }

    [Fact]
    public async Task LoanPayment_GetAllInRange_FiltersCorrectly()
    {
        var loan = await _loans.AddAsync(MakeLoan());
        await _loans.AddPaymentAsync(new LoanPaymentDto
        {
            LoanId = loan.LoanId, PaymentDate = new DateTime(2025, 1, 1),
            TotalPayment = 900m, PrincipalPortion = 400m,
            InterestPortion = 500m, RemainingBalance = 79_600m,
        });
        await _loans.AddPaymentAsync(new LoanPaymentDto
        {
            LoanId = loan.LoanId, PaymentDate = new DateTime(2025, 8, 1),
            TotalPayment = 900m, PrincipalPortion = 405m,
            InterestPortion = 495m, RemainingBalance = 72_000m,
        });

        var inRange = await _loans.GetAllPaymentsInRangeAsync(
            new DateTime(2025, 1, 1), new DateTime(2025, 6, 30));

        inRange.Should().HaveCount(1);
        inRange.Single().PaymentDate.Should().Be(new DateTime(2025, 1, 1));
    }

    [Fact]
    public async Task Loan_DeleteSampleData_OnlyRemovesSamples()
    {
        await _loans.AddAsync(MakeLoan(sample: true));
        await _loans.AddAsync(MakeLoan(sample: false));

        await _loans.DeleteSampleDataAsync();
        var all = await _loans.GetAllAsync();

        all.Should().HaveCount(1);
        all.Single().IsSampleData.Should().BeFalse();
    }

    // ── BudgetRepository ─────────────────────────────────────────────────────

    [Fact]
    public async Task Budget_Upsert_CreatesNewEntry()
    {
        var entry = await _budget.UpsertAsync(new BudgetEntryDto
        {
            FiscalYear      = 2025,
            Category        = "FeedHay",
            TransactionType = TransactionType.Expense,
            Month           = 0,
            BudgetAmount    = 8_000m,
        });

        entry.BudgetEntryId.Should().BeGreaterThan(0);
        var saved = await _budget.GetByFiscalYearAsync(2025);
        saved.Should().HaveCount(1);
        saved.Single().BudgetAmount.Should().Be(8_000m);
    }

    [Fact]
    public async Task Budget_Upsert_UpdatesExistingEntry()
    {
        await _budget.UpsertAsync(new BudgetEntryDto
        {
            FiscalYear = 2025, Category = "FeedHay",
            TransactionType = TransactionType.Expense, Month = 0, BudgetAmount = 8_000m,
        });

        await _budget.UpsertAsync(new BudgetEntryDto
        {
            FiscalYear = 2025, Category = "FeedHay",
            TransactionType = TransactionType.Expense, Month = 0, BudgetAmount = 10_000m,
        });

        var saved = await _budget.GetByFiscalYearAsync(2025);
        saved.Should().HaveCount(1);
        saved.Single().BudgetAmount.Should().Be(10_000m);
    }

    [Fact]
    public async Task Budget_GetByFiscalYear_ReturnsOnlyMatchingYear()
    {
        await _budget.UpsertAsync(new BudgetEntryDto
        {
            FiscalYear = 2025, Category = "FeedHay",
            TransactionType = TransactionType.Expense, Month = 0, BudgetAmount = 8_000m,
        });
        await _budget.UpsertAsync(new BudgetEntryDto
        {
            FiscalYear = 2026, Category = "FeedHay",
            TransactionType = TransactionType.Expense, Month = 0, BudgetAmount = 9_000m,
        });

        var results = await _budget.GetByFiscalYearAsync(2025);

        results.Should().HaveCount(1);
        results.Single().FiscalYear.Should().Be(2025);
    }

    [Fact]
    public async Task Budget_DeleteByYear_RemovesOnlyThatYear()
    {
        await _budget.UpsertAsync(new BudgetEntryDto
        {
            FiscalYear = 2025, Category = "FeedHay",
            TransactionType = TransactionType.Expense, Month = 0, BudgetAmount = 8_000m,
        });
        await _budget.UpsertAsync(new BudgetEntryDto
        {
            FiscalYear = 2026, Category = "FeedHay",
            TransactionType = TransactionType.Expense, Month = 0, BudgetAmount = 9_000m,
        });

        await _budget.DeleteByYearAsync(2025);

        var remaining2025 = await _budget.GetByFiscalYearAsync(2025);
        var remaining2026 = await _budget.GetByFiscalYearAsync(2026);

        remaining2025.Should().BeEmpty();
        remaining2026.Should().HaveCount(1);
    }

    [Fact]
    public async Task Budget_DeleteSampleData_OnlyRemovesSamples()
    {
        await _budget.UpsertAsync(new BudgetEntryDto
        {
            FiscalYear = 2025, Category = "FeedHay",
            TransactionType = TransactionType.Expense, Month = 0,
            BudgetAmount = 8_000m, IsSampleData = true,
        });
        await _budget.UpsertAsync(new BudgetEntryDto
        {
            FiscalYear = 2025, Category = "FuelOil",
            TransactionType = TransactionType.Expense, Month = 0,
            BudgetAmount = 3_000m, IsSampleData = false,
        });

        await _budget.DeleteSampleDataAsync();
        var remaining = await _budget.GetByFiscalYearAsync(2025);

        remaining.Should().HaveCount(1);
        remaining.Single().Category.Should().Be("FuelOil");
    }
}
