# AKB5 Summary

Last updated: 2026-04-21

## Repo Snapshot

- Repository root: `C:\Users\Olga\AKB5`
- Active branch: `icon`
- Upstream: `origin/icon`
- Worktree state: modified, local changes not yet committed
- App type: WinForms desktop knowledge-base app on `.NET 8`
- Root project: `asutpKB.csproj`
- Core project: `src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj`
- Tests project: `tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj`

## Current Functional State

- JSON remains the source of truth
- Excel import/export remains a separate contract and workbook format v3 is still supported
- Application icon is sourced from `resources/app.ico`
- Splitter width is persisted per workshop in `%LocalAppData%\AKB5\window-layout-state.json`
- Current local changes implement projection-based workshop-root hiding in the tree UI:
  - when a workshop contains a single root node with `LevelIndex = 0`, that node is hidden in `TreeView`
  - its children are shown as visible roots in the UI
  - newly created workshops now get a real persisted hidden root immediately, with the exact workshop name
  - legacy or imported empty workshops still get a virtual hidden wrapper root so the first visible node can be added immediately
  - if a hidden wrapper is virtual and has no children, persisted snapshot collapses back to an empty root list
  - if a hidden wrapper is already persisted, it is preserved even when it has no children

## Important Design Decisions

- Stay on WinForms
- Keep JSON as the main storage
- Do not rewrite form architecture just to hide the workshop root
- Handle workshop-root hiding in projection/UI-oriented logic, not by rewriting storage format
- Keep storage structure unchanged; only change the UI entry point into the hierarchy
- Do not depend on workshop-name text matching the persisted root text; real data already uses mixed full names and abbreviations
- New workshops should be created with a real hidden root object, not only a virtual projection wrapper

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

- `dotnet test`: passed, `123/123`
- `dotnet build`: passed
- Existing analyzer warnings remain
- `NU1900` warnings remain because vulnerability metadata could not be fetched from NuGet in this environment

## Known Risks / Open Questions

- No manual WinForms smoke test has been run for the current projection-based fix
- The tree behavior still needs a real Windows UI check for:
  - filled workshop selection
  - newly created workshop selection
  - legacy empty workshop selection
  - first visible top-level add
  - rename/delete/move flows under a hidden wrapper root
- Search UX text and actual implementation still do not match; this is unrelated and still open

## Recommended Next Step

Run a Windows smoke test on branch `icon` and verify:

1. No visible workshop root is shown for workshops that use the technical wrapper
2. A filled workshop opens directly at the department level
3. An empty workshop allows adding the first visible department node directly
4. Switching between workshops preserves each workshop tree correctly
