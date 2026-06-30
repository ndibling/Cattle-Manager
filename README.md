# Herd Master

A modern Windows desktop application for managing farm animal herds. Track animals, health records, breeding, pedigrees, and finances — all stored locally on your machine. Supports cattle, horses, goats, sheep, chickens, ducks, geese, pigs, and any custom animal type you define.

![CI](https://github.com/ndibling/Cattle-Manager/actions/workflows/ci.yml/badge.svg)

---

## Features

### Herd Management
- **Multi-Species Herds** — Create herds for any farm animal: cattle, horses, goats, sheep, chickens, ducks, geese, pigs, or custom types you define in Settings. Each herd is tied to an animal type; "herd" vs. "flock" terminology adjusts automatically
- **Dashboard** — At-a-glance stats: total animals, breeding females/males, pregnant animals, and animals due for husbandry tasks
- **Herd Details** — Sortable, filterable, searchable table of all animals with inline action menus
- **Animal Profiles** — "Baseball card" view with photo, full health history, lineage, and reproduction info; Purchase Details and Sale & Valuation sections with asking price, current value, sale price, and buyer info; inline edit mode
- **Add / Edit Animals** — Tabbed form with validation; breed dropdown filtered to the herd's species; autocomplete sire/dam dropdowns, photo upload, and auto-calculated ages and due dates
- **Pedigree Diagram** — Visual 4-generation ancestry tree with clickable nodes and print support
- **Health & Husbandry Tracking** — Worming, vaccination, and health check dates with overdue alerts
- **Breeding Management** — Pregnancy tracking with auto-calculated due dates (283-day gestation), calving records

### Settings & Customization
- **Animal Types** — View built-in types or add your own (e.g. Alpaca, Llama). Choose whether the group is called a "Herd", "Flock", "Pack", etc.
- **Breeds per Type** — Each animal type has its own breed list. Add custom breeds under any type; standard breeds are protected from deletion
- **Measurement Preferences** — Weight (lb/kg) and height (in/cm/hands) units
- **Themes** — Light and Dark mode

### Financial Management
- **Financial Dashboard** — KPI tiles for Net Farm Income, Total Revenue, Total Expenses, Debt-to-Asset Ratio, Average Cost/Head, and Break-Even Price; revenue vs. expense bar chart by month
- **Transactions** — Income and expense ledger with category tagging, payee/payer, receipt attachments, and optional animal linkage; filterable by date, type, and category
- **Asset Register** — Track equipment, land, buildings, vehicles, and breeding stock; animals automatically appear as livestock assets; current value tracked separately from cost basis; Straight-Line, 150% DB, or Section 179 depreciation
- **Loan Tracking** — Full amortization schedules; record actual payments with principal/interest breakdown; outstanding balance calculated to any date
- **Financial Reports** — Profit & Loss, Balance Sheet, Cash Flow Statement, and Tax Summary (Schedule F mapping, SE tax estimate, capital gains); exportable to PDF
- **Budget** — Monthly budget by income/expense category; Actual vs. Budget variance and annual totals computed automatically

### General
- **Export** — Herd list and animal data to CSV; animal profiles and financial reports to PDF
- **Backup & Restore** — One-click database backup and restore
- **Sample Data** — Ships with a pre-populated "Rolling Hills Ranch" demo herd so you can explore the app immediately
- **MSI Installer** — Standard Windows installer with Start Menu and Desktop shortcuts

---

## Screenshots

> Coming soon — install the app and explore the sample herd to see it in action.

---

## Requirements

| Requirement | Minimum |
|---|---|
| Windows | Windows 10 (x64) or later |
| Disk space | 200 MB |
| .NET Runtime | Bundled (self-contained, no separate install needed) |

---

## Installation

1. Go to the [Releases](https://github.com/ndibling/Cattle-Manager/releases) page
2. Download the latest `CattleManager-vX.X.X.msi`
3. Run the installer and follow the prompts
4. Launch **Herd Master** from the Start Menu or Desktop shortcut

Data is stored in `%LOCALAPPDATA%\CattleManager\` and is never transmitted externally.

---

## Getting Started

On first launch the app loads **Rolling Hills Ranch** — a sample Angus cattle herd with 20 animals, a full 4-generation pedigree, health records, breeding history, sample transactions, assets, and loans. Use it to explore all the features.

When you're ready to start with your own data, click **"Clear Sample Data"** on the dashboard. The button disappears permanently once clicked and all sample records are removed.

To add your first real herd, go to **Herds → Add Herd**, choose an animal type, and give it a name.

---

## Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Windows 10+ (WPF requires Windows)
- Visual Studio 2022 or Rider (optional)

### Build & Run

```bash
git clone https://github.com/ndibling/Cattle-Manager.git
cd Cattle-Manager
dotnet restore
dotnet build
dotnet run --project src/CattleManager.App
```

### Run Tests

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Coverage must remain at or above **80%** — the CI pipeline enforces this threshold and fails the build if it drops below.

### Project Structure

```
Cattle-Manager/
├── src/
│   ├── CattleManager.Core/       # Domain models, business logic, service interfaces
│   ├── CattleManager.Data/       # EF Core + SQLite: entities, repositories, migrations
│   └── CattleManager.App/        # WPF application: views, viewmodels, controls
├── tests/
│   └── CattleManager.Tests/      # xUnit unit + integration tests
├── installer/
│   └── CattleManager.Installer/  # WiX v4 MSI installer project
└── .github/workflows/
    ├── ci.yml                    # Build, test, coverage on every push/PR
    └── release.yml               # Build MSI and publish GitHub Release on version tag
```

### Tech Stack

| Layer | Technology |
|---|---|
| Language | C# 12 |
| Platform | .NET 8 (self-contained, win-x64) |
| UI Framework | WPF + [ModernWpf](https://github.com/Kinnara/ModernWpf) |
| MVVM | [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) |
| Database | SQLite via Entity Framework Core 8 |
| PDF Export | [QuestPDF](https://www.questpdf.com/) |
| CSV Export | [CsvHelper](https://joshclose.github.io/CsvHelper/) |
| Logging | [Serilog](https://serilog.net/) |
| Tests | xUnit + Moq + FluentAssertions + coverlet |
| Installer | WiX Toolset v4 |
| CI/CD | GitHub Actions |

---

## CI / CD

| Workflow | Trigger | What it does |
|---|---|---|
| `ci.yml` | Push / PR to `main` | Restore → Build → Test → Coverage gate (≥80%) → Upload binaries artifact |
| `release.yml` | Push a `v*` tag | All of the above → Build MSI → Create GitHub Release with `.msi` attachment |

To publish a release:

```bash
git tag v1.14.0
git push origin v1.14.0
```

---

## Data & Privacy

All data is stored locally in `%LOCALAPPDATA%\CattleManager\CattleManager.db`. The application does not connect to the internet or transmit any data externally.

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes
4. Open a pull request against `main`

Please ensure all tests pass and coverage stays at or above 80% before submitting.

---

## License

This project is provided for personal and commercial use. See [LICENSE](LICENSE) for details.
