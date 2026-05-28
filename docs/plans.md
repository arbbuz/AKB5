# Plans

Last updated: `2026-05-27`

## Active plan

- Work in `C:\Users\Olga\AKB5` on branch `Net`, tracking `origin/Net`.
- Current task: Lvl2/Lvl3 UI table follow-up after pushed commit `d3bc568 Add network topology keyboard movement`. The previous Lvl3 `Состав` cell-copy/column cleanup was manually accepted, but commit/push was not authorized. In `Доп. оборудование`, the former `Компонент` column is now `Заказной номер` and displays only the saved order number without the Siemens-prefixed model text. User-adjusted column widths for `Состав`, `Доп. оборудование`, and Lvl2 `Документация и ПО` now apply globally across the tree and persist through window-layout state. Header sorting is disabled for both Lvl3 grids so row order cannot change after clicking `Slot`, `Роль`, `Тип`, or other headers. Both Lvl3 grid-style tabs now support right-click `Копировать ячейку` for the exact clicked cell. Local validation passed; manual review for the latest table behavior is pending.
- Do not commit or push without fresh direct approval in the current chat.
- Do not reintroduce hover/popup tooltips; guidance must be visible, inline, status-based, or modal.
- Treat old AKB5/Net/network-topology worktrees and snapshots under `C:\Users\Olga\Documents\Codex\...` as historical references only, not as source of truth.

## Completed package

- Add startup timing checkpoints to existing file logs under `%LocalAppData%\AKB5\logs`.
- Keep timing diagnostics non-invasive and operator-invisible.
- Preserve default `scripts\publish.ps1` single-file behavior for the supported end-user flow.
- Add a fast working publish wrapper that builds a self-contained folder package with ReadyToRun and no single-file extraction.
- Validate with format checks, isolated Release build, fast publish, full core test suite, and `git diff --check`.
- Manual fast-publish review is accepted: user launched it several times and reported that startup feels faster.
- Network topology link-type selection is moved from the equipment toolbar ComboBox into a bottom strip below the topology viewport with full choices: `Profibus, оптоволокно`, `Profibus, медь`, `MPI, медь`, `Profinet, оптоволокно`, and `Profinet, медь`.
- Existing links can now be selected directly; pressing a strip button changes the selected link kind immediately. Without a selected link, the strip sets the type for the next new link.
- Network palette validation passed app format, isolated Release build, offscreen layout-smoke PNG, full core tests, and `git diff --check`.
- Lvl2 Docs/Software context menus are implemented locally and validated with app format, isolated Release build, and standard fast publish.
- Lvl2 Docs/Software column default follow-up is implemented locally and validated with app format, isolated Release build to `artifacts\build-check\docs-software-column-defaults`, and standard fast publish.
- Lvl2 Docs/Software user column-width persistence is implemented locally and validated with app format, isolated Release build to `artifacts\build-check\docs-software-column-width-persistence`, and `git diff --check`.
- Lvl3 Composition / Additional Equipment context menus are implemented locally and validated with app format, isolated Release build to `artifacts\build-check\lvl3-context-menus`, and standard fast publish.
- Lvl3 Composition / Additional Equipment column cleanup is implemented locally and validated with app format, isolated Release build to `artifacts\build-check\composition-columns`, and standard fast publish.
- Lvl3 column-fill and dialog cleanup is implemented locally and validated with app format, isolated Release build to `artifacts\build-check\composition-column-fill`, and standard fast publish.
- Lvl3 dialog IP cleanup is implemented locally and validated with app format, isolated Release build to `artifacts\build-check\composition-dialog-ip-hidden`, and standard fast publish.
- Lvl3 dialog layout fix is implemented locally and validated with app format, isolated Release build to `artifacts\build-check\composition-dialog-layout`, and standard fast publish.
- Additional-equipment column default follow-up is implemented locally and validated with app format, isolated Release build to `artifacts\build-check\additional-equipment-column-defaults`, and standard fast publish.
- Additional-equipment visual-alignment follow-up is implemented locally and validated with app format, isolated Release build to `artifacts\build-check\additional-equipment-composition-visual`, and standard fast publish.
- Additional-equipment selection-color revert is implemented locally and validated with app format, isolated Release build to `artifacts\build-check\additional-equipment-selection-revert`, and standard fast publish.
- Additional-equipment system-highlight follow-up is implemented locally and validated with app format, isolated Release build to `artifacts\build-check\additional-equipment-system-highlight`, and standard fast publish.
- Composition system-highlight follow-up is implemented locally and validated with app format, isolated Release build to `artifacts\build-check\composition-system-highlight`, and standard fast publish.
- Next action after this accepted package is to start any future implementation from synced `Net` / `origin/Net`.
- Next possible optimization after measurement: if cold logs repeatedly show `mainform-data-loaded` dominating while SQLite remains fast, add narrower UI binding/session-application timing first; defer database load until after first form display only if that is still justified.

## Current implementation plan

- Keep the Lvl3 `Состав` composition-grid follow-up and Lvl3 `Доп. оборудование` order-number follow-up local until explicit git authorization.
- Keep the Rack detail grid without the visible `Модуль` column; `Заказной номер` remains the user-facing identifier column.
- Keep `Копировать ячейку` tied to the cell under the right-click position, not the whole selected row.
- Keep Lvl3 `Доп. оборудование` displaying saved order numbers only in the renamed `Заказной номер` column.
- Keep Lvl3 `Состав` and `Доп. оборудование` column widths as global persisted view preferences, not per selected tree node.
- Keep Lvl3 `Состав` and `Доп. оборудование` columns non-sortable; preserve domain row order regardless of header clicks.
- Keep right-click `Копировать ячейку` available in both Lvl3 grid-style tabs and tied to the clicked cell value.
- Move selected Network topology objects with arrow keys by exactly one visible grid division (`24 px`) per key press; keep the behavior tied to the existing grid size.
- Keep object snapping tied to the visible 24 px topology grid. Snap newly placed and dragged top-left coordinates; do not migrate existing untouched stored coordinates.
- Keep aligned fan-in/fan-out links separated by route lanes only in the draw path; do not add new persisted link routing metadata.
- Lvl2 Docs/Software menus reuse the existing selected-link workflow.
- Keep Lvl2 Docs/Software list widths screenshot-like: wide `Наименование`, medium `Путь`, narrow `Обновлено` / `Добавлено`.
- Keep Lvl2 Docs/Software user-resized list widths in global window-layout state and apply the same widths to schemes, instructions, and software lists.
- Lvl3 Composition right-click selects the Rack-grid row before opening the menu; edit/delete require a real slot entry, and add-slot stays available when a Rack is selected.
- Lvl3 Additional Equipment right-click selects the row before opening the menu; edit/delete require a selected row, and add stays available when editing is supported.
- Keep removed Lvl3 columns hidden in these views: `Состав` hides `Firmware`, `MPI/DP/PN`, `I address`, `Q address`, `IP-адрес`; `Доп. оборудование` hides `IP-адрес`. This is a UI display cleanup, not a model/data removal.
- Keep Lvl3 list/grid columns full-width: `Состав` uses fill-mode DataGridView columns with `Модуль` widest. `Доп. оборудование` uses the screenshot-reviewed proportions: narrow `№`, wide `Тип`, then `Компонент`, then `Примечание`; row values in the first column are simple numbers.
- Keep `Доп. оборудование` visually aligned with `Состав`: action row, scrollable section card, DataGridView in a bordered panel, but preserve additional-equipment row content and columns.
- Keep selected rows in Lvl3 `Состав` and `Доп. оборудование` explicit like `Документация и ПО`: use system highlight blue with system highlight text, not the softer shared grid accent.
- Keep `Firmware`, `MPI/DP/PN`, `I address`, `Q address`, and `IP-адрес` hidden in `KnowledgeBaseCompositionEntryDialog`; preserve existing saved values for these fields when editing entries.
- Keep `KnowledgeBaseCompositionEntryDialog` buttons inside the table layout, not as a bottom-docked overlay, so `Заказной номер` stays visible.
- Keep topology element coordinates logical and persisted as-is.
- Put the topology canvas inside a scrollable viewport so elements placed lower/right on a large monitor remain reachable on smaller screens.
- Add `Ctrl + mouse wheel` zoom around the pointer; keep ordinary mouse-wheel scrolling for vertical viewport movement.
- Add compact bottom-strip zoom controls (`-`, editable/drop-down percent field, `+`) so users can change/reset zoom without the hotkey; keep them in the same row without the separate `Масштаб` label.
- In the Network tab only, display the ET object kind and new-object prefix as `ET` rather than `ET200`; keep persisted enum/storage compatibility unchanged.
- Keep one optional extra IP address in the existing Network element dialog. Empty `Доп. IP` removes the additional address; filled `Доп. IP` is checked for duplicates across primary and additional IP addresses.
- Add external relationships as a first-class Network topology object kind `Внешняя связь`: it uses the existing element `Name` as the visible text, renders that text inside the card instead of using the regular device icon/IP layout, hides IP fields in the dialog in favor of a multiline `Текст` input, and opens new external-link elements with empty text for labels such as `КСПД`, `АКТ`, or `ВВК`.
- Soften topology card fonts with `TextRenderingHint.ClearTypeGridFit`, integer `Segoe UI` sizes, regular-weight object labels, bold external-link inner text, and wider bold IP badges with a smaller font so long addresses fit. If this is still visually rough after manual review, implement option 1 with `TextRenderer` and rounded screen-coordinate text bounds.
- Keep `docs/decision-log.md` as durable decisions only, not dated phase history, validation history, or handoff transcript.
- Use `scripts\codex-safe.ps1` by default for small/localized shell diagnostics; bypass only after explicit user approval in the current chat.
- Translate mouse hit testing, dragging, right-click add position, link endpoint movement, and link-type strip behavior through the current zoom.
- Draw topology node IP/name text through the scaled graphics pipeline so labels shrink/grow with cards and icons.
- Keep the link-kind selector as a thin fixed bottom strip under the scrollable viewport so it stays visible without covering the diagram. Use full labels and compact auto-measured button widths so link names are not clipped or ellipsized inside the buttons, without an internal legend scrollbar.
- Validation completed with app/core/tests format checks, isolated Release build, offscreen layout-smoke, fast publish, full core test suite, and `git diff --check` before the final `ET` visible-label adjustment. Additional-IP validation passed app/core/tests format, isolated Release build, targeted test, layout-smoke, fast publish, and full core tests (`408/408`). The final visual layout fix passed app format, isolated Release build, layout-smoke, and separate fast folder publish. Additional-IP edit follow-up passed app format, isolated Release build, layout-smoke, and standard fast publish after sequential restore. Inline `Доп. IP` follow-up passed app format, isolated Release build, layout-smoke, and standard fast publish. External-connection object follow-up passed app/core/tests format, isolated Release build, full core tests (`409/409`), layout-smoke, and standard fast publish. Final accepted package passed app/core/tests format, isolated Release build to `artifacts\build-check\network-topology-final`, full core tests (`409/409`), layout-smoke, standard fast publish, and `git diff --check` with CRLF normalization warnings only. Font-softening follow-up passed app format, isolated Release build to `artifacts\build-check\network-topology-font-softening`, layout-smoke, explicit `dotnet restore asutpKB.csproj -r win-x64`, and standard fast publish. IP/font-fit follow-up passed isolated Release build to `artifacts\build-check\network-topology-font-ip-fit`, layout-smoke, explicit runtime restore, and standard fast publish; full core tests were not rerun after these UI-only paint/font adjustments.

## Not active / out of scope

- Changing database format or storage location.
- Reworking WinForms startup into a full architecture rewrite.
- Removing the existing single-file publish mode.
- Reworking Network topology beyond the link-type selector placement.
- Rewriting stored topology coordinates or introducing automatic coordinate migration.
- Direct edits to real `.akb` or JSON user databases.

## Update rule

- Keep only active and near-term plans here.
- Remove completed or rejected items instead of growing a history log.
