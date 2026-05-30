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

        migrationBuilder.CreateTable("Breeds", t => new
        {
            BreedId = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            BreedName = t.Column<string>(maxLength: 100, nullable: false),
            IsStandardBreed = t.Column<bool>(nullable: false)
        }, constraints: t => t.PrimaryKey("PK_Breeds", x => x.BreedId));

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
            HerdType = t.Column<string>(maxLength: 100, nullable: false),
            IsActive = t.Column<bool>(nullable: false, defaultValue: true),
            IsSampleData = t.Column<bool>(nullable: false, defaultValue: false),
            CreatedDate = t.Column<DateTime>(nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_Herds", x => x.HerdId);
            t.ForeignKey("FK_Herds_Farms_FarmId", x => x.FarmId, "Farms", "FarmId", onDelete: ReferentialAction.Cascade);
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

        // Seed standard breeds
        migrationBuilder.InsertData("Breeds", new[] { "BreedId", "BreedName", "IsStandardBreed" }, new object[,]
        {
            { 1, "Angus", true }, { 2, "Hereford", true }, { 3, "Brahman", true },
            { 4, "Charolais", true }, { 5, "Simmental", true }, { 6, "Zebu", true },
            { 7, "Miniature Zebu", true }, { 8, "Longhorn", true }, { 9, "Shorthorn", true },
            { 10, "Limousin", true }, { 11, "Gelbvieh", true }, { 12, "Brangus", true },
            { 13, "Beefmaster", true }, { 14, "Mixed Breed", true }
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
        migrationBuilder.DropTable("Farms");
    }
}
