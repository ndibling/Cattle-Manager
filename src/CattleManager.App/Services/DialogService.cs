using CattleManager.App.ViewModels;
using CattleManager.App.Views;
using Microsoft.Win32;
using System.Windows;

namespace CattleManager.App.Services;

public class DialogService
{
    public AnimalIntakeResult? ShowAnimalIntake()
    {
        var win = new AnimalIntakeWindow { Owner = Application.Current.MainWindow };
        return win.ShowDialog() == true ? win.Result : null;
    }

    public decimal? PromptForDecimal(string message, string title, decimal? defaultValue = null)
    {
        var win = new InputDialogWindow(title, message, defaultValue?.ToString("F2") ?? string.Empty);
        var result = win.ShowDialog();
        if (result != true) return null;
        return decimal.TryParse(win.InputValue, out var v) ? v : null;
    }

    public bool Confirm(string message, string title = "Confirm")
    {
        var result = MessageBox.Show(message, title,
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        return result == MessageBoxResult.Yes;
    }

    public void ShowError(string message, string title = "Error") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string message, string title = "Information") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public string? OpenAnyFile(string title = "Select File")
    {
        var dlg = new OpenFileDialog { Title = title, Multiselect = false };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? OpenImageFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Animal Photo",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*",
            Multiselect = false
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? OpenDatabaseFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Backup Database File",
            Filter = "Database Files|*.db|All Files|*.*"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? SaveDatabaseFile()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Save Backup",
            Filter = "Database Files|*.db|All Files|*.*",
            FileName = $"CattleManager_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? OpenCsvFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Import CSV",
            Filter = "CSV Files|*.csv|All Files|*.*",
            Multiselect = false
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? SaveCsvFile(string defaultName)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Export to CSV",
            Filter = "CSV Files|*.csv|All Files|*.*",
            FileName = defaultName
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? SavePdfFile(string defaultName)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Export to PDF",
            Filter = "PDF Files|*.pdf|All Files|*.*",
            FileName = defaultName
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
