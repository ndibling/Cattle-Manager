using CattleManager.Core.Models;

namespace CattleManager.Core.Services;

public class FinancialService
{
    private readonly ITransactionRepository _transactions;
    private readonly IAssetRepository _assets;
    private readonly ILoanRepository _loans;
    private readonly IBudgetRepository _budget;
    private readonly IAnimalRepository _animals;
    private readonly IAppSettingsRepository _settings;

    private static readonly Dictionary<string, string> ScheduleFMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LivestockSales"]        = "Line 1a — Sales of livestock and other resale items",
        ["BreedingServices"]      = "Line 2 — Sales of livestock, produce, grains, and other products",
        ["HayCropSales"]          = "Line 2 — Sales of livestock, produce, grains, and other products",
        ["GovernmentPayments"]    = "Line 4a — Agricultural program payments",
        ["InsuranceProceeds"]     = "Line 6a — Crop insurance proceeds",
        ["CustomWork"]            = "Line 8 — Other farm income",
        ["MiscellaneousIncome"]   = "Line 8 — Other farm income",
        ["FeedHay"]               = "Line 10 — Feed purchased",
        ["TruckingTransportation"]= "Line 12 — Freight and trucking",
        ["FuelOil"]               = "Line 14 — Gasoline, fuel, and oil",
        ["Insurance"]             = "Line 16 — Insurance (other than health)",
        ["InterestExpense"]       = "Line 17b — Interest — Other",
        ["LaborContractWork"]     = "Line 18 — Labor hired",
        ["RepairsMaintenance"]    = "Line 21 — Repairs and maintenance",
        ["PropertyTaxes"]         = "Line 23 — Taxes",
        ["Utilities"]             = "Line 24 — Utilities",
        ["VeterinaryMedical"]     = "Line 25 — Veterinary, breeding, and medicine",
        ["BreedingFees"]          = "Line 25 — Veterinary, breeding, and medicine",
        ["MarketingAuction"]      = "Line 26 — Other expenses",
        ["SuppliesMiscellaneous"] = "Line 26 — Other expenses",
    };

    public FinancialService(
        ITransactionRepository transactions,
        IAssetRepository assets,
        ILoanRepository loans,
        IBudgetRepository budget,
        IAnimalRepository animals,
        IAppSettingsRepository settings)
    {
        _transactions = transactions;
        _assets = assets;
        _loans = loans;
        _budget = budget;
        _animals = animals;
        _settings = settings;
    }

    // --- Pure math helpers ---

    public decimal CalculateAnnualDepreciation(AssetDto asset, int year)
    {
        if (asset.Category == AssetCategory.Livestock) return 0m;
        if (year < 1 || asset.UsefulLifeYears <= 0) return 0m;

        var depreciableBase = asset.PurchasePrice - asset.SalvageValue;
        if (depreciableBase <= 0) return 0m;

        return asset.DepreciationMethod switch
        {
            DepreciationMethod.StraightLine when year > asset.UsefulLifeYears => 0m,
            DepreciationMethod.StraightLine => depreciableBase / asset.UsefulLifeYears,
            DepreciationMethod.DB150 => CalculateDb150(asset, year),
            DepreciationMethod.Section179 => year == 1 ? asset.PurchasePrice : 0m,
            _ => 0m
        };
    }

    private decimal CalculateDb150(AssetDto asset, int year)
    {
        var rate = 1.5m / asset.UsefulLifeYears;
        var bookValue = asset.PurchasePrice;

        for (int y = 1; y < year; y++)
        {
            if (bookValue <= asset.SalvageValue) return 0m;
            var remainingYears = asset.UsefulLifeYears - y + 1;
            var db = bookValue * rate;
            var sl = (bookValue - asset.SalvageValue) / remainingYears;
            bookValue -= Math.Max(db, sl);
        }

        if (bookValue <= asset.SalvageValue) return 0m;
        var finalRemaining = asset.UsefulLifeYears - year + 1;
        if (finalRemaining <= 0) return 0m;
        var finalDb = bookValue * rate;
        var finalSl = (bookValue - asset.SalvageValue) / finalRemaining;
        return Math.Min(Math.Max(finalDb, finalSl), bookValue - asset.SalvageValue);
    }

    public decimal CalculateLoanBalance(LoanDto loan, DateTime asOf)
    {
        if (asOf <= loan.StartDate) return loan.OriginalPrincipal;

        var periodsPerYear = loan.PaymentFrequency switch
        {
            PaymentFrequency.Monthly    => 12,
            PaymentFrequency.Quarterly  => 4,
            PaymentFrequency.SemiAnnual => 2,
            PaymentFrequency.Annual     => 1,
            _ => 12
        };

        var monthsElapsed = ((asOf.Year - loan.StartDate.Year) * 12) + (asOf.Month - loan.StartDate.Month);
        var periodsElapsed = (int)(monthsElapsed * periodsPerYear / 12.0);
        if (periodsElapsed <= 0) return loan.OriginalPrincipal;

        var periodicRate = loan.InterestRate / periodsPerYear;
        if (periodicRate == 0)
            return Math.Max(0, loan.OriginalPrincipal - loan.PaymentAmount * periodsElapsed);

        // Standard amortization: B_n = P*(1+r)^n - PMT*((1+r)^n - 1)/r
        var factor = (decimal)Math.Pow((double)(1 + periodicRate), periodsElapsed);
        var balance = loan.OriginalPrincipal * factor - loan.PaymentAmount * (factor - 1) / periodicRate;
        return Math.Max(0, balance);
    }

    // --- Report builders ---

    public async Task<ProfitAndLossDto> GetProfitAndLossAsync(DateTime from, DateTime to)
    {
        var transactions = await _transactions.GetByDateRangeAsync(from, to);
        var loanPayments = await _loans.GetAllPaymentsInRangeAsync(from, to);
        var activeAssets = await _assets.GetActiveAsync();
        var fiscalYear = from.Year;

        var incomeItems = transactions
            .Where(t => t.TransactionType == TransactionType.Income)
            .GroupBy(t => t.Category)
            .Select(g => new CategoryLineItem { Category = g.Key, Amount = g.Sum(t => t.Amount) })
            .OrderBy(c => c.Category)
            .ToList();

        var expenseItems = transactions
            .Where(t => t.TransactionType == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .Select(g => new CategoryLineItem { Category = g.Key, Amount = g.Sum(t => t.Amount) })
            .OrderBy(c => c.Category)
            .ToList();

        var grossRevenue = incomeItems.Sum(c => c.Amount);
        var totalExpenses = expenseItems.Sum(c => c.Amount);
        var interestExpense = loanPayments.Sum(p => p.InterestPortion);

        // Prorate annual depreciation by period fraction
        var periodDays = Math.Max(1, (to - from).TotalDays);
        var depreciationTotal = 0m;
        foreach (var asset in activeAssets)
        {
            var ownershipYear = Math.Max(1, fiscalYear - asset.PurchaseDate.Year + 1);
            var annual = CalculateAnnualDepreciation(asset, ownershipYear);
            depreciationTotal += annual * (decimal)(periodDays / 365.25);
        }

        // Budget comparison
        var budgetEntries = await _budget.GetByFiscalYearAsync(fiscalYear);
        var incomeBudget = BuildBudgetComparison(incomeItems, budgetEntries, TransactionType.Income);
        var expenseBudget = BuildBudgetComparison(expenseItems, budgetEntries, TransactionType.Expense);

        return new ProfitAndLossDto
        {
            PeriodFrom = from,
            PeriodTo = to,
            IncomeByCategory = incomeItems,
            ExpensesByCategory = expenseItems,
            GrossRevenue = grossRevenue,
            TotalExpenses = totalExpenses,
            InterestExpense = interestExpense,
            DepreciationTotal = depreciationTotal,
            NetFarmIncome = grossRevenue - totalExpenses - interestExpense - depreciationTotal,
            IncomeBudgetComparison = incomeBudget,
            ExpenseBudgetComparison = expenseBudget
        };
    }

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(DateTime asOf)
    {
        var allTransactions = await _transactions.GetByDateRangeAsync(DateTime.MinValue, asOf);
        var allAssets = await _assets.GetAllAsync();
        var activeLoans = await _loans.GetActiveAsync();
        var allAnimals = await _animals.GetAllAsync();

        // Approximate cash: cumulative income - expenses - principal payments
        var allPayments = await _loans.GetAllPaymentsInRangeAsync(DateTime.MinValue, asOf);
        var cash = allTransactions
            .Where(t => t.TransactionType == TransactionType.Income || t.TransactionType == TransactionType.CapitalInflux)
            .Sum(t => t.Amount)
            - allTransactions.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount)
            - allPayments.Sum(p => p.TotalPayment);

        // Livestock inventory: use asset register current/purchase values for livestock assets;
        // fall back to summing animal purchase prices when no livestock assets have been registered
        var livestockAssets = allAssets.Where(a => a.Category == AssetCategory.Livestock && a.IsActive).ToList();
        var livestockValue = livestockAssets.Count > 0
            ? livestockAssets.Sum(a => a.CurrentValue ?? a.PurchasePrice)
            : allAnimals
                .Where(a => a.Status != AnimalStatus.Sold && a.Status != AnimalStatus.Deceased)
                .Sum(a => a.CurrentValue ?? a.PurchasePrice ?? 0m);

        // Fixed assets with accumulated depreciation
        var assetLines = new List<AssetLineItem>();
        foreach (var asset in allAssets.Where(a => a.Category != AssetCategory.Livestock))
        {
            var yearsOwned = Math.Max(0, asOf.Year - asset.PurchaseDate.Year);
            var accumulated = 0m;
            for (int y = 1; y <= yearsOwned; y++)
                accumulated += CalculateAnnualDepreciation(asset, y);
            accumulated = Math.Min(accumulated, asset.PurchasePrice - asset.SalvageValue);
            assetLines.Add(new AssetLineItem
            {
                Name = asset.AssetName,
                CostBasis = asset.PurchasePrice,
                AccumulatedDepreciation = accumulated
            });
        }

        var totalFixedGross = assetLines.Sum(a => a.CostBasis);
        var totalAccumDepr = assetLines.Sum(a => a.AccumulatedDepreciation);
        var totalFixedNet = totalFixedGross - totalAccumDepr;
        var totalCurrentAssets = Math.Max(0, cash) + livestockValue;
        var totalAssets = totalCurrentAssets + totalFixedNet;

        // Liabilities
        var operatingLineBalance = 0m;
        var longTermLoans = new List<LoanLineItem>();
        foreach (var loan in activeLoans)
        {
            var balance = CalculateLoanBalance(loan, asOf);
            if (loan.LoanType == LoanType.OperatingLineOfCredit)
                operatingLineBalance += balance;
            else
                longTermLoans.Add(new LoanLineItem
                {
                    LenderName = loan.LenderName,
                    LoanType = loan.LoanTypeDisplay,
                    Balance = balance
                });
        }
        var totalCurrentLiabilities = operatingLineBalance;
        var totalLongTerm = longTermLoans.Sum(l => l.Balance);
        var totalLiabilities = totalCurrentLiabilities + totalLongTerm;

        return new BalanceSheetDto
        {
            AsOf = asOf,
            CashApproximate = cash,
            LivestockInventoryValue = livestockValue,
            TotalCurrentAssets = totalCurrentAssets,
            FixedAssets = assetLines,
            TotalFixedAssetsGross = totalFixedGross,
            AccumulatedDepreciation = totalAccumDepr,
            TotalFixedAssetsNet = totalFixedNet,
            TotalAssets = totalAssets,
            OperatingLineBalance = operatingLineBalance,
            TotalCurrentLiabilities = totalCurrentLiabilities,
            LongTermLoans = longTermLoans,
            TotalLongTermLiabilities = totalLongTerm,
            TotalLiabilities = totalLiabilities,
            OwnersEquity = totalAssets - totalLiabilities
        };
    }

    public async Task<CashFlowDto> GetCashFlowStatementAsync(DateTime from, DateTime to)
    {
        var transactions = await _transactions.GetByDateRangeAsync(from, to);
        var loanPayments = await _loans.GetAllPaymentsInRangeAsync(from, to);
        var assets = await _assets.GetAllAsync();

        // Operating: income and expense transactions
        var operatingItems = transactions
            .Where(t => t.TransactionType != TransactionType.CapitalInflux)
            .GroupBy(t => t.Category)
            .Select(g =>
            {
                var isIncome = g.First().TransactionType == TransactionType.Income;
                return new CategoryLineItem
                {
                    Category = g.Key,
                    Amount = isIncome ? g.Sum(t => t.Amount) : -g.Sum(t => t.Amount)
                };
            })
            .OrderBy(c => c.Category)
            .ToList();

        // Investing: asset purchases in period (negative outflow)
        var investingItems = assets
            .Where(a => a.PurchaseDate >= from && a.PurchaseDate <= to)
            .Select(a => new CategoryLineItem { Category = $"Purchase: {a.AssetName}", Amount = -a.PurchasePrice })
            .ToList();

        // Add disposals as investing inflows
        foreach (var disposed in assets.Where(a => a.DisposedDate >= from && a.DisposedDate <= to && a.DisposalPrice.HasValue))
            investingItems.Add(new CategoryLineItem { Category = $"Sale: {disposed.AssetName}", Amount = disposed.DisposalPrice!.Value });

        // Financing: capital influx (positive) and loan principal payments (negative)
        var financingItems = new List<CategoryLineItem>();
        var capitalItems = transactions
            .Where(t => t.TransactionType == TransactionType.CapitalInflux)
            .GroupBy(t => t.Category)
            .Select(g => new CategoryLineItem { Category = g.Key, Amount = g.Sum(t => t.Amount) });
        financingItems.AddRange(capitalItems);
        var principalPaid = loanPayments.Sum(p => p.PrincipalPortion);
        if (principalPaid > 0)
            financingItems.Add(new CategoryLineItem { Category = "Loan Principal Payments", Amount = -principalPaid });

        var operating = operatingItems.Sum(i => i.Amount);
        var investing = investingItems.Sum(i => i.Amount);
        var financing = financingItems.Sum(i => i.Amount);

        return new CashFlowDto
        {
            PeriodFrom = from,
            PeriodTo = to,
            OperatingActivities = operating,
            InvestingActivities = investing,
            FinancingActivities = financing,
            NetCashChange = operating + investing + financing,
            OperatingItems = operatingItems,
            InvestingItems = investingItems,
            FinancingItems = financingItems
        };
    }

    public async Task<TaxSummaryDto> GetTaxSummaryAsync(int fiscalYear)
    {
        var startMonthStr = await _settings.GetAsync("FiscalYearStartMonth");
        var startMonth = int.TryParse(startMonthStr, out var m) ? m : 1;
        var (fiscalStart, fiscalEnd) = GetFiscalYearBounds(fiscalYear, startMonth);

        var transactions = await _transactions.GetByDateRangeAsync(fiscalStart, fiscalEnd);

        var grossSales = transactions
            .Where(t => t.TransactionType == TransactionType.Income &&
                        string.Equals(t.Category, nameof(IncomeCategory.LivestockSales), StringComparison.OrdinalIgnoreCase))
            .Sum(t => t.Amount);

        var otherIncome = transactions
            .Where(t => t.TransactionType == TransactionType.Income &&
                        !string.Equals(t.Category, nameof(IncomeCategory.LivestockSales), StringComparison.OrdinalIgnoreCase))
            .Sum(t => t.Amount);

        var totalExpenses = transactions
            .Where(t => t.TransactionType == TransactionType.Expense)
            .Sum(t => t.Amount);

        var netFarmIncome = grossSales + otherIncome - totalExpenses;

        var seTax = netFarmIncome > 0 ? Math.Round(netFarmIncome * 0.9235m * 0.153m, 2) : 0m;

        var fedRateStr = await _settings.GetAsync("FederalIncomeTaxRate");
        var stateRateStr = await _settings.GetAsync("StateIncomeTaxRate");
        var fedRate = decimal.TryParse(fedRateStr, out var fr) ? fr / 100m : 0.22m;
        var stateRate = decimal.TryParse(stateRateStr, out var sr) ? sr / 100m : 0m;
        var estimatedFedTax = netFarmIncome > 0 ? Math.Round(netFarmIncome * fedRate, 2) : 0m;
        var estimatedStateTax = netFarmIncome > 0 ? Math.Round(netFarmIncome * stateRate, 2) : 0m;

        var allAssets = await _assets.GetAllAsync();
        var capitalGainEvents = allAssets
            .Where(a => a.DisposedDate.HasValue &&
                        a.DisposedDate.Value >= fiscalStart &&
                        a.DisposedDate.Value <= fiscalEnd)
            .Select(a => new CapitalGainEvent
            {
                AssetDescription = a.AssetName,
                AcquisitionDate = a.PurchaseDate,
                SaleDate = a.DisposedDate!.Value,
                CostBasis = a.PurchasePrice,
                SalePrice = a.DisposalPrice ?? 0m
            })
            .ToList();

        var scheduleFItems = BuildScheduleFItems(transactions);

        return new TaxSummaryDto
        {
            FiscalYear = fiscalYear,
            TotalGrossSales = grossSales,
            TotalOtherIncome = otherIncome,
            TotalExpenses = totalExpenses,
            NetFarmIncome = netFarmIncome,
            SelfEmploymentTax = seTax,
            EstimatedFederalTax = estimatedFedTax,
            EstimatedStateTax = estimatedStateTax,
            CapitalGainEvents = capitalGainEvents,
            ScheduleFItems = scheduleFItems
        };
    }

    public async Task<FinancialKpiDto> GetKpisAsync()
    {
        var startMonthStr = await _settings.GetAsync("FiscalYearStartMonth");
        var startMonth = int.TryParse(startMonthStr, out var m) ? m : 1;

        var today = DateTime.Today;
        var fiscalYear = today.Month >= startMonth ? today.Year : today.Year - 1;
        var (fyStart, fyEnd) = GetFiscalYearBounds(fiscalYear, startMonth);

        var pl = await GetProfitAndLossAsync(fyStart, fyEnd);
        var bs = await GetBalanceSheetAsync(today);

        var animalCount = (await _animals.GetAllAsync())
            .Count(a => a.Status != AnimalStatus.Sold && a.Status != AnimalStatus.Deceased);
        var fyTransactions = await _transactions.GetByDateRangeAsync(fyStart, fyEnd);
        var animalsSoldThisYear = fyTransactions
            .Count(t => t.TransactionType == TransactionType.Income &&
                        string.Equals(t.Category, nameof(IncomeCategory.LivestockSales), StringComparison.OrdinalIgnoreCase));

        var debtToAsset = bs.TotalAssets > 0 ? bs.TotalLiabilities / bs.TotalAssets : 0m;
        var currentRatio = bs.TotalCurrentLiabilities > 0
            ? bs.TotalCurrentAssets / bs.TotalCurrentLiabilities : 0m;
        var returnOnAssets = bs.TotalAssets > 0 ? pl.NetFarmIncome / bs.TotalAssets : 0m;
        var avgCostPerHead = animalCount > 0 ? pl.TotalExpenses / animalCount : 0m;
        var breakEven = animalsSoldThisYear > 0 ? pl.TotalExpenses / animalsSoldThisYear : 0m;

        // Monthly revenue and expense bars — reuse fyTransactions already fetched above
        var monthlyData = Enumerable.Range(0, 12).Select(i =>
        {
            var month = new DateTime(fyStart.Year, fyStart.Month, 1).AddMonths(i);
            if (month > fyEnd) return null;
            var monthTx = fyTransactions.Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month).ToList();
            return new MonthlyBarDto
            {
                MonthLabel = month.ToString("MMM"),
                Revenue = monthTx.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount),
                Expenses = monthTx.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount)
            };
        }).Where(d => d is not null).ToList();

        return new FinancialKpiDto
        {
            FiscalYear = fiscalYear,
            NetFarmIncome = pl.NetFarmIncome,
            TotalRevenue = pl.GrossRevenue,
            TotalExpenses = pl.TotalExpenses,
            DebtToAssetRatio = debtToAsset,
            CurrentRatio = currentRatio,
            ReturnOnAssets = returnOnAssets,
            AvgCostPerHead = avgCostPerHead,
            BreakEvenPrice = breakEven,
            MonthlyData = monthlyData!
        };
    }

    // --- Private helpers ---

    private static (DateTime Start, DateTime End) GetFiscalYearBounds(int fiscalYear, int startMonth)
    {
        var start = new DateTime(fiscalYear, startMonth, 1);
        var end = start.AddYears(1).AddDays(-1);
        return (start, end);
    }

    private static IReadOnlyList<BudgetComparisonItem> BuildBudgetComparison(
        IEnumerable<CategoryLineItem> actuals,
        IEnumerable<BudgetEntryDto> budgetEntries,
        TransactionType type)
    {
        var budgetByCategory = budgetEntries
            .Where(b => b.TransactionType == type && b.Month == 0)
            .ToDictionary(b => b.Category, b => b.BudgetAmount, StringComparer.OrdinalIgnoreCase);

        var allCategories = actuals.Select(a => a.Category)
            .Union(budgetByCategory.Keys)
            .Distinct();

        return allCategories.Select(cat => new BudgetComparisonItem
        {
            Category = cat,
            Actual = actuals.FirstOrDefault(a => string.Equals(a.Category, cat, StringComparison.OrdinalIgnoreCase))?.Amount ?? 0m,
            Budget = budgetByCategory.TryGetValue(cat, out var b) ? b : 0m
        }).OrderBy(c => c.Category).ToList();
    }

    private static IReadOnlyList<ScheduleFLineItem> BuildScheduleFItems(IEnumerable<TransactionDto> transactions)
    {
        return transactions
            .GroupBy(t => t.Category, StringComparer.OrdinalIgnoreCase)
            .Where(g => ScheduleFMap.ContainsKey(g.Key))
            .Select(g => new ScheduleFLineItem
            {
                LineDescription = ScheduleFMap[g.Key],
                Category = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .OrderBy(s => s.LineDescription)
            .ToList();
    }
}
