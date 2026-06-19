using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CattleManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CattleManager.Data.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly CattleDbContext _db;
    public TransactionRepository(CattleDbContext db) => _db = db;

    public async Task<IReadOnlyList<TransactionDto>> GetAllAsync()
    {
        var list = await _db.Transactions
            .Include(t => t.LinkedAnimal)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<TransactionDto>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var list = await _db.Transactions
            .Include(t => t.LinkedAnimal)
            .Where(t => t.Date >= from && t.Date <= to)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<TransactionDto>> GetByTypeAsync(TransactionType type)
    {
        var list = await _db.Transactions
            .Include(t => t.LinkedAnimal)
            .Where(t => t.TransactionType == type)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<TransactionDto> AddAsync(TransactionDto dto)
    {
        var e = new Transaction
        {
            TransactionType = dto.TransactionType,
            Category = dto.Category,
            Date = dto.Date,
            Amount = dto.Amount,
            Description = dto.Description,
            PayeePayer = dto.PayeePayer,
            PaymentMethod = dto.PaymentMethod,
            Notes = dto.Notes,
            AttachmentPath = dto.AttachmentPath,
            LinkedAnimalId = dto.LinkedAnimalId,
            TaxRate = dto.TaxRate,
            TaxAmount = dto.TaxAmount,
            IsSampleData = dto.IsSampleData,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        _db.Transactions.Add(e);
        await _db.SaveChangesAsync();
        dto.TransactionId = e.TransactionId;
        return dto;
    }

    public async Task<TransactionDto> UpdateAsync(TransactionDto dto)
    {
        var e = await _db.Transactions.FindAsync(dto.TransactionId)
            ?? throw new ArgumentException($"Transaction {dto.TransactionId} not found");
        e.TransactionType = dto.TransactionType;
        e.Category = dto.Category;
        e.Date = dto.Date;
        e.Amount = dto.Amount;
        e.Description = dto.Description;
        e.PayeePayer = dto.PayeePayer;
        e.PaymentMethod = dto.PaymentMethod;
        e.Notes = dto.Notes;
        e.AttachmentPath = dto.AttachmentPath;
        e.LinkedAnimalId = dto.LinkedAnimalId;
        e.TaxRate = dto.TaxRate;
        e.TaxAmount = dto.TaxAmount;
        e.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.Transactions.FindAsync(id);
        if (e is not null) { _db.Transactions.Remove(e); await _db.SaveChangesAsync(); }
    }

    public async Task DeleteSampleDataAsync()
    {
        var records = await _db.Transactions.Where(t => t.IsSampleData).ToListAsync();
        _db.Transactions.RemoveRange(records);
        await _db.SaveChangesAsync();
    }

    private static TransactionDto Map(Transaction e) => new()
    {
        TransactionId = e.TransactionId,
        TransactionType = e.TransactionType,
        Category = e.Category,
        Date = e.Date,
        Amount = e.Amount,
        Description = e.Description,
        PayeePayer = e.PayeePayer,
        PaymentMethod = e.PaymentMethod,
        Notes = e.Notes,
        AttachmentPath = e.AttachmentPath,
        LinkedAnimalId = e.LinkedAnimalId,
        LinkedAnimalName = e.LinkedAnimal?.BarnName,
        TaxRate = e.TaxRate,
        TaxAmount = e.TaxAmount,
        IsSampleData = e.IsSampleData,
        CreatedDate = e.CreatedDate,
        ModifiedDate = e.ModifiedDate
    };
}

public class AssetRepository : IAssetRepository
{
    private readonly CattleDbContext _db;
    public AssetRepository(CattleDbContext db) => _db = db;

    public async Task<IReadOnlyList<AssetDto>> GetAllAsync()
    {
        var list = await _db.Assets
            .Include(a => a.LinkedAnimal)
            .OrderBy(a => a.AssetName)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AssetDto>> GetActiveAsync()
    {
        var list = await _db.Assets
            .Include(a => a.LinkedAnimal)
            .Where(a => a.DisposedDate == null)
            .OrderBy(a => a.AssetName)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AssetDto>> GetByAnimalAsync(int animalId)
    {
        var list = await _db.Assets
            .Where(a => a.LinkedAnimalId == animalId)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<AssetDto> AddAsync(AssetDto dto)
    {
        var e = new Asset
        {
            AssetName = dto.AssetName,
            Category = dto.Category,
            PurchaseDate = dto.PurchaseDate,
            PurchasePrice = dto.PurchasePrice,
            CurrentValue = dto.CurrentValue,
            DepreciationMethod = dto.DepreciationMethod,
            UsefulLifeYears = dto.UsefulLifeYears,
            SalvageValue = dto.SalvageValue,
            LinkedAnimalId = dto.LinkedAnimalId,
            DisposedDate = dto.DisposedDate,
            DisposalPrice = dto.DisposalPrice,
            Notes = dto.Notes,
            IsSampleData = dto.IsSampleData,
            CreatedDate = DateTime.UtcNow
        };
        _db.Assets.Add(e);
        await _db.SaveChangesAsync();
        dto.AssetId = e.AssetId;
        return dto;
    }

    public async Task<AssetDto> UpdateAsync(AssetDto dto)
    {
        var e = await _db.Assets.FindAsync(dto.AssetId)
            ?? throw new ArgumentException($"Asset {dto.AssetId} not found");
        e.AssetName = dto.AssetName;
        e.Category = dto.Category;
        e.PurchaseDate = dto.PurchaseDate;
        e.PurchasePrice = dto.PurchasePrice;
        e.CurrentValue = dto.CurrentValue;
        e.DepreciationMethod = dto.DepreciationMethod;
        e.UsefulLifeYears = dto.UsefulLifeYears;
        e.SalvageValue = dto.SalvageValue;
        e.LinkedAnimalId = dto.LinkedAnimalId;
        e.DisposedDate = dto.DisposedDate;
        e.DisposalPrice = dto.DisposalPrice;
        e.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.Assets.FindAsync(id);
        if (e is not null) { _db.Assets.Remove(e); await _db.SaveChangesAsync(); }
    }

    public async Task DeleteSampleDataAsync()
    {
        var records = await _db.Assets.Where(a => a.IsSampleData).ToListAsync();
        _db.Assets.RemoveRange(records);
        await _db.SaveChangesAsync();
    }

    private static AssetDto Map(Asset e) => new()
    {
        AssetId = e.AssetId,
        AssetName = e.AssetName,
        Category = e.Category,
        PurchaseDate = e.PurchaseDate,
        PurchasePrice = e.PurchasePrice,
        CurrentValue = e.CurrentValue,
        DepreciationMethod = e.DepreciationMethod,
        UsefulLifeYears = e.UsefulLifeYears,
        SalvageValue = e.SalvageValue,
        LinkedAnimalId = e.LinkedAnimalId,
        LinkedAnimalName = e.LinkedAnimal?.BarnName,
        DisposedDate = e.DisposedDate,
        DisposalPrice = e.DisposalPrice,
        Notes = e.Notes,
        IsSampleData = e.IsSampleData,
        CreatedDate = e.CreatedDate
    };
}

public class LoanRepository : ILoanRepository
{
    private readonly CattleDbContext _db;
    public LoanRepository(CattleDbContext db) => _db = db;

    public async Task<IReadOnlyList<LoanDto>> GetAllAsync()
    {
        var list = await _db.Loans.OrderBy(l => l.LenderName).ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<LoanDto>> GetActiveAsync()
    {
        var list = await _db.Loans.Where(l => l.IsActive).OrderBy(l => l.LenderName).ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<LoanPaymentDto>> GetPaymentsAsync(int loanId)
    {
        var list = await _db.LoanPayments
            .Where(p => p.LoanId == loanId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
        return list.Select(MapPayment).ToList();
    }

    public async Task<IReadOnlyList<LoanPaymentDto>> GetAllPaymentsInRangeAsync(DateTime from, DateTime to)
    {
        var list = await _db.LoanPayments
            .Where(p => p.PaymentDate >= from && p.PaymentDate <= to)
            .ToListAsync();
        return list.Select(MapPayment).ToList();
    }

    public async Task<LoanDto> AddAsync(LoanDto dto)
    {
        var e = new Loan
        {
            LenderName = dto.LenderName,
            LoanType = dto.LoanType,
            OriginalPrincipal = dto.OriginalPrincipal,
            InterestRate = dto.InterestRate,
            StartDate = dto.StartDate,
            MaturityDate = dto.MaturityDate,
            PaymentFrequency = dto.PaymentFrequency,
            PaymentAmount = dto.PaymentAmount,
            IsActive = dto.IsActive,
            Notes = dto.Notes,
            IsSampleData = dto.IsSampleData,
            CreatedDate = DateTime.UtcNow
        };
        _db.Loans.Add(e);
        await _db.SaveChangesAsync();
        dto.LoanId = e.LoanId;
        return dto;
    }

    public async Task<LoanDto> UpdateAsync(LoanDto dto)
    {
        var e = await _db.Loans.FindAsync(dto.LoanId)
            ?? throw new ArgumentException($"Loan {dto.LoanId} not found");
        e.LenderName = dto.LenderName;
        e.LoanType = dto.LoanType;
        e.OriginalPrincipal = dto.OriginalPrincipal;
        e.InterestRate = dto.InterestRate;
        e.StartDate = dto.StartDate;
        e.MaturityDate = dto.MaturityDate;
        e.PaymentFrequency = dto.PaymentFrequency;
        e.PaymentAmount = dto.PaymentAmount;
        e.IsActive = dto.IsActive;
        e.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.Loans.FindAsync(id);
        if (e is not null) { _db.Loans.Remove(e); await _db.SaveChangesAsync(); }
    }

    public async Task<LoanPaymentDto> AddPaymentAsync(LoanPaymentDto dto)
    {
        var e = new LoanPayment
        {
            LoanId = dto.LoanId,
            PaymentDate = dto.PaymentDate,
            TotalPayment = dto.TotalPayment,
            PrincipalPortion = dto.PrincipalPortion,
            InterestPortion = dto.InterestPortion,
            RemainingBalance = dto.RemainingBalance,
            Notes = dto.Notes,
            IsSampleData = dto.IsSampleData
        };
        _db.LoanPayments.Add(e);
        await _db.SaveChangesAsync();
        dto.PaymentId = e.PaymentId;
        return dto;
    }

    public async Task DeletePaymentAsync(int id)
    {
        var e = await _db.LoanPayments.FindAsync(id);
        if (e is not null) { _db.LoanPayments.Remove(e); await _db.SaveChangesAsync(); }
    }

    public async Task DeleteSampleDataAsync()
    {
        var payments = await _db.LoanPayments.Where(p => p.IsSampleData).ToListAsync();
        _db.LoanPayments.RemoveRange(payments);
        var loans = await _db.Loans.Where(l => l.IsSampleData).ToListAsync();
        _db.Loans.RemoveRange(loans);
        await _db.SaveChangesAsync();
    }

    private static LoanDto Map(Loan e) => new()
    {
        LoanId = e.LoanId,
        LenderName = e.LenderName,
        LoanType = e.LoanType,
        OriginalPrincipal = e.OriginalPrincipal,
        InterestRate = e.InterestRate,
        StartDate = e.StartDate,
        MaturityDate = e.MaturityDate,
        PaymentFrequency = e.PaymentFrequency,
        PaymentAmount = e.PaymentAmount,
        IsActive = e.IsActive,
        Notes = e.Notes,
        IsSampleData = e.IsSampleData,
        CreatedDate = e.CreatedDate
    };

    private static LoanPaymentDto MapPayment(LoanPayment e) => new()
    {
        PaymentId = e.PaymentId,
        LoanId = e.LoanId,
        PaymentDate = e.PaymentDate,
        TotalPayment = e.TotalPayment,
        PrincipalPortion = e.PrincipalPortion,
        InterestPortion = e.InterestPortion,
        RemainingBalance = e.RemainingBalance,
        Notes = e.Notes,
        IsSampleData = e.IsSampleData
    };
}

public class BudgetRepository : IBudgetRepository
{
    private readonly CattleDbContext _db;
    public BudgetRepository(CattleDbContext db) => _db = db;

    public async Task<IReadOnlyList<BudgetEntryDto>> GetByFiscalYearAsync(int year)
    {
        var list = await _db.BudgetEntries
            .Where(b => b.FiscalYear == year)
            .OrderBy(b => b.TransactionType)
            .ThenBy(b => b.Category)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<BudgetEntryDto> UpsertAsync(BudgetEntryDto dto)
    {
        var e = await _db.BudgetEntries.FirstOrDefaultAsync(b =>
            b.FiscalYear == dto.FiscalYear &&
            b.Category == dto.Category &&
            b.TransactionType == dto.TransactionType &&
            b.Month == dto.Month);

        if (e is null)
        {
            e = new BudgetEntry
            {
                FiscalYear = dto.FiscalYear,
                Category = dto.Category,
                TransactionType = dto.TransactionType,
                Month = dto.Month,
                IsSampleData = dto.IsSampleData
            };
            _db.BudgetEntries.Add(e);
        }
        e.BudgetAmount = dto.BudgetAmount;
        await _db.SaveChangesAsync();
        dto.BudgetEntryId = e.BudgetEntryId;
        return dto;
    }

    public async Task DeleteByYearAsync(int year)
    {
        var records = await _db.BudgetEntries.Where(b => b.FiscalYear == year).ToListAsync();
        _db.BudgetEntries.RemoveRange(records);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteByCategoryAndYearAsync(string category, int year)
    {
        var records = await _db.BudgetEntries
            .Where(b => b.FiscalYear == year && b.Category == category)
            .ToListAsync();
        _db.BudgetEntries.RemoveRange(records);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteSampleDataAsync()
    {
        var records = await _db.BudgetEntries.Where(b => b.IsSampleData).ToListAsync();
        _db.BudgetEntries.RemoveRange(records);
        await _db.SaveChangesAsync();
    }

    private static BudgetEntryDto Map(BudgetEntry e) => new()
    {
        BudgetEntryId = e.BudgetEntryId,
        FiscalYear = e.FiscalYear,
        Category = e.Category,
        TransactionType = e.TransactionType,
        Month = e.Month,
        BudgetAmount = e.BudgetAmount,
        IsSampleData = e.IsSampleData
    };
}
