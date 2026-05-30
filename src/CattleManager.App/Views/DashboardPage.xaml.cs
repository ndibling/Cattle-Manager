using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class DashboardPage : Page
{
    private readonly DashboardViewModel _vm;

    public DashboardPage(DashboardViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
