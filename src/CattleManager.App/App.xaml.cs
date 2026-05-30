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
                "Cattle Manager — Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                bool schemaOk = await DatabaseSchemaIsValidAsync(db);
                if (!schemaOk)
                {
                    Log.Warning("Existing database is missing required tables — recreating schema.");
                    await db.Database.EnsureDeletedAsync();
                    await db.Database.EnsureCreatedAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database initialization failed");
            MessageBox.Show($"Failed to initialize the database:\n{ex.Message}",
                "Cattle Manager — Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        services.AddScoped<HealthService>();
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
