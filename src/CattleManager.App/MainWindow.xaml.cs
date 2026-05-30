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
        var vm = App.Services.GetRequiredService<HerdDetailsViewModel>();
        _nav.NavigateTo(new HerdDetailsPage(vm));
        HighlightNav(BtnHerd);
    }

    private void BtnAddAnimal_Click(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<AnimalFormViewModel>();
        _nav.NavigateTo(new AnimalFormPage(vm));
        HighlightNav(BtnAddAnimal);
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
        foreach (var btn in new[] { BtnDashboard, BtnHerd, BtnAddAnimal, BtnSettings })
        {
            btn.Tag = btn == active ? "Active" : btn.Name.Replace("Btn", "");
        }
    }
}
