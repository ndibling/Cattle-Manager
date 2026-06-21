namespace CattleManager.Core.Models;

public class PastureDto
{
    public int     PastureId   { get; set; }
    public string  PastureName { get; set; } = string.Empty;
    public string? Notes       { get; set; }
    public int     SortOrder   { get; set; }
}
