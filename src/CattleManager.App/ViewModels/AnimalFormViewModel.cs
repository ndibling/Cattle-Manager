using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.ObjectModel;
using System.Linq;

namespace CattleManager.App.ViewModels;

public partial class AnimalFormViewModel : ObservableObject
{
    private readonly IAnimalRepository _animals;
    private readonly IBreedRepository _breeds;
    private readonly BreedingService _breedingService;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    private AnimalIntakeResult? _pendingIntake;

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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFemale))]
    private Gender _gender;
    public bool IsFemale => Gender == Gender.Female;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBreedingAllowed))]
    private AnimalStatus _status;
    public bool IsBreedingAllowed => Status != AnimalStatus.Inactive && Status != AnimalStatus.Deceased && Status != AnimalStatus.Sold;
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

    // Acquisition
    [ObservableProperty] private bool _bornOnProperty = true;
    [ObservableProperty] private string? _sellerName;
    [ObservableProperty] private string? _sellerAddress;
    [ObservableProperty] private DateTime? _purchaseDate;
    [ObservableProperty] private decimal? _purchasePrice;

    // Sale
    [ObservableProperty] private decimal? _askingPrice;
    [ObservableProperty] private decimal? _currentValue;
    [ObservableProperty] private decimal? _salePrice;
    [ObservableProperty] private string? _buyerName;
    [ObservableProperty] private string? _buyerAddress;

    // Additional attributes
    [ObservableProperty] private string? _tagNumber;
    [ObservableProperty] private ChondroStatus _chondro;
    [ObservableProperty] private string _hornsSelection = "Unknown";
    [ObservableProperty] private string _isGoodMotherSelection = "Unknown";
    [ObservableProperty] private string? _pastureLocation;
    [ObservableProperty] private string? _pastureState;
    [ObservableProperty] private decimal? _expectedHeightAtMaturity;
    [ObservableProperty] private DateTime? _soldDate;
    [ObservableProperty] private AnimalDto? _selectedSire;
    [ObservableProperty] private AnimalDto? _selectedDam;
    [ObservableProperty] private string? _externalSireName;
    [ObservableProperty] private string? _externalDamName;
    [ObservableProperty] private bool _sireInHerd = true;
    [ObservableProperty] private bool _damInHerd = true;
    [ObservableProperty] private DateTime? _lastWormingDate;
    [ObservableProperty] private DateTime? _lastVaccinationDate;
    [ObservableProperty] private DateTime? _lastHealthCheckDate;
    [ObservableProperty] private DateTime? _lastHoofTrimmingDate;
    [ObservableProperty] private string? _healthNotes;
    [ObservableProperty] private bool _isBreeding;
    [ObservableProperty] private bool _isPregnant;
    [ObservableProperty] private AnimalDto? _pregnancySire;
    [ObservableProperty] private DateTime? _breedingDate;
    [ObservableProperty] private DateTime? _expectedDueDate;
    [ObservableProperty] private string? _reproductionNotes;
    [ObservableProperty] private MaleBreedingStatus _maleBreedingStatus;
    [ObservableProperty] private bool _isForSale;
    [ObservableProperty] private ObservableCollection<AnimalDto> _availableMales = [];
    [ObservableProperty] private ObservableCollection<AnimalDto> _availableFemales = [];
    [ObservableProperty] private ObservableCollection<AnimalDto> _availableAnimals = [];
    [ObservableProperty] private string? _validationError;
    [ObservableProperty] private string _formTitle = "Add New Animal";

    public IReadOnlyList<AnimalStatus> StatusOptions { get; } = Enum.GetValues<AnimalStatus>();
    public IReadOnlyList<Gender> GenderOptions { get; } = Enum.GetValues<Gender>();
    public IReadOnlyList<WeightUnit> WeightUnitOptions { get; } = Enum.GetValues<WeightUnit>();
    public IReadOnlyList<HeightUnit> HeightUnitOptions { get; } = Enum.GetValues<HeightUnit>();
    public IReadOnlyList<MaleBreedingStatus> MaleBreedingStatusOptions { get; } = Enum.GetValues<MaleBreedingStatus>();
    public IReadOnlyList<ChondroStatus> ChondroOptions { get; } = Enum.GetValues<ChondroStatus>();
    public IReadOnlyList<string> YesNoUnknownOptions { get; } = ["Unknown", "Yes", "No"];
    public IReadOnlyList<string> StateOptions { get; } =
    [
        "Alabama", "Alaska", "Arizona", "Arkansas", "California", "Colorado", "Connecticut",
        "Delaware", "Florida", "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa",
        "Kansas", "Kentucky", "Louisiana", "Maine", "Maryland", "Massachusetts", "Michigan",
        "Minnesota", "Mississippi", "Missouri", "Montana", "Nebraska", "Nevada", "New Hampshire",
        "New Jersey", "New Mexico", "New York", "North Carolina", "North Dakota", "Ohio",
        "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island", "South Carolina", "South Dakota",
        "Tennessee", "Texas", "Utah", "Vermont", "Virginia", "Washington", "West Virginia",
        "Wisconsin", "Wyoming"
    ];

    public AnimalFormViewModel(IAnimalRepository animals,
        IBreedRepository breeds, BreedingService breedingService,
        NavigationService nav, DialogService dialog)
    {
        _animals = animals;
        _breeds  = breeds;
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
            AvailableMales   = new ObservableCollection<AnimalDto>(herdAnimals.Where(a => a.Gender == Gender.Male));
            AvailableFemales = new ObservableCollection<AnimalDto>(herdAnimals.Where(a => a.Gender == Gender.Female));

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
        BornOnProperty = a.BornOnProperty; SellerName = a.SellerName; SellerAddress = a.SellerAddress;
        PurchaseDate = a.PurchaseDate; PurchasePrice = a.PurchasePrice;
        IsForSale = a.IsForSale;
        AskingPrice = a.AskingPrice; CurrentValue = a.CurrentValue; SalePrice = a.SalePrice;
        BuyerName = a.BuyerName; BuyerAddress = a.BuyerAddress; SoldDate = a.SoldDate;
        TagNumber = a.TagNumber; Chondro = a.Chondro;
        HornsSelection = a.Horns == true ? "Yes" : a.Horns == false ? "No" : "Unknown";
        IsGoodMotherSelection = a.IsGoodMother == true ? "Yes" : a.IsGoodMother == false ? "No" : "Unknown";
        PastureLocation = a.PastureLocation; PastureState = a.PastureState;
        ExpectedHeightAtMaturity = a.ExpectedHeightAtMaturity;
        SireInHerd = a.SireId.HasValue;
        if (a.SireId.HasValue) SelectedSire = AvailableAnimals.FirstOrDefault(x => x.AnimalId == a.SireId);
        ExternalSireName = a.ExternalSireName;
        DamInHerd = a.DamId.HasValue;
        if (a.DamId.HasValue) SelectedDam = AvailableFemales.FirstOrDefault(x => x.AnimalId == a.DamId);
        ExternalDamName = a.ExternalDamName;
        LastWormingDate = a.LastWormingDate; LastVaccinationDate = a.LastVaccinationDate;
        LastHealthCheckDate = a.LastHealthCheckDate; LastHoofTrimmingDate = a.LastHoofTrimmingDate;
        HealthNotes = a.HealthNotes;
        IsBreeding = a.IsBreeding; IsPregnant = a.IsPregnant;
        if (a.PregnancySireId.HasValue)
            PregnancySire = AvailableMales.FirstOrDefault(x => x.AnimalId == a.PregnancySireId);
        BreedingDate = a.BreedingDate; ExpectedDueDate = a.ExpectedDueDate;
        ReproductionNotes = a.ReproductionNotes;
        if (a.MaleBreedingStatus.HasValue) MaleBreedingStatus = a.MaleBreedingStatus.Value;
    }

    partial void OnStatusChanged(AnimalStatus value)
    {
        if (!IsBreedingAllowed && IsBreeding)
            IsBreeding = false;
        if (value == AnimalStatus.ForSale && !IsForSale)
            IsForSale = true;
        else if (value != AnimalStatus.ForSale && IsForSale)
            IsForSale = false;
    }

    partial void OnIsForSaleChanged(bool value)
    {
        if (value)
        {
            if (Status != AnimalStatus.ForSale) Status = AnimalStatus.ForSale;
        }
        else
        {
            if (Status == AnimalStatus.ForSale) Status = AnimalStatus.Healthy;
            AskingPrice = null;
            SalePrice   = null;
            SoldDate    = null;
            BuyerName   = null;
            BuyerAddress = null;
        }
    }

    partial void OnIsBreedingChanged(bool value)
    {
        if (!value) MaleBreedingStatus = default;
    }

    partial void OnBreedingDateChanged(DateTime? value)
    {
        if (value.HasValue && IsPregnant)
            ExpectedDueDate = _breedingService.CalculateDueDate(value.Value);
    }

    public void ApplyIntake(AnimalIntakeResult intake)
    {
        _pendingIntake = intake;
        BornOnProperty = intake.BornOnFarm;
        if (intake.BornOnFarm)
        {
            BreedersName = intake.BreedersName;
            CurrentOwner = intake.CurrentOwner;
        }
        else
        {
            SellerName    = intake.SellerName;
            SellerAddress = intake.SellerAddress;
            PurchaseDate  = intake.PurchaseDate;
            PurchasePrice = intake.PurchasePrice;
        }
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
            AnimalDto saved;
            if (IsNewAnimal)
                saved = await _animals.AddAsync(dto);
            else
                saved = await _animals.UpdateAsync(dto);

            await SyncAnimalAssetAsync(saved);
            if (_pendingIntake is { BornOnFarm: false })
                await CreatePurchaseExpenseAsync(saved, _pendingIntake);
            _pendingIntake = null;
            _nav.GoBack();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save animal");
            var inner = ex.InnerException?.Message ?? ex.Message;
            ValidationError = inner == ex.Message ? ex.Message : $"{ex.Message}\n{inner}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Runs in its own DI scope so a failure cannot leave a stuck entity on
    // the shared DbContext and corrupt subsequent animal saves.
    private static async Task SyncAnimalAssetAsync(AnimalDto saved)
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var assets = scope.ServiceProvider.GetRequiredService<IAssetRepository>();

            var existing = (await assets.GetByAnimalAsync(saved.AnimalId)).FirstOrDefault();
            var value    = saved.CurrentValue ?? saved.PurchasePrice ?? 0m;

            if (existing is null)
            {
                await assets.AddAsync(new AssetDto
                {
                    AssetName          = saved.BarnName,
                    Category           = AssetCategory.Livestock,
                    PurchaseDate       = saved.PurchaseDate ?? saved.BirthDate,
                    PurchasePrice      = saved.PurchasePrice ?? 0m,
                    CurrentValue       = value,
                    DepreciationMethod = DepreciationMethod.StraightLine,
                    UsefulLifeYears    = 0,
                    SalvageValue       = 0m,
                    LinkedAnimalId     = saved.AnimalId,
                });
            }
            else
            {
                existing.AssetName    = saved.BarnName;
                existing.CurrentValue = value;
                if (saved.PurchasePrice.HasValue)
                    existing.PurchasePrice = saved.PurchasePrice.Value;
                if (saved.Status == AnimalStatus.Sold && existing.DisposedDate is null)
                {
                    existing.DisposedDate  = saved.SoldDate ?? DateTime.Today;
                    existing.DisposalPrice = saved.SalePrice;
                }
                await assets.UpdateAsync(existing);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Asset sync failed for animal {AnimalId} — non-fatal", saved.AnimalId);
        }
    }

    private static async Task CreatePurchaseExpenseAsync(AnimalDto saved, AnimalIntakeResult intake)
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var transactions = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();

            var notesParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(intake.SellerAddress))
                notesParts.Add($"Seller address: {intake.SellerAddress}");
            if (!string.IsNullOrWhiteSpace(intake.ExpenseNotes))
                notesParts.Add(intake.ExpenseNotes);

            await transactions.AddAsync(new TransactionDto
            {
                TransactionType = TransactionType.Expense,
                Category        = intake.ExpenseCategoryKey ?? "Other",
                Date            = intake.PurchaseDate ?? DateTime.Today,
                Amount          = intake.PurchasePrice ?? 0m,
                Description     = $"Livestock purchase – {saved.BarnName}",
                PayeePayer      = intake.SellerName,
                Notes           = notesParts.Count > 0 ? string.Join("\n", notesParts) : null,
                LinkedAnimalId  = saved.AnimalId,
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Purchase expense creation failed for animal {AnimalId} — non-fatal", saved.AnimalId);
        }
    }

    private string? Validate()
    {
        if (HerdId == 0) return "No herd is selected. Please select a herd before adding animals.";
        if (string.IsNullOrWhiteSpace(BarnName)) return "Barn Name is required.";
        if (SelectedBreed is null) return "Breed is required.";
        if (BirthDate > DateTime.Today) return "Birth Date cannot be in the future.";
        if (Weight.HasValue && Weight <= 0) return "Weight must be a positive number.";
        if (Height.HasValue && Height <= 0) return "Height must be a positive number.";
        if (IsBreeding && !IsBreedingAllowed)
            return "An Inactive or Deceased animal cannot be marked as a breeding animal.";
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
        BornOnProperty = BornOnProperty,
        SellerName = BornOnProperty ? null : SellerName, SellerAddress = BornOnProperty ? null : SellerAddress,
        PurchaseDate = BornOnProperty ? null : PurchaseDate, PurchasePrice = BornOnProperty ? null : PurchasePrice,
        IsForSale = IsForSale,
        AskingPrice = IsForSale ? AskingPrice : null,
        CurrentValue = CurrentValue,
        SalePrice = SalePrice,
        BuyerName = BuyerName,
        BuyerAddress = BuyerAddress,
        SoldDate = SoldDate,
        TagNumber = TagNumber, Chondro = Chondro,
        Horns = HornsSelection == "Yes" ? true : HornsSelection == "No" ? false : (bool?)null,
        IsGoodMother = IsGoodMotherSelection == "Yes" ? true : IsGoodMotherSelection == "No" ? false : (bool?)null,
        PastureLocation = PastureLocation, PastureState = PastureState,
        ExpectedHeightAtMaturity = ExpectedHeightAtMaturity,
        SireId = SireInHerd ? SelectedSire?.AnimalId : null,
        ExternalSireName = SireInHerd ? null : ExternalSireName,
        DamId = DamInHerd ? SelectedDam?.AnimalId : null,
        ExternalDamName = DamInHerd ? null : ExternalDamName,
        LastWormingDate = LastWormingDate, LastVaccinationDate = LastVaccinationDate,
        LastHealthCheckDate = LastHealthCheckDate, LastHoofTrimmingDate = LastHoofTrimmingDate,
        HealthNotes = HealthNotes,
        IsBreeding = IsBreeding, IsPregnant = IsPregnant,
        PregnancySireId = PregnancySire?.AnimalId,
        BreedingDate = BreedingDate, ExpectedDueDate = ExpectedDueDate,
        ReproductionNotes = ReproductionNotes,
        MaleBreedingStatus = Gender == Gender.Male && IsBreeding ? MaleBreedingStatus : null,
        CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
    };

    [RelayCommand]
    private void Cancel() => _nav.GoBack();
}
