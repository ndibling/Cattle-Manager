using CattleManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CattleManager.Data;

public class CattleDbContext : DbContext
{
    public CattleDbContext(DbContextOptions<CattleDbContext> options) : base(options) { }

    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<Herd> Herds => Set<Herd>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Breed> Breeds => Set<Breed>();
    public DbSet<AnimalType> AnimalTypes => Set<AnimalType>();
    public DbSet<HealthRecord> HealthRecords => Set<HealthRecord>();
    public DbSet<BreedingRecord> BreedingRecords => Set<BreedingRecord>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AnimalPhoto> AnimalPhotos => Set<AnimalPhoto>();
    public DbSet<AnimalAttachment> AnimalAttachments => Set<AnimalAttachment>();
    public DbSet<BullExposureRecord> BullExposureRecords => Set<BullExposureRecord>();

    // Financial tables
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<BudgetEntry> BudgetEntries => Set<BudgetEntry>();

    public DbSet<Pasture> Pastures => Set<Pasture>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Animal>(entity =>
        {
            entity.HasKey(a => a.AnimalId);
            entity.HasIndex(a => new { a.HerdId, a.BarnName }).IsUnique();

            entity.HasOne(a => a.Sire)
                  .WithMany(a => a.SireOffspring)
                  .HasForeignKey(a => a.SireId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.Dam)
                  .WithMany(a => a.DamOffspring)
                  .HasForeignKey(a => a.DamId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.PregnancySire)
                  .WithMany()
                  .HasForeignKey(a => a.PregnancySireId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.Herd)
                  .WithMany(h => h.Animals)
                  .HasForeignKey(a => a.HerdId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Breed)
                  .WithMany(b => b.Animals)
                  .HasForeignKey(a => a.BreedId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(a => a.Weight).HasPrecision(10, 2);
            entity.Property(a => a.Height).HasPrecision(10, 2);
            entity.Property(a => a.PurchasePrice).HasPrecision(10, 2);
            entity.Property(a => a.AskingPrice).HasPrecision(10, 2);
            entity.Property(a => a.CurrentValue).HasPrecision(10, 2);
            entity.Property(a => a.SalePrice).HasPrecision(10, 2);
        });

        modelBuilder.Entity<BreedingRecord>(entity =>
        {
            entity.HasKey(b => b.BreedingRecordId);

            entity.HasOne(b => b.Sire)
                  .WithMany(a => a.BreedingRecordsAsSire)
                  .HasForeignKey(b => b.SireId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Dam)
                  .WithMany(a => a.BreedingRecordsAsDam)
                  .HasForeignKey(b => b.DamId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Offspring)
                  .WithMany()
                  .HasForeignKey(b => b.OffspringId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(s => s.Key);
        });

        modelBuilder.Entity<AnimalPhoto>(entity =>
        {
            entity.HasKey(p => p.AnimalPhotoId);
            entity.HasOne(p => p.Animal)
                  .WithMany(a => a.Photos)
                  .HasForeignKey(p => p.AnimalId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnimalAttachment>(entity =>
        {
            entity.HasKey(a => a.AnimalAttachmentId);
            entity.HasOne(a => a.Animal)
                  .WithMany(a => a.Attachments)
                  .HasForeignKey(a => a.AnimalId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BullExposureRecord>(entity =>
        {
            entity.HasKey(e => e.ExposureRecordId);
            entity.HasOne(e => e.Dam)
                  .WithMany(a => a.BullExposuresAsDam)
                  .HasForeignKey(e => e.DamId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Sire)
                  .WithMany(a => a.BullExposuresAsSire)
                  .HasForeignKey(e => e.SireId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.TransactionId);
            entity.Property(t => t.Amount).HasPrecision(10, 2);
            entity.Property(t => t.TaxRate).HasPrecision(8, 6);
            entity.Property(t => t.TaxAmount).HasPrecision(10, 2);
            entity.HasOne(t => t.LinkedAnimal)
                  .WithMany()
                  .HasForeignKey(t => t.LinkedAnimalId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(a => a.AssetId);
            entity.Property(a => a.PurchasePrice).HasPrecision(10, 2);
            entity.Property(a => a.CurrentValue).HasPrecision(10, 2);
            entity.Property(a => a.SalvageValue).HasPrecision(10, 2);
            entity.Property(a => a.DisposalPrice).HasPrecision(10, 2);
            entity.HasOne(a => a.LinkedAnimal)
                  .WithMany()
                  .HasForeignKey(a => a.LinkedAnimalId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(l => l.LoanId);
            entity.Property(l => l.OriginalPrincipal).HasPrecision(10, 2);
            entity.Property(l => l.InterestRate).HasPrecision(8, 4);
            entity.Property(l => l.PaymentAmount).HasPrecision(10, 2);
        });

        modelBuilder.Entity<LoanPayment>(entity =>
        {
            entity.HasKey(p => p.PaymentId);
            entity.Property(p => p.TotalPayment).HasPrecision(10, 2);
            entity.Property(p => p.PrincipalPortion).HasPrecision(10, 2);
            entity.Property(p => p.InterestPortion).HasPrecision(10, 2);
            entity.Property(p => p.RemainingBalance).HasPrecision(10, 2);
            entity.HasOne(p => p.Loan)
                  .WithMany(l => l.Payments)
                  .HasForeignKey(p => p.LoanId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BudgetEntry>(entity =>
        {
            entity.HasKey(b => b.BudgetEntryId);
            entity.Property(b => b.BudgetAmount).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Herd>(entity =>
        {
            entity.Property(h => h.HerdType)
                  .HasMaxLength(100)
                  .HasDefaultValue(string.Empty);
        });

        modelBuilder.Entity<AnimalType>(entity =>
        {
            entity.HasKey(t => t.AnimalTypeId);
            entity.HasMany(t => t.Breeds)
                  .WithOne(b => b.AnimalType)
                  .HasForeignKey(b => b.AnimalTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(t => t.Herds)
                  .WithOne(h => h.AnimalType)
                  .HasForeignKey(h => h.AnimalTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnimalType>().HasData(
            new AnimalType { AnimalTypeId = 1, TypeName = "Cattle",  GroupTerm = "Herd",  IsStandardType = true },
            new AnimalType { AnimalTypeId = 2, TypeName = "Horse",   GroupTerm = "Herd",  IsStandardType = true },
            new AnimalType { AnimalTypeId = 3, TypeName = "Goat",    GroupTerm = "Herd",  IsStandardType = true },
            new AnimalType { AnimalTypeId = 4, TypeName = "Sheep",   GroupTerm = "Flock", IsStandardType = true },
            new AnimalType { AnimalTypeId = 5, TypeName = "Chicken", GroupTerm = "Flock", IsStandardType = true },
            new AnimalType { AnimalTypeId = 6, TypeName = "Duck",    GroupTerm = "Flock", IsStandardType = true },
            new AnimalType { AnimalTypeId = 7, TypeName = "Goose",   GroupTerm = "Flock", IsStandardType = true },
            new AnimalType { AnimalTypeId = 8, TypeName = "Pig",     GroupTerm = "Herd",  IsStandardType = true }
        );

        modelBuilder.Entity<Breed>().HasData(
            // Cattle (AnimalTypeId = 1)
            new Breed { BreedId = 1,  BreedName = "Angus",         IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 2,  BreedName = "Hereford",      IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 3,  BreedName = "Brahman",       IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 4,  BreedName = "Charolais",     IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 5,  BreedName = "Simmental",     IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 6,  BreedName = "Zebu",          IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 7,  BreedName = "Miniature Zebu",IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 8,  BreedName = "Longhorn",      IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 9,  BreedName = "Shorthorn",     IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 10, BreedName = "Limousin",      IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 11, BreedName = "Gelbvieh",      IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 12, BreedName = "Brangus",       IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 13, BreedName = "Beefmaster",    IsStandardBreed = true, AnimalTypeId = 1 },
            new Breed { BreedId = 14, BreedName = "Mixed Breed",   IsStandardBreed = true, AnimalTypeId = 1 },
            // Horse (AnimalTypeId = 2)
            new Breed { BreedId = 15, BreedName = "Arabian",               IsStandardBreed = true, AnimalTypeId = 2 },
            new Breed { BreedId = 16, BreedName = "Quarter Horse",         IsStandardBreed = true, AnimalTypeId = 2 },
            new Breed { BreedId = 17, BreedName = "Thoroughbred",          IsStandardBreed = true, AnimalTypeId = 2 },
            new Breed { BreedId = 18, BreedName = "Paint",                 IsStandardBreed = true, AnimalTypeId = 2 },
            new Breed { BreedId = 19, BreedName = "Appaloosa",             IsStandardBreed = true, AnimalTypeId = 2 },
            new Breed { BreedId = 20, BreedName = "Morgan",                IsStandardBreed = true, AnimalTypeId = 2 },
            new Breed { BreedId = 21, BreedName = "Tennessee Walking Horse",IsStandardBreed = true, AnimalTypeId = 2 },
            new Breed { BreedId = 22, BreedName = "Draft",                 IsStandardBreed = true, AnimalTypeId = 2 },
            new Breed { BreedId = 23, BreedName = "Mixed Breed",           IsStandardBreed = true, AnimalTypeId = 2 },
            // Goat (AnimalTypeId = 3)
            new Breed { BreedId = 24, BreedName = "Boer",        IsStandardBreed = true, AnimalTypeId = 3 },
            new Breed { BreedId = 25, BreedName = "Nubian",      IsStandardBreed = true, AnimalTypeId = 3 },
            new Breed { BreedId = 26, BreedName = "Alpine",      IsStandardBreed = true, AnimalTypeId = 3 },
            new Breed { BreedId = 27, BreedName = "Saanen",      IsStandardBreed = true, AnimalTypeId = 3 },
            new Breed { BreedId = 28, BreedName = "Kiko",        IsStandardBreed = true, AnimalTypeId = 3 },
            new Breed { BreedId = 29, BreedName = "Pygmy",       IsStandardBreed = true, AnimalTypeId = 3 },
            new Breed { BreedId = 30, BreedName = "LaMancha",    IsStandardBreed = true, AnimalTypeId = 3 },
            new Breed { BreedId = 31, BreedName = "Mixed Breed", IsStandardBreed = true, AnimalTypeId = 3 },
            // Sheep (AnimalTypeId = 4)
            new Breed { BreedId = 32, BreedName = "Merino",      IsStandardBreed = true, AnimalTypeId = 4 },
            new Breed { BreedId = 33, BreedName = "Dorset",      IsStandardBreed = true, AnimalTypeId = 4 },
            new Breed { BreedId = 34, BreedName = "Suffolk",     IsStandardBreed = true, AnimalTypeId = 4 },
            new Breed { BreedId = 35, BreedName = "Hampshire",   IsStandardBreed = true, AnimalTypeId = 4 },
            new Breed { BreedId = 36, BreedName = "Katahdin",    IsStandardBreed = true, AnimalTypeId = 4 },
            new Breed { BreedId = 37, BreedName = "Rambouillet", IsStandardBreed = true, AnimalTypeId = 4 },
            new Breed { BreedId = 38, BreedName = "Mixed Breed", IsStandardBreed = true, AnimalTypeId = 4 },
            // Chicken (AnimalTypeId = 5)
            new Breed { BreedId = 39, BreedName = "Rhode Island Red", IsStandardBreed = true, AnimalTypeId = 5 },
            new Breed { BreedId = 40, BreedName = "Leghorn",          IsStandardBreed = true, AnimalTypeId = 5 },
            new Breed { BreedId = 41, BreedName = "Plymouth Rock",    IsStandardBreed = true, AnimalTypeId = 5 },
            new Breed { BreedId = 42, BreedName = "Buff Orpington",   IsStandardBreed = true, AnimalTypeId = 5 },
            new Breed { BreedId = 43, BreedName = "Australorp",       IsStandardBreed = true, AnimalTypeId = 5 },
            new Breed { BreedId = 44, BreedName = "Silkie",           IsStandardBreed = true, AnimalTypeId = 5 },
            new Breed { BreedId = 45, BreedName = "Bantam",           IsStandardBreed = true, AnimalTypeId = 5 },
            new Breed { BreedId = 46, BreedName = "Mixed Breed",      IsStandardBreed = true, AnimalTypeId = 5 },
            // Duck (AnimalTypeId = 6)
            new Breed { BreedId = 47, BreedName = "Pekin",       IsStandardBreed = true, AnimalTypeId = 6 },
            new Breed { BreedId = 48, BreedName = "Mallard",     IsStandardBreed = true, AnimalTypeId = 6 },
            new Breed { BreedId = 49, BreedName = "Rouen",       IsStandardBreed = true, AnimalTypeId = 6 },
            new Breed { BreedId = 50, BreedName = "Muscovy",     IsStandardBreed = true, AnimalTypeId = 6 },
            new Breed { BreedId = 51, BreedName = "Cayuga",      IsStandardBreed = true, AnimalTypeId = 6 },
            new Breed { BreedId = 52, BreedName = "Mixed Breed", IsStandardBreed = true, AnimalTypeId = 6 },
            // Goose (AnimalTypeId = 7)
            new Breed { BreedId = 53, BreedName = "African",     IsStandardBreed = true, AnimalTypeId = 7 },
            new Breed { BreedId = 54, BreedName = "Chinese",     IsStandardBreed = true, AnimalTypeId = 7 },
            new Breed { BreedId = 55, BreedName = "Embden",      IsStandardBreed = true, AnimalTypeId = 7 },
            new Breed { BreedId = 56, BreedName = "Toulouse",    IsStandardBreed = true, AnimalTypeId = 7 },
            new Breed { BreedId = 57, BreedName = "Mixed Breed", IsStandardBreed = true, AnimalTypeId = 7 },
            // Pig (AnimalTypeId = 8)
            new Breed { BreedId = 58, BreedName = "Yorkshire",    IsStandardBreed = true, AnimalTypeId = 8 },
            new Breed { BreedId = 59, BreedName = "Berkshire",    IsStandardBreed = true, AnimalTypeId = 8 },
            new Breed { BreedId = 60, BreedName = "Duroc",        IsStandardBreed = true, AnimalTypeId = 8 },
            new Breed { BreedId = 61, BreedName = "Hampshire",    IsStandardBreed = true, AnimalTypeId = 8 },
            new Breed { BreedId = 62, BreedName = "Landrace",     IsStandardBreed = true, AnimalTypeId = 8 },
            new Breed { BreedId = 63, BreedName = "Chester White", IsStandardBreed = true, AnimalTypeId = 8 },
            new Breed { BreedId = 64, BreedName = "Mixed Breed",  IsStandardBreed = true, AnimalTypeId = 8 }
        );
    }
}
