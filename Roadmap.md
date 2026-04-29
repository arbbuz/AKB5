# Roadmap

Last updated: 2026-04-29
Branch baseline: `to`
Implementation status: `Phase 0 complete on to, Phase 1 complete on to, Phase 2 complete on to, Phase 3 complete on to, Phase 3B complete on to, Phase 4 complete on to, Phase 5 complete on to, Phase 6 complete on to, Phase 7A complete on to, Phase 7B complete on to, Phase 7C complete on to, Phase 7D complete on to, next unfinished slice is Phase 7E`

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
- The next roadmap phase is now maintenance-schedule generation, not typed-data workbook redesign.
- `Phase 7A` is complete on `to`: `Lvl2` inventory number support now follows visible hierarchy level, typed `MaintenanceScheduleProfiles` are persisted in JSON/session state, and engineering nodes expose a `График ТО` tab with per-node `ТО1` / `ТО2` / `ТО3` hour norms.
- `Phase 7B` is complete on `to`: Russian production-calendar calculation for `5/2` workdays is available as a reusable service.
- `Phase 7C` is complete on `to`: the resolver and monthly planner generate month demand from `ТО1` / `ТО2` / `ТО3` norms and compare it against the selected monthly workshop budget.
- `Phase 7D` is complete on `to`: the yearly workbook export is template-driven, exposed in the UI, and can also import maintenance norms from `123.xlsx`.
- On 2026-04-29, the current `Phase 7` worktree passed verification build and passed `dotnet test` (`243/243`) using isolated output paths.

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
- `Phase 7E. Future yearly schedule source`
  - keep the monthly planner extensible so a later yearly schedule can become the source of `ТО1/ТО2/ТО3` placement without redesigning the whole phase

Acceptance:

- the user can choose a workshop, month, year, and hour budget and receive a ready Excel file in the approved form
- the generated workbook preserves the visual structure of the enterprise sample
- the monthly planner respects workdays only and enforces the selected monthly workshop budget
- the design stays extensible for a future externally provided yearly maintenance schedule

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

Remaining:

1. Phase 7E. Future yearly schedule source

## AI handoff / next-dialog instructions

When a new AI session starts, read in this exact order:

1. `AGENTS.md`
2. `docs/codex-handoff.md`
3. `Roadmap.md`

Then continue from the next unfinished phase only.

Recommended prompt for the next AI session:

```text
Read AGENTS.md, docs/codex-handoff.md, and Roadmap.md.
We are on branch to.
Continue implementation from the next unfinished roadmap phase.
Do not redesign the roadmap unless you find a concrete technical contradiction in the codebase.
Keep JSON source-of-truth compatibility and treat Excel v3 as a legacy transition layer.
```

## Immediate next step

Continue Phase 7:

- preserve the completed `Phase 7A` / `7B` / `7C` / `7D` workflow as the current baseline
- decide whether to add split-across-days support for one `ТО2` / `ТО3` occurrence
- decide when to replace deterministic `ТО2` / `ТО3` month placement with a formal yearly schedule source in `Phase 7E`
- keep workbook `v3` readable as legacy, but do not expand it as the main feature direction
- keep JSON source-of-truth compatibility and preserve Russian-only UI
