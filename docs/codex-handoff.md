# Current State

Last updated: `2026-04-30`

## Repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active integration branch: `to`
- Latest feature integration commit for the maintenance-planning stream: `7d56084`
- Latest docs synchronization commit: `87c5023`
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
- Next agreed implementation slice after manual review: decide whether `Phase 7E` needs import/external-source hardening or can be treated complete

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
  - `KnowledgeBaseRussianProductionCalendarService` provides reusable Russian `5/2` workday calculation and is configured for `2025` and `2026`
  - `KnowledgeBaseMaintenanceMonthWorkResolverService` resolves monthly work demand from stored norms and deterministic cycle offsets
  - `KnowledgeBaseMaintenanceMonthlyPlannerService` plans against a monthly hour budget, distributes work across working days, and does not enforce a hard daily `<= 8` cap
  - the export workflow is template-driven and writes one selected month into a yearly accumulating workbook while preserving the rest of the workbook
  - `Файл` contains workshop-level `Импорт норм ТО...`, `Сформировать график ТО за месяц...`, `Сформировать годовой график ТО...`, and `Пересчитать график ТО до конца года...` commands; import/export commands are no longer shown inside each per-node `График ТО` tab
  - the `Сформировать график ТО` dialog shows resolved monthly demand before the user confirms the available workshop budget
  - the yearly generation command shows 12-month demand and generates all months by orchestrating the existing monthly engine
  - the future-month recalculation command opens an existing yearly workbook, preserves earlier month sheets, and rewrites only the selected start month through December
  - `Phase 7E` adds optional per-profile `YearScheduleEntries` stored in JSON; when present, they drive manual 12-month `ТО1` / `ТО2` / `ТО3` placement
  - profiles without `YearScheduleEntries` keep the previous deterministic offset behavior
  - the per-node `График ТО` profile dialog can enable manual annual placement and edit the 12-month source
  - maintenance norms can be imported from `C:\Users\Olga\Downloads\123.xlsx`
  - import matching uses inventory number first, then normalized equipment/system names, and can read the workbook even when it is open in Excel
- User-facing application UI on `to` remains Russian-only

## Validated status

Actually run on the worktree on `2026-04-30`:

```powershell
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7d-menu-maintenance-commands
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7d-year-generation-command
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7d-complete
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7e-year-schedule-source
```

- `dotnet format --verify-no-changes`: passed for app, core, and tests
- Verification `dotnet build`: passed
- `dotnet test`: passed, `250/250`
- Verification artifacts: `artifacts\verify\phase7e-year-schedule-source`
- Manual exe path for review: `C:\Users\Olga\AKB5\artifacts\verify\phase7e-year-schedule-source\build\Release\net8.0-windows\asutpKB.exe`
- Startup smoke was not rerun for the final `Phase 7D` completion slice
- Full Excel round-trip validation (`generate -> open -> edit/save -> import back`) was not run

## Active objective

- Keep the completed `Phase 7` workflow on `to` stable as the baseline
- Manual-review the completed `Phase 7E` first-slice build before committing/pushing
- Preserve deterministic month placement as fallback for profiles that do not enable manual annual placement
- Keep `Phase 7F` production-calendar JSON/UI/import configuration deferred until explicitly prioritized

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
- Production-calendar years are currently configured in `KnowledgeBaseRussianProductionCalendarService`; user-facing JSON/UI/import configuration is deferred to `Phase 7F`
- The planner may place more than one large maintenance item on the same day when needed; it only prefers to spread `ТО2` / `ТО3` apart when possible
- The first release keeps deterministic rule-based month placement for `ТО2` / `ТО3`; a future yearly schedule source may replace that without redesigning the export pipeline
- The yearly workbook export must stay template-driven and preserve existing month sheets, layout, formulas, merges, and signature blocks outside the rewritten month
- The monthly generation mechanism stays the canonical engine
- The yearly command is built on top of the monthly engine by generating months `1..12` sequentially into the same workbook
- The yearly command uses one selected monthly workshop budget for all months and defaults it to the maximum calculated monthly demand for the selected year
- `Phase 7E` stores manual annual maintenance placement in `KbMaintenanceScheduleProfile.YearScheduleEntries`
- Empty `YearScheduleEntries` means old deterministic month placement remains active for that profile
- Manual annual placement is a 12-month profile template, not a production-calendar configuration and not a per-year holiday source
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
- `Controls/KnowledgeBaseMaintenanceScheduleScreenControl.cs`
- `Forms/KnowledgeBaseMaintenanceWorkbookExportDialog.cs`
- `Forms/KnowledgeBaseMaintenanceYearWorkbookExportDialog.cs`
- `Forms/KnowledgeBaseMaintenanceYearWorkbookRecalculationDialog.cs`
- `UiServices/KnowledgeBaseMaintenanceWorkbookUiWorkflowService.cs`
- `Models/KbMaintenanceYearScheduleEntry.cs`
- `Services/KnowledgeBaseMaintenanceWorkbookGenerationService.cs`
- `Services/KnowledgeBaseMaintenanceWorkbookExportService.cs`
- `Services/KnowledgeBaseMaintenanceMonthDemandSummaryService.cs`
- `Services/KnowledgeBaseMaintenanceMonthWorkResolverService.cs`
- `Services/KnowledgeBaseMaintenanceMonthlyPlannerService.cs`
- `Services/KnowledgeBaseMaintenanceScheduleNormImportService.cs`
- `Models/KbMaintenanceScheduleProfile.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceMonthWorkResolverServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceWorkbookGenerationServiceTests.cs`
- `scripts/verify-step.ps1`
- `resources/templates/maintenance-year-template.xlsx`
- `C:\Users\Olga\Downloads\123.xlsx`
- `C:\Users\Olga\Downloads\456.xlsx`

## Known limits / open follow-up

- `Phase 7E` first slice is implemented: manual per-profile year placement source is available, but external import of a yearly schedule source is not implemented
- 2027 and later production calendars are not configured yet
- A single `ТО2` or `ТО3` occurrence is still planned as one assignment; splitting one maintenance item across multiple working days is not implemented yet
- The planner can place multiple large maintenance items on the same day; that is a soft-avoidance area, not a validated optimization target
- Maintenance profiles have no explicit active-from / active-to dates yet, so the agreed replanning strategy is to freeze past months and recalculate only future months
- Norm import from `123.xlsx` is materially better than the first strict matcher, but some rows still remain unmatched when names diverge too much from the KB tree
- Full Excel round-trip import of generated yearly workbooks has not been validated

## Recommended next step

- Manually review the completed `Phase 7E` build from `artifacts\verify\phase7e-year-schedule-source`
- After review, decide whether to:
  - treat `Phase 7E` as complete at the manual per-profile source layer
  - add import/external-source hardening for `Phase 7E`
  - split one `ТО2` / `ТО3` occurrence across multiple working days
  - improve maintenance-norm import coverage further

## Commands to run before finishing future implementation work

```powershell
git status --short
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7e-step-name
# The script stops at BUILD: PASS / TESTS: PASS and leaves artifacts in artifacts\verify\<step>.
```
