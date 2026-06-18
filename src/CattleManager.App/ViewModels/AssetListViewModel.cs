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

public sealed class AssetDisplayItem
{
    public int AssetId { get; init; }
    public string AssetName { get; init; } = string.Empty;
    public string CategoryDisplay { get; init; } = string.Empty;
    public string PurchaseDateDisplay { get; init; } = string.Empty;
    public string CostBasisDisplay { get; init; } = string.Empty;
    public string DepreciationMethodDisplay { get; init; } = string.Empty;
    public string AnnualDepreciationDisplay { get; init; } = string.Empty;
    public string BookValueDisplay { get; init; } = string.Empty;
    public string? LinkedAnimalName { get; init; }
    public bool IsActive { get; init; }

    public decimal PurchasePrice { get; init; }
    public decimal BookValue { get; init; }

    public AssetDto Source { get; init; } = null!;
}

public partial class AssetListViewModel : ObservableObject
{
    private readonly IAssetRepository _assets;
    private readonly FinancialService _financialService;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    private ObservableCollection<AssetDisplayItem> _allItems = [];
    private readonly CollectionViewSource _viewSource = new();

    [ObservableProperty] private string _filterCategory = "All";
    [ObservableProperty] private string _filterStatus = "Active";
    [ObservableProperty] private ICollectionView? _assetsView;
    [ObservableProperty] private AssetDisplayItem? _selectedAsset;
    [ObservableProperty] private int _visibleCount;
    [ObservableProperty] private decimal _totalCostBasis;
    [ObservableProperty] private decimal _totalBookValue;
    [ObservableProperty] private bool _isLoading;

    public IReadOnlyList<string> CategoryOptions { get; } =
    [
        "All", "Livestock", "Machinery & Equipment", "Land", "Building", "Vehicle", "Other"
    ];

    public IReadOnlyList<string> StatusOptions { get; } = ["All", "Active", "Disposed"];

    public AssetListViewModel(IAssetRepository assets, FinancialService financialService,
        NavigationService nav, DialogService dialog)
    {
        _assets          = assets;
        _financialService = financialService;
        _nav             = nav;
        _dialog          = dialog;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _assets.GetAllAsync();
            var items = list.Select(a => BuildDisplayItem(a)).ToList();
            _allItems = new ObservableCollection<AssetDisplayItem>(items);
            _viewSource.Source = _allItems;
            _viewSource.Filter -= ApplyFilters;
            _viewSource.Filter += ApplyFilters;
            AssetsView = _viewSource.View;
            Refresh();
        }
        finally { IsLoading = false; }
    }

    private AssetDisplayItem BuildDisplayItem(AssetDto asset)
    {
        int lastYear = asset.DisposedDate?.Year ?? DateTime.Today.Year;
        int ownershipYears = Math.Max(1, lastYear - asset.PurchaseDate.Year + 1);

        decimal accumulated = 0;
        for (int y = 1; y <= ownershipYears; y++)
            accumulated += _financialService.CalculateAnnualDepreciation(asset, y);

        int currentYear = DateTime.Today.Year - asset.PurchaseDate.Year + 1;
        decimal thisYearDepr = asset.IsActive
            ? _financialService.CalculateAnnualDepreciation(asset, Math.Max(1, currentYear))
            : 0m;

        decimal bookValue = Math.Max(asset.SalvageValue, asset.PurchasePrice - accumulated);

        return new AssetDisplayItem
        {
            AssetId                   = asset.AssetId,
            AssetName                 = asset.AssetName,
            CategoryDisplay           = asset.CategoryDisplay,
            PurchaseDateDisplay       = asset.PurchaseDate.ToString("MM/dd/yyyy"),
            CostBasisDisplay          = asset.PurchasePrice.ToString("C"),
            DepreciationMethodDisplay = asset.DepreciationMethodDisplay,
            AnnualDepreciationDisplay = thisYearDepr.ToString("C"),
            BookValueDisplay          = bookValue.ToString("C"),
            LinkedAnimalName          = asset.LinkedAnimalName,
            IsActive                  = asset.IsActive,
            PurchasePrice             = asset.PurchasePrice,
            BookValue                 = bookValue,
            Source                    = asset
        };
    }

    private void ApplyFilters(object sender, FilterEventArgs e)
    {
        if (e.Item is not AssetDisplayItem item) { e.Accepted = false; return; }

        if (FilterCategory != "All" && item.CategoryDisplay != FilterCategory)
        { e.Accepted = false; return; }

        if (FilterStatus == "Active" && !item.IsActive)   { e.Accepted = false; return; }
        if (FilterStatus == "Disposed" && item.IsActive)  { e.Accepted = false; return; }

        e.Accepted = true;
    }

    partial void OnFilterCategoryChanged(string value) => Refresh();
    partial void OnFilterStatusChanged(string value) => Refresh();

    private void Refresh()
    {
        _viewSource.View?.Refresh();
        var visible = _viewSource.View?.Cast<AssetDisplayItem>().ToList() ?? [];
        VisibleCount    = visible.Count;
        TotalCostBasis  = visible.Sum(i => i.PurchasePrice);
        TotalBookValue  = visible.Sum(i => i.BookValue);
    }

    [RelayCommand]
    private void AddAsset()
    {
        var vm = App.Services.GetRequiredService<AssetFormViewModel>();
        vm.InitNew();
        _nav.NavigateTo(new AssetFormPage(vm));
    }

    [RelayCommand]
    private void EditAsset(AssetDisplayItem? item)
    {
        if (item is null) return;
        var vm = App.Services.GetRequiredService<AssetFormViewModel>();
        vm.InitEdit(item.Source);
        _nav.NavigateTo(new AssetFormPage(vm));
    }

    [RelayCommand]
    private async Task DeleteAssetAsync(AssetDisplayItem? item)
    {
        if (item is null) return;
        if (!_dialog.Confirm($"Delete asset \"{item.AssetName}\"? This cannot be undone.", "Delete Asset"))
            return;
        await _assets.DeleteAsync(item.AssetId);
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
    private void ClearFilters()
    {
        FilterCategory = "All";
        FilterStatus   = "Active";
    }
}
