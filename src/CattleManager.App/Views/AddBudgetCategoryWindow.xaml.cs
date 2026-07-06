using CattleManager.App.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public record BudgetCategoryResult(
    string CategoryKey,
    string CategoryDisplay,
    string Type,
    decimal[] MonthlyAmounts   // 12 elements, index 0 = January
);

public partial class AddBudgetCategoryWindow : Window
{
    private readonly IReadOnlyList<BudgetCategoryOption> _available;
    private string? _selectedType;
    private string? _selectedCategoryKey;
    private string? _selectedCategoryDisplay;
    private bool _isRecurring;

    private readonly TextBox[] _recurringAmounts = new TextBox[12];

    public BudgetCategoryResult? Result { get; private set; }

    public AddBudgetCategoryWindow(IReadOnlyList<BudgetCategoryOption> available)
    {
        _available = available;
        InitializeComponent();
        OneTimeDatePicker.SelectedDate = DateTime.Today;
        BuildRecurringRows();
    }

    private void BuildRecurringRows()
    {
        for (int i = 0; i < 12; i++)
        {
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i + 1);

            var label = new TextBlock
            {
                Text = monthName,
                FontSize = 13,
                MinWidth = 130,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var box = new TextBox
            {
                Width = 120,
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 13,
                Text = "0.00",
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            _recurringAmounts[i] = box;

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
            row.Children.Add(label);
            row.Children.Add(box);
            RecurringMonthsPanel.Children.Add(row);
        }
    }

    private void TypeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string type) return;
        _selectedType = type;

        var primary = (Style)FindResource("PrimaryButton");
        ExpenseBtn.Style = type == "Expense" ? primary : null;
        IncomeBtn.Style  = type == "Income"  ? primary : null;

        CategoryCombo.ItemsSource   = _available.Where(c => c.Type == type).ToList();
        CategoryCombo.SelectedIndex = -1;

        CategorySection.Visibility   = Visibility.Visible;
        RecurrenceSection.Visibility = Visibility.Collapsed;
        OneTimeSection.Visibility    = Visibility.Collapsed;
        RecurringSection.Visibility  = Visibility.Collapsed;

        _selectedCategoryKey     = null;
        _selectedCategoryDisplay = null;
        _isRecurring             = false;
        OneTimeRadio.IsChecked   = false;
        RecurringRadio.IsChecked = false;

        UpdateSaveButton();
    }

    private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryCombo.SelectedItem is not BudgetCategoryOption opt) return;

        _selectedCategoryKey     = opt.Key;
        _selectedCategoryDisplay = opt.Display;

        RecurrenceSection.Visibility = Visibility.Visible;
        OneTimeSection.Visibility    = Visibility.Collapsed;
        RecurringSection.Visibility  = Visibility.Collapsed;

        _isRecurring             = false;
        OneTimeRadio.IsChecked   = false;
        RecurringRadio.IsChecked = false;

        UpdateSaveButton();
    }

    private void Recurrence_Changed(object sender, RoutedEventArgs e)
    {
        _isRecurring = RecurringRadio.IsChecked == true;

        OneTimeSection.Visibility   = _isRecurring ? Visibility.Collapsed : Visibility.Visible;
        RecurringSection.Visibility = _isRecurring ? Visibility.Visible   : Visibility.Collapsed;

        UpdateSaveButton();
    }

    private void ApplyToAll_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(RecurringAmountBox.Text.Trim(), NumberStyles.Number,
                CultureInfo.CurrentCulture, out var amt)) return;
        foreach (var box in _recurringAmounts)
            box.Text = amt.ToString("F2", CultureInfo.CurrentCulture);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCategoryKey is null || _selectedCategoryDisplay is null || _selectedType is null) return;

        var amounts = new decimal[12];

        if (_isRecurring)
        {
            for (int i = 0; i < 12; i++)
                decimal.TryParse(_recurringAmounts[i].Text.Trim(), NumberStyles.Number,
                    CultureInfo.CurrentCulture, out amounts[i]);
        }
        else
        {
            // One-time: place the amount in the month of the selected date
            var date = OneTimeDatePicker.SelectedDate ?? DateTime.Today;
            if (decimal.TryParse(OneTimeAmountBox.Text.Trim(), NumberStyles.Number,
                    CultureInfo.CurrentCulture, out var amt))
                amounts[date.Month - 1] = amt;
        }

        Result = new BudgetCategoryResult(_selectedCategoryKey, _selectedCategoryDisplay, _selectedType, amounts);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }

    private void UpdateSaveButton()
    {
        SaveBtn.IsEnabled = _selectedCategoryKey is not null
            && (OneTimeRadio.IsChecked == true || RecurringRadio.IsChecked == true);
    }
}
