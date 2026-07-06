using CattleManager.App.Services;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;

namespace CattleManager.App.ViewModels;

public record CategoryOption(string Key, string Display);

public partial class TransactionFormViewModel : ObservableObject
{
    private readonly ITransactionRepository _transactions;
    private readonly IAssetRepository       _assets;
    private readonly IAnimalRepository      _animals;
    private readonly NavigationService      _nav;
    private readonly DialogService          _dialog;
    private readonly ExportService          _export;
    private readonly IFarmRepository        _farms;

    private int? _pendingLinkedAnimalId;
    private int? _linkedAssetId; // set when editing a transaction that already has a linked asset

    private static readonly System.Collections.Generic.HashSet<string> _imageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff" };

    public bool IsImageAttachment =>
        AttachmentPath is not null &&
        _imageExtensions.Contains(System.IO.Path.GetExtension(AttachmentPath));

    private static readonly IReadOnlyList<CategoryOption> ExpenseCategories =
    [
        new("LivestockPurchase",     "Livestock Purchase"),
        new("FeedHay",               "Feed & Hay"),
        new("VeterinaryMedical",     "Veterinary & Medical"),
        new("BreedingFees",          "Breeding Fees / AI"),
        new("FuelOil",               "Fuel & Oil"),
        new("RepairsMaintenance",    "Repairs & Maintenance"),
        new("Utilities",             "Utilities"),
        new("LaborContractWork",     "Labor / Contract Work"),
        new("TruckingTransportation","Trucking / Transportation"),
        new("Insurance",             "Insurance"),
        new("PropertyTaxes",         "Property Taxes"),
        new("MarketingAuction",      "Marketing / Auction Fees"),
        new("SuppliesMiscellaneous", "Supplies & Miscellaneous"),
        new("InterestExpense",       "Interest Expense"),
        new("Other",                 "Other"),
    ];

    private static readonly IReadOnlyList<CategoryOption> IncomeCategories =
    [
        new("LivestockSales",      "Livestock Sales"),
        new("BreedingServices",    "Breeding Services"),
        new("HayCropSales",        "Hay / Crop Sales"),
        new("CustomWork",          "Custom Work Income"),
        new("GovernmentPayments",  "Government Payments"),
        new("InsuranceProceeds",   "Insurance Proceeds"),
        new("MiscellaneousIncome", "Miscellaneous Income"),
    ];

    private static readonly IReadOnlyList<CategoryOption> CapitalCategories =
    [
        new("Grant",             "Grant"),
        new("EquityInvestment",  "Equity Investment"),
        new("SharePurchase",     "Share Purchase"),
        new("Other",             "Other"),
    ];

    // Asset category and depreciation options (mirrors AssetFormViewModel)
    public IReadOnlyList<CategoryOption> AssetCategoryOptions { get; } =
    [
        new("Livestock",          "Livestock"),
        new("MachineryEquipment", "Machinery & Equipment"),
        new("Land",               "Land"),
        new("Building",           "Building"),
        new("Vehicle",            "Vehicle"),
        new("Other",              "Other"),
    ];

    public IReadOnlyList<CategoryOption> AssetDepreciationOptions { get; } =
    [
        new("StraightLine", "Straight Line"),
        new("DB150",        "150% Declining Balance"),
        new("Section179",   "Section 179 (full year 1)"),
    ];

    // --- Core transaction fields ---
    [ObservableProperty] private int _transactionId;
    [ObservableProperty] private string _transactionTypeStr = "Expense";
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private string _amountText = string.Empty;
    [ObservableProperty] private CategoryOption? _selectedCategory;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _payeePayer = string.Empty;
    [ObservableProperty] private string _paymentMethod = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string? _attachmentPath;
    [ObservableProperty] private AnimalDto? _linkedAnimal;
    [ObservableProperty] private string _taxRateText = "0";
    [ObservableProperty] private decimal _taxAmount;
    [ObservableProperty] private decimal _netProceeds;
    [ObservableProperty] private string? _validationError;
    [ObservableProperty] private bool _isNew;
    [ObservableProperty] private IReadOnlyList<CategoryOption> _categoryOptions = [];
    [ObservableProperty] private ObservableCollection<AnimalDto> _animalOptions = [];

    // --- Asset purchase fields ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAssetSection))]
    private bool _isAssetPurchase;

    [ObservableProperty] private string _assetName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAssetDepreciationDetails))]
    private CategoryOption? _selectedAssetCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAssetDepreciationDetails))]
    private CategoryOption? _selectedAssetDepreciation;

    [ObservableProperty] private string _assetUsefulLifeText = "7";
    [ObservableProperty] private string _assetSalvageValueText = "0";

    // ---

    public IReadOnlyList<string> TypeOptions { get; } = ["Income", "Expense", "Capital"];

    public string FormTitle => IsNew
        ? $"Add {TransactionTypeStr} Transaction"
        : $"Edit {TransactionTypeStr} Transaction";

    public bool IsExpenseType => CurrentType == TransactionType.Expense;

    public bool ShowAssetSection => IsExpenseType && IsAssetPurchase;

    public bool ShowAssetDepreciationDetails =>
        SelectedAssetDepreciation?.Key is null or "StraightLine" or "DB150";

    private TransactionType CurrentType => TransactionTypeStr switch
    {
        "Income"  => TransactionType.Income,
        "Capital" => TransactionType.CapitalInflux,
        _         => TransactionType.Expense
    };

    public TransactionFormViewModel(ITransactionRepository transactions,
        IAssetRepository assets, IAnimalRepository animals,
        NavigationService nav, DialogService dialog,
        ExportService export, IFarmRepository farms)
    {
        _transactions = transactions;
        _assets       = assets;
        _animals      = animals;
        _nav          = nav;
        _dialog       = dialog;
        _export       = export;
        _farms        = farms;
        UpdateCategoryOptions();
        SelectedAssetCategory    = AssetCategoryOptions[0];
        SelectedAssetDepreciation = AssetDepreciationOptions[0];
    }

    public void InitNew(TransactionType type)
    {
        IsNew = true;
        TransactionTypeStr = type switch
        {
            TransactionType.Income        => "Income",
            TransactionType.CapitalInflux => "Capital",
            _                             => "Expense"
        };
        UpdateCategoryOptions();
        SelectedCategory = CategoryOptions.Count > 0 ? CategoryOptions[0] : null;
        OnPropertyChanged(nameof(FormTitle));
    }

    public void InitEdit(TransactionDto dto)
    {
        IsNew               = false;
        TransactionId       = dto.TransactionId;
        TransactionTypeStr  = dto.TypeDisplay == "Capital" ? "Capital" : dto.TypeDisplay;
        Date                = dto.Date;
        AmountText          = dto.Amount.ToString("F2");
        Description         = dto.Description;
        PayeePayer          = dto.PayeePayer ?? string.Empty;
        PaymentMethod       = dto.PaymentMethod ?? string.Empty;
        Notes               = dto.Notes ?? string.Empty;
        AttachmentPath      = dto.AttachmentPath;
        TaxRateText         = (dto.TaxRate * 100).ToString("F2");
        _pendingLinkedAnimalId = dto.LinkedAnimalId;
        UpdateCategoryOptions();
        SelectedCategory = CategoryOptions.FirstOrDefault(c => c.Key == dto.Category)
                           ?? CategoryOptions.FirstOrDefault();
        RecalcTax();
        OnPropertyChanged(nameof(FormTitle));
    }

    public async Task LoadAsync()
    {
        var list = await _animals.GetAllAsync();
        AnimalOptions = new ObservableCollection<AnimalDto>(list.OrderBy(a => a.BarnName));
        if (_pendingLinkedAnimalId.HasValue)
            LinkedAnimal = AnimalOptions.FirstOrDefault(a => a.AnimalId == _pendingLinkedAnimalId.Value);

        // For edit: load linked asset if one exists for this transaction
        if (!IsNew && TransactionId > 0)
        {
            var linked = await _assets.GetByTransactionIdAsync(TransactionId);
            if (linked is not null)
            {
                _linkedAssetId         = linked.AssetId;
                IsAssetPurchase        = true;
                AssetName              = linked.AssetName;
                SelectedAssetCategory  = AssetCategoryOptions.FirstOrDefault(
                    c => c.Key == linked.Category.ToString()) ?? AssetCategoryOptions[0];
                SelectedAssetDepreciation = AssetDepreciationOptions.FirstOrDefault(
                    d => d.Key == linked.DepreciationMethod.ToString()) ?? AssetDepreciationOptions[0];
                AssetUsefulLifeText    = linked.UsefulLifeYears.ToString();
                AssetSalvageValueText  = linked.SalvageValue.ToString("F2");
            }
        }
    }

    partial void OnTransactionTypeStrChanged(string value)
    {
        UpdateCategoryOptions();
        SelectedCategory = CategoryOptions.Count > 0 ? CategoryOptions[0] : null;
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(IsExpenseType));
        OnPropertyChanged(nameof(ShowAssetSection));
        // Clear asset purchase flag when switching away from Expense
        if (CurrentType != TransactionType.Expense)
            IsAssetPurchase = false;
    }

    partial void OnIsAssetPurchaseChanged(bool value)
    {
        // Pre-fill asset name from description when first checking the box
        if (value && string.IsNullOrWhiteSpace(AssetName) && !string.IsNullOrWhiteSpace(Description))
            AssetName = Description;
        // Pre-select asset category based on expense category
        if (value && SelectedCategory is not null)
            SelectedAssetCategory = SuggestAssetCategory(SelectedCategory.Key);
    }

    partial void OnSelectedAssetDepreciationChanged(CategoryOption? value)
    {
        if (value?.Key == "Section179")
        {
            AssetUsefulLifeText  = "1";
            AssetSalvageValueText = "0";
        }
    }

    partial void OnAmountTextChanged(string value)  => RecalcTax();
    partial void OnTaxRateTextChanged(string value) => RecalcTax();

    private void UpdateCategoryOptions()
    {
        CategoryOptions = CurrentType switch
        {
            TransactionType.Income        => IncomeCategories,
            TransactionType.CapitalInflux => CapitalCategories,
            _                             => ExpenseCategories
        };
    }

    private void RecalcTax()
    {
        if (!decimal.TryParse(AmountText, out var amount))       amount = 0m;
        if (!decimal.TryParse(TaxRateText, out var ratePercent)) ratePercent = 0m;
        TaxAmount   = Math.Round(amount * ratePercent / 100, 2);
        NetProceeds = amount - TaxAmount;
    }

    private CategoryOption SuggestAssetCategory(string expenseCategoryKey) =>
        expenseCategoryKey switch
        {
            "LivestockPurchase" =>
                AssetCategoryOptions.First(c => c.Key == "Livestock"),
            "FuelOil" or "RepairsMaintenance" or "SuppliesMiscellaneous" =>
                AssetCategoryOptions.First(c => c.Key == "MachineryEquipment"),
            _ =>
                AssetCategoryOptions.First(c => c.Key == "MachineryEquipment"),
        };

    [RelayCommand]
    private void BrowseAttachment()
    {
        var path = _dialog.OpenAnyFile("Select Receipt or Attachment");
        if (path is not null) AttachmentPath = path;
    }

    [RelayCommand]
    private void ClearAttachment() => AttachmentPath = null;

    partial void OnAttachmentPathChanged(string? value) => OnPropertyChanged(nameof(IsImageAttachment));

    [RelayCommand]
    private async Task SaveAsync()
    {
        var error = Validate();
        if (error is not null) { ValidationError = error; return; }
        ValidationError = null;

        var dto = BuildDto();
        TransactionDto saved;
        if (IsNew)
            saved = await _transactions.AddAsync(dto);
        else
            saved = await _transactions.UpdateAsync(dto);

        await UpdateLinkedAnimalAsync(saved);
        await SyncLinkedAssetAsync(saved);
        if (IsNew) await TryGenerateBillOfSaleAsync(saved);
        _nav.GoBack();
    }

    [RelayCommand]
    private void Cancel() => _nav.GoBack();

    private async Task SyncLinkedAssetAsync(TransactionDto tx)
    {
        if (!IsAssetPurchase || tx.TransactionType != TransactionType.Expense) return;

        decimal.TryParse(AmountText,             out var amount);
        decimal.TryParse(AssetSalvageValueText,  out var salvage);
        int.TryParse(AssetUsefulLifeText,        out var life);

        var assetDto = new AssetDto
        {
            AssetId             = _linkedAssetId ?? 0,
            AssetName           = string.IsNullOrWhiteSpace(AssetName) ? tx.Description : AssetName.Trim(),
            Category            = Enum.Parse<AssetCategory>(SelectedAssetCategory!.Key),
            PurchaseDate        = tx.Date,
            PurchasePrice       = amount,
            DepreciationMethod  = Enum.Parse<DepreciationMethod>(SelectedAssetDepreciation!.Key),
            UsefulLifeYears     = life > 0 ? life : 7,
            SalvageValue        = salvage,
            LinkedAnimalId      = tx.LinkedAnimalId,
            LinkedTransactionId = tx.TransactionId,
            Notes               = tx.Notes,
        };

        if (_linkedAssetId.HasValue)
            await _assets.UpdateAsync(assetDto);
        else
        {
            var added = await _assets.AddAsync(assetDto);
            _linkedAssetId = added.AssetId;
        }
    }

    private async Task TryGenerateBillOfSaleAsync(TransactionDto saved)
    {
        if (saved.TransactionType != TransactionType.Income) return;
        if (saved.Category != "LivestockSales") return;
        if (saved.LinkedAnimalId is null) return;

        try
        {
            var animal   = await _animals.GetByIdAsync(saved.LinkedAnimalId.Value);
            var farm     = await _farms.GetDefaultAsync();
            var farmName = farm?.FarmName ?? "Farm";

            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CattleManager", "BillsOfSale");
            Directory.CreateDirectory(folder);

            var safeName = string.Concat(
                (animal?.BarnName ?? "Animal").Split(Path.GetInvalidFileNameChars()));
            var pdfPath = Path.Combine(folder,
                $"BillOfSale_{safeName}_{saved.Date:yyyyMMdd}_{saved.TransactionId}.pdf");

            await Task.Run(() => _export.ExportBillOfSaleToPdf(saved, animal, farmName, pdfPath));

            saved.AttachmentPath = pdfPath;
            await _transactions.UpdateAsync(saved);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Bill of sale PDF generation failed for transaction {Id} — non-fatal", saved.TransactionId);
        }
    }

    private string? Validate()
    {
        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0)
            return "Amount must be greater than zero.";
        if (string.IsNullOrWhiteSpace(Description))
            return "Description is required.";
        if (SelectedCategory is null)
            return "Category is required.";
        if (IsAssetPurchase && IsExpenseType)
        {
            if (SelectedAssetCategory is null)
                return "Asset category is required.";
            if (SelectedAssetDepreciation is null)
                return "Depreciation method is required.";
        }
        return null;
    }

    private TransactionDto BuildDto()
    {
        decimal.TryParse(AmountText, out var amount);
        decimal.TryParse(TaxRateText, out var ratePercent);
        return new TransactionDto
        {
            TransactionId   = TransactionId,
            TransactionType = CurrentType,
            Category        = SelectedCategory!.Key,
            Date            = Date,
            Amount          = amount,
            Description     = Description.Trim(),
            PayeePayer      = string.IsNullOrWhiteSpace(PayeePayer)    ? null : PayeePayer.Trim(),
            PaymentMethod   = string.IsNullOrWhiteSpace(PaymentMethod) ? null : PaymentMethod.Trim(),
            Notes           = string.IsNullOrWhiteSpace(Notes)         ? null : Notes.Trim(),
            AttachmentPath  = AttachmentPath,
            LinkedAnimalId  = LinkedAnimal?.AnimalId,
            TaxRate         = ratePercent / 100,
            TaxAmount       = TaxAmount,
        };
    }

    private async Task UpdateLinkedAnimalAsync(TransactionDto dto)
    {
        if (dto.LinkedAnimalId is null) return;
        var animal = await _animals.GetByIdAsync(dto.LinkedAnimalId.Value);
        if (animal is null) return;

        if (dto.TransactionType == TransactionType.Expense && animal.PurchasePrice is null)
        {
            animal.PurchasePrice = dto.Amount;
            await _animals.UpdateAsync(animal);
        }
        else if (dto.TransactionType == TransactionType.Income && dto.Category == "LivestockSales")
        {
            animal.SalePrice  = dto.Amount;
            animal.BuyerName  = dto.PayeePayer;
            animal.SoldDate   = dto.Date;
            animal.Status     = AnimalStatus.Sold;
            await _animals.UpdateAsync(animal);
            await DisposeLinkedAssetAsync(animal);
        }
    }

    private static async Task DisposeLinkedAssetAsync(AnimalDto animal)
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var assets = scope.ServiceProvider.GetRequiredService<IAssetRepository>();
            var existing = (await assets.GetByAnimalAsync(animal.AnimalId)).FirstOrDefault();
            if (existing is null || existing.DisposedDate is not null) return;
            existing.DisposedDate  = animal.SoldDate ?? DateTime.Today;
            existing.DisposalPrice = animal.SalePrice;
            await assets.UpdateAsync(existing);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Asset disposal failed for animal {AnimalId} — non-fatal", animal.AnimalId);
        }
    }
}
