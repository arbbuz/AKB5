# AKB5 Summary

Last updated: 2026-04-21

## Repo Snapshot

- Repository root: `C:\Users\Olga\AKB5`
- Active branch: `icon`
- Upstream: `origin/icon`
- App type: WinForms desktop knowledge-base app on `.NET 8`
- Root project: `asutpKB.csproj`
- Core project: `src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj`
- Tests project: `tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj`

## Current Functional State

- JSON remains the source of truth
- Excel import/export remains a separate contract and workbook format v3 is still supported
- The application icon is sourced from `resources/app.ico`
- Splitter width is persisted per workshop in `%LocalAppData%\AKB5\window-layout-state.json`
- The current branch contains a projection-based hidden workshop root implementation:
  - persisted workshop wrapper roots can stay in storage
  - the tree UI hides the wrapper and shows its children as visible roots
  - empty workshops get a virtual hidden wrapper in projection so the first visible node can be added immediately
  - the first add into an empty workshop persists that wrapper into the workshop data

## Important Design Decisions

- Stay on WinForms
- Keep JSON as the main storage
- Do not rewrite the form architecture just to hide the workshop root
- Handle workshop-root hiding in projection/UI-oriented logic, not by rewriting all session persistence logic
- Hide a persisted wrapper only in the safe case:
  - single root
  - name matches workshop
  - `LevelIndex == 0`
  - details are empty

## Most Relevant Files

- `Services/KnowledgeBaseWorkshopTreeProjection.cs`
- `Services/KnowledgeBaseTreeMutationWorkflowService.cs`
- `UiServices/KnowledgeBaseTreeViewService.cs`
- `UiServices/KnowledgeBaseTreeMutationUiWorkflowService.cs`
- `Forms/MainForm.cs`
- `Forms/MainForm.Events.cs`
- `Forms/MainForm.WorkflowContexts.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseWorkshopTreeProjectionTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseTreeMutationWorkflowServiceTests.cs`
- `docs/codex-handoff.md`

## Validation Actually Run

```powershell
dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
```

- `dotnet test`: passed, `121/121`
- `dotnet build`: passed
- Existing analyzer warnings remain
- `NU1900` warnings remain because vulnerability metadata could not be fetched from NuGet in this environment

## Known Risks / Open Questions

- No manual WinForms smoke test has been run for the current projection-based fix
- The tree behavior should still be checked manually on Windows for:
  - empty workshop selection
  - first top-level add
  - workshop switching
  - rename/delete/edit flows under hidden wrapper roots
- Search UX text and actual implementation still do not match; this is unrelated and still open

## Recommended Next Step

Run a Windows smoke test on branch `icon` and verify:

1. No visible workshop root is shown in the tree for workshops that use the technical wrapper
2. An empty workshop allows adding the first visible `Department` node directly
3. Switching between workshops preserves each workshop tree correctly
4. Editing existing visible nodes does not reintroduce duplicate visible workshop roots
