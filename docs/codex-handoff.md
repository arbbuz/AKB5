# Current State

Last updated: `2026-05-21`

## Current objective

Continue AKB5 Network UI/UX polishing in the dedicated design worktree after splitting design work away from the clean `Net` logic worktree.

The Network review-filter/layout package, tree icon readability polish, and main-shell menu/toolbar polish are implemented in the design worktree. The current full design worktree is ready for manual review.
The latest follow-up applies the selected visual variant D and the first screenshot corrections: a single inline top toolbar with primary `Сохранить` (Material Symbols `save` + text), quiet text dropdown buttons (`Файл`, `ТО`, `Каталог`, `Сервис`), and quiet icon-only Material Symbols `undo` / `redo`; the tree-collapse action remains next to the object tree, and `Текущий цех` is now wrapped in the same soft-panel visual language.

## Current repo state

- Design repository root for UI/UX work: `C:\Users\Olga\AKB5-design`
- Active branch for this task: `design/network-ui-polish`
- Parent logic branch/worktree: `C:\Users\Olga\AKB5` on `Net`; expected current parent baseline is `b2ea12e Enforce network passport mutation validation`. Keep parent logic work isolated from the design branch.
- Current expected design head: `a89e593 Improve network passport manual entry hints`
- Current startup check on 2026-05-20 for `C:\Users\Olga\AKB5-design`:

```text
## design/network-ui-polish
a89e593 (HEAD -> design/network-ui-polish) Improve network passport manual entry hints
```

- Current startup check on 2026-05-20 for the main logic worktree:

```text
## Net...origin/Net
b2ea12e (HEAD -> Net, origin/Net) Enforce network passport mutation validation
```

- The latest Net mutation-service validation package was accepted manually, committed, and pushed as `b2ea12e`.
- The fresh-chat handoff originally left doc-only edits in the main `Net` worktree; later Net-side local work is tracked in the main worktree handoff.
- The uncommitted local Network review-filter/UI package was moved into `C:\Users\Olga\AKB5-design`; this session's UI/UX work stayed in the design worktree.
- No real `.akb`, JSON data, or Excel files were changed.
- Known dirty code/test files in the design worktree: `Controls/KnowledgeBaseNetworkScreenControl.cs`, `Controls/KnowledgeBaseTreeView.cs`, `Services/KnowledgeBaseNetworkStateService.cs`, `UiServices/KnowledgeBaseTreeNodeVisuals.cs`, and `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkStateServiceTests.cs`.
- Do not run `git commit`, `git push`, `git merge`, `git rebase`, delete stash entries, or remove worktrees in a future chat unless the user explicitly asks in that same chat.

## Current local package awaiting manual review

The uncommitted design package adds:

- a passport filter checkbox `Только проверка` that shows only device/interface/connection rows whose inline `Проверка` text is not empty;
- always-visible passport totals for devices, interfaces, connections, and rows needing `Проверка`;
- `Копировать проверку`, which copies only visible rows with review warnings, grouped by devices/interfaces/connections;
- computed warning-row counters on `KnowledgeBaseNetworkState`, covered by focused tests;
- a stable three-row filter layout so `Фильтр`, the textbox, `Сбросить`, review actions, and counters do not overlap the `Устройства` group;
- a wider `Проверка` column plus subtle row highlighting for rows that need manual review;
- clearer filter placeholder/status text and tooltips for the filter, `Только проверка`, `Копировать видимое`, and `Копировать проверку`;
- an ignored offscreen layout-smoke harness in `artifacts\layout-smoke\network-review-filter-polish\` that checks the new controls, review-only filter behavior, warning-row highlighting, `Проверка` column width, and that the filter row does not intersect the `Устройства` group.
- object-tree icons are enlarged from 20px to 30px, with tree row height/indent adjusted for the larger icons;
- Lvl1/Lvl2/Lvl3 icons use Google Material Symbols SVG path data from the internet: `domain` for `отделение`, `precision_manufacturing` for `система / установка`, and `dns` for `шкаф`; current accent colors are preserved;
- an ignored tree-icon smoke harness in `artifacts\tree-icon-smoke\` verifies 30px icon size, non-blank Lvl1/Lvl2/Lvl3 icons, and writes a preview PNG;
- top-level menu emoji were removed; the top `ToolStrip` now starts with a primary `Сохранить` button (Material Symbols `save` plus text), then quiet text dropdown buttons `Файл`, `ТО`, `Каталог`, and `Сервис`, then quiet Material Symbols icon-only `undo` / `redo`;
- `Файл`, `ТО`, `Каталог`, and `Сервис` are implemented as `ToolStripDropDownButton` items with soft hover/pressed states so they read as modern command-bar controls rather than old 3D buttons;
- the top toolbar and tree toolbar use `ModernToolbarRenderer`; icon buttons briefly shift down/right on press so a click is visibly acknowledged;
- toolbar item heights were normalized so `Сохранить`, `undo` / `redo`, and `Файл` / `ТО` / `Каталог` / `Сервис` align vertically;
- the primary `Сохранить` button width is calculated from the text and icon size so the icon is not clipped;
- `Текущий цех` moved into a soft bordered section with a flat ComboBox so it visually matches the updated toolbar/tree controls better than the old standalone label + native combo row;
- `Свернуть дерево` was moved out of the global toolbar and into a small icon-only toolbar next to the object tree, using `collapse_all`;
- `resources\fonts\MaterialSymbolsOutlined.ttf` is copied with the app so toolbar icons do not depend on a system-installed font.

## Latest completed package

Commit `a89e593 Improve network passport manual entry hints` contains the accepted manual-entry speed and inline duplicate-hints package:

- device/interface/connection action panels expose `Добавить похожее` / `Добавить похожий`;
- the same add-similar actions are available from row context menus;
- add-similar device drafts copy stable context fields such as linked node, role, vendor, model, order number, firmware, location/cabinet, and notes while leaving unique identity fields blank;
- add-similar interface drafts copy device, subnet/gateway/VLAN/protocol/speed/medium/notes while leaving unique address/port/MAC/MPI fields blank;
- add-similar connection drafts copy endpoints and cable metadata while leaving the cable label blank;
- Network state rows expose inline `Проверка` text for narrow duplicate hints;
- device duplicate hints cover PROFINET-name and MAC;
- interface duplicate hints cover IP, MAC, and the same port/name on the same device;
- connection duplicate hints cover duplicate cable labels;
- Network list copies and visible export include the new `Проверка` column.

Commit `207b6b1 Improve network passport review ergonomics` is the previous accepted review package:

- richer connection endpoint text with device/interface/IP/`MPI/DP/PN`;
- visible connection `Длина`, interface `Скорость` / `Примечание`, and device `Производитель` / `Место` columns;
- copy-friendly row/context-menu actions and visible-row exports for devices, interfaces, and connections;
- passport-wide `Копировать видимое` export with table headers;
- persistent selected-row visibility and row tooltips for long manual-review values.

## Decisions already made

- Network passport CRUD is manual-entry first.
- Keep the existing file-reference workflow intact: image preview remains in-form, and `Open original` remains the reliable source action.
- PDF network scheme references are accepted as metadata/`Open original` sources.
- Embedded PDF preview/rendering is not approved.
- Do not start OCR/PDF auto-import, PRONETA/CSV import, live scan, plan/fact comparison, data-quality issue/problem panel, AKB5-driven IP assignment, or AKB5-driven PROFINET-name assignment.
- Do not split future Net manual-entry/UI work into tiny micro-stages; bundle closely related manual-review/UI refinements into one coherent package.
- For WinForms Network UI changes, use focused tests while developing and run one full tests/build/offscreen layout-smoke cycle when a package is ready.
- Do not run interactive UI-smoke unless the user explicitly asks. Prefer non-invasive/offscreen layout-smoke.
- If Codex shows no progress for about 2-3 minutes after a completed tool result, treat it as an orchestration stall and recover early.

## Files already relevant to the task

- `Controls/KnowledgeBaseNetworkScreenControl.cs`
- `Controls/KnowledgeBaseTreeView.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.Network.cs`
- `Forms/MainForm.Events.cs`
- `Forms/KnowledgeBaseNetworkDeviceDialog.cs`
- `Forms/KnowledgeBaseNetworkInterfaceDialog.cs`
- `Forms/KnowledgeBaseNetworkConnectionDialog.cs`
- `Forms/KnowledgeBaseNetworkFileReferenceDialog.cs`
- `Models/KbNetworkDevice.cs`
- `Models/KbNetworkInterface.cs`
- `Models/KbNetworkConnection.cs`
- `Models/KbNetworkFileReference.cs`
- `Services/KnowledgeBaseNetworkStateService.cs`
- `Services/KnowledgeBaseNetworkMutationService.cs`
- `Services/KnowledgeBaseNetworkFieldValidationService.cs`
- `Services/KnowledgeBaseNetworkPreviewService.cs`
- `UiServices/KnowledgeBaseTreeNodeVisuals.cs`
- `asutpKB.csproj`
- `resources/fonts/MaterialSymbolsOutlined.ttf`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkStateServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkMutationServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkFieldValidationServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkPreviewServiceTests.cs`
- `ui-smoke-network-passport.ps1`
- `artifacts/layout-smoke/network-review-filter-polish/` (ignored offscreen smoke artifact)
- `artifacts/tree-icon-smoke/` (ignored tree icon smoke/preview artifact)

## Validation status

Validation completed for the current uncommitted design package:

- focused Network tests: `41/41`;
- focused tree-related tests: `33/33`;
- full Release tests: `433/433`;
- `git diff --check`: passed;
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed;
- app build: passed in Debug and Release;
- isolated Release artifact for manual review, built from the design worktree after applying selected toolbar variant D plus screenshot corrections: `C:\Users\Olga\AKB5-design\artifacts\build-check\toolbar-variant-d-workshop-panel-20260521-012017\asutpKB.exe`;
- non-invasive/offscreen Network layout smoke: `artifacts\layout-smoke\network-review-filter-polish`, passed;
- tree icon smoke/preview: `artifacts\tree-icon-smoke\bin\Debug\net8.0-windows\tree-icons-preview.png`, passed.
- after applying selected toolbar variant D plus screenshot corrections, `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, targeted `git diff --check`, and isolated Release build passed; full tests and layout smokes were not rerun after that narrow shell-layout change.

Validation completed for `a89e593` before commit/push:

- focused Network tests: `41/41`;
- full Release tests: `433/433`;
- `git diff --check`: passed;
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed;
- app build: passed;
- isolated Release artifact: `C:\Users\Olga\AKB5\artifacts\build-check\network-manual-entry-hints-20260520-132919\asutpKB.exe`;
- non-invasive/offscreen layout smoke: `artifacts\layout-smoke\network-manual-entry-hints`, passed.

The offscreen smoke project restore/build emitted pre-existing analyzer warnings when run through `dotnet run`; the smoke itself completed successfully. The official app format/build/test commands above were green.

## Known risks / open questions

- The current design worktree is not committed or pushed and is waiting for manual review.
- The main `C:\Users\Olga\AKB5` worktree is expected to be clean at `b2ea12e`; recheck before coordinating or merging.
- The design branch currently has no separate remote tracking branch; `origin/Net` is still the parent baseline.
- `Добавить похожее` for connections opens a copied endpoint pair as a draft; if the user saves without changing endpoints, the existing duplicate-pair validation rejects it.
- If a default `Release` build output is locked by a running `asutpKB.exe`, use an isolated output path for verification.
- Keep diagnostics compact and avoid broad log scans unless a specific failure requires them.

## Recommended next step

Use the fresh design-worktree artifact above for manual review. If accepted, commit/merge back to `Net`/push only after explicit current-chat approval.

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
