using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CattleManager.Data;
using CattleManager.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CattleManager.Tests.Data;

public class RepositoryTests : IDisposable
{
    private readonly CattleDbContext _db;
    private readonly AnimalRepository _animals;
    private readonly HerdRepository _herds;
    private readonly BreedRepository _breeds;
    private readonly FarmRepository _farms;
    private readonly HealthRecordRepository _health;
    private readonly BreedingRecordRepository _breeding;
    private readonly AppSettingsRepository _settings;

    public RepositoryTests()
    {
        var opts = new DbContextOptionsBuilder<CattleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CattleDbContext(opts);
        _db.Database.EnsureCreated();

        _animals = new AnimalRepository(_db);
        _herds = new HerdRepository(_db);
        _breeds = new BreedRepository(_db);
        _farms = new FarmRepository(_db);
        _health = new HealthRecordRepository(_db);
        _breeding = new BreedingRecordRepository(_db);
        _settings = new AppSettingsRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<(int farmId, int herdId, int breedId)> SetupBasicDataAsync()
    {
        var farm = await _farms.UpsertAsync(new FarmDto { FarmName = "Test Farm" });
        var herd = await _herds.AddAsync(new HerdDto
        {
            FarmId = farm.FarmId, HerdName = "Test Herd", HerdType = "Angus", IsActive = true
        });
        var allBreeds = await _breeds.GetAllAsync();
        return (farm.FarmId, herd.HerdId, allBreeds.First().BreedId);
    }

    // --- AppSettings ---
    [Fact]
    public async Task Settings_SetAndGet_RoundTrip()
    {
        await _settings.SetAsync("TestKey", "TestValue");
        var val = await _settings.GetAsync("TestKey");
        val.Should().Be("TestValue");
    }

    [Fact]
    public async Task Settings_UpdateExisting_Overwrites()
    {
        await _settings.SetAsync("K", "V1");
        await _settings.SetAsync("K", "V2");
        var val = await _settings.GetAsync("K");
        val.Should().Be("V2");
    }

    [Fact]
    public async Task Settings_MissingKey_ReturnsNull()
    {
        var val = await _settings.GetAsync("NonExistent");
        val.Should().BeNull();
    }

    // --- Farm ---
    [Fact]
    public async Task Farm_Upsert_CreatesThenUpdates()
    {
        var farm1 = await _farms.UpsertAsync(new FarmDto { FarmName = "Farm A" });
        farm1.FarmId.Should().BeGreaterThan(0);

        var farm2 = await _farms.UpsertAsync(new FarmDto { FarmName = "Farm B" });
        farm2.FarmId.Should().Be(farm1.FarmId); // same row updated

        var fetched = await _farms.GetDefaultAsync();
        fetched!.FarmName.Should().Be("Farm B");
    }

    // --- Herds ---
    [Fact]
    public async Task Herd_AddGetUpdateDelete()
    {
        var farm = await _farms.UpsertAsync(new FarmDto { FarmName = "F" });
        var herd = await _herds.AddAsync(new HerdDto
        {
            FarmId = farm.FarmId, HerdName = "H1", HerdType = "Angus", IsActive = true
        });
        herd.HerdId.Should().BeGreaterThan(0);

        var fetched = await _herds.GetByIdAsync(herd.HerdId);
        fetched.Should().NotBeNull();
        fetched!.HerdName.Should().Be("H1");

        herd.HerdName = "H1 Updated";
        await _herds.UpdateAsync(herd);
        var updated = await _herds.GetByIdAsync(herd.HerdId);
        updated!.HerdName.Should().Be("H1 Updated");

        await _herds.DeleteAsync(herd.HerdId);
        var deleted = await _herds.GetByIdAsync(herd.HerdId);
        deleted.Should().BeNull();
    }

    // --- Animals ---
    [Fact]
    public async Task Animal_AddGetUpdateDelete()
    {
        var (_, herdId, breedId) = await SetupBasicDataAsync();

        var dto = new AnimalDto
        {
            HerdId = herdId, BreedId = breedId, BarnName = "Bessie",
            Gender = Gender.Female, Status = AnimalStatus.Healthy,
            BirthDate = DateTime.Today.AddYears(-2), CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
        };
        var added = await _animals.AddAsync(dto);
        added.AnimalId.Should().BeGreaterThan(0);
        added.BarnName.Should().Be("Bessie");

        added.BarnName = "Bessie Jr.";
        var updated = await _animals.UpdateAsync(added);
        updated.BarnName.Should().Be("Bessie Jr.");

        await _animals.DeleteAsync(updated.AnimalId);
        var deleted = await _animals.GetByIdAsync(updated.AnimalId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Animal_GetByHerd_ReturnsOnlyHerdAnimals()
    {
        var (_, herdId, breedId) = await SetupBasicDataAsync();
        var farm2 = await _farms.UpsertAsync(new FarmDto { FarmName = "F2" });
        var herd2 = await _herds.AddAsync(new HerdDto { FarmId = farm2.FarmId, HerdName = "H2", HerdType = "Angus", IsActive = true });

        await AddAnimal(herdId, breedId, "A1");
        await AddAnimal(herdId, breedId, "A2");
        await AddAnimal(herd2.HerdId, breedId, "B1");

        var herd1Animals = await _animals.GetByHerdAsync(herdId);
        herd1Animals.Should().HaveCount(2);
        herd1Animals.Should().AllSatisfy(a => a.HerdId.Should().Be(herdId));
    }

    [Fact]
    public async Task Animal_Search_FiltersByName()
    {
        var (_, herdId, breedId) = await SetupBasicDataAsync();
        await AddAnimal(herdId, breedId, "Bessie");
        await AddAnimal(herdId, breedId, "Molly");
        await AddAnimal(herdId, breedId, "Bella");

        var results = await _animals.SearchAsync(herdId, "be");
        results.Should().HaveCount(2); // Bessie + Bella
    }

    [Fact]
    public async Task Animal_BarnNameExists_DetectsConflict()
    {
        var (_, herdId, breedId) = await SetupBasicDataAsync();
        var animal = await AddAnimal(herdId, breedId, "Atlas");

        var exists = await _animals.BarnNameExistsInHerdAsync(herdId, "Atlas");
        exists.Should().BeTrue();

        var existsExcluding = await _animals.BarnNameExistsInHerdAsync(herdId, "Atlas", animal.AnimalId);
        existsExcluding.Should().BeFalse();
    }

    [Fact]
    public async Task Animal_GetOffspring_ReturnsChildren()
    {
        var (_, herdId, breedId) = await SetupBasicDataAsync();
        var sire = await AddAnimal(herdId, breedId, "Bull");
        var dam = await AddAnimal(herdId, breedId, "Cow");
        var calf1 = await AddAnimal(herdId, breedId, "Calf1", sireId: sire.AnimalId, damId: dam.AnimalId);
        var calf2 = await AddAnimal(herdId, breedId, "Calf2", sireId: sire.AnimalId, damId: dam.AnimalId);

        var sireKids = await _animals.GetOffspringAsync(sire.AnimalId);
        sireKids.Should().HaveCount(2);
    }

    // --- Health Records ---
    [Fact]
    public async Task HealthRecord_AddGetDeleteSample()
    {
        var (_, herdId, breedId) = await SetupBasicDataAsync();
        var animal = await AddAnimal(herdId, breedId, "A");

        var rec = await _health.AddAsync(new HealthRecordDto
        {
            AnimalId = animal.AnimalId, RecordDate = DateTime.Today,
            RecordType = HealthRecordType.Worming, Description = "Test worming", IsSampleData = true
        });
        rec.HealthRecordId.Should().BeGreaterThan(0);

        var records = await _health.GetByAnimalAsync(animal.AnimalId);
        records.Should().HaveCount(1);

        await _health.DeleteSampleDataAsync();
        var afterClear = await _health.GetByAnimalAsync(animal.AnimalId);
        afterClear.Should().BeEmpty();
    }

    [Fact]
    public async Task HealthRecord_Delete_RemovesSpecificRecord()
    {
        var (_, herdId, breedId) = await SetupBasicDataAsync();
        var animal = await AddAnimal(herdId, breedId, "A");
        var rec = await _health.AddAsync(new HealthRecordDto
        {
            AnimalId = animal.AnimalId, RecordDate = DateTime.Today,
            RecordType = HealthRecordType.Vaccination, Description = "Vax"
        });

        await _health.DeleteAsync(rec.HealthRecordId);
        var records = await _health.GetByAnimalAsync(animal.AnimalId);
        records.Should().BeEmpty();
    }

    // --- Breeding Records ---
    [Fact]
    public async Task BreedingRecord_AddGetDeleteSample()
    {
        var (_, herdId, breedId) = await SetupBasicDataAsync();
        var sire = await AddAnimal(herdId, breedId, "Bull");
        var dam = await AddAnimal(herdId, breedId, "Cow");

        var rec = await _breeding.AddAsync(new BreedingRecordDto
        {
            SireId = sire.AnimalId, SireBarnName = "Bull",
            DamId = dam.AnimalId, DamBarnName = "Cow",
            BreedingDate = DateTime.Today.AddDays(-100),
            ExpectedDueDate = DateTime.Today.AddDays(183),
            IsSampleData = true
        });
        rec.BreedingRecordId.Should().BeGreaterThan(0);

        var records = await _breeding.GetByAnimalAsync(sire.AnimalId);
        records.Should().HaveCount(1);

        await _breeding.DeleteSampleDataAsync();
        var afterClear = await _breeding.GetByAnimalAsync(sire.AnimalId);
        afterClear.Should().BeEmpty();
    }

    // --- Breeds ---
    [Fact]
    public async Task Breed_GetAll_ReturnsSeededBreeds()
    {
        var breeds = await _breeds.GetAllAsync();
        breeds.Should().NotBeEmpty();
        breeds.Should().Contain(b => b.BreedName == "Angus");
    }

    [Fact]
    public async Task Breed_AddCustom_AppearsInList()
    {
        var newBreed = await _breeds.AddAsync(new BreedDto { BreedName = "Custom Mix" });
        newBreed.BreedId.Should().BeGreaterThan(0);
        newBreed.IsStandardBreed.Should().BeFalse();

        var all = await _breeds.GetAllAsync();
        all.Should().Contain(b => b.BreedName == "Custom Mix");
    }

    // ── Full field round-trip ─────────────────────────────────────────────────
    //
    // MAINTENANCE: When you add a new column to the Animals table you MUST:
    //   1. Add the property to Animal.cs (entity) and AnimalDto.cs
    //   2. Map it in AnimalRepository.MapToDto AND MapToEntityExisting
    //   3. Add it to EnsureColumnsExistAsync in App.xaml.cs (for existing DBs)
    //   4. Set a non-default test value below and assert it survives the round-trip
    //
    // This test exists specifically to catch mapping omissions — fields that are
    // added to the entity but accidentally left out of MapToDto or MapToEntityExisting.
    [Fact]
    public async Task Animal_FullFieldRoundTrip_AllAttributesPersist()
    {
        var (_, herdId, breedId) = await SetupBasicDataAsync();

        // Create a second animal to use as sire so we can test SireId linkage
        var sire = await _animals.AddAsync(new AnimalDto
        {
            HerdId = herdId, BreedId = breedId, BarnName = "TestSire",
            Gender = Gender.Male, Status = AnimalStatus.Healthy,
            BirthDate = new DateTime(2018, 3, 1),
            CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
        });

        var purchaseDate = new DateTime(2021, 5, 10);
        var soldDate     = new DateTime(2024, 1, 15);
        var birthDate    = new DateTime(2020, 4, 20);

        var input = new AnimalDto
        {
            HerdId     = herdId,
            BreedId    = breedId,
            BarnName   = "FullTest",
            RegisteredName         = "RHR Full Test 001",
            RegistrationNumber     = "REG-001",
            RegistrationOrganization = "AHA",
            Gender     = Gender.Female,
            Status     = AnimalStatus.ForSale,
            Weight     = 850.5m,
            WeightUnit = WeightUnit.Pounds,
            Height     = 48.25m,
            HeightUnit = HeightUnit.Inches,
            Coloring   = "Black with white star",
            PhotoPath  = "/photos/test.jpg",
            BirthDate  = birthDate,
            DateAcquired  = purchaseDate,
            CurrentLocation = "North Pasture",
            BreedersName    = "Breeder Co.",
            CurrentOwner    = "Owner Name",
            BornOnProperty  = false,
            SellerName      = "Seller Ranch",
            SellerAddress   = "123 Ranch Rd, Guthrie, OK",
            PurchaseDate    = purchaseDate,
            PurchasePrice   = 3_500m,
            AskingPrice     = 4_200m,
            CurrentValue    = 4_000m,   // ← added v1.6; ensure this maps through
            SalePrice       = 4_100m,
            BuyerName       = "Buyer Farm",
            BuyerAddress    = "456 Farm Ln, Enid, OK",
            SoldDate        = soldDate,
            TagNumber       = "T-099",
            Chondro         = ChondroStatus.Yes,
            Horns           = false,
            IsGoodMother    = true,
            PastureLocation = "South Pen",
            PastureState    = "Oklahoma",
            ExpectedHeightAtMaturity = 54m,
            SireId          = sire.AnimalId,
            ExternalSireName = "External Bull",
            ExternalDamName  = "External Cow",
            LastWormingDate      = new DateTime(2024, 3, 1),
            LastVaccinationDate  = new DateTime(2024, 2, 15),
            LastHealthCheckDate  = new DateTime(2024, 4, 1),
            LastHoofTrimmingDate = new DateTime(2024, 1, 10),
            HealthNotes    = "Healthy overall",
            IsBreeding     = true,
            IsPregnant     = false,
            ReproductionNotes = "Calved 2023",
            MaleBreedingStatus = null,
            IsSampleData   = true,
            CreatedDate    = DateTime.UtcNow,
            ModifiedDate   = DateTime.UtcNow,
        };

        var saved = await _animals.AddAsync(input);
        var loaded = await _animals.GetByIdAsync(saved.AnimalId);

        loaded.Should().NotBeNull();
        loaded!.BarnName.Should().Be("FullTest");
        loaded.RegisteredName.Should().Be("RHR Full Test 001");
        loaded.RegistrationNumber.Should().Be("REG-001");
        loaded.RegistrationOrganization.Should().Be("AHA");
        loaded.Gender.Should().Be(Gender.Female);
        loaded.Status.Should().Be(AnimalStatus.ForSale);
        loaded.Weight.Should().Be(850.5m);
        loaded.WeightUnit.Should().Be(WeightUnit.Pounds);
        loaded.Height.Should().Be(48.25m);
        loaded.HeightUnit.Should().Be(HeightUnit.Inches);
        loaded.Coloring.Should().Be("Black with white star");
        loaded.PhotoPath.Should().Be("/photos/test.jpg");
        loaded.BirthDate.Should().Be(birthDate);
        loaded.DateAcquired.Should().Be(purchaseDate);
        loaded.CurrentLocation.Should().Be("North Pasture");
        loaded.BreedersName.Should().Be("Breeder Co.");
        loaded.CurrentOwner.Should().Be("Owner Name");
        loaded.BornOnProperty.Should().BeFalse();
        loaded.SellerName.Should().Be("Seller Ranch");
        loaded.SellerAddress.Should().Be("123 Ranch Rd, Guthrie, OK");
        loaded.PurchaseDate.Should().Be(purchaseDate);
        loaded.PurchasePrice.Should().Be(3_500m);
        loaded.AskingPrice.Should().Be(4_200m);
        loaded.CurrentValue.Should().Be(4_000m);    // ← v1.6 field
        loaded.SalePrice.Should().Be(4_100m);
        loaded.BuyerName.Should().Be("Buyer Farm");
        loaded.BuyerAddress.Should().Be("456 Farm Ln, Enid, OK");
        loaded.SoldDate.Should().Be(soldDate);
        loaded.TagNumber.Should().Be("T-099");
        loaded.Chondro.Should().Be(ChondroStatus.Yes);
        loaded.Horns.Should().BeFalse();
        loaded.IsGoodMother.Should().BeTrue();
        loaded.PastureLocation.Should().Be("South Pen");
        loaded.PastureState.Should().Be("Oklahoma");
        loaded.ExpectedHeightAtMaturity.Should().Be(54m);
        loaded.SireId.Should().Be(sire.AnimalId);
        loaded.SireBarnName.Should().Be("TestSire");
        loaded.ExternalSireName.Should().Be("External Bull");
        loaded.ExternalDamName.Should().Be("External Cow");
        loaded.LastWormingDate.Should().Be(new DateTime(2024, 3, 1));
        loaded.LastVaccinationDate.Should().Be(new DateTime(2024, 2, 15));
        loaded.LastHealthCheckDate.Should().Be(new DateTime(2024, 4, 1));
        loaded.LastHoofTrimmingDate.Should().Be(new DateTime(2024, 1, 10));
        loaded.HealthNotes.Should().Be("Healthy overall");
        loaded.IsBreeding.Should().BeTrue();
        loaded.IsPregnant.Should().BeFalse();
        loaded.ReproductionNotes.Should().Be("Calved 2023");
        loaded.IsSampleData.Should().BeTrue();
    }

    private async Task<AnimalDto> AddAnimal(int herdId, int breedId, string barnName,
        int? sireId = null, int? damId = null) =>
        await _animals.AddAsync(new AnimalDto
        {
            HerdId = herdId, BreedId = breedId, BarnName = barnName,
            Gender = Gender.Female, Status = AnimalStatus.Healthy,
            BirthDate = DateTime.Today.AddYears(-2),
            SireId = sireId, DamId = damId,
            CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
        });
}
