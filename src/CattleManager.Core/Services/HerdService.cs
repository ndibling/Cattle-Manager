using CattleManager.Core.Models;

namespace CattleManager.Core.Services;

public class HerdService
{
    private readonly IAnimalRepository _animals;
    private readonly IHerdRepository _herds;
    private readonly HealthService _healthService;

    public HerdService(IAnimalRepository animals, IHerdRepository herds, HealthService healthService)
    {
        _animals = animals;
        _herds = herds;
        _healthService = healthService;
    }

    public async Task<HerdSummaryDto> GetSummaryAsync(int herdId)
    {
        var herd = await _herds.GetByIdAsync(herdId);
        var animals = await _animals.GetByHerdAsync(herdId);

        return new HerdSummaryDto
        {
            HerdId = herdId,
            HerdName = herd?.HerdName ?? string.Empty,
            TotalAnimals = animals.Count,
            ActiveAnimals = animals.Count(a =>
                a.Status != AnimalStatus.Deceased &&
                a.Status != AnimalStatus.Inactive &&
                a.Status != AnimalStatus.Sold),
            BreedingFemales = animals.Count(a =>
                a.IsBreeding && a.Gender == Gender.Female &&
                (a.Status == AnimalStatus.Healthy || a.Status == AnimalStatus.Pregnant)),
            BreedingMales = animals.Count(a =>
                a.IsBreeding && a.Gender == Gender.Male && a.Status == AnimalStatus.Healthy),
            DueForHusbandry = animals.Count(a => a.Status != AnimalStatus.Calf && _healthService.IsOverdueForHusbandry(a)),
            PregnantAnimals = animals.Count(a => a.IsPregnant)
        };
    }

    public async Task<IReadOnlyList<HerdDto>> GetAllHerdsAsync() =>
        await _herds.GetAllAsync();
}
