# Plans

Last updated: `2026-04-30`

## Active plan

- Treat `Phase 7A`, `Phase 7B`, `Phase 7C`, and `Phase 7D` on branch `to` as the current working baseline
- Treat the full `Phase 7D follow-up` yearly orchestration as implemented pending manual review
- Treat the first `Phase 7E` slice as implemented pending manual review: manual per-profile annual `ТО1` / `ТО2` / `ТО3` placement stored in JSON
- Keep JSON as the source of truth and keep the yearly workbook as a generated/report artifact
- Keep all new user-facing UI strings Russian-only

## Near-term follow-up

- Manually review the `phase7e-year-schedule-source` build before commit/push
- After review, decide whether to:
  - accept `Phase 7E` at the manual per-profile source layer
  - add import/external-source hardening for `Phase 7E`
  - support splitting one `ТО2` / `ТО3` occurrence across multiple working days
  - improve maintenance-norm import coverage and mismatch reporting for the remaining unmatched rows from `123.xlsx`
  - keep `Phase 7F` production-calendar configuration deferred until it becomes a priority

## Update rule

- Keep only active and near-term plans here
- Remove completed items instead of growing a history log
