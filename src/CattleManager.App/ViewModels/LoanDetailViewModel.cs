using CattleManager.App.Services;
using CattleManager.App.Views;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace CattleManager.App.ViewModels;

public sealed class AmortizationRow
{
    public int      PeriodNumber { get; init; }
    public DateTime PaymentDate  { get; init; }
    public decimal  Payment      { get; init; }
    public decimal  Principal    { get; init; }
    public decimal  Interest     { get; init; }
    public decimal  Balance      { get; init; }
    public bool     IsPaid       { get; init; }

    public string PaymentDateDisplay => PaymentDate.ToString("MM/dd/yyyy");
    public string PaymentDisplay     => Payment.ToString("C");
    public string PrincipalDisplay   => Principal.ToString("C");
    public string InterestDisplay    => Interest.ToString("C");
    public string BalanceDisplay     => Balance.ToString("C");
}

public partial class LoanDetailViewModel : ObservableObject
{
    private readonly ILoanRepository        _loans;
    private readonly ITransactionRepository _transactions;
    private readonly NavigationService      _nav;
    private readonly DialogService          _dialog;

    private LoanDto _loan = new();

    [ObservableProperty] private string  _lenderName          = string.Empty;
    [ObservableProperty] private string  _summaryLine1        = string.Empty;
    [ObservableProperty] private string  _summaryLine2        = string.Empty;
    [ObservableProperty] private string  _currentBalanceLabel = string.Empty;

    [ObservableProperty] private ObservableCollection<AmortizationRow>  _schedule  = [];
    [ObservableProperty] private ObservableCollection<LoanPaymentDto>   _payments  = [];

    // Record-payment inline form
    [ObservableProperty] private bool     _isRecordingPayment;
    [ObservableProperty] private DateTime _newPaymentDate      = DateTime.Today;
    [ObservableProperty] private string   _newPrincipalText    = string.Empty;
    [ObservableProperty] private string   _newInterestText     = string.Empty;
    [ObservableProperty] private string   _newNotesText        = string.Empty;
    [ObservableProperty] private string   _paymentError        = string.Empty;
    [ObservableProperty] private bool     _isSaving;
    [ObservableProperty] private bool     _isLoading;

    // Computed total shown in form
    public string NewTotalDisplay =>
        decimal.TryParse(NewPrincipalText, out var p) && decimal.TryParse(NewInterestText, out var i)
            ? (p + i).ToString("C") : "—";

    public LoanDetailViewModel(ILoanRepository loans, ITransactionRepository transactions,
        NavigationService nav, DialogService dialog)
    {
        _loans        = loans;
        _transactions = transactions;
        _nav          = nav;
        _dialog       = dialog;
    }

    public void Init(LoanDto loan)
    {
        _loan              = loan;
        LenderName         = loan.LenderName;
        SummaryLine1       = $"{loan.LoanTypeDisplay}  ·  " +
                             $"Principal: {loan.OriginalPrincipal:C}  ·  " +
                             $"Rate: {loan.InterestRateDisplay}  ·  " +
                             $"{loan.PaymentFrequencyDisplay} — {loan.PaymentAmount:C}/pmt";
        SummaryLine2       = $"Started: {loan.StartDate:MM/dd/yyyy}" +
                             (loan.MaturityDate.HasValue
                                 ? $"  ·  Matures: {loan.MaturityDate:MM/dd/yyyy}"
                                 : string.Empty) +
                             (loan.IsActive ? "  ·  Active" : "  ·  Paid Off");
        Schedule           = new ObservableCollection<AmortizationRow>(BuildSchedule(loan));
        RefreshBalanceLabel();
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _loans.GetPaymentsAsync(_loan.LoanId);
            Payments = new ObservableCollection<LoanPaymentDto>(
                list.OrderByDescending(p => p.PaymentDate));
            RefreshBalanceLabel();
        }
        finally { IsLoading = false; }
    }

    private void RefreshBalanceLabel()
    {
        var lastBalance = Payments.OrderByDescending(p => p.PaymentDate)
                                   .FirstOrDefault()?.RemainingBalance;
        if (lastBalance.HasValue)
        {
            CurrentBalanceLabel = $"Balance: {lastBalance.Value:C}";
            return;
        }
        // Fall back to last paid row in schedule
        var lastPaidRow = Schedule.LastOrDefault(r => r.IsPaid);
        var balance     = lastPaidRow?.Balance ?? _loan.OriginalPrincipal;
        CurrentBalanceLabel = $"Balance: {balance:C}";
    }

    private static IReadOnlyList<AmortizationRow> BuildSchedule(LoanDto loan)
    {
        int monthsPerPeriod = loan.PaymentFrequency switch
        {
            PaymentFrequency.Quarterly  => 3,
            PaymentFrequency.SemiAnnual => 6,
            PaymentFrequency.Annual     => 12,
            _                           => 1
        };
        decimal periodicRate = loan.PaymentFrequency switch
        {
            PaymentFrequency.Quarterly  => loan.InterestRate / 4m,
            PaymentFrequency.SemiAnnual => loan.InterestRate / 2m,
            PaymentFrequency.Annual     => loan.InterestRate,
            _                           => loan.InterestRate / 12m
        };

        var rows    = new List<AmortizationRow>();
        var today   = DateTime.Today;
        decimal bal = loan.OriginalPrincipal;
        var date    = loan.StartDate.AddMonths(monthsPerPeriod);

        for (int n = 1; bal > 0.005m && n <= 600; n++)
        {
            decimal interest  = Math.Round(bal * periodicRate, 2);
            decimal totalPmt  = Math.Min(loan.PaymentAmount, bal + interest);
            decimal principal = totalPmt - interest;
            bal               = Math.Max(0, bal - principal);

            rows.Add(new AmortizationRow
            {
                PeriodNumber = n,
                PaymentDate  = date,
                Payment      = totalPmt,
                Principal    = principal,
                Interest     = interest,
                Balance      = bal,
                IsPaid       = date < today
            });

            if (loan.MaturityDate.HasValue && date >= loan.MaturityDate.Value) break;
            date = date.AddMonths(monthsPerPeriod);
        }
        return rows;
    }

    [RelayCommand]
    private void ToggleRecordPayment()
    {
        if (IsRecordingPayment)
        {
            IsRecordingPayment = false;
            return;
        }
        // Pre-fill from next unpaid schedule row
        var next = Schedule.FirstOrDefault(r => !r.IsPaid);
        NewPaymentDate   = DateTime.Today;
        NewPrincipalText = (next?.Principal ?? 0m).ToString("F2");
        NewInterestText  = (next?.Interest  ?? 0m).ToString("F2");
        NewNotesText     = string.Empty;
        PaymentError     = string.Empty;
        IsRecordingPayment = true;
        OnPropertyChanged(nameof(NewTotalDisplay));
    }

    partial void OnNewPrincipalTextChanged(string value) => OnPropertyChanged(nameof(NewTotalDisplay));
    partial void OnNewInterestTextChanged(string value)  => OnPropertyChanged(nameof(NewTotalDisplay));

    [RelayCommand]
    private async Task SavePaymentAsync()
    {
        PaymentError = string.Empty;
        if (!decimal.TryParse(NewPrincipalText, out var principal) || principal < 0)
            { PaymentError = "Enter a valid principal amount."; return; }
        if (!decimal.TryParse(NewInterestText, out var interest) || interest < 0)
            { PaymentError = "Enter a valid interest amount."; return; }

        // Remaining balance = last recorded balance (or original principal) minus new principal
        var prevBalance  = Payments.OrderByDescending(p => p.PaymentDate)
                                    .FirstOrDefault()?.RemainingBalance
                           ?? _loan.OriginalPrincipal;
        var newBalance   = Math.Max(0m, prevBalance - principal);

        IsSaving = true;
        try
        {
            var dto = new LoanPaymentDto
            {
                LoanId           = _loan.LoanId,
                PaymentDate      = NewPaymentDate,
                TotalPayment     = principal + interest,
                PrincipalPortion = principal,
                InterestPortion  = interest,
                RemainingBalance = newBalance,
                Notes            = string.IsNullOrWhiteSpace(NewNotesText) ? null : NewNotesText.Trim()
            };
            await _loans.AddPaymentAsync(dto);

            // Record as an expense transaction so it appears in actuals
            await _transactions.AddAsync(new TransactionDto
            {
                TransactionType = TransactionType.Expense,
                Category        = "LoanPayments",
                Date            = NewPaymentDate,
                Amount          = principal + interest,
                Description     = $"Loan payment — {_loan.LenderName}",
                PayeePayer      = _loan.LenderName,
                Notes           = string.IsNullOrWhiteSpace(NewNotesText) ? null : NewNotesText.Trim(),
            });

            IsRecordingPayment = false;
            await LoadAsync();
        }
        catch (Exception ex) { PaymentError = $"Save failed: {ex.Message}"; }
        finally { IsSaving = false; }
    }

    [RelayCommand]
    private async Task DeletePaymentAsync(LoanPaymentDto? pmt)
    {
        if (pmt is null) return;
        if (!_dialog.Confirm(
            $"Delete the {pmt.TotalPayment:C} payment on {pmt.PaymentDate:MM/dd/yyyy}?",
            "Delete Payment"))
            return;
        await _loans.DeletePaymentAsync(pmt.PaymentId);
        Payments.Remove(pmt);
        RefreshBalanceLabel();
    }

    [RelayCommand]
    private void EditLoan()
    {
        var vm = App.Services.GetRequiredService<LoanFormViewModel>();
        vm.InitEdit(_loan);
        _nav.NavigateTo(new LoanFormPage(vm));
    }

    [RelayCommand]
    private void NavigateBack() => _nav.GoBack();
}
