using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace CattleManager.App.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly FinancialService _financial;
    private readonly ExportService _export;
    private readonly ITransactionRepository _transactions;
    private readonly IAnimalRepository _animals;
    private readonly IFarmRepository _farms;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    // ---- tab selection ----
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private bool _isLoading;

    // ---- P&L ----
    [ObservableProperty] private DateTime _pnlFrom = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _pnlTo   = DateTime.Today;
    [ObservableProperty] private ProfitAndLossDto? _pnlResult;
    public bool HasPnlResult => PnlResult is not null;

    // ---- Balance Sheet ----
    [ObservableProperty] private DateTime _bsAsOf = DateTime.Today;
    [ObservableProperty] private BalanceSheetDto? _bsResult;
    public bool HasBsResult => BsResult is not null;

    // ---- Cash Flow ----
    [ObservableProperty] private DateTime _cfFrom = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _cfTo   = DateTime.Today;
    [ObservableProperty] private CashFlowDto? _cfResult;
    public bool HasCfResult => CfResult is not null;

    // ---- Tax Summary ----
    [ObservableProperty] private int _taxYear = DateTime.Today.Year;
    [ObservableProperty] private TaxSummaryDto? _taxResult;
    public bool HasTaxResult    => TaxResult is not null;
    public bool HasScheduleF    => TaxResult?.ScheduleFItems.Count > 0;
    public bool HasCapitalGains => TaxResult?.CapitalGainEvents.Count > 0;
    public IReadOnlyList<int> TaxYearOptions { get; } =
        Enumerable.Range(DateTime.Today.Year - 5, 7).Reverse().ToList();

    // ---- Bill of Sale ----
    [ObservableProperty] private IReadOnlyList<TransactionDto> _saleTransactions = [];
    [ObservableProperty] private TransactionDto? _selectedSale;
    [ObservableProperty] private AnimalDto? _saleLinkedAnimal;
    public bool HasSaleTransactions => SaleTransactions.Count > 0;

    // ---- changed-propagation partial methods ----
    partial void OnPnlResultChanged(ProfitAndLossDto? _)    => OnPropertyChanged(nameof(HasPnlResult));
    partial void OnBsResultChanged(BalanceSheetDto? _)      => OnPropertyChanged(nameof(HasBsResult));
    partial void OnCfResultChanged(CashFlowDto? _)          => OnPropertyChanged(nameof(HasCfResult));
    partial void OnTaxResultChanged(TaxSummaryDto? _)
    {
        OnPropertyChanged(nameof(HasTaxResult));
        OnPropertyChanged(nameof(HasScheduleF));
        OnPropertyChanged(nameof(HasCapitalGains));
    }
    partial void OnSaleTransactionsChanged(IReadOnlyList<TransactionDto> _) =>
        OnPropertyChanged(nameof(HasSaleTransactions));

    partial void OnSelectedSaleChanged(TransactionDto? tx)
    {
        SaleLinkedAnimal = null;
        if (tx?.LinkedAnimalId is int id)
            _ = LoadSaleAnimalAsync(id);
    }

    public ReportsViewModel(FinancialService financial, ExportService export,
        ITransactionRepository transactions, IAnimalRepository animals,
        IFarmRepository farms, NavigationService nav, DialogService dialog)
    {
        _financial    = financial;
        _export       = export;
        _transactions = transactions;
        _animals      = animals;
        _farms        = farms;
        _nav          = nav;
        _dialog       = dialog;
    }

    public async Task LoadAsync()
    {
        var sales = await _transactions.GetByTypeAsync(TransactionType.Income);
        SaleTransactions = sales
            .Where(t => t.Category == "LivestockSales")
            .OrderByDescending(t => t.Date)
            .ToList();
    }

    private async Task LoadSaleAnimalAsync(int animalId)
    {
        SaleLinkedAnimal = await _animals.GetByIdAsync(animalId);
    }

    [RelayCommand]
    private async Task RunReportAsync()
    {
        IsLoading = true;
        try
        {
            switch (SelectedTabIndex)
            {
                case 0: PnlResult = await _financial.GetProfitAndLossAsync(PnlFrom, PnlTo); break;
                case 1: BsResult  = await _financial.GetBalanceSheetAsync(BsAsOf); break;
                case 2: CfResult  = await _financial.GetCashFlowStatementAsync(CfFrom, CfTo); break;
                case 3: TaxResult = await _financial.GetTaxSummaryAsync(TaxYear); break;
            }
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        switch (SelectedTabIndex)
        {
            case 0:
            {
                if (PnlResult is null) { _dialog.ShowInfo("Run the report first.", "No Report Data"); return; }
                var path = _dialog.SavePdfFile($"ProfitAndLoss_{PnlFrom:yyyy-MM-dd}_to_{PnlTo:yyyy-MM-dd}");
                if (path is null) return;
                await Task.Run(() => _export.ExportProfitAndLossToPdf(PnlResult, PnlFrom, PnlTo, path));
                break;
            }
            case 1:
            {
                if (BsResult is null) { _dialog.ShowInfo("Run the report first.", "No Report Data"); return; }
                var path = _dialog.SavePdfFile($"BalanceSheet_{BsAsOf:yyyy-MM-dd}");
                if (path is null) return;
                await Task.Run(() => _export.ExportBalanceSheetToPdf(BsResult, path));
                break;
            }
            case 2:
            {
                if (CfResult is null) { _dialog.ShowInfo("Run the report first.", "No Report Data"); return; }
                var path = _dialog.SavePdfFile($"CashFlow_{CfFrom:yyyy-MM-dd}_to_{CfTo:yyyy-MM-dd}");
                if (path is null) return;
                await Task.Run(() => _export.ExportCashFlowToPdf(CfResult, CfFrom, CfTo, path));
                break;
            }
            case 3:
            {
                if (TaxResult is null) { _dialog.ShowInfo("Run the report first.", "No Report Data"); return; }
                var path = _dialog.SavePdfFile($"TaxSummary_{TaxYear}");
                if (path is null) return;
                await Task.Run(() => _export.ExportTaxSummaryToPdf(TaxResult, path));
                break;
            }
            case 4:
                await GenerateBillOfSaleAsync();
                break;
        }
    }

    [RelayCommand]
    private async Task GenerateBillOfSaleAsync()
    {
        if (SelectedSale is null) { _dialog.ShowInfo("Select a livestock sale transaction first.", "No Selection"); return; }
        var farm     = await _farms.GetDefaultAsync();
        var farmName = farm?.FarmName ?? "Farm";
        var path     = _dialog.SavePdfFile($"BillOfSale_{SelectedSale.Date:yyyy-MM-dd}");
        if (path is null) return;
        var animal = SelectedSale.LinkedAnimalId.HasValue
            ? await _animals.GetByIdAsync(SelectedSale.LinkedAnimalId.Value)
            : null;
        await Task.Run(() => _export.ExportBillOfSaleToPdf(SelectedSale, animal, farmName, path));
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
