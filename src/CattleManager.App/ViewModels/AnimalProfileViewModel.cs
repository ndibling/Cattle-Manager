using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace CattleManager.App.ViewModels;

public partial class AnimalProfileViewModel : ObservableObject
{
    private readonly IAnimalRepository _animals;
    private readonly IHealthRecordRepository _healthRecords;
    private readonly IBreedingRecordRepository _breedingRecords;
    private readonly HealthService _healthService;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    public int AnimalId { get; set; }

    [ObservableProperty] private AnimalDto? _animal;
    [ObservableProperty] private ObservableCollection<HealthRecordDto> _healthHistory = [];
    [ObservableProperty] private ObservableCollection<BreedingRecordDto> _breedingHistory = [];
    [ObservableProperty] private ObservableCollection<AnimalDto> _offspring = [];
    [ObservableProperty] private ObservableCollection<string> _upcomingTasks = [];
    [ObservableProperty] private bool _hasNoUpcomingTasks = true;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _editPhotoPath;
    [ObservableProperty] private string _editBarnName = string.Empty;
    [ObservableProperty] private string? _editRegisteredName;
    [ObservableProperty] private string? _editColoring;
    [ObservableProperty] private string? _editLocation;
    [ObservableProperty] private string? _editHealthNotes;
    [ObservableProperty] private DateTime? _editLastWorming;
    [ObservableProperty] private DateTime? _editLastVaccination;
    [ObservableProperty] private DateTime? _editLastHealthCheck;

    public AnimalProfileViewModel(IAnimalRepository animals, IHealthRecordRepository healthRecords,
        IBreedingRecordRepository breedingRecords, HealthService healthService,
        NavigationService nav, DialogService dialog)
    {
        _animals = animals;
        _healthRecords = healthRecords;
        _breedingRecords = breedingRecords;
        _healthService = healthService;
        _nav = nav;
        _dialog = dialog;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Animal = await _animals.GetByIdAsync(AnimalId);
            if (Animal is null) return;

            var health = await _healthRecords.GetByAnimalAsync(AnimalId);
            HealthHistory = new ObservableCollection<HealthRecordDto>(health);

            var breeding = await _breedingRecords.GetByAnimalAsync(AnimalId);
            BreedingHistory = new ObservableCollection<BreedingRecordDto>(breeding);

            var kids = await _animals.GetOffspringAsync(AnimalId);
            Offspring = new ObservableCollection<AnimalDto>(kids);

            var tasks = _healthService.GetUpcomingTasks(Animal);
            UpcomingTasks = new ObservableCollection<string>(tasks);
            HasNoUpcomingTasks = tasks.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void EnterEditMode()
    {
        if (Animal is null) return;
        EditBarnName = Animal.BarnName;
        EditRegisteredName = Animal.RegisteredName;
        EditColoring = Animal.Coloring;
        EditLocation = Animal.CurrentLocation;
        EditHealthNotes = Animal.HealthNotes;
        EditLastWorming = Animal.LastWormingDate;
        EditLastVaccination = Animal.LastVaccinationDate;
        EditLastHealthCheck = Animal.LastHealthCheckDate;
        EditPhotoPath = Animal.PhotoPath;
        IsEditMode = true;
    }

    [RelayCommand]
    private async Task SaveChangesAsync()
    {
        if (Animal is null) return;
        Animal.BarnName = EditBarnName;
        Animal.RegisteredName = EditRegisteredName;
        Animal.Coloring = EditColoring;
        Animal.CurrentLocation = EditLocation;
        Animal.HealthNotes = EditHealthNotes;
        Animal.LastWormingDate = EditLastWorming;
        Animal.LastVaccinationDate = EditLastVaccination;
        Animal.LastHealthCheckDate = EditLastHealthCheck;
        Animal.PhotoPath = EditPhotoPath;
        await _animals.UpdateAsync(Animal);
        IsEditMode = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CancelEdit() => IsEditMode = false;

    [RelayCommand]
    private void ChangePhoto()
    {
        var path = _dialog.OpenImageFile();
        if (path is not null) EditPhotoPath = path;
    }

    [RelayCommand]
    private void ViewLineage()
    {
        var vm = App.Services.GetRequiredService<PedigreeViewModel>();
        vm.AnimalId = AnimalId;
        _nav.NavigateTo(new PedigreePage(vm));
    }

    [RelayCommand]
    private void EditProfile()
    {
        var vm = App.Services.GetRequiredService<AnimalFormViewModel>();
        vm.AnimalId = AnimalId;
        if (Animal is not null) vm.HerdId = Animal.HerdId;
        _nav.NavigateTo(new AnimalFormPage(vm));
    }

    [RelayCommand]
    private void ViewOffspring(AnimalDto? offspring)
    {
        if (offspring is null) return;
        var vm = App.Services.GetRequiredService<AnimalProfileViewModel>();
        vm.AnimalId = offspring.AnimalId;
        _nav.NavigateTo(new AnimalProfilePage(vm));
    }

    [RelayCommand]
    private void GoBack() => _nav.GoBack();
}
