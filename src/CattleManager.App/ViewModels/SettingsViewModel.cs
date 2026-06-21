using CattleManager.App.Services;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CattleManager.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using System.Text;

namespace CattleManager.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IFarmRepository _farms;
    private readonly IHerdRepository _herds;
    private readonly IBreedRepository _breeds;
    private readonly IAnimalRepository _animals;
    private readonly IAppSettingsRepository _settings;
    private readonly DialogService _dialog;

    [ObservableProperty] private string _farmName = string.Empty;
    [ObservableProperty] private string? _farmAddress;
    [ObservableProperty] private string? _contactInfo;
    [ObservableProperty] private WeightUnit _weightUnit;
    [ObservableProperty] private HeightUnit _heightUnit;
    [ObservableProperty] private ThemeMode _themeMode;
    [ObservableProperty] private AutoBackupFrequency _autoBackup;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _dataStatusMessage;

    // Financial settings
    [ObservableProperty] private string _fiscalYearStartMonth = "January";
    [ObservableProperty] private string _accountingMethod = "Cash";
    [ObservableProperty] private string _stateOfOperation = string.Empty;
    [ObservableProperty] private string _stateSalesTaxRate = "0";
    [ObservableProperty] private string _stateIncomeTaxRate = "0";
    [ObservableProperty] private string _federalIncomeTaxRate = "22";

    public IReadOnlyList<WeightUnit> WeightUnitOptions { get; } = Enum.GetValues<WeightUnit>();
    public IReadOnlyList<HeightUnit> HeightUnitOptions { get; } = Enum.GetValues<HeightUnit>();
    public IReadOnlyList<ThemeMode> ThemeModeOptions { get; } = Enum.GetValues<ThemeMode>();
    public IReadOnlyList<AutoBackupFrequency> AutoBackupOptions { get; } = Enum.GetValues<AutoBackupFrequency>();
    public IReadOnlyList<string> AccountingMethodOptions { get; } = ["Cash", "Accrual"];

    private static readonly IReadOnlyList<string> MonthNames =
        ["January", "February", "March", "April", "May", "June",
         "July", "August", "September", "October", "November", "December"];
    public IReadOnlyList<string> FiscalMonthOptions { get; } = MonthNames;

    // CSV column order used for both import and export
    private static readonly string[] CsvHeaders =
    [
        "HerdName","BarnName","RegisteredName","RegistrationNumber","RegistrationOrganization",
        "BreedName","Gender","Status","BirthDate","DateAcquired","TagNumber",
        "Weight","WeightUnit","Height","HeightUnit","Coloring","BreedersName","CurrentOwner",
        "BornOnProperty","SellerName","SellerAddress","PurchaseDate","PurchasePrice",
        "IsForSale","AskingPrice","CurrentValue","SalePrice","BuyerName","BuyerAddress","SoldDate",
        "PastureLocation","PastureState","ExternalSireName","ExternalDamName",
        "LastWormingDate","LastVaccinationDate","LastHealthCheckDate","LastHoofTrimmingDate",
        "HealthNotes","IsBreeding","IsPregnant","ExpectedDueDate","BreedingDate"
    ];

    public SettingsViewModel(IFarmRepository farms, IHerdRepository herds,
        IBreedRepository breeds, IAnimalRepository animals,
        IAppSettingsRepository settings, DialogService dialog)
    {
        _farms    = farms;
        _herds    = herds;
        _breeds   = breeds;
        _animals  = animals;
        _settings = settings;
        _dialog   = dialog;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var farm = await _farms.GetDefaultAsync();
            FarmName = farm?.FarmName ?? string.Empty;
            FarmAddress = farm?.Address;
            ContactInfo = farm?.ContactInfo;

            var wu = await _settings.GetAsync("WeightUnit");
            WeightUnit = Enum.TryParse<WeightUnit>(wu, out var w) ? w : WeightUnit.Pounds;

            var hu = await _settings.GetAsync("HeightUnit");
            HeightUnit = Enum.TryParse<HeightUnit>(hu, out var h) ? h : HeightUnit.Inches;

            var theme = await _settings.GetAsync("Theme");
            ThemeMode = theme == "Dark" ? ThemeMode.Dark : ThemeMode.Light;

            var ab = await _settings.GetAsync("AutoBackup");
            AutoBackup = Enum.TryParse<AutoBackupFrequency>(ab, out var abv) ? abv : AutoBackupFrequency.Never;

            var fyStart = await _settings.GetAsync("FiscalYearStartMonth");
            FiscalYearStartMonth = int.TryParse(fyStart, out var monthNum) && monthNum >= 1 && monthNum <= 12
                ? MonthNames[monthNum - 1]
                : "January";

            var acctMethod = await _settings.GetAsync("AccountingMethod");
            AccountingMethod = acctMethod is "Cash" or "Accrual" ? acctMethod : "Cash";

            StateOfOperation     = await _settings.GetAsync("StateOfOperation")    ?? string.Empty;
            StateSalesTaxRate    = await _settings.GetAsync("StateSalesTaxRate")   ?? "0";
            StateIncomeTaxRate   = await _settings.GetAsync("StateIncomeTaxRate")  ?? "0";
            FederalIncomeTaxRate = await _settings.GetAsync("FederalIncomeTaxRate") ?? "22";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        try
        {
            await _farms.UpsertAsync(new FarmDto
            {
                FarmName = FarmName, Address = FarmAddress, ContactInfo = ContactInfo
            });
            await _settings.SetAsync("WeightUnit", WeightUnit.ToString());
            await _settings.SetAsync("HeightUnit", HeightUnit.ToString());
            await _settings.SetAsync("Theme", ThemeMode.ToString());
            await _settings.SetAsync("AutoBackup", AutoBackup.ToString());

            var monthIdx = MonthNames.ToList().IndexOf(FiscalYearStartMonth) + 1;
            await _settings.SetAsync("FiscalYearStartMonth", (monthIdx > 0 ? monthIdx : 1).ToString());
            await _settings.SetAsync("AccountingMethod",     AccountingMethod);
            await _settings.SetAsync("StateOfOperation",     StateOfOperation);
            await _settings.SetAsync("StateSalesTaxRate",    StateSalesTaxRate);
            await _settings.SetAsync("StateIncomeTaxRate",   StateIncomeTaxRate);
            await _settings.SetAsync("FederalIncomeTaxRate", FederalIncomeTaxRate);

            ModernWpf.ThemeManager.Current.ApplicationTheme =
                ThemeMode == ThemeMode.Dark
                    ? ModernWpf.ApplicationTheme.Dark
                    : ModernWpf.ApplicationTheme.Light;

            StatusMessage = "Settings saved successfully.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        var destPath = _dialog.SaveDatabaseFile();
        if (destPath is null) return;
        var srcPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CattleManager", "CattleManager.db");
        if (!File.Exists(srcPath)) { _dialog.ShowError("Database file not found."); return; }
        await Task.Run(() => File.Copy(srcPath, destPath, overwrite: true));
        StatusMessage = $"Backup saved to {destPath}";
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (!_dialog.Confirm("Restoring will replace ALL current data with the backup. Continue?", "Restore Backup"))
            return;
        var srcPath = _dialog.OpenDatabaseFile();
        if (srcPath is null) return;
        var destPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CattleManager", "CattleManager.db");
        await Task.Run(() => File.Copy(srcPath, destPath, overwrite: true));
        _dialog.ShowInfo("Restore complete. Please restart the application.", "Restore Complete");
    }

    // ── CSV Export ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ExportAnimalsCsvAsync()
    {
        var path = _dialog.SaveCsvFile($"Animals_{DateTime.Today:yyyyMMdd}");
        if (path is null) return;

        IsLoading = true;
        DataStatusMessage = null;
        try
        {
            var animals  = await _animals.GetAllAsync();
            var herdMap  = (await _herds.GetAllAsync()).ToDictionary(h => h.HerdId, h => h.HerdName);
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", CsvHeaders));

            foreach (var a in animals)
            {
                herdMap.TryGetValue(a.HerdId, out var herdName);
                sb.AppendLine(string.Join(",", new[]
                {
                    CsvField(herdName ?? string.Empty),
                    CsvField(a.BarnName),
                    CsvField(a.RegisteredName),
                    CsvField(a.RegistrationNumber),
                    CsvField(a.RegistrationOrganization),
                    CsvField(a.BreedName),
                    CsvField(a.Gender.ToString()),
                    CsvField(a.Status.ToString()),
                    CsvField(a.BirthDate.ToString("yyyy-MM-dd")),
                    CsvField(a.DateAcquired?.ToString("yyyy-MM-dd")),
                    CsvField(a.TagNumber),
                    CsvField(a.Weight?.ToString()),
                    CsvField(a.WeightUnit.ToString()),
                    CsvField(a.Height?.ToString()),
                    CsvField(a.HeightUnit.ToString()),
                    CsvField(a.Coloring),
                    CsvField(a.BreedersName),
                    CsvField(a.CurrentOwner),
                    CsvField(a.BornOnProperty.ToString()),
                    CsvField(a.SellerName),
                    CsvField(a.SellerAddress),
                    CsvField(a.PurchaseDate?.ToString("yyyy-MM-dd")),
                    CsvField(a.PurchasePrice?.ToString()),
                    CsvField(a.IsForSale.ToString()),
                    CsvField(a.AskingPrice?.ToString()),
                    CsvField(a.CurrentValue?.ToString()),
                    CsvField(a.SalePrice?.ToString()),
                    CsvField(a.BuyerName),
                    CsvField(a.BuyerAddress),
                    CsvField(a.SoldDate?.ToString("yyyy-MM-dd")),
                    CsvField(a.PastureLocation),
                    CsvField(a.PastureState),
                    CsvField(a.ExternalSireName),
                    CsvField(a.ExternalDamName),
                    CsvField(a.LastWormingDate?.ToString("yyyy-MM-dd")),
                    CsvField(a.LastVaccinationDate?.ToString("yyyy-MM-dd")),
                    CsvField(a.LastHealthCheckDate?.ToString("yyyy-MM-dd")),
                    CsvField(a.LastHoofTrimmingDate?.ToString("yyyy-MM-dd")),
                    CsvField(a.HealthNotes),
                    CsvField(a.IsBreeding.ToString()),
                    CsvField(a.IsPregnant.ToString()),
                    CsvField(a.ExpectedDueDate?.ToString("yyyy-MM-dd")),
                    CsvField(a.BreedingDate?.ToString("yyyy-MM-dd")),
                }));
            }

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
            DataStatusMessage = $"Exported {animals.Count} animals to {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "CSV export failed");
            _dialog.ShowError($"Export failed: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    // ── CSV Import ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ImportAnimalsCsvAsync()
    {
        var path = _dialog.OpenCsvFile();
        if (path is null) return;

        IsLoading = true;
        DataStatusMessage = null;
        int imported = 0, skipped = 0;
        var errors = new List<string>();

        try
        {
            var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8);
            if (lines.Length < 2) { _dialog.ShowError("CSV file is empty or has no data rows."); return; }

            var headerMap = ParseCsvRow(lines[0])
                .Select((h, i) => (h.Trim(), i))
                .ToDictionary(x => x.Item1, x => x.i, StringComparer.OrdinalIgnoreCase);

            var allHerds  = (await _herds.GetAllAsync()).ToDictionary(h => h.HerdName, StringComparer.OrdinalIgnoreCase);
            var allBreeds = (await _breeds.GetAllAsync()).ToDictionary(b => b.BreedName, StringComparer.OrdinalIgnoreCase);

            for (int rowIdx = 1; rowIdx < lines.Length; rowIdx++)
            {
                var line = lines[rowIdx].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    var fields = ParseCsvRow(line);
                    string Get(string col) => headerMap.TryGetValue(col, out var idx) && idx < fields.Count ? fields[idx] : string.Empty;

                    var herdName = Get("HerdName");
                    if (string.IsNullOrWhiteSpace(herdName)) { errors.Add($"Row {rowIdx + 1}: HerdName is required."); skipped++; continue; }
                    var barnName = Get("BarnName");
                    if (string.IsNullOrWhiteSpace(barnName)) { errors.Add($"Row {rowIdx + 1}: BarnName is required."); skipped++; continue; }

                    if (!allHerds.TryGetValue(herdName, out var herd))
                    {
                        herd = await _herds.AddAsync(new HerdDto { HerdName = herdName });
                        allHerds[herd.HerdName] = herd;
                    }

                    var breedName = Get("BreedName");
                    if (string.IsNullOrWhiteSpace(breedName)) breedName = "Unknown";
                    if (!allBreeds.TryGetValue(breedName, out var breed))
                    {
                        breed = await _breeds.AddAsync(new BreedDto { BreedName = breedName });
                        allBreeds[breed.BreedName] = breed;
                    }

                    var dto = new AnimalDto
                    {
                        HerdId                 = herd.HerdId,
                        BreedId                = breed.BreedId,
                        BreedName              = breed.BreedName,
                        BarnName               = barnName,
                        RegisteredName         = NullIfEmpty(Get("RegisteredName")),
                        RegistrationNumber     = NullIfEmpty(Get("RegistrationNumber")),
                        RegistrationOrganization = NullIfEmpty(Get("RegistrationOrganization")),
                        Gender                 = ParseEnum(Get("Gender"), Gender.Female),
                        Status                 = ParseEnum(Get("Status"), AnimalStatus.Healthy),
                        BirthDate              = ParseDate(Get("BirthDate")) ?? DateTime.Today,
                        DateAcquired           = ParseDate(Get("DateAcquired")),
                        TagNumber              = NullIfEmpty(Get("TagNumber")),
                        Weight                 = ParseDecimal(Get("Weight")),
                        WeightUnit             = ParseEnum(Get("WeightUnit"), WeightUnit.Pounds),
                        Height                 = ParseDecimal(Get("Height")),
                        HeightUnit             = ParseEnum(Get("HeightUnit"), HeightUnit.Inches),
                        Coloring               = NullIfEmpty(Get("Coloring")),
                        BreedersName           = NullIfEmpty(Get("BreedersName")),
                        CurrentOwner           = NullIfEmpty(Get("CurrentOwner")),
                        BornOnProperty         = ParseBool(Get("BornOnProperty"), true),
                        SellerName             = NullIfEmpty(Get("SellerName")),
                        SellerAddress          = NullIfEmpty(Get("SellerAddress")),
                        PurchaseDate           = ParseDate(Get("PurchaseDate")),
                        PurchasePrice          = ParseDecimal(Get("PurchasePrice")),
                        IsForSale              = ParseBool(Get("IsForSale"), false),
                        AskingPrice            = ParseDecimal(Get("AskingPrice")),
                        CurrentValue           = ParseDecimal(Get("CurrentValue")),
                        SalePrice              = ParseDecimal(Get("SalePrice")),
                        BuyerName              = NullIfEmpty(Get("BuyerName")),
                        BuyerAddress           = NullIfEmpty(Get("BuyerAddress")),
                        SoldDate               = ParseDate(Get("SoldDate")),
                        PastureLocation        = NullIfEmpty(Get("PastureLocation")),
                        PastureState           = NullIfEmpty(Get("PastureState")),
                        ExternalSireName       = NullIfEmpty(Get("ExternalSireName")),
                        ExternalDamName        = NullIfEmpty(Get("ExternalDamName")),
                        LastWormingDate        = ParseDate(Get("LastWormingDate")),
                        LastVaccinationDate    = ParseDate(Get("LastVaccinationDate")),
                        LastHealthCheckDate    = ParseDate(Get("LastHealthCheckDate")),
                        LastHoofTrimmingDate   = ParseDate(Get("LastHoofTrimmingDate")),
                        HealthNotes            = NullIfEmpty(Get("HealthNotes")),
                        IsBreeding             = ParseBool(Get("IsBreeding"), false),
                        IsPregnant             = ParseBool(Get("IsPregnant"), false),
                        ExpectedDueDate        = ParseDate(Get("ExpectedDueDate")),
                        BreedingDate           = ParseDate(Get("BreedingDate")),
                    };

                    await _animals.AddAsync(dto);
                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {rowIdx + 1}: {ex.Message}");
                    skipped++;
                }
            }

            var msg = $"Import complete: {imported} animals added, {skipped} skipped.";
            if (errors.Count > 0)
                msg += $"\n\nFirst errors:\n{string.Join("\n", errors.Take(5))}";
            DataStatusMessage = $"Imported {imported} animals ({skipped} skipped).";
            _dialog.ShowInfo(msg, "Import Complete");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "CSV import failed");
            _dialog.ShowError($"Import failed: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    // ── Clear Database ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ClearDatabaseAsync()
    {
        if (!_dialog.Confirm(
            "This will permanently delete ALL animals, herds, financial records, and health data.\n\n" +
            "Farm settings and application preferences will be kept.\n\n" +
            "This action CANNOT be undone. Are you sure?",
            "Clear All Data"))
            return;

        if (!_dialog.Confirm("Are you absolutely sure? All data will be deleted.", "Confirm Clear"))
            return;

        IsLoading = true;
        DataStatusMessage = null;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CattleDbContext>();

            await db.Database.ExecuteSqlRawAsync("DELETE FROM BullExposureRecords");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM AnimalAttachments");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM AnimalPhotos");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM HealthRecords");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM BreedingRecords");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM LoanPayments");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM Transactions");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM BudgetEntries");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM Assets");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM Loans");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM Animals");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM Herds");

            DataStatusMessage = "Database cleared successfully.";
            _dialog.ShowInfo("All data has been cleared. Farm settings have been preserved.", "Database Cleared");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Clear database failed");
            _dialog.ShowError($"Clear failed: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string CsvField(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static List<string> ParseCsvRow(string line)
    {
        var fields = new List<string>();
        int i = 0;
        while (i <= line.Length)
        {
            if (i == line.Length) { fields.Add(string.Empty); break; }
            if (line[i] == '"')
            {
                i++;
                var sb = new StringBuilder();
                while (i < line.Length)
                {
                    if (line[i] == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i += 2; }
                        else { i++; break; }
                    }
                    else { sb.Append(line[i++]); }
                }
                fields.Add(sb.ToString());
                if (i < line.Length && line[i] == ',') i++;
            }
            else
            {
                var end = line.IndexOf(',', i);
                if (end < 0) { fields.Add(line[i..]); break; }
                fields.Add(line[i..end]);
                i = end + 1;
            }
        }
        return fields;
    }

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : fallback;

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d)) return d;
        if (DateTime.TryParse(value, out var d2)) return d2;
        return null;
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, out var d) ? d : null;

    private static bool ParseBool(string? value, bool fallback) =>
        bool.TryParse(value, out var b) ? b : fallback;
}
