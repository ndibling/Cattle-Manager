using CattleManager.App.ViewModels;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class AnimalIntakeWindow : Window
{
    private FarmDto? _farm;

    private static readonly IReadOnlyList<CategoryOption> ExpenseCategories =
    [
        new("LivestockPurchase",     "Livestock Purchase"),
        new("FeedHay",               "Feed & Hay"),
        new("VeterinaryMedical",     "Veterinary & Medical"),
        new("BreedingFees",          "Breeding Fees / AI"),
        new("FuelOil",               "Fuel & Oil"),
        new("RepairsMaintenance",    "Repairs & Maintenance"),
        new("Utilities",             "Utilities"),
        new("LaborContractWork",     "Labor / Contract Work"),
        new("TruckingTransportation","Trucking / Transportation"),
        new("Insurance",             "Insurance"),
        new("PropertyTaxes",         "Property Taxes"),
        new("MarketingAuction",      "Marketing / Auction Fees"),
        new("SuppliesMiscellaneous", "Supplies & Miscellaneous"),
        new("InterestExpense",       "Interest Expense"),
    ];

    public AnimalIntakeResult? Result { get; private set; }

    public AnimalIntakeWindow()
    {
        InitializeComponent();
        PurchaseDatePicker.SelectedDate = DateTime.Today;
        CategoryCombo.ItemsSource = ExpenseCategories;
        CategoryCombo.SelectedIndex = 0;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var farms = scope.ServiceProvider.GetRequiredService<IFarmRepository>();
            _farm = await farms.GetDefaultAsync();
        }
        catch { }
    }

    private void BornOnFarm_Click(object sender, RoutedEventArgs e)
    {
        Result = new AnimalIntakeResult(
            BornOnFarm: true,
            BreedersName: _farm?.FarmName,
            CurrentOwner: _farm?.ContactInfo ?? _farm?.FarmName,
            BreedersAddress: _farm?.Address,
            SellerName: null,
            SellerAddress: null,
            PurchaseDate: null,
            PurchasePrice: null,
            ExpenseCategoryKey: null,
            ExpenseNotes: null
        );
        DialogResult = true;
    }

    private void Purchased_Click(object sender, RoutedEventArgs e)
    {
        Step1Panel.Visibility = Visibility.Collapsed;
        Step2Panel.Visibility = Visibility.Visible;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Visibility = Visibility.Collapsed;
        Step2Panel.Visibility = Visibility.Collapsed;
        Step1Panel.Visibility = Visibility.Visible;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Visibility = Visibility.Collapsed;

        if (!decimal.TryParse(PurchasePriceBox.Text.Trim(), out decimal price) || price < 0)
        {
            ValidationText.Text = "Please enter a valid purchase price.";
            ValidationText.Visibility = Visibility.Visible;
            PurchasePriceBox.Focus();
            return;
        }

        var category = (CategoryCombo.SelectedItem as CategoryOption)?.Key ?? "Other";
        var sellerName = NullIfEmpty(SellerNameBox.Text);
        var sellerAddress = NullIfEmpty(SellerAddressBox.Text);
        var notes = NullIfEmpty(NotesBox.Text);

        Result = new AnimalIntakeResult(
            BornOnFarm: false,
            BreedersName: null,
            CurrentOwner: null,
            BreedersAddress: null,
            SellerName: sellerName,
            SellerAddress: sellerAddress,
            PurchaseDate: PurchaseDatePicker.SelectedDate ?? DateTime.Today,
            PurchasePrice: price,
            ExpenseCategoryKey: category,
            ExpenseNotes: notes
        );
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
