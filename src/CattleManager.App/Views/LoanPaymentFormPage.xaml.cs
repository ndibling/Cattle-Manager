using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class LoanPaymentFormPage : Page
{
    public LoanPaymentFormPage(TransactionFormViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await vm.LoadAsync();
    }
}
