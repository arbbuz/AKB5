# Plans

Last updated: `2026-05-20`

## Active plan

- Treat `C:\Users\Olga\AKB5-design` on branch `design/network-ui-polish` as the active worktree for current AKB5 Network UI/UX polishing.
- Keep any `C:\Users\Olga\AKB5` / `Net` logic work isolated from this design package.
- Current design worktree baseline is `a89e593 Improve network passport manual entry hints`.
- Current accepted parent `Net` baseline in `C:\Users\Olga\AKB5` / `origin/Net` is `b2ea12e Enforce network passport mutation validation`.
- Do not commit, push, merge, rebase, delete stash entries, or remove worktrees unless the user explicitly asks in the current chat.
- Keep future Net packages in manual-entry / manual-review ergonomics unless the user approves a broader scope.
- Bundle closely related Net manual-entry/UI refinements into one coherent package instead of splitting them into tiny micro-stages.
- Do not run interactive UI-smoke unless the user explicitly asks. Prefer non-invasive/offscreen layout-smoke for Network UI changes.
- Keep all new user-facing UI strings Russian-only.
- Follow `docs/codex-operational-rules.md` for every future Codex turn to control silent stalls and context growth.

## Near-term follow-up

The uncommitted `design/network-ui-polish` Network review-filter/UI package plus tree icon readability polish is ready for manual review in `C:\Users\Olga\AKB5-design`.

Current handoff state:

- review-only filter/copy, warning counters, filter layout fix, warning-row highlighting, wider `Проверка` columns, and filter/action tooltips are included;
- object-tree icons for Lvl1/Lvl2/Lvl3 are enlarged from 20px to 30px using Google Material Symbols path data while preserving current colors;
- focused Network tests, focused tree tests, full Release tests, app format check, `git diff --check`, the non-invasive/offscreen Network layout-smoke, and tree-icon smoke passed;
- current isolated Release artifact: `C:\Users\Olga\AKB5-design\artifacts\build-check\tree-icons-50pct-20260520-224516\asutpKB.exe`;
- recheck the main `Net` worktree before using it; it is expected to be clean at `b2ea12e`.

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
