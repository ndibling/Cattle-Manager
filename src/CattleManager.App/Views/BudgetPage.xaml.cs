using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class BudgetPage : Page
{
    private readonly BudgetViewModel _vm;

    public BudgetPage(BudgetViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        IsVisibleChanged += async (_, e) => { if (e.NewValue is true) await _vm.LoadAsync(); };
    }
}
