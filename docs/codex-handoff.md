# Latest update

- Date: `2026-04-26`
- Local Windows repo for the current session: `C:\Users\Olga\AKB5`
- Active working branch: `interface`
- Current branch state:
  - local `HEAD`: `e7a5169` (`Revert "Stop deferred redraw during interactive window move"`)
  - local branch is aligned with `origin/interface`
  - current uncommitted edits are:
    - `Controls/KnowledgeBaseTreeView.cs`
    - `UiServices/KnowledgeBaseTreeNodeVisuals.cs`
    - `UiServices/KnowledgeBaseTreeViewService.cs`
    - `docs/codex-handoff.md`
- Validation completed on the current local tree:
  - `dotnet format C:\Users\Olga\AKB5\asutpKB.csproj --verify-no-changes --severity error --no-restore`
  - `dotnet format C:\Users\Olga\AKB5\src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`
  - `dotnet format C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`
  - `dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore`
  - `dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-build --no-restore`
  - result: build passed, tests passed (`156/156`)
- Manual Windows GUI verification of the drag symptom has not yet been performed in this session.

# Current objective

- Fix the long-standing window drag lag in the WinForms desktop UI.
- Keep the patch minimal and avoid broad UI rewrites.
- Do not reintroduce the previous full-window redraw suppression experiment from `MainForm`; it caused the gray-background regression and did not address the real bottleneck.

# Drag bug analysis

- There is no custom main-window drag implementation in the current codebase:
  - no `MouseMove` / `MouseUp` loop moving form `Left` / `Top` / `Location`
  - no `Capture` / `ReleaseCapture`
  - no `WM_NCLBUTTONDOWN` / `SendMessage` drag handoff
  - no form drag `Timer`, `DispatcherTimer`, `Task.Delay`, or queued movement callbacks
- The main form is moved by the normal WinForms/native title-bar drag path.
- The visible "window keeps moving after release" behavior is therefore not a stale drag flag in form code.
- The more plausible cause is UI-thread backlog during native drag:
  - `interface` introduced a heavy owner-draw tree in `Controls/KnowledgeBaseTreeView.cs`
  - later tree fixes added synchronous repaint pressure through `Update()`
  - when the OS is already repainting the form during title-bar movement, this extra work can make the window visually lag behind the actual mouse-release moment
  - that lag looks like post-release inertia even though the OS drag loop has already ended

# Relevant history

- `b02861d` introduced the custom owner-draw tree path.
- `62c7de5` kept that direction and still forced synchronous tree repaint behavior.
- `33ceb2d` attempted to fix the symptom in `Forms/MainForm.cs` by suppressing redraw during native move/resize.
- `e7a5169` reverted that attempt after it made the UX worse:
  - client area could disappear into a gray background
  - dragging became slower
  - the underlying lag symptom was still present

# Current local fix

- `Controls/KnowledgeBaseTreeView.cs`
  - removes the owner-draw tree implementation
  - returns to a lightweight native `TreeView`
  - keeps visual cleanup such as no border/lines and consistent sizing
  - uses `StateImageList` for expand/collapse chevrons instead of per-row custom painting
- `UiServices/KnowledgeBaseTreeViewService.cs`
  - stops forcing synchronous `treeView.Update()` in `RefreshTreeViewVisuals(...)`
  - assigns valid state-image indexes while building nodes
- `UiServices/KnowledgeBaseTreeNodeVisuals.cs`
  - provides the state-image list for tree chevrons
  - includes a blank slot plus collapsed/expanded glyphs so `StateImageIndex` values `0/1/2` are valid

# Why the previous fix failed

- It targeted `MainForm` redraw suppression instead of the heavy repaint source.
- It interfered with normal client-area rendering during native window movement.
- It treated the bug like a drag-state problem, but the codebase does not have a manual form drag state to reset.
- As a result it added rendering artifacts without eliminating the UI-thread backlog that likely causes the symptom.

# Product / roadmap state

- `AKB5` remains a WinForms desktop app on `.NET 8`.
- `Phase 0` complete.
- `Phase 1` complete.
- `Phase 2` complete.
- `Phase 3` complete.
- Next unfinished roadmap phase: `Phase 3B`.
- JSON remains the source of truth.
- Excel workbook format remains `v3`.

# Important files

- `Forms/MainForm.cs`
- `Forms/MainForm.Events.cs`
- `Controls/KnowledgeBaseTreeView.cs`
- `UiServices/KnowledgeBaseTreeViewService.cs`
- `UiServices/KnowledgeBaseTreeNodeVisuals.cs`
- `Services/KnowledgeBaseWindowLayoutStateService.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseWindowLayoutStateServiceTests.cs`
- `Roadmap.md`

# Known risks / open questions

- Manual verification of the drag symptom is still required on Windows.
- If lag still reproduces after this tree simplification, the next inspection target should be broader paint pressure in the main form during native move/paint, not drag-state logic.
- `README.md` still lags behind the actual typed-node / typed-composition implementation state.

# Recommended next step

- Run the desktop app and manually reproduce the reported scenario:
  - drag the main window rapidly by the title bar
  - release the left mouse button while still moving the cursor
  - confirm the window stops immediately
  - confirm the client area stays visible and does not turn gray
- If manual verification passes, commit and push the current tree-performance fix.
- After stabilization, continue roadmap work from `Phase 3B`.
