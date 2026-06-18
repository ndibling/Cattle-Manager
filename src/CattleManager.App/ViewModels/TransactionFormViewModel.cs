using CattleManager.App.Services;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CattleManager.App.ViewModels;

public record CategoryOption(string Key, string Display);

public partial class TransactionFormViewModel : ObservableObject
{
    private readonly ITransactionRepository _transactions;
    private readonly IAnimalRepository _animals;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    private int? _pendingLinkedAnimalId;

    private static readonly System.Collections.Generic.HashSet<string> _imageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff" };

    public bool IsImageAttachment =>
        AttachmentPath is not null &&
        _imageExtensions.Contains(System.IO.Path.GetExtension(AttachmentPath));

    private static readonly IReadOnlyList<CategoryOption> ExpenseCategories =
    [
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

    public IReadOnlyList<string> TypeOptions { get; } = ["Income", "Expense", "Capital"];

    public string FormTitle => IsNew
        ? $"Add {TransactionTypeStr} Transaction"
        : $"Edit {TransactionTypeStr} Transaction";

    private TransactionType CurrentType => TransactionTypeStr switch
    {
        "Income"  => TransactionType.Income,
        "Capital" => TransactionType.CapitalInflux,
        _         => TransactionType.Expense
    };

    public TransactionFormViewModel(ITransactionRepository transactions,
        IAnimalRepository animals, NavigationService nav, DialogService dialog)
    {
        _transactions = transactions;
        _animals      = animals;
        _nav          = nav;
        _dialog       = dialog;
        UpdateCategoryOptions();
    }

    public void InitNew(TransactionType type)
    {
        IsNew = true;
        TransactionTypeStr = type switch
        {
            TransactionType.Income       => "Income",
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
    }

    partial void OnTransactionTypeStrChanged(string value)
    {
        UpdateCategoryOptions();
        SelectedCategory = CategoryOptions.Count > 0 ? CategoryOptions[0] : null;
        OnPropertyChanged(nameof(FormTitle));
    }

    partial void OnAmountTextChanged(string value)  => RecalcTax();
    partial void OnTaxRateTextChanged(string value) => RecalcTax();

    private void UpdateCategoryOptions()
    {
        CategoryOptions = CurrentType switch
        {
            TransactionType.Income       => IncomeCategories,
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
        if (IsNew)
            await _transactions.AddAsync(dto);
        else
            await _transactions.UpdateAsync(dto);

        await UpdateLinkedAnimalAsync(dto);
        _nav.GoBack();
    }

    [RelayCommand]
    private void Cancel() => _nav.GoBack();

    private string? Validate()
    {
        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0)
            return "Amount must be greater than zero.";
        if (string.IsNullOrWhiteSpace(Description))
            return "Description is required.";
        if (SelectedCategory is null)
            return "Category is required.";
        return null;
    }

    private TransactionDto BuildDto()
    {
        decimal.TryParse(AmountText, out var amount);
        decimal.TryParse(TaxRateText, out var ratePercent);
        return new TransactionDto
        {
            TransactionId  = TransactionId,
            TransactionType = CurrentType,
            Category       = SelectedCategory!.Key,
            Date           = Date,
            Amount         = amount,
            Description    = Description.Trim(),
            PayeePayer     = string.IsNullOrWhiteSpace(PayeePayer)     ? null : PayeePayer.Trim(),
            PaymentMethod  = string.IsNullOrWhiteSpace(PaymentMethod)  ? null : PaymentMethod.Trim(),
            Notes          = string.IsNullOrWhiteSpace(Notes)          ? null : Notes.Trim(),
            AttachmentPath = AttachmentPath,
            LinkedAnimalId = LinkedAnimal?.AnimalId,
            TaxRate        = ratePercent / 100,
            TaxAmount      = TaxAmount,
        };
    }

    private async Task UpdateLinkedAnimalAsync(TransactionDto dto)
    {
        if (dto.LinkedAnimalId is null) return;
        var animal = await _animals.GetByIdAsync(dto.LinkedAnimalId.Value);
        if (animal is null) return;

        if (dto.TransactionType == TransactionType.Expense)
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
        }
    }
}
