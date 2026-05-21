# New Chat Handoff: AKB5 Net

Last updated: `2026-05-20`

## Start commands

Run these first in the new chat:

```powershell
git -C C:\Users\Olga\AKB5 status --short --branch
git -C C:\Users\Olga\AKB5 log --oneline --decorate -5
```

## Expected state

- Main Net worktree: `C:\Users\Olga\AKB5`
- Branch: `Net`
- Expected `HEAD` and `origin/Net`: `b2ea12e Enforce network passport mutation validation`
- Current local state is tracked in `docs\codex-handoff.md`; after the mutation-service validation package was accepted, committed, and pushed, the main `Net` worktree should be clean.
- Separate design worktree: `C:\Users\Olga\AKB5-design` on `design/network-ui-polish`, carrying UI/UX work that belongs to the separate design chat. Do not use it for a `Net` continuation unless the user explicitly asks.
- Do not commit, push, merge, rebase, delete stash entries, or remove worktrees without explicit approval in the current chat.

## Light context files to read

- `C:\Users\Olga\AKB5\AGENTS.md`
- `C:\Users\Olga\AKB5\docs\codex-handoff.md`
- `C:\Users\Olga\AKB5\docs\plans.md`
- `C:\Users\Olga\AKB5\docs\new-chat-handoff-2026-05-20-net-a89e593.md`

Use `docs\decision-log.md`, `docs\lessons-learned.md`, and `Roadmap.md` only for targeted lookups when the next task needs past decisions, recurring pitfalls, or roadmap context.

## Current Net baseline

The latest accepted Net package is committed and pushed:

- `b2ea12e Enforce network passport mutation validation`

It enforces existing interface address and connection length validation rules inside `KnowledgeBaseNetworkMutationService`, with mutation-service tests for invalid IP, non-contiguous mask, invalid gateway, and invalid cable length.

Previous accepted Net package:

- `a89e593 Improve network passport manual entry hints`

It includes manual-entry speed and inline duplicate-hint ergonomics:

- add-similar actions for devices, interfaces, and connections;
- row context-menu add-similar actions;
- copied drafts keep stable context and leave unique identity/address/cable fields blank;
- inline `Проверка` duplicate hints for device PROFINET-name/MAC, interface IP/MAC/same-port, and connection cable labels;
- Network list copies and visible export include the `Проверка` column.

Previous accepted Net baseline:

- `207b6b1 Improve network passport review ergonomics`

## Branch split

- Use `C:\Users\Olga\AKB5` / `Net` for logic, data model, services, validation, persistence, and coherent manual-entry/manual-review behavior packages.
- Use `C:\Users\Olga\AKB5-design` / `design/network-ui-polish` for UI/UX polish, WinForms layout fixes, visual ergonomics, and offscreen layout-smoke work.
- Do not mix the two worktrees unless the user explicitly asks to merge or coordinate them.

## Scope constraints

- Keep future Net work inside Network manual-entry/manual-review ergonomics unless the user explicitly approves broader scope.
- Do not start OCR/PDF auto-import, PRONETA/CSV import, live scan, plan/fact comparison, separate data-quality issue/problem panels, AKB5-driven IP assignment, AKB5-driven PROFINET-name assignment, or embedded PDF preview.
- Do not run interactive UI-smoke unless explicitly requested; for Network UI use non-invasive/offscreen layout-smoke.
- Bundle future Net manual-entry/UI refinements into one coherent review package rather than micro-stages.
- If Codex shows no movement for 2-3 minutes after a completed tool result, treat it as an orchestration stall and recover immediately.

## Recommended next Net direction

Choose the next Net-side task only after reading the light context and checking the `Net` worktree. Prefer logic/manual-review behavior that does not collide with the separate design branch, for example:

- improving state-service validation summaries;
- strengthening focused tests around existing Network passport behavior;
- refining manual-entry data consistency rules that do not require new automation or separate issue panels.

If the next request is actually visual/layout polish, redirect it to `C:\Users\Olga\AKB5-design` unless the user asks to work in `Net`.
