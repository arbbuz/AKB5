# Latest update

- Date: `2026-04-26`
- Local Windows repo for the current session: `C:\Users\Olga\AKB5`
- Active working branch: `interface`
- Current local branch state:
  - local `HEAD`: `f8914eb` (`Revert "Restore tree chevrons without regressing window drag"`)
  - branch is ahead of `origin/interface` by `2` local revert commits
  - local documentation-only edits are present in `AGENTS.md`, `Roadmap.md`, and `docs/codex-handoff.md`
- Code baseline after the rollback:
  - `git diff --name-only ec5c04e HEAD` is empty
  - application code now matches commit `ec5c04e` exactly
  - this means the current code baseline is the `Windows Build 86` application state
- Roadmap status on that code baseline:
  - `Phase 0` complete
  - `Phase 1` complete
  - `Phase 2` complete
  - `Phase 3` complete
  - next unfinished roadmap phase: `Phase 3B`
- Important rollback fact:
  - the later local/pushed experiments from `050a946` and `4fcb6a1` are intentionally not part of the current code state anymore
  - this includes the later tree-chevron/tree-visual and drag/resize stabilization work
- Validation status:
  - no new `build` / `test` / `format` run was executed after the rollback in this session
  - the only hard guarantee for the rollback itself is code equality to `ec5c04e`

# Current objective

- When coding resumes, continue from `Phase 3B` only, but from the restored `ec5c04e` / `Windows Build 86` code baseline.
- Keep JSON as the source of truth.
- Preserve Excel workbook `v3` readability/compatibility during typed-feature growth.
- Do not reintroduce reverted drag/tree experiments casually; if that area is revisited later, treat it as a separate stabilization task with fresh validation.

# Current repo state

- `AKB5` remains a WinForms desktop app on `.NET 8`.
- `Phase 0` is complete on `interface`:
  - user-facing level configuration is removed from the ordinary UX
  - `LevelIndex` and hidden depth constraints remain internal technical mechanisms
- `Phase 1` is complete on `interface`:
  - `KbNode` has persistent `NodeId`
  - `KbNode` has `NodeType`
  - `SavedData.CurrentSchemaVersion` is `3`
  - legacy JSON/schema data is normalized through `KnowledgeBaseDataService.NormalizeSavedData(...)`
  - hidden workshop wrappers are recognized through explicit `NodeType.WorkshopRoot`
- `Phase 2` is complete on `interface`:
  - the right panel routes by `NodeType`
  - `Department` / `System` / safe legacy cases stay on a clean `Info` screen
  - `Cabinet` / `Device` / `Controller` / `Module` use a tab host
  - reusable `Info` UI lives in `Controls/KnowledgeBaseInfoScreenControl.cs`
- `Phase 3` is complete on `interface`:
  - `SavedData` contains typed `CompositionEntries`
  - composition is resolved independently from left-tree child order
  - slot order is positional via `SlotNumber` + `PositionOrder`
  - the `Composition` screen supports add/edit/delete through a dedicated dialog
- Current tree/UI baseline after the rollback:
  - `Controls/KnowledgeBaseTreeView.cs` is back to the older custom owner-draw implementation from `ec5c04e`
  - `Forms/MainForm.cs` no longer contains the later `WM_ENTERSIZEMOVE` / `WM_EXITSIZEMOVE` redraw-suppression logic
  - later custom `StateImageList` chevron work is not part of the current code state
- Current Excel state:
  - workbook format stays `v3`
  - Excel reads/writes `NodeId`
  - Excel writes/reads a read-only `NodeType` column as part of the transition
  - typed composition entries are still JSON-only and do not round-trip through workbook `v3`
- Current CI workflow state:
  - GitHub workflow `build-and-test` verifies `dotnet format --verify-no-changes` for app/core/tests before build/test
  - the formatting fix from `4fcb6a1` was rolled back together with the later code experiments, so CI should be rechecked if this rollback is later pushed

# Decisions already made

- Keep WinForms architecture; do not rewrite the app.
- Keep JSON storage as the primary persisted model.
- Keep Excel `v3` as a legacy transition exchange format.
- Keep `LevelIndex` as an internal technical coordinate only.
- Use `NodeType` instead of `LevelIndex >= 2` when deciding whether technical fields are applicable.
- Preserve existing node IDs when loading/importing data that already has them.
- Generate missing `NodeId` values during normalization/migration instead of rejecting legacy data.
- Copy/paste should preserve structure and `NodeType`, but must assign new `NodeId` values.
- The current code baseline is intentionally `ec5c04e`; later tree/drag work is not considered active anymore.

# Files already relevant to the task

- `Forms/MainForm.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.WorkspaceHost.cs`
- `Forms/MainForm.NodeDetails.cs`
- `Forms/MainForm.Composition.cs`
- `Controls/KnowledgeBaseTreeView.cs`
- `Controls/KnowledgeBaseInfoScreenControl.cs`
- `Controls/KnowledgeBaseCompositionScreenControl.cs`
- `Forms/KnowledgeBaseCompositionEntryDialog.cs`
- `UiServices/KnowledgeBaseTreeNodeVisuals.cs`
- `UiServices/KnowledgeBaseTreeViewService.cs`
- `Models/KbNode.cs`
- `Models/KbNodeType.cs`
- `Models/SavedData.cs`
- `Services/KnowledgeBaseNodeMetadataService.cs`
- `Services/KnowledgeBaseDataService.cs`
- `Services/JsonStorageService.cs`
- `Services/KnowledgeBaseService.cs`
- `Services/KnowledgeBaseSessionService.cs`
- `Services/KnowledgeBaseTreeController.cs`
- `Services/KnowledgeBaseWorkshopTreeProjection.cs`
- `Services/KnowledgeBaseFormStateService.cs`
- `Services/KnowledgeBaseCompositionStateService.cs`
- `Services/KnowledgeBaseCompositionMutationService.cs`
- `Services/KnowledgeBaseExcelWorkbookParser.cs`
- `Services/KnowledgeBaseXlsxWriter.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseDataServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/JsonStorageServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseFormStateServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseCompositionStateServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseCompositionMutationServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseExcelExchangeServiceTests.cs`
- `Roadmap.md`

# Known risks / open questions

- `Phase 3B` template/copy workflows do not exist yet. Composition still has to be entered manually.
- `Documentation and Software` and `Network` are still placeholder tabs and belong to later roadmap phases.
- Excel workbook `v3` still does not carry typed composition entries; JSON remains the only source of truth for them.
- Because the branch was rolled back to `ec5c04e`, the later attempts to mitigate:
  - windowed drag lag / cursor ghosting
  - custom lightweight tree chevrons/icons
  are no longer present in code and should be considered unresolved unless reimplemented later.
- If this rollback is pushed, GitHub Actions may fail again on the format-verification step because commit `4fcb6a1` was also rolled back.
- `README.md` still does not fully reflect the typed foundation, screen host, composition workflow, and the current rollback baseline.

# Recommended next step

- If the intent is to keep the `Windows Build 86` baseline, either:
  - push the two local revert commits as-is
  - or continue feature work locally from this reverted state
- For roadmap work, the next unfinished implementation phase is still `Phase 3B`:
  - add cabinet/controller templates
  - add `create from template`
  - add `copy composition from existing object`
  - keep JSON source-of-truth compatibility and workbook `v3` readability intact

# Commands to run before finishing future implementation work

```powershell
dotnet format C:\Users\Olga\AKB5\asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format C:\Users\Olga\AKB5\src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```
