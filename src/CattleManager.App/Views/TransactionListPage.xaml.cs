using CattleManager.App.ViewModels;
using CattleManager.Core.Models;
using System.Windows;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class TransactionListPage : Page
{
    private readonly TransactionListViewModel _vm;

    public TransactionListPage(TransactionListViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        IsVisibleChanged += async (_, e) =>
        {
            if (e.NewValue is true) await _vm.LoadAsync();
        };
    }

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_vm.SelectedTransaction is not null)
            _vm.EditTransactionCommand.Execute(_vm.SelectedTransaction);
    }

    private void ActionMenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var tx = btn.Tag as TransactionDto;

        var menu = new ContextMenu();
        var editItem = new MenuItem { Header = "Edit" };
        editItem.Click += (_, _) => _vm.EditTransactionCommand.Execute(tx);
        var deleteItem = new MenuItem { Header = "Delete", Foreground = System.Windows.Media.Brushes.Red };
        deleteItem.Click += (_, _) => _vm.DeleteCommand.Execute(tx);

        menu.Items.Add(editItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(deleteItem);

        menu.PlacementTarget = btn;
        menu.IsOpen = true;
        e.Handled = true;
    }
}
