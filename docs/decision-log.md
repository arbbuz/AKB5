# Decision Log

Last updated: `2026-04-30`

## 2026-04-30

- The full `Phase 7D follow-up` yearly orchestration is implemented pending manual review
- The monthly generation mechanism remains the canonical planning/export engine
- The whole-year generation command is implemented on top of the existing monthly mechanism rather than replacing it
- The whole-year command applies one selected monthly workshop budget to every generated month and defaults it to the maximum calculated monthly demand in the selected year
- Future-month recalculation is implemented by opening an existing yearly workbook and regenerating only the selected start month through December
- Months before the selected start month are frozen during ordinary replanning and must be preserved in the existing workbook
- Production-calendar years are currently code-configured in `KnowledgeBaseRussianProductionCalendarService`; user-facing JSON/UI/import configuration is deferred to `Phase 7F` and is not a current priority
- The first `Phase 7E` yearly source is stored per maintenance profile as `YearScheduleEntries`, a 12-month `ТО1` / `ТО2` / `ТО3` template
- Empty `YearScheduleEntries` means the profile continues to use deterministic rule-based month placement
- Manual annual placement is separate from production-calendar setup; it does not configure holidays or transfer days
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
