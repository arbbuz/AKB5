# Composition Rack UI note: 2026-05-18

Branch: `card`

## Decision

The `Состав` tab should no longer show two tables that duplicate the same Rack data. The accepted UI direction is:

- one detailed Rack table per Rack;
- `Rack0` and `Rack1` shown one below the other on the first screen;
- `Rack2+` shown below with vertical scrolling;
- a compact `+` button adds another Rack;
- the text button `Добавить Rack` is no longer shown;
- selecting a row inside a Rack table makes that Rack and slot the active selection for `Изменить Rack`, `Удалить Rack`, and `Добавить слот...`.

## Local implementation

Current local WIP replaces the old `Rack-компоновка` plus `Детали выбранного Rack` split view with stacked detailed Rack tables.

Touched files:

- `Controls/KnowledgeBaseCompositionScreenControl.cs`
- `Forms/MainForm.cs`
- `Forms/MainForm.Events.cs`

Important implementation notes:

- The old horizontal `SplitContainer` inside the `Состав` tab was removed.
- Old `DetailsPanelHeight` / `DetailsPanelHeightChanged` wiring was removed from `MainForm`.
- Column widths are now saved only for the shared detailed Rack table layout under `composition.rack-details`.
- The previous `composition.rack-slots` width state is obsolete but harmless if present in an existing layout-state JSON.
- WinForms can auto-select rows in non-focused `DataGridView` controls on first layout; the current implementation ignores those automatic selection changes unless the grid has focus.

## Verification

Validated locally before this note was written:

```powershell
dotnet build asutpKB.csproj --configuration Release --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --no-build
git diff --check
```

Result:

- Release build: 0 errors.
- Core tests: `365/365` passed.
- `git diff --check`: passed.
- Temporary WinForms smoke-test: two Rack detail windows render and default selection remains `Rack0` slot 1.
- Temporary WinForms smoke-test: selecting a row in the second Rack switches active selection to `Rack1`.

Manual-review executable:

```text
C:\Users\Olga\AKB5\artifacts\build-check\asutpKB-20260518-121141\asutpKB.exe
```

## Current state

This UI redesign is not committed yet. Last pushed commit remains:

```text
8832b74 Improve rack composition switching
```

Expected `git status --short --branch` at handoff time:

```text
## card...origin/card
 M Controls/KnowledgeBaseCompositionScreenControl.cs
 M Forms/MainForm.Events.cs
 M Forms/MainForm.cs
 M docs/codex-handoff.md
?? docs/composition-rack-ui-note-2026-05-18.md
```
