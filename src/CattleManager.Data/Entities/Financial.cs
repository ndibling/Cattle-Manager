using CattleManager.Core.Models;

namespace CattleManager.Data.Entities;

public class Transaction
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
    public bool IsSampleData { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public Animal? LinkedAnimal { get; set; }
}

public class Asset
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
    public DateTime? DisposedDate { get; set; }
    public decimal? DisposalPrice { get; set; }
    public string? Notes { get; set; }
    public bool IsSampleData { get; set; }
    public DateTime CreatedDate { get; set; }

    public Animal? LinkedAnimal { get; set; }
}

public class Loan
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
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public bool IsSampleData { get; set; }
    public DateTime CreatedDate { get; set; }

    public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
}

public class LoanPayment
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

    public Loan Loan { get; set; } = null!;
}

public class BudgetEntry
{
    public int BudgetEntryId { get; set; }
    public int FiscalYear { get; set; }
    public string Category { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public int Month { get; set; }
    public decimal BudgetAmount { get; set; }
    public bool IsSampleData { get; set; }
}
