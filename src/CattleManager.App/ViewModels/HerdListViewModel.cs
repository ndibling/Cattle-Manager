using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace CattleManager.App.ViewModels;

public sealed class HerdListItem
{
    public int HerdId { get; init; }
    public string HerdName { get; init; } = string.Empty;
    public string HerdType { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int AnimalCount { get; init; }
    public HerdDto Source { get; init; } = null!;
    public string AnimalCountLabel => AnimalCount == 1 ? "1 animal" : $"{AnimalCount} animals";
}

public partial class HerdListViewModel : ObservableObject
{
    private readonly IHerdRepository _herds;
    private readonly IAnimalRepository _animals;
    private readonly NavigationService _nav;
    private readonly DialogService _dialog;

    [ObservableProperty] private ObservableCollection<HerdListItem> _herds2 = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _statusMessage;

    public HerdListViewModel(IHerdRepository herds, IAnimalRepository animals,
        NavigationService nav, DialogService dialog)
    {
        _herds  = herds;
        _animals = animals;
        _nav    = nav;
        _dialog = dialog;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var list = await _herds.GetAllAsync(includeInactive: true);
            var items = new List<HerdListItem>();
            foreach (var h in list)
            {
                var animals = await _animals.GetByHerdAsync(h.HerdId);
                items.Add(new HerdListItem
                {
                    HerdId      = h.HerdId,
                    HerdName    = h.HerdName,
                    HerdType    = h.HerdType,
                    IsActive    = h.IsActive,
                    AnimalCount = animals.Count,
                    Source      = h,
                });
            }
            Herds2 = new ObservableCollection<HerdListItem>(items);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddHerd()
    {
        var vm = App.Services.GetRequiredService<HerdFormViewModel>();
        _nav.NavigateTo(new HerdFormPage(vm));
    }

    [RelayCommand]
    private void EditHerd(HerdListItem item)
    {
        var vm = App.Services.GetRequiredService<HerdFormViewModel>();
        vm.HerdId = item.HerdId;
        _nav.NavigateTo(new HerdFormPage(vm));
    }

    [RelayCommand]
    private async Task DeleteHerd(HerdListItem item)
    {
        var animalWarning = item.AnimalCount > 0
            ? $"\n\nWarning: This will permanently delete all {item.AnimalCount} animal(s) and their records."
            : string.Empty;

        if (!_dialog.Confirm(
            $"Delete herd \"{item.HerdName}\"?{animalWarning}\n\nThis cannot be undone.",
            "Delete Herd"))
            return;

        IsLoading = true;
        try
        {
            await _herds.DeleteAsync(item.HerdId);
            Herds2.Remove(item);
            StatusMessage = $"Herd \"{item.HerdName}\" deleted.";
        }
        catch (Exception ex)
        {
            _dialog.ShowError($"Could not delete herd: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ViewAnimals(HerdListItem item)
    {
        var vm = App.Services.GetRequiredService<HerdDetailsViewModel>();
        vm.HerdId = item.HerdId;
        _nav.NavigateTo(new HerdDetailsPage(vm));
    }
}
