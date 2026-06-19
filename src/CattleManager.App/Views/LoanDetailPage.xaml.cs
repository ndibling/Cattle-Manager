using CattleManager.App.ViewModels;
using CattleManager.Core.Models;
using System.Windows;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class LoanDetailPage : Page
{
    private readonly LoanDetailViewModel _vm;

    public LoanDetailPage(LoanDetailViewModel vm)
    {
        InitializeComponent();
        _vm         = vm;
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private void DeletePayment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LoanPaymentDto pmt)
            _vm.DeletePaymentCommand.Execute(pmt);
    }
}
