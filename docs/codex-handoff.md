# Current State

Last updated: `2026-04-28`

## Repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active integration branch: `interface`
- Implemented on this branch: `Phase 0`, `Phase 1`, `Phase 2`, `Phase 3`, `Phase 3B`, `Phase 4`, `Phase 5`
- Next unfinished roadmap phase: `Phase 6`

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
- User-facing program UI on `interface` is now Russian-only; future UI changes should keep labels, prompts, dialogs, and status text in Russian
- Deleting a node removes typed composition, document links, and software records for the whole deleted subtree
- `summary.md` is only a pointer into the docs harness and is not a second current-state source

## Validated status

Actually run on the current worktree on `2026-04-28`:

```powershell
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore -p:BaseOutputPath=C:\Users\Olga\AKB5\artifacts\verify\build\
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```

- Verification `dotnet build`: passed
- `dotnet test`: passed, `171/171`
- The build used an isolated output path because a running local app instance can lock the default `bin\Release` outputs
- User manual verification on `2026-04-28`: no obvious bugs were found in the visible `Phase 5` search workflow before the later UI-localization pass
- `dotnet format --verify-no-changes` was not rerun after the latest localization edits
- Existing warnings remain, including `NU1900` when NuGet vulnerability metadata is unavailable offline

## Active objective

- Continue roadmap implementation from `Phase 6`
- Replace the current `Network` placeholder with a typed file-based workflow while preserving JSON source-of-truth and Excel `v3` compatibility

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
- `docs/codex-handoff.md` remains the single current-state file for future sessions

## Knowledge harness

- Current state: `docs/codex-handoff.md`
- Active plans: `docs/plans.md`
- Reusable insights: `docs/lessons-learned.md`
- Durable decisions: `docs/decision-log.md`
- On the explicit command `РґРёСЃС‚РёР»Р»РёСЂСѓР№ Р·РЅР°РЅРёСЏ РёР· СЃРµСЃСЃРёРё`, update those files in place and replace stale information instead of appending transcripts

## Relevant files for the current task area

- `Forms/MainForm.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.Events.cs`
- `Forms/MainForm.WorkspaceHost.cs`
- `Forms/MainForm.NodeDetails.cs`
- `Controls/KnowledgeBaseInfoScreenControl.cs`
- `Services/KnowledgeBaseNodeWorkspaceResolverService.cs`
- `Services/KnowledgeBaseFormStateService.cs`
- `UiServices/KnowledgeBaseFileUiWorkflowService.cs`
- `Models/SavedData.cs`
- `Roadmap.md`

## Known limits / open follow-up

- `Phase 6` has not started yet; the `Network` tab is still a placeholder/description-only surface
- The current docs/software UI is intentionally limited to supported engineering node types and does not yet cover all node categories
- `copy composition from existing object` currently copies only typed composition entries; it does not copy docs/software records
- A standard `dotnet build` into the default `Release` output can fail if `asutpKB.exe` is still running and holding DLL locks

## Recommended next step

- Start `Phase 6` network work from `Roadmap.md`
- Add typed network file references for supported engineering nodes
- Provide large in-form image preview and an `Open original` action
- Reuse existing photo/open workflow patterns before introducing new preview dependencies
- Keep all new user-facing strings in Russian

## Commands to run before finishing future implementation work

```powershell
git status --short
# Close running asutpKB.exe first if you want to build into the default Release output.
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```
