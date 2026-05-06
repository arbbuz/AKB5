# Current State

Last updated: `2026-05-06`

## Repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active integration branch: `to`
- Latest feature integration commit for the maintenance-planning stream: `09bf84d Add production calendar PDF import`
- Latest docs synchronization commit: `68d51b6 Distill roadmap state after Phase 7F`
- Latest accepted implementation: `Phase 11D create from template`
- Current local implementation awaiting manual review: none
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
  - `Phase 7F production-calendar configuration`
  - `Phase 7F.1 production-calendar PDF import`
  - `Phase 11A equipment catalog model`
  - `Phase 11B equipment catalog UI`
  - `Phase 11C object template model`
  - `Phase 11D create from template`
- Current active gate: continue to `Phase 11E. Save existing object as template`
- `Phase 11B. Equipment catalog UI` was committed and pushed on `to` as `f80873f Add equipment catalog UI`

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
  - `Файл` contains workshop-level `Импорт норм ТО...`, `Сформировать график ТО за месяц...`, `Сформировать годовой график ТО...`, and `Пересчитать график ТО до конца года...` commands; import/export commands are no longer shown inside each per-node `График ТО` tab
  - `Phase 7E.2` adds `Файл -> Экспорт источника годового графика ТО...` and `Файл -> Импорт источника годового графика ТО...`
  - `Phase 7E` mass-editing grid adds `Файл -> Редактировать источник годового графика ТО...` for current-workshop profile rows
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
  - `Phase 7F` adds `Config.ProductionCalendarYears`, a Russian `Файл -> Производственный календарь...` editor, `Файл -> Импорт производственного календаря JSON...`, JSON import validation, and guided missing-year errors
  - `Phase 7F.1` adds `Файл -> Импорт производственного календаря PDF...`, text-layer PDF parsing through `PdfPig`, a preview dialog before applying imported dates, and `AdditionalWorkingDays` support for transferred working Saturdays/Sundays
  - `phase7e-major-work-split-days` splits one `ТО2` / `ТО3` occurrence into up-to-8-hour assignments across working days when possible
  - `phase7e-norm-import-coverage` improves norm import matching for leading-zero inventory numbers, `ё/е`, and parenthetical equipment names; unresolved rows include source sheet/row context
  - maintenance norms can be imported from `C:\Users\Olga\Downloads\123.xlsx`
  - import matching uses inventory number first, then normalized equipment/system names, and can read the workbook even when it is open in Excel
- `Phase 11` current state on `to`:
  - `Phase 11A` adds the top-level JSON equipment catalog model and normalization
  - `Phase 11B` adds `Файл -> Каталог оборудования...` for listing, adding, editing, deleting, and searching equipment catalog items
  - catalog editing remains separate from tree editing and object-template creation
  - local `Phase 11C` adds top-level JSON/session object templates, template nodes keyed by `TemplateNodeId`, normalization, and an instantiation service that generates fresh real `NodeId` values and remaps linked defaults
  - local `Phase 11D` adds a tree context-menu command `Создать объект из шаблона...` and a Russian selection dialog for persisted object templates
  - creating from a template inserts the whole template subtree, applies normal tree reindexing/depth checks, and appends remapped composition, document/software, network-file, and maintenance-profile defaults
  - `Phase 11D` intentionally does not add template creation/editing; saving an existing object as a template remains `Phase 11E`
- User-facing application UI on `to` remains Russian-only

## Validated status

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
- Current documentation-distillation slice is docs-only; build/test/harness are not required unless code changes are added

## Active objective

- Keep the completed `Phase 7` workflow on `to` stable as the baseline
- Continue to `Phase 11E. Save existing object as template` unless redirected
- Preserve the production-calendar follow-up behavior: manual calendar dates are shown as `дд.мм.гггг`, JSON import accepts both `дд.мм.гггг` and legacy ISO dates, saved app JSON remains backward-compatible, and additional working days can represent transferred working weekends
- Keep `Phase 11B` as the accepted equipment-catalog UI baseline
- Preserve deterministic month placement as fallback for profiles that do not enable manual annual placement
- Keep JSON as the source of truth for calendar configuration; generated workbooks remain report artifacts
- Do not start `Phase 7G` unless it is first defined and accepted in the roadmap
- Keep `Phase 12` deferred until Phase 11 is finished or explicitly redirected

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
- The hard planner constraint is the selected monthly workshop budget, not a daily `<= 8` cap
- Production-calendar years live in `KbConfig.ProductionCalendarYears`; built-in `2025`/`2026` defaults are preserved, while future years can be added through UI, JSON import, or local PDF import
- `Phase 11` is approved as object templates and equipment catalog; `Phase 11A` contains the catalog model only and passed manual review
- `Phase 7F.1` is accepted after manual review and committed/pushed as `09bf84d`; it adds PDF import for production calendars because JSON import is inconvenient for ordinary use
- `Phase 11B` keeps catalog editing separate from tree editing and object-template creation; it only maintains catalog records
- `Phase 11C` kept object templates as data/model/service work only; `Phase 11D` adds the create-from-template UI/workflow on top of it
- `Phase 11C` templates use `TemplateNodeId` for internal references and generate fresh persisted `NodeId` values only when instantiated
- `Phase 11D` creates objects only from already persisted `SavedData.ObjectTemplates`; creating/editing/saving templates remains a later slice
- The user confirmed manual review of Phase 11D and explicitly requested committing/pushing the Phase 11C/11D stack before Phase 11E
- `Phase 12` is approved as backup, snapshots, and change history after Phase 11
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

## Relevant files for the next task area

- `Forms/MainForm.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.Events.cs`
- `Forms/MainForm.Maintenance.cs`
- `Forms/MainForm.ProductionCalendar.cs`
- `Forms/MainForm.EquipmentCatalog.cs`
- `Forms/KnowledgeBaseEquipmentCatalogForm.cs`
- `Forms/KnowledgeBaseEquipmentCatalogItemDialog.cs`
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
- `Services/KnowledgeBaseDataService.cs`
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

- `Phase 7E.2` source exchange is implemented on `to`: it exports/imports the yearly placement source as `.xlsx`
- `Phase 7E` in-app mass-editing grid is implemented on `to`
- `Phase 7E` source editing does not create missing maintenance profiles and does not import labor-hour norms
- Manual-review error `Memory stream is not expandable` during May 2026 график ТО generation was fixed locally by opening the workbook package on an expandable memory stream before OpenXML rewriting
- Manual-review screenshot errors from `C:\Users\Olga\Downloads\archive-2026-04-30_13-50-15` were caused by direct visible `Lvl2` assignments being rejected during workbook model building; the local fix is implemented and verified
- 2027 and later production calendars are not built in; configure the needed year through `Файл -> Производственный календарь...`, JSON import, or local PDF import before generating that year
- OCR is not implemented in `Phase 7F.1`; the real `calendar_2027.pdf` source has a usable text layer, so OCR is deferred until a source PDF requires it
- Manual UI validation of the local PDF import preview/apply workflow passed by user
- `phase7e-major-work-split-days` implements and verifies splitting one `ТО2` / `ТО3` occurrence across multiple working days
- The planner can place multiple large maintenance items on the same day; that is a soft-avoidance area, not a validated optimization target
- Maintenance profiles have no explicit active-from / active-to dates yet, so the agreed replanning strategy is to freeze past months and recalculate only future months
- `phase7e-norm-import-coverage` improves norm import matching and mismatch reporting; some rows may still remain unmatched when names diverge too much from the KB tree
- Full Excel round-trip import of generated yearly workbooks has not been validated

## Recommended next step

- Continue to `Phase 11E. Save existing object as template` unless redirected

## Commands to run before finishing future implementation work

```powershell
git status --short
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName <active-step-name>
# The script stops at BUILD: PASS / TESTS: PASS and leaves artifacts in artifacts\verify\<step>.
```
