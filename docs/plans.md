# Plans

Last updated: `2026-05-20`

## Active plan

- Treat branch `Net` as the active branch for the current AKB5 network-passport work.
- Current baseline is `207b6b1 Improve network passport review ergonomics` on `HEAD` and `origin/Net`.
- The latest Net manual-review ergonomics package was accepted manually, committed, and pushed before this handoff.
- Do not commit or push future changes unless the user explicitly asks in the current chat.
- The current uncommitted package is manual-entry speed plus inline duplicate hints: `Добавить похожее` / `Добавить похожий` for devices/interfaces/connections and row-level `Проверка` duplicate hints.
- Keep future Net packages in manual-entry / manual-review ergonomics unless the user approves a broader scope.
- Bundle closely related Net manual-entry/UI refinements into one coherent package instead of splitting them into tiny micro-stages.
- Current package validation passed focused Network tests, full Release tests, app build, format checks, `git diff --check`, and non-invasive/offscreen layout-smoke.
- Do not run interactive UI-smoke unless the user explicitly asks. Prefer non-invasive/offscreen layout-smoke for Network UI changes.
- Keep all new user-facing UI strings Russian-only.
- Follow `docs/codex-operational-rules.md` for every future Codex turn to control silent stalls and context growth.

## Near-term follow-up

Manual-review the current local package before coding the next Net direction.

- review `C:\Users\Olga\AKB5\artifacts\build-check\network-manual-entry-hints-20260520-132919\asutpKB.exe`;
- if accepted, wait for explicit current-chat approval before commit/push;
- if review finds issues, fix them inside this same package before moving on.

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
