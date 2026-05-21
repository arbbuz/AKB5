# Plans

Last updated: `2026-05-22`

## Active plan

- Treat branch `Net` in `C:\Users\Olga\AKB5` as the active branch.
- Accepted baseline before the current uncommitted package: `92e5af5 Port network UI design polish`.
- Current task: complete full removal of the AKB5 Network feature after the user rejected the Network tab and confirmed full deletion.
- Do not commit, push, merge, rebase, delete stash entries, or remove worktrees unless the user explicitly asks in the current chat.
- Keep user-facing strings Russian-only.
- Keep output compact and report progress during longer validation.

## Current implementation plan

- Remove Network UI, models, services, storage fields, template defaults, tests, and workspace tabs.
- Preserve normal backup-before-save behavior for existing `.akb` files.
- Do not edit real user `.akb` or JSON data files.
- Finish validation before calling the package ready.

## Not active / out of scope

- Any future Network tab, passport, overview, topology, PRONETA/CSV import, live scan, plan/fact comparison, IP/PROFINET assignment, PDF preview, OCR, or Network data-quality panel work.
- Direct edits to user databases.
- Commit/push without explicit approval.

## Update rule

- Keep only active and near-term plans here.
- Remove completed or rejected items instead of growing a history log.
