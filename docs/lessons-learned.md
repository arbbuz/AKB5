# Lessons Learned

Last updated: `2026-04-29`

## UI and tree behavior

- Tree icon selection for `L1/L2/L3` must follow visible hierarchy depth, not only persisted `NodeType`
- When one visual level can contain stale or mixed saved `NodeType` values, prefer structural UI context over persisted type for icon selection
- When the user defines domain terminology such as `L1/L2/L3`, use it consistently in code review, fixes, and docs to avoid level/type confusion
- A `dotnet build` failure in this repo often comes from a running `asutpKB.exe` locking `bin\Release`, not from a compile error
- If one workflow creates both a tree node and its typed composition, keep both mutations inside the tree-history path so `Undo` restores the whole operation together
- When adding a new typed top-level collection, wire it through JSON normalization, session snapshot/dirty tracking, and subtree deletion cleanup in the same slice or the feature will drift out of sync
- In this repo, run `dotnet build` and `dotnet test` sequentially when they target the same `Release` outputs; parallel runs can create avoidable file locks
- If WinForms source files contain mojibake in user-facing strings, treat it as a source-file defect and rewrite the affected file in clean UTF-8 instead of patching isolated garbled literals
- When localizing the UI after a feature lands, update test expectations in the same slice or the suite will keep asserting stale English labels and messages
- If a WinForms screen auto-switches to another tab after list selection, reset the active tab inside `ApplyState` when a new node is loaded; otherwise the previously open tab looks like a false auto-jump on simple context change
- In WinForms, avoid setting `SplitContainer.SplitterDistance` together with min sizes too early in the constructor; on startup it can throw `InvalidOperationException` before the control gets a stable size
- When both list density and preview size matter, a shared split layout can become a losing compromise; separate full-size tabs for `list` and `preview` are often more robust than repeatedly tuning one static split
- When adding a new row to a WinForms summary card, avoid relying on a fixed container height; use auto-sizing where possible and do not report the change complete until the built `exe` shows the field visibly

## Investigation discipline

- A UI symptom reproduced on only one machine is not enough to justify broad workaround code in WinForms shell logic
- Before pushing a behavioral fix, separate repository-wide bugs from environment-specific symptoms
- For this project, the smallest coherent diff is usually the correct one
- Manual verification is useful for WinForms tab workflows, but it should confirm app behavior, not drive new script-only automation work unless the user explicitly asks for it
- If the user keeps the app open during verification, use an isolated `BaseOutputPath` for build validation instead of spending time on false compile investigations caused by file locks
- If a session has a natural green boundary, stop there after build/test and wait for review instead of continuing into docs, handoff, or the next roadmap slice in the same pass
- After a manual UX complaint, prefer changing the interaction model over micro-tuning dimensions if the underlying conflict is structural
- When a user supplies a hand-filled enterprise Excel workbook as an example, treat it first as a form/layout source and only secondarily as a rule source; historical manual entries can contain contradictions
- For a heavily formatted enterprise workbook with merges, hidden formulas, print layout, and signature blocks, prefer template-driven export over rebuilding the sheet structure from scratch
- If a planning marker like `ТО1/2` combines a maintenance type and hours, store them as separate domain concepts even if they render into one Excel cell
- If maintenance types have different hour values in the sample workbook, model separate integer hour norms for `ТО1`, `ТО2`, and `ТО3` instead of one generic labor field
- When the user defines periodicity abstractly but does not care about the starting month yet, use a deterministic per-node cycle offset so the planner stays stable until a formal yearly schedule source is introduced
- Do not confuse the later per-day `<= 8` planner cap with stored per-node `ТО1` / `ТО2` / `ТО3` norms; norms can exceed `8` and must stay separate from daily allocation limits

## Documentation discipline

- Keep one file per knowledge role:
  - current state
  - active plans
  - reusable lessons
  - durable decisions
- Keep `summary.md` as a pointer only, not as a second current-state document
- Replace stale statements instead of appending transcripts
- When a roadmap phase is completed, update `Roadmap.md`, `docs/codex-handoff.md`, `docs/plans.md`, and `docs/decision-log.md` in the same pass
- For this repo, the default delivery loop is `one step -> scripts/verify-step.ps1 -> stop -> manual review -> commit/push`
- Keep the knowledge-refresh trigger ASCII-safe in shared docs; if a localized literal turns into mojibake, replace it with a plain-text description instead of preserving broken bytes
- When a roadmap phase is repurposed rather than merely completed, update the roadmap, handoff state, active plans, and durable decisions together so future sessions do not continue implementing the abandoned direction
