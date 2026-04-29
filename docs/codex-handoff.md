# Current State

Last updated: `2026-04-29`

## Repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active integration branch: `to`
- Latest feature integration commit for the maintenance-planning stream: `7d56084`
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
- Next unfinished roadmap slice: `Phase 7E` future yearly schedule source

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
  - card-field rules now follow visible levels:
    - `Lvl1`: hide `Местоположение` and `Фото`
    - `Lvl2`: show `Инвентарный номер`, hide `Местоположение`, `Фото`, `IP-адрес`, and `Ссылка на схему`
    - `Lvl3+`: hide `Фото`, `IP-адрес`, and `Ссылка на схему`
  - engineering tab visibility for `Lvl3+` now resolves by visible engineering support, not only by persisted `NodeType`
  - engineering nodes expose the `График ТО` workflow
  - `KnowledgeBaseRussianProductionCalendarService` provides reusable Russian `5/2` workday calculation
  - `KnowledgeBaseMaintenanceMonthWorkResolverService` resolves monthly work demand from stored norms and deterministic cycle offsets
  - `KnowledgeBaseMaintenanceMonthlyPlannerService` plans against a monthly hour budget, distributes work across working days, and does not enforce a hard daily `<= 8` cap
  - the export workflow is template-driven and writes one selected month into a yearly accumulating workbook while preserving the rest of the workbook
  - the `Сформировать график ТО` dialog now shows resolved monthly demand before the user confirms the available workshop budget
  - maintenance norms can be imported from `C:\Users\Olga\Downloads\123.xlsx`
  - import matching uses inventory number first, then normalized equipment/system names, and can read the workbook even when it is open in Excel
- User-facing application UI on `to` remains Russian-only

## Validated status

Actually run on the current worktree on `2026-04-29`:

```powershell
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName artifact-launch-fix
```

- Verification `dotnet build`: passed
- `dotnet test`: passed, `243/243`
- Startup smoke was rechecked from `artifacts\verify\artifact-launch-fix\build\Release\net8.0-windows\asutpKB.exe`; the process stayed running instead of exiting immediately
- User manual validation after the workbook repair/export fixes confirmed that the generated Excel file opens in Excel and the export dialog content is visible
- Full Excel round-trip validation (`generate -> open -> edit/save -> import back`) was not run
- `dotnet format --verify-no-changes` was not rerun after the final maintenance/export changes

## Active objective

- Keep the completed `Phase 7` workflow on `to` stable as the new baseline
- Use the current rule-based resolver/planner/export pipeline until a future yearly schedule source is introduced
- Decide the next refinement step explicitly instead of expanding `Phase 7` in one pass

## Durable decisions already made

- `to` is now the active integration branch for current work; `.github/workflows/windows-build.yml` targets `to`
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
- The planner may place more than one large maintenance item on the same day when needed; it only prefers to spread `ТО2` / `ТО3` apart when possible
- The first release keeps deterministic rule-based month placement for `ТО2` / `ТО3`; a future yearly schedule source may replace that without redesigning the export pipeline
- The yearly workbook export must stay template-driven and preserve existing month sheets, layout, formulas, merges, and signature blocks outside the rewritten month
- `docs/codex-handoff.md` remains the single current-state file for future sessions

## Relevant files for the current task area

- `Models/KbMaintenanceScheduleProfile.cs`
- `Models/KbMaintenanceWorkKind.cs`
- `Models/KbMaintenanceMonthWorkItem.cs`
- `Models/KbMaintenanceMonthPlanAssignment.cs`
- `Models/KbMaintenanceMonthPlanDay.cs`
- `Models/KbMaintenanceMonthSheetModel.cs`
- `Models/SavedData.cs`
- `Services/KnowledgeBaseRussianProductionCalendarService.cs`
- `Services/KnowledgeBaseMaintenanceScheduleStateService.cs`
- `Services/KnowledgeBaseMaintenanceMonthWorkResolverService.cs`
- `Services/KnowledgeBaseMaintenanceMonthlyPlannerService.cs`
- `Services/KnowledgeBaseMaintenanceMonthDemandSummaryService.cs`
- `Services/KnowledgeBaseMaintenanceMonthSheetModelBuilderService.cs`
- `Services/KnowledgeBaseMaintenanceWorkbookTemplateService.cs`
- `Services/KnowledgeBaseMaintenanceWorkbookExportService.cs`
- `Services/KnowledgeBaseMaintenanceWorkbookGenerationService.cs`
- `Services/KnowledgeBaseMaintenanceScheduleNormImportService.cs`
- `Services/KnowledgeBaseEngineeringNodeSupportService.cs`
- `Services/KnowledgeBaseNodePresentationService.cs`
- `Services/KnowledgeBaseNodeWorkspaceResolverService.cs`
- `Services/KnowledgeBaseFormStateService.cs`
- `Services/KnowledgeBaseDataService.cs`
- `Forms/MainForm.Maintenance.cs`
- `Forms/KnowledgeBaseMaintenanceScheduleProfileDialog.cs`
- `Forms/KnowledgeBaseMaintenanceWorkbookExportDialog.cs`
- `Controls/KnowledgeBaseMaintenanceScheduleScreenControl.cs`
- `Controls/KnowledgeBaseInfoScreenControl.cs`
- `UiServices/KnowledgeBaseMaintenanceWorkbookUiWorkflowService.cs`
- `scripts/verify-step.ps1`
- `resources/templates/maintenance-year-template.xlsx`
- `C:\Users\Olga\Downloads\123.xlsx`
- `C:\Users\Olga\Downloads\456.xlsx`

## Known limits / open follow-up

- `Phase 7E` future yearly schedule source is not implemented; `ТО2` / `ТО3` month placement still comes from deterministic rule-based offsets
- A single `ТО2` or `ТО3` occurrence is still planned as one assignment; splitting one maintenance item across multiple working days is not implemented yet
- The planner can place multiple large maintenance items on the same day; that is a soft-avoidance area, not a validated optimization target
- Norm import from `123.xlsx` is materially better than the first strict matcher, but some rows still remain unmatched when names diverge too much from the KB tree
- `AGENTS.md` and `Roadmap.md` still contain stale `interface` references and outdated `Phase 7` assumptions until they are synchronized
- Full Excel round-trip import of generated yearly workbooks has not been validated

## Recommended next step

- Choose one explicit next slice before more implementation:
  - support splitting one `ТО2` / `ТО3` occurrence across multiple working days and update export accordingly
  - improve maintenance-norm import coverage and mismatch reporting for the remaining unmatched equipment
  - implement `Phase 7E` by replacing deterministic month placement with an externally provided yearly schedule source
- Before a large new session, synchronize `AGENTS.md` and `Roadmap.md` with the current `to` baseline and current `Phase 7` rules

## Commands to run before finishing future implementation work

```powershell
git status --short
powershell -ExecutionPolicy Bypass -File C:\Users\Olga\AKB5\scripts\verify-step.ps1 -StepName phase7-step-name
# The script stops at BUILD: PASS / TESTS: PASS and leaves artifacts in artifacts\verify\<step>.
```
