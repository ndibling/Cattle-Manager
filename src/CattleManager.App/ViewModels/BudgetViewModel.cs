using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace CattleManager.App.ViewModels;

public partial class BudgetLineViewModel : ObservableObject
{
    [ObservableProperty] private string _budgetAmountText = "0";

    public string  Category        { get; init; } = string.Empty;
    public string  CategoryDisplay { get; init; } = string.Empty;
    public string  TypeDisplay     { get; init; } = string.Empty;
    public decimal ActualYtd       { get; init; }
    public int     BudgetEntryId   { get; set; }

    public decimal BudgetAmount  => decimal.TryParse(BudgetAmountText, out var v) ? v : 0m;
    public decimal Variance      => BudgetAmount - ActualYtd;
    public string  VarianceDisplay => Variance.ToString("C");
    public string  ActualDisplay   => ActualYtd.ToString("C");
    public double  PercentUsed   => BudgetAmount == 0 ? 0 : (double)(ActualYtd / BudgetAmount * 100);
    public string  PercentDisplay  => $"{PercentUsed:F0}%";
    public bool    IsOverBudget  => ActualYtd > BudgetAmount && BudgetAmount > 0;

    partial void OnBudgetAmountTextChanged(string _)
    {
        OnPropertyChanged(nameof(BudgetAmount));
        OnPropertyChanged(nameof(Variance));
        OnPropertyChanged(nameof(VarianceDisplay));
        OnPropertyChanged(nameof(PercentUsed));
        OnPropertyChanged(nameof(PercentDisplay));
        OnPropertyChanged(nameof(IsOverBudget));
    }
}

public partial class BudgetViewModel : ObservableObject
{
    private readonly IBudgetRepository _budgets;
    private readonly ITransactionRepository _transactions;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    [ObservableProperty] private int _fiscalYear = DateTime.Today.Year;
    [ObservableProperty] private ObservableCollection<BudgetLineViewModel> _budgetLines = [];
    [ObservableProperty] private bool _isLoading;

    public IReadOnlyList<int> FiscalYearOptions { get; } =
        Enumerable.Range(DateTime.Today.Year - 4, 6).Reverse().ToList();

    private static readonly IReadOnlyList<(string Key, string Display, string Type)> AllCategories =
    [
        ("LivestockSales",       "Livestock Sales",           "Income"),
        ("BreedingServices",     "Breeding Services",         "Income"),
        ("HayCropSales",         "Hay / Crop Sales",          "Income"),
        ("CustomWork",           "Custom Work Income",        "Income"),
        ("GovernmentPayments",   "Government Payments",       "Income"),
        ("InsuranceProceeds",    "Insurance Proceeds",        "Income"),
        ("MiscellaneousIncome",  "Miscellaneous Income",      "Income"),
        ("FeedHay",              "Feed & Hay",                "Expense"),
        ("VeterinaryMedical",    "Veterinary & Medical",      "Expense"),
        ("BreedingFees",         "Breeding Fees / AI",        "Expense"),
        ("FuelOil",              "Fuel & Oil",                "Expense"),
        ("RepairsMaintenance",   "Repairs & Maintenance",     "Expense"),
        ("Utilities",            "Utilities",                 "Expense"),
        ("LaborContractWork",    "Labor / Contract Work",     "Expense"),
        ("TruckingTransportation","Trucking / Transportation","Expense"),
        ("Insurance",            "Insurance",                 "Expense"),
        ("PropertyTaxes",        "Property Taxes",            "Expense"),
        ("MarketingAuction",     "Marketing / Auction Fees",  "Expense"),
        ("SuppliesMiscellaneous","Supplies & Miscellaneous",  "Expense"),
        ("Other",                "Other",                     "Expense"),
    ];

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

            var actualByCategory = actuals
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            BudgetLines = new ObservableCollection<BudgetLineViewModel>(
                AllCategories.Select(cat =>
                {
                    var entry  = saved.FirstOrDefault(e => e.Category == cat.Key);
                    var actual = actualByCategory.GetValueOrDefault(cat.Key, 0m);
                    return new BudgetLineViewModel
                    {
                        Category         = cat.Key,
                        CategoryDisplay  = cat.Display,
                        TypeDisplay      = cat.Type,
                        ActualYtd        = actual,
                        BudgetEntryId    = entry?.BudgetEntryId ?? 0,
                        BudgetAmountText = (entry?.BudgetAmount ?? 0m).ToString("G"),
                    };
                }));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveBudgetAsync()
    {
        IsLoading = true;
        try
        {
            foreach (var line in BudgetLines)
            {
                if (line.BudgetAmount == 0 && line.BudgetEntryId == 0) continue;

                var txType = line.TypeDisplay == "Income"
                    ? TransactionType.Income
                    : TransactionType.Expense;

                var saved = await _budgets.UpsertAsync(new BudgetEntryDto
                {
                    BudgetEntryId   = line.BudgetEntryId,
                    FiscalYear      = FiscalYear,
                    Category        = line.Category,
                    TransactionType = txType,
                    Month           = 0,
                    BudgetAmount    = line.BudgetAmount,
                });
                line.BudgetEntryId = saved.BudgetEntryId;
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
