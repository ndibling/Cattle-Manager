using CattleManager.Core.Models;
using CattleManager.Core.Services;
using FluentAssertions;
using Xunit;

namespace CattleManager.Tests.Core;

public class BreedingServiceTests
{
    private readonly BreedingService _sut = new();

    [Fact]
    public void CalculateDueDate_Adds283Days()
    {
        var breedingDate = new DateTime(2024, 1, 1);
        var expected = breedingDate.AddDays(283);
        _sut.CalculateDueDate(breedingDate).Should().Be(expected);
    }

    [Fact]
    public void RecordPregnancy_Male_ThrowsInvalidOperation()
    {
        var male = MakeAnimal(Gender.Male);
        var act = () => _sut.RecordPregnancy(male, null, DateTime.Today);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RecordPregnancy_Female_SetsPregnantFields()
    {
        var female = MakeAnimal(Gender.Female);
        var breedingDate = DateTime.Today.AddDays(-30);
        var result = _sut.RecordPregnancy(female, 42, breedingDate);

        result.IsPregnant.Should().BeTrue();
        result.Status.Should().Be(AnimalStatus.Pregnant);
        result.PregnancySireId.Should().Be(42);
        result.BreedingDate.Should().Be(breedingDate);
        result.ExpectedDueDate.Should().Be(breedingDate.AddDays(283));
    }

    [Fact]
    public void RecordPregnancy_WithOverrideDate_UsesOverride()
    {
        var female = MakeAnimal(Gender.Female);
        var overrideDate = DateTime.Today.AddDays(100);
        var result = _sut.RecordPregnancy(female, null, DateTime.Today, overrideDate);
        result.ExpectedDueDate.Should().Be(overrideDate);
    }

    [Fact]
    public void ClearPregnancy_ResetsFields()
    {
        var female = MakeAnimal(Gender.Female);
        female.IsPregnant = true;
        female.IsBreeding = true;
        female.Status = AnimalStatus.Pregnant;
        female.PregnancySireId = 5;
        female.ExpectedDueDate = DateTime.Today.AddDays(100);

        var result = _sut.ClearPregnancy(female);

        result.IsPregnant.Should().BeFalse();
        result.ExpectedDueDate.Should().BeNull();
        result.PregnancySireId.Should().BeNull();
        result.Status.Should().Be(AnimalStatus.BreedingFemale);
    }

    [Fact]
    public void ClearPregnancy_NonBreeding_SetsHealthy()
    {
        var female = MakeAnimal(Gender.Female);
        female.IsPregnant = true;
        female.IsBreeding = false;
        female.Status = AnimalStatus.Pregnant;

        var result = _sut.ClearPregnancy(female);
        result.Status.Should().Be(AnimalStatus.Healthy);
    }

    private static AnimalDto MakeAnimal(Gender gender) => new()
    {
        AnimalId = 1, BarnName = "Test",
        Gender = gender, BirthDate = DateTime.Today.AddYears(-2)
    };
}
