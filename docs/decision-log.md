# Decision Log

Last updated: `2026-05-06`

## 2026-05-06

- `Phase 7F.1` imports production-calendar PDF files through a text layer first, shows a preview before applying changes, and keeps OCR deferred until a real source PDF requires it
- `KbProductionCalendarYear` now supports `AdditionalWorkingDays` so transferred working Saturdays/Sundays can be represented together with additional non-working days
- `C:\Users\Olga\Downloads\calendar_2027.pdf` has a usable text layer and imports as 2027 with additional non-working days `22.02.2027`, `03.05.2027`, `10.05.2027`, `14.06.2027`, `05.11.2027`, `31.12.2027`, plus additional working day `20.02.2027`
- On 2026-05-06, `phase7f1-production-calendar-pdf-import` passed verification build and `dotnet test` (`281/281`) using isolated output paths; the user manually confirmed that production-calendar PDF import works
- `Phase 7F.1` was committed and pushed on `to` as `09bf84d Add production calendar PDF import`
- `Phase 11B` adds an in-app Russian equipment-catalog editor under `Файл -> Каталог оборудования...`; catalog editing remains separate from tree editing and object-template creation
- On 2026-05-06, `phase11b-equipment-catalog-ui` passed verification build and `dotnet test` (`287/287`) using isolated output paths
- `Phase 11B` was committed and pushed on `to` as `f80873f Add equipment catalog UI`

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
- Missing production-calendar years should guide the user to `Файл -> Производственный календарь...` or PDF/JSON import instead of requiring a code change
- There is no approved `Phase 7G` in `Roadmap.md`; new work after `Phase 7F` must be defined and accepted before implementation
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
