# SQLite Storage Plan

Last updated: `2026-05-07`

## Decision

The approved target storage direction is:

- use a single SQLite database file as the application source of truth
- keep JSON as an import/export and first-launch migration compatibility format
- migrate the existing legacy JSON database on first launch when no SQLite database exists yet
- keep Excel workbook `v3` as a legacy exchange layer, not as the primary persistence format

Approved implementation choices:

```text
1A, 2B, 3A, 4A
```

- visible database extension: `.akb`
- first-launch migration UX: confirmation dialog before migration
- post-migration safety export: automatic JSON export next to the new `.akb`
- multi-user policy: simultaneous multi-user editing is unsupported in the first SQLite version

The default user database should move out of `Мои документы`. The proposed default path is:

```text
%LocalAppData%\AKB5\knowledge-base.akb
```

The user must still be able to open another database file explicitly.

## Why This Direction

- SQLite gives transactional writes instead of rewriting the whole JSON file.
- A single `.akb` file remains easy to copy, back up, and move between machines.
- The current `SavedData` aggregate can be used as the migration and compatibility boundary.
- JSON remains available for support, export, and emergency recovery without being the live source of truth.
- Future snapshots, restore, comparison, and change history can be stored in the same database with clear metadata.

## Storage Rules

- Use one primary database file with visible extension `.akb`.
- Do not use `Мои документы` as the default live database location.
- Keep the old `ASUTP_KnowledgeBase.json` file untouched during first-launch migration.
- Use SQLite transactions for all multi-table saves and imports.
- Enable `PRAGMA foreign_keys=ON`.
- Prefer rollback-journal mode initially to keep the live storage effectively single-file at rest; revisit WAL only if performance or concurrency requires it.
- Use SQLite backup APIs or a closed-connection copy for database backups, not ad hoc copying of an open database.
- Keep JSON export deterministic and readable for support.

## Proposed Schema Areas

- `app_metadata`: schema version, created/updated timestamps, last selected workshop
- `config`: application configuration and production-calendar settings
- `workshops`: workshop records
- `nodes`: physical tree nodes keyed by stable `NodeId`
- `composition_entries`: typed composition records keyed by owner node
- `document_links`: scheme/manual/instruction links keyed by owner node
- `software_records`: software records keyed by owner node
- `network_file_references`: network files keyed by owner node
- `maintenance_schedule_profiles`: maintenance profiles keyed by owner node
- `maintenance_year_schedule_entries`: optional month placement source
- `equipment_catalog_items` and `equipment_catalog_properties`
- `object_templates` and `object_template_nodes`
- `snapshots`: snapshot metadata and serialized snapshot payload
- `change_log`: high-value user-visible actions

The first implementation may keep a `SavedData` JSON payload in snapshots and migration tests even when live tables are normalized.

## Implementation Slices

### Phase 12S0. Storage Redesign Plan

Status: approved on `2026-05-07`.

- document the SQLite single-file decision
- pause further JSON-specific snapshot restore work
- keep local JSON snapshot prototype work paused before commit while implementation moves to SQLite

### Phase 12S1. Storage Abstraction

Status: implemented locally on `2026-05-07`; waiting for manual review.

- introduce an app-facing storage interface that loads and saves `SavedData`
- adapt the current JSON storage behind that interface without behavior changes
- keep existing tests passing
- no SQLite dependency in this slice

Acceptance:

- app still works from JSON through the abstraction
- `JsonStorageService` is no longer directly required by UI/file workflow code

### Phase 12S2. SQLite Schema and Repository

Status: implemented locally on `2026-05-07`; waiting for manual review.

- add SQLite dependency and connection factory
- create schema version `1` for the normalized tables
- implement SQLite load/save round trip through `SavedData`
- add focused tests for full `SavedData` round trip and schema creation

Acceptance:

- saving to SQLite and loading back produces the same normalized `SavedData`
- all core domain collections survive the round trip

### Phase 12S3. First-Launch JSON Migration

- on startup, if `%LocalAppData%\AKB5\knowledge-base.akb` is missing and `Мои документы\ASUTP_KnowledgeBase.json` exists, offer migration from the legacy JSON file
- show a confirmation dialog before migration
- leave the JSON file unchanged
- create an automatic post-migration JSON safety export next to the new `.akb`
- record migration status and source path
- show a concise Russian status/message when migration succeeds or fails

Acceptance:

- existing user data appears after first launch without manual import
- migration failure does not delete or modify the legacy JSON
- migration never starts before the user confirms it
- a readable post-migration JSON safety export is created after successful migration

### Phase 12S4. Database File UX

- change default live database path to SQLite
- update `Открыть базу...` / `Сохранить как...` filters for `.akb`
- add explicit JSON import/export commands for full database compatibility
- keep catalog/template JSON exchange separate from full database JSON import/export

Acceptance:

- ordinary users see `.akb` as the database file
- support users can still export/import full JSON when needed

### Phase 12S5. SQLite Backups and Snapshots

- replace JSON `.akb-snapshots` live workflow with SQLite-aware snapshots
- store manual snapshot note, created time, kind, source database path, size, and payload
- create protective snapshots before risky operations
- keep a database backup path for catastrophic recovery

Acceptance:

- manual snapshots work from SQLite
- protective snapshots are created before destructive operations
- snapshots are visible without depending on JSON sidecar files

### Phase 12S6. Restore Selected Snapshot

- restore only after explicit confirmation
- create a pre-restore protective snapshot
- validate restored data before applying it
- reload the UI from the restored database state

Acceptance:

- restore never happens without confirmation
- failed restore leaves the current database intact

### Phase 12S7. Snapshot Comparison

- compare two snapshots at summary level
- cover workshops, nodes, document links, software records, network files, maintenance profiles, production calendars, catalog records, and object templates

Acceptance:

- the user sees a useful summary of added/removed/changed areas before restore or audit work

### Phase 12S8. Change History

- write a lightweight `change_log` record for high-value actions:
  - save
  - import
  - migration
  - manual snapshot
  - restore
  - catalog/template import
- expose a simple read-only history view when useful

Acceptance:

- important storage and import actions are visible after the fact

## Compatibility

- Existing JSON schema version `3` remains readable for migration and full JSON import.
- Full JSON export should keep the current `SavedData` shape unless a future schema migration is explicitly approved.
- Excel workbook `v3` remains readable as a legacy exchange format.
- Catalog/template JSON exchange remains a separate focused exchange format.

## Approved Test Answers

Approved answer:

```text
1A, 2B, 3A, 4A
```

The options are retained below for traceability.

### 1. Visible Database File Extension

Choose one:

- `1A` - Use `.akb` as the visible database extension. Recommended.
  - User sees an application-specific database file; internally it is SQLite.
- `1B` - Use `.db`.
  - Technically transparent, but less product-specific and easier to confuse with arbitrary databases.
- `1C` - Support both `.akb` and `.db` in the UI.
  - Flexible, but adds extra UX and support branching.

### 2. First-Launch JSON Migration UX

Choose one:

- `2A` - Migrate automatically and show a non-blocking status message.
  - Fastest for users, but less explicit when the storage format changes.
- `2B` - Show a confirmation dialog before migration. Recommended.
  - Clearer and safer for an enterprise tool; the old JSON remains untouched either way.
- `2C` - Do not auto-detect legacy JSON; require manual import.
  - Simplest technically, but risks users thinking their old data disappeared.

### 3. Post-Migration JSON Safety Export

Choose one:

- `3A` - Create an automatic post-migration JSON export next to the new `.akb`. Recommended.
  - Gives support a readable fallback and makes migration easier to audit.
- `3B` - Do not create an automatic export; keep only the untouched original JSON.
  - Less disk output, but weaker support trail.
- `3C` - Ask the user whether to create the post-migration export.
  - More control, but more first-launch friction.

### 4. Network Folder / Multi-User Editing Policy

Choose one:

- `4A` - Explicitly state that simultaneous multi-user editing is unsupported in the first SQLite version. Recommended.
  - Prevents false expectations; a shared folder can still be used for manual file transfer/backup workflows.
- `4B` - Allow shared-folder use but warn that only one user should edit at a time.
  - More flexible, but support risk is higher.
- `4C` - Design for multi-user shared-folder editing immediately.
  - Larger scope; likely delays storage migration and still does not replace a real server database.
