using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class AnimalFormPage : Page
{
    private readonly AnimalFormViewModel _vm;

    public AnimalFormPage(AnimalFormViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
