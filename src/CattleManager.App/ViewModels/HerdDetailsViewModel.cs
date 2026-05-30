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
    [ObservableProperty] private IReadOnlyList<string> _genderOptions = ["All", "Male", "Female"];
    [ObservableProperty] private IReadOnlyList<string> _breedOptions = ["All"];
    [ObservableProperty] private IReadOnlyList<string> _statusOptions =
        ["All", "Healthy", "Breeding Female", "Breeding Male", "Pregnant", "Weaned", "For Sale", "Inactive"];

    public HerdDetailsViewModel(IAnimalRepository animals, IHerdRepository herds,
        HealthService healthService, NavigationService nav, DialogService dialog)
    {
        _animals = animals;
        _herds = herds;
        _healthService = healthService;
        _nav = nav;
        _dialog = dialog;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
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

        if (FilterStatus != "All")
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
        var vm = App.Services.GetRequiredService<AnimalFormViewModel>();
        vm.HerdId = HerdId;
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
