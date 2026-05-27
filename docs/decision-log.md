# Decision Log

Last updated: `2026-05-27`

This file keeps durable AKB5 decisions only. Do not use it as a changelog,
handoff transcript, validation log, or phase archive.

## Operating Agreements

- Common Codex operating rules live in `C:\Users\Olga\.codex\AGENTS.md`; this repository keeps only AKB5-specific rules and current task state.
- The active source of truth for current AKB5 work is `C:\Users\Olga\AKB5\docs`, especially `docs/codex-handoff.md` and `docs/plans.md`.
- Historical AKB5 worktrees and snapshots under `C:\Users\Olga\Documents\Codex\...` are reference material only, not current source of truth.
- Future work should keep durable decisions here, current status in `docs/codex-handoff.md`, active next steps in `docs/plans.md`, and reusable implementation lessons in `docs/lessons-learned.md`.
- For small or localized tasks, shell diagnostics should use `scripts\codex-safe.ps1` by default. The guard blocks broad reads/diffs and truncates output; bypassing it requires explicit user approval in the current chat.

## UI And UX

- AKB5 UI should not use hover or popup tooltips. Use visible labels, inline validation/status text, or modal validation messages.
- User-facing program UI should use Russian text.
- The selected object context belongs in one shared right-workspace header above the tab host, not duplicated inside each tab.
- Search is a typed domain workflow with fixed scopes: `All`, `Tree`, `Card`, `Composition`, and `Docs/Software`; navigation resolves to the owning tree node and may switch to the matched domain tab.
- `Documentation and Software` remains separate from `Composition`; file references use dedicated schemes, instructions, and software workflows.

## Storage And Files

- `.akb` SQLite storage is the main live format. JSON remains import/export and first-launch migration compatibility, not the normal live database.
- AKB5 is portable-first: store `akb5.settings.json` next to `asutpKB.exe`, default the database to `database\knowledge-base.akb` next to the program, and remember user-selected database paths.
- Before overwriting or restoring an existing `.akb`, create an external timestamped backup under `backups\yyyy-MM-dd\`.
- The first SQLite implementation does not support simultaneous multi-user editing.
- SQLite snapshots/history belong inside `.akb`; legacy JSON may report that history is unavailable.

## Publish And Startup

- Keep `scripts\publish.ps1` backward-compatible as the supported single-file publish flow.
- Keep `scripts\publish-fast.ps1` as the accepted faster working package: self-contained folder publish, ReadyToRun enabled, no single-file extraction.
- Startup timing diagnostics should go to existing file logs as `StartupTiming` events, not visible operator UI.
- If cold-start logs repeatedly show `mainform-data-loaded` dominating while SQLite remains fast, add narrower timing around UI session application/binding before changing startup architecture.

## Network Scope

- `Net` branch network work stays manual-entry and manual-review first.
- Do not start OCR/PDF auto-import, PRONETA/CSV import, live scan, plan/fact comparison, separate data-quality issue panels, or AKB5-driven IP/PROFINET-name assignment without a new explicit requirement.
- PDF network scheme references are metadata and `Open original` sources only; embedded PDF preview/rendering remains a separate dependency decision.
- Network passport dialog validation should be mirrored at the mutation-service boundary when another caller could otherwise accept invalid values.

## Network Topology

- Keep topology element coordinates logical and resolution-independent. Adapt to different monitors with a scrollable viewport and zoom instead of rewriting saved coordinates.
- Keep `Ctrl + mouse wheel` zoom, and also provide visible bottom-strip zoom controls with decrease, increase, and direct percent entry. `100%` is the original-size reset value.
- Draw topology node text through the same scaled graphics pipeline as cards/icons so labels scale with zoom.
- Link-type selection belongs in a fixed bottom strip below the topology viewport, not in a long toolbar ComboBox or floating canvas overlay.
- Link-type choices use full labels plus the real line color/dash sample. Button widths should be measured from full text so labels are not clipped or ellipsized.
- Clicking a link selects it; clicking a bottom-strip link kind edits the selected link immediately. With no selected link, the strip sets the kind for the next new link.
- The visible old `I/O` topology object option is replaced by generic `ET` in the Network tab. Keep the stored `Et200` enum name/value for compatibility with existing saved topology data.
- `OLM` remains a first-class topology object kind. The scheme legend includes optical/copper Profibus, copper MPI, and optical/copper Profinet link styles.
- External system relationships are represented by the `Внешняя связь` topology object kind. Its visible text is stored in the existing element `Name`, edited through a text field instead of IP fields, starts empty for new elements, and is drawn inside the card rather than as a regular device icon/IP layout.

## Composition And Templates

- The `Lvl3` `Состав` implementation uses a staged hybrid: keep composition on the right panel, group slotted components by Siemens-style `Rack0+`, and do not expand cabinet/board contents into `Lvl4` tree nodes by default.
- Rack rows may be empty without fake components; rack metadata stays with the composition workflow.
- Siemens slot rules are advisory: show S7-300-style hints/warnings, but do not hard-block real-world cabinet layouts.
- Object templates create fresh node ids during instantiation and remap typed records by template node id.
- Applying an object template must use an explicit preview and must not silently overwrite or delete existing user data.
- The retired direct composition-template application commands should not be reintroduced without a new explicit requirement.

## Maintenance Planning

- Maintenance profiles are top-level records keyed by `OwnerNodeId`; normalization keeps at most one profile per owner.
- Only engineering nodes get the maintenance workspace and maintenance-profile editing.
- Current maintenance periodicity is fixed: `ТО1` monthly, `ТО2` quarterly, `ТО3` annual.
- Inclusion rules are fixed: `ТО2` includes `ТО1`; `ТО3` includes `ТО1` and `ТО2`.
- Stored `ТО1` / `ТО2` / `ТО3` norms are non-negative integer labor hours per occurrence, not daily or monthly caps.
- The hard planner constraint is the selected monthly workshop budget. There is no hard daily `<= 8` cap in the current planner.
- Monthly schedule placement should optimize the whole working-month balance using `AK9 = monthly budget / working days` as the soft daily target.
- Monthly generation must still produce a feasible workbook when soft route/load preferences cannot all be satisfied.
- Same-owner repetition on one date remains a hard constraint; shift overload and same-day large-system mixing are last-resort penalties.
- Yearly generation is built on the existing monthly generator. Past months are frozen during ordinary replanning; future months can be recalculated through December.
- Annual maintenance norm import is the preferred source for yearly norm reconciliation when a generated annual plan is available.
- Hidden rows in annual norm workbooks represent retired equipment and must not create or update maintenance profiles.
- Production-calendar years are persisted in `KbConfig.ProductionCalendarYears`; PDF import should prefer a text layer first and keep OCR deferred until a real source requires it.

## Excel And Exchange

- Live Excel exchange contract is version `3` only: workbook id `AKB5.ExcelExchange`, sheets `Meta`, `Levels`, `Workshops`, plus one node worksheet per workshop.
- Excel exchange uses `DocumentFormat.OpenXml`; legacy `v1/v2` import is not supported.
- Catalog/template exchange is separate JSON workflow and should not change the live Excel `v3` database exchange contract.
