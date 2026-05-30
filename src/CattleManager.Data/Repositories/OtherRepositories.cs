using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CattleManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CattleManager.Data.Repositories;

public class HerdRepository : IHerdRepository
{
    private readonly CattleDbContext _db;
    public HerdRepository(CattleDbContext db) => _db = db;

    public async Task<HerdDto?> GetByIdAsync(int id)
    {
        var e = await _db.Herds.FindAsync(id);
        return e is null ? null : Map(e);
    }

    public async Task<IReadOnlyList<HerdDto>> GetAllAsync()
    {
        var list = await _db.Herds.Where(h => h.IsActive).OrderBy(h => h.HerdName).ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<HerdDto> AddAsync(HerdDto dto)
    {
        var e = new Herd
        {
            FarmId = dto.FarmId, HerdName = dto.HerdName,
            HerdType = dto.HerdType, IsActive = dto.IsActive,
            IsSampleData = dto.IsSampleData, CreatedDate = DateTime.UtcNow
        };
        _db.Herds.Add(e);
        await _db.SaveChangesAsync();
        dto.HerdId = e.HerdId;
        return dto;
    }

    public async Task<HerdDto> UpdateAsync(HerdDto dto)
    {
        var e = await _db.Herds.FindAsync(dto.HerdId)
            ?? throw new ArgumentException($"Herd {dto.HerdId} not found");
        e.HerdName = dto.HerdName; e.HerdType = dto.HerdType;
        e.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.Herds.FindAsync(id);
        if (e is not null) { _db.Herds.Remove(e); await _db.SaveChangesAsync(); }
    }

    private static HerdDto Map(Herd e) => new()
    {
        HerdId = e.HerdId, FarmId = e.FarmId, HerdName = e.HerdName,
        HerdType = e.HerdType, IsActive = e.IsActive, IsSampleData = e.IsSampleData,
        CreatedDate = e.CreatedDate
    };
}

public class BreedRepository : IBreedRepository
{
    private readonly CattleDbContext _db;
    public BreedRepository(CattleDbContext db) => _db = db;

    public async Task<IReadOnlyList<BreedDto>> GetAllAsync()
    {
        var list = await _db.Breeds.OrderBy(b => b.BreedName).ToListAsync();
        return list.Select(b => new BreedDto
        {
            BreedId = b.BreedId, BreedName = b.BreedName, IsStandardBreed = b.IsStandardBreed
        }).ToList();
    }

    public async Task<BreedDto> AddAsync(BreedDto dto)
    {
        var e = new Breed { BreedName = dto.BreedName, IsStandardBreed = false };
        _db.Breeds.Add(e);
        await _db.SaveChangesAsync();
        dto.BreedId = e.BreedId;
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.Breeds.FindAsync(id);
        if (e is not null) { _db.Breeds.Remove(e); await _db.SaveChangesAsync(); }
    }
}

public class FarmRepository : IFarmRepository
{
    private readonly CattleDbContext _db;
    public FarmRepository(CattleDbContext db) => _db = db;

    public async Task<FarmDto?> GetDefaultAsync()
    {
        var e = await _db.Farms.FirstOrDefaultAsync();
        return e is null ? null : Map(e);
    }

    public async Task<FarmDto> UpsertAsync(FarmDto dto)
    {
        var e = await _db.Farms.FirstOrDefaultAsync();
        if (e is null)
        {
            e = new Farm();
            _db.Farms.Add(e);
        }
        e.FarmName = dto.FarmName; e.Address = dto.Address; e.ContactInfo = dto.ContactInfo;
        await _db.SaveChangesAsync();
        dto.FarmId = e.FarmId;
        return dto;
    }

    private static FarmDto Map(Farm e) => new()
    {
        FarmId = e.FarmId, FarmName = e.FarmName,
        Address = e.Address, ContactInfo = e.ContactInfo
    };
}

public class HealthRecordRepository : IHealthRecordRepository
{
    private readonly CattleDbContext _db;
    public HealthRecordRepository(CattleDbContext db) => _db = db;

    public async Task<IReadOnlyList<HealthRecordDto>> GetByAnimalAsync(int animalId)
    {
        var list = await _db.HealthRecords
            .Where(h => h.AnimalId == animalId)
            .OrderByDescending(h => h.RecordDate)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<HealthRecordDto> AddAsync(HealthRecordDto dto)
    {
        var e = new HealthRecord
        {
            AnimalId = dto.AnimalId, RecordDate = dto.RecordDate,
            RecordType = dto.RecordType, Description = dto.Description,
            TreatmentDetails = dto.TreatmentDetails, VeterinarianName = dto.VeterinarianName,
            IsSampleData = dto.IsSampleData
        };
        _db.HealthRecords.Add(e);
        await _db.SaveChangesAsync();
        dto.HealthRecordId = e.HealthRecordId;
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.HealthRecords.FindAsync(id);
        if (e is not null) { _db.HealthRecords.Remove(e); await _db.SaveChangesAsync(); }
    }

    public async Task DeleteSampleDataAsync()
    {
        var records = await _db.HealthRecords.Where(h => h.IsSampleData).ToListAsync();
        _db.HealthRecords.RemoveRange(records);
        await _db.SaveChangesAsync();
    }

    private static HealthRecordDto Map(HealthRecord e) => new()
    {
        HealthRecordId = e.HealthRecordId, AnimalId = e.AnimalId,
        RecordDate = e.RecordDate, RecordType = e.RecordType,
        Description = e.Description, TreatmentDetails = e.TreatmentDetails,
        VeterinarianName = e.VeterinarianName, IsSampleData = e.IsSampleData
    };
}

public class BreedingRecordRepository : IBreedingRecordRepository
{
    private readonly CattleDbContext _db;
    public BreedingRecordRepository(CattleDbContext db) => _db = db;

    public async Task<IReadOnlyList<BreedingRecordDto>> GetByAnimalAsync(int animalId)
    {
        var list = await _db.BreedingRecords
            .Include(b => b.Sire).Include(b => b.Dam).Include(b => b.Offspring)
            .Where(b => b.SireId == animalId || b.DamId == animalId)
            .OrderByDescending(b => b.BreedingDate)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<BreedingRecordDto> AddAsync(BreedingRecordDto dto)
    {
        var e = new BreedingRecord
        {
            SireId = dto.SireId, DamId = dto.DamId,
            BreedingDate = dto.BreedingDate, ExpectedDueDate = dto.ExpectedDueDate,
            CalvingDate = dto.CalvingDate, OffspringId = dto.OffspringId,
            OutcomeNotes = dto.OutcomeNotes, IsSampleData = dto.IsSampleData
        };
        _db.BreedingRecords.Add(e);
        await _db.SaveChangesAsync();
        dto.BreedingRecordId = e.BreedingRecordId;
        return dto;
    }

    public async Task DeleteSampleDataAsync()
    {
        var records = await _db.BreedingRecords.Where(b => b.IsSampleData).ToListAsync();
        _db.BreedingRecords.RemoveRange(records);
        await _db.SaveChangesAsync();
    }

    private static BreedingRecordDto Map(BreedingRecord e) => new()
    {
        BreedingRecordId = e.BreedingRecordId,
        SireId = e.SireId, SireBarnName = e.Sire?.BarnName ?? string.Empty,
        DamId = e.DamId, DamBarnName = e.Dam?.BarnName ?? string.Empty,
        BreedingDate = e.BreedingDate, ExpectedDueDate = e.ExpectedDueDate,
        CalvingDate = e.CalvingDate, OffspringId = e.OffspringId,
        OffspringBarnName = e.Offspring?.BarnName,
        OutcomeNotes = e.OutcomeNotes, IsSampleData = e.IsSampleData
    };
}

public class AppSettingsRepository : IAppSettingsRepository
{
    private readonly CattleDbContext _db;
    public AppSettingsRepository(CattleDbContext db) => _db = db;

    public async Task<string?> GetAsync(string key)
    {
        var s = await _db.AppSettings.FindAsync(key);
        return s?.Value;
    }

    public async Task SetAsync(string key, string value)
    {
        var s = await _db.AppSettings.FindAsync(key);
        if (s is null)
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            s.Value = value;
        }
        await _db.SaveChangesAsync();
    }
}
