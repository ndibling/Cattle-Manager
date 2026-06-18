namespace CattleManager.Core.Models;

public class FarmDto
{
    public int FarmId { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactInfo { get; set; }
}

public class BreedDto
{
    public int BreedId { get; set; }
    public string BreedName { get; set; } = string.Empty;
    public bool IsStandardBreed { get; set; }
}

public class AppSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class PedigreeNodeDto
{
    public int? AnimalId { get; set; }
    public string? BarnName { get; set; }
    public string? RegisteredName { get; set; }
    public string? PhotoPath { get; set; }
    public Gender? Gender { get; set; }
    public string? BreedName { get; set; }
    public decimal? Height { get; set; }
    public HeightUnit HeightUnit { get; set; }
    public string? Coloring { get; set; }
    public bool IsInHerd { get; set; }
    public int Generation { get; set; }
    public string Role { get; set; } = string.Empty;
    public PedigreeNodeDto? Sire { get; set; }
    public PedigreeNodeDto? Dam { get; set; }
}
