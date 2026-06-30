using CattleManager.App.Services;
using CattleManager.App.ViewModels;
using CattleManager.Core.Services;
using CattleManager.Data;
using CattleManager.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using System.Windows;

namespace CattleManager.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CattleManager");
        Directory.CreateDirectory(dataDir);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(dataDir, "logs", "cattle-manager-.txt"),
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();

        var services = new ServiceCollection();
        ConfigureServices(services, dataDir);
        Services = services.BuildServiceProvider();

        DispatcherUnhandledException += (_, ex) =>
        {
            Log.Error(ex.Exception, "Unhandled UI exception");
            MessageBox.Show($"An unexpected error occurred:\n{ex.Exception.Message}\n\nDetails saved to log file.",
                "Herd Master — Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        // Create / verify the database schema from the current EF Core model.
        // EnsureCreatedAsync only acts when the database file does not exist.
        // If it returns false (file already existed), probe core tables and
        // delete + recreate if any are missing — handles broken databases left
        // behind by the hand-authored v1.0.0 migration files.
        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CattleDbContext>();
            bool created = await db.Database.EnsureCreatedAsync();
            if (!created)
            {
                await EnsureFinancialTablesExistAsync(db);
                await EnsurePastureTableExistsAsync(db);
                await EnsureAnimalTypesExistAsync(db);
                bool schemaOk = await DatabaseSchemaIsValidAsync(db);
                if (!schemaOk)
                {
                    Log.Warning("Existing database is missing required tables — recreating schema.");
                    await db.Database.EnsureDeletedAsync();
                    await db.Database.EnsureCreatedAsync();
                }
                else
                {
                    await EnsureColumnsExistAsync(db);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database initialization failed");
            MessageBox.Show($"Failed to initialize the database:\n{ex.Message}",
                "Herd Master — Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // Load theme from settings
        await ApplyThemeFromSettingsAsync();

        // Seed sample data on first run
        await SeedSampleDataIfNeededAsync();

        var window = Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    private static void ConfigureServices(IServiceCollection services, string dataDir)
    {
        var dbPath = Path.Combine(dataDir, "CattleManager.db");
        services.AddDbContext<CattleDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IAnimalRepository, AnimalRepository>();
        services.AddScoped<IHerdRepository, HerdRepository>();
        services.AddScoped<IBreedRepository, BreedRepository>();
        services.AddScoped<IAnimalTypeRepository, AnimalTypeRepository>();
        services.AddScoped<IFarmRepository, FarmRepository>();
        services.AddScoped<IHealthRecordRepository, HealthRecordRepository>();
        services.AddScoped<IBreedingRecordRepository, BreedingRecordRepository>();
        services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
        services.AddScoped<IAnimalPhotoRepository, AnimalPhotoRepository>();
        services.AddScoped<IAnimalAttachmentRepository, AnimalAttachmentRepository>();
        services.AddScoped<IBullExposureRepository, BullExposureRepository>();
        services.AddScoped<IPastureRepository, PastureRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();

        services.AddScoped<HealthService>();
        services.AddScoped<FinancialService>();
        services.AddScoped<HerdService>();
        services.AddScoped<ColumnConfigService>();
        services.AddScoped<BreedingService>();
        services.AddScoped<PedigreeService>();
        services.AddScoped<SampleDataSeeder>();
        services.AddScoped<ExportService>();

        services.AddSingleton<NavigationService>();
        services.AddSingleton<DialogService>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<HerdListViewModel>();
        services.AddTransient<HerdFormViewModel>();
        services.AddTransient<HerdDetailsViewModel>();
        services.AddTransient<AnimalProfileViewModel>();
        services.AddTransient<AnimalFormViewModel>();
        services.AddTransient<PedigreeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TransactionListViewModel>();
        services.AddTransient<TransactionFormViewModel>();
        services.AddTransient<AssetListViewModel>();
        services.AddTransient<AssetFormViewModel>();
        services.AddTransient<LoanListViewModel>();
        services.AddTransient<LoanFormViewModel>();
        services.AddTransient<LoanDetailViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<FinancialDashboardViewModel>();
        services.AddTransient<BudgetViewModel>();
        services.AddTransient<PastureViewViewModel>();

        services.AddSingleton<MainWindow>();
    }

    private static async Task ApplyThemeFromSettingsAsync()
    {
        try
        {
            using var scope = Services.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<IAppSettingsRepository>();
            var theme = await settings.GetAsync("Theme");
            var mode = theme == "Dark"
                ? ModernWpf.ApplicationTheme.Dark
                : ModernWpf.ApplicationTheme.Light;
            ModernWpf.ThemeManager.Current.ApplicationTheme = mode;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to apply theme");
        }
    }

    private static async Task SeedSampleDataIfNeededAsync()
    {
        try
        {
            using var scope = Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<SampleDataSeeder>();
            if (await seeder.ShouldSeedAsync())
                await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Sample data seeding failed");
        }
    }

    private static async Task EnsureFinancialTablesExistAsync(CattleDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        bool wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();
        try
        {
            var tables = new (string Name, string Ddl)[]
            {
                ("Transactions", """
                    CREATE TABLE IF NOT EXISTS "Transactions" (
                        "TransactionId" INTEGER NOT NULL CONSTRAINT "PK_Transactions" PRIMARY KEY AUTOINCREMENT,
                        "TransactionType" INTEGER NOT NULL DEFAULT 0,
                        "Category" TEXT NOT NULL DEFAULT '',
                        "Date" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00',
                        "Amount" REAL NOT NULL DEFAULT 0.0,
                        "Description" TEXT NOT NULL DEFAULT '',
                        "PayeePayer" TEXT,
                        "PaymentMethod" TEXT,
                        "Notes" TEXT,
                        "AttachmentPath" TEXT,
                        "LinkedAnimalId" INTEGER,
                        "TaxRate" REAL NOT NULL DEFAULT 0.0,
                        "TaxAmount" REAL NOT NULL DEFAULT 0.0,
                        "IsSampleData" INTEGER NOT NULL DEFAULT 0,
                        "CreatedDate" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00',
                        "ModifiedDate" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00'
                    )
                    """),
                ("Assets", """
                    CREATE TABLE IF NOT EXISTS "Assets" (
                        "AssetId" INTEGER NOT NULL CONSTRAINT "PK_Assets" PRIMARY KEY AUTOINCREMENT,
                        "AssetName" TEXT NOT NULL DEFAULT '',
                        "Category" INTEGER NOT NULL DEFAULT 0,
                        "PurchaseDate" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00',
                        "PurchasePrice" REAL NOT NULL DEFAULT 0.0,
                        "CurrentValue" REAL,
                        "DepreciationMethod" INTEGER NOT NULL DEFAULT 0,
                        "UsefulLifeYears" INTEGER NOT NULL DEFAULT 0,
                        "SalvageValue" REAL NOT NULL DEFAULT 0.0,
                        "LinkedAnimalId" INTEGER,
                        "DisposedDate" TEXT,
                        "DisposalPrice" REAL,
                        "Notes" TEXT,
                        "IsSampleData" INTEGER NOT NULL DEFAULT 0,
                        "CreatedDate" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00'
                    )
                    """),
                ("Loans", """
                    CREATE TABLE IF NOT EXISTS "Loans" (
                        "LoanId" INTEGER NOT NULL CONSTRAINT "PK_Loans" PRIMARY KEY AUTOINCREMENT,
                        "LenderName" TEXT NOT NULL DEFAULT '',
                        "LoanType" INTEGER NOT NULL DEFAULT 0,
                        "OriginalPrincipal" REAL NOT NULL DEFAULT 0.0,
                        "InterestRate" REAL NOT NULL DEFAULT 0.0,
                        "StartDate" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00',
                        "MaturityDate" TEXT,
                        "PaymentFrequency" INTEGER NOT NULL DEFAULT 0,
                        "PaymentAmount" REAL NOT NULL DEFAULT 0.0,
                        "IsActive" INTEGER NOT NULL DEFAULT 1,
                        "Notes" TEXT,
                        "IsSampleData" INTEGER NOT NULL DEFAULT 0,
                        "CreatedDate" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00'
                    )
                    """),
                ("LoanPayments", """
                    CREATE TABLE IF NOT EXISTS "LoanPayments" (
                        "PaymentId" INTEGER NOT NULL CONSTRAINT "PK_LoanPayments" PRIMARY KEY AUTOINCREMENT,
                        "LoanId" INTEGER NOT NULL,
                        "PaymentDate" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00',
                        "TotalPayment" REAL NOT NULL DEFAULT 0.0,
                        "PrincipalPortion" REAL NOT NULL DEFAULT 0.0,
                        "InterestPortion" REAL NOT NULL DEFAULT 0.0,
                        "RemainingBalance" REAL NOT NULL DEFAULT 0.0,
                        "Notes" TEXT,
                        "IsSampleData" INTEGER NOT NULL DEFAULT 0,
                        CONSTRAINT "FK_LoanPayments_Loans_LoanId" FOREIGN KEY ("LoanId") REFERENCES "Loans" ("LoanId") ON DELETE CASCADE
                    )
                    """),
                ("BudgetEntries", """
                    CREATE TABLE IF NOT EXISTS "BudgetEntries" (
                        "BudgetEntryId" INTEGER NOT NULL CONSTRAINT "PK_BudgetEntries" PRIMARY KEY AUTOINCREMENT,
                        "FiscalYear" INTEGER NOT NULL DEFAULT 0,
                        "Category" TEXT NOT NULL DEFAULT '',
                        "TransactionType" INTEGER NOT NULL DEFAULT 0,
                        "Month" INTEGER NOT NULL DEFAULT 0,
                        "BudgetAmount" REAL NOT NULL DEFAULT 0.0,
                        "IsSampleData" INTEGER NOT NULL DEFAULT 0
                    )
                    """)
            };

            foreach (var (name, ddl) in tables)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = ddl;
                await cmd.ExecuteNonQueryAsync();
                Log.Information("Verified table {Table} exists", name);
            }
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }
    }

    private static async Task EnsurePastureTableExistsAsync(CattleDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Pastures"" (
                ""PastureId""   INTEGER NOT NULL CONSTRAINT ""PK_Pastures"" PRIMARY KEY AUTOINCREMENT,
                ""HerdId""      INTEGER NOT NULL DEFAULT 0,
                ""PastureName"" TEXT    NOT NULL DEFAULT '',
                ""Address""     TEXT    NULL,
                ""State""       TEXT    NULL,
                ""Notes""       TEXT    NULL,
                ""SortOrder""   INTEGER NOT NULL DEFAULT 0
            )");
        Log.Information("Verified table Pastures exists");

        // Add columns introduced after initial release
        var conn = db.Database.GetDbConnection();
        bool wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();
        try
        {
            await EnsureTableColumnsAsync(conn, "Pastures", new System.Collections.Generic.Dictionary<string, string>
            {
                ["HerdId"]  = "INTEGER NOT NULL DEFAULT 0",
                ["Address"] = "TEXT NULL",
                ["State"]   = "TEXT NULL",
            });
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }
    }

    private static async Task EnsureAnimalTypesExistAsync(CattleDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        bool wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();
        try
        {
            // Create AnimalTypes table if missing
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    CREATE TABLE IF NOT EXISTS "AnimalTypes" (
                        "AnimalTypeId" INTEGER NOT NULL CONSTRAINT "PK_AnimalTypes" PRIMARY KEY AUTOINCREMENT,
                        "TypeName"      TEXT    NOT NULL DEFAULT '',
                        "GroupTerm"     TEXT    NOT NULL DEFAULT 'Herd',
                        "IsStandardType" INTEGER NOT NULL DEFAULT 0
                    )
                    """;
                await cmd.ExecuteNonQueryAsync();
            }
            Log.Information("Verified table AnimalTypes exists");

            // Seed standard animal types if the table is empty
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM AnimalTypes";
                var count = (long)(await cmd.ExecuteScalarAsync())!;
                if (count == 0)
                {
                    cmd.CommandText = """
                        INSERT INTO AnimalTypes (AnimalTypeId, TypeName, GroupTerm, IsStandardType) VALUES
                        (1,'Cattle','Herd',1),(2,'Horse','Herd',1),(3,'Goat','Herd',1),
                        (4,'Sheep','Flock',1),(5,'Chicken','Flock',1),(6,'Duck','Flock',1),
                        (7,'Goose','Flock',1),(8,'Pig','Herd',1)
                        """;
                    await cmd.ExecuteNonQueryAsync();
                    Log.Information("Seeded standard animal types");
                }
            }

            // Add AnimalTypeId columns to Breeds and Herds (defaulting to Cattle = 1)
            await EnsureTableColumnsAsync(conn, "Breeds", new System.Collections.Generic.Dictionary<string, string>
            {
                ["AnimalTypeId"] = "INTEGER NOT NULL DEFAULT 1"
            });
            await EnsureTableColumnsAsync(conn, "Herds", new System.Collections.Generic.Dictionary<string, string>
            {
                ["AnimalTypeId"] = "INTEGER NOT NULL DEFAULT 1"
            });

            // Seed non-cattle breeds if not present yet (INSERT OR IGNORE needs a unique constraint;
            // use NOT EXISTS instead to be safe)
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Breeds WHERE AnimalTypeId > 1";
                var nonCattleCount = (long)(await cmd.ExecuteScalarAsync())!;
                if (nonCattleCount == 0)
                {
                    cmd.CommandText = """
                        INSERT INTO Breeds (BreedName, IsStandardBreed, AnimalTypeId) VALUES
                        ('Arabian',1,2),('Quarter Horse',1,2),('Thoroughbred',1,2),('Paint',1,2),
                        ('Appaloosa',1,2),('Morgan',1,2),('Tennessee Walking Horse',1,2),('Draft',1,2),('Mixed Breed',1,2),
                        ('Boer',1,3),('Nubian',1,3),('Alpine',1,3),('Saanen',1,3),
                        ('Kiko',1,3),('Pygmy',1,3),('LaMancha',1,3),('Mixed Breed',1,3),
                        ('Merino',1,4),('Dorset',1,4),('Suffolk',1,4),('Hampshire',1,4),
                        ('Katahdin',1,4),('Rambouillet',1,4),('Mixed Breed',1,4),
                        ('Rhode Island Red',1,5),('Leghorn',1,5),('Plymouth Rock',1,5),
                        ('Buff Orpington',1,5),('Australorp',1,5),('Silkie',1,5),('Bantam',1,5),('Mixed Breed',1,5),
                        ('Pekin',1,6),('Mallard',1,6),('Rouen',1,6),('Muscovy',1,6),('Cayuga',1,6),('Mixed Breed',1,6),
                        ('African',1,7),('Chinese',1,7),('Embden',1,7),('Toulouse',1,7),('Mixed Breed',1,7),
                        ('Yorkshire',1,8),('Berkshire',1,8),('Duroc',1,8),('Hampshire',1,8),
                        ('Landrace',1,8),('Chester White',1,8),('Mixed Breed',1,8)
                        """;
                    await cmd.ExecuteNonQueryAsync();
                    Log.Information("Seeded non-cattle breeds for existing database");
                }
            }
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }
    }

    private static async Task EnsureColumnsExistAsync(CattleDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        bool wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();
        try
        {
            await EnsureTableColumnsAsync(conn, "Animals", new System.Collections.Generic.Dictionary<string, string>
            {
                ["IsForSale"]                = "INTEGER NOT NULL DEFAULT 0",
                ["TagNumber"]                = "TEXT",
                ["Chondro"]                  = "INTEGER NOT NULL DEFAULT 0",
                ["Horns"]                    = "INTEGER",
                ["IsGoodMother"]             = "INTEGER",
                ["PastureLocation"]          = "TEXT",
                ["PastureState"]             = "TEXT",
                ["ExpectedHeightAtMaturity"] = "REAL",
                ["CurrentValue"]             = "REAL",
            });
            await EnsureTableColumnsAsync(conn, "Transactions", new System.Collections.Generic.Dictionary<string, string>
            {
                ["TaxRate"]   = "REAL NOT NULL DEFAULT 0.0",
                ["TaxAmount"] = "REAL NOT NULL DEFAULT 0.0",
            });
            await MigrateRemovedStatusValuesAsync(conn);
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }
    }

    // One-time migration: remap integer status values removed in v1.8.
    // Old: BreedingFemale=1, BreedingMale=2, Weaned=4 — none exist in the new enum.
    private static async Task MigrateRemovedStatusValuesAsync(System.Data.Common.DbConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE Animals SET Status = 0, IsBreeding = 1 WHERE Status = 1;
UPDATE Animals SET Status = 0, IsBreeding = 1 WHERE Status = 2;
UPDATE Animals SET Status = 0               WHERE Status = 4;";
        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows > 0)
            Log.Information("Migrated {Count} animal status row(s) to new enum layout", rows);
    }

    private static async Task EnsureTableColumnsAsync(
        System.Data.Common.DbConnection conn, string table,
        System.Collections.Generic.Dictionary<string, string> columns)
    {
        var existing = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"PRAGMA table_info({table})";
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync()) existing.Add(rdr.GetString(1));
        }
        foreach (var (col, def) in columns)
        {
            if (!existing.Contains(col))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {col} {def}";
                await cmd.ExecuteNonQueryAsync();
                Log.Information("Added column {Table}.{Column}", table, col);
            }
        }
    }

    private static async Task<bool> DatabaseSchemaIsValidAsync(CattleDbContext db)
    {
        try
        {
            // Probe each required table; any missing table throws an exception
            _ = await db.Animals.AnyAsync();
            _ = await db.Herds.AnyAsync();
            _ = await db.Farms.AnyAsync();
            _ = await db.Breeds.AnyAsync();
            _ = await db.AnimalTypes.AnyAsync();
            _ = await db.HealthRecords.AnyAsync();
            _ = await db.BreedingRecords.AnyAsync();
            _ = await db.AppSettings.AnyAsync();
            _ = await db.AnimalPhotos.AnyAsync();
            _ = await db.AnimalAttachments.AnyAsync();
            _ = await db.BullExposureRecords.AnyAsync();
            _ = await db.Transactions.AnyAsync();
            _ = await db.Assets.AnyAsync();
            _ = await db.Loans.AnyAsync();
            _ = await db.LoanPayments.AnyAsync();
            _ = await db.BudgetEntries.AnyAsync();
            _ = await db.Pastures.AnyAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
