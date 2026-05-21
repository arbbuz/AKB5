# Current State

Last updated: `2026-05-21`

## Current objective

Continue AKB5 Network work from the accepted `Net` topology-overview baseline. The user approved bringing the safe UI/design work from `C:\Users\Olga\AKB5-design` into `Net` together with `resources\fonts\MaterialSymbolsOutlined.ttf`.

The latest accepted `Net` baseline is `5348f4f Add network topology overview`; it is committed and pushed to `origin/Net`.

Current working tree contains an uncommitted design-port package on top of `5348f4f`; it passed local validation and manual review, but is not committed or pushed.

The current direction for `Сеть` is topology overview first. The overview starts from already created AKB5 tree objects and existing network records; PRONETA/CSV, live scan, import, and automatic assignment remain out of scope unless explicitly approved later.

## Current repo state

- Main Net worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Expected `HEAD` and `origin/Net`: `5348f4f Add network topology overview`
- Current startup check on 2026-05-21:

```text
## Net...origin/Net
5348f4f (HEAD -> Net, origin/Net) Add network topology overview
```

- The topology overview package was accepted manually, committed, and pushed at the user's explicit request.
- The main `Net` worktree has pre-existing documentation/context edits that were not part of commit `5348f4f`; these were not reverted.
- The main `Net` worktree also now has an uncommitted design-port package copied from `design/network-ui-polish`: light-surface layout, toolbar/menu icon rendering, thin splitter/tree visual polish, Network panel polish, warning-row highlight support, and `resources\fonts\MaterialSymbolsOutlined.ttf`.
- The design-port deliberately did not reintroduce the old top passport filter/copy panel or the design-branch review-filter checkbox/buttons.
- Separate design worktree: `C:\Users\Olga\AKB5-design` on `design/network-ui-polish`; do not keep reading or changing it unless the user asks for another comparison.
- Do not run `git commit`, `git push`, `git merge`, `git rebase`, delete stash entries, or remove worktrees unless the user explicitly asks in the current chat.

## Latest completed package

Commit `5348f4f Add network topology overview` contains the accepted first topology-overview slice:

- `Сеть` opens on a new `Обзор` tab instead of the old `Паспорт` table view;
- the overview shows only `Объекты / устройства` and `Топология`;
- the previous overview-bottom `Файлы и снимки` block and separate `Проверка` block are not shown on `Обзор`;
- the old `Паспорт`, `Файлы`, and `Предпросмотр` tabs remain available;
- the old passport filter/copy panel is no longer shown above the passport tables;
- the overview uses already created AKB5 tree objects and existing network devices/interfaces/connections without adding persisted fields.

Uncommitted design-port package on top of `5348f4f` currently contains:

- copied visual/layout work from `design/network-ui-polish` into `Net` without `git merge` or `cherry-pick`;
- `MaterialSymbolsOutlined.ttf` copied as a committed asset candidate and wired through `asutpKB.csproj`;
- `MainForm` toolbar/menu icon rendering via Material Symbols font;
- thin main splitter and light-surface WinForms layout polish;
- Network screen modern border/section panels, overview-first tab order preserved, and warning rows highlighted;
- `KnowledgeBaseNetworkState.ReviewWarningCount` / per-table warning counts for UI support;
- focused state-service test coverage for warning counts.

Commit `b2ea12e Enforce network passport mutation validation` contains the accepted service-boundary validation package:

- `KnowledgeBaseNetworkMutationService.UpsertInterface` reuses `KnowledgeBaseNetworkFieldValidationService` for IP address, subnet mask, and gateway validation before accepting an interface draft.
- `KnowledgeBaseNetworkMutationService.UpsertConnection` reuses the existing connection-field validator before accepting cable length.
- Mutation-service tests cover invalid IP, non-contiguous mask, invalid gateway, and invalid cable length failures.

Commit `a89e593 Improve network passport manual entry hints` contains the accepted manual-entry speed and inline duplicate-hints package:

- device/interface/connection action panels expose `Добавить похожее` / `Добавить похожий`;
- the same add-similar actions are available from row context menus;
- add-similar device drafts copy stable context fields such as linked node, role, vendor, model, order number, firmware, location/cabinet, and notes while leaving unique identity fields blank;
- add-similar interface drafts copy device, subnet/gateway/VLAN/protocol/speed/medium/notes while leaving unique address/port/MAC/MPI fields blank;
- add-similar connection drafts copy endpoints and cable metadata while leaving the cable label blank;
- Network state rows expose inline `Проверка` text for narrow duplicate hints;
- device duplicate hints cover PROFINET-name and MAC;
- interface duplicate hints cover IP, MAC, and the same port/name on the same device;
- connection duplicate hints cover duplicate cable labels;
- Network list copies and visible export include the `Проверка` column.

Commit `207b6b1 Improve network passport review ergonomics` is the previous accepted review package:

- richer connection endpoint text with device/interface/IP/`MPI/DP/PN`;
- visible connection `Длина`, interface `Скорость` / `Примечание`, and device `Производитель` / `Место` columns;
- copy-friendly row/context-menu actions and visible-row exports for devices, interfaces, and connections;
- passport-wide `Копировать видимое` export with table headers;
- persistent selected-row visibility and row tooltips for long manual-review values.

## Branch split

- `C:\Users\Olga\AKB5` / `Net`: accepted Network topology overview plus logic, models, services, validation, persistence, focused tests, and coherent manual-entry/manual-review behavior packages.
- `C:\Users\Olga\AKB5-design` / `design/network-ui-polish`: WinForms layout, visual ergonomics, review-filter UI polish, and offscreen layout-smoke.
- Do not mix the two worktrees unless the user explicitly asks to merge or coordinate them.

## Decisions already made

- Network overview is topology-first for the first screen; detailed manual editing remains available in secondary tabs/dialogs.
- The first accepted overview uses already created AKB5 tree objects as the initial source, with existing network devices/interfaces/connections when they exist.
- The approved practical-minimum fields for the first visible overview are object/device, type/role, IP, PROFINET-name, and cabinet/node placement. More detailed values belong in secondary editing surfaces unless explicitly approved for the overview.
- Do not reintroduce the old top filter/copy panel, overview `Файлы и снимки`, or overview `Проверка` block without a new explicit request.
- Keep the existing file-reference workflow intact: image preview remains in-form, and `Open original` remains the reliable source action.
- PDF network scheme references are accepted as metadata/`Open original` sources.
- Embedded PDF preview/rendering is not approved.
- Do not start OCR/PDF auto-import, PRONETA/CSV import, live scan, plan/fact comparison, data-quality issue/problem panel, AKB5-driven IP assignment, or AKB5-driven PROFINET-name assignment.
- Do not split future Net manual-entry/UI work into tiny micro-stages; bundle closely related manual-review/UI refinements into one coherent package.
- For WinForms Network UI changes, use focused tests while developing and run one full tests/build/offscreen layout-smoke cycle when a package is ready.
- Do not run interactive UI-smoke unless the user explicitly asks. Prefer non-invasive/offscreen layout-smoke.
- If Codex shows no progress for about 2-3 minutes after a completed tool result, treat it as an orchestration stall and recover early.

## Files already relevant to Network work

- `Controls/KnowledgeBaseNetworkScreenControl.cs`
- `Controls/KnowledgeBaseInfoScreenControl.cs`
- `Controls/KnowledgeBaseThinSplitContainer.cs`
- `Controls/KnowledgeBaseTreeView.cs`
- `Forms/MainForm.Network.cs`
- `Forms/MainForm.Events.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.WorkflowContexts.cs`
- `Forms/MainForm.cs`
- `Forms/KnowledgeBaseNetworkDeviceDialog.cs`
- `Forms/KnowledgeBaseNetworkInterfaceDialog.cs`
- `Forms/KnowledgeBaseNetworkConnectionDialog.cs`
- `Forms/KnowledgeBaseNetworkFileReferenceDialog.cs`
- `Models/KbNetworkDevice.cs`
- `Models/KbNetworkInterface.cs`
- `Models/KbNetworkConnection.cs`
- `Models/KbNetworkFileReference.cs`
- `Services/KnowledgeBaseNetworkStateService.cs`
- `Services/KnowledgeBaseFormStateService.cs`
- `Services/KnowledgeBaseNetworkMutationService.cs`
- `Services/KnowledgeBaseNetworkFieldValidationService.cs`
- `Services/KnowledgeBaseNetworkPreviewService.cs`
- `UiServices/KnowledgeBaseTreeNodeVisuals.cs`
- `asutpKB.csproj`
- `resources/fonts/MaterialSymbolsOutlined.ttf`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkStateServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkMutationServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkFieldValidationServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkPreviewServiceTests.cs`
- `ui-smoke-network-passport.ps1`
- `artifacts/layout-smoke/network-design-port-20260521`
- `artifacts/build-check/network-overview-topology-only-20260521-0125/asutpKB.exe`

## Validation status

Validation completed for `a89e593` before commit/push:

- focused Network tests: `41/41`;
- full Release tests: `433/433`;
- `git diff --check`: passed;
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed;
- app build: passed;
- isolated Release artifact at the time of review: `C:\Users\Olga\AKB5\artifacts\build-check\network-manual-entry-hints-20260520-132919\asutpKB.exe`;
- non-invasive/offscreen layout smoke: `artifacts\layout-smoke\network-manual-entry-hints`, passed.

Validation completed for `b2ea12e` before commit/push on 2026-05-20:

- focused validation/mutation tests: `39/39`;
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet build asutpKB.csproj --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed;
- full tests: `438/438`;
- `git diff --check`: passed;
- no UI/layout files changed, so interactive UI-smoke and offscreen layout-smoke were not run.

Validation completed for `5348f4f` before commit/push on 2026-05-21:

- app/core/tests `dotnet format`: passed;
- Debug build: passed;
- Release build in isolated output: `C:\Users\Olga\AKB5\artifacts\build-check\network-overview-topology-only-20260521-0125\asutpKB.exe`;
- focused Network tests: `52/52`;
- full Release tests: `438/438`;
- `git diff --check`: passed;
- interactive UI-smoke was not run because it clicks the real app and can mutate test network records.

Validation completed for the uncommitted design-port package on 2026-05-21:

- Debug app build: passed;
- focused Network tests: `46/46`;
- app/core/tests `dotnet format --verify-no-changes`: passed;
- Release app build: passed;
- full Release tests: `438/438`;
- `git diff --check`: passed;
- non-invasive/offscreen layout smoke: `artifacts\layout-smoke\network-design-port-20260521`, passed after narrowing it to Network overview/warning-highlight checks and direct Material Symbols icon-rendering check;
- interactive UI-smoke was not run;
- manual review passed: main functionality works; small UI blemishes are accepted as deferred follow-up work.

## Known risks / open questions

- The design-port package is uncommitted in `Net`; it passed manual review, but commit/push still require explicit current-chat approval.
- The design branch may still contain its own history/untracked doc file; do not treat it as the active worktree unless the user asks.
- The accepted overview is intentionally minimal; future topology refinements should be proposed and agreed before implementation.
- The old review-filter UI from `design/network-ui-polish` was intentionally not carried into `Net` because the accepted overview package removed the top passport filter/copy panel.
- Small UI blemishes remain and are intentionally deferred for later polishing.
- If a default `Release` build output is locked by a running `asutpKB.exe`, use an isolated output path for verification.
- Keep diagnostics compact and avoid broad log scans unless a specific failure requires them.

## Recommended next step

Next `Net` work should either commit/push the accepted uncommitted design-port package after explicit current-chat approval, or address the deferred small UI blemishes as a follow-up package. Do not add PRONETA/CSV parsing, live scan, automatic assignment, a separate overview quality panel, or extra main-screen fields without explicit approval.

## Commands to run before finishing future implementation work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --no-restore /p:RunAnalyzers=false /p:WarningLevel=0
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --no-restore --logger "console;verbosity=minimal" /p:RunAnalyzers=false /p:WarningLevel=0
git diff --check
```
