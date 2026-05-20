# New Chat Handoff: AKB5 Design Network UI Polish

Date: `2026-05-20`

## Start here

Use the design worktree:

```powershell
cd C:\Users\Olga\AKB5-design
git status --short --branch
git log --oneline --decorate -5
```

Expected:

```text
## design/network-ui-polish
a89e593 (HEAD -> design/network-ui-polish) Improve network passport manual entry hints
```

The main logic worktree should be rechecked before assuming anything about local `Net` changes:

```powershell
git -C C:\Users\Olga\AKB5 status --short --branch
```

It is expected to be clean at `b2ea12e Enforce network passport mutation validation`; keep parent logic work isolated from this design branch.

## Light context files to read

- `C:\Users\Olga\AKB5-design\AGENTS.md`
- `C:\Users\Olga\AKB5-design\docs\codex-handoff.md`
- `C:\Users\Olga\AKB5-design\docs\plans.md`

Use `docs\decision-log.md`, `docs\lessons-learned.md`, and `Roadmap.md` only for targeted lookups when the next task needs past decisions, recurring pitfalls, or roadmap context.

## Current branch split

- `C:\Users\Olga\AKB5` is the `Net` worktree for logic/manual-entry work and is expected to be clean at `b2ea12e`.
- `C:\Users\Olga\AKB5-design` is the `design/network-ui-polish` worktree for UI/UX work.
- The design worktree is based on `a89e593 Improve network passport manual entry hints`; the parent `Net` branch has advanced to `b2ea12e Enforce network passport mutation validation`.
- Do not commit, push, merge, rebase, delete stash entries, or remove worktrees unless the user explicitly asks in the current chat.
- Do not assume the main `Net` worktree is clean without rerunning `git -C C:\Users\Olga\AKB5 status --short --branch`.

## Current design worktree changes

The design worktree contains the uncommitted Network review-filter package plus layout/readability polish and enlarged tree icons:

- `Controls/KnowledgeBaseNetworkScreenControl.cs`: `Только проверка`, review counters, `Копировать проверку`, stable three-row filter layout so `Фильтр` does not overlap `Устройства`, wider `Проверка` columns, subtle warning-row highlighting, and tooltips/clearer filter status text.
- `Controls/KnowledgeBaseTreeView.cs`: 30px tree-icon row height and indent.
- `UiServices/KnowledgeBaseTreeNodeVisuals.cs`: Lvl1/Lvl2/Lvl3 icons use Google Material Symbols SVG path data (`domain`, `precision_manufacturing`, `dns`) with the current accent colors preserved.
- `Services/KnowledgeBaseNetworkStateService.cs`: computed warning-row counters.
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkStateServiceTests.cs`: focused assertions for warning counters.
- `Controls/KnowledgeBaseTreeView.cs` and `UiServices/KnowledgeBaseTreeNodeVisuals.cs`: additional tree visual/icon dirty files currently present in the design worktree; refresh validation before including them in manual review.
- docs/handoff files are updated to describe the local package.
- `docs/new-chat-handoff-2026-05-20-net-a89e593.md` is an existing untracked handoff artifact from the earlier Net transition.

No `.akb`, JSON data, Excel files, OCR/import/scan/PDF-preview work is part of this design branch.

## Last known validation

After the design polish pass, the package passed from `C:\Users\Olga\AKB5-design`:

- focused Network tests: `41/41`;
- focused tree tests: `33/33`;
- full Release tests: `433/433`;
- app/core/tests format checks;
- `git diff --check`;
- non-invasive/offscreen layout smoke: `artifacts\layout-smoke\network-review-filter-polish`;
- tree icon smoke/preview: `artifacts\tree-icon-smoke\bin\Debug\net8.0-windows\tree-icons-preview.png`.

The current manual-review artifact was built from the design worktree:

```text
C:\Users\Olga\AKB5-design\artifacts\build-check\tree-icons-50pct-20260520-224516\asutpKB.exe
```

## Recommended next step

Refresh validation if the tree visual/icon changes remain in the design package, then use the resulting artifact for manual review. When manual review is accepted, ask before committing, merging back to `Net`, or pushing.
