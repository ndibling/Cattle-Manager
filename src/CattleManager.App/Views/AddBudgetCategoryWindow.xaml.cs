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

    private readonly CheckBox[] _oneTimeChecks  = new CheckBox[12];
    private readonly TextBox[]  _oneTimeAmounts = new TextBox[12];
    private readonly TextBox[]  _recurringAmounts = new TextBox[12];

    public BudgetCategoryResult? Result { get; private set; }

    public AddBudgetCategoryWindow(IReadOnlyList<BudgetCategoryOption> available)
    {
        _available = available;
        InitializeComponent();
        BuildMonthRows();
    }

    private void BuildMonthRows()
    {
        for (int i = 0; i < 12; i++)
        {
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i + 1);

            // One-time row: checkbox (label) + amount TextBox
            var check = new CheckBox
            {
                Content = monthName,
                FontSize = 13,
                MinWidth = 130,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var otBox = new TextBox
            {
                Width = 120,
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 13,
                Text = "0.00",
                IsEnabled = false,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            check.Checked   += (_, _) => otBox.IsEnabled = true;
            check.Unchecked += (_, _) => { otBox.IsEnabled = false; otBox.Text = "0.00"; };
            _oneTimeChecks[i]  = check;
            _oneTimeAmounts[i] = otBox;

            var otRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
            otRow.Children.Add(check);
            otRow.Children.Add(otBox);
            OneTimeMonthsPanel.Children.Add(otRow);

            // Recurring row: month label + amount TextBox
            var recLabel = new TextBlock
            {
                Text = monthName,
                FontSize = 13,
                MinWidth = 130,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var recBox = new TextBox
            {
                Width = 120,
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 13,
                Text = "0.00",
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            _recurringAmounts[i] = recBox;

            var recRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
            recRow.Children.Add(recLabel);
            recRow.Children.Add(recBox);
            RecurringMonthsPanel.Children.Add(recRow);
        }
    }

    private void TypeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string type) return;
        _selectedType = type;

        // Highlight the selected type button using PrimaryButton style
        var primary = (Style)FindResource("PrimaryButton");
        ExpenseBtn.Style = type == "Expense" ? primary : null;
        IncomeBtn.Style  = type == "Income"  ? primary : null;

        // Populate category combo filtered to this type
        CategoryCombo.ItemsSource = _available.Where(c => c.Type == type).ToList();
        CategoryCombo.SelectedIndex = -1;

        // Show category section; hide all downstream sections
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

    private void RecurringAmount_Changed(object sender, TextChangedEventArgs e) { }

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
        for (int i = 0; i < 12; i++)
        {
            if (_isRecurring)
            {
                decimal.TryParse(_recurringAmounts[i].Text.Trim(), NumberStyles.Number,
                    CultureInfo.CurrentCulture, out amounts[i]);
            }
            else if (_oneTimeChecks[i].IsChecked == true)
            {
                decimal.TryParse(_oneTimeAmounts[i].Text.Trim(), NumberStyles.Number,
                    CultureInfo.CurrentCulture, out amounts[i]);
            }
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
