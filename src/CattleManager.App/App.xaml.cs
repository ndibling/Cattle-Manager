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
        services.AddScoped<IFarmRepository, FarmRepository>();
        services.AddScoped<IHealthRecordRepository, HealthRecordRepository>();
        services.AddScoped<IBreedingRecordRepository, BreedingRecordRepository>();
        services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
        services.AddScoped<IAnimalPhotoRepository, AnimalPhotoRepository>();
        services.AddScoped<IAnimalAttachmentRepository, AnimalAttachmentRepository>();
        services.AddScoped<IBullExposureRepository, BullExposureRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();

        services.AddScoped<HealthService>();
        services.AddScoped<FinancialService>();
        services.AddScoped<HerdService>();
        services.AddScoped<BreedingService>();
        services.AddScoped<PedigreeService>();
        services.AddScoped<SampleDataSeeder>();
        services.AddScoped<ExportService>();

        services.AddSingleton<NavigationService>();
        services.AddSingleton<DialogService>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<HerdDetailsViewModel>();
        services.AddTransient<AnimalProfileViewModel>();
        services.AddTransient<AnimalFormViewModel>();
        services.AddTransient<PedigreeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TransactionListViewModel>();
        services.AddTransient<TransactionFormViewModel>();

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
        await conn.OpenAsync();
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
            await conn.CloseAsync();
        }
    }

    private static async Task EnsureColumnsExistAsync(CattleDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            await EnsureTableColumnsAsync(conn, "Animals", new System.Collections.Generic.Dictionary<string, string>
            {
                ["TagNumber"]                = "TEXT",
                ["Chondro"]                  = "INTEGER NOT NULL DEFAULT 0",
                ["Horns"]                    = "INTEGER",
                ["IsGoodMother"]             = "INTEGER",
                ["PastureLocation"]          = "TEXT",
                ["PastureState"]             = "TEXT",
                ["ExpectedHeightAtMaturity"] = "REAL",
            });
            await EnsureTableColumnsAsync(conn, "Transactions", new System.Collections.Generic.Dictionary<string, string>
            {
                ["TaxRate"]   = "REAL NOT NULL DEFAULT 0.0",
                ["TaxAmount"] = "REAL NOT NULL DEFAULT 0.0",
            });
        }
        finally
        {
            await conn.CloseAsync();
        }
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
