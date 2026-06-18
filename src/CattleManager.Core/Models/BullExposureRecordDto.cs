namespace CattleManager.Core.Models;

public class BullExposureRecordDto
{
    public int ExposureRecordId { get; set; }
    public int DamId { get; set; }
    public int? SireId { get; set; }
    public string? SireBarnName { get; set; }
    public string? ExternalSireName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsSampleData { get; set; }

    public string BullDisplay => SireBarnName ?? ExternalSireName ?? "Unknown";
}
