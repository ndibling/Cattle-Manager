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

        SeedBreeds(modelBuilder);
    }

    private static void SeedBreeds(ModelBuilder modelBuilder)
    {
        var breeds = new[]
        {
            new Breed { BreedId = 1, BreedName = "Angus", IsStandardBreed = true },
            new Breed { BreedId = 2, BreedName = "Hereford", IsStandardBreed = true },
            new Breed { BreedId = 3, BreedName = "Brahman", IsStandardBreed = true },
            new Breed { BreedId = 4, BreedName = "Charolais", IsStandardBreed = true },
            new Breed { BreedId = 5, BreedName = "Simmental", IsStandardBreed = true },
            new Breed { BreedId = 6, BreedName = "Zebu", IsStandardBreed = true },
            new Breed { BreedId = 7, BreedName = "Miniature Zebu", IsStandardBreed = true },
            new Breed { BreedId = 8, BreedName = "Longhorn", IsStandardBreed = true },
            new Breed { BreedId = 9, BreedName = "Shorthorn", IsStandardBreed = true },
            new Breed { BreedId = 10, BreedName = "Limousin", IsStandardBreed = true },
            new Breed { BreedId = 11, BreedName = "Gelbvieh", IsStandardBreed = true },
            new Breed { BreedId = 12, BreedName = "Brangus", IsStandardBreed = true },
            new Breed { BreedId = 13, BreedName = "Beefmaster", IsStandardBreed = true },
            new Breed { BreedId = 14, BreedName = "Mixed Breed", IsStandardBreed = true },
        };
        modelBuilder.Entity<Breed>().HasData(breeds);
    }
}
