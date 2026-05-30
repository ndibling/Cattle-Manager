using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CattleManager.App.ViewModels;

public partial class AnimalFormViewModel : ObservableObject
{
    private readonly IAnimalRepository _animals;
    private readonly IBreedRepository _breeds;
    private readonly BreedingService _breedingService;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    public int AnimalId { get; set; }
    public int HerdId { get; set; }
    public bool IsNewAnimal => AnimalId == 0;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _barnName = string.Empty;
    [ObservableProperty] private string? _registeredName;
    [ObservableProperty] private string? _registrationNumber;
    [ObservableProperty] private string? _registrationOrganization;
    [ObservableProperty] private BreedDto? _selectedBreed;
    [ObservableProperty] private ObservableCollection<BreedDto> _breeds2 = [];
    [ObservableProperty] private Gender _gender;
    [ObservableProperty] private AnimalStatus _status;
    [ObservableProperty] private DateTime _birthDate = DateTime.Today;
    [ObservableProperty] private DateTime? _dateAcquired;
    [ObservableProperty] private string? _coloring;
    [ObservableProperty] private decimal? _weight;
    [ObservableProperty] private WeightUnit _weightUnit;
    [ObservableProperty] private decimal? _height;
    [ObservableProperty] private HeightUnit _heightUnit;
    [ObservableProperty] private string? _currentLocation;
    [ObservableProperty] private string? _breedersName;
    [ObservableProperty] private string? _currentOwner;
    [ObservableProperty] private string? _photoPath;
    [ObservableProperty] private AnimalDto? _selectedSire;
    [ObservableProperty] private AnimalDto? _selectedDam;
    [ObservableProperty] private string? _externalSireName;
    [ObservableProperty] private string? _externalDamName;
    [ObservableProperty] private bool _sireInHerd = true;
    [ObservableProperty] private bool _damInHerd = true;
    [ObservableProperty] private DateTime? _lastWormingDate;
    [ObservableProperty] private DateTime? _lastVaccinationDate;
    [ObservableProperty] private DateTime? _lastHealthCheckDate;
    [ObservableProperty] private string? _healthNotes;
    [ObservableProperty] private bool _isBreeding;
    [ObservableProperty] private bool _isPregnant;
    [ObservableProperty] private AnimalDto? _pregnancySire;
    [ObservableProperty] private DateTime? _breedingDate;
    [ObservableProperty] private DateTime? _expectedDueDate;
    [ObservableProperty] private string? _reproductionNotes;
    [ObservableProperty] private MaleBreedingStatus _maleBreedingStatus;
    [ObservableProperty] private ObservableCollection<AnimalDto> _availableMales = [];
    [ObservableProperty] private ObservableCollection<AnimalDto> _availableAnimals = [];
    [ObservableProperty] private string? _validationError;
    [ObservableProperty] private string _formTitle = "Add New Animal";

    public IReadOnlyList<AnimalStatus> StatusOptions { get; } = Enum.GetValues<AnimalStatus>();
    public IReadOnlyList<Gender> GenderOptions { get; } = Enum.GetValues<Gender>();
    public IReadOnlyList<WeightUnit> WeightUnitOptions { get; } = Enum.GetValues<WeightUnit>();
    public IReadOnlyList<HeightUnit> HeightUnitOptions { get; } = Enum.GetValues<HeightUnit>();
    public IReadOnlyList<MaleBreedingStatus> MaleBreedingStatusOptions { get; } = Enum.GetValues<MaleBreedingStatus>();

    public AnimalFormViewModel(IAnimalRepository animals, IBreedRepository breeds,
        BreedingService breedingService, NavigationService nav, DialogService dialog)
    {
        _animals = animals;
        _breeds = breeds;
        _breedingService = breedingService;
        _nav = nav;
        _dialog = dialog;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var allBreeds = await _breeds.GetAllAsync();
            Breeds2 = new ObservableCollection<BreedDto>(allBreeds);

            var herdAnimals = HerdId > 0
                ? await _animals.GetByHerdAsync(HerdId)
                : await _animals.GetAllAsync();
            AvailableAnimals = new ObservableCollection<AnimalDto>(herdAnimals);
            AvailableMales = new ObservableCollection<AnimalDto>(herdAnimals.Where(a => a.Gender == Gender.Male));

            if (!IsNewAnimal)
            {
                FormTitle = "Edit Animal";
                var animal = await _animals.GetByIdAsync(AnimalId);
                if (animal is not null) PopulateFromDto(animal);
            }
            else
            {
                FormTitle = "Add New Animal";
                SelectedBreed = Breeds2.FirstOrDefault();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void PopulateFromDto(AnimalDto a)
    {
        BarnName = a.BarnName; RegisteredName = a.RegisteredName;
        RegistrationNumber = a.RegistrationNumber; RegistrationOrganization = a.RegistrationOrganization;
        SelectedBreed = Breeds2.FirstOrDefault(b => b.BreedId == a.BreedId);
        Gender = a.Gender; Status = a.Status; BirthDate = a.BirthDate; DateAcquired = a.DateAcquired;
        Coloring = a.Coloring; Weight = a.Weight; WeightUnit = a.WeightUnit;
        Height = a.Height; HeightUnit = a.HeightUnit;
        CurrentLocation = a.CurrentLocation; BreedersName = a.BreedersName; CurrentOwner = a.CurrentOwner;
        PhotoPath = a.PhotoPath;
        SireInHerd = a.SireId.HasValue;
        if (a.SireId.HasValue) SelectedSire = AvailableAnimals.FirstOrDefault(x => x.AnimalId == a.SireId);
        ExternalSireName = a.ExternalSireName;
        DamInHerd = a.DamId.HasValue;
        if (a.DamId.HasValue) SelectedDam = AvailableAnimals.FirstOrDefault(x => x.AnimalId == a.DamId);
        ExternalDamName = a.ExternalDamName;
        LastWormingDate = a.LastWormingDate; LastVaccinationDate = a.LastVaccinationDate;
        LastHealthCheckDate = a.LastHealthCheckDate; HealthNotes = a.HealthNotes;
        IsBreeding = a.IsBreeding; IsPregnant = a.IsPregnant;
        if (a.PregnancySireId.HasValue)
            PregnancySire = AvailableMales.FirstOrDefault(x => x.AnimalId == a.PregnancySireId);
        BreedingDate = a.BreedingDate; ExpectedDueDate = a.ExpectedDueDate;
        ReproductionNotes = a.ReproductionNotes;
        if (a.MaleBreedingStatus.HasValue) MaleBreedingStatus = a.MaleBreedingStatus.Value;
    }

    partial void OnBreedingDateChanged(DateTime? value)
    {
        if (value.HasValue && IsPregnant)
            ExpectedDueDate = _breedingService.CalculateDueDate(value.Value);
    }

    [RelayCommand]
    private void PickPhoto()
    {
        var path = _dialog.OpenImageFile();
        if (path is not null) PhotoPath = path;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationError = Validate();
        if (ValidationError is not null) return;

        IsLoading = true;
        try
        {
            var dto = BuildDto();
            if (IsNewAnimal)
                await _animals.AddAsync(dto);
            else
                await _animals.UpdateAsync(dto);

            _nav.GoBack();
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(BarnName)) return "Barn Name is required.";
        if (SelectedBreed is null) return "Breed is required.";
        if (BirthDate > DateTime.Today) return "Birth Date cannot be in the future.";
        if (Weight.HasValue && Weight <= 0) return "Weight must be a positive number.";
        if (Height.HasValue && Height <= 0) return "Height must be a positive number.";
        if (IsPregnant && BreedingDate.HasValue && ExpectedDueDate.HasValue && ExpectedDueDate <= BreedingDate)
            return "Expected Due Date must be after Breeding Date.";
        return null;
    }

    private AnimalDto BuildDto() => new()
    {
        AnimalId = AnimalId, HerdId = HerdId,
        BarnName = BarnName.Trim(), RegisteredName = RegisteredName?.Trim(),
        RegistrationNumber = RegistrationNumber?.Trim(), RegistrationOrganization = RegistrationOrganization?.Trim(),
        BreedId = SelectedBreed!.BreedId, BreedName = SelectedBreed.BreedName,
        Gender = Gender, Status = Status, BirthDate = BirthDate, DateAcquired = DateAcquired,
        Coloring = Coloring, Weight = Weight, WeightUnit = WeightUnit,
        Height = Height, HeightUnit = HeightUnit,
        CurrentLocation = CurrentLocation, BreedersName = BreedersName, CurrentOwner = CurrentOwner,
        PhotoPath = PhotoPath,
        SireId = SireInHerd ? SelectedSire?.AnimalId : null,
        ExternalSireName = SireInHerd ? null : ExternalSireName,
        DamId = DamInHerd ? SelectedDam?.AnimalId : null,
        ExternalDamName = DamInHerd ? null : ExternalDamName,
        LastWormingDate = LastWormingDate, LastVaccinationDate = LastVaccinationDate,
        LastHealthCheckDate = LastHealthCheckDate, HealthNotes = HealthNotes,
        IsBreeding = IsBreeding, IsPregnant = IsPregnant,
        PregnancySireId = PregnancySire?.AnimalId,
        BreedingDate = BreedingDate, ExpectedDueDate = ExpectedDueDate,
        ReproductionNotes = ReproductionNotes,
        MaleBreedingStatus = Gender == Gender.Male ? MaleBreedingStatus : null,
        CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
    };

    [RelayCommand]
    private void Cancel() => _nav.GoBack();
}
