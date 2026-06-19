using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class ReportsPage : Page
{
    private readonly ReportsViewModel _vm;

    public ReportsPage(ReportsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        IsVisibleChanged += async (_, e) => { if (e.NewValue is true) await _vm.LoadAsync(); };
    }
}
