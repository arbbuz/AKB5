# Plans

Last updated: `2026-05-04`

## Active plan

- Treat `Phase 7A`, `Phase 7B`, `Phase 7C`, and `Phase 7D` on branch `to` as the current working baseline
- Treat the full `Phase 7D follow-up` yearly orchestration as completed on `to`
- Treat the first `Phase 7E` slice as completed on `to`: manual per-profile annual `ТО1` / `ТО2` / `ТО3` placement stored in JSON
- Treat `Phase 7E.2` as completed on `to`: Excel export/import of the yearly placement source
- Treat the `Phase 7E` in-app mass-editing grid as the active local implementation slice pending manual review
- Keep JSON as the source of truth and keep the yearly workbook as a generated/report artifact
- Keep all new user-facing UI strings Russian-only

## Near-term follow-up

- Manually review the verified `phase7e-year-source-mass-edit-grid` build, then commit/push if accepted
- After review, decide whether to:
  - support splitting one `ТО2` / `ТО3` occurrence across multiple working days
  - improve maintenance-norm import coverage and mismatch reporting for the remaining unmatched rows from `123.xlsx`
  - keep `Phase 7F` production-calendar configuration deferred until it becomes a priority

## Update rule

- Keep only active and near-term plans here
- Remove completed items instead of growing a history log
