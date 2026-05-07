# Plans

Last updated: `2026-05-07`

## Active plan

- Treat `Phase 7A`, `Phase 7B`, `Phase 7C`, and `Phase 7D` on branch `to` as the current working baseline
- Treat the full `Phase 7D follow-up` yearly orchestration as completed on `to`
- Treat the first `Phase 7E` slice as completed on `to`: manual per-profile annual `ТО1` / `ТО2` / `ТО3` placement stored in JSON
- Treat `Phase 7E.2` as completed on `to`: Excel export/import of the yearly placement source
- Treat the `Phase 7E` in-app mass-editing grid as completed on `to`
- Treat the major `ТО2` / `ТО3` split across working days as completed on `to`
- Treat the norm-import coverage and mismatch-reporting follow-up as completed on `to`
- Treat `phase7e-annual-norm-import` as accepted after manual review
- Treat local `phase7g-annual-norm-hidden-rows` as implemented and verified; it is waiting for manual review before commit/push
- Treat `Phase 7F` production-calendar configuration as completed on `to`
- Treat `Phase 7F.1. Production calendar PDF import` as accepted after manual review and committed/pushed on `to`
- Treat `Phase 11. Object templates and equipment catalog` as the active approved roadmap block
- Treat `Phase 11A. Equipment catalog model` as accepted after manual review
- Treat `Phase 11B. Equipment catalog UI` as accepted, committed, and pushed on `to`
- Treat `Phase 11C. Object template model` as accepted after manual review and committed/pushed on `to`
- Treat `Phase 11D. Create from template` as accepted after manual review and committed/pushed on `to`
- Treat `Phase 11E. Save existing object as template` as accepted after manual review and committed/pushed on `to`
- Treat `Phase 11F. Apply template with preview` as accepted after manual review and committed/pushed on `to`
- Treat `Phase 11G. Template import/export` as accepted after manual review and committed/pushed on `to`
- Treat the production-calendar Russian date format follow-up as accepted after manual review
- Treat `Phase 12. Storage redesign, backups, snapshots, and change history` as the active roadmap block
- Treat local `Phase 12A. Automatic JSON snapshots before save`, `Phase 12B. Manual JSON snapshots with note`, and `Phase 12C. Snapshot browser` as verified local prototype work that is paused before commit while storage moves to SQLite
- Treat `Phase 12S0. SQLite single-file storage redesign plan` in `docs/sqlite-storage-plan.md` as approved with choices `1A, 2B, 3A, 4A`
- Treat local `Phase 12S1. Storage abstraction` as implemented and verified
- Treat local `Phase 12S2. SQLite schema and repository` as implemented and verified
- Treat local `Phase 12S3. First-launch JSON migration` as implemented and verified
- Treat local `Phase 12S4. Database file UX` as implemented and verified
- Treat local `Phase 12S5. SQLite backups and snapshots` as implemented and verified
- Treat local `Phase 12S6. Restore selected snapshot` as implemented and verified
- Treat local `Phase 12S7. Snapshot comparison` as implemented and verified
- Treat `Phase 12S8. Change history` as accepted after manual review and committed/pushed on `to`
- Treat SQLite single-file `.akb` storage as the target live source of truth; keep JSON as import/export and first-launch migration compatibility
- Keep the yearly workbook as a generated/report artifact
- Keep all new user-facing UI strings Russian-only

## Near-term follow-up

- Manual review of `phase7g-annual-norm-hidden-rows`; if accepted, commit and push before selecting another roadmap task

## Update rule

- Keep only active and near-term plans here
- Remove completed items instead of growing a history log
