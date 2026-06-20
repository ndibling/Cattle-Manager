namespace CattleManager.Core.Models;

public class HerdDto
{
    public int HerdId { get; set; }
    public int FarmId { get; set; }
    public string HerdName { get; set; } = string.Empty;
    public string HerdType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSampleData { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class HerdSummaryDto
{
    public int HerdId { get; set; }
    public string HerdName { get; set; } = string.Empty;
    public int TotalAnimals { get; set; }
    public int ActiveAnimals { get; set; }
    public int BreedingFemales { get; set; }
    public int BreedingMales { get; set; }
    public int DueForHusbandry { get; set; }
    public int PregnantAnimals { get; set; }
}
