using CattleManager.Core.Models;
using CattleManager.Data;
using CattleManager.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CattleManager.Tests.Data;

public class PastureRepositoryTests : IDisposable
{
    private readonly CattleDbContext _db;
    private readonly PastureRepository _sut;

    public PastureRepositoryTests()
    {
        var opts = new DbContextOptionsBuilder<CattleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new CattleDbContext(opts);
        _db.Database.EnsureCreated();
        _sut = new PastureRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task AddAsync_PersistsAndReturnsWithId()
    {
        var dto = new PastureDto { HerdId = 1, PastureName = "North Pasture", Notes = "Near the creek" };

        var result = await _sut.AddAsync(dto);

        result.PastureId.Should().BeGreaterThan(0);
        result.HerdId.Should().Be(1);
        result.PastureName.Should().Be("North Pasture");
        result.Notes.Should().Be("Near the creek");
    }

    [Fact]
    public async Task GetByHerdAsync_ReturnsOnlyMatchingHerd()
    {
        await _sut.AddAsync(new PastureDto { HerdId = 1, PastureName = "North Pasture" });
        await _sut.AddAsync(new PastureDto { HerdId = 1, PastureName = "South Pasture" });
        await _sut.AddAsync(new PastureDto { HerdId = 2, PastureName = "East Pasture" });

        var herd1 = await _sut.GetByHerdAsync(1);
        var herd2 = await _sut.GetByHerdAsync(2);

        herd1.Should().HaveCount(2);
        herd1.Should().OnlyContain(p => p.HerdId == 1);
        herd2.Should().HaveCount(1);
        herd2[0].PastureName.Should().Be("East Pasture");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPastures_OrderedBySortOrderThenName()
    {
        await _sut.AddAsync(new PastureDto { HerdId = 1, PastureName = "Barn",          SortOrder = 2 });
        await _sut.AddAsync(new PastureDto { HerdId = 1, PastureName = "North Pasture", SortOrder = 0 });
        await _sut.AddAsync(new PastureDto { HerdId = 1, PastureName = "South Pasture", SortOrder = 1 });

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(3);
        result[0].PastureName.Should().Be("North Pasture");
        result[1].PastureName.Should().Be("South Pasture");
        result[2].PastureName.Should().Be("Barn");
    }

    [Fact]
    public async Task UpdateAsync_ChangesNameAndNotes()
    {
        var added = await _sut.AddAsync(new PastureDto { HerdId = 1, PastureName = "Old Name", Notes = "Old notes" });
        added.PastureName = "New Name";
        added.Notes       = "Updated notes";

        var result = await _sut.UpdateAsync(added);

        result.PastureName.Should().Be("New Name");
        result.Notes.Should().Be("Updated notes");
        var fromDb = await _sut.GetAllAsync();
        fromDb.Should().ContainSingle(p => p.PastureName == "New Name" && p.Notes == "Updated notes");
    }

    [Fact]
    public async Task DeleteAsync_RemovesPasture()
    {
        var added = await _sut.AddAsync(new PastureDto { HerdId = 1, PastureName = "To Delete" });

        await _sut.DeleteAsync(added.PastureId);

        var result = await _sut.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        var act = async () => await _sut.DeleteAsync(99999);

        await act.Should().NotThrowAsync();
    }
}
