# Current State

Last updated: `2026-04-29`

## Repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active integration branch: `interface`
- Implemented on this branch: `Phase 0`, `Phase 1`, `Phase 2`, `Phase 3`, `Phase 3B`, `Phase 4`, `Phase 5`, `Phase 6`, `Phase 7A foundation`
- Next unfinished roadmap phase: `Phase 7` planner/export continuation

## Integrated feature state

- `Phase 3` is active in the branch:
  - typed composition entries live in `SavedData.CompositionEntries`
  - the `Composition` screen separates slots from auxiliary equipment
  - ordering is driven by `SlotNumber` + `PositionOrder`, not by left-tree child order
- `Phase 3B` is active in the branch:
  - built-in cabinet/controller composition templates are available
  - `copy composition from existing object` duplicates typed composition entries between nodes of the same `NodeType`
- `Phase 4` is active in the branch:
  - typed documentation/software models live in `Models/KbDocumentKind.cs`, `Models/KbDocumentLink.cs`, and `Models/KbSoftwareRecord.cs`
  - top-level JSON/session persistence includes `DocumentLinks` and `SoftwareRecords`
  - `Documentation and Software` is wired through `Controls/KnowledgeBaseDocsAndSoftwareScreenControl.cs`, `Forms/MainForm.DocsAndSoftware.cs`, and typed edit dialogs
  - the tab is currently available for `Cabinet`, `Device`, `Controller`, and `Module`
  - the UI is intentionally not a clone of `Composition`; it manages three link catalogs:
    - schemes
    - instructions
    - software folders
  - software links record `AddedAt` in the user-facing workflow
  - legacy software timestamps/notes remain compatibility-only persistence fields and are not exposed in the main editing UI
- `Phase 5` is active in the branch:
  - search is no longer name-only
  - indexed matches now cover `Tree`, `Card`, `Composition`, and `Docs/Software`
  - search scopes are `All`, `Tree`, `Card`, `Composition`, and `Docs/Software`
  - search navigation resolves to the owning tree node and preferred workspace tab
- `Phase 6` is active in the branch:
  - typed network file references live in `Models/KbNetworkFileReference.cs` and `Models/KbNetworkPreviewKind.cs`
  - top-level JSON/session persistence includes `NetworkFileReferences`
  - the `Network` workflow is wired through `Controls/KnowledgeBaseNetworkScreenControl.cs`, `Forms/MainForm.Network.cs`, and `Forms/KnowledgeBaseNetworkFileReferenceDialog.cs`
  - the first release is file-based, not interactive-topology-based
  - embedded preview is currently limited to `jpg`, `jpeg`, `png`, `bmp`, and `gif`
  - non-image files stay metadata-only in-form and rely on `Open original`
  - the screen uses separate `Файлы` and `Предпросмотр` tabs; loading a node opens `Файлы`, and automatic switching to `Предпросмотр` is not part of the accepted UX
- `Phase 7A foundation` is active in the branch:
  - `Lvl2` inventory number visibility now follows hierarchy level instead of relying on `NodeType.System`
  - typed maintenance settings live in top-level `SavedData.MaintenanceScheduleProfiles`
  - selected-node state exposes a dedicated maintenance view model through `KnowledgeBaseMaintenanceScheduleStateService`
  - engineering nodes (`Cabinet`, `Device`, `Controller`, `Module`) now have a `График ТО` workspace tab
  - one maintenance profile is stored per owner node and contains `IsIncludedInSchedule` plus separate integer hour norms for `ТО1`, `ТО2`, and `ТО3`
- User-facing program UI on `interface` is now Russian-only; future UI changes should keep labels, prompts, dialogs, and status text in Russian
- Deleting a node removes typed composition, document links, software records, network file references, and maintenance schedule profiles for the whole deleted subtree
- `summary.md` is only a pointer into the docs harness and is not a second current-state source

## Validated status

Actually run on the current worktree on `2026-04-28`:

```powershell
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release -p:BaseOutputPath=C:\Users\Olga\AKB5\artifacts\verify\phase7-build\
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release -p:BaseOutputPath=C:\Users\Olga\AKB5\artifacts\verify\phase7-test\
```

- Verification `dotnet build`: passed
- `dotnet test`: passed, `192/192`
- An earlier attempt to build and test in parallel produced a transient file-lock failure on shared `Release` outputs; the final verification was rerun sequentially and passed
- The build used isolated output paths because a running local app instance or a concurrent verification pass can lock the default `bin\Release` outputs
- `dotnet format --verify-no-changes` was not rerun after the latest `Phase 7A` changes
- Existing warnings remain, including `NU1900` when NuGet vulnerability metadata is unavailable offline

## Active objective

- Continue `Phase 7` from the finished `Phase 7A foundation` slice
- Prepare a cleaned internal Excel template derived from `C:\Users\Olga\Downloads\123.xlsx`
- Implement the Russian production-calendar service, monthly planner, and workbook export using the approved enterprise form

## Durable decisions already made

- `interface` remains the active integration branch
- Use tree taxonomy:
  - `L1` = department
  - `L2` = system
  - `L3` = cabinet
- `Phase 3B` templates remain a built-in code catalog; do not introduce template storage in JSON or Excel unless that becomes an explicit task
- `Phase 4` documentation/software records are stored as top-level `DocumentLinks` and `SoftwareRecords` collections keyed by `OwnerNodeId`
- `Phase 4` keeps JSON schema version at `3` and leaves the Excel workbook contract at `v3`
- `Phase 5` search scopes are fixed to `All`, `Tree`, `Card`, `Composition`, and `Docs/Software`
- Search results must continue to navigate to the owning tree node instead of to detached screen-only records
- User-facing program UI should use Russian only
- `Phase 6` `Network` stays file-based in the first release; do not expand it into an interactive topology editor inside this roadmap slice
- `Phase 6` preview support is intentionally limited to image formats already covered by the in-form workflow; non-image files keep `Open original`
- `Phase 6` opens `Файлы` on node load by default; automatic switching to `Предпросмотр` is not part of the accepted UX
- The old `Phase 7` Excel/exchange-modernization target is superseded by maintenance-schedule generation
- The first maintenance-schedule release should use a yearly accumulating workbook with `12` month sheets based on the approved sample form
- Maintenance planning rules currently fixed for implementation:
  - planning unit is a tree node
  - `Lvl2` node becomes the numbered parent row with inventory number
  - child engineering nodes become the `план/факт` detail rows
  - the application generates only `план`; `факт` remains blank for manual filling on the printed form
  - `ТО1` is monthly, `ТО2` is semiannual, `ТО3` is annual
  - planned nodes need separate integer hour norms for `ТО1`, `ТО2`, and `ТО3`
  - stored `ТО` hour norms are not capped at `8`; the `<= 8` rule belongs to later per-day planner allocation
  - until a formal yearly schedule source exists, `ТО2` / `ТО3` placement should come from a deterministic per-node cycle offset
  - inconsistencies in the historical sample workbook are treated as manual noise, not as the rule source
- `Phase 7A foundation` stores maintenance data as top-level `MaintenanceScheduleProfiles` keyed by `OwnerNodeId`
- Saved-data normalization must keep at most one maintenance profile per `OwnerNodeId`
- `docs/codex-handoff.md` remains the single current-state file for future sessions

## Knowledge harness

- Current state: `docs/codex-handoff.md`
- Active plans: `docs/plans.md`
- Reusable insights: `docs/lessons-learned.md`
- Durable decisions: `docs/decision-log.md`
- On the explicit user request to distill session knowledge, update those files in place and replace stale information instead of appending transcripts

## Relevant files for the current task area

- `Models/KbMaintenanceScheduleProfile.cs`
- `Models/SavedData.cs`
- `Services/JsonStorageService.cs`
- `Services/KnowledgeBaseDataService.cs`
- `Services/KnowledgeBaseFormStateService.cs`
- `Services/KnowledgeBaseMaintenanceScheduleProfileMutationService.cs`
- `Services/KnowledgeBaseMaintenanceScheduleStateService.cs`
- `Services/KnowledgeBaseNodeWorkspaceResolverService.cs`
- `Forms/MainForm.cs`
- `Forms/MainForm.Maintenance.cs`
- `Controls/KnowledgeBaseMaintenanceScheduleScreenControl.cs`
- `Forms/KnowledgeBaseMaintenanceScheduleProfileDialog.cs`
- `Services/KnowledgeBaseExcelExchangeService.cs`
- `Services/KnowledgeBaseExcelWorkbookParser.cs`
- `Services/KnowledgeBaseXlsxReader.cs`
- `Services/KnowledgeBaseXlsxWriter.cs`
- `C:\Users\Olga\Downloads\123.xlsx`
- `docs/workbook-v3.md`
- `Roadmap.md`

## Known limits / open follow-up

- `Phase 7` is only partially implemented; the `Phase 7A foundation` data/UI slice is done, but calendar allocation, workbook planning, and export are still unfinished
- The current maintenance UI is intentionally limited to supported engineering node types and does not yet cover all node categories
- `copy composition from existing object` currently copies only typed composition entries; it does not copy docs/software records, network file references, or maintenance profiles
- `Network` does not provide embedded PDF preview or interactive topology editing in the current phase
- A standard `dotnet build` into the default `Release` output can fail if `asutpKB.exe` is still running and holding DLL locks
- A future externally provided yearly maintenance schedule may later replace the temporary rule-based source of `ТО1` / `ТО2` / `ТО3` month placement

## Recommended next step

- Continue `Phase 7` from the finished `Phase 7A foundation` slice
- Prepare a cleaned internal Excel template derived from `123.xlsx`
- Implement the Russian production-calendar service and monthly planner on top of `MaintenanceScheduleProfiles`
- After planner rules are stable, generate the yearly workbook export with blank `факт` cells
- Keep all new user-facing strings in Russian

## Commands to run before finishing future implementation work

```powershell
git status --short
# Close running asutpKB.exe first if you want to build into the default Release output.
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release -p:BaseOutputPath=C:\Users\Olga\AKB5\artifacts\verify\phase7-build\
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release -p:BaseOutputPath=C:\Users\Olga\AKB5\artifacts\verify\phase7-test\
```
