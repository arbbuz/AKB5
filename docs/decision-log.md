# Decision Log

Last updated: `2026-04-28`

## 2026-04-28

- `Phase 5` is considered implemented on `interface`; the next roadmap phase is `Phase 6`
- Search is now a typed domain workflow with fixed scopes: `All`, `Tree`, `Card`, `Composition`, and `Docs/Software`
- Search navigation must continue to resolve to the owning tree node and may switch the workspace to the preferred tab for the matched domain
- User-facing program UI should use Russian only going forward
- `Phase 5` plus the subsequent UI-localization pass were validated by build/test on `2026-04-28`; tests are now `171/171`
- `Documentation and Software` remains intentionally separate from `Composition`; it uses dedicated link catalogs for schemes, instructions, and software folders
- The user-facing software workflow records `AddedAt`; legacy software timestamps/notes remain compatibility-only persistence fields

## 2026-04-27

- `docs/codex-handoff.md` remains the single current-state file for session startup and handoff
- The knowledge harness uses a fixed set of files:
  - `docs/codex-handoff.md`
  - `docs/plans.md`
  - `docs/lessons-learned.md`
  - `docs/decision-log.md`
- On the explicit command `РґРёСЃС‚РёР»Р»РёСЂСѓР№ Р·РЅР°РЅРёСЏ РёР· СЃРµСЃСЃРёРё`, update the fixed harness files in place
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
