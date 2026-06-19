# Current State

Last updated: `2026-06-19`

## Current objective

Current accepted package on `Net`: reduce visible redraw waves in object-tree workspace tables and stabilize Lvl3 composition column width persistence.

## Current repo state

- Main worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Local code changes are the accepted UI redraw/column-width package, plus the pre-existing local `AGENTS.md` change that is not part of this package.
- No real `.akb` or JSON user data files were edited.
- Latest local review exe: `C:\Users\Olga\AKB5\bin\Release\net8.0-windows\asutpKB.exe`

## Current package

Tracked/source changes for the accepted UI package:

- `Controls\BufferedDataGridView.cs`: adds a double-buffered grid control for WinForms tables that redraw visibly during row rebuilds.
- `Controls\ControlRedrawScope.cs`: adds a small Win32 redraw-suppression helper for scoped UI rebuilds.
- `Controls\KnowledgeBaseCompositionScreenControl.cs`: batches rack-table row rebuilds, suppresses intermediate redraw, keeps composition column widths as real `DataGridViewColumn.Width` values, and applies those widths across same-type rack tables.
- `Controls\KnowledgeBaseAdditionalEquipmentScreenControl.cs`: batches additional-equipment table rebuilds and preserves/apply column width state.
- `Forms\MainForm.cs` and `Forms\MainForm.WorkspaceHost.cs`: suppress workspace host redraw while selected-node panels are swapped or rebuilt.
- Grid-heavy forms under `Forms\`: use the buffered grid control where the redraw wave was visible.

The previous filler-column experiment was removed before acceptance. The current Lvl3 composition table has only the user-facing columns `Slot`, `Role`, `Type`, and `OrderNumber`; there is no hidden or empty filler column.

## Validation status

Validation completed in `C:\Users\Olga\AKB5`:

- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -clp:Summary`: passed, 0 errors, 47 existing warnings.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --no-build -clp:Summary`: passed, 410 tests.
- `git diff --check -- Controls Forms`: passed; only CRLF normalization warnings.

Manual review by the user found the latest accepted behavior good enough to keep for now:

- Lvl3 composition column widths now persist better while switching between Lvl3 objects.
- If the four fixed-width columns are narrower than the table viewport, a blank area can remain on the right. This is accepted for now and should not be changed without a new explicit decision.

## Decisions already made

- Keep Lvl3 composition columns as direct fixed-width columns for now; do not restore `Fill` behavior for those four user columns without a new decision.
- Do not add a visible empty/filler column to consume leftover width.
- Keep the current package pragmatic and scoped to redraw/column-width stabilization.

## Known risks / open questions

- The right-side blank area remains possible when the total fixed column width is smaller than the visible table width.
- Further visual tuning of this table needs a new explicit behavior choice.

## Recommended next step

No immediate implementation step is pending. If the current behavior is accepted after another review pass, commit/push the accepted package on `Net` while leaving the unrelated `AGENTS.md` local change unstaged.

## Commands to run before finishing future implementation work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```
