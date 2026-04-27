# Lessons Learned

Last updated: `2026-04-27`

## UI and tree behavior

- Tree icon selection for `L1/L2/L3` must follow visible hierarchy depth, not only persisted `NodeType`
- When one visual level can contain stale or mixed saved `NodeType` values, prefer structural UI context over persisted type for icon selection
- When the user defines domain terminology such as `L1/L2/L3`, use it consistently in code review, fixes, and docs to avoid level/type confusion
- A `dotnet build` failure in this repo often comes from a running `asutpKB.exe` locking `bin\Release`, not from a compile error

## Investigation discipline

- A UI symptom reproduced on only one machine is not enough to justify broad workaround code in WinForms shell logic
- Before pushing a behavioral fix, separate repository-wide bugs from environment-specific symptoms
- For this project, the smallest coherent diff is usually the correct one

## Documentation discipline

- Keep one file per knowledge role:
  - current state
  - active plans
  - reusable lessons
  - durable decisions
- Keep `summary.md` as a pointer only, not as a second current-state document
- Replace stale statements instead of appending transcripts
