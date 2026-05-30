using CattleManager.App.ViewModels;
using CattleManager.Core.Models;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class PedigreePage : Page
{
    private readonly PedigreeViewModel _vm;

    public PedigreePage(PedigreeViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private void PedigreeCanvas_NodeClicked(object sender, PedigreeNodeDto node) =>
        _vm.ViewAnimalCommand.Execute(node);

    private void PrintPedigree_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var dlg = new System.Windows.Controls.PrintDialog();
        if (dlg.ShowDialog() == true)
            dlg.PrintVisual(PedigreeCanvas, "Cattle Manager — Pedigree");
    }
}
