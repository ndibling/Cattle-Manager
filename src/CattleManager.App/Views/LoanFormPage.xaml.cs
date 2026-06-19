using CattleManager.App.ViewModels;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class LoanFormPage : Page
{
    public LoanFormPage(LoanFormViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
