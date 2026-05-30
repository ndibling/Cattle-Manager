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
