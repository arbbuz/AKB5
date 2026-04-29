# Plans

Last updated: `2026-04-29`

## Active plan

- Treat `Phase 7B`, `Phase 7C`, and `Phase 7D` on branch `to` as the current working baseline
- Stabilize the completed maintenance workflow instead of extending it blindly:
  - monthly demand resolver
  - monthly planner
  - yearly workbook export
  - maintenance-norm import
- Keep JSON as the source of truth and keep the yearly workbook as a generated/report artifact
- Keep all new user-facing UI strings Russian-only

## Near-term follow-up

- Decide whether one `ТО2` / `ТО3` occurrence should be splittable across multiple working days and, if approved, implement it end-to-end in planner plus Excel export
- Improve maintenance-norm import coverage and mismatch reporting for the rows from `123.xlsx` that still do not match the current KB tree cleanly
- Synchronize `AGENTS.md` and `Roadmap.md` with the current `to` branch, completed `Phase 7` slices, and the current maintenance-planning rules
- Keep the planner/export stack extensible so a future yearly schedule source can replace deterministic `ТО2` / `ТО3` month placement without redesigning `Phase 7`

## Update rule

- Keep only active and near-term plans here
- Remove completed items instead of growing a history log
