# Current State

Last updated: `2026-05-13`

## Next session checkpoint

Changed in the current handoff stack:

- Removed the remaining obsolete tree UI entry `Шаблоны -> Добавить из шаблона состава...`.
- Removed the WinForms call chain for that entry: `ctxAddFromTemplate`, `AddChildNodeFromTemplate`, and the UI workflow that opened `KnowledgeBaseCompositionTemplateDialog`.
- Updated current handoff/plans/decision and menu-audit docs to record that hardcoded composition-template add workflows are hidden from UI.
- Fixed annual maintenance norm import for HAVER rows where the workbook writes the system as `АСУ линии фасовки HAVER` and appends `Линия фасовки HAVER` / `FFS600` to equipment names while the KB tree stores `АСУ линии фасовки HAVER FFS600`.
- Annual norm import now re-enables a resolved existing profile when that object is present in the annual workbook, so previously disabled but valid annual rows participate in monthly planning again.

Verified:

- `dotnet format` passed for app, core, and tests.
- `scripts\verify-step.ps1 -StepName hide-composition-template-add-menu` passed.
- `scripts\verify-step.ps1 -StepName norm-import-haver-annual` passed after the annual norm import fix.
- Diagnostic import against a copy of `C:\Users\Olga\Desktop\asutpKB\proj\database\knowledge-base.akb`: `C:\Users\Olga\Desktop\asutpKB\Годовой_график _ТО_АСУТП_КЦ_2026г.xlsx` imports with `unresolved=0`; May 2026 demand becomes `297` hours.
- Verification artifact / manual exe: `C:\Users\Olga\AKB5\artifacts\verify\hide-composition-template-add-menu\build\Release\net8.0-windows\asutpKB.exe`.
- Latest verification artifact / manual exe: `C:\Users\Olga\AKB5\artifacts\verify\norm-import-haver-annual\build\Release\net8.0-windows\asutpKB.exe`.
- Search confirmed no remaining `Forms`/`UiServices` caller for `AddChildNodeFromTemplate` or `ctxAddFromTemplate`; remaining `AddNodeFromTemplate` references are core service/test coverage only.

Remaining for the next session:

- Manual review in `C:\Users\Olga\AKB5\artifacts\verify\hide-composition-template-add-menu\build\Release\net8.0-windows\asutpKB.exe`: confirm tree context menu `Шаблоны` no longer contains `Добавить из шаблона состава...`.
- Manual review in `C:\Users\Olga\AKB5\artifacts\verify\norm-import-haver-annual\build\Release\net8.0-windows\asutpKB.exe`: import `C:\Users\Olga\Desktop\asutpKB\Годовой_график _ТО_АСУТП_КЦ_2026г.xlsx`; expected `Не сопоставлено: 0` and May 2026 demand `297` hours.
- Optional later work only if requested: build a managed template editor/delete workflow; do not reintroduce direct template application without a new explicit requirement.

## Repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active integration branch: `to`
- Latest pushed implementation commit: `9668df5 Retire direct template application`
- Latest maintenance follow-up commit: `7a4895d Fix annual maintenance norm import totals`
- Latest accepted implementation: direct template application retirement plus the local Lvl2 node-type data fix
- Current roadmap implementation item: no active coding item selected after equipment catalog workflow commit
- Implemented on this branch:
  - `Phase 0`
  - `Phase 1`
  - `Phase 2`
  - `Phase 3`
  - `Phase 3B`
  - `Phase 4`
  - `Phase 5`
  - `Phase 6`
  - `Phase 7A foundation`
  - `Phase 7B Russian production calendar`
  - `Phase 7C monthly planning engine`
  - `Phase 7D yearly workbook export workflow`
  - `Phase 7D follow-up` full yearly orchestration
  - `Phase 7E yearly schedule source` first implementation slice
  - `Phase 7E Lvl2 export fix`
  - `Phase 7E.2 yearly schedule source exchange`
  - `Phase 7E in-app mass-editing grid`
  - major `ТО2` / `ТО3` split across working days
  - maintenance-norm import coverage and mismatch reporting
  - `phase7e-annual-norm-import`
  - `phase7g-annual-norm-hidden-rows`
  - `Phase 7F production-calendar configuration`
  - `Phase 7F.1 production-calendar PDF import`
  - `Phase 11A equipment catalog model`
  - `Phase 11B equipment catalog UI`
  - `Phase 11C object template model`
  - `Phase 11D create from template`
  - `Phase 11E save existing object as template`
  - `Phase 11F apply template with preview`
  - `Phase 11G template import/export`
  - `Phase 12S1 storage abstraction`
  - `Phase 12S2 SQLite schema and repository`
  - `Phase 12S3 first-launch JSON migration`
  - `Phase 12S4 database file UX`
  - `Phase 12S5 SQLite snapshots`
  - `Phase 12S6 snapshot restore`
  - `Phase 12S7 snapshot comparison`
  - `Phase 12S8 change history`
  - menu rework first iteration steps 1-6
- Current active gate: choose the next priority; likely candidates are optional template management or portable-first storage review
- `Phase 11B. Equipment catalog UI` was committed and pushed on `to` as `f80873f Add equipment catalog UI`
- `Phase 11C` / `Phase 11D` were committed and pushed on `to` as `3caca67 Add object template creation workflow`
- `Phase 11E` was committed and pushed on `to` as `3c87b6e Add save object as template workflow`
- `Phase 11F` was committed and pushed on `to` as `ca43298 Add apply object template preview workflow`
- `Phase 11G` was accepted after manual review and committed/pushed on `to` as `268b550`
- `Phase 12S8` was accepted after manual review and committed/pushed on `to` as `27a2aba`
- `phase7e-annual-norm-import` passed manual review on 2026-05-07
- `phase7g-annual-norm-hidden-rows` skips hidden annual-plan rows during norm import because hidden rows represent retired equipment in `456.xlsx`; committed/pushed on `to` as `7a4895d`
- The annual norm import HAVER follow-up is verified locally: year source matching handles `Линия/линии` context tails and final Latin model codes such as `FFS600`, and resolved annual rows re-enable previously disabled existing profiles.
- The chief developer approved the SQLite single-file storage plan choices `1A, 2B, 3A, 4A`; local JSON snapshot prototype work is paused before commit while implementation moves through the SQLite storage plan
- Menu rework first iteration steps 1-6 are accepted and committed/pushed on `to` as `8dfffbd`.
- Menu rework adds top-level `Справочники` and `Сервис`, keeps `ТО` immediately after `Файл`, combines snapshots/history into one entry, groups tree templates under `Шаблоны`, expands drag/drop move confirmation, and prompts for protective snapshots before dangerous operations: JSON/Excel full database replacement, maintenance norm import, workshop deletion, and mass template application.
- Equipment catalog and composition catalog-picker follow-ups were committed/pushed on `to` as `3eadf7f Refine equipment catalog workflows`.
- Portable-first storage follow-up is locally implemented and verified: first launch writes/reads `akb5.settings.json` next to `asutpKB.exe`, defaults to `database\knowledge-base.akb` next to the program, lets the user choose another database folder, remembers later `Открыть базу...` / `Сохранить как...` paths, offers to copy the old AppData `.akb`, and creates external timestamped backups in `backups\yyyy-MM-dd\` before overwriting/restoring an existing `.akb`.
- `miniSAP` catalog follow-up is implemented, verified, committed, and pushed: `Справочники -> Каталог оборудования...` now shows only `Наименование`, `Производитель`, `Заказной №`, and `Примечание`, the add/edit dialog uses the same four visible fields, local catalog search uses only visible catalog fields, and `C:\Users\Olga\AppData\Local\AKB5\knowledge-base.akb` contains 131 unique Siemens items imported from `C:\Users\Olga\Downloads\miniSAP.xlsx`.
- Before the `miniSAP` catalog data edit, an external backup was created at `C:\Users\Olga\AppData\Local\AKB5\backups\2026-05-12\knowledge-base-20260512-123022.akb`.
- `composition-catalog-picker` follow-up is implemented, verified, committed, and pushed: `Состав -> Добавить слот...` and `Состав -> Добавить оборудование...` now open a catalog picker through `Выбрать из каталога...`; the picker searches only visible catalog fields and fills the composition entry from the selected catalog item.
- `equipment-catalog-layout-sort` follow-up is implemented, verified, committed, and pushed: the equipment catalog opens maximized on first launch, then remembers user window placement and visible column widths through `window-layout-state.json`; visible catalog columns (`Наименование`, `Производитель`, `Заказной №`, `Примечание`) are clickable and toggle ascending/descending sort.
- `catalog-selection-layout` follow-up is implemented, verified, committed, and pushed: the `Выбрать из каталога...` dialog now uses the same first-launch maximized and saved placement/visible-column-width behavior as the equipment catalog, with separate persisted state so it does not overwrite the main catalog window layout.
- The obsolete `Состав -> Применить шаблон...` button is removed from the composition screen. The tree menu commands `Применить шаблон к объекту...` and `Шаблоны -> Добавить из шаблона состава...` are also removed from the current UI; object-template creation/saving and catalog/template exchange remain available.

## Integrated feature state

- `Phase 3` remains active on `to`:
  - typed composition entries live in `SavedData.CompositionEntries`
  - the `Composition` screen separates slots from auxiliary equipment
  - ordering is driven by `SlotNumber` + `PositionOrder`, not by left-tree child order
- `Phase 4` remains active on `to`:
  - typed documentation/software records live in top-level `DocumentLinks` and `SoftwareRecords`
  - the workflow stays intentionally separate from `Composition`
- `Phase 5` remains active on `to`:
  - indexed search covers `Tree`, `Card`, `Composition`, and `Docs/Software`
  - scopes stay fixed to `All`, `Tree`, `Card`, `Composition`, and `Docs/Software`
- `Phase 6` remains active on `to`:
  - file-based `Network` references live in top-level `NetworkFileReferences`
  - the screen keeps separate `Файлы` and `Предпросмотр` tabs
  - embedded preview remains image-only
- `Phase 7` current state on `to`:
  - typed maintenance settings live in top-level `SavedData.MaintenanceScheduleProfiles`
  - one maintenance profile is stored per `OwnerNodeId`
  - `Lvl2` inventory number visibility follows visible hierarchy level rather than raw `NodeType.System`
  - card-field rules follow visible levels:
    - `Lvl1`: hide `Местоположение` and `Фото`
    - `Lvl2`: show `Инвентарный номер`, hide `Местоположение`, `Фото`, `IP-адрес`, and `Ссылка на схему`
    - `Lvl3+`: hide `Фото`, `IP-адрес`, and `Ссылка на схему`
  - engineering tab visibility for `Lvl3+` resolves by visible engineering support, not only by persisted `NodeType`
  - engineering nodes expose the `График ТО` workflow
  - `KnowledgeBaseRussianProductionCalendarService` provides reusable Russian `5/2` workday calculation and consumes persisted `Config.ProductionCalendarYears`
  - `KnowledgeBaseMaintenanceMonthWorkResolverService` resolves monthly work demand from stored norms and deterministic cycle offsets
  - `KnowledgeBaseMaintenanceMonthlyPlannerService` plans against a monthly hour budget, distributes work across working days, splits one large `ТО2` / `ТО3` occurrence into assignments of up to 8 hours, and does not enforce a hard daily `<= 8` cap
  - the export workflow is template-driven and writes one selected month into a yearly accumulating workbook while preserving the rest of the workbook
  - `ТО` contains workshop-level `Импорт норм ТО...`, `План ТО по месяцам...`, `Экспорт плана ТО по месяцам...`, `Импорт плана ТО по месяцам...`, `Производственный календарь...`, `Импорт производственного календаря PDF...`, `Сформировать график ТО за месяц...`, `Сформировать годовой график ТО...`, and `Пересчитать график ТО до конца года...`; import/export commands are no longer shown inside each per-node `График ТО` tab
  - `Phase 7E.2` adds `ТО -> Экспорт плана ТО по месяцам...` and `ТО -> Импорт плана ТО по месяцам...`
  - `Phase 7E` mass-editing grid adds `ТО -> План ТО по месяцам...` for current-workshop profile rows
  - the `Сформировать график ТО` dialog shows resolved monthly demand before the user confirms the available workshop budget
  - the yearly generation command shows 12-month demand and generates all months by orchestrating the existing monthly engine
  - the future-month recalculation command opens an existing yearly workbook, preserves earlier month sheets, and rewrites only the selected start month through December
  - `Phase 7E` adds optional per-profile `YearScheduleEntries` stored in JSON; when present, they drive manual 12-month `ТО1` / `ТО2` / `ТО3` placement
  - profiles without `YearScheduleEntries` keep the previous deterministic offset behavior
  - maintenance workbook export allows profiles assigned directly to visible `Lvl2`; that node is used as both the workbook group and detail row
  - the per-node `График ТО` profile dialog can enable manual annual placement and edit the 12-month source
  - `Phase 7E.2` adds workshop-level `.xlsx` export/import of the yearly schedule source through `YearScheduleSource` rows keyed by `OwnerNodeId`
  - `Phase 7E.2` import updates only `YearScheduleEntries`; it does not change `ТО1` / `ТО2` / `ТО3` hour norms and does not create missing maintenance profiles
  - `Phase 7E` mass-editing grid updates only `YearScheduleEntries`; it does not change hour norms, inclusion flags, production calendars, or create missing profiles
  - `Phase 7F` adds `Config.ProductionCalendarYears`, a Russian `ТО -> Производственный календарь...` editor, JSON import validation, and guided missing-year errors; production-calendar JSON import is hidden from the menu after the menu rework
  - `Phase 7F.1` adds `ТО -> Импорт производственного календаря PDF...`, text-layer PDF parsing through `PdfPig`, a preview dialog before applying imported dates, and `AdditionalWorkingDays` support for transferred working Saturdays/Sundays
  - `phase7e-major-work-split-days` splits one `ТО2` / `ТО3` occurrence into up-to-8-hour assignments across working days when possible
  - `phase7e-norm-import-coverage` improves norm import matching for leading-zero inventory numbers, `ё/е`, and parenthetical equipment names; unresolved rows include source sheet/row context
  - maintenance norms can be imported from monthly `C:\Users\Olga\Downloads\123.xlsx`; `phase7e-annual-norm-import` also supports annual workbooks with the same structure as `C:\Users\Olga\Downloads\456.xlsx`
  - import matching uses inventory number first, then normalized equipment/system names, and can read the workbook even when it is open in Excel
  - `phase7g-annual-norm-hidden-rows` skips hidden rows in annual workbooks before parsing system headers or equipment rows, so hidden retired equipment does not affect imported monthly demand
- `Phase 11` current state on `to`:
  - `Phase 11A` adds the top-level JSON equipment catalog model and normalization
  - `Phase 11B` adds `Справочники -> Каталог оборудования...` for listing, adding, editing, deleting, and searching equipment catalog items
  - catalog editing remains separate from tree editing and object-template creation
  - `Phase 11C` adds top-level JSON/session object templates, template nodes keyed by `TemplateNodeId`, normalization, and an instantiation service that generates fresh real `NodeId` values and remaps linked defaults
  - `Phase 11D` adds a tree context-menu command `Создать объект из шаблона...` and a Russian selection dialog for persisted object templates
  - creating from a template inserts the whole template subtree, applies normal tree reindexing/depth checks, and appends remapped composition, document/software, network-file, and maintenance-profile defaults
  - `Phase 11E` adds a tree context-menu command for saving the selected object subtree as a persisted template, removes real node ids, remaps typed owner references by generated template-node ids, and skips typed records outside the selected subtree
  - `Phase 11F` added object-template application internals and preview, but the current UI no longer exposes the obsolete tree context-menu commands `Применить шаблон к объекту...` or `Добавить из шаблона состава...`
  - `Phase 11G` adds catalog/template JSON exchange; after menu rework, the commands are exposed as `Сервис -> Экспорт справочников и шаблонов...` and `Сервис -> Импорт справочников и шаблонов...`; export writes a dedicated UTF-8 JSON exchange file and import merges only catalog/templates without touching legacy Excel `v3`
- `Phase 12` accepted storage state:
  - `Phase 12A` adds automatic timestamped JSON snapshots before save; snapshot failure blocks overwrite so the current JSON file stays intact
  - `Phase 12B` adds manual snapshot creation with a required user note, writes the current JSON state to `.akb-snapshots`, and writes `.meta.json` sidecar metadata
  - `Phase 12C` adds a read-only snapshot browser with date, type, snapshot file, source file, size, and note
  - `Phase 12C` reads note/source/timestamp/size from sidecar metadata for manual snapshots and falls back to filename/file info for automatic snapshots without metadata
  - `Phase 12A` / `12B` / `12C` are now treated as verified local prototype work, paused before commit while storage moves to SQLite
  - `Phase 12S0` is approved with choices `1A, 2B, 3A, 4A`; details are in `docs/sqlite-storage-plan.md`
  - `Phase 12S1. Storage abstraction` is accepted and committed/pushed on `to` as part of `27a2aba`
  - `Phase 12S1` adds `IKnowledgeBaseStorageService`, `KnowledgeBaseStorageLoadResult`, and `KnowledgeBaseStorageServiceFactory`; `JsonStorageService` implements the interface and `KnowledgeBaseFileWorkflowService` now depends on the interface
  - `Forms` no longer creates `JsonStorageService` directly; current behavior still uses legacy JSON storage through the factory
  - `Phase 12S2. SQLite schema and repository` is accepted and committed/pushed on `to` as part of `27a2aba`
  - `Phase 12S2` adds `Microsoft.Data.Sqlite` `8.0.13`, `KnowledgeBaseSqliteConnectionFactory`, and `SqliteKnowledgeBaseStorageService`
  - Current SQLite database schema version is `4`; normalized tables cover metadata, config, calendars, workshops, nodes, typed records, maintenance, catalog, object templates, template nodes, snapshots, and change history
  - SQLite save/load round-trips normalized `SavedData`
  - `Phase 12S3. First-launch JSON migration` is accepted and committed/pushed on `to` as part of `27a2aba`; it offers confirmed migration from legacy `Мои документы\ASUTP_KnowledgeBase.json` to `%LocalAppData%\AKB5\knowledge-base.akb`, leaves legacy JSON unchanged, and writes a post-migration JSON safety export
  - `Phase 12S4. Database file UX` is accepted and committed/pushed on `to` as part of `27a2aba`; current builds default to `.akb`, route `.json` paths to legacy JSON storage, update database dialogs to `.akb`, and add full database JSON import/export commands
  - `Phase 12S5. SQLite backups and snapshots` is accepted and committed/pushed on `to` as part of `27a2aba`; SQLite snapshots live inside the `.akb` database with metadata and normalized `SavedData` payloads
  - `Phase 12S6. Restore selected snapshot` is accepted and committed/pushed on `to` as part of `27a2aba`; restore requires confirmation, creates a protective `before-restore` snapshot, reloads restored data, and leaves failed restores intact
  - `Phase 12S7. Snapshot comparison` is accepted and committed/pushed on `to` as part of `27a2aba`; two snapshots can be compared at summary level across high-value data areas
  - `Phase 12S8. Change history` is accepted after manual review and committed/pushed on `to`; `.akb` databases record save, migration, manual snapshot, restore, and catalog/template import actions and expose history through `Файл -> Снимки и история базы...`
  - target storage direction: SQLite single-file database with portable-first default storage beside the program package, visible `.akb` extension, JSON full database import/export, confirmation before first-launch migration from legacy `Мои документы\ASUTP_KnowledgeBase.json`, automatic post-migration JSON safety export, and no simultaneous multi-user editing in the first SQLite version
- User-facing application UI on `to` remains Russian-only

## Validated status

Actually run on the worktree and live AppData `.akb` for local `minisap-equipment-catalog` on `2026-05-12`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter KnowledgeBaseEquipmentCatalogServiceTests
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName minisap-equipment-catalog
git diff --check
```

- `miniSAP.xlsx` parsing found `353` Siemens rows and `131` unique Siemens catalog positions; all `131` were inserted into `C:\Users\Olga\AppData\Local\AKB5\knowledge-base.akb`.
- Inserted catalog fields: `EquipmentKind` = miniSAP name, `Manufacturer` = `Siemens`, `Model` = order number, `Description` = empty note; the shifted row `BPZ:QBM81-10` was normalized to name `Реле перепада давления воздуха Диапазон измерений: 100...1000 Пa`.
- SQLite `PRAGMA quick_check`: `ok`; catalog count is `132`, Siemens count is `131`, imported-id count is `131`, and one `catalog-import` change-log row was added.
- `dotnet format --verify-no-changes`: passed for app, core, and tests.
- Targeted catalog tests passed, `6/6`, with existing analyzer warnings.
- Verification `minisap-equipment-catalog`: build passed; `dotnet test` passed, `344/344`, with existing analyzer warnings.
- `git diff --check`: passed with standard CRLF warnings only.
- Verification artifacts: `artifacts\verify\minisap-equipment-catalog`.
- Final state: implementation was later committed/pushed as part of `3eadf7f`; live AppData `.akb` was modified with backup `C:\Users\Olga\AppData\Local\AKB5\backups\2026-05-12\knowledge-base-20260512-123022.akb`.

Actually run on the worktree for local `composition-catalog-picker` on `2026-05-12`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName composition-catalog-picker
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests.
- Verification `composition-catalog-picker`: build passed; `dotnet test` passed, `344/344`, with existing analyzer warnings.
- Verification artifacts: `artifacts\verify\composition-catalog-picker`.
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\composition-catalog-picker\build\Release\net8.0-windows\asutpKB.exe`.
- Final state: implementation was later committed/pushed as part of `3eadf7f`.

Actually run on the worktree for local `equipment-catalog-layout-sort` on `2026-05-12`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter KnowledgeBaseWindowLayoutStateServiceTests
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName equipment-catalog-layout-sort
git diff --check
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests.
- Targeted window layout state tests passed.
- Verification `equipment-catalog-layout-sort`: build passed; `dotnet test` passed, `346/346`, with existing analyzer warnings.
- `git diff --check`: passed with standard CRLF warnings only.
- Verification artifacts: `artifacts\verify\equipment-catalog-layout-sort`.
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\equipment-catalog-layout-sort\build\Release\net8.0-windows\asutpKB.exe`.
- Final state: implementation was later committed/pushed as part of `3eadf7f`.

Actually run on the worktree for local `catalog-selection-layout` on `2026-05-12`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter KnowledgeBaseWindowLayoutStateServiceTests
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName catalog-selection-layout
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests.
- Targeted window layout state tests passed, `14/14`.
- Verification `catalog-selection-layout`: build passed; `dotnet test` passed, `348/348`, with existing analyzer warnings.
- Verification artifacts: `artifacts\verify\catalog-selection-layout`.
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\catalog-selection-layout\build\Release\net8.0-windows\asutpKB.exe`.
- Final state: implementation was later committed/pushed as part of `3eadf7f`.

Actually run on the worktree for local `portable-first-storage` on `2026-05-12`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
git diff --check
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests
- `dotnet build asutpKB.csproj --configuration Release --no-restore`: passed with existing analyzer warnings
- `dotnet test`: passed, `344/344`, with existing analyzer warnings
- `git diff --check`: passed with standard CRLF warnings only
- Manual exe path for review: `C:\Users\Olga\AKB5\bin\Release\net8.0-windows\asutpKB.exe`
- Final state: local implementation verified; not committed/pushed; waiting for manual review/acceptance

Actually run on the worktree for local `menu-rework-stage6` on `2026-05-10`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName menu-rework-stage6
git diff --check
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Verification `dotnet build`: passed for `menu-rework-stage6`
- `dotnet test`: passed, `340/340`
- `git diff --check`: passed with standard CRLF warnings only
- Verification artifacts: `artifacts\verify\menu-rework-stage6`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\menu-rework-stage6\build\Release\net8.0-windows\asutpKB.exe`
- Final state: accepted and committed/pushed on `to` as `8dfffbd Rework menu structure and safety prompts`

Actually run on the worktree for local `Phase 12S8` on `2026-05-07`:

```powershell
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase12s3-first-launch-json-migration
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase12s4-database-file-ux-json-compatibility
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase12s5-sqlite-snapshots
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase12s6-snapshot-restore
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase12s7-snapshot-comparison
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase12s8-change-history
```

- `phase12s3-first-launch-json-migration`: verification build passed; `dotnet test` passed, `322/322`
- `phase12s4-database-file-ux-json-compatibility`: verification build passed; `dotnet test` passed, `325/325`
- `phase12s5-sqlite-snapshots`: verification build passed; `dotnet test` passed, `328/328`
- `phase12s6-snapshot-restore`: verification build passed; `dotnet test` passed, `330/330`
- `phase12s7-snapshot-comparison`: verification build passed; `dotnet test` passed, `332/332`
- `phase12s8-change-history`: verification build passed; `dotnet test` passed, `333/333`
- Verification artifacts: `artifacts\verify\phase12s8-change-history`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase12s8-change-history\build\Release\net8.0-windows\asutpKB.exe`
- Final verification before acceptance: harness status was `STATE: WAITING_REVIEW`; the accepted stack was later committed/pushed as `27a2aba`

Actually run on the worktree for local `phase7g-annual-norm-hidden-rows` on `2026-05-07`:

```powershell
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "KnowledgeBaseMaintenanceScheduleNormImportServiceTests"
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7g-annual-norm-hidden-rows
```

- Targeted norm-import tests: passed, `11/11`
- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Verification `dotnet build`: passed for `phase7g-annual-norm-hidden-rows`
- `dotnet test`: passed, `335/335`
- Verification artifacts: `artifacts\verify\phase7g-annual-norm-hidden-rows`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase7g-annual-norm-hidden-rows\build\Release\net8.0-windows\asutpKB.exe`
- Final state: committed/pushed on `to` as `7a4895d Fix annual maintenance norm import totals`

Actually run on the worktree for local `phase7e-annual-norm-import` on `2026-05-07`:

```powershell
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "KnowledgeBaseMaintenanceScheduleNormImportServiceTests"
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7e-annual-norm-import
```

- Targeted norm-import tests: passed, `10/10`
- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Verification `dotnet build`: passed for `phase7e-annual-norm-import`
- `dotnet test`: passed, `334/334`
- Verification artifacts: `artifacts\verify\phase7e-annual-norm-import`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase7e-annual-norm-import\build\Release\net8.0-windows\asutpKB.exe`
- Harness status was `STATE: WAITING_REVIEW`; manual review passed on 2026-05-07

Actually run on the worktree for local `Phase 12S2` on `2026-05-07`:

```powershell
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "SqliteKnowledgeBaseStorageServiceTests"
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "SqliteKnowledgeBaseStorageServiceTests|KnowledgeBaseFileWorkflowServiceTests|JsonStorageServiceTests|KnowledgeBaseSnapshotServiceTests"
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase12s2-sqlite-schema-repository
```

- Targeted SQLite storage tests: passed, `3/3`
- Targeted storage/file-workflow/snapshot tests: passed, `30/30`
- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Verification `dotnet build`: passed for `phase12s2-sqlite-schema-repository`
- `dotnet test`: passed, `317/317`
- Verification artifacts: `artifacts\verify\phase12s2-sqlite-schema-repository`
- Historical harness status for that slice: `STATE: WAITING_REVIEW`; later local work advanced through `Phase 12S8`

Actually run on the worktree for local `Phase 12S1` on `2026-05-07`:

```powershell
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "KnowledgeBaseFileWorkflowServiceTests|JsonStorageServiceTests|KnowledgeBaseSnapshotServiceTests"
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase12s1-storage-abstraction
```

- Targeted storage/file-workflow/snapshot tests: passed, `27/27`
- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Verification `dotnet build`: passed for `phase12s1-storage-abstraction`
- `dotnet test`: passed, `314/314`
- Verification artifacts: `artifacts\verify\phase12s1-storage-abstraction`
- Historical harness status for that slice: `STATE: WAITING_REVIEW`; later local work advanced through `Phase 12S8`

Docs-only storage plan update on `2026-05-07`:

- Added `docs/sqlite-storage-plan.md`
- Updated `README.md`, `AGENTS.md`, `Roadmap.md`, `docs/plans.md`, `docs/decision-log.md`, `docs/workbook-v3.md`, and this handoff to reflect the SQLite single-file target direction
- Recorded approved choices `1A, 2B, 3A, 4A`; local implementation has since advanced through `Phase 12S8. Change history`
- No code implementation was started for SQLite storage
- Build/test/harness were not required for this docs-only planning slice

Actually run on the worktree for local `Phase 12C` on `2026-05-07`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "JsonStorageServiceTests|KnowledgeBaseSnapshotServiceTests|KnowledgeBaseFileWorkflowServiceTests"
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase12c-snapshot-browser
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Targeted JSON storage/snapshot/file-workflow tests: passed, `25/25`
- Verification `dotnet build`: passed for `phase12c-snapshot-browser`
- `dotnet test`: passed, `312/312`
- Verification artifacts: `artifacts\verify\phase12c-snapshot-browser`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase12c-snapshot-browser\build\Release\net8.0-windows\asutpKB.exe`
- Local implementation: `Файл -> Просмотреть снимки базы...` opens a read-only snapshot browser for the current JSON database; it shows date, type, snapshot file, source file, size, and note, reads notes from `.meta.json`, and derives fallback metadata for automatic snapshots
- Status: verified local prototype; paused before commit while storage moves to SQLite

Actually run on the worktree for local `Phase 12B` on `2026-05-07`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "JsonStorageServiceTests|KnowledgeBaseSnapshotServiceTests|KnowledgeBaseFileWorkflowServiceTests"
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase12b-manual-json-snapshots
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Targeted JSON storage/snapshot/file-workflow tests: passed, `22/22`
- Verification `dotnet build`: passed for `phase12b-manual-json-snapshots`
- `dotnet test`: passed, `309/309`
- Verification artifacts: `artifacts\verify\phase12b-manual-json-snapshots`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase12b-manual-json-snapshots\build\Release\net8.0-windows\asutpKB.exe`
- Local implementation: `Файл -> Создать снимок базы...` writes the current JSON state to `.akb-snapshots`, requires a user note, and writes a `.meta.json` sidecar consumed by the `Phase 12C` browser and future restore work
- Status: verified local prototype; paused before commit while storage moves to SQLite

Actually run on the worktree for local `Phase 12A` on `2026-05-06`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "JsonStorageServiceTests|KnowledgeBaseSnapshotServiceTests"
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase12a-automatic-json-snapshots
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Targeted JSON storage/snapshot tests: passed, `12/12`
- Verification `dotnet build`: passed for `phase12a-automatic-json-snapshots`
- `dotnet test`: passed, `306/306`
- Verification artifacts: `artifacts\verify\phase12a-automatic-json-snapshots`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase12a-automatic-json-snapshots\build\Release\net8.0-windows\asutpKB.exe`
- Local implementation: `JsonStorageService.Save` creates a timestamped snapshot of the existing JSON file in `.akb-snapshots` before overwrite, keeps the existing `.bak` behavior, and aborts save without overwriting if the protective snapshot cannot be created
- Status: verified local prototype; paused before commit while storage moves to SQLite

Actually run on the worktree for local `Phase 11G` on `2026-05-06`:

```powershell
dotnet build asutpKB.csproj --no-restore
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "KnowledgeBaseCatalogTemplateExchangeServiceTests|KnowledgeBaseEquipmentCatalogServiceTests|KnowledgeBaseObjectTemplateServiceTests"
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase11g-template-import-export
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase11g-template-import-export-layout-fix
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase11g-template-import-export-apply-preview-ui-fix
```

- App build: passed with `0` errors
- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Targeted catalog/template exchange tests: passed, `14/14`
- Verification `dotnet build`: passed for `phase11g-template-import-export`
- `dotnet test`: passed, `302/302`
- Manual-review UI fix: expanded the `Состав шаблона` field in the create-object-from-template dialog so long template details are visible
- Verification `dotnet build`: passed for `phase11g-template-import-export-layout-fix`
- `dotnet test`: passed, `302/302`
- Manual-review UI fix: the apply-object-template dialog now selects the first template explicitly, rebuilds preview when shown, and shows no-change/failure text instead of an empty preview; `Применить` remains enabled only for successful plans with new data
- Verification `dotnet build`: passed for `phase11g-template-import-export-apply-preview-ui-fix`
- `dotnet test`: passed, `302/302`
- Verification artifacts: `artifacts\verify\phase11g-template-import-export-apply-preview-ui-fix`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase11g-template-import-export-apply-preview-ui-fix\build\Release\net8.0-windows\asutpKB.exe`
- Status: manual review passed; committed/pushed on `to` as `268b550`

Actually run on the worktree for local `Phase 11F` on `2026-05-06`:

```powershell
dotnet build asutpKB.csproj --no-restore
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "KnowledgeBaseObjectTemplateServiceTests|KnowledgeBaseTreeMutationWorkflowServiceTests"
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "KnowledgeBaseObjectTemplateServiceTests|KnowledgeBaseTreeMutationWorkflowServiceTests|KnowledgeBaseMaintenanceWorkbookExportServiceTests|KnowledgeBaseMaintenanceScheduleNormImportServiceTests|KnowledgeBaseSessionServiceTests|KnowledgeBaseWindowLayoutStateServiceTests"
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase11f-apply-template-preview
```

- App build: passed with `0` errors
- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Targeted apply-template/object-template/workflow tests: passed, `19/19`
- Post-review encoding regression tests: passed, `55/55`
- Manual-review encoding fix: corrected mojibake in Russian template context-menu/dialog/status strings and affected test literals
- Mojibake scan: generated UTF-8/CP1251 corruption-pattern scan over source/docs/JSON-style files returned `TOTAL=0`
- Verification `dotnet build`: passed for `phase11f-apply-template-preview`
- `dotnet test`: passed, `299/299`
- Verification artifacts: `artifacts\verify\phase11f-apply-template-preview`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase11f-apply-template-preview\build\Release\net8.0-windows\asutpKB.exe`
- Status: manual review passed; committed and pushed on `to` as `ca43298 Add apply object template preview workflow`

Actually run on the worktree for local `Phase 11E` on `2026-05-06`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "KnowledgeBaseObjectTemplateServiceTests|KnowledgeBaseTreeMutationWorkflowServiceTests"
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase11e-save-object-as-template
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Targeted save-object-as-template/object-template/workflow tests: passed, `16/16`
- Verification `dotnet build`: passed for `phase11e-save-object-as-template`
- `dotnet test`: passed, `296/296`
- Verification artifacts: `artifacts\verify\phase11e-save-object-as-template`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase11e-save-object-as-template\build\Release\net8.0-windows\asutpKB.exe`
- Status: manual review passed; committed and pushed on `to` as `3c87b6e Add save object as template workflow`

Actually run on the worktree for local `Phase 11D` on `2026-05-06`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "KnowledgeBaseTreeMutationWorkflowServiceTests|KnowledgeBaseObjectTemplateServiceTests|KnowledgeBaseDataServiceTests|KnowledgeBaseSessionServiceTests|KnowledgeBaseFileWorkflowServiceTests"
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase11d-create-from-template
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Targeted create-from-template/object-template/data/session/workflow tests: passed, `58/58`
- Verification `dotnet build`: passed for `phase11d-create-from-template`
- `dotnet test`: passed, `294/294`
- Verification artifacts: `artifacts\verify\phase11d-create-from-template`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase11d-create-from-template\build\Release\net8.0-windows\asutpKB.exe`
- Status: manual review passed; user requested commit/push before starting Phase 11E

Actually run on the worktree for local `Phase 11C` on `2026-05-06`:

```powershell
dotnet build asutpKB.csproj --no-restore
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter "KnowledgeBaseObjectTemplateServiceTests|KnowledgeBaseDataServiceTests|KnowledgeBaseSessionServiceTests|KnowledgeBaseFileWorkflowServiceTests"
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase11c-object-template-model
```

- App build: passed with `0` warnings and `0` errors
- `dotnet format --verify-no-changes`: passed
- Targeted object-template/data/session/workflow tests: passed, `46/46`
- Verification `dotnet build`: passed for `phase11c-object-template-model`
- `dotnet test`: passed, `292/292`
- Verification artifacts: `artifacts\verify\phase11c-object-template-model`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase11c-object-template-model\build\Release\net8.0-windows\asutpKB.exe`
- Status: manual review passed together with Phase 11D; user requested commit/push before starting Phase 11E

Actually run on the worktree for local `Phase 11B` on `2026-05-06`:

```powershell
dotnet build asutpKB.csproj --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --filter KnowledgeBaseEquipmentCatalogServiceTests
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase11b-equipment-catalog-ui
```

- App build: passed
- Targeted equipment-catalog service tests: passed, `6/6`
- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Verification `dotnet build`: passed for `phase11b-equipment-catalog-ui`
- `dotnet test`: passed, `287/287`
- Verification artifacts: `artifacts\verify\phase11b-equipment-catalog-ui`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase11b-equipment-catalog-ui\build\Release\net8.0-windows\asutpKB.exe`
- User requested commit/push for `Phase 11B` on `2026-05-06`
- Committed and pushed as `f80873f Add equipment catalog UI`

Accepted `Phase 7F.1` run on `2026-05-06`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --filter KnowledgeBaseProductionCalendarPdfImportServiceTests
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7f1-production-calendar-pdf-import
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Targeted PDF import tests: passed, `3/3`
- Verification `dotnet build`: passed for `phase7f1-production-calendar-pdf-import`
- `dotnet test`: passed, `281/281`
- Verification artifacts: `artifacts\verify\phase7f1-production-calendar-pdf-import`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase7f1-production-calendar-pdf-import\build\Release\net8.0-windows\asutpKB.exe`
- Real source smoke: `C:\Users\Olga\Downloads\calendar_2027.pdf` imports through the text layer as year `2027`, with additional non-working days `22.02.2027`, `03.05.2027`, `10.05.2027`, `14.06.2027`, `05.11.2027`, `31.12.2027`, and additional working day `20.02.2027`
- Manual UI review of the PDF import menu/preview/apply flow passed by user on `2026-05-06`; production-calendar PDF import works
- Committed and pushed as `09bf84d Add production calendar PDF import`

Previous accepted baseline run on `2026-05-04`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7d-menu-maintenance-commands
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7d-year-generation-command
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7d-complete
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7e-year-schedule-source
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7e-lvl2-maintenance-export-fix
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7e-year-schedule-source-exchange
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7e-workbook-expandable-stream-fix
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7e-year-source-mass-edit-grid
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7e-major-work-split-days
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7e-norm-import-coverage
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7f-production-calendar-config
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Verification `dotnet build`: passed for `phase7f-production-calendar-config`
- `dotnet test`: passed, `270/270`
- Verification artifacts: `artifacts\verify\phase7f-production-calendar-config`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase7f-production-calendar-config\build\Release\net8.0-windows\asutpKB.exe`
- Startup smoke was not rerun for the final `Phase 7D` completion slice
- Manual UI validation of the new production-calendar editor/import workflow passed by user
- Full Excel round-trip validation (`generate -> open -> edit/save -> import back`) was not run
## Active objective

- Keep the completed `Phase 7` workflow on `to` stable as the baseline
- Preserve the production-calendar follow-up behavior: manual calendar dates are shown as `дд.мм.гггг`, JSON import accepts both `дд.мм.гггг` and legacy ISO dates, saved app JSON remains backward-compatible, and additional working days can represent transferred working weekends
- Keep `Phase 11B` as the accepted equipment-catalog UI baseline
- Treat `3eadf7f Refine equipment catalog workflows` as the latest pushed catalog/composition follow-up baseline
- Preserve deterministic month placement as fallback for profiles that do not enable manual annual placement
- Current local builds use portable-first SQLite `.akb` storage while retaining JSON import/export and first-launch migration compatibility; generated workbooks remain report artifacts
- Treat `Phase 12S8. Change history` as accepted and committed/pushed; treat `phase7e-annual-norm-import` as accepted after manual review; treat `phase7g-annual-norm-hidden-rows` and menu rework first iteration as committed/pushed on `to`
- Follow `docs/codex-operational-rules.md` for every future Codex turn: compact diagnostics, context-budget discipline, 2-3 minute stall recovery, and fresh sessions after large investigations or handoff checkpoints are mandatory.
- Before coding further, confirm whether the next priority is optional template management, portable-first storage review, or another roadmap slice.

## Durable decisions already made

- `to` is the active integration branch for current work; `.github/workflows/windows-build.yml` targets `to`
- Future implementation work should follow the process rule: `one step -> scripts/verify-step.ps1 -> stop -> manual review -> commit/push`
- Use tree taxonomy:
  - `L1` = department
  - `L2` = system
  - `L3` = cabinet
- Visible hierarchy level is the accepted source for `Lvl1/Lvl2/Lvl3` card/tab behavior when legacy saved `NodeType` values are mixed
- Maintenance planning rules currently fixed for implementation:
  - `ТО1` = monthly
  - `ТО2` = quarterly
  - `ТО3` = annual
  - `ТО2` includes `ТО1`
  - `ТО3` includes `ТО1` and `ТО2`
  - a full annual profile therefore resolves to `8 x ТО1`, `3 x ТО2`, `1 x ТО3`
- Stored `ТО1` / `ТО2` / `ТО3` norms are per-occurrence labor hours for one equipment unit, not monthly budgets
- Hidden rows in annual norm workbooks are intentionally skipped; in `456.xlsx` they represent retired equipment and must not affect imported monthly demand
- The hard planner constraint is the selected monthly workshop budget, not a daily `<= 8` cap
- Production-calendar years live in `KbConfig.ProductionCalendarYears`; built-in `2025`/`2026` defaults are preserved, while future years can be added through UI, JSON import, or local PDF import
- `Phase 11` is approved as object templates and equipment catalog; `Phase 11A` contains the catalog model only and passed manual review
- `Phase 7F.1` is accepted after manual review and committed/pushed as `09bf84d`; it adds PDF import for production calendars because JSON import is inconvenient for ordinary use
- `Phase 11B` keeps catalog editing separate from tree editing and object-template creation; it only maintains catalog records
- `Phase 11C` kept object templates as data/model/service work only; `Phase 11D` adds the create-from-template UI/workflow on top of it
- `Phase 11C` templates use `TemplateNodeId` for internal references and generate fresh persisted `NodeId` values only when instantiated
- `Phase 11D` creates objects only from already persisted `SavedData.ObjectTemplates`; creating/editing/saving templates remains a later slice
- The user confirmed manual review of Phase 11D and explicitly requested committing/pushing the Phase 11C/11D stack before Phase 11E
- `Phase 11C` / `Phase 11D` were committed and pushed on `to` as `3caca67 Add object template creation workflow`
- `Phase 11E` saves an existing object subtree into a template by generating fresh template-node ids, stripping real node ids, remapping typed records inside the subtree, and leaving source tree data unchanged
- `Phase 11E` / `Phase 11F` were committed and pushed on `to` as `3c87b6e Add save object as template workflow` and `ca43298 Add apply object template preview workflow`
- `Phase 11F` applies templates to existing objects only after preview; it does not overwrite existing card values or typed records and does not delete user data
- `Phase 11G` exchanges equipment catalog records and object templates through a dedicated JSON file; import is a safe merge and does not replace current duplicates
- Local `Phase 12A` adds automatic timestamped JSON snapshots before save; snapshot failure blocks overwrite so the current JSON file stays intact
- Local `Phase 12B` adds manual snapshot creation with a required note and sidecar metadata for future snapshot browser/restore workflows
- Local `Phase 12C` adds a read-only snapshot browser that lists `.akb-snapshots` entries for the current JSON database with date, type, snapshot file, source file, size, and note
- `Phase 12S1` adds the storage abstraction around current JSON persistence and removes direct `JsonStorageService` dependency from UI/file workflow code
- `Phase 12S2` added SQLite schema/repository support behind the storage abstraction
- `Phase 12S3` added first-launch migration from legacy JSON to `%LocalAppData%\AKB5\knowledge-base.akb`, preserving the legacy JSON file and creating a safety JSON export
- `Phase 12S4` switched current UI/file workflow defaults to `.akb`, kept legacy JSON routing, and added full database JSON import/export
- `Phase 12S5` moved snapshots into the SQLite `.akb` database
- `Phase 12S6` added confirmed snapshot restore with a protective before-restore snapshot
- `Phase 12S7` added summary comparison for two selected snapshots
- `Phase 12S8` added SQLite change-history recording; after menu rework, it is reached through `Файл -> Снимки и история базы...`
- The target storage redesign is SQLite single-file storage with JSON import/export compatibility and first-launch migration from the legacy JSON database; see `docs/sqlite-storage-plan.md`
- A single `ТО2` / `ТО3` occurrence above 8 hours is split into assignments of up to 8 hours; this is assignment chunking for major work, not a hard daily total cap
- The planner may place more than one large maintenance item on the same day when needed; it only prefers to spread `ТО2` / `ТО3` apart when possible
- The first release keeps deterministic rule-based month placement for `ТО2` / `ТО3`; a future yearly schedule source may replace that without redesigning the export pipeline
- The yearly workbook export must stay template-driven and preserve existing month sheets, layout, formulas, merges, and signature blocks outside the rewritten month
- The monthly generation mechanism stays the canonical engine
- The yearly command is built on top of the monthly engine by generating months `1..12` sequentially into the same workbook
- The yearly command uses one selected monthly workshop budget for all months and defaults it to the maximum calculated monthly demand for the selected year
- `Phase 7E` stores manual annual maintenance placement in `KbMaintenanceScheduleProfile.YearScheduleEntries`
- Empty `YearScheduleEntries` means old deterministic month placement remains active for that profile
- Manual annual placement is a 12-month profile template, not a production-calendar configuration and not a per-year holiday source
- `Phase 7E.2` source exchange uses a separate `.xlsx` workbook with stable `OwnerNodeId` matching and editable `M01`..`M12` cells containing `ТО1`, `ТО2`, `ТО3`, or blank for fallback
- `Phase 7E.2` import is intentionally narrow: it changes only `YearScheduleEntries` and leaves inclusion flags and labor-hour norms untouched
- The `Phase 7E` in-app mass-editing grid is also narrow: it edits only `YearScheduleEntries` for configured profiles in the current workshop and leaves norms, inclusion flags, and calendar settings untouched
- A maintenance profile assigned directly to a visible `Lvl2` node is valid for workbook export; only nodes above visible `Lvl2` remain invalid
- Future-month recalculation is implemented by opening an existing yearly workbook and regenerating the selected start month through December into the same workbook
- Months before the selected start month are treated as frozen and must be preserved during ordinary replanning
- Agreed canonical `Phase 7D` user workflow:
  - at the start of the year, generate the whole year in one pass
  - when equipment changes during the year, recalculate only from the current month through December
  - treat past months as frozen during ordinary replanning
- `Сформировать график ТО за месяц...`, `Сформировать годовой график ТО...`, `Пересчитать график ТО до конца года...`, and `Импорт норм ТО...` are workshop-level commands and belong in the top-level `Файл` menu, not inside the per-node `График ТО` tab
- `docs/codex-handoff.md` remains the single current-state file for future sessions
- `docs/codex-operational-rules.md` records mandatory Codex operating rules for stall recovery and context-budget discipline.

## Relevant files for the next task area

- `Forms/MainForm.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.Events.cs`
- `Forms/MainForm.Maintenance.cs`
- `Forms/MainForm.ProductionCalendar.cs`
- `Forms/MainForm.EquipmentCatalog.cs`
- `Forms/KnowledgeBaseEquipmentCatalogForm.cs`
- `Forms/KnowledgeBaseEquipmentCatalogItemDialog.cs`
- `Forms/KnowledgeBaseEquipmentCatalogSelectionDialog.cs`
- `Forms/KnowledgeBaseObjectTemplateCreateDialog.cs`
- `Forms/KnowledgeBaseObjectTemplateSaveDialog.cs`
- `Forms/KnowledgeBaseProductionCalendarForm.cs`
- `Forms/KnowledgeBaseProductionCalendarPdfImportPreviewForm.cs`
- `Controls/KnowledgeBaseMaintenanceScheduleScreenControl.cs`
- `Forms/KnowledgeBaseMaintenanceWorkbookExportDialog.cs`
- `Forms/KnowledgeBaseMaintenanceYearWorkbookExportDialog.cs`
- `Forms/KnowledgeBaseMaintenanceYearWorkbookRecalculationDialog.cs`
- `Forms/KnowledgeBaseMaintenanceYearScheduleSourceDialog.cs`
- `UiServices/KnowledgeBaseMaintenanceWorkbookUiWorkflowService.cs`
- `Models/KbProductionCalendarYear.cs`
- `Models/KbMaintenanceYearScheduleEntry.cs`
- `Services/KnowledgeBaseProductionCalendarJsonImportService.cs`
- `Services/KnowledgeBaseProductionCalendarPdfImportService.cs`
- `Models/KbEquipmentCatalogItem.cs`
- `Services/KnowledgeBaseEquipmentCatalogService.cs`
- `Models/KbCompositionTemplate.cs`
- `Models/KbObjectTemplate.cs`
- `Services/KnowledgeBaseCompositionTemplateService.cs`
- `Services/KnowledgeBaseObjectTemplateService.cs`
- `Services/KnowledgeBaseTreeMutationWorkflowService.cs`
- `UiServices/KnowledgeBaseTreeMutationUiWorkflowService.cs`
- `Services/KnowledgeBaseDataService.cs`
- `Services/IKnowledgeBaseStorageService.cs`
- `Services/KnowledgeBaseStorageLoadResult.cs`
- `Services/KnowledgeBaseStorageServiceFactory.cs`
- `Services/KnowledgeBaseStoragePaths.cs`
- `Services/KnowledgeBaseRoutedStorageService.cs`
- `Services/KnowledgeBaseFirstLaunchMigrationService.cs`
- `Services/KnowledgeBaseFullJsonExchangeService.cs`
- `Services/KnowledgeBaseSqliteConnectionFactory.cs`
- `Services/SqliteKnowledgeBaseStorageService.cs`
- `Services/KnowledgeBaseSnapshotService.cs`
- `Services/KnowledgeBaseSnapshotComparisonService.cs`
- `Services/KnowledgeBaseChangeLog.cs`
- `Services/JsonLoadResult.cs`
- `Services/JsonStorageService.cs`
- `Services/KnowledgeBaseFileWorkflowService.cs`
- `Forms/KnowledgeBaseSnapshotBrowserForm.cs`
- `Forms/KnowledgeBaseChangeHistoryForm.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseFileWorkflowServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/SqliteKnowledgeBaseStorageServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseFirstLaunchMigrationServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseFullJsonExchangeServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseSnapshotServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseSnapshotComparisonServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseDataServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseEquipmentCatalogServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseProductionCalendarPdfImportServiceTests.cs`
- `Services/KnowledgeBaseMaintenanceYearScheduleSourceService.cs`
- `Services/KnowledgeBaseMaintenanceYearScheduleSourceExchangeService.cs`
- `Services/KnowledgeBaseMaintenanceWorkbookGenerationService.cs`
- `Services/KnowledgeBaseMaintenanceWorkbookExportService.cs`
- `Services/KnowledgeBaseMaintenanceMonthDemandSummaryService.cs`
- `Services/KnowledgeBaseMaintenanceMonthWorkResolverService.cs`
- `Services/KnowledgeBaseMaintenanceMonthlyPlannerService.cs`
- `Services/KnowledgeBaseMaintenanceScheduleNormImportService.cs`
- `Models/KbMaintenanceScheduleProfile.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceScheduleNormImportServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceMonthWorkResolverServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceYearScheduleSourceServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceYearScheduleSourceExchangeServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceWorkbookGenerationServiceTests.cs`
- `scripts/verify-step.ps1`
- `src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj`
- `resources/templates/maintenance-year-template.xlsx`
- `C:\Users\Olga\Downloads\123.xlsx`
- `C:\Users\Olga\Downloads\456.xlsx`

## Known limits / open follow-up

- `Состав -> Применить шаблон...` and `Шаблоны -> Добавить из шаблона состава...` no longer expose hardcoded composition templates. The remaining template gap is lack of a dedicated management window to create/edit/delete user-managed composition or object templates.
- Persisted object templates can still be saved, created from, and exchanged through JSON, but direct application to an existing object is no longer exposed in the current UI.
- Menu rework steps 1-6 are accepted and committed/pushed on `to`; deferred follow-ups are password/role access to `Сервис`, ordinary-user edit restrictions, and broader rights separation.
- `Phase 7E.2` source exchange is implemented on `to`: it exports/imports the yearly placement source as `.xlsx`
- `Phase 7E` in-app mass-editing grid is implemented on `to`
- `Phase 7E` source editing does not create missing maintenance profiles and does not import labor-hour norms
- Manual-review error `Memory stream is not expandable` during May 2026 график ТО generation was fixed locally by opening the workbook package on an expandable memory stream before OpenXML rewriting
- Manual-review screenshot errors from `C:\Users\Olga\Downloads\archive-2026-04-30_13-50-15` were caused by direct visible `Lvl2` assignments being rejected during workbook model building; the local fix is implemented and verified
- 2027 and later production calendars are not built in; configure the needed year through `ТО -> Производственный календарь...`, `ТО -> Импорт производственного календаря PDF...`, or service JSON import before generating that year
- OCR is not implemented in `Phase 7F.1`; the real `calendar_2027.pdf` source has a usable text layer, so OCR is deferred until a source PDF requires it
- Manual UI validation of the local PDF import preview/apply workflow passed by user
- `phase7e-major-work-split-days` implements and verifies splitting one `ТО2` / `ТО3` occurrence across multiple working days
- The planner can place multiple large maintenance items on the same day; that is a soft-avoidance area, not a validated optimization target
- Maintenance profiles have no explicit active-from / active-to dates yet, so the agreed replanning strategy is to freeze past months and recalculate only future months
- `phase7e-norm-import-coverage` improves norm import matching and mismatch reporting; some rows may still remain unmatched when names diverge too much from the KB tree
- `phase7e-annual-norm-import` supports annual workbooks with the same structure as `456.xlsx` for `Импорт норм ТО...`, applies `YearScheduleEntries`, and keeps monthly `123.xlsx` compatibility
- `phase7g-annual-norm-hidden-rows` skips hidden annual workbook rows before parsing; in `456.xlsx`, rows `29`, `30`, and `31` are hidden and should not count toward the May total
- Full Excel round-trip import of generated yearly workbooks has not been validated

## Recommended next step

- Catalog/composition follow-ups are pushed as `3eadf7f`.
- Next optional product follow-up is template management: add user-managed composition or object template editing/deletion if the user requests it.
- Otherwise wait for the user to choose the next roadmap priority before coding.

## Commands to run before finishing future implementation work

```powershell
git status --short
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName <active-step-name>
# The script stops at BUILD: PASS / TESTS: PASS and leaves artifacts in artifacts\verify\<step>.
```
