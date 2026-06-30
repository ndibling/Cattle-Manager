using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CattleManager.Data.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("Farms", t => new
        {
            FarmId = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            FarmName = t.Column<string>(maxLength: 200, nullable: false),
            Address = t.Column<string>(maxLength: 500, nullable: true),
            ContactInfo = t.Column<string>(maxLength: 500, nullable: true)
        }, constraints: t => t.PrimaryKey("PK_Farms", x => x.FarmId));

        migrationBuilder.CreateTable("AnimalTypes", t => new
        {
            AnimalTypeId = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            TypeName = t.Column<string>(maxLength: 100, nullable: false),
            GroupTerm = t.Column<string>(maxLength: 50, nullable: false, defaultValue: "Herd"),
            IsStandardType = t.Column<bool>(nullable: false, defaultValue: false)
        }, constraints: t => t.PrimaryKey("PK_AnimalTypes", x => x.AnimalTypeId));

        migrationBuilder.CreateTable("Breeds", t => new
        {
            BreedId = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            BreedName = t.Column<string>(maxLength: 100, nullable: false),
            IsStandardBreed = t.Column<bool>(nullable: false),
            AnimalTypeId = t.Column<int>(nullable: false, defaultValue: 1)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_Breeds", x => x.BreedId);
            t.ForeignKey("FK_Breeds_AnimalTypes_AnimalTypeId", x => x.AnimalTypeId, "AnimalTypes", "AnimalTypeId", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateTable("AppSettings", t => new
        {
            Key = t.Column<string>(maxLength: 100, nullable: false),
            Value = t.Column<string>(nullable: false, defaultValue: "")
        }, constraints: t => t.PrimaryKey("PK_AppSettings", x => x.Key));

        migrationBuilder.CreateTable("Herds", t => new
        {
            HerdId = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            FarmId = t.Column<int>(nullable: false),
            HerdName = t.Column<string>(maxLength: 200, nullable: false),
            AnimalTypeId = t.Column<int>(nullable: false, defaultValue: 1),
            IsActive = t.Column<bool>(nullable: false, defaultValue: true),
            IsSampleData = t.Column<bool>(nullable: false, defaultValue: false),
            CreatedDate = t.Column<DateTime>(nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_Herds", x => x.HerdId);
            t.ForeignKey("FK_Herds_Farms_FarmId", x => x.FarmId, "Farms", "FarmId", onDelete: ReferentialAction.Cascade);
            t.ForeignKey("FK_Herds_AnimalTypes_AnimalTypeId", x => x.AnimalTypeId, "AnimalTypes", "AnimalTypeId", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateTable("Animals", t => new
        {
            AnimalId = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            HerdId = t.Column<int>(nullable: false),
            BarnName = t.Column<string>(maxLength: 100, nullable: false),
            RegisteredName = t.Column<string>(maxLength: 200, nullable: true),
            RegistrationNumber = t.Column<string>(maxLength: 100, nullable: true),
            RegistrationOrganization = t.Column<string>(maxLength: 200, nullable: true),
            BreedId = t.Column<int>(nullable: false),
            Gender = t.Column<int>(nullable: false),
            Status = t.Column<int>(nullable: false),
            Height = t.Column<decimal>(nullable: true),
            HeightUnit = t.Column<int>(nullable: false),
            Weight = t.Column<decimal>(nullable: true),
            WeightUnit = t.Column<int>(nullable: false),
            Coloring = t.Column<string>(maxLength: 500, nullable: true),
            PhotoPath = t.Column<string>(maxLength: 1000, nullable: true),
            BirthDate = t.Column<DateTime>(nullable: false),
            DateAcquired = t.Column<DateTime>(nullable: true),
            CurrentLocation = t.Column<string>(maxLength: 200, nullable: true),
            BreedersName = t.Column<string>(maxLength: 200, nullable: true),
            CurrentOwner = t.Column<string>(maxLength: 200, nullable: true),
            SireId = t.Column<int>(nullable: true),
            DamId = t.Column<int>(nullable: true),
            ExternalSireName = t.Column<string>(maxLength: 200, nullable: true),
            ExternalDamName = t.Column<string>(maxLength: 200, nullable: true),
            LastWormingDate = t.Column<DateTime>(nullable: true),
            LastVaccinationDate = t.Column<DateTime>(nullable: true),
            LastHealthCheckDate = t.Column<DateTime>(nullable: true),
            HealthNotes = t.Column<string>(nullable: true),
            IsBreeding = t.Column<bool>(nullable: false),
            IsPregnant = t.Column<bool>(nullable: false),
            PregnancySireId = t.Column<int>(nullable: true),
            ExpectedDueDate = t.Column<DateTime>(nullable: true),
            BreedingDate = t.Column<DateTime>(nullable: true),
            ReproductionNotes = t.Column<string>(nullable: true),
            MaleBreedingStatus = t.Column<int>(nullable: true),
            IsSampleData = t.Column<bool>(nullable: false, defaultValue: false),
            CreatedDate = t.Column<DateTime>(nullable: false),
            ModifiedDate = t.Column<DateTime>(nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_Animals", x => x.AnimalId);
            t.ForeignKey("FK_Animals_Herds_HerdId", x => x.HerdId, "Herds", "HerdId", onDelete: ReferentialAction.Cascade);
            t.ForeignKey("FK_Animals_Breeds_BreedId", x => x.BreedId, "Breeds", "BreedId", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_Animals_Animals_SireId", x => x.SireId, "Animals", "AnimalId", onDelete: ReferentialAction.SetNull);
            t.ForeignKey("FK_Animals_Animals_DamId", x => x.DamId, "Animals", "AnimalId", onDelete: ReferentialAction.SetNull);
            t.ForeignKey("FK_Animals_Animals_PregnancySireId", x => x.PregnancySireId, "Animals", "AnimalId", onDelete: ReferentialAction.SetNull);
        });

        migrationBuilder.CreateTable("HealthRecords", t => new
        {
            HealthRecordId = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            AnimalId = t.Column<int>(nullable: false),
            RecordDate = t.Column<DateTime>(nullable: false),
            RecordType = t.Column<int>(nullable: false),
            Description = t.Column<string>(maxLength: 500, nullable: false),
            TreatmentDetails = t.Column<string>(nullable: true),
            VeterinarianName = t.Column<string>(maxLength: 200, nullable: true),
            IsSampleData = t.Column<bool>(nullable: false, defaultValue: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_HealthRecords", x => x.HealthRecordId);
            t.ForeignKey("FK_HealthRecords_Animals_AnimalId", x => x.AnimalId, "Animals", "AnimalId", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("BreedingRecords", t => new
        {
            BreedingRecordId = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            SireId = t.Column<int>(nullable: false),
            DamId = t.Column<int>(nullable: false),
            BreedingDate = t.Column<DateTime>(nullable: false),
            ExpectedDueDate = t.Column<DateTime>(nullable: false),
            CalvingDate = t.Column<DateTime>(nullable: true),
            OffspringId = t.Column<int>(nullable: true),
            OutcomeNotes = t.Column<string>(nullable: true),
            IsSampleData = t.Column<bool>(nullable: false, defaultValue: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_BreedingRecords", x => x.BreedingRecordId);
            t.ForeignKey("FK_BreedingRecords_Animals_SireId", x => x.SireId, "Animals", "AnimalId", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_BreedingRecords_Animals_DamId", x => x.DamId, "Animals", "AnimalId", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_BreedingRecords_Animals_OffspringId", x => x.OffspringId, "Animals", "AnimalId", onDelete: ReferentialAction.SetNull);
        });

        // Seed animal types
        migrationBuilder.InsertData("AnimalTypes", new[] { "AnimalTypeId", "TypeName", "GroupTerm", "IsStandardType" }, new object[,]
        {
            { 1, "Cattle",  "Herd",  true },
            { 2, "Horse",   "Herd",  true },
            { 3, "Goat",    "Herd",  true },
            { 4, "Sheep",   "Flock", true },
            { 5, "Chicken", "Flock", true },
            { 6, "Duck",    "Flock", true },
            { 7, "Goose",   "Flock", true },
            { 8, "Pig",     "Herd",  true }
        });

        // Seed standard breeds per animal type
        migrationBuilder.InsertData("Breeds", new[] { "BreedId", "BreedName", "IsStandardBreed", "AnimalTypeId" }, new object[,]
        {
            // Cattle
            { 1,  "Angus",                  true, 1 }, { 2,  "Hereford",             true, 1 },
            { 3,  "Brahman",                true, 1 }, { 4,  "Charolais",            true, 1 },
            { 5,  "Simmental",              true, 1 }, { 6,  "Zebu",                 true, 1 },
            { 7,  "Miniature Zebu",         true, 1 }, { 8,  "Longhorn",             true, 1 },
            { 9,  "Shorthorn",              true, 1 }, { 10, "Limousin",             true, 1 },
            { 11, "Gelbvieh",               true, 1 }, { 12, "Brangus",              true, 1 },
            { 13, "Beefmaster",             true, 1 }, { 14, "Mixed Breed",          true, 1 },
            // Horse
            { 15, "Arabian",                true, 2 }, { 16, "Quarter Horse",        true, 2 },
            { 17, "Thoroughbred",           true, 2 }, { 18, "Paint",                true, 2 },
            { 19, "Appaloosa",              true, 2 }, { 20, "Morgan",               true, 2 },
            { 21, "Tennessee Walking Horse",true, 2 }, { 22, "Draft",                true, 2 },
            { 23, "Mixed Breed",            true, 2 },
            // Goat
            { 24, "Boer",                   true, 3 }, { 25, "Nubian",               true, 3 },
            { 26, "Alpine",                 true, 3 }, { 27, "Saanen",               true, 3 },
            { 28, "Kiko",                   true, 3 }, { 29, "Pygmy",                true, 3 },
            { 30, "LaMancha",               true, 3 }, { 31, "Mixed Breed",          true, 3 },
            // Sheep
            { 32, "Merino",                 true, 4 }, { 33, "Dorset",               true, 4 },
            { 34, "Suffolk",                true, 4 }, { 35, "Hampshire",            true, 4 },
            { 36, "Katahdin",               true, 4 }, { 37, "Rambouillet",          true, 4 },
            { 38, "Mixed Breed",            true, 4 },
            // Chicken
            { 39, "Rhode Island Red",       true, 5 }, { 40, "Leghorn",              true, 5 },
            { 41, "Plymouth Rock",          true, 5 }, { 42, "Buff Orpington",       true, 5 },
            { 43, "Australorp",             true, 5 }, { 44, "Silkie",               true, 5 },
            { 45, "Bantam",                 true, 5 }, { 46, "Mixed Breed",          true, 5 },
            // Duck
            { 47, "Pekin",                  true, 6 }, { 48, "Mallard",              true, 6 },
            { 49, "Rouen",                  true, 6 }, { 50, "Muscovy",              true, 6 },
            { 51, "Cayuga",                 true, 6 }, { 52, "Mixed Breed",          true, 6 },
            // Goose
            { 53, "African",                true, 7 }, { 54, "Chinese",              true, 7 },
            { 55, "Embden",                 true, 7 }, { 56, "Toulouse",             true, 7 },
            { 57, "Mixed Breed",            true, 7 },
            // Pig
            { 58, "Yorkshire",              true, 8 }, { 59, "Berkshire",            true, 8 },
            { 60, "Duroc",                  true, 8 }, { 61, "Hampshire",            true, 8 },
            { 62, "Landrace",               true, 8 }, { 63, "Chester White",        true, 8 },
            { 64, "Mixed Breed",            true, 8 }
        });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("BreedingRecords");
        migrationBuilder.DropTable("HealthRecords");
        migrationBuilder.DropTable("Animals");
        migrationBuilder.DropTable("Herds");
        migrationBuilder.DropTable("AppSettings");
        migrationBuilder.DropTable("Breeds");
        migrationBuilder.DropTable("AnimalTypes");
        migrationBuilder.DropTable("Farms");
    }
}
