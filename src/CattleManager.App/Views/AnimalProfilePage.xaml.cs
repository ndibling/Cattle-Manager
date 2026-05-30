using CattleManager.App.ViewModels;
using CattleManager.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class AnimalProfilePage : Page
{
    private readonly AnimalProfileViewModel _vm;

    public AnimalProfilePage(AnimalProfileViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private void Offspring_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is AnimalDto animal)
            _vm.ViewOffspringCommand.Execute(animal);
    }
}
