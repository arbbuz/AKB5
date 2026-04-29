# Decision Log

Last updated: `2026-04-29`

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

## 2026-04-27

- `docs/codex-handoff.md` remains the single current-state file for session startup and handoff
- The knowledge harness uses a fixed set of files:
  - `docs/codex-handoff.md`
  - `docs/plans.md`
  - `docs/lessons-learned.md`
  - `docs/decision-log.md`
- On the explicit user request to distill session knowledge, update the fixed harness files in place
- Do not create parallel session notes, duplicate summaries, or stale side documents for the same purpose
- Use agreed tree taxonomy:
  - `L1` = department
  - `L2` = system
  - `L3` = cabinet
- Keep `summary.md` only as a redirect into the `docs/` harness, not as a parallel knowledge store
- `Phase 3B` templates are implemented as a built-in code catalog; do not change JSON schema or Excel `v3` just to store templates unless that becomes an explicit task
- `copy composition from existing object` currently copies only typed `CompositionEntries` and only between same-type nodes
- `Phase 4` documentation/software data is stored as top-level `DocumentLinks` and `SoftwareRecords` collections keyed by `OwnerNodeId`
- `Phase 4` keeps JSON schema version at `3` and leaves the Excel workbook contract at `v3`
- `Documentation and Software` UI support is currently limited to `Cabinet`, `Device`, `Controller`, and `Module`
- Deleting a node must remove typed composition, document-link, and software-record data for the whole deleted subtree
