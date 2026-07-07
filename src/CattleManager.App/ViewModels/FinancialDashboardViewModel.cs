using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace CattleManager.App.ViewModels;

public sealed class MonthlyBarViewModel
{
    public string MonthLabel    { get; init; } = string.Empty;
    public double RevenueHeight { get; init; }
    public double ExpenseHeight { get; init; }
    public string RevenueDisplay { get; init; } = string.Empty;
    public string ExpenseDisplay { get; init; } = string.Empty;
}

public partial class FinancialDashboardViewModel : ObservableObject
{
    private readonly FinancialService _financial;
    private readonly NavigationService _nav;

    [ObservableProperty] private FinancialKpiDto? _kpis;
    [ObservableProperty] private IReadOnlyList<MonthlyBarViewModel> _monthlyBars = [];
    [ObservableProperty] private bool _isLoading;

    public string NetFarmIncomeDisplay  => Kpis?.NetFarmIncome.ToString("C0") ?? "—";
    public string TotalRevenueDisplay   => Kpis?.TotalRevenue.ToString("C0")  ?? "—";
    public string TotalExpensesDisplay  => Kpis?.TotalExpenses.ToString("C0") ?? "—";
    public string DebtToAssetDisplay    => Kpis is null ? "—" : $"{Kpis.DebtToAssetRatio:P1}";
    public string AvgCostPerHeadDisplay => Kpis?.AvgCostPerHead.ToString("C0") ?? "—";
    public string BreakEvenPriceDisplay => Kpis?.BreakEvenPrice.ToString("C0") ?? "—";
    public string? FiscalYearDisplay    => Kpis is null ? null : $"FY {Kpis.FiscalYear}";
    public bool   HasMonthlyBars        => MonthlyBars.Count > 0;

    partial void OnKpisChanged(FinancialKpiDto? _)
    {
        OnPropertyChanged(nameof(NetFarmIncomeDisplay));
        OnPropertyChanged(nameof(TotalRevenueDisplay));
        OnPropertyChanged(nameof(TotalExpensesDisplay));
        OnPropertyChanged(nameof(DebtToAssetDisplay));
        OnPropertyChanged(nameof(AvgCostPerHeadDisplay));
        OnPropertyChanged(nameof(BreakEvenPriceDisplay));
        OnPropertyChanged(nameof(FiscalYearDisplay));
        BuildChartBars();
    }

    partial void OnMonthlyBarsChanged(IReadOnlyList<MonthlyBarViewModel> _) =>
        OnPropertyChanged(nameof(HasMonthlyBars));

    public FinancialDashboardViewModel(FinancialService financial, NavigationService nav)
    {
        _financial = financial;
        _nav       = nav;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try { Kpis = await _financial.GetKpisAsync(); }
        finally { IsLoading = false; }
    }

    private void BuildChartBars()
    {
        var data = Kpis?.MonthlyData;
        if (data is null || data.Count == 0) { MonthlyBars = []; return; }

        decimal maxVal = data.Max(m => Math.Max(m.Revenue, m.Expenses));
        const double maxH = 110.0;

        MonthlyBars = data.Select(m => new MonthlyBarViewModel
        {
            MonthLabel     = m.MonthLabel,
            RevenueHeight  = maxVal > 0 ? (double)(m.Revenue  / maxVal) * maxH : 0,
            ExpenseHeight  = maxVal > 0 ? (double)(m.Expenses / maxVal) * maxH : 0,
            RevenueDisplay = m.Revenue.ToString("C0"),
            ExpenseDisplay = m.Expenses.ToString("C0"),
        }).ToList();
    }

    [RelayCommand]
    private void AddTransaction()
    {
        var win = new TransactionPickerWindow { Owner = System.Windows.Application.Current.MainWindow };
        if (win.ShowDialog() != true || win.Result is not { } mode) return;

        var vm = App.Services.GetRequiredService<TransactionFormViewModel>();
        vm.InitNew(mode);
        System.Windows.Controls.Page page = mode switch
        {
            TransactionMode.SellAnimal           => new SellAnimalFormPage(vm),
            TransactionMode.SellEquipment        => new SellEquipmentFormPage(vm),
            TransactionMode.FarmServicesProducts => new FarmServicesProductsFormPage(vm),
            TransactionMode.OtherIncome          => new OtherIncomeFormPage(vm),
            TransactionMode.OperatingExpense     => new OperatingExpenseFormPage(vm),
            TransactionMode.BuyCapitalAsset      => new BuyCapitalAssetFormPage(vm),
            TransactionMode.BuyLivestock         => new BuyLivestockFormPage(vm),
            TransactionMode.CapitalInflux        => new CapitalInfluxFormPage(vm),
            _                                    => new OperatingExpenseFormPage(vm),
        };
        _nav.NavigateTo(page);
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

    [RelayCommand]
    private void NavigateToBudget()
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<BudgetViewModel>();
        _nav.NavigateTo(new BudgetPage(vm));
    }
}
