using CattleManager.App.Services;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CattleManager.App.ViewModels;

public partial class LoanFormViewModel : ObservableObject
{
    private readonly ILoanRepository   _loans;
    private readonly IBudgetRepository _budgets;
    private readonly NavigationService _nav;
    private readonly DialogService     _dialog;

    private int  _loanId;
    private bool _isNew;

    // Backing lists (private static, one allocation for the lifetime of the process)
    private static readonly IReadOnlyList<CategoryOption> _loanTypeOptions =
    [
        new("OperatingLineOfCredit", "Operating Line of Credit"),
        new("EquipmentLoan",         "Equipment Loan"),
        new("RealEstateLoan",        "Real Estate Loan"),
        new("Other",                 "Other"),
    ];

    private static readonly IReadOnlyList<CategoryOption> _frequencyOptions =
    [
        new("Monthly",    "Monthly"),
        new("Quarterly",  "Quarterly"),
        new("SemiAnnual", "Semi-Annual"),
        new("Annual",     "Annual"),
    ];

    // Public instance properties for XAML binding
    public IReadOnlyList<CategoryOption> LoanTypeOptions  { get; } = _loanTypeOptions;
    public IReadOnlyList<CategoryOption> FrequencyOptions { get; } = _frequencyOptions;

    public string FormTitle => _isNew ? "Add Loan" : $"Edit Loan — {LenderName}";

    [ObservableProperty] private string          _lenderName             = string.Empty;
    [ObservableProperty] private CategoryOption? _selectedLoanType;
    [ObservableProperty] private string          _originalPrincipalText  = string.Empty;
    [ObservableProperty] private string          _interestRateText       = string.Empty;
    [ObservableProperty] private DateTime        _startDate              = DateTime.Today;
    [ObservableProperty] private DateTime?       _maturityDate;
    [ObservableProperty] private CategoryOption? _selectedFrequency;
    [ObservableProperty] private string          _paymentAmountText      = string.Empty;
    [ObservableProperty] private string          _paymentDayOfMonthText  = "1";
    [ObservableProperty] private bool            _isActive               = true;
    [ObservableProperty] private string          _notes                  = string.Empty;
    [ObservableProperty] private string          _errorText              = string.Empty;
    [ObservableProperty] private bool            _isSaving;

    public LoanFormViewModel(ILoanRepository loans, IBudgetRepository budgets,
        NavigationService nav, DialogService dialog)
    {
        _loans   = loans;
        _budgets = budgets;
        _nav     = nav;
        _dialog  = dialog;
    }

    public void InitNew()
    {
        _isNew             = true;
        SelectedLoanType   = _loanTypeOptions[0];
        SelectedFrequency  = _frequencyOptions[0];
        IsActive           = true;
        OnPropertyChanged(nameof(FormTitle));
    }

    public void InitEdit(LoanDto dto)
    {
        _isNew                = false;
        _loanId               = dto.LoanId;
        LenderName            = dto.LenderName;
        OriginalPrincipalText = dto.OriginalPrincipal.ToString("F2");
        // Store rate as percent text, e.g. 0.065 → "6.5"
        InterestRateText      = (dto.InterestRate * 100m).ToString("G");
        StartDate             = dto.StartDate;
        MaturityDate          = dto.MaturityDate;
        PaymentAmountText     = dto.PaymentAmount.ToString("F2");
        PaymentDayOfMonthText = dto.PaymentDayOfMonth.ToString();
        IsActive              = dto.IsActive;
        Notes                 = dto.Notes ?? string.Empty;
        SelectedLoanType      = _loanTypeOptions.FirstOrDefault(o => o.Key == dto.LoanType.ToString())
                                ?? _loanTypeOptions[0];
        SelectedFrequency     = _frequencyOptions.FirstOrDefault(o => o.Key == dto.PaymentFrequency.ToString())
                                ?? _frequencyOptions[0];
        OnPropertyChanged(nameof(FormTitle));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorText = string.Empty;

        if (string.IsNullOrWhiteSpace(LenderName))
            { ErrorText = "Lender name is required."; return; }
        if (SelectedLoanType is null)
            { ErrorText = "Select a loan type."; return; }
        if (!decimal.TryParse(OriginalPrincipalText, out var principal) || principal <= 0)
            { ErrorText = "Enter a valid principal amount greater than zero."; return; }
        if (!decimal.TryParse(InterestRateText, out var ratePercent) || ratePercent < 0)
            { ErrorText = "Enter a valid interest rate (e.g. 6.5 for 6.5%)."; return; }
        if (SelectedFrequency is null)
            { ErrorText = "Select a payment frequency."; return; }
        if (!decimal.TryParse(PaymentAmountText, out var payment) || payment <= 0)
            { ErrorText = "Enter a valid payment amount greater than zero."; return; }
        if (!int.TryParse(PaymentDayOfMonthText, out var payDay) || payDay < 1 || payDay > 31)
            { ErrorText = "Payment day of month must be between 1 and 31."; return; }

        IsSaving = true;
        try
        {
            var dto = new LoanDto
            {
                LoanId            = _loanId,
                LenderName        = LenderName.Trim(),
                LoanType          = Enum.Parse<LoanType>(SelectedLoanType.Key),
                OriginalPrincipal = principal,
                InterestRate      = ratePercent / 100m,
                StartDate         = StartDate,
                MaturityDate      = MaturityDate,
                PaymentFrequency  = Enum.Parse<PaymentFrequency>(SelectedFrequency.Key),
                PaymentAmount     = payment,
                PaymentDayOfMonth = payDay,
                IsActive          = IsActive,
                Notes             = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            };

            if (_isNew)
            {
                await _loans.AddAsync(dto);
                await AddBudgetEntriesForLoanAsync(dto);
            }
            else
            {
                await _loans.UpdateAsync(dto);
            }

            _nav.GoBack();
        }
        catch (Exception ex) { ErrorText = $"Save failed: {ex.Message}"; }
        finally { IsSaving = false; }
    }

    private async Task AddBudgetEntriesForLoanAsync(LoanDto loan)
    {
        var maxDate = loan.MaturityDate ?? loan.StartDate.AddYears(5);
        var paymentDates = ComputePaymentDates(loan, loan.StartDate, maxDate);

        // Group by fiscal year and upsert budget entries, accumulating with any existing amounts
        var byYear = paymentDates.GroupBy(d => d.Year);
        foreach (var yearGroup in byYear)
        {
            int year = yearGroup.Key;
            var existing = await _budgets.GetByFiscalYearAsync(year);
            var loanEntries = existing
                .Where(e => e.Category == "LoanPayments")
                .ToDictionary(e => e.Month);

            foreach (var date in yearGroup)
            {
                loanEntries.TryGetValue(date.Month, out var prev);
                await _budgets.UpsertAsync(new BudgetEntryDto
                {
                    BudgetEntryId   = prev?.BudgetEntryId ?? 0,
                    FiscalYear      = year,
                    Category        = "LoanPayments",
                    TransactionType = TransactionType.Expense,
                    Month           = date.Month,
                    BudgetAmount    = (prev?.BudgetAmount ?? 0m) + loan.PaymentAmount,
                });
            }
        }
    }

    private static IReadOnlyList<DateTime> ComputePaymentDates(LoanDto loan, DateTime from, DateTime maxDate)
    {
        int monthsPerPeriod = loan.PaymentFrequency switch
        {
            PaymentFrequency.Quarterly  => 3,
            PaymentFrequency.SemiAnnual => 6,
            PaymentFrequency.Annual     => 12,
            _                           => 1,
        };

        var dates = new List<DateTime>();
        var date = loan.StartDate.AddMonths(monthsPerPeriod);
        while (date <= maxDate)
        {
            if (date >= from)
                dates.Add(date);
            date = date.AddMonths(monthsPerPeriod);
        }
        return dates;
    }

    [RelayCommand]
    private void Cancel() => _nav.GoBack();
}
