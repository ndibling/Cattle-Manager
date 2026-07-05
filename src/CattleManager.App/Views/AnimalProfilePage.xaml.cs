using CattleManager.App.ViewModels;
using CattleManager.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

    private void Offspring_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is AnimalDto animal)
            _vm.ViewOffspringCommand.Execute(animal);
    }

    private void PhotoCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2) return;
        if (sender is not FrameworkElement el || el.Tag is not AnimalPhotoDto photo) return;
        var lightbox = new PhotoLightboxWindow(_vm.AnimalPhotos.ToList(), photo);
        lightbox.Owner = Window.GetWindow(this);
        lightbox.ShowDialog();
        e.Handled = true;
    }
}
