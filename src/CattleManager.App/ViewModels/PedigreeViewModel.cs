using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace CattleManager.App.ViewModels;

public partial class PedigreeViewModel : ObservableObject
{
    private readonly PedigreeService _pedigreeService;
    private readonly NavigationService _nav;

    public int AnimalId { get; set; }

    [ObservableProperty] private PedigreeNodeDto? _root;
    [ObservableProperty] private bool _isLoading;

    public PedigreeViewModel(PedigreeService pedigreeService, NavigationService nav)
    {
        _pedigreeService = pedigreeService;
        _nav = nav;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Root = await _pedigreeService.BuildPedigreeAsync(AnimalId);
        }
        finally
        {
            IsLoading = false;
        }
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
