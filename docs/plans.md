# Plans

Last updated: `2026-05-24`

## Active plan

- Work in `C:\Users\Olga\AKB5` on branch `Net`, tracking `origin/Net`.
- Current task: Codex-doc cleanup by the Dicta pattern. Keep common rules in `C:\Users\Olga\.codex\AGENTS.md`, keep AKB5 `AGENTS.md` project-specific, and keep active continuity in this workspace's `docs`.
- Keep the accepted Network scale/IP changes, the single-instance startup guard, the no-hover-tooltip cleanup, the Lvl2 inventory-number field fix, and the balanced maintenance schedule no-fail fallback fix ready for commit/push.
- Commit/push decision for the current uncommitted AKB5 package is pending user approval.
- Do not reintroduce hover/popup tooltips; guidance must be visible, inline, status-based, or modal.
- Treat old AKB5/Net/network-topology worktrees and snapshots under `C:\Users\Olga\Documents\Codex\...` as historical references only, not as source of truth.

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
- Topology element cards/icons/default spacing should be roughly 20% larger.
- IP badge/text should be roughly 30% larger.
- Adding/editing Network topology elements should reject duplicate normalized IP addresses on the same diagram.
- Empty IP is allowed; partial IP and octets outside `0..255` should be rejected.
- A second simultaneous AKB5 launch should be blocked with a per-user named mutex and a short informational message.
- Maintenance schedule generation should use route-flow planning: a large `Lvl2` system is one with more than two visible `Lvl3` children; a day may contain at most one large system; small systems may be added as fillers only while the resulting day stays within the shift-load limit.
- Daily selection should minimize deviation from `AK9 = planned/requested month hours / working days`, while still preventing a repeated owner object on the same date.
- All working days should be occupied when the month has enough visits to do so.
- The shift-load limit is `16 h` while `AK9 <= 16`; if `AK9 > 16`, the planner may exceed `16 h` and uses `ceil(AK9) + 1` as a small route-constraint allowance.
- Candidate day ranking should remain ordered around constrained scheduling: group with the fewest feasible days first; then shift-load-limit compliance; empty day until the target occupied-day count is reached; future same-system continuation before calendar rollback; nearest continuation; closeness to `AK9`; lower current load; small-system filler below target; lower projected total; weak adjacency penalties; earlier date.
- Same-system continuations should go to the nearest next feasible working day before other balancing criteria.
- Multiple assignments from the same system may be placed on the same date when they are for different owner nodes. A repeated assignment for the same owner node on one date remains blocked, so split work continues on the next working day.
- If one owner object needs more separate visits than available working days, generation should fail clearly instead of creating a repeated-object day.
- Validate with app/core/test format checks, Release build, maintenance-focused tests, full core test suite, publish, and `git diff --check`.
- Manual review passed for the scale/IP-validation follow-up on `2026-05-24`; changes are still uncommitted.
- Single-instance startup guard build/format validation passed on `2026-05-24`; manual double-launch review is still not run.
- Balanced maintenance schedule route-flow fix passed local format/build/tests/publish on `2026-05-24`; manual Excel review is still not run. Latest read-only KЦ full-year diagnostic after shift-limit/visit-queue refinement passed all 12 months with `empty=0`, January max `20` because `AK9=18.67`, and all other months max `16`. Manual-review executable: `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`.
- Tooltip regression cleanup passed format/build/full tests/publish on `2026-05-24`; explicit `ToolTip`, `ToolTipText`, and `SaveToolTip` references were removed, and item/grid automatic tooltips are disabled.
- Lvl2 inventory-number field visibility fix passed app format check, targeted form-state tests, and isolated Release build on `2026-05-24`; manual-review executable: `C:\Users\Olga\AKB5\artifacts\build-check\lvl2-inventory-field\asutpKB.exe`.
- Maintenance no-fail fallback fix passed planner tests, maintenance-focused tests, full test suite, Release build, publish, and KЦ full-year diagnostic on `2026-05-24`; generated February workbook: `C:\Users\Olga\Pictures\Купоросный цех (КЦ)_ГрафикТО_2026_02_fixed.xlsx`.
- Keep existing Level 2-only Network topology behavior unchanged.

## Not active / out of scope

- PRONETA/CSV import, live scan, OCR/PDF import, plan/fact comparison, data-quality panels, AKB5-driven IP/PROFINET-name assignment, or embedded PDF preview.
- Direct edits to real `.akb` or JSON user databases.
- Commit/push of the current uncommitted AKB5 package before the user requests it.

## Update rule

- Keep only active and near-term plans here.
- Remove completed or rejected items instead of growing a history log.
