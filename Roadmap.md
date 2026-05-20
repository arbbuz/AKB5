# Roadmap

Last updated: 2026-05-12
Branch baseline: `to`
Implementation status: `Phase 0 complete on to, Phase 1 complete on to, Phase 2 complete on to, Phase 3 complete on to, Phase 3B complete on to, Phase 4 complete on to, Phase 5 complete on to, Phase 6 complete on to, Phase 7A complete on to, Phase 7B complete on to, Phase 7C complete on to, Phase 7D complete on to, Phase 7E first slice complete on to, Phase 7E.2 source exchange complete on to, Phase 7E mass-editing grid complete on to, major ТО2/ТО3 split complete on to, norm import coverage complete on to, phase7e-annual-norm-import accepted, phase7g-annual-norm-hidden-rows committed/pushed on to, Phase 7F production-calendar configuration complete on to, Phase 7F.1 PDF calendar import complete on to, Phase 11A accepted, production-calendar Russian date format accepted, Phase 11B equipment catalog UI complete on to, Phase 11C object template model accepted, Phase 11D create from template accepted, Phase 11E save existing object as template accepted, Phase 11F apply template with preview accepted, Phase 11G template import/export complete on to, Phase 12S0 SQLite storage plan approved, Phase 12S1-S8 storage redesign complete on to, menu rework first iteration committed/pushed on to, portable-first storage follow-up locally verified and awaiting review`

## Goal

Transform `AKB5` from a level-driven tree editor into a type-driven engineering workstation:

- the left side remains a physical object tree
- the right side becomes a workspace that depends on `NodeType`
- `LevelIndex` remains only as an internal technical coordinate
- composition, documentation/software, and network data stop being forced into one tree shape

## Confirmed decisions

1. The object tree stays on the left as the main physical navigator.
2. The right panel becomes type-driven and can differ by object kind.
3. User-facing level configuration and level renaming are removed from the UI.
4. `LevelIndex` stays in the model as a hidden technical mechanism.
5. Excel workbook `v3` stays a legacy transition layer and is no longer the main direction of future feature work.
6. `MaxLevels` should be hidden from the user.
7. Preferred default for hidden `MaxLevels` is `10`.
8. The first version of the `Network` tab is file-based, not interactive.
9. File-based `Network` must provide:
   - a large preview inside the form
   - an `Open original` action
10. User-facing program UI should use Russian only.
11. The approved target storage direction is SQLite single-file storage with JSON import/export compatibility and first-launch migration from the existing legacy JSON database.
## Non-negotiable architecture rules

1. `NodeType` must become more important than `LevelIndex`.
2. No new right-panel behavior may depend only on `LevelIndex`.
3. New cross-links must never rely on node names or paths.
4. A persistent `NodeId` must exist in the domain model and JSON before composition/doc/network features are built.
5. Do not store all future data in one bloated `KbNodeDetails` object.
6. Do not overload the left tree with composition or network data just to avoid creating proper models.
7. Excel `v3` compatibility must be preserved during the transition, but new feature investment should prefer report/template workflows over broader bidirectional workbook exchange.

## Current technical reality

- Current local builds use portable-first SQLite single-file `.akb` storage: `akb5.settings.json` next to `asutpKB.exe`, default `database\knowledge-base.akb` next to the program, and user-selected database paths remembered in the settings file.
- Current JSON schema version is `3`; it remains the compatibility format for first-launch migration, full JSON import/export, and legacy JSON file routing.
- The domain node now has `NodeId` and `NodeType`; legacy data is normalized/migrated on load.
- Hidden workshop wrappers are now identified through explicit `NodeType.WorkshopRoot` in projection/session workflows.
- `Phase 2` is complete on `to`: the right panel now routes by `NodeType` into a clean `Info` screen or an engineering tab host.
- The generic `Info` screen is now extracted into a reusable control so the same UI can be hosted standalone or inside the `Info` tab.
- `Phase 3` is complete on `to`: `Composition` now uses a dedicated typed model stored in `SavedData.CompositionEntries`.
- The `Composition` screen now shows slots separately from auxiliary equipment and supports in-app add/edit/delete for typed entries.
- Composition ordering is now resolved by `SlotNumber` + `PositionOrder`, independent of child-node order in the left tree.
- `Phase 3B` is complete on `to`: built-in cabinet/controller templates and `copy composition from existing object` are available for typed composition workflows.
- `Phase 4` is complete on `to`: `Documentation and Software` uses dedicated typed records stored in top-level `DocumentLinks` and `SoftwareRecords` collections keyed by `OwnerNodeId`.
- The `Documentation and Software` screen is intentionally separate from `Composition`: it manages scheme links, instruction links, and software-folder links rather than slot-style entries.
- The current `Phase 4` software UX records the date a software link was added (`AddedAt`); legacy software timestamps/notes remain compatibility-only persistence fields and are not part of the main editing UI.
- `Phase 5` is complete on `to`: search indexes `Tree`, `Card`, `Composition`, and `Docs/Software` data and exposes scopes `All`, `Tree`, `Card`, `Composition`, and `Docs/Software`.
- Search results now navigate back to the owning tree node and can switch the workspace to the preferred tab for the matched domain.
- User-facing interface text on `to` is now normalized to Russian; new UI work should keep Russian-only labels, prompts, and status text.
- `Phase 6` is complete on `to`: `Network` now uses typed file references, image preview inside the form, and `Open original` for server/file paths.
- The `Phase 6` `Network` screen uses separate `Файлы` and `Предпросмотр` tabs; node load returns to `Файлы`, and automatic switching to `Предпросмотр` is not part of the accepted UX.
- On 2026-05-19, branch `Net` extends the network work with typed topology/passport storage, CRUD for devices/interfaces/connections on `Сеть -> Паспорт`, manual scheme-entry fields, editable protocol presets (`PROFINET`, `PROFIBUS`, `MPI`), editable medium presets (`Медь`, `Оптика`), local passport filtering, scroll-safe network add/edit dialogs, and PDF network scheme references classified as `PDF` with metadata/`Open original` behavior.
- The current `Net` scope deliberately excludes OCR/PDF auto-import, PRONETA/CSV import, live scan, plan/fact comparison, data-quality issue panels, and AKB5-driven IP/PROFINET-name assignment.
- On 2026-04-28, the current `Phase 6` worktree passed verification build, passed `dotnet test` (`177/177`), and `asutpKB.exe` startup was rechecked after the final `Network` UX fixes.
- Current Excel `v3` now preserves `NodeId` after import and writes/reads a read-only `NodeType` column as part of the transition, but further workbook modernization is no longer the preferred next phase.
- Current CI workflow also verifies `dotnet format --verify-no-changes` for the app project, core project, and tests before `build` / `test`.
- The maintenance-schedule generation roadmap through `Phase 7F.1` is complete on `to`; `phase7g-annual-norm-hidden-rows` is committed/pushed as `7a4895d`; `Phase 11B` equipment catalog UI is complete on `to`; `Phase 11C` through `Phase 11F` object-template slices are accepted and committed/pushed; `Phase 11G` is accepted and committed/pushed; `Phase 12S0` SQLite storage plan is approved; `Phase 12S1` through `Phase 12S8` are accepted and committed/pushed on `to`; menu rework first iteration is committed/pushed as `8dfffbd`.
- Portable-first storage follow-up keeps `.akb` as the main format, remembers the chosen database path in `akb5.settings.json`, can copy the old AppData `.akb` into the selected path on first run, and creates external timestamped `.akb` backups under `backups\yyyy-MM-dd\` before overwriting an existing SQLite database.
- `Phase 7A` is complete on `to`: `Lvl2` inventory number support now follows visible hierarchy level, typed `MaintenanceScheduleProfiles` are persisted in JSON/session state, and engineering nodes expose a `График ТО` tab with per-node `ТО1` / `ТО2` / `ТО3` hour norms.
- `Phase 7B` is complete on `to`: Russian production-calendar calculation for `5/2` workdays is available as a reusable service.
- `Phase 7F` is complete on `to`: production-calendar years are persisted in JSON config, editable from the Russian UI, importable from JSON, and consumed by maintenance schedule generation.
- `Phase 7C` is complete on `to`: the resolver and monthly planner generate month demand from `ТО1` / `ТО2` / `ТО3` norms and compare it against the selected monthly workshop budget.
- `Phase 7D` is complete on `to`: the yearly workbook export is template-driven, exposed in the UI, and can import maintenance norms from monthly `123.xlsx`; `phase7e-annual-norm-import` also supports annual workbooks with the same structure as `456.xlsx`.
- The first `Phase 7D` follow-up slice is complete on `to`: workshop-level `Импорт норм ТО...` and `Сформировать график ТО за месяц...` commands now live in the top-level `Файл` menu instead of the per-node `График ТО` tab.
- The second `Phase 7D` follow-up slice is complete on `to`: `Файл -> Сформировать годовой график ТО...` generates all 12 months in one pass by orchestrating the existing monthly engine.
- The third `Phase 7D` follow-up slice is complete on `to`: `Файл -> Пересчитать график ТО до конца года...` opens an existing yearly workbook, preserves earlier month sheets, and rewrites only the selected start month through December.
- The first `Phase 7E` slice is complete on `to`: maintenance profiles can store manual 12-month `ТО1` / `ТО2` / `ТО3` placement in JSON, the profile dialog can edit it, and the resolver uses it before falling back to deterministic offsets.
- On 2026-04-30, the current `Phase 7E` worktree passed `dotnet format --verify-no-changes`, verification build, and `dotnet test` (`250/250`) using isolated output paths.
- On 2026-05-04, the accepted `Phase 7E` follow-up direction is `Phase 7E.2`: Excel export/import of the yearly schedule source before any dedicated in-app mass-editing grid.
- `Phase 7E.2` is complete on `to`: it exports/imports a separate source workbook with `OwnerNodeId` and editable `M01`..`M12` values; import changes only `YearScheduleEntries` and does not change norms, inclusion flags, or production-calendar settings.
- On 2026-05-04, `phase7e-year-schedule-source-exchange` passed `dotnet format --verify-no-changes`, verification build, and `dotnet test` (`255/255`) using isolated output paths.
- On 2026-05-04, the manual-review `Memory stream is not expandable` workbook generation error was fixed and `phase7e-workbook-expandable-stream-fix` passed verification build and `dotnet test` (`256/256`).
- The `Phase 7E` mass-editing grid adds `Файл -> Редактировать источник годового графика ТО...` for current-workshop profile rows; it edits only month placement and leaves norms, inclusion flags, and production calendars untouched.
- On 2026-05-04, `phase7e-year-source-mass-edit-grid` passed `dotnet format --verify-no-changes`, verification build, and `dotnet test` (`261/261`) using isolated output paths.
- The major-work split follow-up splits one `ТО2` / `ТО3` occurrence into assignments of up to 8 hours and spreads those assignments across working days when possible; the selected monthly workshop budget remains the hard constraint and production-calendar configuration is unchanged.
- On 2026-05-04, `phase7e-major-work-split-days` passed `dotnet format --verify-no-changes`, targeted monthly-planner tests, verification build, and `dotnet test` (`262/262`) using isolated output paths.
- The maintenance-norm import coverage follow-up improves matching for leading-zero inventory numbers, `ё/е`, parenthetical equipment names from `123.xlsx`, and reports unresolved rows with source sheet/row context.
- On 2026-05-04, `phase7e-norm-import-coverage` passed `dotnet format --verify-no-changes`, targeted norm-import tests, verification build, and `dotnet test` (`264/264`) using isolated output paths.
- The `phase7e-annual-norm-import` follow-up lets `ТО -> Импорт норм ТО...` accept annual workbooks by structure, with `456.xlsx` as the reference example; it extracts per-occurrence `ТО1` / `ТО2` / `ТО3` hours from the annual plan columns, applies `YearScheduleEntries`, keeps `123.xlsx` compatibility, passed verification build and `dotnet test` (`334/334`) on 2026-05-07, and passed manual review.
- `phase7g-annual-norm-hidden-rows` skips hidden annual workbook rows before parsing headers or equipment rows, because hidden rows in `456.xlsx` represent retired equipment; it passed verification build and `dotnet test` (`335/335`) on 2026-05-07 and was committed/pushed on `to` as `7a4895d Fix annual maintenance norm import totals`.
- Menu rework first iteration is complete on `to`: top menu commands are regrouped, snapshots/history have one entry, `ТО` is grouped, tree context templates are under `Шаблоны`, move confirmation shows old/new parent and `LvlX -> LvlY`, and dangerous operations offer protective snapshots. It passed `menu-rework-stage6` verification (`340/340`) and was committed/pushed as `8dfffbd Rework menu structure and safety prompts`.
- `Phase 7F` production-calendar configuration is complete on `to`: `Config.ProductionCalendarYears` stores year-specific additional non-working days, `Файл` exposes calendar edit/import commands, and schedule generation resolves the calendar from the saved configuration.
- On 2026-05-04, `phase7f-production-calendar-config` passed `dotnet format --verify-no-changes`, verification build, and `dotnet test` (`270/270`) using isolated output paths.
- On 2026-05-05, the user approved starting from `Phase 11` and then `Phase 12`; `Phase 8` through `Phase 10` remain discussed candidate directions, not active implementation phases.
- `Phase 11A` is accepted after manual review: it adds the equipment catalog domain model, JSON persistence, normalization, deduplication, and focused tests before UI work.

## Hidden-level strategy

Preferred strategy:

- keep `LevelIndex` in data and internal services
- remove all level configuration from user workflows
- stop showing user-facing level names as a primary UI concept
- keep `MaxLevels` in the hidden config with default `10`
- ensure the code respects `MaxLevels` as a value and does not hardcode assumptions that break if it becomes `12` later

Fallback strategy if hidden-config flexibility proves too invasive:

- freeze technical depth at constant `10` for the first typed release
- still keep `LevelIndex` in persisted data for compatibility
- revisit hidden `MaxLevels` only after typed screens and typed data are stable

## Recommended target model

Minimum foundation:

- `KbNode`
  - `NodeId`
  - `Name`
  - `LevelIndex`
  - `NodeType`
  - `Details`
  - `Children`

- `KbNodeDetails`
  - keep generic summary/card fields only
  - add `Note`
  - stop using it as the dumping ground for composition/doc/network data

New typed data should live in dedicated models:

- `KbCompositionEntry`
  - `EntryId`
  - `ParentNodeId`
  - `SlotNumber?`
  - `PositionOrder`
  - `ComponentType`
  - `Model`
  - `IpAddress?`
  - `LastCalibrationAt?`
  - `NextCalibrationAt?`
  - `Notes`

- `KbDocumentLink`
  - `DocumentId`
  - `OwnerNodeId`
  - `Kind`
  - `Title`
  - `Path`
  - `UpdatedAt`

- `KbSoftwareRecord`
  - `SoftwareId`
  - `OwnerNodeId`
  - `Title`
  - `Path`
  - `AddedAt`

- `KbNetworkFileReference`
  - `NetworkAssetId`
  - `OwnerNodeId`
  - `Title`
  - `Path`
  - `PreviewKind`

## Screen model

### Common principle

The right side should resolve by capability, not by pure level.

Suggested capability map:

- `Info`
  - almost all nodes
- `Composition`
  - cabinets, PLC-like devices, expandable engineering containers
- `DocsAndSoftware`
  - nodes that own documents, backups, software artifacts
- `Network`
  - nodes with a network file or diagram

### First concrete screens

- `Department/System`
  - summary
  - object card
  - note

- `Cabinet and deeper engineering nodes`
  - `Info`
  - `Composition`
  - `Documentation and Software`
  - `Network`

Important:

- do not use `all nodes with LevelIndex >= 4` as the long-term logic
- use `NodeType` and capabilities to decide which tabs appear

## Phased roadmap

### Phase 0. Remove user-facing levels from UX

Complexity: `Medium`

Goals:

- remove `SetupForm` and the `configure levels` workflow from the UI
- stop advertising levels as a user concept
- keep `LevelIndex` and `MaxLevels` only as internal mechanics
- keep legacy config structures in JSON/Excel for compatibility

Main changes:

- remove the toolbar/menu entry for level setup
- remove or de-emphasize `LevelName` from the right panel and status/search hints
- introduce hidden default `MaxLevels = 10`
- keep validation of depth internally

Files likely affected:

- `Forms/SetupForm.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.Events.cs`
- `UiServices/KnowledgeBaseWorkshopUiWorkflowService.cs`
- `Services/KnowledgeBaseConfigurationWorkflowService.cs`
- `Services/KnowledgeBaseFormStateService.cs`
- `README.md`
- `docs/workbook-v3.md`

Acceptance:

- the user can no longer rename/configure levels in the UI
- add/move/paste tree operations still respect technical depth
- existing JSON and Excel `v3` files still load

### Phase 1. Foundation: `NodeId`, `NodeType`, schema migration

Complexity: `High`

Goals:

- make every node stably addressable
- decouple behavior from `LevelIndex`
- prepare the project for composition, docs, and network tabs

Main changes:

- add `NodeId` and `NodeType` to `KbNode`
- add migration from JSON schema `2` to a new schema version
- preserve legacy JSON load by generating missing IDs/types deterministically during migration
- update tree mutation, clone, copy/paste, drag/drop, import/export workflows to preserve `NodeId`
- define a conservative initial `NodeType` enum
- map legacy nodes to initial types using migration rules

Recommended initial `NodeType` set:

- `WorkshopRoot`
- `Department`
- `System`
- `Cabinet`
- `Device`
- `Controller`
- `Module`
- `DocumentNode`
- `Unknown`

Excel strategy in this phase:

- keep workbook `v3` support
- leave `Levels` as legacy
- do not require Excel to edit all new typed fields yet
- only extend `v3` with optional typed columns if strictly necessary

Acceptance:

- all nodes have persistent IDs after load/save
- rename/move operations do not break typed references
- the application no longer depends on node names/paths for future cross-links

### Phase 2. Replace the flat right panel with a screen host

Complexity: `High`

Goals:

- stop treating the right side as a single static card
- introduce a screen resolver by `NodeType`

Main changes:

- replace the current one-card layout with a typed workspace host
- keep an `Info` screen as the default/fallback
- add a screen resolver service that maps `NodeType` to a view model and visible tabs
- move generic summary/card/note into reusable components
- stop using `LevelIndex >= 2` as the rule for technical fields

Files likely affected:

- `Forms/MainForm.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.NodeDetails.cs`
- `Services/KnowledgeBaseFormStateService.cs`
- new screen/view-model services

Acceptance:

- `Department` and `System` show a clean `Info` screen
- `Cabinet` and typed engineering nodes can show a tab host
- unknown/legacy node types still fall back to a safe generic screen

### Phase 3. Composition model and cabinet-focused workflow

Complexity: `High`

Goals:

- represent cabinet/PLC contents as ordered engineering composition, not as a forced tree garland

Main changes:

- add the composition data model
- add the `Composition` tab
- support slot ordering and non-slot auxiliary equipment
- store PLC IP and module calibration dates at the composition-entry level where relevant
- sort composition by slot/order, never alphabetically
- keep deep tree children under a cabinet as legacy-readable, but stop relying on them as the primary model

Important UX rule:

- composition order is positional, not lexical

Acceptance:

- a cabinet can display ordered slot content
- extra equipment can be shown separately from slots
- edits are persisted independently of tree child order

### Phase 3B. Templates and copy-from-sample

Complexity: `Medium-High`

Goals:

- reduce manual data entry

Main changes:

- add cabinet/controller templates
- add `create from template`
- add `copy composition from existing object`
- auto-fill inherited context from the parent tree location

Acceptance:

- a new cabinet can be created from a template with prefilled composition
- a similar cabinet can be cloned without rebuilding slot content manually

### Phase 4. Documentation and Software

Complexity: `Medium`

Goals:

- gather engineering references in one place without polluting the base card
- keep documentation/software management separate from the slot-oriented `Composition` workflow

Main changes:

- add `Documentation and Software` tab
- add typed lists for:
  - scheme links
  - manuals/instructions
  - links to folders with current software versions
- store the date a software link was added
- provide open actions for file/server paths

Acceptance:

- a node can store multiple document/software entries
- software-link added dates are explicit and do not depend on file name conventions
- docs/software data is not modeled as a second `Composition` screen

### Phase 5. Search redesign

Complexity: `Medium-High`

Goals:

- search by engineering meaning, not only by tree label

Main changes:

- replace name-only matching with indexed search across:
  - node name
  - summary/card fields
  - note
  - composition
  - documents/software
- expose scopes such as:
  - `Tree`
  - `Card`
  - `Composition`
  - `Docs/Software`
  - `All`

Important:

- avoid a binary switch only between `tree` and `screens`
- search scopes should reflect actual data domains

Acceptance:

- searches can find an object by IP/model/document title/slot content where relevant
- results still navigate back to the owning tree node

### Phase 6. Network tab, file-based first

Complexity: `Medium`

Goals:

- deliver useful network context without building an interactive topology editor

First version scope:

- `Network` tab stores one or more file references
- large preview inside the form
- `Open original` action

Recommended first previewable types:

- `jpg`
- `jpeg`
- `png`
- `bmp`
- optionally `gif`

Recommended first-release behavior for non-image files:

- show metadata, title, and a clear `Open original` action
- do not promise embedded PDF preview until a rendering dependency is approved

Why this restriction matters:

- image preview can reuse the existing photo/open workflow patterns
- embedded PDF preview in WinForms is a separate dependency and maintenance decision

Acceptance:

- the user can preview an image-based network scheme directly in the right panel
- the original file can always be opened via shell

`Net` branch follow-up accepted on 2026-05-19:

- typed network topology/passport storage exists for devices, interfaces, and connections
- `Сеть -> Паспорт` supports manual CRUD for devices, interfaces, and connections without changing the first file-reference workflow
- interface and connection dialogs provide editable presets for protocol (`PROFINET`, `PROFIBUS`, `MPI`) and medium (`Медь`, `Оптика`)
- passport filtering covers visible network fields such as device names, interfaces, IP data, endpoints, protocol, medium, cable, and notes
- network device/interface/connection dialogs use scroll-safe layouts and have layout-smoke coverage for visible fields/buttons
- network scheme file references can store a manual source-note/comment for the exact visual fragment used from a scheme
- the interface passport grid shows stored `MPI/DP/PN` and medium values so manual scheme data is visible without reopening the edit dialog
- the interface dialog validates manually entered IP address, mask, and gateway values before saving while still allowing blank unknown fields
- the connection dialog validates manually entered cable length while still allowing blank unknown length
- the connection passport grid shows richer endpoint text with device/interface/IP/`MPI/DP/PN` context and a visible cable-length column for manual scheme checks
- the connection passport grid supports copy-friendly manual checks: selected rows can be copied as tab-separated text, and endpoints A/B can be copied separately
- the device passport grid shows vendor/location values and supports copy-friendly manual checks: selected rows, device summary, PROFINET-name, and MAC can be copied separately
- the interface passport grid exposes speed/notes columns and supports copy-friendly manual checks: selected rows copy as tab-separated text, and interface summary/IP/`MPI/DP/PN` values can be copied separately
- the passport filter area can copy all currently visible device/interface/connection rows with table headers for filtered manual review
- each passport grid context menu can copy its own currently visible rows with headers
- passport grids keep selected rows visible after focus changes and expose row tooltips for long manual-review values
- PDF network scheme references are accepted as `PDF` metadata/`Open original` sources; embedded PDF preview and OCR/import remain outside the current scope

### Phase 7. Maintenance schedule generation

Complexity: `High`

Goals:

- generate an enterprise-ready monthly maintenance schedule for equipment entered in the tree
- keep the output Excel workbook visually identical to the approved plant form
- make the first release work as a yearly accumulating workbook with one sheet per month

Main changes:

- use a template-driven Excel workflow instead of extending the legacy exchange workbook
- add a dedicated typed maintenance-planning model keyed to tree nodes
- store separate integer labor-hour norms for `ТО1`, `ТО2`, and `ТО3` where relevant
- add inventory number support to the `Lvl2` summary/card workflow
- generate/update one workbook per `workshop + year` with `12` month sheets
- keep `план` and blank `факт` rows exactly as required by the approved form
- compute monthly allocation only across working days of the Russian production calendar
- raise a warning/error when the selected month cannot fit inside the requested total hour budget

Confirmed planning rules for the first implementation:

- planning unit is a tree node
- hierarchy should follow the current assumption:
  - `Lvl2` node becomes the numbered parent row with inventory number
  - child engineering nodes become the `план/факт` detail rows
- only the `план` data is generated by the program; `факт` stays blank in the workbook for manual paper-side filling
- `ТО1` means monthly maintenance
- `ТО2` means quarterly maintenance
- `ТО3` means annual maintenance
- `ТО2` includes `ТО1`
- `ТО3` includes `ТО1` and `ТО2`
- until a formal yearly schedule source exists, `ТО2` / `ТО3` month placement should come from a deterministic per-node cycle offset that can later be replaced without redesigning the planner
- sample inconsistencies are treated as manual historical noise, not as the business rule

Recommended implementation slices:

- `Phase 7A. Domain and template foundation`
  - status: completed on `to`
  - add inventory number to `Lvl2` summary
  - define typed maintenance settings per planned node, including separate integer hour norms for `ТО1`, `ТО2`, and `ТО3`
  - prepare a cleaned internal Excel template derived from the approved sample
- `Phase 7B. Russian production calendar`
  - status: completed on `to`
  - implement reusable workday calculation for `5/2`
  - exclude Saturdays, Sundays, and official Russian non-working holidays
  - keep the calendar data replaceable by year without rewriting planner logic
- `Phase 7C. Monthly planning engine`
  - status: completed on `to`
  - select workshop, month, year, and total monthly hour budget
  - determine which nodes require `ТО1`, `ТО2`, or `ТО3`
  - allocate work across working days using the monthly workshop budget without a hard daily `<= 8` cap
- `Phase 7D. Year workbook export`
  - status: completed on `to`
  - create or update the yearly accumulating workbook
  - write only the selected month sheet while preserving the rest of the workbook
  - preserve formulas, merges, print layout, and signature blocks from the template
  - follow-up UX hardening:
    - done: move the monthly `Сформировать график ТО за месяц...` command from the per-node `График ТО` tab into the top-level `Файл` menu because the command operates on the whole workshop, not on one selected engineering node
    - done: move `Импорт норм ТО...` to the same top-level area for the same reason; it is also a workshop-wide action rather than a per-node edit
  - done: keep the monthly generation mechanism as the engine, but add a top-level yearly command above it as the main start-of-year user workflow
  - done: add a top-level recalculation command that rewrites only the selected start month through December in an existing yearly workbook
  - canonical user workflow:
    - at the start of the year, generate the whole year in one pass
    - when equipment changes during the year, recalculate only from the current month through December
    - treat past months as frozen and do not rewrite them during ordinary replanning
- `Phase 7E. Future yearly schedule source`
  - first slice status: completed on `to`
  - done: store manual per-profile 12-month `ТО1` / `ТО2` / `ТО3` placement in JSON as `YearScheduleEntries`
  - done: expose the manual annual placement in the per-node `График ТО` profile dialog
  - done: make the monthly resolver use manual placement when present and keep deterministic offsets as fallback for profiles without it
  - `Phase 7E.2` status: completed on `to`
  - done: export/import the yearly placement source through a separate `.xlsx` workbook keyed by stable `OwnerNodeId`
  - done: keep import narrow so it changes only `YearScheduleEntries`, leaving norms and production calendars untouched
  - follow-up status: in-app mass-editing grid completed on `to`
  - done: add a workshop-level grid for bulk editing `M01`..`M12` values with `ТО1`, `ТО2`, `ТО3`, or blank fallback
  - done: keep the grid narrow so it changes only `YearScheduleEntries`, leaving norms, inclusion flags, and production calendars untouched
  - follow-up status: major `ТО2` / `ТО3` split completed on `to`
  - done: split one `ТО2` / `ТО3` occurrence into assignments of up to 8 hours and distribute them across working days when possible
  - done: keep the selected monthly workshop budget as the hard constraint; do not add a hard daily total cap
- `Phase 7F. Production calendar configuration`
  - status: completed on `to`
  - done: move production-calendar year data into persisted JSON configuration while preserving built-in defaults for already supported years
  - done: add a user-facing Russian UI for viewing, adding, editing, and validating non-working transfer days by year
  - done: add JSON import support for production-calendar data, separate from the legacy Excel `v3` exchange workbook
  - done: keep the planner/export API consuming a resolved calendar service so schedule generation logic does not depend on the storage or UI mechanism
  - done: show a clear guided error when the selected year is missing, pointing the user to calendar setup/import instead of requiring a code change
- `Phase 7F.1. Production calendar PDF import`
  - status: complete on `to`; accepted after manual review; committed and pushed as `09bf84d`
  - done: import production-calendar data from PDF files such as `calendar_2027.pdf`, because JSON is not convenient for ordinary use
  - done: prefer text-layer PDF parsing first; add OCR only if real source files require it
  - done: add import preview before applying changes
  - done: keep Russian date display/input as `дд.мм.гггг`
  - done: extend the calendar model with additional working days as well as additional non-working days so transferred weekends can be represented correctly
  - on 2026-05-06, `phase7f1-production-calendar-pdf-import` passed verification build and `dotnet test` (`281/281`) using isolated output paths; user manual UI review passed

Acceptance:

- the user can choose a workshop, month, year, and hour budget and receive a ready Excel file in the approved form
- the generated workbook preserves the visual structure of the enterprise sample
- the monthly planner respects workdays only and enforces the selected monthly workshop budget
- the design stays extensible for a future externally provided yearly maintenance schedule

### Phase 11. Object templates and equipment catalog

Complexity: `High`

Status: accepted through `Phase 11G`; `Phase 11A` accepted after manual review; `Phase 11B` is complete on `to`; `Phase 11C` through `Phase 11F` are accepted after manual review and committed/pushed; `Phase 11G` is accepted after manual review and committed/pushed on `to`.

Goals:

- add a structured equipment catalog so manufacturers, series, models, equipment kinds, and typical metadata are entered consistently
- add reusable object templates so common cabinets, PLC/HMI devices, modules, switches, UPS units, and ASUTP subsystems can be created without repeated manual entry
- reduce naming drift and incomplete object creation when the knowledge base grows
- keep template application explicit and previewable; never overwrite user-entered data silently

Scope:

- `Phase 11A. Equipment catalog model`
  - status: accepted after manual review
  - add catalog item domain model
  - persist catalog items in JSON
  - normalize catalog data on load/save
  - deduplicate by stable catalog item id
  - add focused tests
- `Phase 11B. Equipment catalog UI`
  - status: complete on `to`; committed and pushed as `f80873f`
  - done: add Russian UI for listing, adding, editing, deleting, and searching catalog items
  - done: keep catalog editing separate from tree editing until object-template creation is implemented
  - on 2026-05-06, `phase11b-equipment-catalog-ui` passed verification build and `dotnet test` (`287/287`) using isolated output paths
- `Phase 11C. Object template model`
  - status: accepted after manual review
  - done: add object-template model with template nodes and generated fresh `NodeId` values on creation
  - done: support optional defaults for card fields, composition, documents/software, maintenance profile stubs, network file references, and future network interface stubs
  - done: persist templates as top-level JSON/session data and normalize them on load/save
  - on 2026-05-06, `phase11c-object-template-model` passed verification build and `dotnet test` (`292/292`) using isolated output paths
- `Phase 11D. Create from template`
  - status: accepted after manual review
  - done: create new tree objects from persisted object templates with new ids
  - done: preserve tree depth through existing attach/reindex rules
  - done: add Russian tree context-menu dialog for selecting a template and optional root-object name
  - done: append remapped composition, document/software, network-file, and maintenance-profile defaults for generated nodes
  - on 2026-05-06, `phase11d-create-from-template` passed verification build and `dotnet test` (`294/294`) using isolated output paths
- `Phase 11E. Save existing object as template`
  - status: accepted after manual review; committed and pushed on `to`
  - done: convert a well-filled existing object subtree into a reusable template
  - done: remove real `NodeId` values and owner-specific references from the saved template
  - done: remap composition, document/software, network-file, and maintenance-profile records inside the selected subtree by generated template-node ids
  - on 2026-05-06, `phase11e-save-object-as-template` passed verification build and `dotnet test` (`296/296`) using isolated output paths; manual review passed
- `Phase 11F. Apply template with preview`
  - status: accepted after manual review; committed and pushed on `to` as `ca43298`
  - done: apply a template to an existing object only after showing what will be added, skipped, or left unchanged
  - done: add missing subtree nodes and typed records without deleting existing data
  - done: fill only empty supported card fields and skip already-filled fields instead of overwriting them
  - on 2026-05-06, `phase11f-apply-template-preview` passed verification build and `dotnet test` (`299/299`) using isolated output paths
  - after manual review found mojibake in a template context-menu string, Russian template workflow strings were corrected; post-review targeted regression passed (`55/55`) and a generated mojibake scan returned `TOTAL=0`
- `Phase 11G. Template import/export`
  - status: accepted after manual review; committed/pushed on `to` as `268b550`
  - done: exchange catalog/templates through dedicated UTF-8 JSON files
  - done: keep this separate from legacy Excel `v3`
  - done: import through a safe merge where existing catalog/template ids and catalog semantic duplicates are not overwritten
  - on 2026-05-06, `phase11g-template-import-export` passed verification build and `dotnet test` (`302/302`) using isolated output paths
  - after manual review found that `Состав шаблона` was too narrow in the create-object-from-template dialog, the dialog layout was corrected and `phase11g-template-import-export-layout-fix` passed verification build and `dotnet test` (`302/302`) using isolated output paths
  - after manual review found an empty preview with an inactive `Применить` button in the apply-object-template dialog, the dialog now selects the first template explicitly, rebuilds the preview when shown, displays no-change/failure text, and `phase11g-template-import-export-apply-preview-ui-fix` passed verification build and `dotnet test` (`302/302`) using isolated output paths

Acceptance:

- the equipment catalog is persisted in JSON and survives normalization without duplicates
- the user can maintain equipment catalog records in Russian UI
- object templates can create new objects with fresh ids
- applying templates uses preview and does not silently overwrite existing data
- templates/catalog data can be exported and imported as JSON
- tests cover normalization, object creation from templates, and saving existing object subtrees as templates

### Phase 12. SQLite storage, backups, snapshots, and change history

Complexity: `High`

Status: accepted through `Phase 12S8. Change history`; committed/pushed on `to` as `27a2aba`.

Accepted storage status:

- `Phase 12A. Automatic JSON snapshots before save`
  - status: verified local prototype, paused before commit while storage moves to SQLite
  - on 2026-05-06, targeted `JsonStorageServiceTests|KnowledgeBaseSnapshotServiceTests` passed (`12/12`)
  - on 2026-05-06, `phase12a-automatic-json-snapshots` passed verification build and `dotnet test` (`306/306`) using isolated output paths
- `Phase 12B. Manual JSON snapshots with note`
  - status: verified local prototype, paused before commit while storage moves to SQLite
  - on 2026-05-07, targeted `JsonStorageServiceTests|KnowledgeBaseSnapshotServiceTests|KnowledgeBaseFileWorkflowServiceTests` passed (`22/22`)
  - on 2026-05-07, `phase12b-manual-json-snapshots` passed verification build and `dotnet test` (`309/309`) using isolated output paths
- `Phase 12C. Snapshot browser`
  - status: verified local prototype, paused before commit while storage moves to SQLite
  - on 2026-05-07, targeted `JsonStorageServiceTests|KnowledgeBaseSnapshotServiceTests|KnowledgeBaseFileWorkflowServiceTests` passed (`25/25`)
  - on 2026-05-07, `phase12c-snapshot-browser` passed verification build and `dotnet test` (`312/312`) using isolated output paths
- `Phase 12S0. SQLite single-file storage redesign plan`
  - status: approved on 2026-05-07 with choices `1A, 2B, 3A, 4A`
  - plan document: `docs/sqlite-storage-plan.md`
- `Phase 12S1. Storage abstraction`
  - status: accepted and committed/pushed on `to` as part of `27a2aba`
  - added `IKnowledgeBaseStorageService`, `KnowledgeBaseStorageLoadResult`, and `KnowledgeBaseStorageServiceFactory`
  - `JsonStorageService` implements the storage interface while preserving the existing JSON load/save behavior
  - `KnowledgeBaseFileWorkflowService` now depends on the storage interface instead of the concrete JSON service
  - `Forms` no longer creates `JsonStorageService` directly
  - on 2026-05-07, targeted storage/file-workflow/snapshot tests passed (`27/27`)
  - on 2026-05-07, `phase12s1-storage-abstraction` passed verification build and `dotnet test` (`314/314`)
- `Phase 12S2. SQLite schema and repository`
  - status: accepted and committed/pushed on `to` as part of `27a2aba`
  - added `Microsoft.Data.Sqlite` `8.0.13` to the core project
  - added `KnowledgeBaseSqliteConnectionFactory` with foreign keys enabled, rollback journal mode, and pooling disabled for predictable single-file handling
  - added `SqliteKnowledgeBaseStorageService` as an alternative `IKnowledgeBaseStorageService` implementation
  - initial schema version `1` created normalized tables for metadata, config, production calendars, workshops, tree nodes, typed records, maintenance profiles/year entries, catalog items/properties, object templates, and template nodes; the accepted SQLite stack is now database schema version `4`
  - SQLite save/load round-trips a normalized `SavedData`; UI switching happens in `Phase 12S4`
  - on 2026-05-07, targeted SQLite storage tests passed (`3/3`)
  - on 2026-05-07, targeted storage/file-workflow/snapshot tests passed (`30/30`)
  - on 2026-05-07, `phase12s2-sqlite-schema-repository` passed verification build and `dotnet test` (`317/317`)
- `Phase 12S3. First-launch JSON migration`
  - status: accepted and committed/pushed on `to` as part of `27a2aba`
  - offers migration from `Мои документы\ASUTP_KnowledgeBase.json` to `%LocalAppData%\AKB5\knowledge-base.akb` only when no `.akb` exists and legacy JSON is present
  - requires user confirmation, leaves the JSON source untouched, records migration metadata, and writes a post-migration JSON safety export next to the `.akb`
  - on 2026-05-07, `phase12s3-first-launch-json-migration` passed verification build and `dotnet test` (`322/322`)
- `Phase 12S4. Database file UX`
  - status: accepted and committed/pushed on `to` as part of `27a2aba`
  - switches the default live path to `.akb`, routes `.json` paths to legacy JSON storage and `.akb` paths to SQLite storage, updates database dialogs to `.akb`, and adds full database JSON import/export
  - on 2026-05-07, `phase12s4-database-file-ux-json-compatibility` passed verification build and `dotnet test` (`325/325`)
- `Phase 12S5. SQLite backups and snapshots`
  - status: accepted and committed/pushed on `to` as part of `27a2aba`
  - stores SQLite-backed snapshots inside the `.akb` database with metadata and normalized `SavedData` payloads; legacy JSON still uses `.akb-snapshots`
  - on 2026-05-07, `phase12s5-sqlite-snapshots` passed verification build and `dotnet test` (`328/328`)
- `Phase 12S6. Restore selected snapshot`
  - status: accepted and committed/pushed on `to` as part of `27a2aba`
  - restores selected SQLite snapshots only after confirmation, creates a protective `before-restore` snapshot, reloads restored data into the UI, and preserves current data on failed restore
  - on 2026-05-07, `phase12s6-snapshot-restore` passed verification build and `dotnet test` (`330/330`)
- `Phase 12S7. Snapshot comparison`
  - status: accepted and committed/pushed on `to` as part of `27a2aba`
  - compares two snapshots at summary level across high-value data areas before restore/audit decisions
  - on 2026-05-07, `phase12s7-snapshot-comparison` passed verification build and `dotnet test` (`332/332`)
- `Phase 12S8. Change history`
  - status: accepted after manual review; committed/pushed on `to` as `27a2aba`
  - writes SQLite change-history entries for save, migration, manual snapshot, restore, and catalog/template import, and exposes the read-only history through `Файл -> Снимки и история базы...` for `.akb` databases
  - on 2026-05-07, `phase12s8-change-history` passed verification build and `dotnet test` (`333/333`)

Storage decision:

- use one SQLite database file as the live application source of truth
- previous proposed default path: `%LocalAppData%\AKB5\knowledge-base.akb`; superseded by the portable-first default in the approved 2026-05-12 follow-up
- use `.akb` as the visible database extension
- show a confirmation dialog before first-launch migration
- create an automatic post-migration JSON safety export next to the new `.akb`
- do not support simultaneous multi-user editing in the first SQLite version
- do not use `Мои документы` as the default live database location
- keep the legacy `Мои документы\ASUTP_KnowledgeBase.json` file unchanged during migration
- keep JSON as a full database import/export and first-launch migration compatibility format
- keep catalog/template JSON exchange separate from full database JSON import/export
- keep Excel workbook `v3` as a legacy exchange layer, not as the main storage direction

Goals:

- replace whole-file JSON persistence with transactional SQLite writes
- preserve existing user data through first-launch migration from JSON
- keep a convenient single-file database for copy/backup/support workflows
- make important changes reviewable through SQLite-aware snapshots and history
- provide a practical restore path before larger multi-user or role-based workflows are considered

Accepted implementation slices:

- `Phase 12S1. Storage abstraction`
  - status: accepted and committed/pushed on `to`
  - introduce an app-facing storage interface that loads and saves `SavedData`
  - adapt current JSON storage behind that interface without behavior changes
  - acceptance: UI/file workflow no longer depends directly on `JsonStorageService`
- `Phase 12S2. SQLite schema and repository`
  - status: accepted and committed/pushed on `to`
  - add SQLite dependency, connection factory, schema versioning, and normalized tables
  - implement SQLite load/save round trip through `SavedData`
  - acceptance: full normalized `SavedData` survives save/load round trip
- `Phase 12S3. First-launch JSON migration`
  - status: accepted and committed/pushed on `to`
  - offer migration from `Мои документы\ASUTP_KnowledgeBase.json` when no SQLite database exists
  - show a confirmation dialog before migration
  - leave the JSON source file untouched and report migration status
  - create an automatic post-migration JSON safety export next to the new `.akb`
  - acceptance: existing user data appears after first launch without manual import and migration never starts before confirmation
- `Phase 12S4. Database file UX`
  - status: accepted and committed/pushed on `to`
  - switch default live database path to SQLite
  - update open/save dialogs to `.akb`
  - add explicit full JSON import/export compatibility commands
  - acceptance: ordinary users work with `.akb`, support can still import/export full JSON
- `Phase 12S5. SQLite backups and snapshots`
  - status: accepted and committed/pushed on `to`
  - replace `.akb-snapshots` JSON workflow with SQLite-aware snapshots
  - store note, kind, source database path, timestamp, size, and snapshot payload
  - acceptance: manual/protective snapshots work from SQLite
- `Phase 12S6. Restore selected snapshot`
  - status: accepted and committed/pushed on `to`
  - restore only after explicit confirmation and a pre-restore protective snapshot
  - acceptance: failed restore leaves the current database intact
- `Phase 12S7. Snapshot comparison`
  - status: accepted and committed/pushed on `to`
  - compare two snapshots at summary level across high-value data areas
  - acceptance: added/removed/changed areas are visible before restore/audit work
- `Phase 12S8. Change history`
  - status: accepted after manual review; committed/pushed on `to`
  - log save, import, migration, manual snapshot, restore, and catalog/template import actions
  - acceptance: high-value storage actions are visible after the fact

Acceptance:

- the app no longer uses `Мои документы\ASUTP_KnowledgeBase.json` as the default live database
- first launch can migrate existing JSON data into SQLite after confirmation without modifying the JSON file
- JSON full database import/export remains available
- SQLite save/load is transactional and covered by round-trip tests
- users can create, browse, restore, and compare snapshots from SQLite-backed storage
- restore never happens without explicit confirmation
- important storage/import/restore actions are visible in change history

### Optional future phase. Interactive network topology

Complexity: `Very High`

Not part of the first roadmap wave.

Only consider after:

- `NodeId` and typed data are stable
- file-based network tab proves insufficient
- there is a clear data model for nodes, ports, links, coordinates, and interaction rules

## Risk register

### Low to medium risks

- removing level setup from UI
- hiding level names from the main UX
- defaulting hidden `MaxLevels` to `10`

### Medium risks

- refactoring the right panel into a screen host
- keeping legacy Excel `v3` while typed features grow
- first file-based network preview if users expect embedded PDF immediately

### High risks

- introducing persistent `NodeId` into JSON/domain and preserving it across all workflows
- replacing level-driven logic with `NodeType`/capabilities consistently
- introducing composition without corrupting current tree workflows
- migration from legacy note/module/deep-tree usage into dedicated typed data

## Testing strategy by phase

Always add tests in the same phase as the feature.

Minimum required coverage:

- JSON load/save migration tests
- tree mutation tests with persistent IDs
- copy/paste/move/reindex tests after `NodeType` introduction
- form-state tests for screen resolution
- composition ordering tests
- search tests for each scope
- exchange tests proving `v3` remains readable
- maintenance planner tests for workday filtering, month-budget overflow behavior, and yearly type-placement rules
- production-calendar configuration tests for JSON round-trip, UI/import validation rules, and planner behavior when a year is missing or configured
- template-export tests proving generated workbooks preserve required structure

Manual UI checks will still be required for:

- screen switching by node type
- tab visibility rules
- large file/image preview behavior
- real data density in cabinet composition
- generated monthly maintenance workbook compared against the approved sample

## Recommended implementation order for the next coding sessions

Completed on `to`:

1. Phase 0
2. Phase 1
3. Phase 2
4. Phase 3
5. Phase 3B
6. Phase 4
7. Phase 5
8. Phase 6
9. Phase 7A foundation
10. Phase 7B. Russian production calendar
11. Phase 7C. Monthly planning engine
12. Phase 7D. Year workbook export
13. Phase 7E. Future yearly schedule source, first slice complete on `to`
14. Phase 7E.2 source exchange, complete on `to`
15. Phase 7E in-app mass-editing grid, complete on `to`
16. Major `ТО2` / `ТО3` split across multiple working days, complete on `to`
17. Maintenance-norm import coverage and mismatch reporting, complete on `to`
18. Phase 7F production-calendar configuration, complete on `to`
19. Phase 7F.1 PDF calendar import, complete on `to`
20. Phase 11B. Equipment catalog UI, complete on `to`
21. Phase 11C. Object template model, accepted after manual review
22. Phase 11D. Create from template, accepted after manual review
23. Phase 11E. Save existing object as template, accepted after manual review
24. Phase 11F. Apply template with preview, accepted after manual review
25. Phase 11G. Template import/export, accepted after manual review
26. Phase 12S1. Storage abstraction, accepted and committed/pushed on `to`
27. Phase 12S2. SQLite schema/repository, accepted and committed/pushed on `to`
28. Phase 12S3. First-launch JSON migration, accepted and committed/pushed on `to`
29. Phase 12S4. Database file UX, accepted and committed/pushed on `to`
30. Phase 12S5. SQLite snapshots, accepted and committed/pushed on `to`
31. Phase 12S6. Restore selected snapshot, accepted and committed/pushed on `to`
32. Phase 12S7. Snapshot comparison, accepted and committed/pushed on `to`
33. Phase 12S8. Change history, accepted and committed/pushed on `to`
34. `phase7e-annual-norm-import`, accepted after manual review
35. `phase7g-annual-norm-hidden-rows`, committed/pushed on `to`
36. Menu rework first iteration, committed/pushed on `to`

Approved next:

1. Review and accept portable-first storage and external `.akb` backups.

Not active:

1. Phase 8 through Phase 10 were discussed as possible directions but are not currently selected for implementation

## AI handoff / next-dialog instructions

When a new AI session starts, read in this exact order:

1. `AGENTS.md`
2. `docs/codex-handoff.md`
3. `Roadmap.md`

Then continue from the next explicitly prioritized task only. If no task is prioritized, do not invent a new phase.

Recommended prompt for the next AI session:

```text
Read AGENTS.md, docs/codex-handoff.md, and Roadmap.md.
We are on branch to.
Continue implementation only from the next explicitly prioritized roadmap task.
If Roadmap.md says no next coding phase is prioritized, stop after reporting the current state.
Do not redesign the roadmap unless you find a concrete technical contradiction in the codebase.
Keep JSON import/export and first-launch migration compatibility and treat Excel v3 as a legacy transition layer.
Phase 12S8 is accepted and committed/pushed on to. phase7e-annual-norm-import is accepted after manual review. phase7g-annual-norm-hidden-rows is committed/pushed on to. Menu rework first iteration is committed/pushed on to. Portable-first storage follow-up is locally verified and awaiting review; do not start another coding phase until it is accepted.
```

## Immediate next step

Continue from the next explicitly prioritized task:

- preserve the completed `Phase 7A` / `7B` / `7C` / `7D` / `7E` / `7F` / `7F.1` workflow as the current baseline
- review and accept portable-first storage and external `.akb` backups before starting a new roadmap phase
- treat future-month recalculation as completed `Phase 7D` orchestration/workflow built on top of the existing monthly engine, not as a replacement for it
- keep workbook `v3` readable as legacy, but do not expand it as the main feature direction
- keep SQLite single-file storage, JSON import/export and first-launch migration compatibility, and Russian-only UI intact during future work
