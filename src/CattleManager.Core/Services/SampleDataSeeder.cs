using CattleManager.Core.Models;

namespace CattleManager.Core.Services;

public class SampleDataSeeder
{
    private readonly IAnimalRepository _animals;
    private readonly IHerdRepository _herds;
    private readonly IBreedRepository _breeds;
    private readonly IFarmRepository _farms;
    private readonly IHealthRecordRepository _healthRecords;
    private readonly IBreedingRecordRepository _breedingRecords;
    private readonly IAppSettingsRepository _settings;

    public SampleDataSeeder(
        IAnimalRepository animals, IHerdRepository herds, IBreedRepository breeds,
        IFarmRepository farms, IHealthRecordRepository healthRecords,
        IBreedingRecordRepository breedingRecords, IAppSettingsRepository settings)
    {
        _animals = animals;
        _herds = herds;
        _breeds = breeds;
        _farms = farms;
        _healthRecords = healthRecords;
        _breedingRecords = breedingRecords;
        _settings = settings;
    }

    public async Task<bool> ShouldSeedAsync()
    {
        var loaded = await _settings.GetAsync("SampleDataLoaded");
        return loaded != "true";
    }

    public async Task SeedAsync()
    {
        var farm = await _farms.UpsertAsync(new FarmDto
        {
            FarmName = "Rolling Hills Ranch",
            Address = "1234 Prairie Road, Tulsa, OK 74101",
            ContactInfo = "ranch@rollinghills.example"
        });

        var allBreeds = await _breeds.GetAllAsync();
        var angus = allBreeds.First(b => b.BreedName == "Angus");

        var herd = await _herds.AddAsync(new HerdDto
        {
            FarmId = farm.FarmId,
            HerdName = "Rolling Hills Angus",
            HerdType = "Angus",
            IsActive = true,
            IsSampleData = true
        });

        var today = DateTime.Today;

        // Great-great-grandparents (generation 4 - just external names, used in ExternalSireName/DamName)
        // Great-grandparents (generation 3)
        var ggSire = await AddAnimal(herd.HerdId, angus.BreedId, "Legend", "RHR Legend 001", Gender.Male,
            AnimalStatus.Inactive, today.AddYears(-12), null, null,
            externalSire: "Top Gun 9999", externalDam: "Prairie Queen 888");
        var ggDam = await AddAnimal(herd.HerdId, angus.BreedId, "Belle Star", "RHR Belle Star 002", Gender.Female,
            AnimalStatus.Inactive, today.AddYears(-11), null, null,
            externalSire: "Iron Duke 7777", externalDam: "Rosie 666");

        // Grandparents (generation 2)
        var gSire = await AddAnimal(herd.HerdId, angus.BreedId, "Thunder", "RHR Thunder 003", Gender.Male,
            AnimalStatus.Inactive, today.AddYears(-9), ggSire.AnimalId, ggDam.AnimalId);
        var gDam = await AddAnimal(herd.HerdId, angus.BreedId, "Daisy", "RHR Daisy 004", Gender.Female,
            AnimalStatus.Inactive, today.AddYears(-8), ggSire.AnimalId, ggDam.AnimalId);

        // Parents (generation 1)
        var parentSire = await AddAnimal(herd.HerdId, angus.BreedId, "Hercules", "RHR Hercules 005", Gender.Male,
            AnimalStatus.Inactive, today.AddYears(-7), gSire.AnimalId, gDam.AnimalId);
        var parentDam = await AddAnimal(herd.HerdId, angus.BreedId, "Clover", "RHR Clover 006", Gender.Female,
            AnimalStatus.Inactive, today.AddYears(-6), gSire.AnimalId, gDam.AnimalId);

        // THE BULL — subject animal with full 4-generation pedigree
        var bull = await AddAnimal(herd.HerdId, angus.BreedId, "Atlas", "RHR Atlas 007", Gender.Male,
            AnimalStatus.BreedingMale, today.AddYears(-4), parentSire.AnimalId, parentDam.AnimalId,
            weight: 1850, weightUnit: WeightUnit.Pounds, height: 58, heightUnit: HeightUnit.Inches,
            coloring: "Solid black, no markings", location: "Bull Pen 1",
            maleBreedingStatus: MaleBreedingStatus.Active, isBreeding: true,
            lastWorming: today.AddDays(-45), lastVaccination: today.AddMonths(-3),
            lastHealthCheck: today.AddMonths(-1),
            tagNumber: "A-007", chondro: ChondroStatus.NonCarrier, horns: false,
            pastureLocation: "Bull Pen 1", pastureState: "Oklahoma",
            expectedHeightAtMaturity: 60);

        // Breeding cows
        var cow1 = await AddAnimal(herd.HerdId, angus.BreedId, "Molly", "RHR Molly 010", Gender.Female,
            AnimalStatus.BreedingFemale, today.AddYears(-5), gSire.AnimalId, gDam.AnimalId,
            weight: 1150, weightUnit: WeightUnit.Pounds, height: 50, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Pasture A", isBreeding: true,
            lastWorming: today.AddDays(-60), lastVaccination: today.AddMonths(-2),
            lastHealthCheck: today.AddMonths(-2),
            tagNumber: "C-010", chondro: ChondroStatus.NonCarrier, horns: false,
            isGoodMother: true, pastureLocation: "Pasture A", pastureState: "Oklahoma",
            expectedHeightAtMaturity: 50);

        var cow2 = await AddAnimal(herd.HerdId, angus.BreedId, "Rosie", "RHR Rosie 011", Gender.Female,
            AnimalStatus.BreedingFemale, today.AddYears(-4), gSire.AnimalId, gDam.AnimalId,
            weight: 1100, weightUnit: WeightUnit.Pounds, height: 49, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Pasture A", isBreeding: true,
            lastWorming: today.AddDays(-30), lastVaccination: today.AddMonths(-8),
            lastHealthCheck: today.AddMonths(-3),
            tagNumber: "C-011", chondro: ChondroStatus.NonCarrier, horns: false,
            isGoodMother: true, pastureLocation: "Pasture A", pastureState: "Oklahoma",
            expectedHeightAtMaturity: 49);

        var cow3 = await AddAnimal(herd.HerdId, angus.BreedId, "Bessie", "RHR Bessie 012", Gender.Female,
            AnimalStatus.Healthy, today.AddYears(-3), null, null,
            weight: 1050, weightUnit: WeightUnit.Pounds, height: 48, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Pasture B",
            lastWorming: today.AddDays(-90), lastVaccination: today.AddMonths(-14),
            tagNumber: "C-012", chondro: ChondroStatus.NeedsTesting,
            pastureLocation: "Pasture B", pastureState: "Oklahoma", expectedHeightAtMaturity: 48);

        var cow4 = await AddAnimal(herd.HerdId, angus.BreedId, "Buttercup", "RHR Buttercup 013", Gender.Female,
            AnimalStatus.BreedingFemale, today.AddYears(-6), null, parentDam.AnimalId,
            weight: 1200, weightUnit: WeightUnit.Pounds, height: 51, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Pasture A", isBreeding: true,
            lastWorming: today.AddDays(-100), lastVaccination: today.AddMonths(-2),
            tagNumber: "C-013", chondro: ChondroStatus.NonCarrier, horns: false,
            isGoodMother: true, pastureLocation: "Pasture A", pastureState: "Oklahoma",
            expectedHeightAtMaturity: 51);

        var cow5 = await AddAnimal(herd.HerdId, angus.BreedId, "Hazel", "RHR Hazel 014", Gender.Female,
            AnimalStatus.BreedingFemale, today.AddYears(-5), null, parentDam.AnimalId,
            weight: 1120, weightUnit: WeightUnit.Pounds, height: 50, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Pasture B", isBreeding: true,
            lastWorming: today.AddDays(-110), lastVaccination: today.AddMonths(-13),
            tagNumber: "C-014", chondro: ChondroStatus.NotTested,
            pastureLocation: "Pasture B", pastureState: "Oklahoma", expectedHeightAtMaturity: 50);

        var cow6 = await AddAnimal(herd.HerdId, angus.BreedId, "Luna", "RHR Luna 015", Gender.Female,
            AnimalStatus.BreedingFemale, today.AddYears(-3), null, null,
            weight: 1000, weightUnit: WeightUnit.Pounds, height: 47, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Pasture B", isBreeding: true,
            lastWorming: today.AddDays(-85), lastVaccination: today.AddMonths(-1),
            tagNumber: "C-015", chondro: ChondroStatus.NotTested,
            pastureLocation: "Pasture B", pastureState: "Oklahoma", expectedHeightAtMaturity: 47);

        // Pregnant cows
        var breedingDate1 = today.AddDays(-120);
        var preg1 = await AddAnimal(herd.HerdId, angus.BreedId, "Stella", "RHR Stella 016", Gender.Female,
            AnimalStatus.Pregnant, today.AddYears(-4), null, null,
            weight: 1180, weightUnit: WeightUnit.Pounds, height: 50, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Maternity Pen", isBreeding: true, isPregnant: true,
            pregnancySireId: bull.AnimalId, breedingDate: breedingDate1,
            expectedDueDate: breedingDate1.AddDays(283),
            lastWorming: today.AddDays(-55), lastVaccination: today.AddMonths(-4));

        var breedingDate2 = today.AddDays(-95);
        var preg2 = await AddAnimal(herd.HerdId, angus.BreedId, "Flora", "RHR Flora 017", Gender.Female,
            AnimalStatus.Pregnant, today.AddYears(-5), null, parentDam.AnimalId,
            weight: 1160, weightUnit: WeightUnit.Pounds, height: 50, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Maternity Pen", isBreeding: true, isPregnant: true,
            pregnancySireId: bull.AnimalId, breedingDate: breedingDate2,
            expectedDueDate: breedingDate2.AddDays(283),
            lastWorming: today.AddDays(-60), lastVaccination: today.AddMonths(-5));

        // Calves
        var calf1 = await AddAnimal(herd.HerdId, angus.BreedId, "Rascal", "RHR Rascal 020", Gender.Male,
            AnimalStatus.Healthy, today.AddMonths(-4), bull.AnimalId, cow1.AnimalId,
            weight: 380, weightUnit: WeightUnit.Pounds, height: 38, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Calf Pen",
            lastWorming: today.AddDays(-30),
            tagNumber: "L-020", pastureState: "Oklahoma");

        var calf2 = await AddAnimal(herd.HerdId, angus.BreedId, "Dolly", "RHR Dolly 021", Gender.Female,
            AnimalStatus.Healthy, today.AddMonths(-5), bull.AnimalId, cow2.AnimalId,
            weight: 350, weightUnit: WeightUnit.Pounds, height: 36, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Calf Pen",
            lastWorming: today.AddDays(-40),
            tagNumber: "L-021", pastureState: "Oklahoma");

        var calf3 = await AddAnimal(herd.HerdId, angus.BreedId, "Bucky", "RHR Bucky 022", Gender.Male,
            AnimalStatus.Healthy, today.AddMonths(-3), bull.AnimalId, cow4.AnimalId,
            weight: 320, weightUnit: WeightUnit.Pounds, height: 35, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Calf Pen",
            lastWorming: today.AddDays(-20),
            tagNumber: "L-022", pastureState: "Oklahoma");

        // For Sale
        var forSale1 = await AddAnimal(herd.HerdId, angus.BreedId, "Duke", "RHR Duke 030", Gender.Male,
            AnimalStatus.ForSale, today.AddYears(-2), bull.AnimalId, cow5.AnimalId,
            weight: 950, weightUnit: WeightUnit.Pounds, height: 45, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Sale Pen",
            lastWorming: today.AddDays(-120), lastVaccination: today.AddMonths(-16),
            tagNumber: "S-030", chondro: ChondroStatus.NonCarrier, horns: false,
            pastureLocation: "Sale Pen", pastureState: "Oklahoma",
            bornOnProperty: false,
            purchaseDate: today.AddYears(-2).AddDays(45),
            purchasePrice: 2800m,
            sellerName: "Clearwater Cattle Co.",
            sellerAddress: "8901 Clearwater Rd, Guthrie, OK 73044",
            expectedHeightAtMaturity: 54);

        var forSale2 = await AddAnimal(herd.HerdId, angus.BreedId, "Penny", "RHR Penny 031", Gender.Female,
            AnimalStatus.ForSale, today.AddYears(-2), bull.AnimalId, cow6.AnimalId,
            weight: 900, weightUnit: WeightUnit.Pounds, height: 44, heightUnit: HeightUnit.Inches,
            coloring: "Solid black", location: "Sale Pen",
            lastWorming: today.AddDays(-130), lastVaccination: today.AddMonths(-15),
            tagNumber: "S-031", chondro: ChondroStatus.NonCarrier, horns: false,
            pastureLocation: "Sale Pen", pastureState: "Oklahoma",
            bornOnProperty: false,
            purchaseDate: today.AddYears(-2).AddDays(45),
            purchasePrice: 2200m,
            sellerName: "Clearwater Cattle Co.",
            sellerAddress: "8901 Clearwater Rd, Guthrie, OK 73044",
            expectedHeightAtMaturity: 50);

        // Inactive/Retired
        var retired = await AddAnimal(herd.HerdId, angus.BreedId, "Old Buck", "RHR Old Buck 040", Gender.Male,
            AnimalStatus.Inactive, today.AddYears(-10), ggSire.AnimalId, ggDam.AnimalId,
            weight: 1900, weightUnit: WeightUnit.Pounds, height: 60, heightUnit: HeightUnit.Inches,
            coloring: "Solid black, grey muzzle", location: "Retirement Pasture",
            maleBreedingStatus: MaleBreedingStatus.Retired,
            lastWorming: today.AddDays(-95), lastVaccination: today.AddMonths(-14),
            healthNotes: "Retired from breeding at age 9. Arthritis in left rear leg being managed.");

        // Add health records
        await AddHealthRecords(bull.AnimalId, today);
        await AddHealthRecords(cow1.AnimalId, today);
        await AddHealthRecords(cow2.AnimalId, today);
        await AddHealthRecords(preg1.AnimalId, today);
        await AddHealthRecords(preg2.AnimalId, today);

        // Add breeding records (historical calvings for the bull)
        await _breedingRecords.AddAsync(new BreedingRecordDto
        {
            SireId = bull.AnimalId, DamId = cow1.AnimalId,
            SireBarnName = bull.BarnName, DamBarnName = cow1.BarnName,
            BreedingDate = today.AddMonths(-17),
            ExpectedDueDate = today.AddMonths(-17).AddDays(283),
            CalvingDate = today.AddMonths(-4),
            OffspringId = calf1.AnimalId, OffspringBarnName = calf1.BarnName,
            OutcomeNotes = "Healthy bull calf, no complications.", IsSampleData = true
        });
        await _breedingRecords.AddAsync(new BreedingRecordDto
        {
            SireId = bull.AnimalId, DamId = cow2.AnimalId,
            SireBarnName = bull.BarnName, DamBarnName = cow2.BarnName,
            BreedingDate = today.AddMonths(-18),
            ExpectedDueDate = today.AddMonths(-18).AddDays(283),
            CalvingDate = today.AddMonths(-5),
            OffspringId = calf2.AnimalId, OffspringBarnName = calf2.BarnName,
            OutcomeNotes = "Healthy heifer calf, easy delivery.", IsSampleData = true
        });
        await _breedingRecords.AddAsync(new BreedingRecordDto
        {
            SireId = bull.AnimalId, DamId = cow4.AnimalId,
            SireBarnName = bull.BarnName, DamBarnName = cow4.BarnName,
            BreedingDate = today.AddMonths(-16),
            ExpectedDueDate = today.AddMonths(-16).AddDays(283),
            CalvingDate = today.AddMonths(-3),
            OffspringId = calf3.AnimalId, OffspringBarnName = calf3.BarnName,
            OutcomeNotes = "Healthy bull calf.", IsSampleData = true
        });

        await _settings.SetAsync("SampleDataLoaded", "true");
    }

    private async Task AddHealthRecords(int animalId, DateTime today)
    {
        var records = new[]
        {
            new HealthRecordDto { AnimalId = animalId, RecordDate = today.AddDays(-45),
                RecordType = HealthRecordType.Worming, Description = "Routine worming",
                TreatmentDetails = "Ivermectin pour-on 1ml/10kg", IsSampleData = true },
            new HealthRecordDto { AnimalId = animalId, RecordDate = today.AddMonths(-3),
                RecordType = HealthRecordType.Vaccination, Description = "Annual vaccinations",
                TreatmentDetails = "Clostridial 7-way + Pasturella + IBR/BVD", IsSampleData = true },
            new HealthRecordDto { AnimalId = animalId, RecordDate = today.AddMonths(-1),
                RecordType = HealthRecordType.HealthCheck, Description = "Routine health check",
                TreatmentDetails = "All vitals normal. BCS 5/9.", VeterinarianName = "Dr. Sarah Mitchell", IsSampleData = true }
        };
        foreach (var r in records)
            await _healthRecords.AddAsync(r);
    }

    private async Task<AnimalDto> AddAnimal(
        int herdId, int breedId, string barnName, string? registeredName, Gender gender,
        AnimalStatus status, DateTime birthDate,
        int? sireId, int? damId,
        decimal? weight = null, WeightUnit weightUnit = WeightUnit.Pounds,
        decimal? height = null, HeightUnit heightUnit = HeightUnit.Inches,
        string? coloring = null, string? location = null,
        bool isBreeding = false, bool isPregnant = false,
        int? pregnancySireId = null, DateTime? breedingDate = null,
        DateTime? expectedDueDate = null, string? reproductionNotes = null,
        MaleBreedingStatus? maleBreedingStatus = null,
        DateTime? lastWorming = null, DateTime? lastVaccination = null,
        DateTime? lastHealthCheck = null, string? healthNotes = null,
        string? externalSire = null, string? externalDam = null,
        string? tagNumber = null, ChondroStatus chondro = ChondroStatus.NotTested,
        bool? horns = null, bool? isGoodMother = null,
        string? pastureLocation = null, string? pastureState = null,
        decimal? expectedHeightAtMaturity = null,
        DateTime? purchaseDate = null, decimal? purchasePrice = null,
        string? sellerName = null, string? sellerAddress = null,
        bool bornOnProperty = true, decimal? askingPrice = null)
    {
        return await _animals.AddAsync(new AnimalDto
        {
            HerdId = herdId, BreedId = breedId,
            BarnName = barnName, RegisteredName = registeredName,
            Gender = gender, Status = status, BirthDate = birthDate,
            SireId = sireId, DamId = damId,
            ExternalSireName = externalSire, ExternalDamName = externalDam,
            Weight = weight, WeightUnit = weightUnit,
            Height = height, HeightUnit = heightUnit,
            Coloring = coloring, CurrentLocation = location,
            IsBreeding = isBreeding, IsPregnant = isPregnant,
            PregnancySireId = pregnancySireId, BreedingDate = breedingDate,
            ExpectedDueDate = expectedDueDate, ReproductionNotes = reproductionNotes,
            MaleBreedingStatus = maleBreedingStatus,
            LastWormingDate = lastWorming, LastVaccinationDate = lastVaccination,
            LastHealthCheckDate = lastHealthCheck, HealthNotes = healthNotes,
            TagNumber = tagNumber, Chondro = chondro, Horns = horns,
            IsGoodMother = isGoodMother, PastureLocation = pastureLocation,
            PastureState = pastureState, ExpectedHeightAtMaturity = expectedHeightAtMaturity,
            BornOnProperty = bornOnProperty,
            PurchaseDate = purchaseDate, PurchasePrice = purchasePrice,
            SellerName = sellerName, SellerAddress = sellerAddress,
            IsSampleData = true,
            CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
        });
    }

    public async Task ClearSampleDataAsync()
    {
        var all = await _animals.GetAllAsync();
        var sampleAnimals = all.Where(a => a.IsSampleData).ToList();

        await _healthRecords.DeleteSampleDataAsync();
        await _breedingRecords.DeleteSampleDataAsync();

        foreach (var animal in sampleAnimals)
            await _animals.DeleteAsync(animal.AnimalId);

        var herds = await _herds.GetAllAsync();
        foreach (var herd in herds.Where(h => h.IsSampleData))
            await _herds.DeleteAsync(herd.HerdId);

        await _settings.SetAsync("SampleDataCleared", "true");
    }
}
