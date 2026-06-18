using CattleManager.App.Services;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CattleManager.App.ViewModels;

public partial class AssetFormViewModel : ObservableObject
{
    private readonly IAssetRepository _assets;
    private readonly IAnimalRepository _animals;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    private int? _pendingLinkedAnimalId;

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

    public IReadOnlyList<CategoryOption> CategoryOptions { get; } = AssetCategoryOptions;
    public IReadOnlyList<CategoryOption> DepreciationMethodOptions { get; } = DepreciationOptions;

    public string FormTitle => IsNew ? "Add Asset" : $"Edit Asset — {AssetName}";

    public bool ShowDepreciationDetails =>
        SelectedDepreciation?.Key is null or "StraightLine" or "DB150";

    public bool CanDispose => !IsNew && IsActive;

    public AssetFormViewModel(IAssetRepository assets, IAnimalRepository animals,
        NavigationService nav, DialogService dialog)
    {
        _assets  = assets;
        _animals = animals;
        _nav     = nav;
        _dialog  = dialog;
        SelectedCategory    = AssetCategoryOptions[0];
        SelectedDepreciation = DepreciationOptions[0];
    }

    public void InitNew()
    {
        IsNew = true;
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

        SelectedCategory = CategoryOptions.FirstOrDefault(
            c => c.Key == dto.Category.ToString()) ?? CategoryOptions[0];
        SelectedDepreciation = DepreciationMethodOptions.FirstOrDefault(
            d => d.Key == dto.DepreciationMethod.ToString()) ?? DepreciationMethodOptions[0];

        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(CanDispose));
        OnPropertyChanged(nameof(ShowDepreciationDetails));
    }

    public async Task LoadAsync()
    {
        var list = await _animals.GetAllAsync();
        AnimalOptions = new ObservableCollection<AnimalDto>(list.OrderBy(a => a.BarnName));
        if (_pendingLinkedAnimalId.HasValue)
            LinkedAnimal = AnimalOptions.FirstOrDefault(a => a.AnimalId == _pendingLinkedAnimalId.Value);
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

        var dto = BuildDto();
        if (IsNew)
            await _assets.AddAsync(dto);
        else
            await _assets.UpdateAsync(dto);

        _nav.GoBack();
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
        decimal.TryParse(CurrentValueText, out var currentValue);
        decimal.TryParse(SalvageValueText, out var salvageValue);
        int.TryParse(UsefulLifeYearsText, out var usefulLife);

        DateTime? disposedDate  = IsDisposalExpanded ? DisposalDate : null;
        decimal? disposalPrice  = null;
        if (IsDisposalExpanded && decimal.TryParse(DisposalPriceText, out var dp))
            disposalPrice = dp;

        return new AssetDto
        {
            AssetId          = AssetId,
            AssetName        = AssetName.Trim(),
            Category         = Enum.Parse<AssetCategory>(SelectedCategory!.Key),
            PurchaseDate     = PurchaseDate,
            PurchasePrice    = purchasePrice,
            CurrentValue     = string.IsNullOrWhiteSpace(CurrentValueText) ? null : currentValue,
            DepreciationMethod = Enum.Parse<DepreciationMethod>(SelectedDepreciation!.Key),
            UsefulLifeYears  = usefulLife,
            SalvageValue     = salvageValue,
            LinkedAnimalId   = LinkedAnimal?.AnimalId,
            Notes            = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            DisposedDate     = disposedDate,
            DisposalPrice    = disposalPrice,
        };
    }
}
