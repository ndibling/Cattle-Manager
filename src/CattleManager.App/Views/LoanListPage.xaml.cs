using CattleManager.App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CattleManager.App.Views;

public partial class LoanListPage : Page
{
    private readonly LoanListViewModel _vm;

    public LoanListPage(LoanListViewModel vm)
    {
        InitializeComponent();
        _vm         = vm;
        DataContext = vm;
        IsVisibleChanged += async (_, e) =>
        {
            if (e.NewValue is true) await _vm.LoadAsync();
        };
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.SelectedLoan is not null)
            _vm.ViewDetailsCommand.Execute(_vm.SelectedLoan);
    }

    private void ActionMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var item = btn.Tag as LoanDisplayItem;
        var menu = new ContextMenu();

        var view = new MenuItem { Header = "View Details" };
        view.Click += (_, _) => _vm.ViewDetailsCommand.Execute(item);
        menu.Items.Add(view);

        var edit = new MenuItem { Header = "Edit" };
        edit.Click += (_, _) => _vm.EditLoanCommand.Execute(item);
        menu.Items.Add(edit);

        menu.Items.Add(new Separator());

        var delete = new MenuItem
        {
            Header     = "Delete",
            Foreground = System.Windows.Media.Brushes.Red
        };
        delete.Click += (_, _) => _vm.DeleteLoanCommand.Execute(item);
        menu.Items.Add(delete);

        menu.PlacementTarget = btn;
        menu.IsOpen = true;
    }
}
