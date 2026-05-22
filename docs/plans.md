# Plans

Last updated: `2026-05-22`

## Active plan

- Treat `C:\Users\Olga\Documents\Codex\2026-05-22\net-merge-workspace-resolver\base-d429330` on branch `net-ui-fixes-on-d429330` as the active worktree for this recovered package.
- Current task: manually accepted Level 2 `Сеть` topology canvas package has been committed and pushed to `origin/Net`.
- Do not commit, push, merge, rebase, delete stash entries, or remove worktrees unless the user explicitly asks in the current chat.
- Keep user-facing strings Russian-only.
- Keep user-facing progress compact and validation-focused.

## Current implementation plan

- Keep the Network topology canvas scoped to Level 2 nodes.
- Persist topology through `KbNodeDetails.NetworkTopology` and the SQLite `details_network_topology_json` column.
- Preserve existing dirty/save behavior by routing canvas edits through `HandleNodeDetailsChanged`.
- Keep tests focused on tab availability, form state, SQLite schema/round-trip, and related workbook regression coverage already in the package.
- For future work, start from updated `origin/Net` and choose the next task explicitly.

## Not active / out of scope

- Network feature removal in this worktree, unless the user explicitly redirects to that task.
- PRONETA/CSV import, live scan, OCR/PDF import, plan/fact comparison, data-quality issue panels, AKB5-driven IP/PROFINET-name assignment, or embedded PDF preview.
- Direct edits to user databases.
- Commit/push without explicit approval.

## Update rule

- Keep only active and near-term plans here.
- Remove completed or rejected items instead of growing a history log.
