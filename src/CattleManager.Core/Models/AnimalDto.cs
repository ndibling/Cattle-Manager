namespace CattleManager.Core.Models;

public class AnimalDto
{
    public int AnimalId { get; set; }
    public int HerdId { get; set; }
    public string BarnName { get; set; } = string.Empty;
    public string? RegisteredName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? RegistrationOrganization { get; set; }
    public int BreedId { get; set; }
    public string BreedName { get; set; } = string.Empty;
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

    // Acquisition
    public bool BornOnProperty { get; set; } = true;
    public string? SellerName { get; set; }
    public string? SellerAddress { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }

    // Sale info
    public decimal? AskingPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public string? BuyerName { get; set; }
    public string? BuyerAddress { get; set; }
    public DateTime? SoldDate { get; set; }

    // Additional attributes
    public string? TagNumber { get; set; }
    public ChondroStatus Chondro { get; set; }
    public bool? Horns { get; set; }
    public bool? IsGoodMother { get; set; }
    public string? PastureLocation { get; set; }
    public string? PastureState { get; set; }
    public decimal? ExpectedHeightAtMaturity { get; set; }

    public int? SireId { get; set; }
    public string? SireBarnName { get; set; }
    public int? DamId { get; set; }
    public string? DamBarnName { get; set; }
    public string? ExternalSireName { get; set; }
    public string? ExternalDamName { get; set; }
    public DateTime? LastWormingDate { get; set; }
    public DateTime? LastVaccinationDate { get; set; }
    public DateTime? LastHealthCheckDate { get; set; }
    public DateTime? LastHoofTrimmingDate { get; set; }
    public string? HealthNotes { get; set; }
    public bool IsBreeding { get; set; }
    public bool IsPregnant { get; set; }
    public int? PregnancySireId { get; set; }
    public string? PregnancySireBarnName { get; set; }
    public DateTime? ExpectedDueDate { get; set; }
    public DateTime? BreedingDate { get; set; }
    public string? ReproductionNotes { get; set; }
    public MaleBreedingStatus? MaleBreedingStatus { get; set; }
    public bool IsSampleData { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public string AgeDisplay
    {
        get
        {
            var today = DateTime.Today;
            var years = today.Year - BirthDate.Year;
            var months = today.Month - BirthDate.Month;
            if (months < 0) { years--; months += 12; }
            if (years == 0) return $"{months}mo";
            return months == 0 ? $"{years}yr" : $"{years}yr {months}mo";
        }
    }

    public string WeightDisplay => Weight.HasValue
        ? $"{Weight:0.#} {(WeightUnit == WeightUnit.Pounds ? "lbs" : "kg")}"
        : string.Empty;

    public string HeightDisplay => Height.HasValue
        ? HeightUnit switch
        {
            HeightUnit.Hands => $"{Height:0.#} hh",
            HeightUnit.Centimeters => $"{Height:0.#} cm",
            _ => $"{Height:0.#} in"
        }
        : string.Empty;

    public string ExpectedHeightAtMaturityDisplay => ExpectedHeightAtMaturity.HasValue
        ? HeightUnit switch
        {
            HeightUnit.Hands => $"{ExpectedHeightAtMaturity:0.#} hh",
            HeightUnit.Centimeters => $"{ExpectedHeightAtMaturity:0.#} cm",
            _ => $"{ExpectedHeightAtMaturity:0.#} in"
        }
        : string.Empty;

    public string HornsDisplay => Horns == true ? "Yes" : Horns == false ? "No" : "Unknown";

    public string IsGoodMotherDisplay => IsGoodMother == true ? "Yes" : IsGoodMother == false ? "No" : "Unknown";
}
