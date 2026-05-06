# Lessons Learned

Last updated: `2026-05-06`

## UI and tree behavior

- Tree icon, card-field, and tab-visibility rules for `Lvl1/Lvl2/Lvl3` should follow visible hierarchy depth, not only persisted `NodeType`
- When one visible level can contain stale or mixed saved `NodeType` values, prefer structural UI context over persisted type for card and workspace behavior
- When the user defines domain terminology such as `L1/L2/L3`, use it consistently in code, tests, and docs to avoid level/type confusion
- When adding a new row to a WinForms summary card, avoid relying on a fixed container height; use auto-sizing where possible and do not report the change complete until the built `exe` shows the field visibly
- If a WinForms dialog starts carrying derived planning data, show the calculated demand before asking the user to choose a budget; otherwise the budget field reads like an unexplained magic number
- If a command operates on the whole workshop rather than on the selected node, place it in a top-level menu or workshop-level workflow entry point, not inside every node card

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

## Documentation discipline

- Keep one file per knowledge role:
  - current state
  - active plans
  - reusable lessons
  - durable decisions
- Keep `summary.md` as a pointer only, not as a second current-state document
- Replace stale statements instead of appending transcripts
- For this repo, the default delivery loop is `one step -> scripts/verify-step.ps1 -> stop -> manual review -> commit/push`
- When a branch or roadmap baseline changes, synchronize the handoff/docs promptly or future sessions will continue from the wrong branch and the wrong assumptions
- Do not treat a user-supplied phase label such as `7G` as approved scope if `Roadmap.md` has no such phase; first define and accept the scope in the roadmap
- Documentation distillation should include `AGENTS.md` and `README.md` when they contain branch or phase summaries; otherwise stale startup guidance can override the current handoff
