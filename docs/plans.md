# Plans

Last updated: `2026-05-20`

## Active plan

- Treat branch `Net` in `C:\Users\Olga\AKB5` as the active branch for future AKB5 network-passport logic/manual-entry work.
- Current accepted baseline is `a89e593 Improve network passport manual entry hints` on `HEAD` and `origin/Net`.
- The latest Net manual-entry hints package was accepted manually, committed, and pushed before this handoff.
- Current local uncommitted `Net` review package enforces existing Network field validation inside `KnowledgeBaseNetworkMutationService` for interface IP/mask/gateway values and connection length.
- Current UI/UX polishing is isolated in `C:\Users\Olga\AKB5-design` on `design/network-ui-polish`; do not mix that worktree into `Net` unless the user explicitly asks.
- Do not commit, push, merge, rebase, delete stash entries, or remove worktrees unless the user explicitly asks in the current chat.
- Keep future Net packages in manual-entry / manual-review ergonomics unless the user approves a broader scope.
- Bundle closely related Net manual-entry/UI refinements into one coherent package instead of splitting them into tiny micro-stages.
- Do not run interactive UI-smoke unless the user explicitly asks. Prefer non-invasive/offscreen layout-smoke for Network UI changes.
- Keep all new user-facing UI strings Russian-only.
- Follow `docs/codex-operational-rules.md` for every future Codex turn to control silent stalls and context growth.

## Near-term follow-up

Manual-review the current local mutation-service validation package before starting another `Net` direction.

After that review, choose the next coherent `Net` direction only inside manual-entry/manual-review ergonomics, preferably logic/state-service work that does not overlap with the active design-branch UI polish. Good candidates:

- improve state-service validation/review summaries that support manual passport review;
- strengthen focused tests around accepted Network passport behavior;
- refine manual-entry data consistency rules without adding automation, imports, or a separate issue panel.

If a future `Net` change needs visible UI work, keep it bundled as one review package and coordinate with `design/network-ui-polish`.

## Not active / out of scope

- OCR or PDF auto-import for network schemes.
- PRONETA/CSV import.
- Live network scan.
- Plan/fact comparison.
- Separate data-quality issue/problem panel.
- Assigning IP addresses or PROFINET names from AKB5.
- Embedded PDF preview/rendering dependency.
- Phase 8 through Phase 10 remain discussed candidate directions, not active implementation phases.

## Update rule

- Keep only active and near-term plans here.
- Remove completed items instead of growing a history log.
