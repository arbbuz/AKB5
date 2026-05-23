# Plans

Last updated: `2026-05-23`

## Active plan

- Work in `C:\Users\Olga\AKB5` on branch `Net`, tracking `origin/Net`.
- Current task: manual review of the fixed existing-link endpoint drag/reassignment.
- Do not commit, push, merge, rebase, create remote branches, or edit user data files without explicit approval in the current chat.
- Keep user-facing progress compact and validation-focused.

## Current implementation plan

- Selected icon pair is implemented and manually accepted: SCALANCE `ix:network-wired`, HMI `ix:panel-ipc`.
- Right-click on a link segment should offer `Удалить связь` and remove only that clicked `KbNetworkLink`.
- Context-menu link-start command should read `Связать`; the toolbar command remains `Связь`.
- Keep object deletion separate from link deletion; do not remove connected objects when deleting a link.
- Link routing should connect device edge/port to device edge/port, not center-to-center.
- Orthogonal routes should avoid unrelated device rectangles and should not visually merge a PLC-SCALANCE link with an SRV-SCALANCE link.
- Devices above/below any object should connect to top/bottom ports; same-row devices should connect to left/right ports.
- Port positions should be distributed per object side from all incident links to avoid false T-junctions on any object type.
- Existing link endpoints should be draggable to another object without deleting/recreating the link.
- Invalid drops should be ignored when they are outside an object, on the fixed endpoint object, or would duplicate an existing connection.
- Drop validation must happen before clearing drag state; otherwise the dragged link snaps back to the old endpoint.
- Validate with app format check and isolated Release build.
- Keep existing Level 2-only Network topology behavior unchanged.

## Not active / out of scope

- PRONETA/CSV import, live scan, OCR/PDF import, plan/fact comparison, data-quality panels, AKB5-driven IP/PROFINET-name assignment, or embedded PDF preview.
- Direct edits to real `.akb` or JSON user databases.
- Commit/push without explicit approval.

## Update rule

- Keep only active and near-term plans here.
- Remove completed or rejected items instead of growing a history log.
