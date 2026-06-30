using CattleManager.App.Services;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CattleManager.App.ViewModels;

public partial class HerdFormViewModel : ObservableObject
{
    private readonly IHerdRepository _herds;
    private readonly IFarmRepository _farms;
    private readonly IAnimalTypeRepository _animalTypes;
    private readonly NavigationService _nav;

    public int HerdId { get; set; }
    public bool IsNewHerd => HerdId == 0;

    [ObservableProperty] private string _formTitle = "Add Herd";
    [ObservableProperty] private string _herdName = string.Empty;
    [ObservableProperty] private AnimalTypeDto? _selectedAnimalType;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string? _validationError;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<AnimalTypeDto> AnimalTypeOptions { get; } = [];

    public HerdFormViewModel(IHerdRepository herds, IFarmRepository farms,
        IAnimalTypeRepository animalTypes, NavigationService nav)
    {
        _herds       = herds;
        _farms       = farms;
        _animalTypes = animalTypes;
        _nav         = nav;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var types = await _animalTypes.GetAllAsync();
            AnimalTypeOptions.Clear();
            foreach (var t in types) AnimalTypeOptions.Add(t);

            if (!IsNewHerd)
            {
                FormTitle = "Edit Herd";
                var herd = await _herds.GetByIdAsync(HerdId);
                if (herd is not null)
                {
                    HerdName = herd.HerdName;
                    IsActive = herd.IsActive;
                    SelectedAnimalType = AnimalTypeOptions.FirstOrDefault(t => t.AnimalTypeId == herd.AnimalTypeId);
                }
            }
            else
            {
                SelectedAnimalType ??= AnimalTypeOptions.FirstOrDefault();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationError = Validate();
        if (ValidationError is not null) return;

        IsLoading = true;
        try
        {
            if (IsNewHerd)
            {
                var farm = await _farms.GetDefaultAsync();
                await _herds.AddAsync(new HerdDto
                {
                    FarmId       = farm?.FarmId ?? 0,
                    HerdName     = HerdName.Trim(),
                    AnimalTypeId = SelectedAnimalType!.AnimalTypeId,
                    IsActive     = IsActive,
                });
            }
            else
            {
                await _herds.UpdateAsync(new HerdDto
                {
                    HerdId       = HerdId,
                    HerdName     = HerdName.Trim(),
                    AnimalTypeId = SelectedAnimalType!.AnimalTypeId,
                    IsActive     = IsActive,
                });
            }
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

    [RelayCommand]
    private void Cancel() => _nav.GoBack();

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(HerdName)) return "Herd Name is required.";
        if (SelectedAnimalType is null) return "Animal Type is required.";
        return null;
    }
}
