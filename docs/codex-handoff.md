# Current State

Last updated: `2026-05-22`

## Current objective

Continue AKB5 Network UI/UX polishing in the dedicated design worktree after splitting design work away from the clean `Net` logic worktree.

The Network review-filter/layout package, tree icon readability polish, main-shell menu/toolbar polish, splitter polish, and workshop selector resize are committed and pushed on the design branch through `a2da5aa Polish network UI layout and splitter`.
The current design package adds the fixed 24px status bar and the approved style-only right-panel refresh from `artifacts\previews\right-panel-style-only-components-preview.html`: layout, tab order, object order, icons, tree placement, button placement, and behavior stay intact; only the visual shell of existing right-panel sections, tables, fields, buttons, and empty states changes. The latest follow-up replaces standard flat button painting with a custom owner-drawn workspace button to remove hover/focus border artifacts on the right-panel action buttons.

## Current repo state

- Design repository root for UI/UX work: `C:\Users\Olga\AKB5-design`
- Active branch for this task: `design/network-ui-polish`
- Parent logic branch/worktree: `C:\Users\Olga\AKB5` on `Net`; accepted parent baseline is `b2ea12e Enforce network passport mutation validation`, but the latest local check on 2026-05-21 showed Net-side dirty files. Keep parent logic work isolated from the design branch and recheck before using it.
- Current expected design head after the right-panel style-only package: latest `design/network-ui-polish` commit.
- Expected startup check after the right-panel style-only commit/push for `C:\Users\Olga\AKB5-design`:

```text
## design/network-ui-polish...origin/design/network-ui-polish
```

- Current startup check on 2026-05-21 for the main logic worktree:

```text
## Net...origin/Net
 M Controls/KnowledgeBaseNetworkScreenControl.cs
 M docs/codex-handoff.md
 M docs/plans.md
```

- The latest Net mutation-service validation package was accepted manually, committed, and pushed as `b2ea12e`.
- The fresh-chat handoff originally left doc-only edits in the main `Net` worktree; later Net-side local work is tracked in the main worktree handoff.
- The Network review-filter/UI package was moved into `C:\Users\Olga\AKB5-design`, then committed/pushed on `design/network-ui-polish` through `a2da5aa`; later UI/UX work must stay in the design worktree unless the user explicitly changes scope.
- No real `.akb`, JSON data, or Excel files were changed.
- Expected dirty code files in the design worktree after commit/push: none.
- Ignored local preview artifacts relevant to the next step: `artifacts\previews\right-panel-style-only-components-preview.html` is the approved style-only direction; `right-panel-visual-skin-only-preview.html` and `right-panel-docs-style-tabs-preview.html` are reference/alternate previews, not git changes.
- Do not run `git commit`, `git push`, `git merge`, `git rebase`, delete stash entries, or remove worktrees in a future chat unless the user explicitly asks in that same chat.

## Current design package

The committed design package through `a2da5aa` adds:

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
- light-surface/splitter polish and workshop selector resizing so the shell reads as one coherent WinForms command surface.

The status-bar fixed-height follow-up after `a2da5aa` fixes the bottom status bar at a stable 24px height: it adds `StatusBarHeight`, disables `StatusStrip` autosizing/stretch/sizing grip, normalizes status-label height/margins/padding, and reapplies the height on resize.

The style-only right-panel pass adds a shared `Controls/KnowledgeBaseWorkspaceVisuals.cs` helper and applies the approved soft section/table/field/button/empty-state shell across the existing right-panel controls without changing tab order, object order, button order, icons, tree placement, or behavior. The latest button-artifact fix uses custom owner-drawn workspace buttons so the flat button border stays stable instead of showing a jagged focus/hover contour.

## Latest completed package

Commit `a2da5aa Polish network UI layout and splitter` is the current committed/pushed design branch head and contains the Network review-filter/layout, tree icon readability, toolbar/menu, light-shell, splitter, and workshop selector polish described above.

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

Validation completed for the committed design package through `a2da5aa`:

- focused Network tests: `41/41`;
- focused tree-related tests: `33/33`;
- full Release tests: `433/433`;
- `git diff --check`: passed;
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed;
- app build: passed in Debug and Release;
- isolated Release artifact for manual review after the later status-bar fixed-height follow-up exists at `C:\Users\Olga\AKB5-design\artifacts\build-check\statusbar-fixed-height-20260521\asutpKB.exe`;
- non-invasive/offscreen Network layout smoke: `artifacts\layout-smoke\network-review-filter-polish`, passed;
- tree icon smoke/preview: `artifacts\tree-icon-smoke\bin\Debug\net8.0-windows\tree-icons-preview.png`, passed.
- after applying selected toolbar variant D plus screenshot corrections and later narrow shell/layout adjustments, targeted `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, targeted `git diff --check`, and isolated Release builds passed in the earlier session; full tests and layout smokes were not rerun after each narrow shell-layout change.
- recovery check on 2026-05-21: `git diff --check -- Forms/MainForm.Layout.cs` passed for the current uncommitted status-bar fixed-height diff.
- right-panel style-only pass validation on 2026-05-21: `git diff --check`, Debug `dotnet build asutpKB.csproj --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`, and `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore` passed; after the button-artifact fix, targeted `git diff --check -- Controls\KnowledgeBaseWorkspaceVisuals.cs Controls\KnowledgeBaseInfoScreenControl.cs`, Debug build, app format check, and isolated Release build passed.
- pre-commit validation on 2026-05-22 for the combined status-bar/right-panel package: `git diff --check`, app/core/tests `dotnet format --verify-no-changes --severity error --no-restore`, Release `dotnet build asutpKB.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`, and Release `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --logger "console;verbosity=minimal" /p:RunAnalyzers=false /p:WarningLevel=0` passed (`433/433`).
- latest isolated Release artifact after the right-panel button-artifact fix: `C:\Users\Olga\AKB5-design\artifacts\build-check\right-panel-button-artifacts-20260521-231829\asutpKB.exe`.

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

- The design branch is being committed/pushed with the status-bar fixed-height follow-up and right-panel style-only controls/helper changes.
- The main `C:\Users\Olga\AKB5` worktree was dirty on 2026-05-21; recheck before coordinating or merging.
- Preview files under `artifacts\previews\` are ignored local artifacts and will not travel with git unless explicitly added or documented.
- `Добавить похожее` for connections opens a copied endpoint pair as a draft; if the user saves without changing endpoints, the existing duplicate-pair validation rejects it.
- If a default `Release` build output is locked by a running `asutpKB.exe`, use an isolated output path for verification.
- Keep diagnostics compact and avoid broad log scans unless a specific failure requires them.

## Recommended next step

Use the latest right-panel button-artifact artifact above for manual review. If more polishing is needed, keep it style-only and preserve current layout/order/behavior. Merge back to `Net` only after explicit current-chat approval and a fresh check of the main worktree.

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
