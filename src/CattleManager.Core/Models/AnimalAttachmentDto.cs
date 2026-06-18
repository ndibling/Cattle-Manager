namespace CattleManager.Core.Models;

public class AnimalAttachmentDto
{
    public int AnimalAttachmentId { get; set; }
    public int AnimalId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSampleData { get; set; }
}
