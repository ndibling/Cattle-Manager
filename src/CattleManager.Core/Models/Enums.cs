namespace CattleManager.Core.Models;

public enum Gender { Male, Female }

public enum AnimalStatus
{
    Healthy,
    BreedingFemale,
    BreedingMale,
    Pregnant,
    Weaned,
    ForSale,
    Sold,
    Inactive,
    Deceased
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
