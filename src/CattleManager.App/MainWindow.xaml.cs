using CattleManager.App.Services;
using CattleManager.App.ViewModels;
using CattleManager.App.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace CattleManager.App;

public partial class MainWindow : Window
{
    private readonly NavigationService _nav;

    public MainWindow(NavigationService nav)
    {
        _nav = nav;
        InitializeComponent();
        _nav.Initialize(MainFrame);
        NavigateToDashboard();
    }

    private void NavigateToDashboard()
    {
        var vm = App.Services.GetRequiredService<DashboardViewModel>();
        var page = new DashboardPage(vm);
        _nav.NavigateTo(page);
        HighlightNav(BtnDashboard);
    }

    private void BtnDashboard_Click(object sender, RoutedEventArgs e)
    {
        _nav.ClearBack();
        NavigateToDashboard();
    }

    private void BtnHerd_Click(object sender, RoutedEventArgs e)
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<HerdListViewModel>();
        _nav.NavigateTo(new HerdListPage(vm));
        HighlightNav(BtnHerd);
    }

    private void BtnPastureView_Click(object sender, RoutedEventArgs e)
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<PastureViewViewModel>();
        _nav.NavigateTo(new PastureViewPage(vm));
        HighlightNav(BtnPastureView);
    }

    private void BtnAddAnimal_Click(object sender, RoutedEventArgs e)
    {
        var win = new AnimalIntakeWindow { Owner = this, RequireHerdSelection = true };
        if (win.ShowDialog() != true || win.Result is null) return;
        if (win.Result.SelectedHerdId is not int herdId) return;

        var vm = App.Services.GetRequiredService<AnimalFormViewModel>();
        vm.HerdId = herdId;
        vm.ApplyIntake(win.Result);
        _nav.NavigateTo(new AnimalFormPage(vm));
        HighlightNav(BtnAddAnimal);
    }

    private void BtnFinancials_Click(object sender, RoutedEventArgs e)
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<FinancialDashboardViewModel>();
        _nav.NavigateTo(new FinancialDashboardPage(vm));
        HighlightNav(BtnFinancials);
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        _nav.ClearBack();
        var vm = App.Services.GetRequiredService<SettingsViewModel>();
        _nav.NavigateTo(new SettingsPage(vm));
        HighlightNav(BtnSettings);
    }

    private void HighlightNav(Button active)
    {
        foreach (var btn in new[] { BtnDashboard, BtnHerd, BtnPastureView, BtnAddAnimal, BtnFinancials, BtnSettings })
        {
            btn.Tag = btn == active ? "Active" : btn.Name.Replace("Btn", "");
        }
    }
}
