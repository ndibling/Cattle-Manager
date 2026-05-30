using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CattleManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CattleManager.Data.Repositories;

public class AnimalRepository : IAnimalRepository
{
    private readonly CattleDbContext _db;

    public AnimalRepository(CattleDbContext db) => _db = db;

    public async Task<AnimalDto?> GetByIdAsync(int animalId)
    {
        var entity = await _db.Animals
            .Include(a => a.Breed)
            .Include(a => a.Sire)
            .Include(a => a.Dam)
            .FirstOrDefaultAsync(a => a.AnimalId == animalId);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyList<AnimalDto>> GetByHerdAsync(int herdId)
    {
        var entities = await _db.Animals
            .Include(a => a.Breed)
            .Include(a => a.Sire)
            .Include(a => a.Dam)
            .Where(a => a.HerdId == herdId)
            .OrderBy(a => a.BarnName)
            .ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<AnimalDto>> GetAllAsync()
    {
        var entities = await _db.Animals
            .Include(a => a.Breed)
            .OrderBy(a => a.BarnName)
            .ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<AnimalDto> AddAsync(AnimalDto dto)
    {
        var entity = MapToEntity(dto);
        entity.CreatedDate = DateTime.UtcNow;
        entity.ModifiedDate = DateTime.UtcNow;
        _db.Animals.Add(entity);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(entity.AnimalId) ?? throw new InvalidOperationException("Add failed");
    }

    public async Task<AnimalDto> UpdateAsync(AnimalDto dto)
    {
        var entity = await _db.Animals.FindAsync(dto.AnimalId)
            ?? throw new ArgumentException($"Animal {dto.AnimalId} not found");
        MapToEntityExisting(dto, entity);
        entity.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(entity.AnimalId) ?? throw new InvalidOperationException("Update failed");
    }

    public async Task DeleteAsync(int animalId)
    {
        var entity = await _db.Animals.FindAsync(animalId);
        if (entity is not null)
        {
            _db.Animals.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<AnimalDto>> GetOffspringAsync(int parentId)
    {
        var entities = await _db.Animals
            .Include(a => a.Breed)
            .Where(a => a.SireId == parentId || a.DamId == parentId)
            .OrderBy(a => a.BirthDate)
            .ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<AnimalDto>> SearchAsync(int herdId, string searchTerm)
    {
        var lower = searchTerm.ToLower();
        var entities = await _db.Animals
            .Include(a => a.Breed)
            .Where(a => a.HerdId == herdId &&
                (a.BarnName.ToLower().Contains(lower) ||
                 (a.RegisteredName != null && a.RegisteredName.ToLower().Contains(lower))))
            .OrderBy(a => a.BarnName)
            .ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<bool> BarnNameExistsInHerdAsync(int herdId, string barnName, int? excludeAnimalId = null)
    {
        var query = _db.Animals.Where(a => a.HerdId == herdId &&
            a.BarnName.ToLower() == barnName.ToLower());
        if (excludeAnimalId.HasValue)
            query = query.Where(a => a.AnimalId != excludeAnimalId.Value);
        return await query.AnyAsync();
    }

    private static AnimalDto MapToDto(Animal e) => new()
    {
        AnimalId = e.AnimalId, HerdId = e.HerdId,
        BarnName = e.BarnName, RegisteredName = e.RegisteredName,
        RegistrationNumber = e.RegistrationNumber, RegistrationOrganization = e.RegistrationOrganization,
        BreedId = e.BreedId, BreedName = e.Breed?.BreedName ?? string.Empty,
        Gender = e.Gender, Status = e.Status,
        Height = e.Height, HeightUnit = e.HeightUnit,
        Weight = e.Weight, WeightUnit = e.WeightUnit,
        Coloring = e.Coloring, PhotoPath = e.PhotoPath,
        BirthDate = e.BirthDate, DateAcquired = e.DateAcquired,
        CurrentLocation = e.CurrentLocation, BreedersName = e.BreedersName, CurrentOwner = e.CurrentOwner,
        SireId = e.SireId, SireBarnName = e.Sire?.BarnName,
        DamId = e.DamId, DamBarnName = e.Dam?.BarnName,
        ExternalSireName = e.ExternalSireName, ExternalDamName = e.ExternalDamName,
        LastWormingDate = e.LastWormingDate, LastVaccinationDate = e.LastVaccinationDate,
        LastHealthCheckDate = e.LastHealthCheckDate, HealthNotes = e.HealthNotes,
        IsBreeding = e.IsBreeding, IsPregnant = e.IsPregnant,
        PregnancySireId = e.PregnancySireId, ExpectedDueDate = e.ExpectedDueDate,
        BreedingDate = e.BreedingDate, ReproductionNotes = e.ReproductionNotes,
        MaleBreedingStatus = e.MaleBreedingStatus,
        IsSampleData = e.IsSampleData, CreatedDate = e.CreatedDate, ModifiedDate = e.ModifiedDate
    };

    private static Animal MapToEntity(AnimalDto dto)
    {
        var e = new Animal();
        MapToEntityExisting(dto, e);
        return e;
    }

    private static void MapToEntityExisting(AnimalDto dto, Animal e)
    {
        e.HerdId = dto.HerdId; e.BarnName = dto.BarnName;
        e.RegisteredName = dto.RegisteredName; e.RegistrationNumber = dto.RegistrationNumber;
        e.RegistrationOrganization = dto.RegistrationOrganization;
        e.BreedId = dto.BreedId; e.Gender = dto.Gender; e.Status = dto.Status;
        e.Height = dto.Height; e.HeightUnit = dto.HeightUnit;
        e.Weight = dto.Weight; e.WeightUnit = dto.WeightUnit;
        e.Coloring = dto.Coloring; e.PhotoPath = dto.PhotoPath;
        e.BirthDate = dto.BirthDate; e.DateAcquired = dto.DateAcquired;
        e.CurrentLocation = dto.CurrentLocation; e.BreedersName = dto.BreedersName;
        e.CurrentOwner = dto.CurrentOwner;
        e.SireId = dto.SireId; e.DamId = dto.DamId;
        e.ExternalSireName = dto.ExternalSireName; e.ExternalDamName = dto.ExternalDamName;
        e.LastWormingDate = dto.LastWormingDate; e.LastVaccinationDate = dto.LastVaccinationDate;
        e.LastHealthCheckDate = dto.LastHealthCheckDate; e.HealthNotes = dto.HealthNotes;
        e.IsBreeding = dto.IsBreeding; e.IsPregnant = dto.IsPregnant;
        e.PregnancySireId = dto.PregnancySireId; e.ExpectedDueDate = dto.ExpectedDueDate;
        e.BreedingDate = dto.BreedingDate; e.ReproductionNotes = dto.ReproductionNotes;
        e.MaleBreedingStatus = dto.MaleBreedingStatus;
        e.IsSampleData = dto.IsSampleData;
    }
}
