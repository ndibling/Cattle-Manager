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

public partial class HerdDetailsViewModel : ObservableObject
{
    private readonly IAnimalRepository _animals;
    private readonly IHerdRepository _herds;
    private readonly HealthService _healthService;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;
    private readonly ColumnConfigService _columnConfig;

    private ObservableCollection<AnimalDto> _allAnimals = [];
    private readonly CollectionViewSource _viewSource = new();

    public int HerdId { get; set; }

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _filterGender = "All";
    [ObservableProperty] private string _filterBreed = "All";
    [ObservableProperty] private string _filterStatus = "All";
    [ObservableProperty] private ICollectionView? _animalsView;
    [ObservableProperty] private AnimalDto? _selectedAnimal;
    [ObservableProperty] private int _visibleCount;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _pageTitle = "Herd Details";
    [ObservableProperty] private IReadOnlyList<string> _genderOptions = ["All", "Male", "Female"];
    [ObservableProperty] private IReadOnlyList<string> _breedOptions = ["All"];
    [ObservableProperty] private IReadOnlyList<string> _statusOptions =
        ["All", "Active", "Healthy", "Pregnant", "For Sale", "Sold", "Inactive", "Deceased", "Calf", "Breeding Female", "Breeding Male", "Due for Husbandry"];

    // ---- Column visibility (default ON = currently shown; default OFF = new optional) ----
    [ObservableProperty] private bool _showRegisteredName   = true;
    [ObservableProperty] private bool _showBreed            = true;
    [ObservableProperty] private bool _showGender           = true;
    [ObservableProperty] private bool _showAge              = true;
    [ObservableProperty] private bool _showBirthDate        = true;
    [ObservableProperty] private bool _showWeight           = true;
    [ObservableProperty] private bool _showHeight           = true;
    [ObservableProperty] private bool _showLastWorming      = true;
    [ObservableProperty] private bool _showStatus           = true;
    [ObservableProperty] private bool _showTagNumber        = false;
    [ObservableProperty] private bool _showDateAcquired     = false;
    [ObservableProperty] private bool _showPurchasePrice    = false;
    [ObservableProperty] private bool _showCurrentValue     = false;
    [ObservableProperty] private bool _showAskingPrice      = false;
    [ObservableProperty] private bool _showSalePrice        = false;
    [ObservableProperty] private bool _showSoldDate         = false;
    [ObservableProperty] private bool _showBuyerName        = false;
    [ObservableProperty] private bool _showPastureAddress   = false;
    [ObservableProperty] private bool _showLastVaccination  = false;
    [ObservableProperty] private bool _showLastHealthCheck  = false;
    [ObservableProperty] private bool _showLastHoofTrimming = false;
    [ObservableProperty] private bool _showSireName         = false;
    [ObservableProperty] private bool _showDamName          = false;
    [ObservableProperty] private bool _showExpectedDueDate  = false;

    public HerdDetailsViewModel(IAnimalRepository animals, IHerdRepository herds,
        HealthService healthService, NavigationService nav, DialogService dialog,
        ColumnConfigService columnConfig)
    {
        _animals = animals;
        _herds = herds;
        _healthService = healthService;
        _nav = nav;
        _dialog = dialog;
        _columnConfig = columnConfig;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await LoadColumnConfigAsync();

            if (HerdId > 0)
            {
                var herd = await _herds.GetByIdAsync(HerdId);
                PageTitle = herd?.HerdName ?? "Herd Details";
            }

            var list = HerdId > 0
                ? await _animals.GetByHerdAsync(HerdId)
                : await _animals.GetAllAsync();

            _allAnimals = new ObservableCollection<AnimalDto>(list);

            var breeds = _allAnimals.Select(a => a.BreedName).Distinct().OrderBy(b => b).ToList();
            BreedOptions = new[] { "All" }.Concat(breeds).ToList();

            _viewSource.Source = _allAnimals;
            _viewSource.Filter -= ApplyFilters;
            _viewSource.Filter += ApplyFilters;
            AnimalsView = _viewSource.View;
            RefreshCount();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters(object sender, FilterEventArgs e)
    {
        if (e.Item is not AnimalDto a) { e.Accepted = false; return; }

        if (FilterGender != "All" && a.Gender.ToString() != FilterGender)
        { e.Accepted = false; return; }

        if (FilterBreed != "All" && a.BreedName != FilterBreed)
        { e.Accepted = false; return; }

        if (FilterStatus == "Active")
        {
            if (a.Status == AnimalStatus.Deceased || a.Status == AnimalStatus.Inactive || a.Status == AnimalStatus.Sold)
            { e.Accepted = false; return; }
        }
        else if (FilterStatus == "Due for Husbandry")
        {
            if (a.Gender == Gender.Male || a.Status == AnimalStatus.Calf || a.Status == AnimalStatus.Deceased || !IsOverdue(a)) { e.Accepted = false; return; }
        }
        else if (FilterStatus == "Breeding Female")
        {
            if (!a.IsBreeding || a.Gender != Gender.Female ||
                (a.Status != AnimalStatus.Healthy && a.Status != AnimalStatus.Pregnant))
            { e.Accepted = false; return; }
        }
        else if (FilterStatus == "Breeding Male")
        {
            if (!a.IsBreeding || a.Gender != Gender.Male || a.Status != AnimalStatus.Healthy)
            { e.Accepted = false; return; }
        }
        else if (FilterStatus != "All")
        {
            var statusStr = FilterStatus.Replace(" ", "");
            if (!a.Status.ToString().Equals(statusStr, StringComparison.OrdinalIgnoreCase))
            { e.Accepted = false; return; }
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.ToLower();
            if (!a.BarnName.ToLower().Contains(q) &&
                !(a.RegisteredName?.ToLower().Contains(q) ?? false))
            { e.Accepted = false; return; }
        }

        e.Accepted = true;
    }

    partial void OnSearchTextChanged(string value) => Refresh();
    partial void OnFilterGenderChanged(string value) => Refresh();
    partial void OnFilterBreedChanged(string value) => Refresh();
    partial void OnFilterStatusChanged(string value) => Refresh();

    private void Refresh()
    {
        _viewSource.View?.Refresh();
        RefreshCount();
    }

    private void RefreshCount()
    {
        VisibleCount = _viewSource.View?.Cast<object>().Count() ?? 0;
    }

    public bool IsOverdue(AnimalDto a) => _healthService.IsOverdueForHusbandry(a);

    private async Task LoadColumnConfigAsync()
    {
        var c = await _columnConfig.LoadAsync();
        ShowRegisteredName   = c.ShowRegisteredName;
        ShowBreed            = c.ShowBreed;
        ShowGender           = c.ShowGender;
        ShowAge              = c.ShowAge;
        ShowBirthDate        = c.ShowBirthDate;
        ShowWeight           = c.ShowWeight;
        ShowHeight           = c.ShowHeight;
        ShowLastWorming      = c.ShowLastWorming;
        ShowStatus           = c.ShowStatus;
        ShowTagNumber        = c.ShowTagNumber;
        ShowDateAcquired     = c.ShowDateAcquired;
        ShowPurchasePrice    = c.ShowPurchasePrice;
        ShowCurrentValue     = c.ShowCurrentValue;
        ShowAskingPrice      = c.ShowAskingPrice;
        ShowSalePrice        = c.ShowSalePrice;
        ShowSoldDate         = c.ShowSoldDate;
        ShowBuyerName        = c.ShowBuyerName;
        ShowPastureAddress   = c.ShowPastureAddress;
        ShowLastVaccination  = c.ShowLastVaccination;
        ShowLastHealthCheck  = c.ShowLastHealthCheck;
        ShowLastHoofTrimming = c.ShowLastHoofTrimming;
        ShowSireName         = c.ShowSireName;
        ShowDamName          = c.ShowDamName;
        ShowExpectedDueDate  = c.ShowExpectedDueDate;
    }

    public async Task SaveColumnConfigAsync()
    {
        await _columnConfig.SaveAsync(new ColumnConfig
        {
            ShowRegisteredName   = ShowRegisteredName,
            ShowBreed            = ShowBreed,
            ShowGender           = ShowGender,
            ShowAge              = ShowAge,
            ShowBirthDate        = ShowBirthDate,
            ShowWeight           = ShowWeight,
            ShowHeight           = ShowHeight,
            ShowLastWorming      = ShowLastWorming,
            ShowStatus           = ShowStatus,
            ShowTagNumber        = ShowTagNumber,
            ShowDateAcquired     = ShowDateAcquired,
            ShowPurchasePrice    = ShowPurchasePrice,
            ShowCurrentValue     = ShowCurrentValue,
            ShowAskingPrice      = ShowAskingPrice,
            ShowSalePrice        = ShowSalePrice,
            ShowSoldDate         = ShowSoldDate,
            ShowBuyerName        = ShowBuyerName,
            ShowPastureAddress   = ShowPastureAddress,
            ShowLastVaccination  = ShowLastVaccination,
            ShowLastHealthCheck  = ShowLastHealthCheck,
            ShowLastHoofTrimming = ShowLastHoofTrimming,
            ShowSireName         = ShowSireName,
            ShowDamName          = ShowDamName,
            ShowExpectedDueDate  = ShowExpectedDueDate,
        });
    }

    [RelayCommand]
    private void ViewProfile(AnimalDto? animal)
    {
        if (animal is null) return;
        var vm = App.Services.GetRequiredService<AnimalProfileViewModel>();
        vm.AnimalId = animal.AnimalId;
        _nav.NavigateTo(new AnimalProfilePage(vm));
    }

    [RelayCommand]
    private void EditAnimal(AnimalDto? animal)
    {
        if (animal is null) return;
        var vm = App.Services.GetRequiredService<AnimalFormViewModel>();
        vm.AnimalId = animal.AnimalId;
        vm.HerdId = animal.HerdId;
        _nav.NavigateTo(new AnimalFormPage(vm));
    }

    [RelayCommand]
    private void ViewLineage(AnimalDto? animal)
    {
        if (animal is null) return;
        var vm = App.Services.GetRequiredService<PedigreeViewModel>();
        vm.AnimalId = animal.AnimalId;
        _nav.NavigateTo(new PedigreePage(vm));
    }

    [RelayCommand]
    private async Task DeleteAnimalAsync(AnimalDto? animal)
    {
        if (animal is null) return;
        var offspring = await _animals.GetOffspringAsync(animal.AnimalId);
        var msg = $"Are you sure you want to delete {animal.BarnName}? This action cannot be undone.";
        if (offspring.Count > 0)
            msg += $"\n\nWarning: This animal has {offspring.Count} offspring in the herd. Deleting may affect pedigree records.";

        if (!_dialog.Confirm(msg, "Delete Animal")) return;
        await _animals.DeleteAsync(animal.AnimalId);
        _allAnimals.Remove(animal);
        Refresh();
    }

    [RelayCommand]
    private void AddNewAnimal()
    {
        var intake = _dialog.ShowAnimalIntake();
        if (intake is null) return;

        var vm = App.Services.GetRequiredService<AnimalFormViewModel>();
        vm.HerdId = HerdId;
        vm.ApplyIntake(intake);
        _nav.NavigateTo(new AnimalFormPage(vm));
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        FilterGender = "All";
        FilterBreed = "All";
        FilterStatus = "All";
    }
}
