using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace CattleManager.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly HerdService _herdService;
    private readonly IHerdRepository _herds;
    private readonly IFarmRepository _farms;
    private readonly IAppSettingsRepository _settings;
    private readonly SampleDataSeeder _seeder;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    [ObservableProperty] private string _farmName = "My Farm";
    [ObservableProperty] private HerdDto? _selectedHerd;
    [ObservableProperty] private IReadOnlyList<HerdDto> _herds2 = [];
    [ObservableProperty] private int _totalAnimals;
    [ObservableProperty] private int _breedingFemales;
    [ObservableProperty] private int _breedingMales;
    [ObservableProperty] private int _dueForHusbandry;
    [ObservableProperty] private int _pregnantAnimals;
    [ObservableProperty] private bool _showSampleDataBanner;
    [ObservableProperty] private bool _isLoading;

    public DashboardViewModel(HerdService herdService, IHerdRepository herds,
        IFarmRepository farms, IAppSettingsRepository settings,
        SampleDataSeeder seeder, NavigationService nav, DialogService dialog)
    {
        _herdService = herdService;
        _herds = herds;
        _farms = farms;
        _settings = settings;
        _seeder = seeder;
        _nav = nav;
        _dialog = dialog;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var farm = await _farms.GetDefaultAsync();
            FarmName = farm?.FarmName ?? "My Farm";

            var allHerds = await _herds.GetAllAsync();
            Herds2 = allHerds;

            if (SelectedHerd is null || !allHerds.Any(h => h.HerdId == SelectedHerd.HerdId))
                SelectedHerd = allHerds.FirstOrDefault();

            await RefreshStatsAsync();

            var loaded = await _settings.GetAsync("SampleDataLoaded");
            var cleared = await _settings.GetAsync("SampleDataCleared");
            ShowSampleDataBanner = loaded == "true" && cleared != "true";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedHerdChanged(HerdDto? value)
    {
        _ = RefreshStatsAsync();
    }

    private async Task RefreshStatsAsync()
    {
        if (SelectedHerd is null) return;
        var summary = await _herdService.GetSummaryAsync(SelectedHerd.HerdId);
        TotalAnimals = summary.TotalAnimals;
        BreedingFemales = summary.BreedingFemales;
        BreedingMales = summary.BreedingMales;
        DueForHusbandry = summary.DueForHusbandry;
        PregnantAnimals = summary.PregnantAnimals;
    }

    private void NavigateToHerd(string filterStatus = "All")
    {
        if (SelectedHerd is null) return;
        var vm = App.Services.GetRequiredService<HerdDetailsViewModel>();
        vm.HerdId = SelectedHerd.HerdId;
        vm.FilterStatus = filterStatus;
        _nav.NavigateTo(new HerdDetailsPage(vm));
    }

    [RelayCommand] private void ViewHerdDetails()      => NavigateToHerd();
    [RelayCommand] private void ViewBreedingFemales()  => NavigateToHerd("Breeding Female");
    [RelayCommand] private void ViewBreedingMales()    => NavigateToHerd("Breeding Male");
    [RelayCommand] private void ViewPregnant()         => NavigateToHerd("Pregnant");
    [RelayCommand] private void ViewDueForHusbandry()  => NavigateToHerd("Due for Husbandry");

    [RelayCommand]
    private void AddNewAnimal()
    {
        var vm = App.Services.GetRequiredService<AnimalFormViewModel>();
        if (SelectedHerd is not null) vm.HerdId = SelectedHerd.HerdId;
        _nav.NavigateTo(new AnimalFormPage(vm));
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = App.Services.GetRequiredService<SettingsViewModel>();
        _nav.NavigateTo(new SettingsPage(vm));
    }

    [RelayCommand]
    private async Task ClearSampleDataAsync()
    {
        var confirmed = _dialog.Confirm(
            "Remove all sample animals, herds, and health records?\n\nYour real data will not be affected.",
            "Clear Sample Data");
        if (!confirmed) return;

        IsLoading = true;
        try
        {
            await _seeder.ClearSampleDataAsync();
            ShowSampleDataBanner = false;
            await LoadAsync();
            _dialog.ShowInfo("Sample data has been removed.", "Sample Data Cleared");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
