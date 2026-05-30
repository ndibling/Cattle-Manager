namespace CattleManager.Core.Models;

public class HealthRecordDto
{
    public int HealthRecordId { get; set; }
    public int AnimalId { get; set; }
    public DateTime RecordDate { get; set; }
    public HealthRecordType RecordType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? TreatmentDetails { get; set; }
    public string? VeterinarianName { get; set; }
    public bool IsSampleData { get; set; }
}
