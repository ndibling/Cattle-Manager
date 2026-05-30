using CattleManager.Core.Models;

namespace CattleManager.Data.Entities;

public class Animal
{
    public int AnimalId { get; set; }
    public int HerdId { get; set; }
    public string BarnName { get; set; } = string.Empty;
    public string? RegisteredName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? RegistrationOrganization { get; set; }
    public int BreedId { get; set; }
    public Gender Gender { get; set; }
    public AnimalStatus Status { get; set; }
    public decimal? Height { get; set; }
    public HeightUnit HeightUnit { get; set; }
    public decimal? Weight { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public string? Coloring { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime BirthDate { get; set; }
    public DateTime? DateAcquired { get; set; }
    public string? CurrentLocation { get; set; }
    public string? BreedersName { get; set; }
    public string? CurrentOwner { get; set; }
    public int? SireId { get; set; }
    public int? DamId { get; set; }
    public string? ExternalSireName { get; set; }
    public string? ExternalDamName { get; set; }
    public DateTime? LastWormingDate { get; set; }
    public DateTime? LastVaccinationDate { get; set; }
    public DateTime? LastHealthCheckDate { get; set; }
    public string? HealthNotes { get; set; }
    public bool IsBreeding { get; set; }
    public bool IsPregnant { get; set; }
    public int? PregnancySireId { get; set; }
    public DateTime? ExpectedDueDate { get; set; }
    public DateTime? BreedingDate { get; set; }
    public string? ReproductionNotes { get; set; }
    public MaleBreedingStatus? MaleBreedingStatus { get; set; }
    public bool IsSampleData { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public Herd Herd { get; set; } = null!;
    public Breed Breed { get; set; } = null!;
    public Animal? Sire { get; set; }
    public Animal? Dam { get; set; }
    public Animal? PregnancySire { get; set; }
    public ICollection<Animal> SireOffspring { get; set; } = new List<Animal>();
    public ICollection<Animal> DamOffspring { get; set; } = new List<Animal>();
    public ICollection<HealthRecord> HealthRecords { get; set; } = new List<HealthRecord>();
    public ICollection<BreedingRecord> BreedingRecordsAsSire { get; set; } = new List<BreedingRecord>();
    public ICollection<BreedingRecord> BreedingRecordsAsDam { get; set; } = new List<BreedingRecord>();
}
