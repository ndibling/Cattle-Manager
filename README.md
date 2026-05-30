# 🐄 Cattle Manager

A modern Windows desktop application for managing cattle herds. Track animals, health records, breeding, pedigrees, and more — all stored locally on your machine.

![CI](https://github.com/ndibling/Cattle-Manager/actions/workflows/ci.yml/badge.svg)

---

## Features

- **Dashboard** — At-a-glance herd stats: total animals, breeding females/males, pregnant cows, and animals due for husbandry tasks
- **Herd Details** — Sortable, filterable, searchable table of all animals with inline action menus
- **Animal Profiles** — "Baseball card" view with photo, full health history, lineage, and reproduction info; inline edit mode
- **Add / Edit Animals** — Tabbed form with validation, autocomplete sire/dam dropdowns, photo upload, and auto-calculated ages and due dates
- **Pedigree Diagram** — Visual 4-generation ancestry tree with clickable nodes and print support
- **Health & Husbandry Tracking** — Worming, vaccination, and health check dates with overdue alerts
- **Breeding Management** — Pregnancy tracking with auto-calculated due dates (283-day gestation), calving records
- **Export** — Herd list to CSV, animal profiles to PDF
- **Backup & Restore** — One-click database backup and restore
- **Sample Data** — Ships with a pre-populated "Rolling Hills Ranch" demo herd so you can explore the app immediately
- **Light / Dark Mode** — Switchable theme via Settings
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
4. Launch **Cattle Manager** from the Start Menu or Desktop shortcut

Data is stored in `%LOCALAPPDATA%\CattleManager\` and is never transmitted externally.

---

## Getting Started

On first launch the app loads **Rolling Hills Ranch** — a sample Angus herd with 20 animals, a full 4-generation pedigree, health records, and breeding history. Use it to explore all the features.

When you're ready to start with your own data, click **"Clear Sample Data"** on the dashboard. The button disappears permanently once clicked and all sample records are removed.

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
git tag v1.0.0
git push origin v1.0.0
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

This project is provided for personal and commercial use in cattle herd management. See [LICENSE](LICENSE) for details.
