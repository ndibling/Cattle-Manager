# Herd Master — Financial Module: Requirements

> Items marked `[USER]` originated from the owner's initial requirements.
> Items marked `[ACCT]` were added based on standard cattle farm accounting practice.

---

## 1. General / Setup

- `[USER]` The application shall have a dedicated **Financial Mode**, accessible from the sidebar navigation
- `[USER]` A **fiscal year start date** (month + day) shall be configurable in the Settings page
- `[ACCT]` The application shall support either **cash-basis** or **accrual-basis** accounting, selectable in Settings (most small cattle operations use cash basis)
- `[ACCT]` A **Chart of Accounts** — a configurable list of expense and income categories — shall be the foundation for all transactions

---

## 2. Expense Tracking

- `[USER]` Expenses shall be tracked monthly and aggregated by fiscal year
- `[USER]` Users shall be able to enter **expense reports** manually that are captured and rolled into calculations
- `[USER]` Expense reports shall accept **receipt image attachments** (JPEG, PNG, PDF)
- `[ACCT]` Each expense entry shall be assigned to one of the following **standard farm expense categories** (user-editable):
  - Feed & Hay
  - Veterinary & Medical
  - Breeding Fees / AI (artificial insemination)
  - Fuel & Oil
  - Repairs & Maintenance (equipment, fences, buildings)
  - Utilities (water, electricity)
  - Labor / Contract Work
  - Trucking / Transportation
  - Insurance
  - Property Taxes
  - Marketing / Auction Fees
  - Supplies & Miscellaneous
  - Interest (loan interest payments — operating cost, not principal)
- `[ACCT]` Expense entries shall include: date, category, vendor/payee, description, amount, payment method, and optional receipt attachment
- `[ACCT]` Expenses shall be filterable and searchable by date range, category, and amount

---

## 3. Income Tracking

- `[USER]` All income for the business shall be tracked, including animal sales and sales of other assets
- `[ACCT]` Each income entry shall be assigned to one of the following **standard farm income categories** (user-editable):
  - Livestock Sales (calves, feeders, cull cows, slaughter cattle)
  - Breeding Services / Stud Fees
  - Hay / Crop Sales
  - Custom Work Income
  - Government / USDA Program Payments (FSA, CRP, EQIP, ARC/PLC)
  - Insurance Proceeds
  - Miscellaneous Income
- `[ACCT]` Income entries shall include: date, category, payer/buyer, description, amount, and optional documentation attachment
- `[USER]` **Capital influx** (grants, equity investment, share purchases) shall be tracked separately from operating income and shall not flow into the P&L operating income line — it appears on the balance sheet as equity or liability

---

## 4. Asset Register & Depreciation

- `[USER]` The application shall maintain an **asset register** covering:
  - Livestock (breeding stock — not market animals)
  - Machinery & Equipment
  - Land & Real Property
  - Buildings & Improvements
  - Vehicles
  - Other
- `[ACCT]` Each asset entry shall include: name/description, purchase date, purchase price (cost basis), current estimated value, depreciation method, useful life (years), and salvage value
- `[ACCT]` The application shall support the following **depreciation methods**:
  - Straight-Line (most common for buildings/equipment)
  - 150% Declining Balance (common for farm equipment)
  - Section 179 / Bonus Depreciation (full first-year expensing for equipment)
- `[ACCT]` The asset register shall calculate and display **annual depreciation** for each asset and cumulative accumulated depreciation
- `[ACCT]` Animals in the herd marked as breeding stock shall automatically appear in the asset register at their purchase price (or raised cost basis); market/feeder animals are inventory, not fixed assets
- `[ACCT]` When an asset is sold or disposed of, the application shall calculate the **gain or loss on sale** (sale price minus book value)

---

## 5. Livestock Inventory & Cost Basis

- `[ACCT]` Each animal in the herd shall have a **cost basis** — the total invested cost of that animal (purchase price for bought animals; accumulated raising costs for born-on-property animals)
- `[ACCT]` The application shall track total **livestock inventory value** (sum of cost basis of all animals currently owned) for balance sheet purposes
- `[ACCT]` Feed, vet, and other costs shall be optionally assignable to a specific animal or animal group for cost-of-production tracking

---

## 6. Animal ↔ Financial Linkage

- `[USER]` When recording a **purchase expense** for a cow, the application shall prompt to link it to an existing animal record or create a new one; the purchase price on the animal's profile is auto-populated
- `[USER]` When recording **income from the sale of a cow**, the application shall prompt to link it to an animal record; the sale price, buyer name, and sale date on that animal's profile are auto-updated
- `[USER]` The application shall **auto-generate a bill of sale document** (printable / exportable as PDF) when a livestock or asset sale is recorded, containing:
  - Seller name and address
  - Buyer name and address
  - Date of sale
  - Description of animal(s): tag number, breed, sex, approximate weight, age
  - Sale price
  - Terms of sale
  - Signature lines

---

## 7. Debt & Loan Management

- `[USER]` The application shall track debts and loan obligations with the following attributes:
  - Lender name
  - Loan type (Operating Line of Credit, Equipment Loan, Real Estate / Land Loan, Other)
  - Original principal
  - Interest rate (fixed or variable)
  - Loan start date and maturity date
  - Payment frequency (monthly, quarterly, annual)
  - Payment amount
- `[ACCT]` The application shall generate an **amortization schedule** for each loan showing payment date, payment amount, interest portion, principal portion, and remaining balance
- `[ACCT]` Loan payments shall be split between interest expense (flows to P&L) and principal reduction (balance sheet only)
- `[ACCT]` The application shall display total outstanding debt and a debt-to-asset ratio on the financial dashboard

---

## 8. Financial Statements

- `[USER]` **Profit & Loss Statement** — filterable by: current month, any month, fiscal year-to-date, prior fiscal year, lifetime. Sections:
  - Gross Revenue (by income category)
  - Operating Expenses (by expense category)
  - Operating Income
  - Interest Expense (from loans)
  - Depreciation
  - Net Farm Income
- `[USER]` **Balance Sheet** — point-in-time snapshot showing:
  - Assets: Current (cash, livestock inventory, receivables), Fixed (land, equipment, buildings net of depreciation)
  - Liabilities: Current (operating line, accounts payable), Long-Term (land loans, equipment loans)
  - Owner's Equity / Net Worth
- `[ACCT]` **Cash Flow Statement** — operating, investing, and financing activities for a selected period
- `[ACCT]` **Cost of Production Report** — total expenses divided by units (head, pounds) for a selected period
- `[ACCT]` All statements shall be **printable and exportable to PDF**

---

## 9. Financial KPIs Dashboard

- `[ACCT]` A financial summary dashboard shall display key metrics:
  - Net Farm Income (current fiscal year)
  - Total Revenue vs. Total Expenses (current fiscal year, with bar/trend visual)
  - Debt-to-Asset Ratio
  - Current Ratio (current assets / current liabilities)
  - Return on Assets (net income / total assets)
  - Average Cost per Head (total expenses / head count)
  - Break-Even Sale Price per Animal (total cost / animals sold)

---

## 10. Budgeting

- `[ACCT]` Users shall be able to enter an **annual budget** by expense and income category
- `[ACCT]` The P&L view shall include a **Budget vs. Actual** column showing variance for each category
- `[ACCT]` The financial dashboard shall show a budget utilization indicator (% of budget used year-to-date)

---

## 11. Accounts Payable & Receivable

- `[ACCT]` **Accounts Payable**: ability to record vendor bills that have been received but not yet paid, with due date and outstanding balance
- `[ACCT]` **Accounts Receivable**: ability to record amounts owed to the business (e.g., livestock sold on terms), with due date and outstanding balance
- `[ACCT]` Aging reports for both AP and AR (30/60/90+ days)

---

## 12. Tax Tracking & Reporting

- `[ACCT]` The application shall generate a **Schedule F summary report** (Farm Income and Expense summary) that maps income/expense categories to their IRS Schedule F line items — for use as reference when preparing taxes (the app does not file taxes)
- `[ACCT]` The application shall generate an **annual depreciation summary** showing each asset, depreciation method, and current-year depreciation amount for use with IRS Form 4562
- `[USER]` Each sale transaction (livestock or other asset) shall record applicable **taxes collected or owed**, including:
  - **Federal taxes**: self-employment tax on net farm income, federal income tax withholding (if applicable), capital gains tax on asset sales (short-term vs. long-term based on holding period)
  - **State taxes**: configurable state sales tax rate (some states tax livestock sales; varies by state) and state income tax on farm income
- `[USER]` The application shall allow the user to configure the **state of operation** in Settings, and provide a default state sales/income tax rate that can be overridden per transaction
- `[USER]` For each sale, the application shall display and store: gross sale amount, applicable tax rate(s), tax amount(s) collected or owed, and net proceeds
- `[USER]` The application shall generate an annual **Tax Summary Report** showing:
  - Total gross sales (federal and state taxable events)
  - Total estimated federal tax liability on sales/income
  - Total estimated state tax liability on sales/income
  - Capital gains events: asset description, acquisition date, sale date, cost basis, sale price, gain/loss, short-term vs. long-term classification
  - Quarterly estimated tax payment schedule (federal Form 1040-ES guidance)
- `[ACCT]` Self-employment tax (15.3% on net farm income) shall be calculated and displayed in the Tax Summary Report as it is a significant and often overlooked obligation for sole proprietors
- `[ACCT]` All tax reports are informational and for planning/reference purposes only — the application does not file taxes or interface with tax agencies

---

## Out of Scope (for now)

- Full double-entry bookkeeping / general ledger
- Payroll processing
- Bank feed / automatic transaction import
- Multi-entity / multi-farm support
- Tax filing integration
