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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoHerds))]
    private IReadOnlyList<HerdDashboardViewModel> _herdCards = [];
    [ObservableProperty] private bool _showSampleDataBanner;
    [ObservableProperty] private bool _isLoading;

    public bool HasNoHerds => HerdCards.Count == 0;

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
            var cards = new List<HerdDashboardViewModel>();
            foreach (var herd in allHerds)
            {
                var summary = await _herdService.GetSummaryAsync(herd.HerdId);
                cards.Add(new HerdDashboardViewModel(summary, _nav));
            }
            HerdCards = cards;

            var loaded = await _settings.GetAsync("SampleDataLoaded");
            var cleared = await _settings.GetAsync("SampleDataCleared");
            ShowSampleDataBanner = loaded == "true" && cleared != "true";
        }
        finally
        {
            IsLoading = false;
        }
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
