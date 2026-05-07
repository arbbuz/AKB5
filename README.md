# AKB5

## Overview

`AKB5` is a WinForms application on C# / .NET 8 for maintaining an engineering knowledge base for industrial automation systems.

Current direction of the project:

- the left side is a physical object tree
- the right side is a type-driven workspace resolved by `NodeType`
- current builds default to SQLite single-file `.akb` storage
- JSON remains an import/export and first-launch migration compatibility format
- Excel workbook `v3` remains a legacy transition exchange format
- user-facing program UI is Russian-only

The active integration branch is `to`.

## Current implementation state

Implemented on `to`:

- `Phase 0` - user-facing levels removed from the main UX
- `Phase 1` - persistent `NodeId` / `NodeType` foundation and migration
- `Phase 2` - right-panel workspace host
- `Phase 3` - typed `Composition`
- `Phase 3B` - composition templates and copy-from-existing-object
- `Phase 4` - typed `Documentation and Software`
- `Phase 5` - scoped search across `Tree`, `Card`, `Composition`, and `Docs/Software`
- `Phase 6` - file-based `Network` tab with image preview and `Open original`
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
- local `Phase 12S1` - storage abstraction implemented and verified
- local `Phase 12S2` - SQLite schema/repository implemented and verified
- local `Phase 12S3` - first-launch JSON migration implemented and verified
- local `Phase 12S4` - `.akb` database file UX and full JSON import/export implemented and verified
- local `Phase 12S5` - SQLite-backed snapshots implemented and verified
- local `Phase 12S6` - restore selected snapshot implemented and verified
- local `Phase 12S7` - snapshot comparison implemented and verified
- `Phase 12S8` - change history accepted after manual review and committed/pushed on `to`

Next approved work:

- no `Phase 7G` is approved in `Roadmap.md`
- current gate: define the next roadmap task before coding further
- no next coding phase is explicitly prioritized yet

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
- generated maintenance workbooks are report artifacts; current builds default to `.akb` storage and keep JSON for import/export and migration compatibility
- production-calendar years are configured in `Config.ProductionCalendarYears` through `Файл -> Производственный календарь...`, JSON import, or local PDF import
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
