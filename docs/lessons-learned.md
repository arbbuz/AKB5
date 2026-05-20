# Lessons Learned

Last updated: `2026-05-20`

## UI and tree behavior

- Tree icon, card-field, and tab-visibility rules for `Lvl1/Lvl2/Lvl3` should follow visible hierarchy depth, not only persisted `NodeType`
- When one visible level can contain stale or mixed saved `NodeType` values, prefer structural UI context over persisted type for card and workspace behavior
- When the user defines domain terminology such as `L1/L2/L3`, use it consistently in code, tests, and docs to avoid level/type confusion
- When adding a new row to a WinForms summary card, avoid relying on a fixed container height; use auto-sizing where possible and do not report the change complete until the built `exe` shows the field visibly
- If a WinForms dialog starts carrying derived planning data, show the calculated demand before asking the user to choose a budget; otherwise the budget field reads like an unexplained magic number
- If a command operates on the whole workshop rather than on the selected node, place it in a top-level menu or workshop-level workflow entry point, not inside every node card
- For menu cleanup, group commands by user workflow instead of storage format: `.akb` open/save in `Файл`, maintenance planning in `ТО`, reference data in `Справочники`, and support/import/export/reload operations in `Сервис`.
- For risky operations such as full database replacement, maintenance norm import, workshop deletion, and mass template application, offer or create a protective snapshot before mutating data.
- If two dialogs expose the same table concept, persist their window placement and column widths separately unless the user explicitly wants shared geometry; otherwise one workflow can overwrite another workflow's saved layout.
- When catalog data is used inside composition editing, keep the picker behavior aligned with the catalog browser: same visible columns, same local search scope, same sorting expectations, and same remembered layout behavior.
- Avoid leaving demo-like hardcoded templates under user-facing production commands. If users need real operational templates, make them persisted and manageable from the UI instead of requiring code edits.
- Distinguish clearly between `composition templates` and `object templates`: composition templates currently fill only `SavedData.CompositionEntries`, while object templates can carry tree substructure plus typed records. Similar menu labels can otherwise lead users to expect one system to manage the other.

## Network passport workflow

- For network passport work, useful manual-review ergonomics can be more valuable than premature automation: visible endpoint context, copy-friendly rows, filtered export, selected-row visibility, and row tooltips help users compare AKB5 against external schemes without importing those schemes.
- Keep network PDF references as source metadata plus `Open original` until an embedded renderer is explicitly approved. Do not let accepting PDF references imply OCR, parsing, or embedded preview.
- Treat OCR/PDF auto-import, PRONETA/CSV import, live scan, plan/fact comparison, quality-issue panels, and AKB5-driven IP/PROFINET-name assignment as separate future product decisions, not natural follow-ons from manual passport entry.
- For WinForms Network UI changes, a source diff and unit tests are not enough; use non-invasive/offscreen layout-smoke to confirm controls remain visible and reachable without disturbing manual app work.
- When a branch has just moved to a new focused stream such as `Net`, update `docs/codex-handoff.md`, `docs/plans.md`, `AGENTS.md`, and `Roadmap.md` together or later sessions may resume from an old `to`/`card` priority.
- For manual network-passport entry, prefer add-from-similar drafts that copy stable context fields but leave unique identity/address fields blank; this speeds repeated entry without creating accidental duplicates.
- Narrow inline duplicate hints belong in the row being reviewed, not in a separate issue panel, while the `Net` stream is still manual-entry/manual-review first.

## Planner and workbook logic

- A hand-filled enterprise workbook should be treated first as a form/layout source and only then as a rule source; validate business rules against multiple examples before hardcoding them
- Do not invent operational caps from intuition; in this maintenance workflow the hard constraint is the monthly workshop budget, not a daily `<= 8` cap
- When one `ТО2` / `ТО3` occurrence has more than 8 labor hours, split that occurrence into assignment chunks instead of converting the chunk size into a global daily-cap rule
- If a future year fails with `производственный календарь ещё не настроен`, do not patch the service for one year; configure that year through the production-calendar UI or PDF/JSON import
- When maintenance types include one another, keep the norms separate per type but resolve monthly demand so higher tiers replace lower tiers instead of stacking on top of them
- If the user does not yet provide a formal yearly schedule source, a deterministic per-node cycle offset is a workable interim rule for `ТО2` / `ТО3` month placement
- For a heavily formatted enterprise workbook with merges, print layout, formulas, and signature blocks, prefer template-driven export over rebuilding the sheet structure from scratch
- Keep the monthly planner/export path as the canonical engine even if users want a yearly command; the yearly workflow should orchestrate repeated monthly generation instead of replacing the month-based core
- Keep annual `ТО1/ТО2/ТО3` placement separate from production-calendar setup: the former decides maintenance type by month, the latter decides working/non-working days
- For production-calendar PDFs, try the text layer first and validate against a real source before adding OCR; `calendar_2027.pdf` imports cleanly without OCR
- Do not reject maintenance profiles assigned directly to visible `Lvl2` nodes; real data can store a profile on the system-level node itself, and the export should use it as both group and row
- If equipment can appear or disappear during the year and the model has no active-from / active-to dates, the safest workflow is to freeze past months and recalculate only the current month through December
- For future-month replanning without active date ranges, require an existing yearly workbook and rewrite only the selected month range; generating a new workbook from scratch would leave past months blank instead of preserving them
- An Excel repair prompt after month regeneration can come from structural leftovers, not only from formulas:
  - stale `calcChain`
  - stale row tails below the rewritten block
  - stale `rowBreaks`
- When rewriting one month sheet inside an existing workbook, clear the old tail rows and related row-break metadata or Excel may report corrupted sheet content even if the new rows themselves are valid
- When OpenXML rewrites an in-memory workbook that may grow, do not open `SpreadsheetDocument` over `new MemoryStream(byte[])`; copy the bytes into a fresh expandable `MemoryStream` first

## Import and file-handling discipline

- Importing data from human-maintained Excel files needs forgiving normalization; exact string matching is rarely enough once real equipment names diverge by spaces, suffixes, or dot-separated context
- For maintenance norm import, match by system/equipment inventory number first and fall back to normalized names only when inventory data is missing or ambiguous
- For maintenance norm import, unresolved messages need source sheet/row references; a short count without location is not enough for manual cleanup of enterprise workbooks
- In annual maintenance norm workbooks, hidden rows can intentionally mark retired equipment; skip hidden rows before parsing system headers or equipment rows so old equipment does not affect monthly totals
- For yearly ТО source exchange, keep the import contract narrow: edit only `YearScheduleEntries`, and do not let a schedule-source workbook silently change norms, inclusion flags, or calendar settings
- For in-app yearly ТО source editing, keep context columns read-only and constrain month cells to `ТО1`, `ТО2`, `ТО3`, or blank so bulk editing cannot drift into profile/norm/calendar mutation
- If users keep the source workbook open in Excel, open it with sharing flags that tolerate `ReadWrite` and `Delete`; otherwise the import workflow fails for the wrong reason
- Temporary debug entry points inside the repo can silently hijack a WinForms app if the project glob compiles them; explicitly exclude `artifacts/**/*.cs` from the main app project

## Investigation discipline

- A `dotnet build` failure in this repo often comes from a running `asutpKB.exe` locking `bin\Release`, not from a compile error
- In this repo, run `dotnet build` and `dotnet test` sequentially when they target the same `Release` outputs; parallel runs can create avoidable file locks
- If the user keeps the app open during verification, use an isolated `BaseOutputPath` for build validation instead of spending time on false compile investigations caused by file locks
- If a session has a natural green boundary, stop there after build/test and wait for review instead of continuing into docs, handoff, or the next roadmap slice in the same pass
- A source diff is not enough for WinForms changes; confirm the control still fits and remains reachable in the built application
- Codex turns can stall after completed tool results without an active shell command; if there is no new tool output, token-count movement, or visible progress for about 2-3 minutes, interrupt/resume or start a fresh session and verify disk state compactly.
- Treat context as a hard budget: avoid broad log/session scans, full `Get-Content -Raw`, and full `git diff` unless explicitly requested or narrowed first; prefer aggregates, top-N summaries, `git diff --stat`, and `git status --short`.
- For documentation-only distillation and handoff updates, follow `docs/codex-operational-rules.md`: read relevant sections only, patch docs, verify compactly, and finalize instead of expanding into broad diagnostics.

## Documentation discipline

- Keep one file per knowledge role:
  - current state
  - active plans
  - reusable lessons
  - durable decisions
- Keep `summary.md` as a pointer only, not as a second current-state document
- Replace stale statements instead of appending transcripts
- For this repo, the default delivery loop is `one step -> scripts/verify-step.ps1 -> stop -> manual review -> commit/push`, but approved `Net` manual-entry/UI refinements should be bundled into coherent review packages instead of forced into tiny micro-stages
- When a branch or roadmap baseline changes, synchronize the handoff/docs promptly or future sessions will continue from the wrong branch and the wrong assumptions
- Do not treat a user-supplied phase label such as `7G` as approved scope if `Roadmap.md` has no such phase; first define and accept the scope in the roadmap
- Documentation distillation should include `AGENTS.md` and `README.md` when they contain branch or phase summaries; otherwise stale startup guidance can override the current handoff
