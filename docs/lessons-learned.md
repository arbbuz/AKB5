# Lessons Learned

Last updated: `2026-04-28`

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

## Investigation discipline

- A UI symptom reproduced on only one machine is not enough to justify broad workaround code in WinForms shell logic
- Before pushing a behavioral fix, separate repository-wide bugs from environment-specific symptoms
- For this project, the smallest coherent diff is usually the correct one
- Manual verification is useful for WinForms tab workflows, but it should confirm app behavior, not drive new script-only automation work unless the user explicitly asks for it
- If the user keeps the app open during verification, use an isolated `BaseOutputPath` for build validation instead of spending time on false compile investigations caused by file locks

## Documentation discipline

- Keep one file per knowledge role:
  - current state
  - active plans
  - reusable lessons
  - durable decisions
- Keep `summary.md` as a pointer only, not as a second current-state document
- Replace stale statements instead of appending transcripts
- When a roadmap phase is completed, update `Roadmap.md`, `docs/codex-handoff.md`, `docs/plans.md`, and `docs/decision-log.md` in the same pass
