using CattleManager.App.Services;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CattleManager.App.ViewModels;

public partial class AssetFormViewModel : ObservableObject
{
    private readonly IAssetRepository       _assets;
    private readonly ITransactionRepository _transactions;
    private readonly IAnimalRepository      _animals;
    private readonly NavigationService      _nav;
    private readonly DialogService          _dialog;

    private int?           _pendingLinkedAnimalId;
    private int?           _linkedTransactionId;    // set from dto on edit, or after creating a new tx
    private TransactionDto? _linkedTransaction;     // full tx loaded on edit, for in-place update

    private static readonly IReadOnlyList<CategoryOption> AssetCategoryOptions =
    [
        new("Livestock",           "Livestock"),
        new("MachineryEquipment",  "Machinery & Equipment"),
        new("Land",                "Land"),
        new("Building",            "Building"),
        new("Vehicle",             "Vehicle"),
        new("Other",               "Other"),
    ];

    private static readonly IReadOnlyList<CategoryOption> DepreciationOptions =
    [
        new("StraightLine", "Straight Line"),
        new("DB150",        "150% Declining Balance"),
        new("Section179",   "Section 179 (full year 1)"),
    ];

    // Map asset category → expense category key
    private static string ExpenseCategoryKey(string assetCategoryKey) => assetCategoryKey switch
    {
        "Livestock"          => "LivestockPurchase",
        "MachineryEquipment" => "FarmEquipment",
        "Vehicle"            => "FarmEquipment",
        _                    => "Other",
    };

    [ObservableProperty] private int _assetId;
    [ObservableProperty] private string _assetName = string.Empty;
    [ObservableProperty] private CategoryOption? _selectedCategory;
    [ObservableProperty] private DateTime _purchaseDate = DateTime.Today;
    [ObservableProperty] private string _purchasePriceText = string.Empty;
    [ObservableProperty] private string _currentValueText = string.Empty;
    [ObservableProperty] private CategoryOption? _selectedDepreciation;
    [ObservableProperty] private string _usefulLifeYearsText = "7";
    [ObservableProperty] private string _salvageValueText = "0";
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private AnimalDto? _linkedAnimal;
    [ObservableProperty] private bool _isNew;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private bool _isDisposalExpanded;
    [ObservableProperty] private DateTime _disposalDate = DateTime.Today;
    [ObservableProperty] private string _disposalPriceText = string.Empty;
    [ObservableProperty] private decimal _gainOrLoss;
    [ObservableProperty] private string? _validationError;
    [ObservableProperty] private ObservableCollection<AnimalDto> _animalOptions = [];

    // --- Expense-link fields ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowExpenseFields))]
    private bool _createExpense = true;

    [ObservableProperty] private string _expenseVendorName = string.Empty;

    // True when no linked transaction exists yet (new, or edit without prior link)
    public bool HasLinkedExpense => _linkedTransactionId.HasValue;

    // Show the vendor / note fields only when user wants to create one and none exists yet
    public bool ShowExpenseFields => CreateExpense && !HasLinkedExpense;

    // ---

    public IReadOnlyList<CategoryOption> CategoryOptions          { get; } = AssetCategoryOptions;
    public IReadOnlyList<CategoryOption> DepreciationMethodOptions { get; } = DepreciationOptions;

    public string FormTitle => IsNew ? "Add Asset" : $"Edit Asset — {AssetName}";

    public bool IsDepreciable => SelectedCategory?.Key != "Livestock";
    public bool IsLivestock   => SelectedCategory?.Key == "Livestock";

    public bool ShowDepreciationDetails =>
        IsDepreciable && SelectedDepreciation?.Key is null or "StraightLine" or "DB150";

    public bool CanDispose => !IsNew && IsActive;

    public AssetFormViewModel(IAssetRepository assets, ITransactionRepository transactions,
        IAnimalRepository animals, NavigationService nav, DialogService dialog)
    {
        _assets       = assets;
        _transactions = transactions;
        _animals      = animals;
        _nav          = nav;
        _dialog       = dialog;
        SelectedCategory     = AssetCategoryOptions[0];
        SelectedDepreciation = DepreciationOptions[0];
    }

    public void InitNew()
    {
        IsNew         = true;
        CreateExpense = true;
        OnPropertyChanged(nameof(FormTitle));
    }

    public void InitEdit(AssetDto dto)
    {
        IsNew               = false;
        IsActive            = dto.IsActive;
        AssetId             = dto.AssetId;
        AssetName           = dto.AssetName;
        PurchaseDate        = dto.PurchaseDate;
        PurchasePriceText   = dto.PurchasePrice.ToString("F2");
        CurrentValueText    = dto.CurrentValue?.ToString("F2") ?? string.Empty;
        UsefulLifeYearsText = dto.UsefulLifeYears.ToString();
        SalvageValueText    = dto.SalvageValue.ToString("F2");
        Notes               = dto.Notes ?? string.Empty;
        _pendingLinkedAnimalId = dto.LinkedAnimalId;
        _linkedTransactionId   = dto.LinkedTransactionId;

        SelectedCategory = CategoryOptions.FirstOrDefault(
            c => c.Key == dto.Category.ToString()) ?? CategoryOptions[0];
        SelectedDepreciation = DepreciationMethodOptions.FirstOrDefault(
            d => d.Key == dto.DepreciationMethod.ToString()) ?? DepreciationMethodOptions[0];

        // If already linked, CreateExpense = true (always sync); no vendor field needed
        CreateExpense = _linkedTransactionId.HasValue;

        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(CanDispose));
        OnPropertyChanged(nameof(ShowDepreciationDetails));
        OnPropertyChanged(nameof(HasLinkedExpense));
        OnPropertyChanged(nameof(ShowExpenseFields));
    }

    public async Task LoadAsync()
    {
        var list = await _animals.GetAllAsync();
        AnimalOptions = new ObservableCollection<AnimalDto>(list.OrderBy(a => a.BarnName));
        if (_pendingLinkedAnimalId.HasValue)
            LinkedAnimal = AnimalOptions.FirstOrDefault(a => a.AnimalId == _pendingLinkedAnimalId.Value);

        // Load linked transaction for edit so we can update it in-place (preserving payee, notes, etc.)
        if (_linkedTransactionId.HasValue)
        {
            _linkedTransaction = await _transactions.GetByIdAsync(_linkedTransactionId.Value);
            if (_linkedTransaction is not null)
                ExpenseVendorName = _linkedTransaction.PayeePayer ?? string.Empty;
        }
    }

    partial void OnSelectedCategoryChanged(CategoryOption? value)
    {
        OnPropertyChanged(nameof(IsDepreciable));
        OnPropertyChanged(nameof(IsLivestock));
        OnPropertyChanged(nameof(ShowDepreciationDetails));
    }

    partial void OnSelectedDepreciationChanged(CategoryOption? value)
    {
        OnPropertyChanged(nameof(ShowDepreciationDetails));
        if (value?.Key == "Section179")
        {
            UsefulLifeYearsText = "1";
            SalvageValueText    = "0";
        }
    }

    partial void OnDisposalPriceTextChanged(string value) => RecalcGainOrLoss();
    partial void OnPurchasePriceTextChanged(string value) => RecalcGainOrLoss();

    private void RecalcGainOrLoss()
    {
        decimal.TryParse(DisposalPriceText, out var disposal);
        decimal.TryParse(PurchasePriceText, out var cost);
        GainOrLoss = disposal - cost;
    }

    [RelayCommand]
    private void ToggleDisposal()
    {
        IsDisposalExpanded = !IsDisposalExpanded;
        if (IsDisposalExpanded && string.IsNullOrWhiteSpace(DisposalPriceText))
            RecalcGainOrLoss();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var error = Validate();
        if (error is not null) { ValidationError = error; return; }
        ValidationError = null;

        decimal.TryParse(PurchasePriceText, out var purchasePrice);
        var assetDto = BuildDto();

        if (IsNew)
        {
            var saved = await _assets.AddAsync(assetDto);
            AssetId = saved.AssetId;
            await SyncExpenseAsync(saved, purchasePrice);
        }
        else
        {
            await _assets.UpdateAsync(assetDto);
            await SyncExpenseAsync(assetDto, purchasePrice);
        }

        _nav.GoBack();
    }

    private async Task SyncExpenseAsync(AssetDto asset, decimal purchasePrice)
    {
        if (!CreateExpense) return;

        if (_linkedTransactionId.HasValue && _linkedTransaction is not null)
        {
            // Update the existing transaction: keep all original fields, sync price / date / description
            _linkedTransaction.Amount        = purchasePrice;
            _linkedTransaction.Date          = PurchaseDate;
            _linkedTransaction.Description   = string.IsNullOrWhiteSpace(AssetName)
                                                ? _linkedTransaction.Description
                                                : AssetName.Trim();
            _linkedTransaction.LinkedAnimalId = LinkedAnimal?.AnimalId;
            if (!string.IsNullOrWhiteSpace(ExpenseVendorName))
                _linkedTransaction.PayeePayer = ExpenseVendorName.Trim();
            await _transactions.UpdateAsync(_linkedTransaction);
        }
        else
        {
            // Create a new expense and link it back to the asset
            var tx = await _transactions.AddAsync(new TransactionDto
            {
                TransactionType = TransactionType.Expense,
                Category        = ExpenseCategoryKey(SelectedCategory?.Key ?? "Other"),
                Date            = PurchaseDate,
                Amount          = purchasePrice,
                Description     = string.IsNullOrWhiteSpace(AssetName) ? "Asset purchase" : AssetName.Trim(),
                PayeePayer      = string.IsNullOrWhiteSpace(ExpenseVendorName) ? null : ExpenseVendorName.Trim(),
                LinkedAnimalId  = LinkedAnimal?.AnimalId,
                Notes           = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            });

            _linkedTransactionId = tx.TransactionId;
            _linkedTransaction   = tx;

            // Write the link back onto the asset record
            asset.LinkedTransactionId = tx.TransactionId;
            await _assets.UpdateAsync(asset);

            OnPropertyChanged(nameof(HasLinkedExpense));
            OnPropertyChanged(nameof(ShowExpenseFields));
        }
    }

    [RelayCommand]
    private void Cancel() => _nav.GoBack();

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(AssetName))
            return "Asset name is required.";
        if (!decimal.TryParse(PurchasePriceText, out var price) || price < 0)
            return "Purchase price must be a valid amount.";
        if (SelectedCategory is null)
            return "Category is required.";
        if (IsDisposalExpanded)
        {
            if (!decimal.TryParse(DisposalPriceText, out var dp) || dp < 0)
                return "Disposal price must be a valid amount.";
        }
        return null;
    }

    private AssetDto BuildDto()
    {
        decimal.TryParse(PurchasePriceText, out var purchasePrice);
        decimal.TryParse(CurrentValueText,  out var currentValue);
        decimal.TryParse(SalvageValueText,  out var salvageValue);
        int.TryParse(UsefulLifeYearsText,   out var usefulLife);

        DateTime? disposedDate = IsDisposalExpanded ? DisposalDate : null;
        decimal? disposalPrice = null;
        if (IsDisposalExpanded && decimal.TryParse(DisposalPriceText, out var dp))
            disposalPrice = dp;

        return new AssetDto
        {
            AssetId             = AssetId,
            AssetName           = AssetName.Trim(),
            Category            = Enum.Parse<AssetCategory>(SelectedCategory!.Key),
            PurchaseDate        = PurchaseDate,
            PurchasePrice       = purchasePrice,
            CurrentValue        = string.IsNullOrWhiteSpace(CurrentValueText) ? null : currentValue,
            DepreciationMethod  = Enum.Parse<DepreciationMethod>(SelectedDepreciation!.Key),
            UsefulLifeYears     = usefulLife,
            SalvageValue        = salvageValue,
            LinkedAnimalId      = LinkedAnimal?.AnimalId,
            LinkedTransactionId = _linkedTransactionId,
            Notes               = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            DisposedDate        = disposedDate,
            DisposalPrice       = disposalPrice,
        };
    }
}
