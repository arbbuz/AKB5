# Plans

Last updated: `2026-05-26`

## Active plan

- Work in `C:\Users\Olga\AKB5` on branch `Net`, tracking `origin/Net`.
- Current task: `Сеть` topology extension with ET200, OLM, and selectable link types from the supplied network scheme legend.
- Do not commit or push without fresh direct approval in the current chat.
- Do not reintroduce hover/popup tooltips; guidance must be visible, inline, status-based, or modal.
- Treat old AKB5/Net/network-topology worktrees and snapshots under `C:\Users\Olga\Documents\Codex\...` as historical references only, not as source of truth.

## Current implementation plan

- Replace the visible Network topology `I/O` choice with `ET200`, while preserving legacy stored enum value `6` as an obsolete alias that normalizes/renders as `Et200`.
- Add `OLM` as a new topology element kind.
- Use the selected graphics:
  - `ET200-A`: modular station.
  - `OLM-C`: two-optoport module.
- Add five link kinds and render them as:
  - optical Profibus: magenta dashed;
  - copper Profibus: magenta solid;
  - copper MPI: red solid;
  - optical Profinet: green dashed;
  - copper Profinet: green solid.
- Default old links without a stored kind to copper Profinet.
- Let the toolbar dropdown choose the type for new links.
- Let the right-click link context menu change the type for an existing link.
- Keep existing Level 2-only Network topology behavior unchanged.
- Validate with app/core/test format checks, targeted topology/data tests, isolated Release build, full core test suite, offscreen Network topology smoke, and `git diff --check`.

## Not active / out of scope

- PRONETA/CSV import, live scan, OCR/PDF import, plan/fact comparison, data-quality panels, AKB5-driven IP/PROFINET-name assignment, or embedded PDF preview.
- Direct edits to real `.akb` or JSON user databases.
- New PDF parsing/import automation for this task.

## Update rule

- Keep only active and near-term plans here.
- Remove completed or rejected items instead of growing a history log.
