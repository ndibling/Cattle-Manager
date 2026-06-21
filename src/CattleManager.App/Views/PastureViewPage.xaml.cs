using CattleManager.App.ViewModels;
using CattleManager.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CattleManager.App.Views;

public partial class PastureViewPage : Page
{
    private readonly PastureViewViewModel _vm;

    public PastureViewPage(PastureViewViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private void AnimalCard_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not FrameworkElement el || el.Tag is not AnimalDto animal) return;
        DragDrop.DoDragDrop(el, animal, DragDropEffects.Move);
    }

    private void PastureLane_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(AnimalDto)))
        {
            e.Effects = DragDropEffects.None;
            return;
        }
        e.Effects = DragDropEffects.Move;
        if (GetLaneFromSender(sender) is { } lane)
            lane.IsDragOver = true;
        e.Handled = true;
    }

    private void PastureLane_DragLeave(object sender, DragEventArgs e)
    {
        if (GetLaneFromSender(sender) is { } lane)
            lane.IsDragOver = false;
    }

    private async void PastureLane_Drop(object sender, DragEventArgs e)
    {
        if (GetLaneFromSender(sender) is not { } targetLane) return;
        targetLane.IsDragOver = false;

        if (e.Data.GetData(typeof(AnimalDto)) is not AnimalDto animal) return;
        var targetGroup = GetGroupContainingLane(targetLane);
        if (targetGroup is null) return;

        await _vm.MoveAnimalAsync(animal, targetLane, targetGroup);
        e.Handled = true;
    }

    private void RenamePastureBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not PastureLane lane) return;
        lane.RenameText = lane.PastureName;
        lane.IsRenaming = true;
    }

    private async void DeletePastureBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not PastureLane lane) return;
        await _vm.DeletePastureAsync(lane);
    }

    private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not PastureLane lane) return;
        if (e.Key == Key.Enter) _ = _vm.CommitRenameAsync(lane);
        else if (e.Key == Key.Escape) lane.IsRenaming = false;
    }

    private async void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not PastureLane lane) return;
        await _vm.CommitRenameAsync(lane);
    }

    private async void CommitRenameBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not PastureLane lane) return;
        await _vm.CommitRenameAsync(lane);
    }

    private async void AddPastureBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not HerdBoardGroup group) return;
        await _vm.AddPastureToGroupAsync(group);
    }

    private async void AddPastureTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not FrameworkElement el || el.Tag is not HerdBoardGroup group) return;
        await _vm.AddPastureToGroupAsync(group);
    }

    private static PastureLane? GetLaneFromSender(object sender)
        => sender is FrameworkElement el ? el.Tag as PastureLane : null;

    private HerdBoardGroup? GetGroupContainingLane(PastureLane lane)
    {
        foreach (var group in _vm.HerdGroups)
            if (group.Lanes.Contains(lane)) return group;
        return null;
    }
}
