namespace CattleManager.Core.Models;

public enum Gender { Male, Female }

public enum AnimalStatus
{
    Healthy   = 0,
    Pregnant  = 3,
    ForSale   = 5,
    Sold      = 6,
    Inactive  = 7,
    Deceased  = 8,
    Calf      = 9,
}

public enum MaleBreedingStatus { Active, Retired, Incapacitated }

public enum HealthRecordType
{
    Worming,
    Vaccination,
    HealthCheck,
    Injury,
    MedicalTreatment,
    HoofTrimming
}

public enum ChondroStatus { NotTested, NonCarrier, NeedsTesting, Yes }
public enum WeightUnit { Pounds, Kilograms }
public enum HeightUnit { Inches, Hands, Centimeters }
public enum ThemeMode { Light, Dark }
public enum AutoBackupFrequency { Never, Daily, Weekly }

// Financial module enums
public enum TransactionType { Income, Expense, CapitalInflux }

public enum ExpenseCategory
{
    FeedHay, VeterinaryMedical, BreedingFees, FuelOil, RepairsMaintenance,
    Utilities, LaborContractWork, TruckingTransportation, Insurance,
    PropertyTaxes, MarketingAuction, SuppliesMiscellaneous, InterestExpense, Other
}

public enum IncomeCategory
{
    LivestockSales, BreedingServices, HayCropSales, CustomWork,
    GovernmentPayments, InsuranceProceeds, MiscellaneousIncome
}

public enum CapitalInfluxType { Grant, EquityInvestment, SharePurchase, Other }

public enum AssetCategory { Livestock, MachineryEquipment, Land, Building, Vehicle, Other }

public enum DepreciationMethod { StraightLine, DB150, Section179 }

public enum LoanType { OperatingLineOfCredit, EquipmentLoan, RealEstateLoan, Other }

public enum PaymentFrequency { Monthly, Quarterly, SemiAnnual, Annual }
