namespace CattleManager.Data.Entities;

public class Pasture
{
    public int     PastureId   { get; set; }
    public int     HerdId      { get; set; }
    public string  PastureName { get; set; } = string.Empty;
    public string? Address     { get; set; }
    public string? State       { get; set; }
    public string? Notes       { get; set; }
    public int     SortOrder   { get; set; }
}
