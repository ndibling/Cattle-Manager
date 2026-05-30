using CattleManager.Core.Models;

namespace CattleManager.Core.Services;

public class HealthService
{
    private const int WormingIntervalWeeks = 10;
    private const int VaccinationIntervalDays = 365;
    private const int HealthCheckIntervalDays = 180;

    public bool IsOverdueForWorming(AnimalDto animal)
    {
        if (animal.LastWormingDate is null) return true;
        return (DateTime.Today - animal.LastWormingDate.Value).TotalDays > WormingIntervalWeeks * 7;
    }

    public bool IsOverdueForVaccination(AnimalDto animal)
    {
        if (animal.LastVaccinationDate is null) return true;
        return (DateTime.Today - animal.LastVaccinationDate.Value).TotalDays > VaccinationIntervalDays;
    }

    public bool IsOverdueForHealthCheck(AnimalDto animal)
    {
        if (animal.LastHealthCheckDate is null) return false;
        return (DateTime.Today - animal.LastHealthCheckDate.Value).TotalDays > HealthCheckIntervalDays;
    }

    public bool IsOverdueForHusbandry(AnimalDto animal) =>
        IsOverdueForWorming(animal) || IsOverdueForVaccination(animal);

    public IReadOnlyList<string> GetUpcomingTasks(AnimalDto animal)
    {
        var tasks = new List<string>();
        if (IsOverdueForWorming(animal))
            tasks.Add(animal.LastWormingDate is null
                ? "Worming: Never recorded"
                : $"Worming overdue (last: {animal.LastWormingDate:MMM d, yyyy})");
        if (IsOverdueForVaccination(animal))
            tasks.Add(animal.LastVaccinationDate is null
                ? "Vaccination: Never recorded"
                : $"Vaccination overdue (last: {animal.LastVaccinationDate:MMM d, yyyy})");
        if (animal.IsPregnant && animal.ExpectedDueDate.HasValue)
        {
            var daysUntilDue = (animal.ExpectedDueDate.Value - DateTime.Today).Days;
            tasks.Add(daysUntilDue >= 0
                ? $"Calving due in {daysUntilDue} day{(daysUntilDue == 1 ? "" : "s")} ({animal.ExpectedDueDate:MMM d, yyyy})"
                : $"Calving overdue by {-daysUntilDue} day{(-daysUntilDue == 1 ? "" : "s")}");
        }
        return tasks;
    }
}
