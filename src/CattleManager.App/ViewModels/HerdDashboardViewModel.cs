using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;


namespace CattleManager.App.ViewModels;

public class HerdDashboardViewModel
{
    private readonly NavigationService _nav;

    public string HerdName { get; }
    public int HerdId { get; }
    public int TotalAnimals { get; }
    public int ActiveAnimals { get; }
    public int BreedingFemales { get; }
    public int BreedingMales { get; }
    public int PregnantAnimals { get; }
    public int DueForHusbandry { get; }

    public ICommand ViewHerdDetailsCommand { get; }
    public ICommand ViewActiveAnimalsCommand { get; }
    public ICommand ViewBreedingFemalesCommand { get; }
    public ICommand ViewBreedingMalesCommand { get; }
    public ICommand ViewPregnantCommand { get; }
    public ICommand ViewDueForHusbandryCommand { get; }
    public ICommand AddNewAnimalCommand { get; }

    public HerdDashboardViewModel(HerdSummaryDto summary, NavigationService nav)
    {
        _nav = nav;
        HerdName       = summary.HerdName;
        HerdId         = summary.HerdId;
        TotalAnimals   = summary.TotalAnimals;
        ActiveAnimals  = summary.ActiveAnimals;
        BreedingFemales = summary.BreedingFemales;
        BreedingMales  = summary.BreedingMales;
        PregnantAnimals = summary.PregnantAnimals;
        DueForHusbandry = summary.DueForHusbandry;

        ViewHerdDetailsCommand     = new RelayCommand(() => Navigate("All"));
        ViewActiveAnimalsCommand   = new RelayCommand(() => Navigate("Active"));
        ViewBreedingFemalesCommand = new RelayCommand(() => Navigate("Breeding Female"));
        ViewBreedingMalesCommand   = new RelayCommand(() => Navigate("Breeding Male"));
        ViewPregnantCommand        = new RelayCommand(() => Navigate("Pregnant"));
        ViewDueForHusbandryCommand = new RelayCommand(() => Navigate("Due for Husbandry"));
        AddNewAnimalCommand        = new RelayCommand(DoAddNewAnimal);
    }

    private void Navigate(string filter)
    {
        var vm = App.Services.GetRequiredService<HerdDetailsViewModel>();
        vm.HerdId = HerdId;
        vm.FilterStatus = filter;
        _nav.NavigateTo(new HerdDetailsPage(vm));
    }

    private void DoAddNewAnimal()
    {
        var dialog = App.Services.GetRequiredService<DialogService>();
        var intake = dialog.ShowAnimalIntake();
        if (intake is null) return;

        var vm = App.Services.GetRequiredService<AnimalFormViewModel>();
        vm.HerdId = HerdId;
        vm.ApplyIntake(intake);
        _nav.NavigateTo(new AnimalFormPage(vm));
    }
}
