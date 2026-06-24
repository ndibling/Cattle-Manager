using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace CattleManager.App.ViewModels;

public partial class PedigreeViewModel : ObservableObject
{
    private readonly PedigreeService _pedigreeService;
    private readonly IAnimalRepository _animals;
    private readonly NavigationService _nav;

    public int AnimalId { get; set; }

    [ObservableProperty] private PedigreeNodeDto? _root;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<AnimalDto> AllAnimals { get; } = [];

    public PedigreeViewModel(PedigreeService pedigreeService, IAnimalRepository animals, NavigationService nav)
    {
        _pedigreeService = pedigreeService;
        _animals = animals;
        _nav = nav;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Root = await _pedigreeService.BuildPedigreeAsync(AnimalId);

            var all = await _animals.GetAllAsync();
            AllAnimals.Clear();
            foreach (var a in all) AllAnimals.Add(a);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task AssignParentAsync(int childAnimalId, string role, int? parentAnimalId, string? externalName)
    {
        await _pedigreeService.AssignParentAsync(childAnimalId, role, parentAnimalId, externalName);
        await LoadAsync();
    }

    public async Task RemoveParentAsync(int childAnimalId, string role)
    {
        await _pedigreeService.RemoveParentAsync(childAnimalId, role);
        await LoadAsync();
    }

    [RelayCommand]
    private void ViewAnimal(PedigreeNodeDto? node)
    {
        if (node?.AnimalId is null || !node.IsInHerd) return;
        var vm = App.Services.GetRequiredService<AnimalProfileViewModel>();
        vm.AnimalId = node.AnimalId.Value;
        _nav.NavigateTo(new AnimalProfilePage(vm));
    }

    [RelayCommand]
    private void GoBack() => _nav.GoBack();
}
