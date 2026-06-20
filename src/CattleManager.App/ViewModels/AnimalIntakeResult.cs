namespace CattleManager.App.ViewModels;

public record AnimalIntakeResult(
    bool BornOnFarm,
    string? BreedersName,
    string? CurrentOwner,
    string? BreedersAddress,
    string? SellerName,
    string? SellerAddress,
    DateTime? PurchaseDate,
    decimal? PurchasePrice,
    string? ExpenseCategoryKey,
    string? ExpenseNotes
);
