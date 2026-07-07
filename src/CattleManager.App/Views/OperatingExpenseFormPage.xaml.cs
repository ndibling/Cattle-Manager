using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class OperatingExpenseFormPage : Page
{
    public OperatingExpenseFormPage(TransactionFormViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await vm.LoadAsync();
    }
}
