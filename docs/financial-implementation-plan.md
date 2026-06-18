# Herd Master — Financial Module: Software Implementation Plan

## Context
Adding a full Financial Mode to Herd Master based on the approved requirements in `docs/financial-requirements.md`. This is a large feature delivered in 6 incremental phases. Each phase is independently shippable. The existing animal purchase/sale fields on `Animal` are already in place — this plan builds the full accounting layer around them.

---

## Architecture Overview

### New DB Tables (4)
| Table | Purpose |
|-------|---------|
| `Transactions` | All income, expense, and capital influx entries |
| `Assets` | Asset register (equipment, land, buildings, breeding stock) |
| `Loans` | Debt/loan tracking |
| `LoanPayments` | Individual loan payment records |
| `BudgetEntries` | Annual budget by category and month |

### New Enums (add to `src/CattleManager.Core/Models/Enums.cs`)
```
TransactionType       { Income, Expense, CapitalInflux }
ExpenseCategory       { FeedHay, VeterinaryMedical, BreedingFees, FuelOil, 
                        RepairsMaintenance, Utilities, LaborContractWork, 
                        TruckingTransportation, Insurance, PropertyTaxes, 
                        MarketingAuction, SuppliesMiscellaneous, InterestExpense, Other }
IncomeCategory        { LivestockSales, BreedingServices, HayCropSales, CustomWork, 
                        GovernmentPayments, InsuranceProceeds, MiscellaneousIncome }
CapitalInfluxType     { Grant, EquityInvestment, SharePurchase, Other }
AssetCategory         { Livestock, MachineryEquipment, Land, Building, Vehicle, Other }
DepreciationMethod    { StraightLine, DB150, Section179 }
LoanType              { OperatingLineOfCredit, EquipmentLoan, RealEstateLoan, Other }
PaymentFrequency      { Monthly, Quarterly, SemiAnnual, Annual }
```

### New Settings Keys (stored in AppSettings table)
| Key | Default | Purpose |
|-----|---------|---------|
| `FiscalYearStartMonth` | `"1"` | 1–12; January default |
| `AccountingMethod` | `"Cash"` | "Cash" or "Accrual" |
| `StateOfOperation` | `""` | State name for tax rates |
| `StateSalesTaxRate` | `"0"` | Decimal, e.g. "0.045" |
| `StateIncomeTaxRate` | `"0"` | Decimal |
| `FederalIncomeTaxRate` | `"0.22"` | Estimated bracket rate |

---

## Critical: Non-Destructive DB Schema Migration

The existing `DatabaseSchemaIsValidAsync()` probes all DbSet tables. Adding new DbSets without first creating those tables would cause it to wipe the user's data on upgrade.

**Required fix in `src/CattleManager.App/App.xaml.cs`:**
1. Add `EnsureFinancialTablesExistAsync(CattleDbContext db)` — uses raw SQL `CREATE TABLE IF NOT EXISTS` for all 5 new tables (Transactions, Assets, Loans, LoanPayments, BudgetEntries)
2. Call it **before** `DatabaseSchemaIsValidAsync()` runs so the probe finds them
3. Update `DatabaseSchemaIsValidAsync()` to also probe the 5 new table names

This follows the same approach as the existing `EnsureColumnsExistAsync()`.

---

## Phase 1 — Foundation (Data + Core Layer)
*No UI. Sets up all new entities, DTOs, services, and DB migration.*

### Files to Create

**`src/CattleManager.Data/Entities/Financial.cs`** — 5 entity classes:
- `Transaction` — TransactionId, TransactionType (enum), Category (string), Date, Amount (decimal 10,2), Description, PayeePayer, PaymentMethod, Notes, AttachmentPath, LinkedAnimalId (FK nullable, SetNull), IsSampleData, CreatedDate, ModifiedDate
- `Asset` — AssetId, AssetName, Category (enum), PurchaseDate, PurchasePrice (10,2), CurrentValue (10,2), DepreciationMethod (enum), UsefulLifeYears, SalvageValue (10,2), LinkedAnimalId (FK nullable, SetNull), DisposedDate?, DisposalPrice? (10,2), Notes, IsSampleData, CreatedDate
- `Loan` — LoanId, LenderName, LoanType (enum), OriginalPrincipal (10,2), InterestRate (decimal 8,4), StartDate, MaturityDate?, PaymentFrequency (enum), PaymentAmount (10,2), IsActive, Notes, IsSampleData, CreatedDate
- `LoanPayment` — PaymentId, LoanId (FK Cascade), PaymentDate, TotalPayment (10,2), PrincipalPortion (10,2), InterestPortion (10,2), RemainingBalance (10,2), Notes, IsSampleData
- `BudgetEntry` — BudgetEntryId, FiscalYear (int), Category (string), TransactionType (enum), Month (int, 0=annual total), BudgetAmount (10,2)

**`src/CattleManager.Core/Models/FinancialDtos.cs`** — DTO classes mirroring entities:
- `TransactionDto`, `AssetDto`, `LoanDto`, `LoanPaymentDto`, `BudgetEntryDto`
- `ProfitAndLossDto` — computed: GrossRevenue (by category), TotalExpenses (by category), NetFarmIncome, DepreciationTotal, InterestExpense
- `BalanceSheetDto` — CurrentAssets, FixedAssets, TotalAssets, CurrentLiabilities, LongTermLiabilities, TotalLiabilities, OwnersEquity
- `CashFlowDto` — OperatingActivities, InvestingActivities, FinancingActivities, NetCashChange
- `TaxSummaryDto` — TotalGrossSales, EstimatedFederalTax, EstimatedStateTax, SelfEmploymentTax, CapitalGainEvents list
- `FinancialKpiDto` — NetFarmIncome, TotalRevenue, TotalExpenses, DebtToAssetRatio, CurrentRatio, AvgCostPerHead, BreakEvenPrice

**`src/CattleManager.Core/Services/IFinancialRepository.cs`** — 4 repository interfaces:
```csharp
ITransactionRepository: GetAllAsync(), GetByDateRangeAsync(from, to), GetByTypeAsync(type), 
    AddAsync(dto), UpdateAsync(dto), DeleteAsync(id), DeleteSampleDataAsync()
IAssetRepository: GetAllAsync(), GetActiveAsync(), GetByAnimalAsync(animalId),
    AddAsync(dto), UpdateAsync(dto), DeleteAsync(id), DeleteSampleDataAsync()
ILoanRepository: GetAllAsync(), GetActiveAsync(), GetPaymentsAsync(loanId),
    AddAsync(dto), UpdateAsync(dto), DeleteAsync(id),
    AddPaymentAsync(dto), DeletePaymentAsync(id), DeleteSampleDataAsync()
IBudgetRepository: GetByFiscalYearAsync(year), UpsertAsync(dto), DeleteByYearAsync(year)
```

**`src/CattleManager.Core/Services/FinancialService.cs`** — business logic (no interface, concrete class like HealthService):
```csharp
// Dependencies: ITransactionRepository, IAssetRepository, ILoanRepository,
//               IBudgetRepository, IAnimalRepository, IAppSettingsRepository
Task<ProfitAndLossDto> GetProfitAndLossAsync(DateTime from, DateTime to)
Task<BalanceSheetDto> GetBalanceSheetAsync(DateTime asOf)
Task<CashFlowDto> GetCashFlowStatementAsync(DateTime from, DateTime to)
Task<TaxSummaryDto> GetTaxSummaryAsync(int fiscalYear)
Task<FinancialKpiDto> GetKpisAsync()
decimal CalculateAnnualDepreciation(AssetDto asset, int year)  // sync, pure math
decimal CalculateLoanBalance(LoanDto loan, DateTime asOf)       // sync, pure math
Task<string> GenerateBillOfSaleAsync(TransactionDto sale, string outputPath)  // PDF via QuestPDF
```

**`src/CattleManager.Data/Repositories/FinancialRepositories.cs`** — 4 repository implementations following the exact same pattern as `OtherRepositories.cs`.

### Files to Modify

**`src/CattleManager.Data/CattleDbContext.cs`**
- Add 5 new DbSet properties
- Configure entity relationships and decimal precision in `OnModelCreating`
- Animal navigation: `Transaction.LinkedAnimal`, `Asset.LinkedAnimal` (SetNull)

**`src/CattleManager.App/App.xaml.cs`**
- Add `EnsureFinancialTablesExistAsync()` with `CREATE TABLE IF NOT EXISTS` DDL for all 5 tables
- Update `DatabaseSchemaIsValidAsync()` to probe the 5 new table names
- Register 4 new repositories as Scoped
- Register `FinancialService` as Scoped

**`src/CattleManager.Core/Models/Enums.cs`** — add 8 new enums listed above

### Tests to Add
**`tests/CattleManager.Tests/Core/FinancialServiceTests.cs`**
- Test `CalculateAnnualDepreciation` for all 3 methods (straight-line, DB150, Section 179)
- Test `CalculateLoanBalance` for amortization math
- Test `GetProfitAndLoss` with mocked transaction data

**`tests/CattleManager.Tests/Data/RepositoryTests.cs`** — add facts for:
- Transaction CRUD, date range query
- Asset CRUD, active filter
- Loan + LoanPayment CRUD

---

## Phase 2 — Transaction Management (Income & Expenses)
*Daily use: entering expenses and income, with receipt attachments and animal linkage.*

### New UI Files

**`src/CattleManager.App/Views/TransactionListPage.xaml` + `.xaml.cs`**
- Pattern: HerdDetailsPage (DataGrid + filter bar)
- Filters: date range, TransactionType (All/Income/Expense/Capital), category
- Columns: Date, Type (badge), Category, Description, PayeePayer, Amount
- Toolbar: "Add Expense", "Add Income", "Add Capital", export CSV
- DataGrid row double-click → TransactionFormPage in edit mode

**`src/CattleManager.App/ViewModels/TransactionListViewModel.cs`**
- `ICollectionView` with `CollectionViewSource` filter (same pattern as HerdDetailsViewModel)
- `FilterType`, `FilterCategory`, `FilterDateFrom`, `FilterDateTo` properties
- `TotalIncome`, `TotalExpenses`, `NetAmount` computed footer stats
- `[RelayCommand] AddTransactionAsync(TransactionType)`, `DeleteAsync(TransactionDto)`

**`src/CattleManager.App/Views/TransactionFormPage.xaml` + `.xaml.cs`**
- Pattern: AnimalFormPage (multi-section CardStyle form)
- Sections:
  - Transaction Details: Type, Date, Amount, Category, Description
  - Payee/Payer: Name, address
  - Receipt Attachment: file picker + thumbnail preview (reuse AnimalAttachment pattern + PathToImageConverter)
  - Animal Linkage: optional ComboBox to link to an animal record; selecting one auto-populates purchase/sale price on that animal
  - Tax Details: gross amount, applicable tax rate, tax amount, net proceeds
- Validation: amount > 0, date required, category required

**`src/CattleManager.App/ViewModels/TransactionFormViewModel.cs`**
- On save with LinkedAnimalId set: update `Animal.PurchasePrice` (if Expense type) or `Animal.SalePrice + BuyerName + SoldDate` (if Income type) via `IAnimalRepository`
- `AttachmentPath` via `DialogService.OpenAnyFile()` (filter: images + PDFs)
- Tax calculation: `TaxAmount = Amount * TaxRate`, `NetAmount = Amount - TaxAmount`

### Files to Modify

**`src/CattleManager.App/MainWindow.xaml`** — add nav button between Herd Details and Settings:
```xaml
<Button x:Name="BtnFinancials" Content="💰  Financials"
        Click="BtnFinancials_Click" Style="{StaticResource NavButtonStyle}" Tag="Financials"/>
```

**`src/CattleManager.App/MainWindow.xaml.cs`** — add `BtnFinancials_Click` handler navigating to `TransactionListPage`

**`src/CattleManager.App/App.xaml.cs`** — register `TransactionListViewModel`, `TransactionFormViewModel` as Transient

**`src/CattleManager.App/App.xaml`** — add `DecimalToStringConverter` (for Amount TextBox two-way binding)

---

## Phase 3 — Asset Register & Depreciation

### New UI Files

**`src/CattleManager.App/Views/AssetListPage.xaml` + `.xaml.cs`**
- DataGrid columns: Name, Category, Purchase Date, Cost Basis, Current Value, Annual Depreciation, Book Value, Linked Animal
- Toolbar: "Add Asset"
- Footer: Total Assets (cost), Total Book Value

**`src/CattleManager.App/ViewModels/AssetListViewModel.cs`**
- On load: call `FinancialService.CalculateAnnualDepreciation()` for each asset to populate display
- `[RelayCommand] DisposeAsset(AssetDto)` — opens dialog for disposal price/date, calculates gain/loss

**`src/CattleManager.App/Views/AssetFormPage.xaml` + `.xaml.cs`**
- Sections: Basic Info (name, category, purchase date, cost), Depreciation (method, useful life, salvage value), Animal Linkage (optional ComboBox)
- On save: if breeding stock linked to animal, auto-add animal to asset register

**`src/CattleManager.App/ViewModels/AssetFormViewModel.cs`**

### Files to Modify
**`src/CattleManager.App/App.xaml.cs`** — register 2 new ViewModels

---

## Phase 4 — Loan Management

### New UI Files

**`src/CattleManager.App/Views/LoanListPage.xaml` + `.xaml.cs`**
- DataGrid: Lender, Type, Original Principal, Balance, Rate, Next Payment, Status badge (Active/Paid Off)
- Toolbar: "Add Loan"
- Footer: Total Outstanding Debt

**`src/CattleManager.App/ViewModels/LoanListViewModel.cs`**
- `[RelayCommand] ViewAmortization(LoanDto)` → navigates to LoanDetailPage

**`src/CattleManager.App/Views/LoanFormPage.xaml` + `.xaml.cs`**
- Fields: Lender, Type, Principal, Interest Rate, Start Date, Maturity Date, Payment Frequency, Payment Amount, Notes

**`src/CattleManager.App/Views/LoanDetailPage.xaml` + `.xaml.cs`**
- Shows loan summary at top
- DataGrid: amortization schedule (all future payments: date, payment, principal, interest, balance)
- Actual payments panel: list of recorded payments with "Record Payment" button
- "Record Payment" opens a mini-form dialog (date, amounts auto-calculated, overrideable)

**`src/CattleManager.App/ViewModels/LoanDetailViewModel.cs`**
- `GenerateAmortizationSchedule()` — computed from loan terms (calls `FinancialService.CalculateLoanBalance`)
- `[RelayCommand] RecordPaymentAsync()` — dialog via DialogService, saves via `ILoanRepository.AddPaymentAsync()`

### Files to Modify
**`src/CattleManager.App/App.xaml.cs`** — register 3 new ViewModels

---

## Phase 5 — Financial Reports & Bill of Sale

### New UI Files

**`src/CattleManager.App/Views/ReportsPage.xaml` + `.xaml.cs`**
- Tab-style navigation (using TabControl or StackPanel + ContentPresenter) with 4 report views:
  1. **P&L** — period selector (month/YTD/prior year/lifetime), table showing income by category, expenses by category, subtotals, net income. Budget vs Actual columns if budget exists.
  2. **Balance Sheet** — date picker, structured table (Current Assets, Fixed Assets, Liabilities, Equity)
  3. **Cash Flow** — period selector, operating/investing/financing sections
  4. **Tax Summary** — fiscal year selector, Schedule F mapping table, capital gains list, estimated tax liabilities, quarterly payment guide

**`src/CattleManager.App/ViewModels/ReportsViewModel.cs`**
- `SelectedReport` (string or enum), `PeriodFrom`, `PeriodTo`, `FiscalYear`
- `[RelayCommand] GeneratePdfAsync()` — calls `FinancialService.GenerateBillOfSaleAsync()` or `ExportService` financial methods
- Computed properties built by calling `FinancialService.GetProfitAndLossAsync()` etc.

### Files to Modify

**`src/CattleManager.Core/Services/ExportService.cs`** — add financial report PDF methods:
```csharp
Task ExportProfitAndLossToPdfAsync(ProfitAndLossDto pl, DateTime from, DateTime to, string path)
Task ExportBalanceSheetToPdfAsync(BalanceSheetDto bs, DateTime asOf, string path)
Task ExportTaxSummaryToPdfAsync(TaxSummaryDto tax, int fiscalYear, string path)
Task ExportBillOfSaleToPdfAsync(TransactionDto sale, AnimalDto? animal, FarmDto farm, string path)
```
All use QuestPDF (already installed), follow the same fluent API pattern as `ExportAnimalProfileToPdf`.

**`src/CattleManager.App/App.xaml.cs`** — register `ReportsViewModel`

**`src/CattleManager.App/Converters/Converters.cs`** — add `DecimalToCurrencyConverter` (`1234.56 → "$1,234.56"`) for report display

---

## Phase 6 — Financial Dashboard + Settings + Budget

### New UI Files

**`src/CattleManager.App/Views/FinancialDashboardPage.xaml` + `.xaml.cs`**
- Replaces `TransactionListPage` as the landing page when clicking "Financials" in sidebar
- KPI stat tiles (using existing `StatCardButtonStyle`): Net Farm Income, Total Revenue, Total Expenses, Debt-to-Asset Ratio, Avg Cost/Head, Break-Even Price
- Clickable tiles navigate to relevant sub-pages
- Quick links: "Add Expense", "Add Income", "View Reports", "View Assets", "View Loans"
- Bar chart: Revenue vs Expenses by month (implemented as simple WPF Canvas or ItemsControl bars — no external chart library needed)

**`src/CattleManager.App/ViewModels/FinancialDashboardViewModel.cs`**
- Loads `FinancialKpiDto` via `FinancialService.GetKpisAsync()`
- Monthly bar data as `ObservableCollection<MonthlyBarDto>` (month label + revenue + expense amounts)
- Navigation commands to all financial sub-pages

**`src/CattleManager.App/Views/BudgetPage.xaml` + `.xaml.cs`**
- Fiscal year selector at top
- Grid: Category | Budget Amount | Actual YTD | Variance | % Used
- Inline editing of budget amounts
- "Save Budget" button

**`src/CattleManager.App/ViewModels/BudgetViewModel.cs`**

### Files to Modify

**`src/CattleManager.App/Views/SettingsPage.xaml`** — add new CardStyle section "Financial Settings":
- Fiscal Year Start Month (ComboBox: Jan–Dec)
- Accounting Method (ComboBox: Cash / Accrual)
- State of Operation (TextBox)
- State Sales Tax Rate % (TextBox)
- State Income Tax Rate % (TextBox)
- Federal Income Tax Rate % (TextBox, labeled "Estimated Federal Rate")

**`src/CattleManager.App/ViewModels/SettingsViewModel.cs`** — load/save 6 new settings keys

**`src/CattleManager.App/App.xaml.cs`** — register `FinancialDashboardViewModel`, `BudgetViewModel`; update BtnFinancials navigation target to `FinancialDashboardPage`

---

## Financial Sub-Navigation Pattern

Within the Financial section, use a secondary nav bar at the top of `FinancialDashboardPage` and all financial pages — a horizontal `WrapPanel` of text buttons (Transactions | Assets | Loans | Reports | Budget) that navigate between financial sub-pages without going back to the main sidebar. This keeps the financial section feel cohesive.

---

## Sample / Test Data

Follows the exact same pattern as the existing `SampleDataSeeder` — data is seeded on first launch, flagged with `IsSampleData = true`, and clearable from the Dashboard's existing "Clear Sample Data" button.

### Files to Modify

**`src/CattleManager.Core/Services/SampleDataSeeder.cs`** — extend `SeedAsync()` and `ClearSampleDataAsync()`:

**`SeedAsync()` additions** (called after existing animal/herd seed):
- **Transactions (15 sample entries):**
  - Expenses: Feed & Hay ($1,200 — "Coastal Bermuda hay, 10 bales"), Vet bill ($450 — linked to Atlas), AI breeding fee ($300), Fuel ($180), Fencing repair ($650), Insurance premium ($1,800/yr), Auction commission ($85)
  - Income: Sale of Duke ($2,800 — linked to Duke, updates SalePrice/BuyerName/SoldDate on animal), Hay sales ($600), Government ARC payment ($1,200)
  - Capital influx: USDA EQIP grant ($5,000)
  - All dated within the current fiscal year, spread across different months

- **Assets (5 sample entries):**
  - 2016 John Deere 5075E Tractor — Equipment, $42,000, StraightLine 15yr, salvage $5,000
  - Rolling Hills Ranch Land (40 acres) — Land, $120,000, no depreciation
  - Hay Barn — Building, $28,000, StraightLine 20yr, salvage $2,000
  - Atlas (bull) — Livestock, $3,500, StraightLine 7yr, linked to Atlas's AnimalId
  - 2019 F-250 Pickup — Vehicle, $38,000, DB150 5yr, salvage $8,000

- **Loans (2 sample entries):**
  - Land mortgage: First Ag Bank, $95,000 original, 6.5% fixed, 20yr, monthly $713, started 3 years ago
  - Equipment loan: AgriFinance, $38,000 original, 7.2% fixed, 5yr, monthly $753, started 1 year ago
  - Each loan has 3–6 recorded `LoanPayment` entries showing principal/interest breakdown

- **BudgetEntries (annual budget for current fiscal year):**
  - Income budget: Livestock Sales $15,000, Hay Sales $3,000, Government Payments $2,000
  - Expense budget: Feed & Hay $8,000, Vet & Medical $2,500, Fuel $3,000, Insurance $2,000, Labor $4,000, Misc $1,500

**`ClearSampleDataAsync()` additions** — delete financial sample data in FK-safe order:
```csharp
// LoanPayments first (FK to Loans), then Loans, then Transactions, then Assets, then BudgetEntries
await db.LoanPayments.Where(p => p.IsSampleData).ExecuteDeleteAsync();
await db.Loans.Where(l => l.IsSampleData).ExecuteDeleteAsync();
await db.Transactions.Where(t => t.IsSampleData).ExecuteDeleteAsync();
await db.Assets.Where(a => a.IsSampleData).ExecuteDeleteAsync();
await db.BudgetEntries.Where(b => b.IsSampleData).ExecuteDeleteAsync();
```

**Dashboard "Clear Sample Data" button** already calls `SampleDataSeeder.ClearSampleDataAsync()` — no changes needed there since the extension above handles the new tables.

---

## Verification (per phase)

| Phase | Test |
|-------|------|
| 1 | `dotnet test` passes; `dotnet build` passes; no DB wipe on upgrade of existing DB |
| 2 | Add an expense linked to a cow → cow's PurchasePrice updates; receipt image attaches and previews |
| 3 | Add equipment asset; verify straight-line depreciation math; dispose asset → gain/loss shown |
| 4 | Add a loan; verify amortization schedule totals; record a payment → balance reduces |
| 5 | Generate P&L PDF for date range with data; generate bill of sale PDF with animal details |
| 6 | Financial dashboard KPIs match manual sum of transactions; budget vs actual % correct; settings persist across restart |

---

## File Count Summary
- **New files:** ~20 (entities, DTOs, services, repositories, pages, viewmodels)
- **Modified files:** ~6 (DbContext, App.xaml.cs, Enums.cs, MainWindow, SettingsPage, ExportService)
- **New tests:** ~2 test files, ~15 new test facts
- **Existing patterns reused:** CardStyle, NavButtonStyle, StatCardButtonStyle, PrimaryButton, ICollectionView filtering, RelayCommand, QuestPDF, DialogService, AnimalAttachment file picker
