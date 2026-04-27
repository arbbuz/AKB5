# Current State

Last updated: `2026-04-27`

## Repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active branch: `interface`
- Current `HEAD`: `3222380`
- Working tree is clean

## Latest accepted change set

- Commit `3222380` (`Polish tree sync and add session knowledge harness`)
- Workspace tabs reset to `Info` when switching to a different object
- Keyboard arrow navigation stays inside the tree
- `CollapseTreeToRoots()` now keeps the right workspace synchronized with the selected tree node
- `L1/L2/L3` icons are resolved by visible tree depth instead of stale persisted `NodeType`
- The session knowledge harness now lives in `docs/`

## Validated status

Actually run on this accepted state:

```powershell
dotnet format C:\Users\Olga\AKB5\asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format C:\Users\Olga\AKB5\src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-build --no-restore
```

- `dotnet format`: passed for app, core, and tests
- `dotnet build`: passed
- `dotnet test`: passed, `156/156`
- Existing warnings remain, including `NU1900` when NuGet vulnerability metadata is unavailable

## Active objective

- Continue roadmap implementation from `Phase 3B`
- Keep UI polish incremental and user-verified when needed

## Durable decisions already made

- `interface` remains the active integration branch
- Use tree taxonomy:
  - `L1` = department
  - `L2` = system
  - `L3` = cabinet
- Treat drag-lag as an environment-specific investigation unless it reproduces on multiple machines with the same repo state
- `docs/codex-handoff.md` is the single current-state file for future sessions

## Knowledge harness

- Current state: `docs/codex-handoff.md`
- Active plans: `docs/plans.md`
- Reusable insights: `docs/lessons-learned.md`
- Durable decisions: `docs/decision-log.md`
- On the explicit command `дистиллируй знания из сессии`, update those files in place and replace stale information instead of appending session transcripts

## Relevant files for the current task area

- `Forms/MainForm.cs`
- `Forms/MainForm.Events.cs`
- `Forms/MainForm.WorkspaceHost.cs`
- `Forms/MainForm.Layout.cs`
- `UiServices/KnowledgeBaseTreeNodeVisuals.cs`
- `UiServices/KnowledgeBaseTreeViewService.cs`
- `AGENTS.md`
- `Roadmap.md`

## Recommended next step

- Continue implementation from `Phase 3B`
- Keep using the fixed `docs/` harness for state, plans, lessons learned, and durable decisions
