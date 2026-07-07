using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class CapitalInfluxFormPage : Page
{
    public CapitalInfluxFormPage(TransactionFormViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await vm.LoadAsync();
    }
}
