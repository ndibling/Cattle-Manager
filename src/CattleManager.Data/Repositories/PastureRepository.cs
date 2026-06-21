using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CattleManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CattleManager.Data.Repositories;

public class PastureRepository : IPastureRepository
{
    private readonly CattleDbContext _db;
    public PastureRepository(CattleDbContext db) => _db = db;

    public async Task<IReadOnlyList<PastureDto>> GetAllAsync()
    {
        var list = await _db.Pastures.OrderBy(p => p.SortOrder).ThenBy(p => p.PastureName).ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PastureDto>> GetByHerdAsync(int herdId)
    {
        var list = await _db.Pastures
            .Where(p => p.HerdId == herdId)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.PastureName)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<PastureDto> AddAsync(PastureDto dto)
    {
        var e = new Pasture
        {
            HerdId      = dto.HerdId,
            PastureName = dto.PastureName,
            Address     = dto.Address,
            State       = dto.State,
            Notes       = dto.Notes,
            SortOrder   = dto.SortOrder,
        };
        _db.Pastures.Add(e);
        await _db.SaveChangesAsync();
        dto.PastureId = e.PastureId;
        return dto;
    }

    public async Task<PastureDto> UpdateAsync(PastureDto dto)
    {
        var e = await _db.Pastures.FindAsync(dto.PastureId)
            ?? throw new ArgumentException($"Pasture {dto.PastureId} not found");
        e.PastureName = dto.PastureName;
        e.Address     = dto.Address;
        e.State       = dto.State;
        e.Notes       = dto.Notes;
        e.SortOrder   = dto.SortOrder;
        await _db.SaveChangesAsync();
        return dto;
    }

    public async Task DeleteAsync(int pastureId)
    {
        var e = await _db.Pastures.FindAsync(pastureId);
        if (e is null) return;
        _db.Pastures.Remove(e);
        await _db.SaveChangesAsync();
    }

    private static PastureDto Map(Pasture e) => new()
    {
        PastureId   = e.PastureId,
        HerdId      = e.HerdId,
        PastureName = e.PastureName,
        Address     = e.Address,
        State       = e.State,
        Notes       = e.Notes,
        SortOrder   = e.SortOrder,
    };
}
