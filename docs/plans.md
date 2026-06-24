# Plans

Last updated: `2026-06-23`

## Active plan

- Work in `C:\Users\Olga\AKB5` on branch `Net`, tracking `origin/Net`.
- Current repo state: `Net` is synchronized with `origin/Net`; local uncommitted changes include the user-approved `AGENTS.md` rule update, this plan file, and the in-progress Global Search 2.0 service/tests work.
- Active implementation direction: revise Global Search 2.0 before continuing, because the first expanded index pass exposed an unacceptable behavior class: search results can navigate to a tab where the matched value is not visible to the user.
- Do not commit, push, merge, rebase, or revert `AGENTS.md` without fresh direct approval in the current chat.
- Do not reintroduce hover/popup tooltips; guidance must be visible, inline, status-based, or modal.
- Treat old AKB5 worktrees and snapshots under `C:\Users\Olga\Documents\Codex\...` as historical references only, not as source of truth.

## Most Recent Accepted Package

- Latest accepted package on `Net`: workspace table redraw and Lvl3 composition column-width stabilization.
- Commit pushed to `origin/Net`: `791f066 Stabilize workspace table redraw`.
- Added `Controls\BufferedDataGridView.cs` for double-buffered WinForms grids.
- Added `Controls\ControlRedrawScope.cs` for scoped redraw suppression during UI rebuilds.
- Batched rack/additional-equipment table row rebuilds and reduced visible redraw waves in grid-heavy forms.
- Lvl3 composition columns `Slot`, `Role`, `Type`, and `OrderNumber` now keep direct fixed pixel widths through `DataGridViewColumn.Width`.
- The visible empty/filler-column experiment was rejected and removed before acceptance.
- Current accepted tradeoff: if the total fixed width of the four Lvl3 composition columns is smaller than the table viewport, a blank area may remain on the right. Do not change this without a new explicit UX decision.

## Completed Recent Technical Packages

- PDF import was moved out of the main executable into the optional `pdf-import` module.
- OpenXML/Excel exchange was moved out of the main executable into the optional `excel-exchange` module.
- `scripts\publish.ps1` publishes the main app plus sibling `pdf-import` and `excel-exchange` module folders.
- Portable-first SQLite `.akb` storage remains the current storage baseline.

## Near-Term Candidate Work

### 1. Global Search 2.0

Goal: make one search find operational data without sending the user to a place where the matched value is invisible.

What the current review found:

- `Пакет A` added the needed result target contract and should be kept.
- The first implementation attempt for `Пакет B` expanded the searchable index too far. It was not an accepted behavior baseline: it indexed hidden IDs, dialog-only fields, style-only network data, and maintenance row details that are not visible after the result opens.
- `Пакет B` is now implemented in the working tree as a corrective cleanup: searchable text is reduced to visible fields, hidden values remain only as target metadata, and regression tests cover hidden numeric values such as `24`.
- The equipment catalog stays out of the nearest Global Search 2.0 scope because it already has its own search and solves a different workflow.

Mandatory rule:

- Searchable text may include only values that are visible immediately after opening the result, or values that the result UI itself clearly displays as `area / object / field / value`.
- Internal IDs, storage keys, hidden row identifiers, and navigation-only values may stay in target metadata, but must not be searchable text.
- Fields that exist only in edit dialogs must not be searchable until the result UI shows the exact `field: value`, or the navigation opens/reveals that exact field.
- Short numeric queries such as `24` are high risk. No broad normalization, fuzzy search, or hidden-field expansion until the visible-index rule is enforced by tests.

`Пакет A. Контракт результата поиска`

Status: verified and kept.

Scope:

- Keep `KnowledgeBaseTreeSearchMatch.Target`: result kind, owner node id, entity id, field key, and row key.
- Keep target metadata separate from searchable text.
- Allow internal ids only inside target metadata.

Exit criteria:

- Search-focused tests verify target metadata for visible fields.
- Tests prove hidden ids do not match as searchable text.

`Пакет B. Очистка индекса до видимых полей`

Status: implemented in the working tree; automated validation passed. Manual GUI review is still needed before starting `Пакет C`.

Keep searchable now:

- Tree: node name/path only.
- Card/info tab: description, plus inventory number only for node levels where the info tab actually shows inventory.
- Composition table: `Slot`, `Role`, `Type`, `OrderNumber`.
- Additional equipment table: `№`, `Type`, `OrderNumber`, `Notes`.
- Documents/software lists: title, path, document updated date, software added date.
- Network screen: visible element text only: element name, main IP, additional IP values, external-connection text.
- Maintenance screen: visible profile summary only: included/excluded flag, TO1/TO2/TO3 norms, and year placement only if the indexed text exactly matches the visible summary string.

Remove from searchable text in the current code:

- Tree node type.
- Card location, photo path, technical IP, schema link, and any field hidden for the opened node level.
- Composition dialog-only fields: model, firmware, MPI/DP/PN, input/output addresses, comment, interfaces, row IP, calibration dates, notes.
- Additional-equipment dialog-only fields: model, firmware, MPI/DP/PN, input/output addresses, comment, interfaces, row IP, calibration dates.
- Document/software IDs and document kind text if the opened list does not show that exact text as a column/value.
- Software last-changed date, backup date, and notes.
- Network element kind text, link kind text, link label, and endpoint-pair text because they are not rendered as the exact searchable text after navigation.
- Maintenance month, work kind, explicit month-row hours, and generated row-plan text until the UI can reveal that exact row.

Required tests:

- Current positive tests for non-visible fields were converted into no-match regression tests.
- Positive tests now cover only fields visible at the opened destination.
- A short numeric regression verifies that hidden/internal `24` does not produce an invisible match.

Manual check after package:

- Searching `24` must not open a tab where `24` is absent on screen.
- Search results for visible table/list fields must open the owning object and the correct workspace tab.

`Пакет C. Понятный список результатов`

Status: next implementation package after manual review accepts `Пакет B`.

Scope:

- Add a compact result list with visible columns: area, object/path, field, value.
- Enter/double-click opens the selected result.
- Previous/next buttons continue to work.
- The user must see why a result matched before relying on exact row/dialog navigation.

`Пакет D. Точная навигация к видимому месту`

Status: can be implemented with `Пакет C` only if the diff remains focused; otherwise do it immediately after `Пакет C`.

Scope:

- Composition: select rack/slot row and matching visible column.
- Additional equipment: select row and matching visible column.
- Documents/software: select the document or software row.
- Network: select a visible element. Do not select/search links until link text is rendered or fully explained in the result list.
- Maintenance: select/open the visible summary only. Do not search month-row details until the UI can reveal the exact row.

`Пакет E. Расширение поиска на дополнительные поля`

Status: do not start until `Пакет C` and the relevant `Пакет D` navigation are accepted.

Scope:

- Reintroduce additional fields one domain at a time, not all at once.
- Each reintroduced field must either be visible in the destination or be fully displayed in the result list.
- Composition/additional equipment, software, network links, and maintenance month details each need their own positive and no-match tests.

`Пакет F. Сброс кэша поиска`

Status: correctness package before advanced search behavior.

Scope:

- Verify the search index invalidates after changes in tree, card, composition, additional equipment, documents/software, network, maintenance, and imports.
- Add tests that mutate data without changing list counts, so stale cached matches cannot survive.

`Пакет G. Нормализация и стабильная сортировка`

Status: last, after the visible/explainable index and cache behavior are stable.

Scope:

- Case-insensitive text.
- Whitespace-tolerant order numbers.
- Partial IP search.
- Multi-word AND search.
- Stable ranking: exact match, prefix match, contains match, then tree/display order.

Why it matters:

- On a large database, users should not need to know whether a value lives in the tree, card, composition, network passport, maintenance data, documents, or software records, but every result must be explainable on screen.

### 2. Database Quality Check

Goal: add a user-facing command that checks the database for practical data problems.

Candidate scope:

- Duplicate IP addresses and network names.
- Empty important card/composition fields.
- Duplicate slots inside one cabinet.
- Broken document/software paths.
- Missing or incomplete maintenance profiles/norms.
- Production-calendar conflicts.
- Broken references to deleted nodes.
- Duplicate or unused catalog records.

Suggested UI:

- A list of findings with severity: error, warning, information.
- Object/path context for each finding.
- Action to navigate to the affected object.
- Later extension: export the report.

Why it matters:

- This gives the database maintainer a real operational tool before audits, handoff, or bulk updates.

### 3. Safer High-Risk Operations

Goal: make import, restore, and replacement actions harder to misuse.

Candidate scope:

- One confirmation flow for replacing the current database from JSON/Excel, restoring from snapshot, and importing maintenance norms.
- Show the selected file, what will be replaced/changed, whether a protective snapshot will be created, and what the user can check first.
- Prefer action labels such as `Создать снимок и заменить`, `Только проверить`, `Отмена` instead of generic `OK`.
- After completion, show a concise result summary.

Why it matters:

- It directly reduces the risk of accidental data replacement and support questions after file operations.

### 4. Long-Table Ergonomics

Goal: improve daily work in large tables.

Candidate scope:

- Local filters in the equipment catalog, network passport, and other long grids.
- "Only problematic" filter where validation data exists.
- Preserve user widths/sort/filter state where it is stable and useful.

Why it matters:

- It reduces scrolling and visual scanning in the places users already feel as slow or noisy.

## Not Active / Out Of Scope

- Reopening the accepted Lvl3 fixed-width column behavior without a new explicit decision.
- Adding a visible empty/filler column to consume leftover table width.
- Reworking startup into asynchronous/deferred database loading.
- Removing or changing SQLite `.akb` storage behavior.
- Adding OCR or embedded PDF preview/rendering.
- Adding PRONETA/CSV import, live network scan, plan/fact comparison, AKB5-driven IP assignment, or AKB5-driven PROFINET-name assignment without a new explicit scope decision.

## Validation Baseline

Recent accepted package validation:

- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet build asutpKB.csproj --configuration Release --no-restore`: passed, 0 errors, existing warnings.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --no-build`: passed, 410 tests.
- `git diff --check -- Controls Forms`: passed, only CRLF normalization warnings.

## Commands To Run Before Finishing Future Implementation Work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```

## Update Rule

- Keep only active and near-term plans here.
- Remove completed or rejected items instead of growing a history log.
