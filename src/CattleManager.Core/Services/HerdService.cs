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
            BreedingFemales = animals.Count(a =>
                a.Gender == Gender.Female &&
                (a.Status == AnimalStatus.BreedingFemale || a.Status == AnimalStatus.Pregnant)),
            BreedingMales = animals.Count(a =>
                a.Gender == Gender.Male &&
                a.Status == AnimalStatus.BreedingMale &&
                a.MaleBreedingStatus == Models.MaleBreedingStatus.Active),
            DueForHusbandry = animals.Count(a => _healthService.IsOverdueForHusbandry(a)),
            PregnantAnimals = animals.Count(a => a.IsPregnant)
        };
    }

    public async Task<IReadOnlyList<HerdDto>> GetAllHerdsAsync() =>
        await _herds.GetAllAsync();
}
