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
    private readonly IAnimalPhotoRepository _photos;
    private readonly IAnimalAttachmentRepository _attachments;
    private readonly IBullExposureRepository _bullExposures;
    private readonly HealthService _healthService;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    public int AnimalId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFemale))]
    [NotifyPropertyChangedFor(nameof(HasHooves))]
    [NotifyPropertyChangedFor(nameof(ShowSaleDetails))]
    private AnimalDto? _animal;

    public bool IsFemale      => Animal?.Gender == Gender.Female;
    public bool HasHooves     => Animal?.HasHooves ?? true;
    public bool ShowSaleDetails => Animal?.IsForSale == true || Animal?.Status == AnimalStatus.Sold;

    [ObservableProperty] private ObservableCollection<HealthRecordDto>       _healthHistory      = [];
    [ObservableProperty] private ObservableCollection<BreedingRecordDto>     _breedingHistory    = [];
    [ObservableProperty] private ObservableCollection<AnimalDto>             _offspring          = [];
    [ObservableProperty] private ObservableCollection<string>                _upcomingTasks      = [];
    [ObservableProperty] private ObservableCollection<AnimalPhotoDto>        _animalPhotos       = [];
    [ObservableProperty] private ObservableCollection<AnimalAttachmentDto>   _animalAttachments  = [];
    [ObservableProperty] private ObservableCollection<BullExposureRecordDto> _bullExposureRecords = [];
    [ObservableProperty] private bool _hasNoUpcomingTasks = true;
    [ObservableProperty] private bool _isLoading;

    // Bull exposure add form
    [ObservableProperty] private bool _isAddingExposure;
    [ObservableProperty] private DateTime  _newExposureStartDate        = DateTime.Today;
    [ObservableProperty] private DateTime? _newExposureEndDate;
    [ObservableProperty] private AnimalDto? _newExposureSire;
    [ObservableProperty] private string?   _newExposureExternalSireName;
    [ObservableProperty] private bool      _newExposureSireInHerd       = true;
    [ObservableProperty] private string?   _newExposureNotes;
    [ObservableProperty] private ObservableCollection<AnimalDto> _availableSires = [];

    public AnimalProfileViewModel(IAnimalRepository animals, IHealthRecordRepository healthRecords,
        IBreedingRecordRepository breedingRecords, IAnimalPhotoRepository photos,
        IAnimalAttachmentRepository attachments, IBullExposureRepository bullExposures,
        HealthService healthService, NavigationService nav, DialogService dialog)
    {
        _animals = animals;
        _healthRecords = healthRecords;
        _breedingRecords = breedingRecords;
        _photos = photos;
        _attachments = attachments;
        _bullExposures = bullExposures;
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

            var photoList = await _photos.GetByAnimalAsync(AnimalId);
            AnimalPhotos = new ObservableCollection<AnimalPhotoDto>(photoList);

            var attachList = await _attachments.GetByAnimalAsync(AnimalId);
            AnimalAttachments = new ObservableCollection<AnimalAttachmentDto>(attachList);

            var exposures = await _bullExposures.GetByAnimalAsync(AnimalId);
            BullExposureRecords = new ObservableCollection<BullExposureRecordDto>(exposures);

            var herdMales = Animal.HerdId > 0
                ? await _animals.GetByHerdAsync(Animal.HerdId)
                : await _animals.GetAllAsync();
            AvailableSires = new ObservableCollection<AnimalDto>(
                herdMales.Where(a => a.Gender == Gender.Male));
        }
        finally
        {
            IsLoading = false;
        }
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
    private async Task AddPhotoAsync()
    {
        var path = _dialog.OpenImageFile();
        if (path is null) return;
        var dto = new AnimalPhotoDto
        {
            AnimalId  = AnimalId,
            FilePath  = path,
            SortOrder = AnimalPhotos.Count,
            AddedDate = DateTime.Now,
        };
        await _photos.AddAsync(dto);
        var updated = await _photos.GetByAnimalAsync(AnimalId);
        AnimalPhotos = new ObservableCollection<AnimalPhotoDto>(updated);
    }

    [RelayCommand]
    private async Task DeletePhotoAsync(AnimalPhotoDto photo)
    {
        if (!_dialog.Confirm($"Remove photo?")) return;
        await _photos.DeleteAsync(photo.AnimalPhotoId);
        AnimalPhotos.Remove(photo);
    }

    [RelayCommand]
    private async Task AddAttachmentAsync()
    {
        var path = _dialog.OpenAnyFile("Select Attachment");
        if (path is null) return;
        var dto = new AnimalAttachmentDto
        {
            AnimalId = AnimalId,
            FilePath = path,
            FileName = System.IO.Path.GetFileName(path)
        };
        await _attachments.AddAsync(dto);
        var updated = await _attachments.GetByAnimalAsync(AnimalId);
        AnimalAttachments = new ObservableCollection<AnimalAttachmentDto>(updated);
    }

    [RelayCommand]
    private async Task DeleteAttachmentAsync(AnimalAttachmentDto attachment)
    {
        if (!_dialog.Confirm($"Remove \"{attachment.FileName}\"?")) return;
        await _attachments.DeleteAsync(attachment.AnimalAttachmentId);
        AnimalAttachments.Remove(attachment);
    }

    [RelayCommand]
    private void ShowAddExposure()
    {
        NewExposureStartDate = DateTime.Today;
        NewExposureEndDate = null;
        NewExposureSire = null;
        NewExposureExternalSireName = null;
        NewExposureSireInHerd = true;
        NewExposureNotes = null;
        IsAddingExposure = true;
    }

    [RelayCommand]
    private void CancelAddExposure() => IsAddingExposure = false;

    [RelayCommand]
    private async Task SaveExposureAsync()
    {
        var dto = new BullExposureRecordDto
        {
            DamId = AnimalId,
            SireId = NewExposureSireInHerd ? NewExposureSire?.AnimalId : null,
            ExternalSireName = NewExposureSireInHerd ? null : NewExposureExternalSireName,
            StartDate = NewExposureStartDate,
            EndDate = NewExposureEndDate,
            Notes = NewExposureNotes
        };
        await _bullExposures.AddAsync(dto);
        var updated = await _bullExposures.GetByAnimalAsync(AnimalId);
        BullExposureRecords = new ObservableCollection<BullExposureRecordDto>(updated);
        IsAddingExposure = false;
    }

    [RelayCommand]
    private async Task DeleteExposureAsync(BullExposureRecordDto record)
    {
        if (!_dialog.Confirm("Remove this bull exposure record?")) return;
        await _bullExposures.DeleteAsync(record.ExposureRecordId);
        BullExposureRecords.Remove(record);
    }

    [RelayCommand]
    private void GoBack() => _nav.GoBack();
}
