# Lessons Learned

Last updated: `2026-04-29`

## UI and tree behavior

- Tree icon, card-field, and tab-visibility rules for `Lvl1/Lvl2/Lvl3` should follow visible hierarchy depth, not only persisted `NodeType`
- When one visible level can contain stale or mixed saved `NodeType` values, prefer structural UI context over persisted type for card and workspace behavior
- When the user defines domain terminology such as `L1/L2/L3`, use it consistently in code, tests, and docs to avoid level/type confusion
- When adding a new row to a WinForms summary card, avoid relying on a fixed container height; use auto-sizing where possible and do not report the change complete until the built `exe` shows the field visibly
- If a WinForms dialog starts carrying derived planning data, show the calculated demand before asking the user to choose a budget; otherwise the budget field reads like an unexplained magic number

## Planner and workbook logic

- A hand-filled enterprise workbook should be treated first as a form/layout source and only then as a rule source; validate business rules against multiple examples before hardcoding them
- Do not invent operational caps from intuition; in this maintenance workflow the hard constraint is the monthly workshop budget, not a daily `<= 8` cap
- When maintenance types include one another, keep the norms separate per type but resolve monthly demand so higher tiers replace lower tiers instead of stacking on top of them
- If the user does not yet provide a formal yearly schedule source, a deterministic per-node cycle offset is a workable interim rule for `ТО2` / `ТО3` month placement
- For a heavily formatted enterprise workbook with merges, print layout, formulas, and signature blocks, prefer template-driven export over rebuilding the sheet structure from scratch
- An Excel repair prompt after month regeneration can come from structural leftovers, not only from formulas:
  - stale `calcChain`
  - stale row tails below the rewritten block
  - stale `rowBreaks`
- When rewriting one month sheet inside an existing workbook, clear the old tail rows and related row-break metadata or Excel may report corrupted sheet content even if the new rows themselves are valid

## Import and file-handling discipline

- Importing data from human-maintained Excel files needs forgiving normalization; exact string matching is rarely enough once real equipment names diverge by spaces, suffixes, or dot-separated context
- For maintenance norm import, match by system/equipment inventory number first and fall back to normalized names only when inventory data is missing or ambiguous
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
