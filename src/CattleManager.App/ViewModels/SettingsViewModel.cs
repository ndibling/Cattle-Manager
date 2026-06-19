using CattleManager.App.Services;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace CattleManager.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IFarmRepository _farms;
    private readonly IHerdRepository _herds;
    private readonly IBreedRepository _breeds;
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

    public SettingsViewModel(IFarmRepository farms, IHerdRepository herds,
        IBreedRepository breeds, IAppSettingsRepository settings, DialogService dialog)
    {
        _farms = farms;
        _herds = herds;
        _breeds = breeds;
        _settings = settings;
        _dialog = dialog;
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

            StateOfOperation   = await _settings.GetAsync("StateOfOperation")   ?? string.Empty;
            StateSalesTaxRate  = await _settings.GetAsync("StateSalesTaxRate")  ?? "0";
            StateIncomeTaxRate = await _settings.GetAsync("StateIncomeTaxRate") ?? "0";
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
            await _settings.SetAsync("AccountingMethod",    AccountingMethod);
            await _settings.SetAsync("StateOfOperation",    StateOfOperation);
            await _settings.SetAsync("StateSalesTaxRate",   StateSalesTaxRate);
            await _settings.SetAsync("StateIncomeTaxRate",  StateIncomeTaxRate);
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
        File.Copy(srcPath, destPath, overwrite: true);
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
        File.Copy(srcPath, destPath, overwrite: true);
        _dialog.ShowInfo("Restore complete. Please restart Herd Master.", "Restore Complete");
    }
}
