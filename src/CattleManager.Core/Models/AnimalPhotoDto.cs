namespace CattleManager.Core.Models;

public class AnimalPhotoDto
{
    public int AnimalPhotoId { get; set; }
    public int AnimalId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public bool IsSampleData { get; set; }
    public DateTime AddedDate { get; set; }
}
