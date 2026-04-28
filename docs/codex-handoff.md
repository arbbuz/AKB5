# Current State

Last updated: `2026-04-28`

## Repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active integration branch: `interface`
- Implemented on this branch: `Phase 0`, `Phase 1`, `Phase 2`, `Phase 3`, `Phase 3B`, `Phase 4`, `Phase 5`, `Phase 6`
- Next unfinished roadmap phase: `Phase 7`

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
- User-facing program UI on `interface` is now Russian-only; future UI changes should keep labels, prompts, dialogs, and status text in Russian
- Deleting a node removes typed composition, document links, software records, and network file references for the whole deleted subtree
- `summary.md` is only a pointer into the docs harness and is not a second current-state source

## Validated status

Actually run on the current worktree on `2026-04-28`:

```powershell
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore -p:BaseOutputPath=C:\Users\Olga\AKB5\artifacts\verify\build\
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```

- Verification `dotnet build`: passed
- `dotnet test`: passed, `177/177`
- The build used an isolated output path because a running local app instance can lock the default `bin\Release` outputs
- A standard `Release` build into the default output was also rechecked after the later `Phase 6` UX fixes
- `asutpKB.exe` startup was checked after the final `Phase 6` `Network`-tab layout changes
- `dotnet format --verify-no-changes` was not rerun after the latest `Phase 6` changes
- Existing warnings remain, including `NU1900` when NuGet vulnerability metadata is unavailable offline

## Active objective

- Continue roadmap implementation from `Phase 7`
- Replace the old workbook-modernization direction with maintenance-schedule generation while keeping JSON source-of-truth
- Build a template-driven yearly workbook for monthly maintenance schedules using the approved enterprise form

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
  - planned nodes will need separate integer hour norms for `ТО1`, `ТО2`, and `ТО3`
  - until a formal yearly schedule source exists, `ТО2` / `ТО3` placement should come from a deterministic per-node cycle offset
  - inconsistencies in the historical sample workbook are treated as manual noise, not as the rule source
- `docs/codex-handoff.md` remains the single current-state file for future sessions

## Knowledge harness

- Current state: `docs/codex-handoff.md`
- Active plans: `docs/plans.md`
- Reusable insights: `docs/lessons-learned.md`
- Durable decisions: `docs/decision-log.md`
- On the explicit user request to distill session knowledge, update those files in place and replace stale information instead of appending transcripts

## Relevant files for the current task area

- `Models/SavedData.cs`
- `Services/JsonStorageService.cs`
- `Services/KnowledgeBaseDataService.cs`
- `Services/KnowledgeBaseExcelExchangeService.cs`
- `Services/KnowledgeBaseExcelWorkbookParser.cs`
- `Services/KnowledgeBaseXlsxReader.cs`
- `Services/KnowledgeBaseXlsxWriter.cs`
- `Services/KnowledgeBaseSessionService.cs`
- `C:\Users\Olga\Downloads\123.xlsx`
- `docs/workbook-v3.md`
- `Roadmap.md`

## Known limits / open follow-up

- `Phase 7` implementation has not started yet; only the business direction and the sample-form analysis are fixed
- The current docs/software UI is intentionally limited to supported engineering node types and does not yet cover all node categories
- `copy composition from existing object` currently copies only typed composition entries; it does not copy docs/software records or network file references
- `Network` does not provide embedded PDF preview or interactive topology editing in the current phase
- A standard `dotnet build` into the default `Release` output can fail if `asutpKB.exe` is still running and holding DLL locks
- A future externally provided yearly maintenance schedule may later replace the temporary rule-based source of `ТО1` / `ТО2` / `ТО3` month placement

## Recommended next step

- Start `Phase 7` maintenance-schedule work from `Roadmap.md`
- Add the `Lvl2` inventory-number field and define typed maintenance settings for planned nodes
- Prepare a cleaned internal Excel template derived from `123.xlsx`
- Keep all new user-facing strings in Russian

## Commands to run before finishing future implementation work

```powershell
git status --short
# Close running asutpKB.exe first if you want to build into the default Release output.
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```
