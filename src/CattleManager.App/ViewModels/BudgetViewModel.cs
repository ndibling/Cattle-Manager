using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Globalization;

namespace CattleManager.App.ViewModels;

public partial class BudgetMonthViewModel : ObservableObject
{
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public int BudgetEntryId { get; set; }

    [ObservableProperty] private decimal _budgetAmount;
    public decimal ActualAmount { get; init; }

    public decimal Variance    => BudgetAmount - ActualAmount;
    public bool IsOverBudget   => ActualAmount > BudgetAmount && BudgetAmount > 0;
    public string ActualDisplay   => ActualAmount.ToString("C");
    public string VarianceDisplay => Variance.ToString("C");

    partial void OnBudgetAmountChanged(decimal _)
    {
        OnPropertyChanged(nameof(Variance));
        OnPropertyChanged(nameof(VarianceDisplay));
        OnPropertyChanged(nameof(IsOverBudget));
    }
}

public partial class BudgetCategoryViewModel : ObservableObject
{
    public string Category        { get; init; } = string.Empty;
    public string CategoryDisplay { get; init; } = string.Empty;
    public string TypeDisplay     { get; init; } = string.Empty;
    public ObservableCollection<BudgetMonthViewModel> Months { get; } = [];

    public decimal AnnualBudget   => Months.Sum(m => m.BudgetAmount);
    public decimal AnnualActual   => Months.Sum(m => m.ActualAmount);
    public decimal AnnualVariance => AnnualBudget - AnnualActual;
    public bool    IsOverBudget   => AnnualActual > AnnualBudget && AnnualBudget > 0;
    public string  AnnualBudgetDisplay   => AnnualBudget.ToString("C");
    public string  AnnualActualDisplay   => AnnualActual.ToString("C");
    public string  AnnualVarianceDisplay => AnnualVariance.ToString("C");

    public void RefreshTotals()
    {
        OnPropertyChanged(nameof(AnnualBudget));
        OnPropertyChanged(nameof(AnnualActual));
        OnPropertyChanged(nameof(AnnualVariance));
        OnPropertyChanged(nameof(IsOverBudget));
        OnPropertyChanged(nameof(AnnualBudgetDisplay));
        OnPropertyChanged(nameof(AnnualActualDisplay));
        OnPropertyChanged(nameof(AnnualVarianceDisplay));
    }
}

public partial class BudgetViewModel : ObservableObject
{
    private readonly IBudgetRepository _budgets;
    private readonly ITransactionRepository _transactions;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    [ObservableProperty] private int _fiscalYear = DateTime.Today.Year;
    [ObservableProperty] private ObservableCollection<BudgetCategoryViewModel> _categories = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private BudgetCategoryOption? _selectedCategoryToAdd;

    public bool HasNoCategories => Categories.Count == 0;

    partial void OnCategoriesChanged(ObservableCollection<BudgetCategoryViewModel> _)
        => OnPropertyChanged(nameof(HasNoCategories));

    public IReadOnlyList<int> FiscalYearOptions { get; } =
        Enumerable.Range(DateTime.Today.Year - 4, 6).Reverse().ToList();

    private static readonly IReadOnlyList<(string Key, string Display, string Type)> AllCategories =
    [
        ("LivestockSales",        "Livestock Sales",            "Income"),
        ("BreedingServices",      "Breeding Services",          "Income"),
        ("HayCropSales",          "Hay / Crop Sales",           "Income"),
        ("CustomWork",            "Custom Work Income",         "Income"),
        ("GovernmentPayments",    "Government Payments",        "Income"),
        ("InsuranceProceeds",     "Insurance Proceeds",         "Income"),
        ("MiscellaneousIncome",   "Miscellaneous Income",       "Income"),
        ("LivestockPurchase",     "Livestock Purchase",         "Expense"),
        ("FeedHay",               "Feed & Hay",                 "Expense"),
        ("VeterinaryMedical",     "Veterinary & Medical",       "Expense"),
        ("BreedingFees",          "Breeding Fees / AI",         "Expense"),
        ("FuelOil",               "Fuel & Oil",                 "Expense"),
        ("RepairsMaintenance",    "Repairs & Maintenance",      "Expense"),
        ("Utilities",             "Utilities",                  "Expense"),
        ("LaborContractWork",     "Labor / Contract Work",      "Expense"),
        ("TruckingTransportation","Trucking / Transportation",  "Expense"),
        ("Insurance",             "Insurance",                  "Expense"),
        ("PropertyTaxes",         "Property Taxes",             "Expense"),
        ("MarketingAuction",      "Marketing / Auction Fees",   "Expense"),
        ("SuppliesMiscellaneous", "Supplies & Miscellaneous",   "Expense"),
        ("Other",                 "Other",                      "Expense"),
    ];

    private ObservableCollection<BudgetCategoryOption> _availableToAdd = [];
    public ObservableCollection<BudgetCategoryOption> AvailableToAdd
    {
        get => _availableToAdd;
        private set => SetProperty(ref _availableToAdd, value);
    }

    public BudgetViewModel(IBudgetRepository budgets, ITransactionRepository transactions,
        NavigationService nav, DialogService dialog)
    {
        _budgets      = budgets;
        _transactions = transactions;
        _nav          = nav;
        _dialog       = dialog;
    }

    public async Task LoadAsync() => await LoadForYearAsync(FiscalYear);

    partial void OnFiscalYearChanged(int value) => _ = LoadForYearAsync(value);

    private async Task LoadForYearAsync(int year)
    {
        IsLoading = true;
        try
        {
            var from = new DateTime(year, 1, 1);
            var to   = new DateTime(year, 12, 31);

            var saved   = await _budgets.GetByFiscalYearAsync(year);
            var actuals = await _transactions.GetByDateRangeAsync(from, to);

            // actuals grouped by (category, month)
            var actualByMonth = actuals
                .GroupBy(t => (t.Category, t.Date.Month))
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            // Which categories have saved entries this year?
            var savedCategories = saved
                .Select(e => e.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var catVms = new ObservableCollection<BudgetCategoryViewModel>();
            foreach (var cat in AllCategories.Where(c => savedCategories.Contains(c.Key)))
            {
                var catVm = BuildCategoryVm(cat, saved, actualByMonth);
                catVms.Add(catVm);
            }
            Categories = catVms;
            RefreshAvailableToAdd();
        }
        finally { IsLoading = false; }
    }

    private BudgetCategoryViewModel BuildCategoryVm(
        (string Key, string Display, string Type) cat,
        IReadOnlyList<BudgetEntryDto> saved,
        Dictionary<(string, int), decimal> actualByMonth)
    {
        var catVm = new BudgetCategoryViewModel
        {
            Category        = cat.Key,
            CategoryDisplay = cat.Display,
            TypeDisplay     = cat.Type,
        };
        for (int m = 1; m <= 12; m++)
        {
            var entry  = saved.FirstOrDefault(e => e.Category == cat.Key && e.Month == m);
            var actual = actualByMonth.GetValueOrDefault((cat.Key, m), 0m);
            var monthVm = new BudgetMonthViewModel
            {
                Month        = m,
                MonthName    = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m),
                BudgetEntryId = entry?.BudgetEntryId ?? 0,
                BudgetAmount = entry?.BudgetAmount ?? 0m,
                ActualAmount = actual,
            };
            monthVm.PropertyChanged += (_, _) => catVm.RefreshTotals();
            catVm.Months.Add(monthVm);
        }
        return catVm;
    }

    private void RefreshAvailableToAdd()
    {
        var active = Categories.Select(c => c.Category).ToHashSet(StringComparer.OrdinalIgnoreCase);
        AvailableToAdd = new ObservableCollection<BudgetCategoryOption>(
            AllCategories
                .Where(c => !active.Contains(c.Key))
                .Select(c => new BudgetCategoryOption(c.Key, c.Display, c.Type)));
        SelectedCategoryToAdd = null;
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        if (SelectedCategoryToAdd is null) return;
        var cat = AllCategories.First(c => c.Key == SelectedCategoryToAdd.Key);

        // Load existing actuals for this year so the new category shows real numbers
        var from    = new DateTime(FiscalYear, 1, 1);
        var to      = new DateTime(FiscalYear, 12, 31);
        var actuals = await _transactions.GetByDateRangeAsync(from, to);
        var actualByMonth = actuals
            .GroupBy(t => (t.Category, t.Date.Month))
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var catVm = BuildCategoryVm(cat, [], actualByMonth);
        Categories.Add(catVm);
        RefreshAvailableToAdd();
    }

    [RelayCommand]
    private async Task RemoveCategoryAsync(BudgetCategoryViewModel cat)
    {
        if (!_dialog.Confirm($"Remove '{cat.CategoryDisplay}' from this budget? Saved amounts will be deleted.", "Remove Category"))
            return;
        await _budgets.DeleteByCategoryAndYearAsync(cat.Category, FiscalYear);
        Categories.Remove(cat);
        RefreshAvailableToAdd();
    }

    [RelayCommand]
    private async Task SaveBudgetAsync()
    {
        IsLoading = true;
        try
        {
            foreach (var cat in Categories)
            {
                var txType = cat.TypeDisplay == "Income"
                    ? TransactionType.Income
                    : TransactionType.Expense;

                foreach (var month in cat.Months)
                {
                    var saved = await _budgets.UpsertAsync(new BudgetEntryDto
                    {
                        BudgetEntryId   = month.BudgetEntryId,
                        FiscalYear      = FiscalYear,
                        Category        = cat.Category,
                        TransactionType = txType,
                        Month           = month.Month,
                        BudgetAmount    = month.BudgetAmount,
                    });
                    month.BudgetEntryId = saved.BudgetEntryId;
                }
            }
            _dialog.ShowInfo("Budget saved successfully.", "Saved");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ClearBudgetAsync()
    {
        if (!_dialog.Confirm($"Clear all budget entries for {FiscalYear}? This cannot be undone.", "Clear Budget"))
            return;
        await _budgets.DeleteByYearAsync(FiscalYear);
        await LoadForYearAsync(FiscalYear);
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<FinancialDashboardViewModel>();
        _nav.NavigateTo(new FinancialDashboardPage(vm));
    }

    [RelayCommand]
    private void NavigateToTransactions()
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<TransactionListViewModel>();
        _nav.NavigateTo(new TransactionListPage(vm));
    }

    [RelayCommand]
    private void NavigateToAssets()
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<AssetListViewModel>();
        _nav.NavigateTo(new AssetListPage(vm));
    }

    [RelayCommand]
    private void NavigateToLoans()
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<LoanListViewModel>();
        _nav.NavigateTo(new LoanListPage(vm));
    }

    [RelayCommand]
    private void NavigateToReports()
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<ReportsViewModel>();
        _nav.NavigateTo(new ReportsPage(vm));
    }
}

public class BudgetCategoryOption
{
    public string Key     { get; }
    public string Display { get; }
    public string Type    { get; }
    public string FullDisplay => $"[{Type}] {Display}";

    public BudgetCategoryOption(string key, string display, string type)
    {
        Key     = key;
        Display = display;
        Type    = type;
    }
}
