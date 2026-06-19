using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class FinancialDashboardPage : Page
{
    private readonly FinancialDashboardViewModel _vm;

    public FinancialDashboardPage(FinancialDashboardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        IsVisibleChanged += async (_, e) => { if (e.NewValue is true) await _vm.LoadAsync(); };
    }
}
