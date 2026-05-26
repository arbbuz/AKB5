# Current State

Last updated: `2026-05-26`

## Current objective

Current active work is the `Сеть` topology extension on `Net`: add the user-selected `ET200` and `OLM` topology element options and add selectable link types matching the supplied network scheme legend.

Implemented in the current uncommitted package:

- `I/O` is replaced in the Network UI by `ET200`; the old enum value `6` remains as an obsolete `Io` alias and now normalizes/renders as `Et200`.
- `OLM` is added as a new topology element kind.
- Device graphics follow the selected preview choices: `ET200-A` modular station and `OLM-C` two-optoport module.
- `KbNetworkLinkKind` adds five link types from the screenshot: optical Profibus, copper Profibus, copper MPI, optical Profinet, copper Profinet.
- Existing saved links without an explicit type remain compatible and default to copper Profinet.
- New links take the currently selected toolbar link type; right-clicking an existing link offers a `Тип связи` submenu to change its type.
- Link rendering now uses the screenshot style: magenta dashed/solid for Profibus, red solid for MPI, green dashed/solid for Profinet.

Do not commit, push, merge, rebase, or create/remove remote branches without explicit approval in the current chat.

## Current repo state

- Main worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Local `Net` was clean and aligned to `origin/Net` before the current uncommitted Network extension package.
- Latest pushed commit before this package: `e6fa973 Rebalance maintenance monthly loads`.
- No real `.akb` or JSON user data files were edited.
- Current review executable for this package: `C:\Users\Olga\AKB5\artifacts\build-check\network-et200-olm-links\asutpKB.exe`.
- Smoke preview from the real WinForms control: `C:\Users\Olga\AKB5\artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`.

## Current package

Changed tracked files:

- `Controls/KnowledgeBaseNetworkTopologyScreenControl.cs`
- `Models/KbNetworkTopology.cs`
- `Services/KnowledgeBaseDataService.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseDataServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/SqliteKnowledgeBaseStorageServiceTests.cs`
- `docs/codex-handoff.md`
- `docs/plans.md`

Ignored validation artifacts:

- `artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj`
- `artifacts\layout-smoke\network-topology-extensions\Program.cs`
- `artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`
- `artifacts\build-check\network-et200-olm-links\asutpKB.exe`

## Validation status

Validation completed in `C:\Users\Olga\AKB5`:

- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~KnowledgeBaseDataServiceTests|FullyQualifiedName~SqliteKnowledgeBaseStorageServiceTests" /p:RunAnalyzers=false /p:WarningLevel=0`: passed, 41 tests.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-et200-olm-links /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- `dotnet run --project artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj --configuration Release /p:RunAnalyzers=false /p:WarningLevel=0`: passed; produced the smoke PNG listed above and counted visible dark/magenta/red/green pixels.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed, 407 tests.
- `git diff --check`: passed with CRLF normalization warnings only.

## Decisions already made

- Work only on `Net / origin/Net`.
- Do not commit or push without fresh direct approval in the current chat.
- Keep the Level 2 Network topology scope; do not broaden into PRONETA/CSV import, live scan, OCR/PDF import, plan/fact comparison, IP assignment automation, or embedded PDF preview.
- Do not reintroduce hover/popup tooltips. Use visible labels, inline validation/status text, or modal validation messages instead.
- User selected `ET200-A` for ET200, `OLM-C` for OLM, and the five screenshot link styles for topology links.

## Files already relevant to the task

- `Controls/KnowledgeBaseNetworkTopologyScreenControl.cs`
- `Models/KbNetworkTopology.cs`
- `Services/KnowledgeBaseDataService.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseDataServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/SqliteKnowledgeBaseStorageServiceTests.cs`
- `docs/codex-handoff.md`
- `docs/plans.md`

## Known risks / open questions

- Manual review in the real app is still needed for toolbar/dropdown ergonomics and the exact visual weight of the ET200/OLM icons.
- Existing persisted links with no `Kind` property intentionally render as copper Profinet.
- The smoke test verifies nonblank rendering and link colors; it does not replace operator acceptance of the network diagram workflow.

## Recommended next step

Manual review the Network topology extension through `C:\Users\Olga\AKB5\artifacts\build-check\network-et200-olm-links\asutpKB.exe`. If accepted, request a fresh commit/push for the current `Net` changes.

## Commands to run before finishing future implementation work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-et200-olm-links /p:RunAnalyzers=false /p:WarningLevel=0
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0
dotnet run --project artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj --configuration Release /p:RunAnalyzers=false /p:WarningLevel=0
git diff --check
```
