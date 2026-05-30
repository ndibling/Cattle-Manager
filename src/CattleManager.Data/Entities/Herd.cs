namespace CattleManager.Data.Entities;

public class Herd
{
    public int HerdId { get; set; }
    public int FarmId { get; set; }
    public string HerdName { get; set; } = string.Empty;
    public string HerdType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsSampleData { get; set; }
    public DateTime CreatedDate { get; set; }

    public Farm Farm { get; set; } = null!;
    public ICollection<Animal> Animals { get; set; } = new List<Animal>();
}

public class Farm
{
    public int FarmId { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactInfo { get; set; }

    public ICollection<Herd> Herds { get; set; } = new List<Herd>();
}

public class Breed
{
    public int BreedId { get; set; }
    public string BreedName { get; set; } = string.Empty;
    public bool IsStandardBreed { get; set; }

    public ICollection<Animal> Animals { get; set; } = new List<Animal>();
}

public class HealthRecord
{
    public int HealthRecordId { get; set; }
    public int AnimalId { get; set; }
    public DateTime RecordDate { get; set; }
    public CattleManager.Core.Models.HealthRecordType RecordType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? TreatmentDetails { get; set; }
    public string? VeterinarianName { get; set; }
    public bool IsSampleData { get; set; }

    public Animal Animal { get; set; } = null!;
}

public class BreedingRecord
{
    public int BreedingRecordId { get; set; }
    public int SireId { get; set; }
    public int DamId { get; set; }
    public DateTime BreedingDate { get; set; }
    public DateTime ExpectedDueDate { get; set; }
    public DateTime? CalvingDate { get; set; }
    public int? OffspringId { get; set; }
    public string? OutcomeNotes { get; set; }
    public bool IsSampleData { get; set; }

    public Animal Sire { get; set; } = null!;
    public Animal Dam { get; set; } = null!;
    public Animal? Offspring { get; set; }
}

public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
