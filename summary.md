# AKB5 Summary

Last updated: 2026-04-20

## Purpose

This file is a short handoff summary for a new dialog or another AI agent.
It reflects the current inspected state of the repository on branch `icon`.

## Repo Snapshot

- Repository root: `C:\Users\Olga\AKB5`
- Active branch: `icon`
- Upstream: `origin/icon`
- Worktree state at inspection time: modified, with local persisted splitter-state changes not yet committed
- App type: WinForms knowledge-base application on `.NET 8`
- Root app project: `asutpKB.csproj`
- Core library project: `src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj`
- Tests project: `tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj`

## Current Functional State

- The project uses JSON as the source of truth.
- Excel is a separate editable import/export format.
- Supported Excel contract is workbook format v3.
- The application icon source is now `resources/app.ico`.
- `asutpKB.csproj` embeds `resources/app.ico` as `ApplicationIcon` and also copies it to build/publish output.
- Runtime WinForms windows use `AppIconProvider` to load `resources/app.ico` from `AppContext.BaseDirectory`, so replacing that file does not require code changes.
- Replacing `resources/app.ico` updates the window icon without code changes; rebuilding is still required if the executable file icon itself also needs to change.
- `MainForm` now remembers splitter width per workshop, and switching between items inside the same workshop does not create separate splitter states.
- Splitter state is persisted across app restarts in `%LocalAppData%\AKB5\window-layout-state.json`.
- Splitter state is intentionally separate from the knowledge-base JSON and does not affect dirty/save prompts for domain data.
- The current branch already contains the recent UI changes around:
  - hidden workshop wrapper root handling
  - creation of visible top-level nodes inside workshops with hidden wrapper roots
  - removal of the photo preview panel
  - removal of the top duplicated status labels
  - bottom status bar without time prefix in the last-action label

## Architecture Summary

- `Forms/` contains WinForms screens and UI shell code.
- `UiServices/` contains WinForms-specific workflows and screen orchestration.
- `Models/` and `Services/` contain the domain model and testable non-UI logic.
- `src/AsutpKnowledgeBase.Core` is a linked core project that currently includes files from `../../Models/**/*.cs` and `../../Services/**/*.cs` instead of physically owning moved sources.
- `tests/AsutpKnowledgeBase.Core.Tests` covers the core library, not the WinForms UI layer.

## Important Design Decisions Already Present

- Do not rewrite the app away from WinForms.
- Do not replace JSON storage as the main source of truth.
- Do not collapse Excel logic into JSON storage.
- The hidden workshop wrapper root is intentional:
  - in storage/model, the technical workshop root may remain at `LevelIndex = 0`
  - in the UI, its children can be shown as visible roots
- The recent bug around adding a new visible top-level node was treated as a UI selection issue, not as a domain-model issue.

## Strong Sides

- Core logic is meaningfully separated from the form and is testable.
- File workflows are more robust than usual for a desktop tool:
  - JSON save uses temp file + backup
  - load can fall back to backup
  - logging is structured and persistent
- The hidden-wrapper workshop projection is implemented as an explicit service and is covered by tests.
- Excel import/export is treated as a real contract, with validation and regression coverage.
- CI includes restore, format verification, build, test, and publish checks on Windows.

## Weak Sides

- `MainForm` is still a thick shell even after partial extraction.
- Excel parser and writer remain very large and cognitively expensive to modify.
- Search UI text and actual behavior do not match:
  - the UI says search works by name, path, and level
  - the implemented search service currently matches only node names
- Dirty-state and undo/redo are based on full serialized snapshots of the current session data, which is simple and reliable but may become costly for large trees.
- The physical repo structure still carries technical debt because the core project links shared source files instead of fully owning them.
- There is no automated WinForms UI validation for click selection, right-click behavior, drag-and-drop, or layout behavior.

## Validation Actually Run

Commands run during inspection:

```powershell
dotnet restore C:\Users\Olga\AKB5\asutpKB.csproj
dotnet restore C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj
dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
```

Observed results:

- `dotnet test`: passed, `117/117`
- `dotnet build`: passed
- Build output now includes `bin/Release/net8.0-windows/resources/app.ico`.
- A later `dotnet build` and `dotnet test` on `2026-04-20` also passed after the persisted splitter-state change.
- Build/test still emit existing analyzer warnings.
- NuGet vulnerability metadata lookup produced `NU1900` warnings because the environment could not fetch the vulnerability index, but restore/build/test still completed.

## Known Risks / Open Questions

- No manual WinForms smoke test was run in this session.
- The splitter-state behavior is covered by service-level tests and successful build/test, but it still needs a real Windows UI smoke test across full app restart.
- The current UX around the context command `Добавить сюда` may still feel ambiguous for users when no node is selected and a new visible top-level node is expected.
- The search behavior mismatch should be treated as either:
  - a UX wording bug
  - or an incomplete search implementation
- Large Excel classes are well-tested, but still risky to refactor quickly without disciplined incremental changes.

## Recommended Next Step

1. Run a manual Windows smoke test on branch `icon` for:
   - click on empty tree space
   - right-click selection behavior
   - adding a visible top-level node in a workshop with hidden wrapper root
   - drag-and-drop
   - save/load/import/export
2. Resolve the search mismatch:
   - either implement search by path/level
   - or change the UI text to match actual behavior
3. After that, continue small-scope refactoring:
   - keep moving orchestration out of `MainForm`
   - avoid broad architectural rewrites

## Files Most Relevant For Further Work

- `AGENTS.md`
- `docs/codex-handoff.md`
- `Forms/MainForm.cs`
- `Forms/SetupForm.cs`
- `Forms/InputDialog.cs`
- `AppIconProvider.cs`
- `resources/app.ico`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.Events.cs`
- `Forms/MainForm.NodeDetails.cs`
- `Forms/MainForm.WorkflowContexts.cs`
- `UiServices/KnowledgeBaseTreeViewService.cs`
- `UiServices/KnowledgeBaseTreeMutationUiWorkflowService.cs`
- `UiServices/KnowledgeBaseFileUiWorkflowService.cs`
- `UiServices/KnowledgeBaseWorkshopUiWorkflowService.cs`
- `Services/KnowledgeBaseWorkshopTreeProjection.cs`
- `Services/KnowledgeBaseTreeMutationWorkflowService.cs`
- `Services/KnowledgeBaseFileWorkflowService.cs`
- `Services/JsonStorageService.cs`
- `Services/KnowledgeBaseWindowLayoutStateService.cs`
- `Services/KnowledgeBaseExcelExchangeService.cs`
- `Services/KnowledgeBaseExcelWorkbookParser.cs`
- `Services/KnowledgeBaseXlsxWriter.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseWorkshopTreeProjectionTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseTreeMutationWorkflowServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseExcelExchangeServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseWindowLayoutStateServiceTests.cs`

## Notes For The Next Agent

- Work on `icon`, not `main`.
- Prefer small diffs.
- Do not move the application icon path again unless there is a packaging reason; current convention is `resources/app.ico`.
- To swap the app icon without code changes, replace `resources/app.ico`; rebuild if the `.exe` file icon also needs to reflect the new asset.
- Splitter width is now persisted per workshop in `%LocalAppData%\AKB5\window-layout-state.json`; it is intentionally outside the domain JSON file.
- Respect the existing Excel v3 contract.
- Do not claim UI behavior is validated unless a real manual Windows check was performed.
