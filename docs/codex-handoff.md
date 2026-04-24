# Latest update

- Local Windows repo for the current session: `C:\Users\Olga\AKB5`
- Active working branch: `interface`
- `Phase 0` is complete locally.
- `Phase 1` is complete locally and validated on the current worktree.
- `Phase 2` is complete locally and validated on the current worktree.
- `Phase 3` is complete locally and validated on the current worktree.
- Latest validation in this session:
  - `dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore`
  - `dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore`
  - automated result: `156/156`

# Current objective

- Start `Phase 3B` template/copy workflow work from the now-complete typed `Composition` screen.
- Keep JSON as the source of truth.
- Preserve Excel workbook `v3` compatibility while typed data is introduced incrementally.

# Current repo state

- `AKB5` remains a WinForms desktop app on `.NET 8`.
- The left tree still uses the hidden workshop-root projection, but the wrapper is now recognized by explicit `NodeType.WorkshopRoot` instead of the old shape-only heuristic.
- The domain model now contains:
  - `KbNode.NodeId`
  - `KbNode.NodeType`
- `SavedData.CurrentSchemaVersion` is now `3`.
- Legacy JSON/schema data is normalized through `KnowledgeBaseDataService.NormalizeSavedData(...)`.
- Missing legacy node IDs are generated deterministically from workshop name + sibling path.
- New copied/pasted nodes receive fresh GUID-based `NodeId` values.
- `NodeType` now drives:
  - technical-field visibility and cleanup in form-state/UI logic
  - Excel import/export technical-field behavior
  - tree icon selection
- The right panel now routes by `NodeType`:
  - `Department` / `System` / legacy-safe cases stay on a clean `Info` screen
  - `Cabinet` / `Device` / `Controller` / `Module` switch to a tab host with live `Composition`, plus placeholder `Documentation and Software` and `Network` tabs
- The generic `Info` screen now lives in reusable `Controls/KnowledgeBaseInfoScreenControl.cs` and is reparented between the standalone host and the `Info` tab instead of being hardcoded directly inside `MainForm`.
- `SavedData` now contains `CompositionEntries`, and session/file workflow paths persist typed composition independently from tree child order.
- The `Composition` tab now:
  - resolves typed entries from `SavedData.CompositionEntries`
  - sorts them by `SlotNumber` and `PositionOrder`
  - renders slots separately from auxiliary equipment
  - keeps legacy child nodes as a read-only fallback projection until typed entries are filled
  - supports add/edit/delete through a dedicated composition-entry dialog
- Excel `v3` now reads and writes a read-only `NodeType` column while keeping `Levels` as a legacy transition sheet.

# Decisions already made

- Keep WinForms architecture; do not rewrite the app.
- Keep JSON storage as the primary persisted model.
- Keep Excel `v3` as a legacy transition exchange format.
- Keep `LevelIndex` as an internal technical coordinate only.
- Use `NodeType` instead of `LevelIndex >= 2` when deciding whether technical fields are applicable.
- Preserve existing node IDs when loading/importing data that already has them.
- Generate missing `NodeId` values during normalization/migration instead of rejecting legacy data.
- Copy/paste should preserve structure and `NodeType`, but must assign new `NodeId` values.

# Files already relevant to the task

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
- `Services/KnowledgeBaseExcelWorkbookParser.cs`
- `Services/KnowledgeBaseXlsxWriter.cs`
- `Services/KnowledgeBaseFormStateService.cs`
- `Services/KnowledgeBaseCompositionStateService.cs`
- `Services/KnowledgeBaseCompositionMutationService.cs`
- `Forms/MainForm.NodeDetails.cs`
- `Forms/MainForm.Composition.cs`
- `Forms/MainForm.Events.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.WorkspaceHost.cs`
- `Forms/KnowledgeBaseCompositionEntryDialog.cs`
- `Controls/KnowledgeBaseCompositionScreenControl.cs`
- `Controls/KnowledgeBaseInfoScreenControl.cs`
- `UiServices/KnowledgeBaseTreeNodeVisuals.cs`
- `UiServices/KnowledgeBaseTreeViewService.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseDataServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/JsonStorageServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseExcelExchangeServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseFormStateServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseCompositionStateServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseCompositionMutationServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseServiceTests.cs`
- `Roadmap.md`

# Known risks / open questions

- `Phase 3` is complete locally, but `Phase 3B` template/copy workflows do not exist yet. Composition still has to be entered manually.
- `Documentation and Software` and `Network` are still placeholder tabs and belong to later roadmap phases.
- Excel `v3` compatibility is preserved, but workbook exchange still does not carry typed composition entries; JSON remains the only source of truth for them.
- `README.md` still needs a later refresh if the user wants public-facing docs to match the current typed foundation and `Composition` workflow.

# Recommended next step

- Start `Phase 3B`:
  - add cabinet/controller templates
  - add `create from template`
  - add `copy composition from existing object`
  - keep JSON source-of-truth compatibility and workbook `v3` readability intact
- Refresh `README.md` later if user-facing docs also need to reflect the typed foundation.

# Commands to run before finishing future implementation work

```powershell
dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```
