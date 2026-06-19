using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace CattleManager.App.ViewModels;

public sealed class LoanDisplayItem
{
    public int     LoanId                   { get; init; }
    public string  LenderName               { get; init; } = string.Empty;
    public string  LoanTypeDisplay          { get; init; } = string.Empty;
    public string  OriginalPrincipalDisplay { get; init; } = string.Empty;
    public string  CurrentBalanceDisplay    { get; init; } = string.Empty;
    public string  InterestRateDisplay      { get; init; } = string.Empty;
    public string  PaymentAmountDisplay     { get; init; } = string.Empty;
    public string  NextPaymentDateDisplay   { get; init; } = string.Empty;
    public bool    IsActive                 { get; init; }
    public decimal CurrentBalance           { get; init; }
    public LoanDto Source                   { get; init; } = null!;
}

public partial class LoanListViewModel : ObservableObject
{
    private readonly ILoanRepository    _loans;
    private readonly FinancialService   _financialService;
    private readonly NavigationService  _nav;
    private readonly DialogService      _dialog;

    private ObservableCollection<LoanDisplayItem> _allItems  = [];
    private readonly CollectionViewSource         _viewSource = new();

    [ObservableProperty] private string              _filterStatus    = "Active";
    [ObservableProperty] private ICollectionView?    _loansView;
    [ObservableProperty] private LoanDisplayItem?    _selectedLoan;
    [ObservableProperty] private int                 _visibleCount;
    [ObservableProperty] private decimal             _totalOutstanding;
    [ObservableProperty] private bool                _isLoading;

    public IReadOnlyList<string> StatusOptions { get; } = ["All", "Active", "Paid Off"];

    public LoanListViewModel(ILoanRepository loans, FinancialService financialService,
        NavigationService nav, DialogService dialog)
    {
        _loans            = loans;
        _financialService = financialService;
        _nav              = nav;
        _dialog           = dialog;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list  = await _loans.GetAllAsync();
            _allItems = new ObservableCollection<LoanDisplayItem>(list.Select(BuildDisplayItem));
            _viewSource.Source   = _allItems;
            _viewSource.Filter  -= OnFilter;
            _viewSource.Filter  += OnFilter;
            LoansView            = _viewSource.View;
            Refresh();
        }
        finally { IsLoading = false; }
    }

    private LoanDisplayItem BuildDisplayItem(LoanDto loan)
    {
        var balance = _financialService.CalculateLoanBalance(loan, DateTime.Today);
        return new LoanDisplayItem
        {
            LoanId                   = loan.LoanId,
            LenderName               = loan.LenderName,
            LoanTypeDisplay          = loan.LoanTypeDisplay,
            OriginalPrincipalDisplay = loan.OriginalPrincipal.ToString("C"),
            CurrentBalanceDisplay    = balance.ToString("C"),
            InterestRateDisplay      = loan.InterestRateDisplay,
            PaymentAmountDisplay     = loan.PaymentAmount.ToString("C"),
            NextPaymentDateDisplay   = loan.IsActive ? NextPaymentDate(loan).ToString("MM/dd/yyyy") : "—",
            IsActive                 = loan.IsActive,
            CurrentBalance           = balance,
            Source                   = loan
        };
    }

    private static DateTime NextPaymentDate(LoanDto loan)
    {
        int months = loan.PaymentFrequency switch
        {
            PaymentFrequency.Quarterly  => 3,
            PaymentFrequency.SemiAnnual => 6,
            PaymentFrequency.Annual     => 12,
            _                           => 1
        };
        var d = loan.StartDate.AddMonths(months);
        while (d <= DateTime.Today) d = d.AddMonths(months);
        return d;
    }

    private void OnFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not LoanDisplayItem item) { e.Accepted = false; return; }
        e.Accepted = FilterStatus switch
        {
            "Active"   => item.IsActive,
            "Paid Off" => !item.IsActive,
            _          => true
        };
    }

    partial void OnFilterStatusChanged(string value) => Refresh();

    private void Refresh()
    {
        _viewSource.View?.Refresh();
        var visible       = _viewSource.View?.Cast<LoanDisplayItem>().ToList() ?? [];
        VisibleCount      = visible.Count;
        TotalOutstanding  = visible.Where(i => i.IsActive).Sum(i => i.CurrentBalance);
    }

    [RelayCommand]
    private void AddLoan()
    {
        var vm = App.Services.GetRequiredService<LoanFormViewModel>();
        vm.InitNew();
        _nav.NavigateTo(new LoanFormPage(vm));
    }

    [RelayCommand]
    private void ViewDetails(LoanDisplayItem? item)
    {
        if (item is null) return;
        var vm = App.Services.GetRequiredService<LoanDetailViewModel>();
        vm.Init(item.Source);
        _nav.NavigateTo(new LoanDetailPage(vm));
    }

    [RelayCommand]
    private void EditLoan(LoanDisplayItem? item)
    {
        if (item is null) return;
        var vm = App.Services.GetRequiredService<LoanFormViewModel>();
        vm.InitEdit(item.Source);
        _nav.NavigateTo(new LoanFormPage(vm));
    }

    [RelayCommand]
    private async Task DeleteLoanAsync(LoanDisplayItem? item)
    {
        if (item is null) return;
        if (!_dialog.Confirm(
            $"Delete loan from \"{item.LenderName}\"? All recorded payments will also be removed.",
            "Delete Loan"))
            return;
        await _loans.DeleteAsync(item.LoanId);
        _allItems.Remove(item);
        Refresh();
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
    private void NavigateToReports()
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<ReportsViewModel>();
        _nav.NavigateTo(new ReportsPage(vm));
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<FinancialDashboardViewModel>();
        _nav.NavigateTo(new FinancialDashboardPage(vm));
    }

    [RelayCommand]
    private void NavigateToBudget()
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<BudgetViewModel>();
        _nav.NavigateTo(new BudgetPage(vm));
    }
}
