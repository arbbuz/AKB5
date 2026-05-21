# Plans

Last updated: `2026-05-22`

## Active plan

- Treat `C:\Users\Olga\AKB5-design` on branch `design/network-ui-polish` as the active worktree for current AKB5 Network UI/UX polishing.
- Keep any `C:\Users\Olga\AKB5` / `Net` logic work isolated from this design package.
- Current design worktree head after the right-panel style-only package is the latest commit on `design/network-ui-polish`.
- Current accepted parent `Net` baseline in `C:\Users\Olga\AKB5` / `origin/Net` is `b2ea12e Enforce network passport mutation validation`, but the latest 2026-05-21 local check showed dirty Net-side files; recheck before using or merging.
- Do not commit, push, merge, rebase, delete stash entries, or remove worktrees unless the user explicitly asks in the current chat.
- Keep future Net packages in manual-entry / manual-review ergonomics unless the user approves a broader scope.
- Bundle closely related Net manual-entry/UI refinements into one coherent package instead of splitting them into tiny micro-stages.
- Do not run interactive UI-smoke unless the user explicitly asks. Prefer non-invasive/offscreen layout-smoke for Network UI changes.
- Keep all new user-facing UI strings Russian-only.
- Follow `docs/codex-operational-rules.md` for every future Codex turn to control silent stalls and context growth.

## Near-term follow-up

The `design/network-ui-polish` Network review-filter/UI package plus tree icon readability, toolbar/menu, splitter, and workshop selector polish is committed/pushed through `a2da5aa` in `C:\Users\Olga\AKB5-design`. The current design package adds `Forms/MainForm.Layout.cs` status-bar fixed height and the style-only right-panel controls/helper changes.

Current handoff state:

- review-only filter/copy, warning counters, filter layout fix, warning-row highlighting, wider `Проверка` columns, and filter/action tooltips are included;
- object-tree icons for Lvl1/Lvl2/Lvl3 are enlarged from 20px to 30px using Google Material Symbols path data while preserving current colors;
- main shell toolbar/menu, light surface, splitter, and workshop selector polish are included in `a2da5aa`;
- focused Network tests, focused tree tests, full Release tests, app/core/tests format checks, `git diff --check`, the non-invasive/offscreen Network layout-smoke, and tree-icon smoke passed before the later narrow shell/status-bar follow-ups; targeted format/diff/build checks passed for those narrow UI changes in the earlier session;
- recovery check on 2026-05-21: `git diff --check -- Forms/MainForm.Layout.cs` passed for the current uncommitted status-bar diff;
- current isolated Release artifact for manual review: `C:\Users\Olga\AKB5-design\artifacts\build-check\statusbar-fixed-height-20260521\asutpKB.exe`;
- approved visual direction: `C:\Users\Olga\AKB5-design\artifacts\previews\right-panel-style-only-components-preview.html`, style-only right-panel refresh; do not change layout, tab order, object order, icons, tree placement, button placement, or behavior;
- current right-panel style-only pass applies soft section/table/field/button/empty-state shell across existing right-panel controls, and the latest fix replaces standard flat button painting with owner-drawn workspace buttons to remove hover/focus border artifacts;
- latest isolated Release artifact for manual review: `C:\Users\Olga\AKB5-design\artifacts\build-check\right-panel-button-artifacts-20260521-231829\asutpKB.exe`;
- pre-commit validation on 2026-05-22 passed: `git diff --check`, app/core/tests format checks, Release build, and Release tests `433/433`;
- recheck the main `Net` worktree before using it; latest local check showed dirty files.

Good next directions should stay within manual passport editing and review comfort. Do not start OCR/PDF auto-import, PRONETA/CSV import, live scan, plan/fact comparison, separate data-quality issue/problem panels, AKB5-driven IP/PROFINET-name assignment, or embedded PDF preview unless the user explicitly changes scope.

## Not active / out of scope

- OCR or PDF auto-import for network schemes.
- PRONETA/CSV import.
- Live network scan.
- Plan/fact comparison.
- Separate data-quality issue/problem panel.
- Assigning IP addresses or PROFINET names from AKB5.
- Embedded PDF preview/rendering dependency.
- Phase 8 through Phase 10 remain discussed candidate directions, not active implementation phases.

## Update rule

- Keep only active and near-term plans here.
- Remove completed items instead of growing a history log.
