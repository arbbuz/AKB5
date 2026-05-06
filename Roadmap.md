# Roadmap

Last updated: 2026-05-06
Branch baseline: `to`
Implementation status: `Phase 0 complete on to, Phase 1 complete on to, Phase 2 complete on to, Phase 3 complete on to, Phase 3B complete on to, Phase 4 complete on to, Phase 5 complete on to, Phase 6 complete on to, Phase 7A complete on to, Phase 7B complete on to, Phase 7C complete on to, Phase 7D complete on to, Phase 7E first slice complete on to, Phase 7E.2 source exchange complete on to, Phase 7E mass-editing grid complete on to, major ТО2/ТО3 split complete on to, norm import coverage complete on to, Phase 7F production-calendar configuration complete on to, Phase 7F.1 PDF calendar import complete on to, Phase 11A accepted, production-calendar Russian date format accepted, local Phase 11B equipment catalog UI verified and waiting manual review`

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
## Non-negotiable architecture rules

1. `NodeType` must become more important than `LevelIndex`.
2. No new right-panel behavior may depend only on `LevelIndex`.
3. New cross-links must never rely on node names or paths.
4. A persistent `NodeId` must exist in the domain model and JSON before composition/doc/network features are built.
5. Do not store all future data in one bloated `KbNodeDetails` object.
6. Do not overload the left tree with composition or network data just to avoid creating proper models.
7. Excel `v3` compatibility must be preserved during the transition, but new feature investment should prefer report/template workflows over broader bidirectional workbook exchange.

## Current technical reality

- JSON is still the source of truth.
- Current JSON schema version is `3`.
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
- On 2026-04-28, the current `Phase 6` worktree passed verification build, passed `dotnet test` (`177/177`), and `asutpKB.exe` startup was rechecked after the final `Network` UX fixes.
- Current Excel `v3` now preserves `NodeId` after import and writes/reads a read-only `NodeType` column as part of the transition, but further workbook modernization is no longer the preferred next phase.
- Current CI workflow also verifies `dotnet format --verify-no-changes` for the app project, core project, and tests before `build` / `test`.
- The maintenance-schedule generation roadmap through `Phase 7F.1` is complete on `to`; local `Phase 11B` equipment catalog UI is verified and waiting for manual review.
- `Phase 7A` is complete on `to`: `Lvl2` inventory number support now follows visible hierarchy level, typed `MaintenanceScheduleProfiles` are persisted in JSON/session state, and engineering nodes expose a `График ТО` tab with per-node `ТО1` / `ТО2` / `ТО3` hour norms.
- `Phase 7B` is complete on `to`: Russian production-calendar calculation for `5/2` workdays is available as a reusable service.
- `Phase 7F` is complete on `to`: production-calendar years are persisted in JSON config, editable from the Russian UI, importable from JSON, and consumed by maintenance schedule generation.
- `Phase 7C` is complete on `to`: the resolver and monthly planner generate month demand from `ТО1` / `ТО2` / `ТО3` norms and compare it against the selected monthly workshop budget.
- `Phase 7D` is complete on `to`: the yearly workbook export is template-driven, exposed in the UI, and can also import maintenance norms from `123.xlsx`.
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

Status: approved; `Phase 11A` accepted after manual review; local `Phase 11B` is verified and waiting for manual review.

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
  - status: local implementation verified; waiting manual review
  - done locally: add Russian UI for listing, adding, editing, deleting, and searching catalog items
  - done locally: keep catalog editing separate from tree editing until object-template creation is implemented
  - on 2026-05-06, `phase11b-equipment-catalog-ui` passed verification build and `dotnet test` (`287/287`) using isolated output paths
- `Phase 11C. Object template model`
  - add object-template model with template nodes and generated fresh `NodeId` values on creation
  - support optional defaults for card fields, composition, documents/software, maintenance profile stubs, and future network interface stubs
- `Phase 11D. Create from template`
  - create new tree objects from templates with new ids
  - preserve tree depth and visible-level rules
- `Phase 11E. Save existing object as template`
  - convert a well-filled existing object subtree into a reusable template
  - remove real `NodeId` values and owner-specific references from the saved template
- `Phase 11F. Apply template with preview`
  - apply a template to an existing object only after showing what will be added, skipped, or left unchanged
  - do not delete or overwrite user data without explicit confirmation
- `Phase 11G. Template import/export`
  - exchange catalog/templates through dedicated JSON files
  - keep this separate from legacy Excel `v3`

Acceptance:

- the equipment catalog is persisted in JSON and survives normalization without duplicates
- the user can maintain equipment catalog records in Russian UI
- object templates can create new objects with fresh ids
- applying templates uses preview and does not silently overwrite existing data
- templates/catalog data can be exported and imported as JSON
- tests cover normalization and object creation from templates

### Phase 12. Backup, snapshots, and change history

Complexity: `Medium-High`

Status: approved after Phase 11.

Goals:

- protect the JSON source of truth from accidental loss or destructive edits
- make important changes reviewable through snapshots and history
- provide a practical restore path before larger multi-user or role-based workflows are considered

Scope:

- automatic timestamped JSON snapshots before destructive operations and save operations
- manual snapshot creation with user note
- snapshot browser with date, source file, size, and note
- restore selected snapshot after confirmation
- compare two snapshots at summary level: workshops, nodes, documents, software, network files, maintenance profiles, production calendars, catalog/template records
- lightweight change history for high-value actions

Acceptance:

- users can create and restore snapshots from the UI
- the app creates protective snapshots before risky operations
- snapshot restore never happens without explicit confirmation
- snapshot comparison reports what changed at a useful summary level
- snapshot files remain separate from the main JSON and from Excel exchange workbooks

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

Local verified, waiting manual review:

1. Phase 11B. Equipment catalog UI

Approved after manual review and commit/push:

1. Phase 11C. Object template model
2. Remaining Phase 11 object-template/catalog slices
3. Phase 12. Backup, snapshots, and change history

Not active:

1. No `Phase 7G` exists in this roadmap
2. Phase 8 through Phase 10 were discussed as possible directions but are not currently selected for implementation

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
Keep JSON source-of-truth compatibility and treat Excel v3 as a legacy transition layer.
```

## Immediate next step

Continue from the next explicitly prioritized task:

- preserve the completed `Phase 7A` / `7B` / `7C` / `7D` / `7E` / `7F` / `7F.1` workflow as the current baseline
- manually review local `Phase 11B. Equipment catalog UI`
- after accepting `Phase 11B`, commit/push it and continue to `Phase 11C. Object template model`, unless explicitly redirected
- do not start a new `Phase 7G`; it is not part of this roadmap
- treat future-month recalculation as completed `Phase 7D` orchestration/workflow built on top of the existing monthly engine, not as a replacement for it
- keep workbook `v3` readable as legacy, but do not expand it as the main feature direction
- keep JSON source-of-truth compatibility and preserve Russian-only UI
