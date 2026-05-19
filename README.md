# AKB5

## Overview

`AKB5` is a WinForms application on C# / .NET 8 for maintaining an engineering knowledge base for industrial automation systems.

Current direction of the project:

- the left side is a physical object tree
- the right side is a type-driven workspace resolved by `NodeType`
- current builds use portable-first SQLite single-file `.akb` storage
- JSON remains an import/export and first-launch migration compatibility format
- Excel workbook `v3` remains a legacy transition exchange format
- user-facing program UI is Russian-only

The active integration branch is `to`.

## Current implementation state

Implemented on `to`, with active `Net` branch follow-ups noted where relevant:

- `Phase 0` - user-facing levels removed from the main UX
- `Phase 1` - persistent `NodeId` / `NodeType` foundation and migration
- `Phase 2` - right-panel workspace host
- `Phase 3` - typed `Composition`
- `Phase 3B` - composition templates and copy-from-existing-object
- `Phase 4` - typed `Documentation and Software`
- `Phase 5` - scoped search across `Tree`, `Card`, `Composition`, and `Docs/Software`
- `Phase 6` - file-based `Network` tab with image preview and `Open original`
- `Net` branch follow-up (2026-05-19) - network passport CRUD for devices, interfaces, and connections; manual scheme-entry fields; editable protocol/medium presets; passport filtering; robust network dialog layouts; PDF network scheme references as metadata/`Open original` sources; source-note comments for scheme references; interface passport columns for stored `MPI/DP/PN` and medium values; and validation for manual interface IP/mask/gateway entry. OCR/PDF auto-import, PRONETA/CSV, live scan, plan/fact comparison, data-quality issue panels, and AKB5-driven IP/PROFINET assignment remain intentionally out of scope.
- `Phase 7A` - maintenance-planning domain foundation, inventory number support, `График ТО` profiles
- `Phase 7B` - Russian `5/2` production-calendar workday calculation
- `Phase 7C` - monthly maintenance planning engine with monthly workshop budget validation
- `Phase 7D` - template-driven monthly/yearly maintenance workbook generation and future-month recalculation
- `Phase 7E` - manual annual `ТО1` / `ТО2` / `ТО3` placement source in JSON
- `Phase 7E.2` - Excel source exchange for annual ТО placement
- `Phase 7E` follow-ups - in-app source mass-editing grid, large `ТО2` / `ТО3` work splitting, norm-import matching/reporting
- `Phase 7F` - production-calendar configuration through JSON, UI editor, and JSON import
- `Phase 7F.1` - PDF production-calendar import, verified, accepted, committed, and pushed on `to`
- `Phase 11A` - equipment catalog model, accepted after manual review
- `Phase 11B` - Russian equipment catalog UI for list/add/edit/delete/search
- `Phase 11C` - object-template model, accepted after manual review and committed/pushed on `to`
- `Phase 11D` - create tree objects from persisted object templates, accepted after manual review and committed/pushed on `to`
- `Phase 11E` - save existing object subtree as a reusable object template, accepted and committed/pushed on `to`
- `Phase 11F` - apply object templates to existing objects with preview, accepted and committed/pushed on `to`
- `Phase 11G` - template/catalog JSON import/export, accepted after manual review and committed/pushed on `to`
- local `Phase 12A` / `12B` / `12C` JSON snapshot prototype - verified locally, paused before commit while storage moves to SQLite
- `Phase 12S0` - SQLite single-file storage redesign plan, approved with choices `1A, 2B, 3A, 4A`
- `Phase 12S1` - storage abstraction accepted and committed/pushed on `to`
- `Phase 12S2` - SQLite schema/repository accepted and committed/pushed on `to`
- `Phase 12S3` - first-launch JSON migration accepted and committed/pushed on `to`
- `Phase 12S4` - `.akb` database file UX and full JSON import/export accepted and committed/pushed on `to`
- `Phase 12S5` - SQLite-backed snapshots accepted and committed/pushed on `to`
- `Phase 12S6` - restore selected snapshot accepted and committed/pushed on `to`
- `Phase 12S7` - snapshot comparison accepted and committed/pushed on `to`
- `Phase 12S8` - change history accepted after manual review and committed/pushed on `to`
- `phase7e-annual-norm-import` - annual maintenance norm import by workbook structure, using `456.xlsx` as the reference example, accepted after manual review
- `phase7g-annual-norm-hidden-rows` - annual norm import skips hidden rows for retired equipment; committed/pushed on `to` as `7a4895d`
- menu rework first iteration - top menus, unified snapshots/history entry, grouped `ТО`, shorter tree context menu, improved move confirmation, and protective snapshot prompts; committed/pushed on `to` as `8dfffbd`

Next approved work:

- review and accept portable-first storage and external `.akb` backups
- `Phase 8` through `Phase 10` remain discussed candidate directions, not active implementation phases

## Data and persistence

Current JSON schema version: `3`

Core persisted structures:

- `KbNode`
  - `NodeId`
  - `NodeType`
  - `Details`
  - `Children`
- `SavedData.CompositionEntries`
- `SavedData.DocumentLinks`
- `SavedData.SoftwareRecords`
- `SavedData.NetworkFileReferences`
- `SavedData.MaintenanceScheduleProfiles`
- `SavedData.ObjectTemplates`
- `Config.ProductionCalendarYears`

Important persistence rules:

- SQLite single-file `.akb` is the current target live storage format
- portable-first startup stores `akb5.settings.json` next to `asutpKB.exe`
- without existing settings, the first launch offers either `database\knowledge-base.akb` next to the program or a user-selected database folder
- opening or saving another `.akb` path updates `akb5.settings.json`
- before overwriting an existing `.akb`, an external copy is created under `backups\yyyy-MM-dd\knowledge-base-yyyyMMdd-HHmmss.akb`
- JSON is the legacy persistence and full database import/export compatibility format
- typed cross-links must use stable IDs, never node names or paths
- Excel `v3` must stay readable during the transition

## Search

Current search behavior on `to`:

- indexed matches across `Tree`, `Card`, `Composition`, and `Docs/Software`
- scopes: `All`, `Tree`, `Card`, `Composition`, `Docs/Software`
- navigation always returns to the owning node in the tree
- results may switch the workspace to the preferred tab for the matched domain

## Documentation and Software

The `Documentation and Software` workflow is intentionally separate from `Composition`.

It stores typed records for:

- schemes
- manuals and instructions
- software folders / software links

Software links record `AddedAt` in the main UI workflow.

## Maintenance schedule generation

The maintenance workflow is implemented through `Phase 7F` on branch `to`.

Current behavior:

- maintenance settings are stored in `SavedData.MaintenanceScheduleProfiles` keyed by stable `OwnerNodeId`
- `ТО1` is monthly, `ТО2` is quarterly, `ТО3` is annual
- `ТО2` includes `ТО1`; `ТО3` includes `ТО1` and `ТО2`
- stored norms are per-occurrence labor hours, not monthly budgets
- the hard planner constraint is the selected monthly workshop budget, not a daily `<= 8` cap
- large `ТО2` / `ТО3` occurrences can be split into assignments up to 8 hours across working days
- manual annual placement is stored as per-profile `YearScheduleEntries`; empty entries keep deterministic fallback placement
- yearly source exchange edits only `YearScheduleEntries` and does not change norms, inclusion flags, or calendar settings
- generated maintenance workbooks are report artifacts; current builds default to portable-first `.akb` storage and keep JSON for import/export and migration compatibility
- production-calendar years are configured in `Config.ProductionCalendarYears` through `ТО -> Производственный календарь...`, PDF import, or service JSON import
- production-calendar JSON import accepts either `{ "ProductionCalendarYears": [...] }` or an array of year objects; each date must belong to its configured year
- production-calendar PDF import uses the PDF text layer first and previews found working/non-working date changes before applying them

## Excel workbook `v3`

Workbook `v3` is still the supported exchange format.

It remains a legacy-compatible transition layer and currently preserves:

- `NodeId`
- `NodeType`
- node card fields such as `NodeName`, `Description`, `Location`, `PhotoPath`, `IpAddress`, `SchemaLink`

Detailed workbook behavior and contract:

- [docs/workbook-v3.md](./docs/workbook-v3.md)

Deployment notes:

- [docs/deployment.md](./docs/deployment.md)

## Repository structure

- [asutpKB.csproj](./asutpKB.csproj) - root WinForms project
- [Program.cs](./Program.cs) - entry point
- [Forms](./Forms) - WinForms shell and dialog logic
- [Controls](./Controls) - reusable WinForms controls
- [Models](./Models) - shared domain models
- [Services](./Services) - non-UI logic, JSON, exchange, state services
- [UiServices](./UiServices) - WinForms-specific workflow/services
- [src/AsutpKnowledgeBase.Core](./src/AsutpKnowledgeBase.Core) - core library for testable logic
- [tests/AsutpKnowledgeBase.Core.Tests](./tests/AsutpKnowledgeBase.Core.Tests) - regression and unit tests
- [scripts/publish.ps1](./scripts/publish.ps1) and [scripts/publish.cmd](./scripts/publish.cmd) - publish flow

## Build and test

Typical local verification:

```powershell
dotnet restore asutpKB.csproj
dotnet restore tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj -c Release --no-restore
dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj -c Release --no-restore
```

If a local app instance is running, `dotnet build` into the default `Release` output may fail because `asutpKB.exe` locks DLLs. In that case either close the app or use an isolated output path for verification.

For WinForms `Network` UI changes, also run layout-smoke coverage against the affected windows/tabs. Prefer a non-invasive in-process layout smoke when an interactive app run would disturb manual work; `scripts/ui-smoke-network-passport.ps1` remains available for explicit executable-level checks. If the default `Release` output is locked, build to an isolated output path and copy `akb5.settings.json` plus `database\knowledge-base.akb` beside it before running executable-level smoke.

## Publish

Supported publish target:

- `win-x64`

Publish command:

```powershell
scripts\publish.cmd
```

Or directly:

```powershell
dotnet publish asutpKB.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish/win-x64
```

## Handoff documentation

Read these files in order for a new AI or engineering session:

1. [AGENTS.md](./AGENTS.md)
2. [docs/codex-handoff.md](./docs/codex-handoff.md)
3. [Roadmap.md](./Roadmap.md)

Reusable startup prompt:

- [docs/codex-start-prompt.md](./docs/codex-start-prompt.md)
