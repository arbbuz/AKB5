# Decision Log

Last updated: `2026-04-28`

## 2026-04-28

- `Phase 5` is considered implemented on `interface`
- Search is now a typed domain workflow with fixed scopes: `All`, `Tree`, `Card`, `Composition`, and `Docs/Software`
- Search navigation must continue to resolve to the owning tree node and may switch the workspace to the preferred tab for the matched domain
- User-facing program UI should use Russian only going forward
- `Phase 5` plus the subsequent UI-localization pass were validated by build/test on `2026-04-28`; tests are now `171/171`
- `Documentation and Software` remains intentionally separate from `Composition`; it uses dedicated link catalogs for schemes, instructions, and software folders
- The user-facing software workflow records `AddedAt`; legacy software timestamps/notes remain compatibility-only persistence fields
- `Phase 6` is considered implemented on `interface`; the next roadmap phase is `Phase 7`
- `Phase 6` stores file-based network context in top-level `NetworkFileReferences` keyed by `OwnerNodeId`
- `Phase 6` keeps embedded preview limited to image formats already supported by the in-form workflow; non-image files stay metadata-only with `Open original`
- `Phase 6` uses separate `Файлы` and `Предпросмотр` tabs instead of a permanently split list/preview layout
- Loading a node must open `Файлы` by default; automatic switching to `Предпросмотр` is not part of the accepted UX
- `Phase 6` validation after the final UX fixes reached `177/177`, and `asutpKB.exe` startup was rechecked
- The old `Phase 7` Excel/exchange-modernization direction is superseded by maintenance-schedule generation
- The first `Phase 7` release should produce one yearly accumulating workbook per workshop with `12` month sheets based on the approved enterprise form
- Maintenance planning is now defined by business rule, not by historical quirks in the hand-filled sample workbook
- Current fixed maintenance rules are:
  - `ТО1` = monthly
  - `ТО2` = semiannual
  - `ТО3` = annual
- The planning hierarchy is fixed as:
  - `Lvl2` node = numbered parent row with inventory number
  - child engineering nodes = `план/факт` detail rows
- The application generates only `план`; `факт` stays blank in the workbook for manual paper-side filling
- Planned nodes will require separate integer hour norms for `ТО1`, `ТО2`, and `ТО3`
- Until a formal yearly schedule source is provided, `ТО2` / `ТО3` placement should come from a deterministic per-node cycle offset

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
