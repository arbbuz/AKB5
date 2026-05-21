# Current State

Last updated: `2026-05-22`

## Current objective

Finish the user-requested removal of the `Сеть` feature from AKB5 on branch `Net`.

The request changed after the rejected overview-to-passport navigation package: the user explicitly asked to remove the Network tab completely and then confirmed full removal.

Do not commit, push, merge, rebase, or remove unrelated worktrees without explicit approval in the current chat.

## Current repo state

- Main worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Accepted baseline before this uncommitted package: `92e5af5 Port network UI design polish`
- Current working tree contains an uncommitted full Network-removal package.
- No real `.akb` or JSON user data files have been edited.

## Current package

The uncommitted package removes Network as an application feature:

- deleted Network UI/control/dialog files;
- removed the selected-node `Сеть` workspace tab creation, event wiring, workspace mapping, and resolver enum value;
- removed Network domain models, mutation/state/preview/validation services, and focused Network tests;
- removed Network fields from `SavedData`, session state, form state, snapshot serialization/comparison, object templates, composition rack metadata, tree mutation cleanup, and delete-impact UI;
- removed SQLite Network table creation/load/save paths and bumped SQLite schema version to `11`;
- on save/restore, old SQLite Network tables and old Network columns are dropped inside the normal transaction after the existing external backup step;
- updated affected core tests to stop expecting Network records or template defaults.

## Validation status

Validation is still in progress.

Completed in this interrupted package:

- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-removed-inprogress /clp:ErrorsOnly` passed; executable path:
  `C:\Users\Olga\AKB5\artifacts\build-check\network-removed-inprogress\asutpKB.exe`
- `dotnet build tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /clp:ErrorsOnly` passed.

Not yet completed:

- app/core/tests `dotnet format --verify-no-changes`;
- full Release tests;
- final app Release build after docs/test cleanup;
- `git diff --check`;
- final `git status --short --branch`.

One `dotnet test` run was manually interrupted after about 49 seconds and must not be counted as passed.

## Known risks / open questions

- Historical docs (`Roadmap.md`, `docs/decision-log.md`, old handoff files) may still mention Network as past work; current code and current handoff/plans should be the source for the active state.
- SQLite removal intentionally drops old Network tables/columns only during save/restore, after the existing external backup step. Loading an old DB ignores those old tables.
- Need full validation before reporting the package ready.

## Recommended next step

Continue from the current working tree:

1. Run the remaining validation commands.
2. Fix any test failures.
3. Keep output compact.
4. Do not commit/push without explicit approval.

## Commands to run before finishing

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-removed-final /clp:ErrorsOnly
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /clp:ErrorsOnly
git diff --check
git status --short --branch
```
