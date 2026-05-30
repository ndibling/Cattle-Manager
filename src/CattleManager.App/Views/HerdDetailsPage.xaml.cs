using CattleManager.App.ViewModels;
using CattleManager.Core.Models;
using System.Windows;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class HerdDetailsPage : Page
{
    private readonly HerdDetailsViewModel _vm;

    public HerdDetailsPage(HerdDetailsViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_vm.SelectedAnimal is not null)
            _vm.ViewProfileCommand.Execute(_vm.SelectedAnimal);
    }

    private void ActionMenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var animal = btn.Tag as AnimalDto;

        var menu = new ContextMenu();
        var viewItem = new MenuItem { Header = "View Profile" };
        viewItem.Click += (_, _) => _vm.ViewProfileCommand.Execute(animal);
        var editItem = new MenuItem { Header = "Edit Details" };
        editItem.Click += (_, _) => _vm.EditAnimalCommand.Execute(animal);
        var lineageItem = new MenuItem { Header = "View Lineage" };
        lineageItem.Click += (_, _) => _vm.ViewLineageCommand.Execute(animal);
        var deleteItem = new MenuItem { Header = "Delete", Foreground = System.Windows.Media.Brushes.Red };
        deleteItem.Click += (_, _) => _vm.DeleteAnimalCommand.Execute(animal);

        menu.Items.Add(viewItem);
        menu.Items.Add(editItem);
        menu.Items.Add(lineageItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(deleteItem);

        menu.PlacementTarget = btn;
        menu.IsOpen = true;
        e.Handled = true;
    }
}
