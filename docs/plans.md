# Plans

Last updated: `2026-05-13`

## Active plan

- Treat `Phase 7A`, `Phase 7B`, `Phase 7C`, and `Phase 7D` on branch `to` as the current working baseline
- Treat the full `Phase 7D follow-up` yearly orchestration as completed on `to`
- Treat the first `Phase 7E` slice as completed on `to`: manual per-profile annual `ТО1` / `ТО2` / `ТО3` placement stored in JSON
- Treat `Phase 7E.2` as completed on `to`: Excel export/import of the yearly placement source
- Treat the `Phase 7E` in-app mass-editing grid as completed on `to`
- Treat the major `ТО2` / `ТО3` split across working days as completed on `to`
- Treat the norm-import coverage and mismatch-reporting follow-up as completed on `to`
- Treat `phase7e-annual-norm-import` as accepted after manual review
- Treat `phase7g-annual-norm-hidden-rows` as committed/pushed on `to` as `7a4895d Fix annual maintenance norm import totals`
- Treat the annual norm import HAVER matching and resolved-profile re-enable follow-up as verified locally; annual workbooks should reconcile monthly demand directly when all rows are matched.
- Treat the TO profile dialog annual-plan UX follow-up as locally implemented: the annual plan is applied/cleared explicitly without closing the dialog, missing months display as `Авто`, norm-based months display effective profile hours instead of `0`, and imported per-month hours are preserved unless edited.
- Treat the right-workspace selected-object context follow-up as locally implemented: selected object name/path are shown in a shared header above all right-side tabs.
- Treat `Phase 7F` production-calendar configuration as completed on `to`
- Treat `Phase 7F.1. Production calendar PDF import` as accepted after manual review and committed/pushed on `to`
- Treat `Phase 11. Object templates and equipment catalog` as an accepted roadmap block
- Treat `Phase 11A. Equipment catalog model` as accepted after manual review
- Treat `Phase 11B. Equipment catalog UI` as accepted, committed, and pushed on `to`
- Treat `Phase 11C. Object template model` as accepted after manual review and committed/pushed on `to`
- Treat `Phase 11D. Create from template` as accepted after manual review and committed/pushed on `to`
- Treat `Phase 11E. Save existing object as template` as accepted after manual review and committed/pushed on `to`
- Treat `Phase 11F. Apply template with preview` as accepted after manual review and committed/pushed on `to`
- Treat `Phase 11G. Template import/export` as accepted after manual review and committed/pushed on `to`
- Treat the production-calendar Russian date format follow-up as accepted after manual review
- Treat `Phase 12. Storage redesign, backups, snapshots, and change history` as an accepted roadmap block
- Treat local `Phase 12A. Automatic JSON snapshots before save`, `Phase 12B. Manual JSON snapshots with note`, and `Phase 12C. Snapshot browser` as verified local prototype work that is paused before commit while storage moves to SQLite
- Treat `Phase 12S0. SQLite single-file storage redesign plan` in `docs/sqlite-storage-plan.md` as approved with choices `1A, 2B, 3A, 4A`
- Treat `Phase 12S1. Storage abstraction` as accepted and committed/pushed on `to`
- Treat `Phase 12S2. SQLite schema and repository` as accepted and committed/pushed on `to`
- Treat `Phase 12S3. First-launch JSON migration` as accepted and committed/pushed on `to`
- Treat `Phase 12S4. Database file UX` as accepted and committed/pushed on `to`
- Treat `Phase 12S5. SQLite backups and snapshots` as accepted and committed/pushed on `to`
- Treat `Phase 12S6. Restore selected snapshot` as accepted and committed/pushed on `to`
- Treat `Phase 12S7. Snapshot comparison` as accepted and committed/pushed on `to`
- Treat `Phase 12S8. Change history` as accepted after manual review and committed/pushed on `to`
- Treat menu rework first iteration steps 1-6 as accepted and committed/pushed on `to` as `8dfffbd Rework menu structure and safety prompts`
- Treat the equipment catalog miniSAP import, catalog visible-field cleanup, composition catalog picker, catalog window layout/sort, and catalog picker layout follow-ups as committed/pushed on `to` as `3eadf7f Refine equipment catalog workflows`
- Treat portable-first storage and external `.akb` backups as locally verified and awaiting review
- Treat SQLite single-file `.akb` storage as the target live source of truth; keep JSON as import/export and first-launch migration compatibility
- Keep the yearly workbook as a generated/report artifact
- Keep all new user-facing UI strings Russian-only
- Follow `docs/codex-operational-rules.md` for every future Codex turn to control silent stalls and context growth.

## Near-term follow-up

- Template cleanup: the obsolete `Состав -> Применить шаблон...` button and tree menu commands `Применить шаблон к объекту...` / `Шаблоны -> Добавить из шаблона состава...` are removed from the current UI.
- Remaining optional template task: add a user-managed workflow to create/edit/delete composition or object templates, preferably backed by real catalog-selected equipment.
- Review and accept portable-first storage: `akb5.settings.json` beside `asutpKB.exe`, default `database\knowledge-base.akb`, first-launch folder choice, remembered open/save-as path, old AppData `.akb` copy prompt, and external backups in `backups\yyyy-MM-dd\`.
- Do not start Phase 8-10 or a broad roadmap slice until portable-first storage is accepted; a narrow template-management correction may be done first if the user explicitly requests it.
- Deferred menu follow-ups remain future work, not part of the accepted first iteration: password/role access to `Сервис`, ordinary-user edit restrictions, and broader rights separation.

## Update rule

- Keep only active and near-term plans here
- Remove completed items instead of growing a history log
