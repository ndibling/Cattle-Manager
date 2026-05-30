using CattleManager.Core.Models;
using CattleManager.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CattleManager.Tests.Core;

public class PedigreeServiceTests
{
    private readonly Mock<IAnimalRepository> _animals = new();

    private PedigreeService CreateSut() => new(_animals.Object);

    [Fact]
    public async Task BuildPedigreeAsync_UnknownAnimal_ThrowsArgumentException()
    {
        _animals.Setup(a => a.GetByIdAsync(99)).ReturnsAsync((AnimalDto?)null);
        await Assert.ThrowsAsync<ArgumentException>(() => CreateSut().BuildPedigreeAsync(99));
    }

    [Fact]
    public async Task BuildPedigreeAsync_Subject_PopulatesRoot()
    {
        var subject = new AnimalDto { AnimalId = 1, BarnName = "Atlas", Gender = Gender.Male, BirthDate = DateTime.Today.AddYears(-3) };
        _animals.Setup(a => a.GetByIdAsync(1)).ReturnsAsync(subject);

        var result = await CreateSut().BuildPedigreeAsync(1, maxGenerations: 0);

        result.AnimalId.Should().Be(1);
        result.BarnName.Should().Be("Atlas");
        result.IsInHerd.Should().BeTrue();
        result.Generation.Should().Be(0);
    }

    [Fact]
    public async Task BuildPedigreeAsync_WithSireInHerd_PopulatesSireNode()
    {
        var subject = new AnimalDto { AnimalId = 1, BarnName = "Calf", Gender = Gender.Male, BirthDate = DateTime.Today, SireId = 2 };
        var sire = new AnimalDto { AnimalId = 2, BarnName = "Bull", Gender = Gender.Male, BirthDate = DateTime.Today.AddYears(-5) };
        _animals.Setup(a => a.GetByIdAsync(1)).ReturnsAsync(subject);
        _animals.Setup(a => a.GetByIdAsync(2)).ReturnsAsync(sire);

        var result = await CreateSut().BuildPedigreeAsync(1, maxGenerations: 1);

        result.Sire.Should().NotBeNull();
        result.Sire!.BarnName.Should().Be("Bull");
        result.Sire.IsInHerd.Should().BeTrue();
    }

    [Fact]
    public async Task BuildPedigreeAsync_WithExternalSire_CreatesUnknownNode()
    {
        var subject = new AnimalDto { AnimalId = 1, BarnName = "Calf", Gender = Gender.Male, BirthDate = DateTime.Today, ExternalSireName = "Outside Bull" };
        _animals.Setup(a => a.GetByIdAsync(1)).ReturnsAsync(subject);

        var result = await CreateSut().BuildPedigreeAsync(1, maxGenerations: 1);

        result.Sire.Should().NotBeNull();
        result.Sire!.BarnName.Should().Be("Outside Bull");
        result.Sire.IsInHerd.Should().BeFalse();
    }

    [Fact]
    public async Task BuildPedigreeAsync_NoParents_SireAndDamNull()
    {
        var subject = new AnimalDto { AnimalId = 1, BarnName = "Loner", Gender = Gender.Female, BirthDate = DateTime.Today };
        _animals.Setup(a => a.GetByIdAsync(1)).ReturnsAsync(subject);

        var result = await CreateSut().BuildPedigreeAsync(1, maxGenerations: 2);

        result.Sire.Should().BeNull();
        result.Dam.Should().BeNull();
    }
}
