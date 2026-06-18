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

public partial class TransactionListViewModel : ObservableObject
{
    private readonly ITransactionRepository _transactions;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    private ObservableCollection<TransactionDto> _allTransactions = [];
    private readonly CollectionViewSource _viewSource = new();

    [ObservableProperty] private string _filterType = "All";
    [ObservableProperty] private string _filterCategory = "All";
    [ObservableProperty] private DateTime? _filterDateFrom;
    [ObservableProperty] private DateTime? _filterDateTo;
    [ObservableProperty] private ICollectionView? _transactionsView;
    [ObservableProperty] private TransactionDto? _selectedTransaction;
    [ObservableProperty] private int _visibleCount;
    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpenses;
    [ObservableProperty] private decimal _netAmount;
    [ObservableProperty] private bool _isLoading;

    public IReadOnlyList<string> TypeOptions { get; } = ["All", "Income", "Expense", "Capital"];

    public TransactionListViewModel(ITransactionRepository transactions,
        NavigationService nav, DialogService dialog)
    {
        _transactions = transactions;
        _nav = nav;
        _dialog = dialog;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _transactions.GetAllAsync();
            _allTransactions = new ObservableCollection<TransactionDto>(list);
            _viewSource.Source = _allTransactions;
            _viewSource.Filter -= ApplyFilters;
            _viewSource.Filter += ApplyFilters;
            TransactionsView = _viewSource.View;
            Refresh();
        }
        finally { IsLoading = false; }
    }

    private void ApplyFilters(object sender, FilterEventArgs e)
    {
        if (e.Item is not TransactionDto t) { e.Accepted = false; return; }

        if (FilterType != "All")
        {
            bool typeMatch = FilterType switch
            {
                "Income"  => t.TransactionType == TransactionType.Income,
                "Expense" => t.TransactionType == TransactionType.Expense,
                "Capital" => t.TransactionType == TransactionType.CapitalInflux,
                _         => true
            };
            if (!typeMatch) { e.Accepted = false; return; }
        }

        if (FilterCategory != "All" && t.Category != FilterCategory)
        { e.Accepted = false; return; }

        if (FilterDateFrom.HasValue && t.Date < FilterDateFrom.Value)
        { e.Accepted = false; return; }

        if (FilterDateTo.HasValue && t.Date > FilterDateTo.Value)
        { e.Accepted = false; return; }

        e.Accepted = true;
    }

    partial void OnFilterTypeChanged(string value) => Refresh();
    partial void OnFilterCategoryChanged(string value) => Refresh();
    partial void OnFilterDateFromChanged(DateTime? value) => Refresh();
    partial void OnFilterDateToChanged(DateTime? value) => Refresh();

    private void Refresh()
    {
        _viewSource.View?.Refresh();
        var visible = _viewSource.View?.Cast<TransactionDto>().ToList() ?? [];
        VisibleCount = visible.Count;
        TotalIncome   = visible.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount);
        TotalExpenses = visible.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount);
        NetAmount = TotalIncome - TotalExpenses;
    }

    [RelayCommand]
    private void AddTransaction(string? typeStr)
    {
        var type = typeStr switch
        {
            "Expense" => TransactionType.Expense,
            "Capital" => TransactionType.CapitalInflux,
            _         => TransactionType.Income
        };
        var vm = App.Services.GetRequiredService<TransactionFormViewModel>();
        vm.InitNew(type);
        _nav.NavigateTo(new TransactionFormPage(vm));
    }

    [RelayCommand]
    private void EditTransaction(TransactionDto? tx)
    {
        if (tx is null) return;
        var vm = App.Services.GetRequiredService<TransactionFormViewModel>();
        vm.InitEdit(tx);
        _nav.NavigateTo(new TransactionFormPage(vm));
    }

    [RelayCommand]
    private async Task DeleteAsync(TransactionDto? tx)
    {
        if (tx is null) return;
        if (!_dialog.Confirm(
            $"Delete this {tx.TypeDisplay} transaction of {tx.AmountDisplay}?", "Delete Transaction"))
            return;
        await _transactions.DeleteAsync(tx.TransactionId);
        _allTransactions.Remove(tx);
        Refresh();
    }

    [RelayCommand]
    private void NavigateToAssets()
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<AssetListViewModel>();
        _nav.NavigateTo(new AssetListPage(vm));
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterType     = "All";
        FilterCategory = "All";
        FilterDateFrom = null;
        FilterDateTo   = null;
    }
}
