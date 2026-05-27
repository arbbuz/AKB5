# Plans

Last updated: `2026-05-27`

## Active plan

- Work in `C:\Users\Olga\AKB5` on branch `Net`, tracking `origin/Net`.
- Current task: Network topology font rendering follow-up is implemented locally after pushed commit `5f5a993 Improve network topology controls`. User reported jagged/ragged text in the topology; option 2 was requested first: ClearType rendering hint plus less aggressive integer-sized fonts. Follow-up feedback said IP badges were clipped and external-link text should stay bold. Manual review is accepted and commit/push is explicitly authorized in the current chat.
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
- Next action is to wait for the next user request.
- Next possible optimization after measurement: if cold logs repeatedly show `mainform-data-loaded` dominating while SQLite remains fast, add narrower UI binding/session-application timing first; defer database load until after first form display only if that is still justified.

## Current implementation plan

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
