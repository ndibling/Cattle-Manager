namespace CattleManager.App.ViewModels;

public enum TransactionMode
{
    // Income
    SellAnimal,           // LivestockSales — animal required, marks Sold, bill of sale
    SellEquipment,        // MiscellaneousIncome — asset combobox, marks asset disposed
    FarmServicesProducts, // CustomWork / BreedingServices / HayCropSales
    OtherIncome,          // GovernmentPayments / InsuranceProceeds / MiscellaneousIncome

    // Expense
    OperatingExpense,     // All expense categories except LivestockPurchase
    BuyCapitalAsset,      // Expense + asset fields always visible
    BuyLivestock,         // LivestockPurchase — optional animal link

    // Capital
    CapitalInflux,        // Grant / EquityInvestment / SharePurchase / Other

    // Loan
    LoanPayment,          // Records principal + interest portions, updates loan balance
}
