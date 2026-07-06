using CattleManager.Core.Models;

namespace CattleManager.Core.Services;

public interface ITransactionRepository
{
    Task<IReadOnlyList<TransactionDto>> GetAllAsync();
    Task<IReadOnlyList<TransactionDto>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<IReadOnlyList<TransactionDto>> GetByTypeAsync(TransactionType type);
    Task<TransactionDto> AddAsync(TransactionDto dto);
    Task<TransactionDto> UpdateAsync(TransactionDto dto);
    Task DeleteAsync(int id);
    Task DeleteSampleDataAsync();
}

public interface IAssetRepository
{
    Task<IReadOnlyList<AssetDto>> GetAllAsync();
    Task<IReadOnlyList<AssetDto>> GetActiveAsync();
    Task<IReadOnlyList<AssetDto>> GetByAnimalAsync(int animalId);
    Task<AssetDto?> GetByTransactionIdAsync(int transactionId);
    Task<AssetDto> AddAsync(AssetDto dto);
    Task<AssetDto> UpdateAsync(AssetDto dto);
    Task DeleteAsync(int id);
    Task DeleteSampleDataAsync();
}

public interface ILoanRepository
{
    Task<IReadOnlyList<LoanDto>> GetAllAsync();
    Task<IReadOnlyList<LoanDto>> GetActiveAsync();
    Task<IReadOnlyList<LoanPaymentDto>> GetPaymentsAsync(int loanId);
    Task<IReadOnlyList<LoanPaymentDto>> GetAllPaymentsInRangeAsync(DateTime from, DateTime to);
    Task<LoanDto> AddAsync(LoanDto dto);
    Task<LoanDto> UpdateAsync(LoanDto dto);
    Task DeleteAsync(int id);
    Task<LoanPaymentDto> AddPaymentAsync(LoanPaymentDto dto);
    Task DeletePaymentAsync(int id);
    Task DeleteSampleDataAsync();
}

public interface IBudgetRepository
{
    Task<IReadOnlyList<BudgetEntryDto>> GetByFiscalYearAsync(int year);
    Task<BudgetEntryDto> UpsertAsync(BudgetEntryDto dto);
    Task DeleteByYearAsync(int year);
    Task DeleteByCategoryAndYearAsync(string category, int year);
    Task DeleteSampleDataAsync();
}
