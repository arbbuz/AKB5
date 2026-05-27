# Current State

Last updated: `2026-05-27`

## Current objective

Current active work is a Network topology grid-snap/fan-link follow-up on `Net` after pushed commit `cdf375d Polish Lvl2 and Lvl3 list interactions`: topology objects created from the toolbar/context menu and objects moved by drag now snap their top-left coordinate to the existing 24 px canvas grid; links from multiple objects on the same horizontal/vertical level to one common block now route through separate orthogonal lanes instead of sharing the same segment. The implementation is local and validated with app format, Release build, `git diff --check`, and offscreen topology layout-smoke. Manual review is accepted, and the current chat authorized commit/push for this package.

Previous committed/pushed package includes:

- Startup timing checkpoints are logged through the existing `IAppLogger` as `StartupTiming` events.
- Timing checkpoints cover `Program.Main`, `MainForm` construction, startup storage selection, synchronous data load, SQLite schema/load/normalize phases, and file workflow storage load.
- Existing startup behavior remains synchronous; this package measures where time is spent and provides a faster package shape, but does not yet move database loading off the UI startup path.
- `scripts\publish.ps1` now supports `-PublishMode SingleFile|Folder` and `-ReadyToRun`.
- New `scripts\publish-fast.ps1` / `scripts\publish-fast.cmd` build a self-contained folder publish with `PublishSingleFile=false` and `PublishReadyToRun=true`.
- Fast publish output is `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe`.
- Startup logs are written to `%LocalAppData%\AKB5\logs\app-yyyy-MM-dd.ndjson`; filter event name `StartupTiming`.
- Network topology link-type selection no longer uses a long ComboBox in the equipment toolbar; it is now a bottom strip below the topology viewport.
- The link-type strip uses full button labels such as `Profibus, оптоволокно`, `Profibus, медь`, `MPI, медь`, `Profinet, оптоволокно`, and `Profinet, медь`, with the real line color/dash sample. Button widths are measured from the full regular/bold text instead of fixed short widths, so labels must not be ellipsized.
- Clicking an existing link selects it, synchronizes the strip to that link kind, and clicking a strip button immediately changes the selected link kind. With no selected link, the strip still chooses the kind for the next new link.

Current follow-up package:

- The Network topology field now uses a scrollable viewport around a logical canvas instead of clipping content to the current screen size.
- `Ctrl + mouse wheel` zooms the topology canvas around the mouse pointer; normal wheel scrolling still moves the viewport vertically.
- The bottom strip now includes compact fixed-right zoom controls: `-`, an editable/drop-down percent field, and `+`; choosing/typing `100%` returns to the original scale. The separate `Масштаб` label and the internal link-type strip scrollbar were removed so the row stays compact.
- In the Network tab only, visible object labels and new-object default prefixes changed from `ET200` to `ET`; the stored enum remains `KbNetworkElementKind.Et200` for compatibility with existing saved topology data.
- The existing Network element dialog now has an optional `Доп. IP` row. Leaving it empty removes the additional IP; entering a value keeps duplicate checks across primary and additional IP addresses on all topology objects. The object context menu no longer exposes separate add/edit additional-IP dialogs.
- Network topology now has a `Внешняя связь` object kind for external systems. It stores its visible text in the existing element `Name` field, appears in the toolbar/context add menu/type dropdown, draws as a card with an internal text field instead of an IP/device-icon card, and uses a text-entry dialog mode without `IP` / `Доп. IP` fields. New external-link elements open with an empty `Текст` field instead of the old default `Внешняя связь-01` text.
- Network topology card text now uses `ClearTypeGridFit` in the canvas paint path and integer `Segoe UI` sizes. Object names use regular weight, IP badges remain bold with a wider badge and slightly smaller font to avoid clipping long addresses, and external-link card text is bold again so it matches the rest of the topology.
- `docs/decision-log.md` was pruned from a dated project archive into 65 lines of domain-grouped durable decisions; build/test history and temporary phase status were removed from that file.
- `scripts/codex-safe.ps1` was added as a repo-level shell guard: it blocks broad `git diff`, full `decision-log` reads, raw/wildcard docs reads, broad repo `rg`, recursive unfiltered enumeration, and destructive git/delete patterns; it also truncates command output.
- Element/link hit testing, dragging, context-menu add position, link endpoint dragging, and link-kind strip behavior are translated through the current zoom.
- Node IP/name text is drawn through the same scaled graphics pipeline as cards/icons, so labels scale with the topology instead of staying at screen size.
- The link-kind selector is now a thin fixed strip under the scrollable viewport, so it stays visible without covering topology nodes or links.
- Topology objects created from the toolbar/context menu and dragged objects now snap their top-left coordinate to the existing 24 px canvas grid.
- Links from multiple topology objects on the same horizontal/vertical level to one common block now route through separate orthogonal lanes, so their visible segments do not overlap.
- Lvl2 `Документация и ПО` link lists now have a right-click menu with `Открыть`, `Изменить`, `Добавить`, and `Удалить`. Right-clicking a row selects it before opening the menu; right-clicking empty list space clears selection. `Добавить` is list-specific for schemes, instructions, or software, while open/edit/delete reuse the existing selected-link workflow.
- Lvl2 `Документация и ПО` link lists now resize columns to screenshot-like proportions: wide `Наименование`, medium `Путь`, narrow `Обновлено` / `Добавлено`.
- Lvl3 `Состав` Rack grids now have a right-click menu with `Изменить`, `Добавить слот`, and `Удалить`. Right-clicking a row selects it before opening the menu; right-clicking empty grid space selects the Rack and clears the entry selection so only add-slot remains available.
- Lvl3 `Состав` Rack-grid selected rows use `SystemColors.Highlight` / `SystemColors.HighlightText`, so selection is visually explicit like the Lvl2 `Документация и ПО` lists.
- Lvl3 `Доп. оборудование` now uses the same visual structure as `Состав`: top action row, scrollable section card, and `DataGridView` inside the bordered panel. Its content remains the additional-equipment rows and columns.
- Lvl3 `Доп. оборудование` now has a right-click menu with `Изменить`, `Добавить`, and `Удалить`. Right-clicking a row selects it before opening the menu; right-clicking empty grid space clears selection so only add remains available.
- Lvl3 `Состав` no longer displays columns `Firmware`, `MPI/DP/PN`, `I address`, `Q address`, or `IP-адрес`; Lvl3 `Доп. оборудование` no longer displays `IP-адрес`. This is display-only: saved model fields and dialogs were not removed.
- Lvl3 `Состав` Rack grids use fill-width columns with `Модуль` as the widest column. Lvl3 `Доп. оборудование` uses fill-width `DataGridView` columns with the reviewed screenshot proportions.
- Lvl3 `Доп. оборудование` now shows the left column as `№` with simple row numbers `1`, `2`, `3`, etc. Its default widths follow the reviewed screenshot: narrow number column, wide `Тип`, then `Компонент`, then `Примечание`.
- Lvl3 `Доп. оборудование` selected rows use `SystemColors.Highlight` / `SystemColors.HighlightText`, so selection is visually explicit like the Lvl2 `Документация и ПО` lists.
- `KnowledgeBaseCompositionEntryDialog` no longer shows `Firmware`, `MPI/DP/PN`, `I address`, `Q address`, or `IP-адрес` for adding/editing slots or additional equipment. Existing saved values for those fields are preserved when an entry is edited.
- `KnowledgeBaseCompositionEntryDialog` button panel is now part of the dialog table layout instead of a bottom-docked overlay, so `Заказной номер` remains visible and is not covered by `Сохранить` / `Отмена`.

Do not commit, push, merge, rebase, or create/remove remote branches without explicit approval in the current chat.

## Current repo state

- Main worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Base commit before this follow-up: `cdf375d Polish Lvl2 and Lvl3 list interactions`.
- Manual review for the current Network grid-snap/fan-link package is accepted.
- Commit/push is authorized in the current chat.
- No real `.akb` or JSON user data files were edited.
- Existing local `AGENTS.md` edits are present and were not made or touched as part of this task.
- Current grid-snap build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\network-topology-grid-snap\asutpKB.exe`.
- Current fast-publish review executable: `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe`.
- Current Docs/Software context-menu build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\docs-software-context-menu\asutpKB.exe`.
- Current Docs/Software column default build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\docs-software-column-defaults\asutpKB.exe`.
- Current Lvl3 context-menu build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\lvl3-context-menus\asutpKB.exe`.
- Current Lvl3 column cleanup build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\composition-columns\asutpKB.exe`.
- Current Lvl3 column-fill/dialog cleanup build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\composition-column-fill\asutpKB.exe`.
- Current Lvl3 dialog IP cleanup build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\composition-dialog-ip-hidden\asutpKB.exe`.
- Current Lvl3 dialog layout build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\composition-dialog-layout\asutpKB.exe`.
- Current additional-equipment column default build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\additional-equipment-column-defaults\asutpKB.exe`.
- Current additional-equipment visual alignment build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\additional-equipment-composition-visual\asutpKB.exe`.
- Current additional-equipment selection-revert build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\additional-equipment-selection-revert\asutpKB.exe`.
- Current additional-equipment system-highlight build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\additional-equipment-system-highlight\asutpKB.exe`.
- Current composition system-highlight build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\composition-system-highlight\asutpKB.exe`.
- Standard `artifacts\publish-fast\win-x64` was refreshed successfully after the Docs/Software, Docs/Software column default, Lvl3 context-menu, Lvl3 column-cleanup, Lvl3 column-fill/dialog-cleanup, Lvl3 dialog IP cleanup, Lvl3 dialog layout, additional-equipment column default, additional-equipment visual-alignment, additional-equipment selection-color revert, additional-equipment system-highlight, and composition system-highlight follow-ups.
- Fast publish is a folder package with 273 files, not a single-file exe.

## Current package

Current changed tracked files:

- `AGENTS.md` (pre-existing local change, not part of this task)
- `Controls/KnowledgeBaseNetworkTopologyScreenControl.cs`
- `docs/codex-handoff.md`
- `docs/plans.md`
- `docs/decision-log.md`

Ignored validation artifacts:

- `artifacts\build-check\network-topology-grid-snap\asutpKB.exe`
- `artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`
- `artifacts\build-check\docs-software-context-menu\asutpKB.exe`
- `artifacts\build-check\docs-software-column-defaults\asutpKB.exe`
- `artifacts\build-check\lvl3-context-menus\asutpKB.exe`
- `artifacts\build-check\composition-columns\asutpKB.exe`
- `artifacts\build-check\composition-column-fill\asutpKB.exe`
- `artifacts\build-check\composition-dialog-ip-hidden\asutpKB.exe`
- `artifacts\build-check\composition-dialog-layout\asutpKB.exe`
- `artifacts\build-check\additional-equipment-column-defaults\asutpKB.exe`
- `artifacts\build-check\additional-equipment-composition-visual\asutpKB.exe`
- `artifacts\build-check\additional-equipment-selection-revert\asutpKB.exe`
- `artifacts\build-check\additional-equipment-system-highlight\asutpKB.exe`
- `artifacts\build-check\composition-system-highlight\asutpKB.exe`
- `artifacts\publish-fast\win-x64\asutpKB.exe`

## Validation status

Validation completed in `C:\Users\Olga\AKB5`:

- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\startup-timing /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-fast.ps1`: passed; output `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe`.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed, 407 tests.
- `git diff --check`: passed with CRLF normalization warnings only.
- Manual fast-publish review: accepted. User launched `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe` several times and reported that startup feels faster.
- Startup log review in `%LocalAppData%\AKB5\logs\app-2026-05-26.ndjson`: latest warm launches reached `mainform-constructor-completed` in about `670-717 ms`; SQLite `sqlite-load-total` was about `173-183 ms`. One earlier cold launch reached `mainform-data-loaded` in about `12.5 s`, so repeated cold-start slowness should be investigated inside UI session application/binding rather than SQLite read itself.
- Network link-type palette follow-up: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore` passed.
- Network link-type palette follow-up: `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-kind-palette /p:RunAnalyzers=false /p:WarningLevel=0` passed with 0 warnings and 0 errors.
- Network link-type palette follow-up: `dotnet run --project artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj --configuration Release /p:RunAnalyzers=false /p:WarningLevel=0` passed and produced `artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`; the smoke now verifies selecting a link and changing it through the palette.
- Network link-type palette follow-up: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-fast.ps1` re-ran successfully after the UI change; fast publish output now includes the new palette.
- Network link-type palette follow-up: `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0` passed, 407 tests.
- Network link-type palette follow-up: `git diff --check` passed with CRLF normalization warnings only.
- Network zoom/viewport follow-up: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore` passed.
- Network zoom/viewport follow-up: `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore` passed.
- Network zoom/viewport follow-up: `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore` passed.
- Network zoom/viewport follow-up: `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom /p:RunAnalyzers=false /p:WarningLevel=0` passed with 0 warnings and 0 errors.
- Network zoom/viewport follow-up: `dotnet run --project artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj --configuration Release /p:RunAnalyzers=false /p:WarningLevel=0` passed; smoke now verifies link-kind palette interaction, a small viewport with a low element needing vertical scroll, and zoom-out behavior.
- Network zoom/viewport follow-up: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-fast.ps1` passed; output `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe`.
- Network zoom/viewport follow-up: `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0` passed, 407 tests.
- Network zoom/viewport follow-up: `git diff --check` passed with CRLF normalization warnings only.
- Text scaling fix follow-up: after replacing topology-card `TextRenderer` calls with scaled `Graphics.DrawString`, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom /p:RunAnalyzers=false /p:WarningLevel=0`, layout-smoke, `dotnet test` 407 tests, and `scripts\publish-fast.ps1` passed. One earlier parallel build attempt failed only because `obj\Release\net8.0-windows\asutpKB.dll` was temporarily locked; a sequential rerun passed.
- Bottom link-kind strip follow-up: after moving the link-type selector out of the canvas overlay and into a fixed bottom strip, `dotnet format` for app/core/tests passed, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom /p:RunAnalyzers=false /p:WarningLevel=0` passed with 0 warnings and 0 errors, layout-smoke passed and refreshed `artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`, `dotnet test` passed 407 tests, `scripts\publish-fast.ps1` passed, and `git diff --check` passed with CRLF normalization warnings only.
- Full link-kind labels follow-up: `опт.` abbreviations were removed from the bottom strip and selected-link status; the strip now uses full labels such as `Profibus, оптоволокно` and `Profinet, оптоволокно`. After manual review found remaining ellipses inside fixed-width buttons, the button width calculation was changed to measure the full regular and selected bold label text plus the line sample area. `rg -n "опт\." Controls\KnowledgeBaseNetworkTopologyScreenControl.cs docs\codex-handoff.md docs\plans.md docs\decision-log.md` found no matches. `dotnet format` for app/core/tests passed, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-labels /p:RunAnalyzers=false /p:WarningLevel=0` passed with 0 warnings and 0 errors, layout-smoke passed and refreshed the PNG with full labels visible, `dotnet test` passed 407 tests, and `scripts\publish-fast.ps1` passed.
- Final label-width follow-up: after manual review still showed a clipped last character in the selected `Profinet, медь` button, the link-kind button sizing added a measured-text safety margin while keeping the total strip narrow enough for ordinary widths. App format, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-labels /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false`, layout-smoke, and `scripts\publish-fast.ps1` passed. The refreshed smoke PNG shows all five link-kind buttons in one row without clipped labels or an internal strip scrollbar at the smoke width. Full core tests were not rerun after this final padding-only paint/layout adjustment.
- Visible zoom-control follow-up: the bottom strip now adds compact fixed-right zoom controls (`-`, percent combo field, `+`) so zoom can be changed without the hotkey. The separate `Масштаб` label and the internal legend scrollbar were removed after review so the link-type row stays compact. `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom-controls /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false`, layout-smoke, and `scripts\publish-fast.ps1` passed. Full core tests were not rerun after this UI-only follow-up.
- ET visible-name follow-up: Network tab UI strings for adding/editing ET objects and the new-object default prefix were changed from `ET200` to `ET`; `rg -n '"ET200"|ET200' Controls\KnowledgeBaseNetworkTopologyScreenControl.cs` found no visible `ET200` text in the Network control, `git diff --check` passed with CRLF normalization warnings only, `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore` passed, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-et-label /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false` passed with 0 warnings and 0 errors, layout-smoke passed and refreshed `artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`, and `scripts\publish-fast.ps1` passed. Full core tests were not rerun after this label-only UI change.
- Additional-IP follow-up: added `AdditionalIpAddresses` on Network topology elements, a right-click `Add IP address` workflow for selected objects, duplicate checking across primary and additional addresses, and a compact secondary IP display on topology cards. Validation passed: app/core/tests `dotnet format`, Release build to `artifacts\build-check\network-topology-additional-ip`, targeted normalization test, Network topology layout-smoke, `scripts\publish-fast.ps1`, and full core tests (`408/408`).
- Additional-IP layout fix: after screenshots showed a clipped add-IP dialog and overlapping card contents, the add-IP dialog was widened, topology cards were made taller, and the secondary IP badge spacing/icon/name positions were adjusted. App format passed, Release build to `artifacts\build-check\network-topology-additional-ip-layout-fix` passed with 0 warnings and 0 errors, layout-smoke passed, and a separate fast folder publish was created at `artifacts\publish-fast-layout-fix\win-x64`. The old `artifacts\build-check\network-topology-additional-ip\asutpKB.exe` was not overwritten because it was open as process `asutpKB (5768)`.
- Additional-IP edit follow-up: right-clicking an object with an additional IP now offers editing existing additional IP addresses. The edit dialog is prefilled with the selected address and duplicate checking excludes only that same address on the same object. App format passed, Release build to `artifacts\build-check\network-topology-edit-additional-ip` passed with 0 warnings and 0 errors, layout-smoke passed, and standard `scripts\publish-fast.ps1` passed after a sequential `dotnet restore asutpKB.csproj -r win-x64`.
- Additional-IP inline follow-up: the separate add/edit additional-IP UI was replaced by the existing `Элемент сети` dialog with a `Доп. IP` row. Empty `Доп. IP` deletes the additional address; filled `Доп. IP` is validated against the same duplicate map. Fixed the octet handlers so primary and additional IP fields focus and advance independently. App format passed, Release build to `artifacts\build-check\network-topology-additional-ip-inline` passed with 0 warnings and 0 errors, layout-smoke passed, and standard `scripts\publish-fast.ps1` passed.
- External-connection object follow-up: added `KbNetworkElementKind.ExternalConnection = 9`, default text `Внешняя связь`, toolbar/context menu/type-dropdown entries, toolbar icon, and a topology card that renders the element name inside an internal text-field rectangle for external system labels. Validation passed: app/core/tests `dotnet format`, Release build to `artifacts\build-check\network-topology-external-connection`, full core tests (`409/409`), layout-smoke with a sample `MES / LIMS` external card, and standard `scripts\publish-fast.ps1`.
- External-connection text-dialog follow-up: when `Внешняя связь` is selected in `Элемент сети`, the dialog hides `IP` and `Доп. IP`, relabels `Название` to `Текст`, and uses a multiline text box for the card text. App format passed, Release build to `artifacts\build-check\network-topology-external-text-dialog` passed with 0 warnings and 0 errors, and layout-smoke passed. Standard `scripts\publish-fast.ps1` failed because a running `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe` process locked a font file; a separate review publish succeeded at `artifacts\publish-fast-external-text\win-x64`.
- Final accepted Network topology package: new `Внешняя связь` elements now start with an empty `Текст` field so users can enter labels such as `КСПД`, `АКТ`, or `ВВК` immediately. Validation passed: app/core/tests `dotnet format`, Release build to `artifacts\build-check\network-topology-final` with 0 warnings and 0 errors, full core tests (`409/409`), layout-smoke, standard `scripts\publish-fast.ps1`, and `git diff --check` with CRLF normalization warnings only.
- Network topology font-softening follow-up: canvas text rendering now sets `TextRenderingHint.ClearTypeGridFit`; topology card names/external text use integer `Segoe UI` regular fonts; IP badges use integer `Segoe UI` bold fonts. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\network-topology-font-softening`, layout-smoke, explicit `dotnet restore asutpKB.csproj -r win-x64`, and standard `scripts\publish-fast.ps1`. Full core tests were not rerun after this UI-only paint/font adjustment.
- Network topology IP/font fit follow-up: widened primary IP badges from 120 px to 128 px inside the existing 134 px card, reduced primary IP font from 10 bold to 9 bold, and made the external-link inner text bold again. Validation passed: Release build to `artifacts\build-check\network-topology-font-ip-fit`, layout-smoke, explicit `dotnet restore asutpKB.csproj -r win-x64`, and standard `scripts\publish-fast.ps1`. Full core tests were not rerun after this UI-only paint/font adjustment.
- Decision-log cleanup: `docs\decision-log.md` was reduced from 216 lines of dated history to 65 lines of durable domain decisions. No app build/test was run for this documentation-only cleanup.
- Shell guard follow-up: `scripts\codex-safe.ps1` was added and `AGENTS.md` now requires using it by default for small/localized shell diagnostics. Verified allowed `git status --short --branch`; verified blocked full `git diff`; verified blocked full `Get-Content docs\decision-log.md`. No app build/test was run for this guard/documentation-only change.
- Docs/Software context-menu follow-up: added right-click menus to the scheme, instruction, and software link lists on the Lvl2 `Документация и ПО` tab. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\docs-software-context-menu` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1`. Full core tests were not rerun after this UI-only context-menu change.
- Docs/Software column default follow-up: set Lvl2 `Документация и ПО` default/resized column widths to wide `Наименование`, medium `Путь`, and narrow date columns. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\docs-software-column-defaults` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1`. Full core tests were not rerun after this UI-only display change.
- Lvl3 context-menu follow-up: added right-click selection and menus to the `Состав` Rack grids and the `Доп. оборудование` list. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\lvl3-context-menus` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1`. Full core tests were not rerun after this UI-only context-menu change.
- Lvl3 column-cleanup follow-up: removed unconfirmed technical columns from the visible `Состав` and `Доп. оборудование` lists. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\composition-columns` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1`. Full core tests were not rerun after this UI-only display change.
- Lvl3 column-fill/dialog-cleanup follow-up: remaining columns now fill available width (`Модуль` widest in `Состав`, `Компонент` widest in `Доп. оборудование`), and `Firmware` / `MPI/DP/PN` / `I address` / `Q address` were removed from the composition entry dialog. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\composition-column-fill` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1`. Full core tests were not rerun after this UI-only display/dialog change.
- Lvl3 dialog IP cleanup follow-up: `IP-адрес` was removed from the composition entry dialog for adding/editing slots and additional equipment; existing saved IP values are preserved when editing. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\composition-dialog-ip-hidden` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1`. Full core tests were not rerun after this UI-only dialog change.
- Lvl3 dialog layout follow-up: fixed the composition entry dialog after screenshot review showed `Заказной номер` hidden under the bottom buttons. The button panel is now inside the table layout and the dialog height is adjusted. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\composition-dialog-layout` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1`. Full core tests were not rerun after this UI-only layout change.
- Additional-equipment column default follow-up: the Lvl3 `Доп. оборудование` first column now uses header `№` and simple row numbers, and column widths are set to match the reviewed screenshot proportions. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\additional-equipment-column-defaults` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1`. Full core tests were not rerun after this UI-only display change.
- Additional-equipment visual-alignment follow-up: Lvl3 `Доп. оборудование` was changed from the source/summary/ListView layout to the same visual structure used by `Состав`: action row, scrollable section card, and `DataGridView` in a bordered panel. The row content remains additional-equipment data. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\additional-equipment-composition-visual` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1`. Full core tests were not rerun after this UI-only layout/control change.
- Additional-equipment selection-color revert: the attempted neutral selection coloring was reverted after review confirmed the first row color was only normal selection. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\additional-equipment-selection-revert` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1` after the previously open `publish-fast` executable was closed.
- Additional-equipment system-highlight follow-up: Lvl3 `Доп. оборудование` now uses system selection colors like the Lvl2 `Документация и ПО` ListView rows (`SystemColors.Highlight` and `SystemColors.HighlightText`) instead of the softer shared grid accent. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\additional-equipment-system-highlight` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1`.
- Composition system-highlight follow-up: Lvl3 `Состав` Rack grids now use the same explicit system selection colors as `Доп. оборудование` and the Lvl2 `Документация и ПО` ListView rows (`SystemColors.Highlight` and `SystemColors.HighlightText`) instead of the softer shared grid accent. Validation passed: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, Release build to `artifacts\build-check\composition-system-highlight` with 0 warnings and 0 errors, and standard `scripts\publish-fast.ps1`.
- Manual review for the Lvl2/Lvl3 UI package was accepted by the user before commit/push. Full core tests were not rerun after the final UI-only selection-highlight changes.
- Network grid-snap/fan-link follow-up: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore` passed.
- Network grid-snap/fan-link follow-up: `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-grid-snap /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false` passed with 0 warnings and 0 errors.
- Network grid-snap/fan-link follow-up: `git diff --check` passed with CRLF normalization warnings for `AGENTS.md` and `Controls/KnowledgeBaseNetworkTopologyScreenControl.cs`.
- Network grid-snap/fan-link follow-up: `dotnet run --project artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj --configuration Release /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false` passed and refreshed `artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`; the smoke now checks 24 px snap and separated fan-link lanes.
- Full core tests and publish-fast were not rerun after this UI-only topology routing/dragging change.

## Decisions already made

- Work only on `Net / origin/Net`.
- Do not commit or push without fresh direct approval in the current chat.
- Keep the default `scripts\publish.ps1` mode backward compatible as `SingleFile`.
- Use `scripts\publish-fast.ps1` as the accepted faster working package: folder publish, self-contained, ReadyToRun, no single-file extraction.
- Keep startup diagnostics in file logs, not visible UI, so normal operators are not interrupted.
- For Network topology, choose and edit link types through the full-label bottom strip below the topology viewport rather than through a toolbar ComboBox or a floating overlay; do not add hover/popup tooltips.
- Keep compact visible zoom controls in the bottom strip: `Ctrl + mouse wheel` remains supported, but users must also be able to change/reset zoom through the percent field and `-` / `+` buttons. Do not add a separate `Масштаб` label unless the layout is redesigned again.
- In the Network tab, display the ET object kind as `ET` rather than `ET200`; keep the stored `Et200` enum name/value unchanged for compatibility.
- In the Network topology, represent external system relationships with the `Внешняя связь` object kind and draw its `Name` as text inside the card.
- For Network topology, keep stored element coordinates logical and resolution-independent; adapt to screen size with viewport scrolling and zoom instead of rewriting coordinates.
- For Network topology, snap newly placed and dragged objects to the existing 24 px grid; do not migrate untouched stored coordinates.
- For Network topology, route multiple same-level links to a common block through separate orthogonal lanes.

## Files already relevant to the task

- `Program.cs`
- `Controls\KnowledgeBaseNetworkTopologyScreenControl.cs`
- `Controls\KnowledgeBaseDocsAndSoftwareScreenControl.cs`
- `Controls\KnowledgeBaseCompositionScreenControl.cs`
- `Controls\KnowledgeBaseAdditionalEquipmentScreenControl.cs`
- `Forms\MainForm.Composition.cs`
- `Forms\MainForm.DocsAndSoftware.cs`
- `Forms\MainForm.Events.cs`
- `Services\KnowledgeBaseFileWorkflowService.cs`
- `Services\SqliteKnowledgeBaseStorageService.cs`
- `Services\FileAppLogger.cs`
- `scripts\publish.ps1`
- `scripts\publish-fast.ps1`
- `scripts\publish-fast.cmd`
- `scripts\codex-safe.ps1`
- `docs/codex-handoff.md`
- `docs/plans.md`
- `docs/decision-log.md`

## Known risks / open questions

- Manual comparison is qualitative so far: fast publish feels faster to the user, but no external stopwatch trace compares old single-file vs fast folder publish.
- Fast publish has many files. It should be copied/launched as a whole folder, not as a lone `asutpKB.exe`.
- Startup timing logs show elapsed checkpoints after the app starts; they do not measure Windows Defender/SmartScreen time before managed code begins.
- If `mainform-data-loaded` repeatedly dominates cold startup while SQLite remains fast, first add narrower timing around loaded-session UI application/binding; then consider deferred/asynchronous database load after first form display.
- Manual review should check right-click behavior on existing rows and empty space in each Lvl2 `Документация и ПО` list and in the Lvl3 `Состав` / `Доп. оборудование` tabs, confirm Lvl2/Lvl3 list widths match the reviewed proportions, confirm removed Lvl3 columns are no longer visible, confirm `Доп. оборудование` visually matches `Состав` while keeping its additional-equipment content, and confirm the add/edit dialog shows all remaining fields without button overlap.
- Targeted inspection of `C:\Users\Olga\.codex\config.toml` did not reveal an active command/tool policy key, so `scripts\codex-safe.ps1` is a repo-level guard rather than a dispatcher-level hard guarantee.
- Manual review for the current Network change should check that toolbar-created, context-created, and dragged topology objects visually snap to grid intersections, and that several same-row objects connected to one common block draw as separate lanes at normal and changed zoom levels.

## Recommended next step

After commit/push, start the next implementation from synced `Net` / `origin/Net` and confirm `git status --short --branch` before editing. Future commit/push still requires fresh explicit approval in the active chat.

## Commands to run before finishing future implementation work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\startup-timing /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-kind-palette /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-labels /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom-controls /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-external-connection /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-final /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-font-softening /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-font-ip-fit /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\docs-software-column-defaults /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\lvl3-context-menus /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\composition-columns /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\composition-column-fill /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\composition-dialog-ip-hidden /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\composition-dialog-layout /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\additional-equipment-column-defaults /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\additional-equipment-composition-visual /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\additional-equipment-selection-revert /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\additional-equipment-system-highlight /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\composition-system-highlight /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-grid-snap /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-fast.ps1
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0
dotnet run --project artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj --configuration Release /p:RunAnalyzers=false /p:WarningLevel=0
git diff --check
```
