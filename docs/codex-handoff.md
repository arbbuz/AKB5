# Current State

Last updated: `2026-05-18`

## Current objective

Implement the accepted staged hybrid for the `Lvl3` right-panel tab `Состав`: keep cabinet/board contents out of `Lvl4` tree nodes and manage them on the right side grouped by Siemens-style racks starting from `Rack0`.

Roadmap file:

- `docs/composition-rack-roadmap-2026-05-17.md`
- `docs/composition-rack-ui-note-2026-05-18.md`

## Current checkpoint 2026-05-18

Latest pushed commit on `origin/card`:

```text
8832b74 Improve rack composition switching
```

Current local worktree has an uncommitted UI redesign for the `Состав` tab:

- `Controls/KnowledgeBaseCompositionScreenControl.cs`
- `Forms/MainForm.cs`
- `Forms/MainForm.Events.cs`

The redesign removes the duplicated upper/lower Rack view. The `Состав` tab now uses one detailed Rack table per Rack, stacked vertically. Two Rack tables are intended to be visible on screen; `Rack2+` appears below with vertical scrolling. The old text button `Добавить Rack` is replaced by a compact `+` button. The old horizontal splitter and saved splitter-height wiring were removed because there is no longer a top/bottom split inside the tab.

Current manual-review build:

```text
C:\Users\Olga\AKB5\artifacts\build-check\asutpKB-20260518-121141\asutpKB.exe
```

Validation already run for this local WIP:

- `dotnet build asutpKB.csproj --configuration Release --no-restore`
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --no-build` (`365/365`)
- `git diff --check`
- temporary WinForms smoke-test: two Rack detail windows render and default selection remains `Rack0` slot 1;
- temporary WinForms smoke-test: selecting a row in the second Rack switches active selection to `Rack1`.

Recommended next action: start the new chat by reading the standalone handoff at `C:\Users\Olga\Documents\Codex\2026-05-18\akb5-card-handoff-cc-users-olga\akb5-card-handoff-2026-05-18-rack-details-ui.md`, then run `git -C C:\Users\Olga\AKB5 status --short --branch`.

## Current repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active branch for this task: `card`
- Baseline commit before local edits: `6700841 Implement card workspace level split`
- `origin/card` matched local `card` before this implementation started
- No `git commit` or `git push` has been run in this session

Recovery note after context overflow:

- A fresh session saved external WIP backups under `C:\Users\Olga\Documents\Codex\2026-05-17\89-akb\backups`, fixed stale test call sites for the new rack-aware method signatures, and re-ran compact validation.

## Implemented in the current local worktree

- Added `RackNumber` to `KbCompositionEntry`.
- Added `RackNumber` to composition templates and object-template composition entries.
- Existing records without an explicit rack remain compatible and normalize to `Rack0`.
- SQLite schema version is bumped to `5`; old `.akb` databases get `composition_entries.rack_number INTEGER NOT NULL DEFAULT 0`.
- `KnowledgeBaseCompositionStateService` now groups slotted entries into `Rack0+` and builds placeholder slot rows for the Step 7-like rack view.
- Added S7-300-style advisory slot roles through `KnowledgeBaseCompositionRackSlotRulesService`:
  - `Rack0`: `PS`, `CPU`, `IM`, `SM/FM/CP`
  - `Rack1+`: `PS`, `Свободен`, `IM`, `SM/FM/CP`
- Implemented Stage 2 `SIMATIC S7-300` advisory profile:
  - slot mismatches are surfaced as non-blocking warnings;
  - expansion racks show `IM 360/361/365` hints where relevant;
  - rack cards, rack details, and the edit dialog show a `Проверка` field;
  - the summary displays the active profile and warning count.
- Reworked the WinForms `Состав` screen:
  - upper area: rack cards like `(0) UR / Rack0`;
  - lower area: detail table for the selected rack;
  - auxiliary/non-slotted equipment remains in a separate block.
- Fixed a startup crash in the first rack UI build by replacing unsafe `SplitContainer.SplitterDistance` startup sizing with a plain percent-based `TableLayoutPanel`.
- Updated the composition entry dialog so slotted entries can choose `Rack` and `Slot`.
- `Добавить слот...` uses the selected rack/empty slot when available; otherwise it appends to the next free slot in the selected rack.
- Updated search, copy/template workflows, JSON/SQLite round-trip tests, and normalization tests for rack-aware composition.

## Decisions already made

- `Lvl1` = department, `Lvl2` = system, `Lvl3` = cabinet/board.
- Cabinet/board contents must not be expanded into `Lvl4` tree nodes by default.
- Stage 1 uses a compatible hybrid: rack number lives on `CompositionEntries`; there is no separate persisted rack entity yet.
- Empty racks are not persisted in Stage 1. A rack appears once it has at least one slotted entry; `Rack0` is shown as the default starting layout.
- Siemens slot rules are advisory, not blocking validation. Stage 2 adds visible warnings/hints while still allowing real-world cabinet layouts to be saved.

## Files already relevant to the task

- `docs/composition-rack-roadmap-2026-05-17.md`
- `Models/KbCompositionEntry.cs`
- `Models/KbCompositionTemplate.cs`
- `Models/KbObjectTemplate.cs`
- `Services/KnowledgeBaseCompositionRackSlotRulesService.cs`
- `Services/KnowledgeBaseCompositionStateService.cs`
- `Services/KnowledgeBaseCompositionMutationService.cs`
- `Services/KnowledgeBaseCompositionTemplateService.cs`
- `Services/KnowledgeBaseObjectTemplateService.cs`
- `Services/KnowledgeBaseDataService.cs`
- `Services/KnowledgeBaseTreeSearchService.cs`
- `Services/SqliteKnowledgeBaseStorageService.cs`
- `Controls/KnowledgeBaseCompositionScreenControl.cs`
- `Forms/MainForm.Composition.cs`
- `Forms/KnowledgeBaseCompositionEntryDialog.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/*Composition*`
- `tests/AsutpKnowledgeBase.Core.Tests/SqliteKnowledgeBaseStorageServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseDataServiceTests.cs`

## Validation status

Passed:

- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`
- `dotnet build asutpKB.csproj --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --logger "console;verbosity=minimal" /p:RunAnalyzers=false /p:WarningLevel=0` (`389/389`)
- Targeted `KnowledgeBaseCompositionStateServiceTests` (`5/5`)
- Release build into `artifacts\verify\composition-rack-stage2\build`
- Release tests into `artifacts\verify\composition-rack-stage2\test` (`389/389`)
- Startup smoke of the Release `asutpKB.exe`: process stayed running after 5 seconds; the test instance was then stopped.
- `git diff --check`
- Fresh recovery validation after fixing stale test call sites:
  - `dotnet build asutpKB.csproj --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`
  - `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --logger "console;verbosity=minimal" /p:RunAnalyzers=false /p:WarningLevel=0` (`389/389`)
  - `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`
  - `git diff --check`
- Fresh Stage 3 manual-review build:
  - `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\verify\composition-rack-stage3\build /p:RunAnalyzers=false /p:WarningLevel=0`
- Fresh Stage 3 UI-fixes manual-review build:
  - `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\verify\composition-rack-stage3-ui-fixes\build /p:RunAnalyzers=false /p:WarningLevel=0`

Note: `scripts\verify-step.ps1 -StepName composition-rack-hybrid` exited early at its build wrapper with no useful build error in the log. The same Release build and Release tests were run manually and passed in the intended artifact folder.

Manual review executable:

```text
C:\Users\Olga\AKB5\artifacts\verify\composition-rack-stage3-ui-fixes\build\asutpKB.exe
```

## Known risks / open questions

- Manual UI review has not been done yet.
- Stage 1 does not persist empty racks as first-class records.
- Stage 1 does not yet model Step 7 subslot/interface rows such as `X1`, `X2`, `Port 1`, `Port 2`.
- Stage 2 does not hard-block invalid Siemens slot placement; it displays warnings/hints only.
- If manual review shows the rack cards need different sizing, the next change should stay in `Controls/KnowledgeBaseCompositionScreenControl.cs`.

## Recommended next step

Run manual review of the `Состав` tab on `Lvl3` cabinets/boards:

- confirm `Карточка`, `Состав`, `График ТО` still appear only on `Lvl3`;
- add several slotted entries into `Rack0` and `Rack1`;
- confirm `Проверка` warns for obvious mismatches, for example CPU in `Rack1` slot 2 or non-PS in slot 1;
- confirm `IM 360/361/365` hints are useful and not too noisy for incomplete cabinets;
- confirm existing old composition entries appear under `Rack0`;
- confirm non-slotted equipment stays in the separate `Доп. оборудование` tab;
- confirm editing/deleting selected rack rows still works.

After manual review, either adjust the UI details or request `git commit` / `git push` explicitly.

## Commands to run before finishing future implementation work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --no-restore /p:RunAnalyzers=false /p:WarningLevel=0
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --logger "console;verbosity=minimal" /p:RunAnalyzers=false /p:WarningLevel=0
git diff --check
```
