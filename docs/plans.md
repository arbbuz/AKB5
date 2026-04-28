# Plans

Last updated: `2026-04-28`

## Active plan

- Continue roadmap implementation from `Phase 7`
- Replace the old `Phase 7` workbook-modernization direction with maintenance-schedule generation
- Build a template-driven yearly Excel workbook with one sheet per month using the approved enterprise form
- Add typed maintenance-planning data keyed to tree nodes, including separate integer hour norms for `ТО1`, `ТО2`, and `ТО3`, plus inventory-number support for `Lvl2`
- Keep Excel `v3` readable as legacy, but do not expand it as the main feature direction
- Keep all new user-facing UI strings Russian-only
- Preserve JSON source-of-truth and avoid accidental contract drift across typed collections

## Near-term follow-up

- Treat `Phase 6` network workflow as stabilized unless the user reports a concrete regression
- Prepare a cleaned internal template derived from `C:\Users\Olga\Downloads\123.xlsx`
- Implement a Russian production-calendar service for working-day allocation and monthly hour-cap validation
- Use a deterministic per-node cycle offset for `ТО2` / `ТО3` placement until a formal yearly schedule source is available
- Keep the maintenance planner extensible so a future yearly schedule can become the source of `ТО1` / `ТО2` / `ТО3` placement
- Use an explicit session-knowledge refresh request after substantial sessions to refresh the harness in `docs/`

## Update rule

- Keep only active and near-term plans here
- Remove completed items instead of growing a history log
