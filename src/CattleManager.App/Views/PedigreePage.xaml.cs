using CattleManager.App.ViewModels;
using CattleManager.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CattleManager.App.Views;

public partial class PedigreePage : Page
{
    private readonly PedigreeViewModel _vm;

    private bool _isPanning;
    private Point _panOrigin;
    private Point _scrollOrigin;

    public PedigreePage(PedigreeViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private void PedigreeCanvas_NodeClicked(object sender, PedigreeNodeDto node) =>
        _vm.ViewAnimalCommand.Execute(node);

    private void PrintPedigree_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
            dlg.PrintVisual(PedigreeCanvas, "Herd Master — Pedigree");
    }

    private void Scroll_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _panOrigin = e.GetPosition(PedigreeScroll);
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
