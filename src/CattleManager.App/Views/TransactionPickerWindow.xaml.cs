using CattleManager.App.ViewModels;
using System.Windows;

namespace CattleManager.App.Views;

public partial class TransactionPickerWindow : Window
{
    public TransactionMode? Result { get; private set; }

    public TransactionPickerWindow()
    {
        InitializeComponent();
    }

    private void Pick(TransactionMode mode)
    {
        Result = mode;
        DialogResult = true;
    }

    private void SellAnimal_Click(object sender, RoutedEventArgs e)           => Pick(TransactionMode.SellAnimal);
    private void SellEquipment_Click(object sender, RoutedEventArgs e)        => Pick(TransactionMode.SellEquipment);
    private void FarmServicesProducts_Click(object sender, RoutedEventArgs e) => Pick(TransactionMode.FarmServicesProducts);
    private void OtherIncome_Click(object sender, RoutedEventArgs e)          => Pick(TransactionMode.OtherIncome);
    private void OperatingExpense_Click(object sender, RoutedEventArgs e)     => Pick(TransactionMode.OperatingExpense);
    private void BuyCapitalAsset_Click(object sender, RoutedEventArgs e)      => Pick(TransactionMode.BuyCapitalAsset);
    private void BuyLivestock_Click(object sender, RoutedEventArgs e)         => Pick(TransactionMode.BuyLivestock);
    private void LoanPayment_Click(object sender, RoutedEventArgs e)          => Pick(TransactionMode.LoanPayment);
    private void CapitalInflux_Click(object sender, RoutedEventArgs e)        => Pick(TransactionMode.CapitalInflux);
    private void Cancel_Click(object sender, RoutedEventArgs e)               => DialogResult = false;
}
