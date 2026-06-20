using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace CattleManager.App.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly FinancialService _financial;
    private readonly ExportService _export;
    private readonly IFarmRepository _farms;
    private readonly ITransactionRepository _transRepo;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    // ---- tab selection ----
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowExportPdf))]
    private int _selectedTabIndex;
    [ObservableProperty] private bool _isLoading;

    public bool ShowExportPdf => SelectedTabIndex != 4;

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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBillsOfSale))]
    private IReadOnlyList<BillsOfSaleYearGroup> _billsOfSaleGroups = [];
    public bool HasBillsOfSale => BillsOfSaleGroups.Count > 0;

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

    public ReportsViewModel(FinancialService financial, ExportService export,
        IFarmRepository farms, ITransactionRepository transRepo,
        NavigationService nav, DialogService dialog)
    {
        _financial = financial;
        _export    = export;
        _farms     = farms;
        _transRepo = transRepo;
        _nav       = nav;
        _dialog    = dialog;
    }

    public async Task LoadAsync()
    {
        await LoadBillsOfSaleAsync();
    }

    private async Task LoadBillsOfSaleAsync()
    {
        var allTx = await _transRepo.GetAllAsync();
        var salesWithPdf = allTx
            .Where(t => t.TransactionType == TransactionType.Income
                     && t.Category == "LivestockSales"
                     && !string.IsNullOrEmpty(t.AttachmentPath))
            .OrderByDescending(t => t.Date)
            .ToList();

        BillsOfSaleGroups = salesWithPdf
            .GroupBy(t => t.Date.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => new BillsOfSaleYearGroup
            {
                Year  = g.Key,
                Items = g.Select(t => new BillOfSaleRecord
                {
                    TransactionId = t.TransactionId,
                    AnimalName    = t.LinkedAnimalName ?? t.Description,
                    DateDisplay   = t.Date.ToString("MM/dd/yyyy"),
                    AmountDisplay = t.Amount.ToString("C"),
                    PdfPath       = t.AttachmentPath ?? string.Empty,
                    HasPdf        = !string.IsNullOrEmpty(t.AttachmentPath)
                                 && File.Exists(t.AttachmentPath),
                }).ToList()
            })
            .ToList();
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
        }
    }

    [RelayCommand]
    private void CreateSaleTransaction()
    {
        var vm = App.Services.GetRequiredService<TransactionFormViewModel>();
        vm.InitNew(TransactionType.Income);
        _nav.NavigateTo(new TransactionFormPage(vm));
    }

    [RelayCommand]
    private void DownloadBillOfSale(BillOfSaleRecord? record)
    {
        if (record is null || !record.HasPdf)
        {
            _dialog.ShowInfo("The PDF file is not available.", "File Not Found");
            return;
        }
        var dest = _dialog.SavePdfFile($"BillOfSale_{record.AnimalName}_{DateTime.Today:yyyyMMdd}");
        if (dest is null) return;
        try
        {
            File.Copy(record.PdfPath, dest, overwrite: true);
        }
        catch (Exception ex)
        {
            _dialog.ShowError($"Could not save file: {ex.Message}");
        }
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

public class BillOfSaleRecord
{
    public int TransactionId { get; init; }
    public string AnimalName { get; init; } = string.Empty;
    public string DateDisplay { get; init; } = string.Empty;
    public string AmountDisplay { get; init; } = string.Empty;
    public string PdfPath { get; init; } = string.Empty;
    public bool HasPdf { get; init; }
}

public class BillsOfSaleYearGroup
{
    public int Year { get; init; }
    public IReadOnlyList<BillOfSaleRecord> Items { get; init; } = [];
}
