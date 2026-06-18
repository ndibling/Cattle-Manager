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

    public async Task<IReadOnlyList<HerdDto>> GetAllAsync(bool includeInactive = false)
    {
        var query = _db.Herds.AsQueryable();
        if (!includeInactive) query = query.Where(h => h.IsActive);
        var list = await query.OrderBy(h => h.HerdName).ToListAsync();
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
        if (e is null) return;

        // BreedingRecord.SireId/DamId have DeleteBehavior.Restrict, so they block
        // the cascade-delete of herd animals. Remove breeding records referencing
        // animals in this herd before EF attempts to cascade-delete them.
        var animalIds = await _db.Animals
            .Where(a => a.HerdId == id)
            .Select(a => a.AnimalId)
            .ToListAsync();

        if (animalIds.Count > 0)
        {
            var affectedBreedingRecords = await _db.BreedingRecords
                .Where(b => animalIds.Contains(b.SireId) || animalIds.Contains(b.DamId))
                .ToListAsync();
            _db.BreedingRecords.RemoveRange(affectedBreedingRecords);
        }

        _db.Herds.Remove(e);
        await _db.SaveChangesAsync();
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

public class AnimalPhotoRepository : IAnimalPhotoRepository
{
    private readonly CattleDbContext _db;
    public AnimalPhotoRepository(CattleDbContext db) => _db = db;

    public async Task<IReadOnlyList<AnimalPhotoDto>> GetByAnimalAsync(int animalId)
    {
        var list = await _db.AnimalPhotos
            .Where(p => p.AnimalId == animalId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<AnimalPhotoDto> AddAsync(AnimalPhotoDto dto)
    {
        var e = new AnimalPhoto
        {
            AnimalId = dto.AnimalId, FilePath = dto.FilePath,
            Caption = dto.Caption, SortOrder = dto.SortOrder,
            IsSampleData = dto.IsSampleData
        };
        _db.AnimalPhotos.Add(e);
        await _db.SaveChangesAsync();
        dto.AnimalPhotoId = e.AnimalPhotoId;
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.AnimalPhotos.FindAsync(id);
        if (e is not null) { _db.AnimalPhotos.Remove(e); await _db.SaveChangesAsync(); }
    }

    public async Task DeleteSampleDataAsync()
    {
        var records = await _db.AnimalPhotos.Where(p => p.IsSampleData).ToListAsync();
        _db.AnimalPhotos.RemoveRange(records);
        await _db.SaveChangesAsync();
    }

    private static AnimalPhotoDto Map(AnimalPhoto e) => new()
    {
        AnimalPhotoId = e.AnimalPhotoId, AnimalId = e.AnimalId,
        FilePath = e.FilePath, Caption = e.Caption,
        SortOrder = e.SortOrder, IsSampleData = e.IsSampleData
    };
}

public class AnimalAttachmentRepository : IAnimalAttachmentRepository
{
    private readonly CattleDbContext _db;
    public AnimalAttachmentRepository(CattleDbContext db) => _db = db;

    public async Task<IReadOnlyList<AnimalAttachmentDto>> GetByAnimalAsync(int animalId)
    {
        var list = await _db.AnimalAttachments
            .Where(a => a.AnimalId == animalId)
            .OrderBy(a => a.FileName)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<AnimalAttachmentDto> AddAsync(AnimalAttachmentDto dto)
    {
        var e = new AnimalAttachment
        {
            AnimalId = dto.AnimalId, FilePath = dto.FilePath,
            FileName = dto.FileName, Description = dto.Description,
            IsSampleData = dto.IsSampleData
        };
        _db.AnimalAttachments.Add(e);
        await _db.SaveChangesAsync();
        dto.AnimalAttachmentId = e.AnimalAttachmentId;
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.AnimalAttachments.FindAsync(id);
        if (e is not null) { _db.AnimalAttachments.Remove(e); await _db.SaveChangesAsync(); }
    }

    public async Task DeleteSampleDataAsync()
    {
        var records = await _db.AnimalAttachments.Where(a => a.IsSampleData).ToListAsync();
        _db.AnimalAttachments.RemoveRange(records);
        await _db.SaveChangesAsync();
    }

    private static AnimalAttachmentDto Map(AnimalAttachment e) => new()
    {
        AnimalAttachmentId = e.AnimalAttachmentId, AnimalId = e.AnimalId,
        FilePath = e.FilePath, FileName = e.FileName,
        Description = e.Description, IsSampleData = e.IsSampleData
    };
}

public class BullExposureRepository : IBullExposureRepository
{
    private readonly CattleDbContext _db;
    public BullExposureRepository(CattleDbContext db) => _db = db;

    public async Task<IReadOnlyList<BullExposureRecordDto>> GetByAnimalAsync(int animalId)
    {
        var list = await _db.BullExposureRecords
            .Include(e => e.Sire)
            .Where(e => e.DamId == animalId || e.SireId == animalId)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<BullExposureRecordDto> AddAsync(BullExposureRecordDto dto)
    {
        var e = new BullExposureRecord
        {
            DamId = dto.DamId, SireId = dto.SireId,
            ExternalSireName = dto.ExternalSireName,
            StartDate = dto.StartDate, EndDate = dto.EndDate,
            Notes = dto.Notes, IsSampleData = dto.IsSampleData
        };
        _db.BullExposureRecords.Add(e);
        await _db.SaveChangesAsync();
        dto.ExposureRecordId = e.ExposureRecordId;
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.BullExposureRecords.FindAsync(id);
        if (e is not null) { _db.BullExposureRecords.Remove(e); await _db.SaveChangesAsync(); }
    }

    public async Task DeleteSampleDataAsync()
    {
        var records = await _db.BullExposureRecords.Where(e => e.IsSampleData).ToListAsync();
        _db.BullExposureRecords.RemoveRange(records);
        await _db.SaveChangesAsync();
    }

    private static BullExposureRecordDto Map(BullExposureRecord e) => new()
    {
        ExposureRecordId = e.ExposureRecordId,
        DamId = e.DamId, SireId = e.SireId,
        SireBarnName = e.Sire?.BarnName,
        ExternalSireName = e.ExternalSireName,
        StartDate = e.StartDate, EndDate = e.EndDate,
        Notes = e.Notes, IsSampleData = e.IsSampleData
    };
}
