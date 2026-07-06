namespace CattleManager.Core.Models;

public class TransactionDto
{
    public int TransactionId { get; set; }
    public TransactionType TransactionType { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PayeePayer { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public int? LinkedAnimalId { get; set; }
    public string? LinkedAnimalName { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetProceeds => Amount - TaxAmount;
    public bool IsSampleData { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public string AmountDisplay => Amount.ToString("C");
    public string TypeDisplay => TransactionType switch
    {
        TransactionType.Income => "Income",
        TransactionType.Expense => "Expense",
        TransactionType.CapitalInflux => "Capital",
        _ => string.Empty
    };
}

public class AssetDto
{
    public int AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public AssetCategory Category { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal? CurrentValue { get; set; }
    public DepreciationMethod DepreciationMethod { get; set; }
    public int UsefulLifeYears { get; set; }
    public decimal SalvageValue { get; set; }
    public int? LinkedAnimalId { get; set; }
    public string? LinkedAnimalName { get; set; }
    public int? LinkedTransactionId { get; set; }
    public DateTime? DisposedDate { get; set; }
    public decimal? DisposalPrice { get; set; }
    public string? Notes { get; set; }
    public bool IsSampleData { get; set; }
    public DateTime CreatedDate { get; set; }

    public bool IsActive => DisposedDate is null;

    public string CategoryDisplay => Category switch
    {
        AssetCategory.MachineryEquipment => "Machinery & Equipment",
        _ => Category.ToString()
    };

    public string DepreciationMethodDisplay => DepreciationMethod switch
    {
        DepreciationMethod.StraightLine => "Straight Line",
        DepreciationMethod.DB150        => "150% Declining Balance",
        DepreciationMethod.Section179   => "Section 179",
        _                               => DepreciationMethod.ToString()
    };
}

public class LoanDto
{
    public int LoanId { get; set; }
    public string LenderName { get; set; } = string.Empty;
    public LoanType LoanType { get; set; }
    public decimal OriginalPrincipal { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? MaturityDate { get; set; }
    public PaymentFrequency PaymentFrequency { get; set; }
    public decimal PaymentAmount { get; set; }
    public int PaymentDayOfMonth { get; set; } = 1;
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public bool IsSampleData { get; set; }
    public DateTime CreatedDate { get; set; }

    public string InterestRateDisplay => $"{InterestRate * 100:F2}%";
    public string LoanTypeDisplay => LoanType switch
    {
        LoanType.OperatingLineOfCredit => "Operating Line of Credit",
        LoanType.EquipmentLoan         => "Equipment Loan",
        LoanType.RealEstateLoan        => "Real Estate Loan",
        _                              => "Other"
    };
    public string PaymentFrequencyDisplay => PaymentFrequency switch
    {
        PaymentFrequency.Monthly    => "Monthly",
        PaymentFrequency.Quarterly  => "Quarterly",
        PaymentFrequency.SemiAnnual => "Semi-Annual",
        PaymentFrequency.Annual     => "Annual",
        _                           => PaymentFrequency.ToString()
    };
}

public class LoanPaymentDto
{
    public int PaymentId { get; set; }
    public int LoanId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal TotalPayment { get; set; }
    public decimal PrincipalPortion { get; set; }
    public decimal InterestPortion { get; set; }
    public decimal RemainingBalance { get; set; }
    public string? Notes { get; set; }
    public bool IsSampleData { get; set; }
}

public class BudgetEntryDto
{
    public int BudgetEntryId { get; set; }
    public int FiscalYear { get; set; }
    public string Category { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public int Month { get; set; }
    public decimal BudgetAmount { get; set; }
    public bool IsSampleData { get; set; }
}

// --- Report DTOs ---

public class CategoryLineItem
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class BudgetComparisonItem
{
    public string Category { get; set; } = string.Empty;
    public decimal Actual { get; set; }
    public decimal Budget { get; set; }
    public decimal Variance => Budget - Actual;
    public double PercentUsed => Budget == 0 ? 0 : (double)(Actual / Budget * 100);
}

public class AssetLineItem
{
    public string Name { get; set; } = string.Empty;
    public decimal CostBasis { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal BookValue => CostBasis - AccumulatedDepreciation;
}

public class LoanLineItem
{
    public string LenderName { get; set; } = string.Empty;
    public string LoanType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class CapitalGainEvent
{
    public string AssetDescription { get; set; } = string.Empty;
    public DateTime AcquisitionDate { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal CostBasis { get; set; }
    public decimal SalePrice { get; set; }
    public decimal GainOrLoss => SalePrice - CostBasis;
    public bool IsLongTerm => (SaleDate - AcquisitionDate).TotalDays > 365;
}

public class ScheduleFLineItem
{
    public string LineDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class MonthlyBarDto
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
}

public class ProfitAndLossDto
{
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public IReadOnlyList<CategoryLineItem> IncomeByCategory { get; set; } = [];
    public IReadOnlyList<CategoryLineItem> ExpensesByCategory { get; set; } = [];
    public decimal GrossRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal InterestExpense { get; set; }
    public decimal DepreciationTotal { get; set; }
    public decimal NetFarmIncome { get; set; }
    public IReadOnlyList<BudgetComparisonItem> IncomeBudgetComparison { get; set; } = [];
    public IReadOnlyList<BudgetComparisonItem> ExpenseBudgetComparison { get; set; } = [];
}

public class BalanceSheetDto
{
    public DateTime AsOf { get; set; }
    public decimal CashApproximate { get; set; }
    public decimal LivestockInventoryValue { get; set; }
    public decimal TotalCurrentAssets { get; set; }
    public IReadOnlyList<AssetLineItem> FixedAssets { get; set; } = [];
    public decimal TotalFixedAssetsGross { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal TotalFixedAssetsNet { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal OperatingLineBalance { get; set; }
    public decimal TotalCurrentLiabilities { get; set; }
    public IReadOnlyList<LoanLineItem> LongTermLoans { get; set; } = [];
    public decimal TotalLongTermLiabilities { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal OwnersEquity { get; set; }
}

public class CashFlowDto
{
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public decimal OperatingActivities { get; set; }
    public decimal InvestingActivities { get; set; }
    public decimal FinancingActivities { get; set; }
    public decimal NetCashChange { get; set; }
    public IReadOnlyList<CategoryLineItem> OperatingItems { get; set; } = [];
    public IReadOnlyList<CategoryLineItem> InvestingItems { get; set; } = [];
    public IReadOnlyList<CategoryLineItem> FinancingItems { get; set; } = [];
}

public class TaxSummaryDto
{
    public int FiscalYear { get; set; }
    public decimal TotalGrossSales { get; set; }
    public decimal TotalOtherIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetFarmIncome { get; set; }
    public decimal SelfEmploymentTax { get; set; }
    public decimal EstimatedFederalTax { get; set; }
    public decimal EstimatedStateTax { get; set; }
    public IReadOnlyList<CapitalGainEvent> CapitalGainEvents { get; set; } = [];
    public IReadOnlyList<ScheduleFLineItem> ScheduleFItems { get; set; } = [];
}

public class FinancialKpiDto
{
    public int FiscalYear { get; set; }
    public decimal NetFarmIncome { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal DebtToAssetRatio { get; set; }
    public decimal CurrentRatio { get; set; }
    public decimal ReturnOnAssets { get; set; }
    public decimal AvgCostPerHead { get; set; }
    public decimal BreakEvenPrice { get; set; }
    public IReadOnlyList<MonthlyBarDto> MonthlyData { get; set; } = [];
}
