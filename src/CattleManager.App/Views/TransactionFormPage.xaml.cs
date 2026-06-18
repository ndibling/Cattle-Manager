using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class TransactionFormPage : Page
{
    private readonly TransactionFormViewModel _vm;

    public TransactionFormPage(TransactionFormViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
