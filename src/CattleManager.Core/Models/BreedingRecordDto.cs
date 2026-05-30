namespace CattleManager.Core.Models;

public class BreedingRecordDto
{
    public int BreedingRecordId { get; set; }
    public int SireId { get; set; }
    public string SireBarnName { get; set; } = string.Empty;
    public int DamId { get; set; }
    public string DamBarnName { get; set; } = string.Empty;
    public DateTime BreedingDate { get; set; }
    public DateTime ExpectedDueDate { get; set; }
    public DateTime? CalvingDate { get; set; }
    public int? OffspringId { get; set; }
    public string? OffspringBarnName { get; set; }
    public string? OutcomeNotes { get; set; }
    public bool IsSampleData { get; set; }
}
