# AKB5

## Overview

`AKB5` is a WinForms application on C# / .NET 8 for maintaining an engineering knowledge base for industrial automation systems.

Current direction of the project:

- the left side is a physical object tree
- the right side is a type-driven workspace resolved by `NodeType`
- JSON remains the source of truth
- Excel workbook `v3` remains a legacy transition exchange format
- user-facing program UI is Russian-only

The active integration branch is `interface`.

## Current implementation state

Implemented on `interface`:

- `Phase 0` - user-facing levels removed from the main UX
- `Phase 1` - persistent `NodeId` / `NodeType` foundation and migration
- `Phase 2` - right-panel workspace host
- `Phase 3` - typed `Composition`
- `Phase 3B` - composition templates and copy-from-existing-object
- `Phase 4` - typed `Documentation and Software`
- `Phase 5` - scoped search across `Tree`, `Card`, `Composition`, and `Docs/Software`

Next roadmap phase:

- `Phase 6` - file-based `Network` tab

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

Important persistence rules:

- JSON is the primary source of truth
- typed cross-links must use stable IDs, never node names or paths
- Excel `v3` must stay readable during the transition

## Search

Current search behavior on `interface`:

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
dotnet format asutpKB.csproj --verify-no-changes --severity warn --no-restore
dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity warn --no-restore
dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity warn --no-restore
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
