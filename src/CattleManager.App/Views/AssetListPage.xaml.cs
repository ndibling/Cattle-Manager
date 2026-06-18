using CattleManager.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class AssetListPage : Page
{
    private readonly AssetListViewModel _vm;

    public AssetListPage(AssetListViewModel vm)
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
        if (_vm.SelectedAsset is not null)
            _vm.EditAssetCommand.Execute(_vm.SelectedAsset);
    }

    private void ActionMenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var item = btn.Tag as AssetDisplayItem;

        var menu = new ContextMenu();
        var editItem = new MenuItem { Header = "Edit" };
        editItem.Click += (_, _) => _vm.EditAssetCommand.Execute(item);
        var deleteItem = new MenuItem { Header = "Delete", Foreground = System.Windows.Media.Brushes.Red };
        deleteItem.Click += (_, _) => _vm.DeleteAssetCommand.Execute(item);

        menu.Items.Add(editItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(deleteItem);

        menu.PlacementTarget = btn;
        menu.IsOpen = true;
        e.Handled = true;
    }
}
