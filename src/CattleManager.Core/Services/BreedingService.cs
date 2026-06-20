using CattleManager.Core.Models;

namespace CattleManager.Core.Services;

public class BreedingService
{
    private const int CattleGestationDays = 283;

    public DateTime CalculateDueDate(DateTime breedingDate) =>
        breedingDate.AddDays(CattleGestationDays);

    public AnimalDto RecordPregnancy(AnimalDto female, int? sireId, DateTime breedingDate, DateTime? overrideDueDate = null)
    {
        if (female.Gender != Gender.Female)
            throw new InvalidOperationException("Pregnancy can only be recorded for female animals.");

        female.IsPregnant = true;
        female.IsBreeding = true;
        female.Status = AnimalStatus.Pregnant;
        female.PregnancySireId = sireId;
        female.BreedingDate = breedingDate;
        female.ExpectedDueDate = overrideDueDate ?? CalculateDueDate(breedingDate);
        return female;
    }

    public AnimalDto ClearPregnancy(AnimalDto female)
    {
        female.IsPregnant = false;
        female.ExpectedDueDate = null;
        female.PregnancySireId = null;
        female.Status = AnimalStatus.Healthy;
        return female;
    }

    public async Task<IReadOnlyList<AnimalDto>> GetOffspringAsync(IAnimalRepository repository, int parentId) =>
        await repository.GetOffspringAsync(parentId);

    public async Task<int> CountOffspringInHerdAsync(IAnimalRepository repository, int herdId, int sireId)
    {
        var animals = await repository.GetByHerdAsync(herdId);
        return animals.Count(a => a.SireId == sireId);
    }
}
