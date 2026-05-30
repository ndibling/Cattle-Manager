using CattleManager.Core.Models;
using CattleManager.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CattleManager.Tests.Core;

public class HerdServiceTests
{
    private readonly Mock<IAnimalRepository> _animals = new();
    private readonly Mock<IHerdRepository> _herds = new();
    private readonly HealthService _healthService = new();

    private HerdService CreateSut() => new(_animals.Object, _herds.Object, _healthService);

    [Fact]
    public async Task GetSummaryAsync_CountsCorrectly()
    {
        _herds.Setup(h => h.GetByIdAsync(1)).ReturnsAsync(new HerdDto { HerdId = 1, HerdName = "Test" });
        _animals.Setup(a => a.GetByHerdAsync(1)).ReturnsAsync(new[]
        {
            Animal(Gender.Female, AnimalStatus.BreedingFemale, isBreeding: true),
            Animal(Gender.Female, AnimalStatus.Pregnant, isBreeding: true, isPregnant: true),
            Animal(Gender.Male, AnimalStatus.BreedingMale, maleStatus: MaleBreedingStatus.Active),
            Animal(Gender.Male, AnimalStatus.BreedingMale, maleStatus: MaleBreedingStatus.Retired),
            Animal(Gender.Female, AnimalStatus.Healthy),
            // Overdue for worming: last worming 100 days ago, threshold is 70 days
            Animal(Gender.Female, AnimalStatus.Healthy,
                lastWorming: DateTime.Today.AddDays(-100),
                lastVaccination: DateTime.Today.AddDays(-10))
        });

        var result = await CreateSut().GetSummaryAsync(1);

        result.TotalAnimals.Should().Be(6);
        result.BreedingFemales.Should().Be(2); // BreedingFemale + Pregnant
        result.BreedingMales.Should().Be(1);   // only Active
        result.PregnantAnimals.Should().Be(1);
        result.DueForHusbandry.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSummaryAsync_NoHerd_StillReturnsStats()
    {
        _herds.Setup(h => h.GetByIdAsync(99)).ReturnsAsync((HerdDto?)null);
        _animals.Setup(a => a.GetByHerdAsync(99)).ReturnsAsync(Array.Empty<AnimalDto>());

        var result = await CreateSut().GetSummaryAsync(99);

        result.TotalAnimals.Should().Be(0);
        result.HerdName.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllHerdsAsync_DelegatesToRepository()
    {
        var herds = new[] { new HerdDto { HerdId = 1 }, new HerdDto { HerdId = 2 } };
        _herds.Setup(h => h.GetAllAsync(It.IsAny<bool>())).ReturnsAsync(herds);

        var result = await CreateSut().GetAllHerdsAsync();
        result.Should().HaveCount(2);
    }

    private static AnimalDto Animal(Gender gender, AnimalStatus status,
        bool isBreeding = false, bool isPregnant = false,
        MaleBreedingStatus? maleStatus = null,
        DateTime? lastWorming = null, DateTime? lastVaccination = null) => new()
    {
        BarnName = "x", Gender = gender, Status = status,
        BirthDate = DateTime.Today.AddYears(-2),
        IsBreeding = isBreeding, IsPregnant = isPregnant,
        MaleBreedingStatus = maleStatus,
        LastWormingDate = lastWorming ?? DateTime.Today.AddDays(-10),
        LastVaccinationDate = lastVaccination ?? DateTime.Today.AddDays(-30)
    };
}
