# Current State

Last updated: `2026-04-28`

## Repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active integration branch: `interface`
- Implemented on this branch: `Phase 0`, `Phase 1`, `Phase 2`, `Phase 3`, `Phase 3B`, `Phase 4`
- Next unfinished roadmap phase: `Phase 5`

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
- Deleting a node removes typed composition, document links, and software records for the whole deleted subtree
- `summary.md` is only a pointer into the docs harness and is not a second current-state source

## Validated status

Actually run on the current worktree on `2026-04-28`:

```powershell
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```

- `dotnet build`: passed
- `dotnet test`: passed, `169/169`
- Local manual verification on `2026-04-28`: no obvious bugs were found in the visible `Phase 4` workflow
- `dotnet format --verify-no-changes` was not rerun after the latest `Phase 4` stabilization edits
- Existing warnings remain, including `NU1900` when NuGet vulnerability metadata is unavailable offline

## Active objective

- Continue roadmap implementation from `Phase 5`
- Redesign search around actual data domains while keeping JSON source-of-truth and Excel `v3` compatibility intact

## Durable decisions already made

- `interface` remains the active integration branch
- Use tree taxonomy:
  - `L1` = department
  - `L2` = system
  - `L3` = cabinet
- `Phase 3B` templates remain a built-in code catalog; do not introduce template storage in JSON or Excel unless that becomes an explicit task
- `Phase 4` documentation/software records are stored as top-level `DocumentLinks` and `SoftwareRecords` collections keyed by `OwnerNodeId`
- `Phase 4` keeps JSON schema version at `3` and leaves the Excel workbook contract at `v3`
- `Documentation and Software` remains limited to supported engineering node types already using the right-panel workspace host
- `docs/codex-handoff.md` remains the single current-state file for future sessions

## Knowledge harness

- Current state: `docs/codex-handoff.md`
- Active plans: `docs/plans.md`
- Reusable insights: `docs/lessons-learned.md`
- Durable decisions: `docs/decision-log.md`
- On the explicit command `дистиллируй знания из сессии`, update those files in place and replace stale information instead of appending transcripts

## Relevant files for the current task area

- `Forms/MainForm.cs`
- `Forms/MainForm.Events.cs`
- `Forms/MainForm.Composition.cs`
- `Forms/MainForm.DocsAndSoftware.cs`
- `Controls/KnowledgeBaseCompositionScreenControl.cs`
- `Controls/KnowledgeBaseDocsAndSoftwareScreenControl.cs`
- `Forms/KnowledgeBaseCompositionEntryDialog.cs`
- `Forms/KnowledgeBaseCompositionTemplateDialog.cs`
- `Forms/KnowledgeBaseCompositionCopySourceDialog.cs`
- `Forms/KnowledgeBaseDocumentLinkDialog.cs`
- `Forms/KnowledgeBaseSoftwareRecordDialog.cs`
- `Services/KnowledgeBaseCompositionStateService.cs`
- `Services/KnowledgeBaseCompositionTemplateService.cs`
- `Services/KnowledgeBaseDocsAndSoftwareStateService.cs`
- `Services/KnowledgeBaseDocsAndSoftwareMutationService.cs`
- `Services/KnowledgeBaseTreeMutationWorkflowService.cs`
- `Services/KnowledgeBaseSessionService.cs`
- `Models/SavedData.cs`
- `Roadmap.md`

## Known limits / open follow-up

- The current docs/software UI is intentionally limited to supported engineering node types and does not yet cover all node categories
- `copy composition from existing object` currently copies only typed composition entries; it does not copy docs/software records
- `Phase 5` search redesign has not started yet; current search still matches node names only

## Recommended next step

- Start `Phase 5` search redesign from `Roadmap.md`
- Introduce indexed search across `Tree`, `Card`, `Composition`, and `Docs/Software`
- Expose scope-aware search that still navigates back to the owning tree node

## Commands to run before finishing future implementation work

```powershell
git status --short
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
$env:DOTNET_CLI_HOME='C:\Users\Olga\AKB5\.dotnet-cli'; dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```
