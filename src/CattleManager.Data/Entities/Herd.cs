namespace CattleManager.Data.Entities;

public class AnimalType
{
    public int AnimalTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string GroupTerm { get; set; } = "Herd";
    public bool IsStandardType { get; set; }

    public ICollection<Herd> Herds { get; set; } = new List<Herd>();
    public ICollection<Breed> Breeds { get; set; } = new List<Breed>();
}

public class Herd
{
    public int HerdId { get; set; }
    public int FarmId { get; set; }
    public string HerdName { get; set; } = string.Empty;
    // Legacy column kept for existing-DB compatibility; always written as "" by EF.
    public string HerdType { get; set; } = string.Empty;
    public int AnimalTypeId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSampleData { get; set; }
    public DateTime CreatedDate { get; set; }

    public Farm Farm { get; set; } = null!;
    public AnimalType AnimalType { get; set; } = null!;
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
    public int AnimalTypeId { get; set; }

    public AnimalType AnimalType { get; set; } = null!;
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

public class AnimalPhoto
{
    public int AnimalPhotoId { get; set; }
    public int AnimalId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public bool IsSampleData { get; set; }

    public Animal Animal { get; set; } = null!;
}

public class AnimalAttachment
{
    public int AnimalAttachmentId { get; set; }
    public int AnimalId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSampleData { get; set; }

    public Animal Animal { get; set; } = null!;
}

public class BullExposureRecord
{
    public int ExposureRecordId { get; set; }
    public int DamId { get; set; }
    public int? SireId { get; set; }
    public string? ExternalSireName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsSampleData { get; set; }

    public Animal Dam { get; set; } = null!;
    public Animal? Sire { get; set; }
}
