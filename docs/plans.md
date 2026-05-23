# Plans

Last updated: `2026-05-23`

## Active plan

- Treat `C:\Users\Olga\Documents\Codex\2026-05-23\network-topology-icons` on branch `net-topology-icons` as the active worktree for the topology icons/camera-removal package.
- Current task: user manual review of the published `ПЧ / преобразователь частоты` build.
- Do not commit, push, merge, rebase, delete stash entries, or remove worktrees unless the user explicitly asks in the current chat.
- Keep the dirty prompt-removal worktree isolated and untouched.
- Keep user-facing strings Russian-only.
- Keep user-facing progress compact and validation-focused.

## Current implementation plan

- Keep the Network topology canvas scoped to Level 2 nodes.
- Use the approved icon mapping from `artifacts\icon-review\icon-review.html`, with the later approved replacement of panel by `ПЧ / преобразователь частоты` using Siemens iX `drive.svg`.
- Keep HMI available as an approved topology element.
- Keep camera unavailable for new/edited topology elements.
- Use the published build at `artifacts\publish\network-topology-icons-vfd-win-x64\asutpKB.exe` for manual review.

## Not active / out of scope

- The separate prompt-removal package in another dirty worktree.
- PRONETA/CSV import, live scan, OCR/PDF import, plan/fact comparison, data-quality issue panels, AKB5-driven IP/PROFINET-name assignment, or embedded PDF preview.
- Direct edits to user databases.
- Commit/push without explicit approval.

## Update rule

- Keep only active and near-term plans here.
- Remove completed or rejected items instead of growing a history log.
