# Latest update

- Local Windows repo for the current session: `C:\Users\Olga\AKB5`
- Active working branch: `interface`
- `Phase 0` is complete locally.
- `Phase 1` is complete locally and validated on the current worktree.
- Latest validation in this session:
  - `dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore`
  - `dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore`
  - automated result: `143/143`

# Current objective

- Start `Phase 2` screen-host work from the now-stabilized typed foundation.
- Keep JSON as the source of truth.
- Preserve Excel workbook `v3` compatibility while the typed UI is introduced incrementally.

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
- `Forms/MainForm.NodeDetails.cs`
- `UiServices/KnowledgeBaseTreeNodeVisuals.cs`
- `UiServices/KnowledgeBaseTreeViewService.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseDataServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/JsonStorageServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseExcelExchangeServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseFormStateServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseServiceTests.cs`
- `Roadmap.md`

# Known risks / open questions

- The right panel is still the legacy flat/details card. `Phase 2` has not started.
- Automated coverage now checks the new normalization rules, JSON save/load, Excel exchange, and `NodeType`-driven technical fields, but not every possible future typed workflow.
- `README.md` still needs a later refresh if the user wants public-facing docs to match the current `Phase 1` foundation exactly.

# Recommended next step

- Start `Phase 2`:
  - introduce a screen resolver by `NodeType`
  - replace the flat right panel with a typed host while keeping a safe `Info` fallback
  - keep JSON source-of-truth compatibility and workbook `v3` readability intact
- Refresh `README.md` later if user-facing docs also need to reflect the typed foundation.

# Commands to run before finishing future implementation work

```powershell
dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```
