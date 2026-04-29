# Plans

Last updated: `2026-04-29`

## Active plan

- Continue roadmap implementation from `Phase 7` after the finished `Phase 7A foundation` slice
- Keep the current foundation in branch as the baseline:
  - `Lvl2` inventory number support
  - typed `MaintenanceScheduleProfiles`
  - `График ТО` workspace and editing UI for engineering nodes
- Build a template-driven yearly Excel workbook with one sheet per month using the approved enterprise form
- Implement the Russian production-calendar service and monthly hour-cap validation for planner allocation
- Keep Excel `v3` readable as legacy, but do not expand it as the main feature direction
- Keep all new user-facing UI strings Russian-only
- Preserve JSON source-of-truth and avoid accidental contract drift across typed collections

## Near-term follow-up

- Prepare a cleaned internal template derived from `C:\Users\Olga\Downloads\123.xlsx`
- Implement deterministic monthly planner generation from `ТО1` / `ТО2` / `ТО3` norms and per-node cycle offsets
- Add workbook export into the approved form with generated `план` rows and blank `факт`
- Keep the maintenance planner extensible so a future yearly schedule can become the source of `ТО1` / `ТО2` / `ТО3` placement
- Use an explicit session-knowledge refresh request after substantial sessions to refresh the harness in `docs/`

## Update rule

- Keep only active and near-term plans here
- Remove completed items instead of growing a history log
