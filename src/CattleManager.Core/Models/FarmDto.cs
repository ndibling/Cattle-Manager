namespace CattleManager.Core.Models;

public class FarmDto
{
    public int FarmId { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactInfo { get; set; }
}

public class AnimalTypeDto
{
    public int AnimalTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string GroupTerm { get; set; } = "Herd";
    public bool IsStandardType { get; set; }
}

public class BreedDto
{
    public int BreedId { get; set; }
    public string BreedName { get; set; } = string.Empty;
    public bool IsStandardBreed { get; set; }
    public int AnimalTypeId { get; set; }
    public string AnimalTypeName { get; set; } = string.Empty;
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
    public double PhotoOffsetX { get; set; } = 0.5;
    public double PhotoOffsetY { get; set; } = 0.5;
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
    /// <summary>AnimalId of the node whose SireId/DamId references this node. Null on the root subject.</summary>
    public int? ChildAnimalId { get; set; }
}
