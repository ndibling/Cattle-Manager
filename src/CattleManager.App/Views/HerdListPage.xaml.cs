using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class HerdListPage : Page
{
    private readonly HerdListViewModel _vm;

    public HerdListPage(HerdListViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        IsVisibleChanged += async (_, e) =>
        {
            if (e.NewValue is true) await _vm.LoadAsync();
        };
    }
}
