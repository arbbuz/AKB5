# Current State

Last updated: `2026-05-20`

## Current objective

Continue AKB5 Network work from the accepted `Net` baseline while keeping visual/UI polish isolated in the separate design worktree.

Current local `Net` review package: enforce existing Network field validation at the mutation-service boundary so invalid interface IP/mask/gateway values and invalid connection lengths cannot bypass the dialogs.

The next `Net` task should stay inside manual-entry / manual-review ergonomics unless the user explicitly broadens scope. Prefer logic, validation, persistence, and focused-test work that does not collide with the active `design/network-ui-polish` UI package.

## Current repo state

- Main Net worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Expected `HEAD` and `origin/Net`: `a89e593 Improve network passport manual entry hints`
- Current startup check on 2026-05-20:

```text
## Net...origin/Net
a89e593 (HEAD -> Net, origin/Net, design/network-ui-polish) Improve network passport manual entry hints
```

- The latest Net manual-entry hints package was accepted manually, committed, and pushed at the user's explicit request.
- Before the current implementation pass, handoff-related dirtiness was doc-only: `AGENTS.md`, `Roadmap.md`, `docs/codex-handoff.md`, `docs/plans.md`, `docs/decision-log.md`, `docs/lessons-learned.md`, and `docs/new-chat-handoff-2026-05-20-net-a89e593.md`.
- Current local uncommitted `Net` application/test changes are limited to `Services/KnowledgeBaseNetworkMutationService.cs` and `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkMutationServiceTests.cs`.
- Separate design worktree: `C:\Users\Olga\AKB5-design` on `design/network-ui-polish`, carrying the uncommitted Network review-filter/layout UI package for manual review.
- Do not run `git commit`, `git push`, `git merge`, `git rebase`, delete stash entries, or remove worktrees unless the user explicitly asks in the current chat.

## Latest completed package

Current local uncommitted review package:

- `KnowledgeBaseNetworkMutationService.UpsertInterface` now reuses `KnowledgeBaseNetworkFieldValidationService` for IP address, subnet mask, and gateway validation before accepting an interface draft.
- `KnowledgeBaseNetworkMutationService.UpsertConnection` now reuses the existing connection-field validator before accepting cable length.
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

- `C:\Users\Olga\AKB5` / `Net`: logic, models, services, validation, persistence, focused tests, and coherent manual-entry/manual-review behavior packages.
- `C:\Users\Olga\AKB5-design` / `design/network-ui-polish`: WinForms layout, visual ergonomics, review-filter UI polish, and offscreen layout-smoke.
- Do not mix the two worktrees unless the user explicitly asks to merge or coordinate them.

## Decisions already made

- Network passport CRUD is manual-entry first.
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
- `Forms/MainForm.Network.cs`
- `Forms/MainForm.Events.cs`
- `Forms/KnowledgeBaseNetworkDeviceDialog.cs`
- `Forms/KnowledgeBaseNetworkInterfaceDialog.cs`
- `Forms/KnowledgeBaseNetworkConnectionDialog.cs`
- `Forms/KnowledgeBaseNetworkFileReferenceDialog.cs`
- `Models/KbNetworkDevice.cs`
- `Models/KbNetworkInterface.cs`
- `Models/KbNetworkConnection.cs`
- `Models/KbNetworkFileReference.cs`
- `Services/KnowledgeBaseNetworkStateService.cs`
- `Services/KnowledgeBaseNetworkMutationService.cs`
- `Services/KnowledgeBaseNetworkFieldValidationService.cs`
- `Services/KnowledgeBaseNetworkPreviewService.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkStateServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkMutationServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkFieldValidationServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkPreviewServiceTests.cs`
- `ui-smoke-network-passport.ps1`
- `artifacts/layout-smoke/network-manual-entry-hints/` (ignored offscreen smoke artifact)

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

Validation completed for the current local mutation-service validation package on 2026-05-20:

- focused validation/mutation tests: `39/39`;
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet build asutpKB.csproj --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed;
- full tests: `438/438`;
- `git diff --check`: passed;
- no UI/layout files changed, so interactive UI-smoke and offscreen layout-smoke were not run.

## Known risks / open questions

- The design branch currently has uncommitted UI/UX work; keep it isolated from `Net` until the user asks to merge.
- If a default `Release` build output is locked by a running `asutpKB.exe`, use an isolated output path for verification.
- Keep diagnostics compact and avoid broad log scans unless a specific failure requires them.

## Recommended next step

Manual-review the current local `Net` mutation-service validation package. If it is accepted, request commit/push explicitly in the current chat; if not, refine the same package before starting another Network direction. Do not commit/push/merge without explicit current-chat approval.

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
