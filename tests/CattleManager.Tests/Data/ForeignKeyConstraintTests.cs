using CattleManager.Core.Models;
using CattleManager.Data;
using CattleManager.Data.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CattleManager.Tests.Data;

// These tests use a real SQLite in-memory database (not EF InMemory) because
// EF InMemory skips FK enforcement. The SQLite engine actually checks FK constraints,
// so these tests will catch regressions at the data layer.
public class ForeignKeyConstraintTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly CattleDbContext _db;
    private readonly AnimalRepository _animals;
    private readonly HerdRepository _herds;
    private readonly FarmRepository _farms;

    public ForeignKeyConstraintTests()
    {
        _conn = new SqliteConnection("Data Source=:memory:");
        _conn.Open();

        var opts = new DbContextOptionsBuilder<CattleDbContext>()
            .UseSqlite(_conn)
            .Options;
        _db = new CattleDbContext(opts);
        _db.Database.EnsureCreated();

        _animals = new AnimalRepository(_db);
        _herds = new HerdRepository(_db);
        _farms = new FarmRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    // Root cause of the "SQLite Error 19: FOREIGN KEY constraint failed" bug:
    // When the user reached Add Animal from the Dashboard with no herd selected,
    // HerdId was left at 0. No herd row with ID 0 exists, so the INSERT failed.
    // This test guards against that regression at the data layer.
    [Fact]
    public async Task AddAnimal_WithNonExistentHerdId_ThrowsDbUpdateException()
    {
        // Arrange — no herd is inserted; HerdId = 0 has no matching Herds row
        var dto = new AnimalDto
        {
            HerdId = 0,
            BreedId = 1,   // Angus, seeded by EnsureCreated
            BarnName = "TestCow",
            Gender = Gender.Female,
            Status = AnimalStatus.Healthy,
            BirthDate = DateTime.Today.AddYears(-1),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var act = async () => await _animals.AddAsync(dto);

        // Assert — must throw before committing, not silently insert a corrupt row
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task AddAnimal_WithValidHerdId_Succeeds()
    {
        // Arrange
        var farm = await _farms.UpsertAsync(new FarmDto { FarmName = "Test Farm" });
        var herd = await _herds.AddAsync(new HerdDto
        {
            FarmId = farm.FarmId, HerdName = "Test Herd", HerdType = "Angus", IsActive = true
        });

        var dto = new AnimalDto
        {
            HerdId = herd.HerdId,
            BreedId = 1,
            BarnName = "TestCow",
            Gender = Gender.Female,
            Status = AnimalStatus.Healthy,
            BirthDate = DateTime.Today.AddYears(-1),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var saved = await _animals.AddAsync(dto);

        // Assert
        saved.AnimalId.Should().BeGreaterThan(0);
        saved.HerdId.Should().Be(herd.HerdId);
    }
}
