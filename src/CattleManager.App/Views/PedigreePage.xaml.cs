using CattleManager.App.Controls;
using CattleManager.App.ViewModels;
using CattleManager.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CattleManager.App.Views;

public partial class PedigreePage : Page
{
    private readonly PedigreeViewModel _vm;

    private bool  _isPanning;
    private Point _panOrigin;
    private Point _scrollOrigin;

    public PedigreePage(PedigreeViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();

        PedigreeCanvas.AddParentRequested    += OnAddParentRequested;
        PedigreeCanvas.RemoveParentRequested += OnRemoveParentRequested;

        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private void PedigreeCanvas_NodeClicked(object sender, PedigreeNodeDto node) =>
        _vm.ViewAnimalCommand.Execute(node);

    private async void OnAddParentRequested(object? sender, AddParentEventArgs e)
    {
        var dialog = new AddParentDialog(e.ChildBarnName, e.Role, _vm.AllAnimals)
        {
            Owner = Window.GetWindow(this)
        };
        if (dialog.ShowDialog() == true)
            await _vm.AssignParentAsync(e.ChildAnimalId, e.Role, dialog.SelectedAnimalId, dialog.ExternalName);
    }

    private async void OnRemoveParentRequested(object? sender, RemoveParentEventArgs e)
    {
        var result = MessageBox.Show(
            $"Remove \"{e.BarnName}\" as {e.Role} from this pedigree?",
            "Remove Parent",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            await _vm.RemoveParentAsync(e.ChildAnimalId, e.Role);
    }

    private void PrintPedigree_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
            dlg.PrintVisual(PedigreeCanvas, "Herd Master — Pedigree");
    }

    private void Scroll_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Don't pan when the click originated from an action button
        var src = e.OriginalSource as DependencyObject;
        while (src is not null && src != PedigreeScroll)
        {
            if (src is Button) return;
            src = VisualTreeHelper.GetParent(src);
        }

        _isPanning    = true;
        _panOrigin    = e.GetPosition(PedigreeScroll);
        _scrollOrigin = new Point(PedigreeScroll.HorizontalOffset, PedigreeScroll.VerticalOffset);
        PedigreeScroll.CaptureMouse();
        PedigreeScroll.Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    private void Scroll_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;
        var pos = e.GetPosition(PedigreeScroll);
        PedigreeScroll.ScrollToHorizontalOffset(_scrollOrigin.X - (pos.X - _panOrigin.X));
        PedigreeScroll.ScrollToVerticalOffset(_scrollOrigin.Y - (pos.Y - _panOrigin.Y));
    }

    private void Scroll_MouseUp(object sender, MouseButtonEventArgs e) => StopPan();

    private void Scroll_MouseLeave(object sender, MouseEventArgs e) => StopPan();

    private void StopPan()
    {
        if (!_isPanning) return;
        _isPanning = false;
        PedigreeScroll.ReleaseMouseCapture();
        PedigreeScroll.Cursor = Cursors.Arrow;
    }
}
