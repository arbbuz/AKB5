# Plans

Last updated: `2026-05-26`

## Active plan

- Work in `C:\Users\Olga\AKB5` on branch `Net`, tracking `origin/Net`.
- Current task: startup diagnostics / fast publish / Network topology link-type selection package is accepted, committed, and pushed. Wait for the next user request.
- Do not commit or push without fresh direct approval in the current chat.
- Do not reintroduce hover/popup tooltips; guidance must be visible, inline, status-based, or modal.
- Treat old AKB5/Net/network-topology worktrees and snapshots under `C:\Users\Olga\Documents\Codex\...` as historical references only, not as source of truth.

## Completed package

- Add startup timing checkpoints to existing file logs under `%LocalAppData%\AKB5\logs`.
- Keep timing diagnostics non-invasive and operator-invisible.
- Preserve default `scripts\publish.ps1` single-file behavior for the supported end-user flow.
- Add a fast working publish wrapper that builds a self-contained folder package with ReadyToRun and no single-file extraction.
- Validate with format checks, isolated Release build, fast publish, full core test suite, and `git diff --check`.
- Manual fast-publish review is accepted: user launched it several times and reported that startup feels faster.
- Network topology link-type selection is moved from the equipment toolbar ComboBox into a grouped floating palette on the canvas: `Profibus` (`опт.` / `медь`), `MPI` (`медь`), and `Profinet` (`опт.` / `медь`).
- Existing links can now be selected directly; pressing a palette button changes the selected link kind immediately. Without a selected link, the palette sets the type for the next new link.
- Network palette validation passed app format, isolated Release build, offscreen layout-smoke PNG, full core tests, and `git diff --check`.
- Next action is to wait for the next user request.
- Next possible optimization after measurement: if cold logs repeatedly show `mainform-data-loaded` dominating while SQLite remains fast, add narrower UI binding/session-application timing first; defer database load until after first form display only if that is still justified.

## Not active / out of scope

- Changing database format or storage location.
- Reworking WinForms startup into a full architecture rewrite.
- Removing the existing single-file publish mode.
- Reworking Network topology beyond the link-type selector placement.
- Direct edits to real `.akb` or JSON user databases.

## Update rule

- Keep only active and near-term plans here.
- Remove completed or rejected items instead of growing a history log.
