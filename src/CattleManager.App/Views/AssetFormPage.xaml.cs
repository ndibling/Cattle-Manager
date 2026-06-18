using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class AssetFormPage : Page
{
    private readonly AssetFormViewModel _vm;

    public AssetFormPage(AssetFormViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
