# Decision Log

Last updated: `2026-05-13`

## 2026-05-13

- Annual maintenance norm import remains the recommended source for ТО norms and yearly placement when a generated annual plan is available; the monthly workbook is not a complete norm source for yearly demand reconciliation.
- HAVER annual-plan rows from `Годовой_график _ТО_АСУТП_КЦ_2026г.xlsx` are matched by conservative name variants that trim `Линия фасовки HAVER` from equipment names when the KB system is named `АСУ линии фасовки HAVER FFS600`, and can also trim a final Latin model code such as `FFS600`.
- A resolved annual-plan row must set `IsIncludedInSchedule=true` on the existing maintenance profile. Otherwise a row present in the annual plan can update hours/year placement but still be omitted from monthly demand.

## 2026-05-12

- Codex operational rules for avoiding silent turn stalls and rapid context growth are mandatory for future AKB5 work; see `docs/codex-operational-rules.md`. Key rules: interrupt/resume after 2-3 minutes with no progress after a completed tool result, aggregate broad diagnostics before output, avoid full `git diff` unless explicitly requested or narrowed, and use fresh sessions after large investigations or handoff checkpoints.
- Equipment catalog workflow follow-ups were committed/pushed on `to` as `3eadf7f Refine equipment catalog workflows`; they include miniSAP Siemens catalog import, four-column catalog UI/search, catalog selection from composition dialogs, sortable catalog columns, and saved layout/column widths for both the catalog browser and catalog picker.
- The obsolete `Состав -> Применить шаблон...` button and tree menu commands `Применить шаблон к объекту...` / `Шаблоны -> Добавить из шаблона состава...` are retired from the current UI. Keep object-template creation/saving and catalog/template exchange available, but do not reintroduce direct template application or hardcoded composition-template add workflows without a new explicit requirement.
- Approved storage follow-up: keep `.akb` as the main format, make the app portable-first without a marker file, store `akb5.settings.json` next to `asutpKB.exe`, default to `database\knowledge-base.akb` next to the program, let the first launch choose another database folder, remember later open/save-as database paths, and create external timestamped `.akb` backups under `backups\yyyy-MM-dd\` before overwriting/restoring an existing SQLite database.
- Durable reporting rule: after every code or data change that produces or verifies a local build, the response must include the full Windows path to the local `asutpKB.exe` copy for manual review, preferably as the first line of the final response; do not rely only on a shortened or markdown-only link.

## 2026-05-10

- Menu rework first iteration steps 1-6 were accepted and committed/pushed on `to` as `8dfffbd Rework menu structure and safety prompts`; final pre-commit verification passed format checks, `menu-rework-stage6` build/test (`340/340`), and `git diff --check`.
- No next coding phase is currently prioritized after the accepted menu rework; future Phase 8-10 or roadmap-slice work requires an explicit user request.

## 2026-05-08

- Durable communication rule for AKB5 work: user-visible progress must stay brief (`current step`, `result`, `next step`). Codex must not paste raw diagnostic commands, command transcripts, internal ids, owner mappings, large diffs, or code excerpts into chat unless explicitly requested; verbose diagnostics belong in `artifacts` with only key counts/status summarized.
- Durable safety rule for AKB5 work: irreversible or external actions require current-task authorization. Codex must not reuse older approval for `git commit`, `git push`, destructive file operations, branch rewrites, or broad database edits. Before changing a real `.akb`/JSON data file, Codex must identify the exact file, target object count, and fields/records to be changed; if visible data is not found, verify encoding, active storage, process state, and alternate files before drawing conclusions.
- Menu rework first-iteration decision: use a top-level `Сервис` menu instead of `Администрирование`; keep workshop commands in `Файл` for now; keep `ТО` immediately after `Файл`; add `Справочники` for `Каталог оборудования...`; move service JSON/catalog exchange and reload commands to `Сервис`; hide full database Excel exchange and production-calendar JSON import from menus while keeping code under the hood; combine snapshots/history into one entry; group tree template commands under `Шаблоны`; show `Добавить отдел` only on empty tree space; include old/new level in move confirmation. Passwords and roles are deferred.
- `phase7g-annual-norm-hidden-rows` was committed/pushed on `to` as `7a4895d Fix annual maintenance norm import totals`.
- Tree move level metadata handling was corrected and committed/pushed on `to` as `4d334a7 Fix tree move level metadata handling`.

## 2026-05-06

- `Phase 7F.1` imports production-calendar PDF files through a text layer first, shows a preview before applying changes, and keeps OCR deferred until a real source PDF requires it
- `KbProductionCalendarYear` now supports `AdditionalWorkingDays` so transferred working Saturdays/Sundays can be represented together with additional non-working days
- `C:\Users\Olga\Downloads\calendar_2027.pdf` has a usable text layer and imports as 2027 with additional non-working days `22.02.2027`, `03.05.2027`, `10.05.2027`, `14.06.2027`, `05.11.2027`, `31.12.2027`, plus additional working day `20.02.2027`
- On 2026-05-06, `phase7f1-production-calendar-pdf-import` passed verification build and `dotnet test` (`281/281`) using isolated output paths; the user manually confirmed that production-calendar PDF import works
- `Phase 7F.1` was committed and pushed on `to` as `09bf84d Add production calendar PDF import`
- `Phase 11B` adds an in-app Russian equipment-catalog editor, now exposed as `Справочники -> Каталог оборудования...`; catalog editing remains separate from tree editing and object-template creation
- On 2026-05-06, `phase11b-equipment-catalog-ui` passed verification build and `dotnet test` (`287/287`) using isolated output paths
- `Phase 11B` was committed and pushed on `to` as `f80873f Add equipment catalog UI`
- `Phase 11C` adds top-level JSON/session object-template persistence and normalization without adding the create-from-template UI yet
- Object templates store stable `TemplateNodeId` values instead of persisted real `NodeId` values; instantiation creates fresh `NodeId` values and remaps card defaults, composition, documents/software, network file references, maintenance profile stubs, and future network-interface stubs by template node id
- On 2026-05-06, `phase11c-object-template-model` passed verification build and `dotnet test` (`292/292`) using isolated output paths; manual review passed together with Phase 11D
- `Phase 11D` adds creation of a new tree object from persisted `SavedData.ObjectTemplates` through a Russian context-menu dialog; it does not create/edit/save templates yet
- Creating from an object template inserts the full template subtree, reindexes it through the normal tree rules, generates fresh `NodeId` values, and appends remapped composition, document/software, network-file, and maintenance-profile defaults
- On 2026-05-06, `phase11d-create-from-template` passed verification build and `dotnet test` (`294/294`) using isolated output paths; manual review passed and the user requested commit/push before Phase 11E
- `Phase 11C` / `Phase 11D` were committed and pushed on `to` as `3caca67 Add object template creation workflow`
- Local `Phase 11E` saves a selected existing object subtree as a persisted object template; it generates fresh template-node ids, strips real node ids, remaps typed owner references inside the selected subtree, skips records outside that subtree, and leaves source object data unchanged
- On 2026-05-06, `phase11e-save-object-as-template` passed verification build and `dotnet test` (`296/296`) using isolated output paths; manual review passed and the user requested continuing without push
- `Phase 11E` was committed as `3c87b6e Add save object as template workflow`; it was later pushed to `to` together with `Phase 11F`
- `Phase 11F` applies a selected object template to an existing object only after showing an explicit preview of added, skipped, and unchanged data; it adds missing subtree nodes and typed records, fills only empty supported card fields, and never overwrites or deletes existing user data
- On 2026-05-06, `phase11f-apply-template-preview` passed verification build and `dotnet test` (`299/299`) using isolated output paths; manual review passed
- After manual review found mojibake in a template context-menu string, Russian template workflow context-menu/dialog/status strings and affected test literals were corrected; post-review targeted regression passed (`55/55`), the generated UTF-8/CP1251 corruption-pattern scan returned `TOTAL=0`, and `phase11f-apply-template-preview` still passed (`299/299`)
- `Phase 11E` / `Phase 11F` were committed and pushed on `to` as `3c87b6e Add save object as template workflow` and `ca43298 Add apply object template preview workflow`
- Local `Phase 11G` exports equipment catalog records and object templates to a dedicated UTF-8 JSON exchange file and imports that file back through a safe merge where existing catalog/template ids and catalog semantic duplicates are not overwritten
- On 2026-05-06, targeted catalog/template exchange tests passed (`14/14`), app/core/tests format verification passed, and `phase11g-template-import-export` passed verification build and `dotnet test` (`302/302`) using isolated output paths
- After manual review found that `Состав шаблона` was too narrow in the create-object-from-template dialog, the dialog layout was corrected and `phase11g-template-import-export-layout-fix` passed verification build and `dotnet test` (`302/302`) using isolated output paths
- After manual review found an empty preview with an inactive `Применить` button in the apply-object-template dialog, the dialog now selects the first template explicitly, rebuilds the preview when shown, displays no-change/failure text, and `phase11g-template-import-export-apply-preview-ui-fix` passed verification build and `dotnet test` (`302/302`) using isolated output paths
- The user confirmed manual review of `Phase 11G`; it was later pushed to `to` as `268b550`
- `Phase 12. Backup, snapshots, and change history` is now the active approved roadmap step
- Local `Phase 12A` creates a timestamped JSON snapshot in `.akb-snapshots` before overwriting an existing database file, preserves the existing `.bak` fallback copy, and aborts save without overwriting if the snapshot cannot be created
- On 2026-05-06, app/core/tests format verification passed, targeted JSON storage/snapshot tests passed (`12/12`), and `phase12a-automatic-json-snapshots` passed verification build and `dotnet test` (`306/306`) using isolated output paths; local `Phase 12A` is now treated as verified prototype work paused before commit while storage moves to SQLite

## 2026-05-07

- Local `Phase 12B` adds `Файл -> Создать снимок базы...`, requires a user note, writes the current in-memory JSON state to `.akb-snapshots` without changing the main JSON file, and stores note/source/timestamp/size in a `.meta.json` sidecar for future snapshot browser/restore work
- On 2026-05-07, app/core/tests format verification passed, targeted JSON storage/snapshot/file-workflow tests passed (`22/22`), and `phase12b-manual-json-snapshots` passed verification build and `dotnet test` (`309/309`) using isolated output paths; local `Phase 12B` is carried into the `Phase 12C` review stack and must not be committed/pushed before acceptance
- Local `Phase 12C` adds `Файл -> Просмотреть снимки базы...` and a read-only snapshot browser that lists current database snapshots from `.akb-snapshots` with date, type, snapshot file, source file, size, and note
- `Phase 12C` reads manual snapshot notes from `.meta.json` sidecars and derives fallback date/type/source/size for automatic snapshots that have no metadata sidecar
- On 2026-05-07, app/core/tests format verification passed, targeted JSON storage/snapshot/file-workflow tests passed (`25/25`), and `phase12c-snapshot-browser` passed verification build and `dotnet test` (`312/312`) using isolated output paths; local `Phase 12C` is now treated as verified prototype work paused before commit while storage moves to SQLite
- The chief developer rejected using a live JSON database in `Мои документы`; the approved target direction is SQLite single-file storage with JSON import/export compatibility and first-launch migration from the existing legacy JSON database
- JSON-specific `Phase 12A` / `12B` / `12C` snapshot work is treated as verified local prototype work and is paused before commit; the SQLite storage implementation plan is approved in `docs/sqlite-storage-plan.md`
- The previously proposed live database path was `%LocalAppData%\AKB5\knowledge-base.akb`; this was later superseded by the portable-first 2026-05-12 follow-up, while the legacy `Мои документы\ASUTP_KnowledgeBase.json` file still must remain untouched during first-launch migration
- The chief developer approved the SQLite storage plan choices as `1A, 2B, 3A, 4A`: visible `.akb` extension, confirmation dialog before first-launch migration, automatic post-migration JSON safety export next to the new `.akb`, and no simultaneous multi-user editing support in the first SQLite version
- With `Phase 12S0` approved, the next implementation step is `Phase 12S1. Storage abstraction`; SQLite code should not be added before direct `JsonStorageService` dependencies are moved behind an app-facing storage interface
- Local `Phase 12S1` introduces `IKnowledgeBaseStorageService`, `KnowledgeBaseStorageLoadResult`, and `KnowledgeBaseStorageServiceFactory`; current JSON persistence remains the implementation behind the abstraction and no SQLite dependency is added in this slice
- `KnowledgeBaseFileWorkflowService` now depends on `IKnowledgeBaseStorageService`, and `Forms` no longer creates `JsonStorageService` directly
- On 2026-05-07, targeted storage/file-workflow/snapshot tests passed (`27/27`), app/core/tests format verification passed, and `phase12s1-storage-abstraction` passed verification build and `dotnet test` (`314/314`); pre-acceptance harness state was `WAITING_REVIEW`
- The user requested starting `Phase 12S2` after local `Phase 12S1` verification
- Local `Phase 12S2` adds `Microsoft.Data.Sqlite` `8.0.13`, `KnowledgeBaseSqliteConnectionFactory`, and `SqliteKnowledgeBaseStorageService`; it creates SQLite schema version `1` and round-trips normalized `SavedData` through the storage abstraction without switching the UI default away from JSON
- SQLite schema version `1` stores metadata, config, production calendars, workshops, nodes, typed records, maintenance profiles/year entries, catalog records/properties, object templates, and template nodes in dedicated tables
- On 2026-05-07, targeted SQLite storage tests passed (`3/3`), targeted storage/file-workflow/snapshot tests passed (`30/30`), app/core/tests format verification passed, and `phase12s2-sqlite-schema-repository` passed verification build and `dotnet test` (`317/317`); pre-acceptance harness state was `WAITING_REVIEW`
- Local `Phase 12S3` adds first-launch migration from legacy `Мои документы\ASUTP_KnowledgeBase.json` to `%LocalAppData%\AKB5\knowledge-base.akb`; migration is offered only when the `.akb` file is missing and legacy JSON exists, requires user confirmation in the UI, leaves the JSON source unchanged, and writes a post-migration JSON safety export next to the `.akb`
- On 2026-05-07, `phase12s3-first-launch-json-migration` passed verification build and `dotnet test` (`322/322`); pre-acceptance harness state was `WAITING_REVIEW`
- Local `Phase 12S4` switches the default live path to `.akb` through `KnowledgeBaseRoutedStorageService`, keeps legacy JSON readable by extension, updates open/save dialogs for `.akb`, and adds full database JSON import/export commands separate from catalog/template JSON exchange
- On 2026-05-07, `phase12s4-database-file-ux-json-compatibility` passed verification build and `dotnet test` (`325/325`); pre-acceptance harness state was `WAITING_REVIEW`
- Local `Phase 12S5` stores SQLite snapshots inside the `.akb` database instead of `.akb-snapshots` sidecars for SQLite-backed storage; manual snapshots and automatic before-save snapshots write metadata and a normalized `SavedData` payload
- On 2026-05-07, `phase12s5-sqlite-snapshots` passed verification build and `dotnet test` (`328/328`); pre-acceptance harness state was `WAITING_REVIEW`
- Local `Phase 12S6` restores a selected SQLite snapshot only after explicit confirmation, creates a protective `before-restore` snapshot first, reloads the UI from restored data, and leaves failed restores without replacing current data
- On 2026-05-07, `phase12s6-snapshot-restore` passed verification build and `dotnet test` (`330/330`); pre-acceptance harness state was `WAITING_REVIEW`
- Local `Phase 12S7` adds snapshot comparison at summary level across high-value data areas and exposes it from the snapshot browser / `Файл -> Сравнить снимки...`
- On 2026-05-07, `phase12s7-snapshot-comparison` passed verification build and `dotnet test` (`332/332`); pre-acceptance harness state was `WAITING_REVIEW`
- Local `Phase 12S8` adds SQLite change history records for save, migration, manual snapshot, restore, and catalog/template import, plus a read-only `Файл -> История изменений...` view for `.akb` databases; legacy JSON reports that history is unavailable
- On 2026-05-07, `phase12s8-change-history` passed verification build and `dotnet test` (`333/333`); pre-acceptance harness state was `WAITING_REVIEW`
- The user requested committing and pushing all accepted changes; `Phase 11G` was pushed as `268b550 Add catalog template JSON exchange`, and the SQLite storage/change-history stack through `Phase 12S8` was committed/pushed as `27a2aba Add SQLite storage history workflow`
- The accepted SQLite storage/change-history stack uses database schema version `4`; JSON `SavedData.SchemaVersion` remains separate from the SQLite `PRAGMA user_version`.
- After the user requested the next stage, the current transition step is to define the next roadmap task before coding further; no next implementation phase is explicitly prioritized yet
- Local `phase7e-annual-norm-import` adds support for importing maintenance norms directly from annual workbooks with the same structure as `456.xlsx`; the importer detects annual files by workbook contents, still accepts monthly `123.xlsx`, and annual rows also apply `YearScheduleEntries` from the 12 plan columns
- On 2026-05-07, targeted norm-import tests passed (`10/10`), app/core/tests format verification passed, and `phase7e-annual-norm-import` passed verification build and `dotnet test` (`334/334`); pre-acceptance harness state was `WAITING_REVIEW`
- On 2026-05-07, the user confirmed manual review passed for `phase7e-annual-norm-import`; the accepted follow-up is the latest completed slice before selecting the next roadmap task
- The user clarified that hidden rows in annual plan `456.xlsx` represent retired equipment and must not be included in norm import totals; rows `29`, `30`, and `31` are hidden in that workbook
- Local `phase7g-annual-norm-hidden-rows` skips hidden rows before parsing annual workbook system headers or equipment rows, so hidden retired equipment cannot create or update maintenance profiles
- On 2026-05-07, targeted norm-import tests passed (`11/11`), app/core/tests format verification passed, and `phase7g-annual-norm-hidden-rows` passed verification build and `dotnet test` (`335/335`); pre-acceptance harness state was `WAITING_REVIEW`

## 2026-05-05

- The user approved starting the next roadmap work from `Phase 11. Object templates and equipment catalog`, followed by `Phase 12. Backup, snapshots, and change history`
- `Phase 8` through `Phase 10` were discussed as possible directions but are not active implementation phases right now
- `Phase 11A` starts with the equipment catalog model and JSON normalization before UI and object-template workflows
- `Phase 11A` keeps the catalog as top-level JSON data, normalizes it on load/save, deduplicates stable catalog ids, and leaves catalog UI for `Phase 11B`
- On 2026-05-05, `phase11a-equipment-catalog-model` passed verification build and `dotnet test` (`274/274`) using isolated output paths
- Production-calendar manual editing should use Russian date display/input format `дд.мм.гггг`; JSON import accepts both `дд.мм.гггг` and legacy ISO dates while saved app JSON remains backward-compatible
- On 2026-05-05, `production-calendar-russian-date-format` passed verification build and `dotnet test` (`275/275`) using isolated output paths
- The user confirmed manual review of `Phase 11A` and accepted the practical order: commit/push the accepted local changes, then implement PDF import for production calendars before returning to `Phase 11B`
- `Phase 7F.1` should import production calendars from PDF with preview; prefer text-layer parsing first, add OCR only if real source PDFs require it, and consider support for additional working days as well as additional non-working days
- Equipment catalog/template work must not change legacy Excel `v3`; catalog/template exchange should use dedicated JSON when implemented
- Template application must eventually use preview and must not silently overwrite user data

## 2026-05-04

- The accepted `Phase 7E` follow-up direction is to add Excel source exchange before considering a dedicated mass-editing UI
- `Phase 7E.2` yearly source exchange is a separate `.xlsx` workflow, not the final generated maintenance workbook and not the legacy Excel v3 database exchange
- `Phase 7E.2` import matches rows by stable `OwnerNodeId` and updates only `KbMaintenanceScheduleProfile.YearScheduleEntries`
- `Phase 7E.2` import intentionally does not change inclusion flags, `ТО1` / `ТО2` / `ТО3` labor-hour norms, or production-calendar settings
- The `Phase 7E` in-app mass-editing grid follows the same narrow source-editing contract: it edits only `YearScheduleEntries` for configured current-workshop profiles and does not create profiles, import norms, toggle inclusion, or configure calendars
- Maintenance workbook rewriting must use an expandable memory stream because real monthly exports can make the XLSX package larger than the embedded template package
- The major-work split follow-up is limited to planner assignment chunking: one `ТО2` / `ТО3` occurrence above 8 hours is split into up-to-8-hour assignments and distributed across working days when possible
- The major-work split follow-up does not introduce a hard daily total cap, does not split `ТО1`, and does not change production-calendar configuration
- Norm import matching may use conservative name/inventory variants from the approved `123.xlsx` structure, including leading-zero inventory equivalence and parenthetical equipment-name variants, while ambiguity still leaves rows unresolved
- Norm import mismatch reporting should include source sheet and row so users can correct either the workbook or the KB tree without guessing
- `Phase 7F` production-calendar configuration was explicitly prioritized by the user and is completed on `to`
- Production-calendar years are persisted in `KbConfig.ProductionCalendarYears`; built-in `2025`/`2026` defaults are preserved and future years are added through UI or PDF/JSON import
- Production-calendar import is separate from the legacy Excel `v3` database exchange and from the yearly ТО source workbook
- Missing production-calendar years should guide the user to `ТО -> Производственный календарь...`, PDF import, or service JSON import instead of requiring a code change
- `Phase 7G` is now used for the narrow annual norm import hidden-row fix; broader new work after that still must be explicitly defined and accepted before implementation
- Documentation distillation must update the full handoff harness and public-facing project docs when branch/phase state changes, not only `docs/codex-handoff.md`

## 2026-04-30

- The full `Phase 7D follow-up` yearly orchestration is implemented on `to`
- The monthly generation mechanism remains the canonical planning/export engine
- The whole-year generation command is implemented on top of the existing monthly mechanism rather than replacing it
- The whole-year command applies one selected monthly workshop budget to every generated month and defaults it to the maximum calculated monthly demand in the selected year
- Future-month recalculation is implemented by opening an existing yearly workbook and regenerating only the selected start month through December
- Months before the selected start month are frozen during ordinary replanning and must be preserved in the existing workbook
- Before `Phase 7F`, production-calendar years were code-configured in `KnowledgeBaseRussianProductionCalendarService`; the current local slice moves that configuration into JSON/UI/import
- The first `Phase 7E` yearly source is stored per maintenance profile as `YearScheduleEntries`, a 12-month `ТО1` / `ТО2` / `ТО3` template
- Empty `YearScheduleEntries` means the profile continues to use deterministic rule-based month placement
- Manual annual placement is separate from production-calendar setup; it does not configure holidays or transfer days
- Maintenance assignments owned by visible `Lvl2` nodes are valid export inputs; the monthly sheet model uses the same `Lvl2` node as both workbook group and detail row
- The agreed canonical user workflow is:
  - at the start of the year, generate the whole year in one pass
  - when equipment changes during the year, recalculate only from the current month through December
  - treat past months as frozen during ordinary replanning
- `Сформировать график ТО за месяц...`, `Сформировать годовой график ТО...`, `Пересчитать график ТО до конца года...`, and `Импорт норм ТО...` are workshop-level commands and belong in the top-level `Файл` menu

## 2026-04-29

- `to` is the active integration branch for the current roadmap stream
- `.github/workflows/windows-build.yml` should target branch `to`
- `Phase 7A`, `Phase 7B`, `Phase 7C`, and `Phase 7D` are considered implemented on `to`
- `Lvl2` inventory number visibility must follow visible hierarchy level, not `NodeType.System` alone
- Card and tab behavior for `Lvl1/Lvl2/Lvl3` should resolve from visible structure when legacy saved `NodeType` values are mixed
- Maintenance settings are stored as top-level `MaintenanceScheduleProfiles` keyed by `OwnerNodeId`
- Saved-data normalization must keep at most one maintenance profile per `OwnerNodeId`
- Only engineering nodes get the `График ТО` workspace and maintenance-profile editing
- Maintenance periodicity for the current implementation is fixed as:
  - `ТО1` = monthly
  - `ТО2` = quarterly
  - `ТО3` = annual
- Maintenance inclusion rules are fixed as:
  - `ТО2` includes `ТО1`
  - `ТО3` includes `ТО1` and `ТО2`
  - a full annual profile therefore resolves to `8 x ТО1`, `3 x ТО2`, `1 x ТО3`
- Stored `ТО1` / `ТО2` / `ТО3` norms are non-negative integer labor hours per occurrence, not per-day or per-month caps
- The hard planner constraint is the selected monthly workshop budget; there is no hard daily `<= 8` cap in the current planner
- The planner may place multiple `ТО2` / `ТО3` items on the same day when needed; avoiding that is only a preference, not a blocking rule
- The first maintenance-export release must remain template-driven and rewrite only the selected month inside the yearly workbook
- The export dialog must show resolved monthly demand before the user confirms the available monthly workshop budget
- Maintenance norms can be imported from the approved sample workbook and should match by inventory number first, then by normalized names
- The import workflow must tolerate the source workbook being open in Excel
- Future implementation work should run in micro-steps: `one step -> verify-step -> stop -> review -> commit/push`

## 2026-04-28

- `Phase 5` is considered implemented on the active integration branch
- Search is a typed domain workflow with fixed scopes: `All`, `Tree`, `Card`, `Composition`, and `Docs/Software`
- Search navigation must continue to resolve to the owning tree node and may switch the workspace to the preferred tab for the matched domain
- User-facing program UI should use Russian only going forward
- `Documentation and Software` remains intentionally separate from `Composition`; it uses dedicated link catalogs for schemes, instructions, and software folders
- The user-facing software workflow records `AddedAt`; legacy software timestamps/notes remain compatibility-only persistence fields
- `Phase 6` is considered implemented on the active integration branch
- `Phase 6` stores file-based network context in top-level `NetworkFileReferences` keyed by `OwnerNodeId`
- `Phase 6` keeps embedded preview limited to image formats already supported by the in-form workflow; non-image files stay metadata-only with `Open original`
- `Phase 6` uses separate `Файлы` and `Предпросмотр` tabs instead of a permanently split list/preview layout
- Loading a node must open `Файлы` by default; automatic switching to `Предпросмотр` is not part of the accepted UX
- The old `Phase 7` Excel/exchange-modernization direction is superseded by maintenance-schedule generation
