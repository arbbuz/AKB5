# Current State

Last updated: `2026-04-27`

## Repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active branch: `interface`
- Current work is being integrated directly into `interface`.

## Latest accepted change set

- Reset to `Info` when switching to a different object with workspace tabs.
- Keyboard arrow navigation kept inside the tree.
- Updated inner glyph for `L1` icon without changing tile shape or color.
- `Forms/MainForm.cs`
  - `CollapseTreeToRoots()` now explicitly synchronizes the right workspace with the node that remains selected after collapse.
- `UiServices/KnowledgeBaseTreeNodeVisuals.cs`
  - `L1/L2/L3` tree icons are now resolved by visible hierarchy depth, not only by stored `NodeType`.
- `UiServices/KnowledgeBaseTreeViewService.cs`
  - visible depth is passed while building `TreeView` nodes so icon selection matches actual placement in the tree.

## Validated status

Actually run on the current local state:

```powershell
dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-build --no-restore
```

- `dotnet build`: passed
- `dotnet test`: passed, `156/156`
- Existing warnings remain, including `NU1900` when NuGet vulnerability metadata is unavailable.

## Active objective

- Continue small, user-verified UI corrections on `interface` without widening the change scope.
- Continue roadmap work from `Phase 3B` after the accepted polish set is integrated.

## Durable decisions already made

- `interface` remains the active integration branch.
- Treat `L1/L2/L3` as:
  - `L1` = отделение
  - `L2` = системы
  - `L3` = шкаф
- The drag-lag investigation is deferred as environment-specific unless it starts reproducing on multiple machines.
- `docs/codex-handoff.md` is the single current-state file for future sessions.

## Knowledge harness

- Current state lives here: `docs/codex-handoff.md`
- Active plans live in: `docs/plans.md`
- Reusable insights live in: `docs/lessons-learned.md`
- Durable decisions live in: `docs/decision-log.md`
- On the explicit command `дистиллируй знания из сессии`, update those files in place:
  - merge new knowledge into the right file
  - replace stale statements
  - do not create parallel notes or duplicate documents

## Relevant files for the current task

- `Forms/MainForm.cs`
- `Forms/MainForm.Events.cs`
- `Forms/MainForm.WorkspaceHost.cs`
- `Forms/MainForm.Layout.cs`
- `UiServices/KnowledgeBaseTreeNodeVisuals.cs`
- `UiServices/KnowledgeBaseTreeViewService.cs`
- `AGENTS.md`
- `Roadmap.md`

## Recommended next step

- Continue implementation from `Phase 3B`.
- Keep using the fixed `docs/` harness for state, plans, lessons learned, and durable decisions.
