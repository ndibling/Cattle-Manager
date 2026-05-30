using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CattleManager.Data;
using CattleManager.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CattleManager.Tests.Core;

public class SampleDataSeederTests : IDisposable
{
    private readonly CattleDbContext _db;
    private readonly SampleDataSeeder _seeder;

    public SampleDataSeederTests()
    {
        var opts = new DbContextOptionsBuilder<CattleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CattleDbContext(opts);
        _db.Database.EnsureCreated();

        var animals = new AnimalRepository(_db);
        var herds = new HerdRepository(_db);
        var breeds = new BreedRepository(_db);
        var farms = new FarmRepository(_db);
        var health = new HealthRecordRepository(_db);
        var breeding = new BreedingRecordRepository(_db);
        var settings = new AppSettingsRepository(_db);

        _seeder = new SampleDataSeeder(animals, herds, breeds, farms, health, breeding, settings);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ShouldSeedAsync_BeforeSeeding_ReturnsTrue()
    {
        var result = await _seeder.ShouldSeedAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSeedAsync_AfterSeeding_ReturnsFalse()
    {
        await _seeder.SeedAsync();
        var result = await _seeder.ShouldSeedAsync();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SeedAsync_CreatesFarm()
    {
        await _seeder.SeedAsync();
        var farm = await _db.Farms.FirstOrDefaultAsync();
        farm.Should().NotBeNull();
        farm!.FarmName.Should().Be("Rolling Hills Ranch");
    }

    [Fact]
    public async Task SeedAsync_CreatesSampleHerd()
    {
        await _seeder.SeedAsync();
        var herds = await _db.Herds.Where(h => h.IsSampleData).ToListAsync();
        herds.Should().HaveCount(1);
        herds[0].HerdName.Should().Be("Rolling Hills Angus");
    }

    [Fact]
    public async Task SeedAsync_CreatesExpectedAnimalCount()
    {
        await _seeder.SeedAsync();
        var animals = await _db.Animals.Where(a => a.IsSampleData).ToListAsync();
        animals.Should().HaveCountGreaterThan(15);
    }

    [Fact]
    public async Task SeedAsync_CreatesBullWithPedigree()
    {
        await _seeder.SeedAsync();
        var bull = await _db.Animals.FirstOrDefaultAsync(a => a.BarnName == "Atlas");
        bull.Should().NotBeNull();
        bull!.Gender.Should().Be(Gender.Male);
        bull.SireId.Should().NotBeNull();
        bull.DamId.Should().NotBeNull();
    }

    [Fact]
    public async Task SeedAsync_CreatesPregnantCows()
    {
        await _seeder.SeedAsync();
        var pregnant = await _db.Animals
            .Where(a => a.IsSampleData && a.IsPregnant)
            .ToListAsync();
        pregnant.Should().HaveCountGreaterThan(0);
        pregnant.Should().AllSatisfy(a =>
        {
            a.ExpectedDueDate.Should().NotBeNull();
            a.BreedingDate.Should().NotBeNull();
            a.PregnancySireId.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task SeedAsync_CreatesHealthRecords()
    {
        await _seeder.SeedAsync();
        var records = await _db.HealthRecords.Where(h => h.IsSampleData).ToListAsync();
        records.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task SeedAsync_CreatesBreedingRecords()
    {
        await _seeder.SeedAsync();
        var records = await _db.BreedingRecords.Where(b => b.IsSampleData).ToListAsync();
        records.Should().HaveCountGreaterThan(0);
        records.Should().AllSatisfy(r => r.CalvingDate.Should().NotBeNull());
    }

    [Fact]
    public async Task SeedAsync_SetsSampleDataLoadedSetting()
    {
        await _seeder.SeedAsync();
        var setting = await _db.AppSettings.FindAsync("SampleDataLoaded");
        setting.Should().NotBeNull();
        setting!.Value.Should().Be("true");
    }

    [Fact]
    public async Task ClearSampleDataAsync_RemovesAllSampleAnimals()
    {
        await _seeder.SeedAsync();
        await _seeder.ClearSampleDataAsync();

        var animals = await _db.Animals.Where(a => a.IsSampleData).ToListAsync();
        animals.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearSampleDataAsync_RemovesSampleHerd()
    {
        await _seeder.SeedAsync();
        await _seeder.ClearSampleDataAsync();

        var herds = await _db.Herds.Where(h => h.IsSampleData).ToListAsync();
        herds.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearSampleDataAsync_RemovesSampleHealthRecords()
    {
        await _seeder.SeedAsync();
        await _seeder.ClearSampleDataAsync();

        var records = await _db.HealthRecords.Where(h => h.IsSampleData).ToListAsync();
        records.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearSampleDataAsync_SetsSampleDataClearedSetting()
    {
        await _seeder.SeedAsync();
        await _seeder.ClearSampleDataAsync();

        var setting = await _db.AppSettings.FindAsync("SampleDataCleared");
        setting.Should().NotBeNull();
        setting!.Value.Should().Be("true");
    }

    [Fact]
    public async Task ShouldSeedAsync_AfterClear_ReturnsFalse()
    {
        await _seeder.SeedAsync();
        await _seeder.ClearSampleDataAsync();

        // SampleDataLoaded is "true" so ShouldSeed is false even after clearing
        var result = await _seeder.ShouldSeedAsync();
        result.Should().BeFalse();
    }
}
