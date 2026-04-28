# Decision Log

Last updated: `2026-04-28`

## 2026-04-28

- `Phase 4` is considered implemented on `interface`; the next roadmap phase is `Phase 5`
- The `Documentation and Software` screen is intentionally not modeled like `Composition`; it uses separate typed link catalogs for schemes, instructions, and software folders
- The user-facing software workflow records `AddedAt`; legacy software timestamps/notes remain compatibility-only persistence fields
- Manual verification on `2026-04-28` found no obvious bugs in the visible `Phase 4` workflow

## 2026-04-27

- `docs/codex-handoff.md` remains the single current-state file for session startup and handoff
- The knowledge harness uses a fixed set of files:
  - `docs/codex-handoff.md`
  - `docs/plans.md`
  - `docs/lessons-learned.md`
  - `docs/decision-log.md`
- On the explicit command `дистиллируй знания из сессии`, update the fixed harness files in place
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
