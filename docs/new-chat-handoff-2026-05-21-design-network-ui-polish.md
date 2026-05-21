# New Chat Handoff: AKB5 Design Network UI Polish

Date: `2026-05-22`

## Start here

Use the design worktree:

```powershell
cd C:\Users\Olga\AKB5-design
git status --short --branch
git log --oneline --decorate -5
```

Expected design state after the right-panel style-only commit/push:

```text
## design/network-ui-polish...origin/design/network-ui-polish
```

Read only the light context set first:

- `C:\Users\Olga\AKB5-design\AGENTS.md`
- `C:\Users\Olga\AKB5-design\docs\codex-handoff.md`
- `C:\Users\Olga\AKB5-design\docs\plans.md`

The main logic worktree must be rechecked before use:

```powershell
git -C C:\Users\Olga\AKB5 status --short --branch
```

Keep `C:\Users\Olga\AKB5` / `Net` isolated unless the user explicitly asks to coordinate or merge.

## Current state

- Design branch contains the Network review-filter/UI package, 30px Material Symbols tree icons, toolbar/menu polish, light shell/splitter polish, workshop selector resize, fixed 24px status bar, and the approved style-only right-panel shell.
- The right-panel style pass follows `C:\Users\Olga\AKB5-design\artifacts\previews\right-panel-style-only-components-preview.html`.
- The right-panel update changes only the visual shell of existing sections, tables, fields, buttons, and empty states. It does not change layout, tab order, object order, icons, tree placement, button placement, or behavior.
- The latest button-artifact fix uses owner-drawn workspace buttons so hover/focus does not draw jagged button contours.
- No `.akb`, JSON data, Excel files, OCR/import/scan/PDF-preview work is part of this branch.

## Preview / artifact

Latest isolated Release artifact for manual review:

```text
C:\Users\Olga\AKB5-design\artifacts\build-check\right-panel-button-artifacts-20260521-231829\asutpKB.exe
```

Approved preview:

```text
C:\Users\Olga\AKB5-design\artifacts\previews\right-panel-style-only-components-preview.html
```

## Validation

Pre-commit validation on 2026-05-22 passed:

- `git diff --check`
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`
- `dotnet build asutpKB.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --logger "console;verbosity=minimal" /p:RunAnalyzers=false /p:WarningLevel=0` (`433/433`)

## Recommended next step

Manual-review the latest artifact. If more polishing is needed, keep it style-only and preserve current layout/order/behavior. Merge back to `Net` only after explicit current-chat approval and a fresh check of the main worktree.
