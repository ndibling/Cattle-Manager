using CattleManager.Core.Models;

namespace CattleManager.Core.Services;

public interface IAnimalRepository
{
    Task<AnimalDto?> GetByIdAsync(int animalId);
    Task<IReadOnlyList<AnimalDto>> GetByHerdAsync(int herdId);
    Task<IReadOnlyList<AnimalDto>> GetAllAsync();
    Task<AnimalDto> AddAsync(AnimalDto animal);
    Task<AnimalDto> UpdateAsync(AnimalDto animal);
    Task DeleteAsync(int animalId);
    Task<IReadOnlyList<AnimalDto>> GetOffspringAsync(int parentId);
    Task<IReadOnlyList<AnimalDto>> SearchAsync(int herdId, string searchTerm);
    Task<bool> BarnNameExistsInHerdAsync(int herdId, string barnName, int? excludeAnimalId = null);
}

public interface IHerdRepository
{
    Task<HerdDto?> GetByIdAsync(int herdId);
    Task<IReadOnlyList<HerdDto>> GetAllAsync(bool includeInactive = false);
    Task<HerdDto> AddAsync(HerdDto herd);
    Task<HerdDto> UpdateAsync(HerdDto herd);
    Task DeleteAsync(int herdId);
}

public interface IBreedRepository
{
    Task<IReadOnlyList<BreedDto>> GetAllAsync();
    Task<BreedDto> AddAsync(BreedDto breed);
    Task DeleteAsync(int breedId);
}

public interface IFarmRepository
{
    Task<FarmDto?> GetDefaultAsync();
    Task<FarmDto> UpsertAsync(FarmDto farm);
}

public interface IHealthRecordRepository
{
    Task<IReadOnlyList<HealthRecordDto>> GetByAnimalAsync(int animalId);
    Task<HealthRecordDto> AddAsync(HealthRecordDto record);
    Task DeleteAsync(int healthRecordId);
    Task DeleteSampleDataAsync();
}

public interface IBreedingRecordRepository
{
    Task<IReadOnlyList<BreedingRecordDto>> GetByAnimalAsync(int animalId);
    Task<BreedingRecordDto> AddAsync(BreedingRecordDto record);
    Task DeleteSampleDataAsync();
}

public interface IAppSettingsRepository
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
}

public interface IAnimalPhotoRepository
{
    Task<IReadOnlyList<AnimalPhotoDto>> GetByAnimalAsync(int animalId);
    Task<AnimalPhotoDto> AddAsync(AnimalPhotoDto photo);
    Task DeleteAsync(int photoId);
    Task DeleteSampleDataAsync();
}

public interface IAnimalAttachmentRepository
{
    Task<IReadOnlyList<AnimalAttachmentDto>> GetByAnimalAsync(int animalId);
    Task<AnimalAttachmentDto> AddAsync(AnimalAttachmentDto attachment);
    Task DeleteAsync(int attachmentId);
    Task DeleteSampleDataAsync();
}

public interface IBullExposureRepository
{
    Task<IReadOnlyList<BullExposureRecordDto>> GetByAnimalAsync(int animalId);
    Task<BullExposureRecordDto> AddAsync(BullExposureRecordDto record);
    Task DeleteAsync(int exposureId);
    Task DeleteSampleDataAsync();
}

public interface IPastureRepository
{
    Task<IReadOnlyList<PastureDto>> GetAllAsync();
    Task<PastureDto> AddAsync(PastureDto dto);
    Task<PastureDto> UpdateAsync(PastureDto dto);
    Task DeleteAsync(int pastureId);
}
