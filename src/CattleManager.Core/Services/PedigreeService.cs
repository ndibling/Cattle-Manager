using CattleManager.Core.Models;

namespace CattleManager.Core.Services;

public class PedigreeService
{
    private readonly IAnimalRepository _animals;

    public PedigreeService(IAnimalRepository animals)
    {
        _animals = animals;
    }

    public async Task<PedigreeNodeDto> BuildPedigreeAsync(int animalId, int maxGenerations = 4)
    {
        var animal = await _animals.GetByIdAsync(animalId)
            ?? throw new ArgumentException($"Animal {animalId} not found");
        return await BuildNodeAsync(animal, 0, maxGenerations);
    }

    private async Task<PedigreeNodeDto> BuildNodeAsync(AnimalDto animal, int generation, int maxGenerations)
    {
        var node = new PedigreeNodeDto
        {
            AnimalId = animal.AnimalId,
            BarnName = animal.BarnName,
            RegisteredName = animal.RegisteredName,
            PhotoPath = animal.PhotoPath,
            Gender = animal.Gender,
            BreedName = animal.BreedName,
            IsInHerd = true,
            Generation = generation,
            Role = generation == 0 ? "Subject" : (animal.Gender == Gender.Male ? "Sire" : "Dam")
        };

        if (generation >= maxGenerations) return node;

        if (animal.SireId.HasValue)
        {
            var sire = await _animals.GetByIdAsync(animal.SireId.Value);
            node.Sire = sire is not null
                ? await BuildNodeAsync(sire, generation + 1, maxGenerations)
                : CreateUnknownNode(animal.ExternalSireName, Gender.Male, generation + 1, "Sire");
        }
        else if (!string.IsNullOrEmpty(animal.ExternalSireName))
        {
            node.Sire = CreateUnknownNode(animal.ExternalSireName, Gender.Male, generation + 1, "Sire");
        }

        if (animal.DamId.HasValue)
        {
            var dam = await _animals.GetByIdAsync(animal.DamId.Value);
            node.Dam = dam is not null
                ? await BuildNodeAsync(dam, generation + 1, maxGenerations)
                : CreateUnknownNode(animal.ExternalDamName, Gender.Female, generation + 1, "Dam");
        }
        else if (!string.IsNullOrEmpty(animal.ExternalDamName))
        {
            node.Dam = CreateUnknownNode(animal.ExternalDamName, Gender.Female, generation + 1, "Dam");
        }

        return node;
    }

    private static PedigreeNodeDto CreateUnknownNode(string? name, Gender gender, int generation, string role) =>
        new()
        {
            BarnName = string.IsNullOrEmpty(name) ? "Unknown" : name,
            Gender = gender,
            IsInHerd = false,
            Generation = generation,
            Role = role
        };
}
