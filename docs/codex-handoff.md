# Current State

Last updated: `2026-05-20`

## Current objective

Prepare the next `Net` branch network-passport manual-review package for user review.

The current local package improves manual entry and review without adding import/scan automation:

- `Добавить похожее` / `Добавить похожий` actions for devices, interfaces, and connections;
- inline `Проверка` columns with duplicate hints for repeated device PROFINET-name/MAC, interface IP/MAC/port, and connection cable labels.

This package is implemented locally and verified, but it is not manually accepted yet and must not be committed or pushed without a new explicit request.

## Current repo state

- Repository root: `C:\Users\Olga\AKB5`
- Active branch for this task: `Net`
- Tracking branch: `origin/Net`
- Current expected head: `207b6b1 Improve network passport review ergonomics`
- Current startup check on 2026-05-20:

```text
## Net...origin/Net
207b6b1 (HEAD -> Net, origin/Net) Improve network passport review ergonomics
```

- Working tree was clean at the startup check, then became dirty from the inherited doc-only context refresh and the current local Net package.
- The latest accepted Net review-ergonomics package was committed and pushed at the user's explicit request in the previous chat.
- Do not run `git commit` or `git push` for future changes unless the user explicitly asks in the current chat.
- Current application changes are local only and are limited to Network UI/state/test files; no real `.akb`, JSON data, or Excel files were edited.

Current substantial dirty files for the local package:

- `Controls/KnowledgeBaseNetworkScreenControl.cs`
- `Forms/MainForm.Events.cs`
- `Forms/MainForm.Network.cs`
- `Services/KnowledgeBaseNetworkStateService.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseNetworkStateServiceTests.cs`

The inherited context files are also dirty: `AGENTS.md`, `Roadmap.md`, `docs/codex-handoff.md`, `docs/plans.md`, `docs/decision-log.md`, and `docs/lessons-learned.md`.

## Latest completed package

Commit `207b6b1 Improve network passport review ergonomics` contains the manually accepted Stage 5F / Stage 6 / Stage 7 network passport review package:

- richer connection endpoint text with device/interface/IP/`MPI/DP/PN`;
- visible connection `Длина` column;
- copy-friendly connection row actions: row button, `Ctrl+C`, and context-menu copy for selected row, visible rows, interface A, and interface B;
- visible interface `Скорость` and `Примечание` columns;
- copy-friendly interface summaries and context-menu copy for row, visible rows, interface summary, IP, and `MPI/DP/PN`;
- visible device `Производитель` and `Место` columns;
- copy-friendly device row actions and context-menu copy for row, visible rows, device summary, PROFINET-name, and MAC;
- passport-wide `Копировать видимое` export for currently visible device/interface/connection rows with table headers;
- per-grid copy of visible rows with headers;
- persistent selected-row visibility and row tooltips for long manual-review values;
- README/Roadmap updates for the accepted Net state;
- `KnowledgeBaseNetworkStateServiceTests` coverage for richer endpoint/device/interface state text.

## Current local package pending review

The current uncommitted package adds manual-entry speed and inline duplicate hints:

- device/interface/connection action panels now expose `Добавить похожее` / `Добавить похожий`;
- the same add-similar actions are available from row context menus;
- add-similar device drafts copy stable context fields such as role/vendor/model/order/firmware/location/cabinet/notes but leave unique identity fields blank;
- add-similar interface drafts copy device, subnet/gateway/VLAN/protocol/speed/medium/notes but leave unique address/port/MAC fields blank;
- add-similar connection drafts copy endpoints and cable metadata while leaving the cable label blank;
- Network state rows now expose inline `Проверка` text for narrow duplicate hints;
- Network list copies/visible export include the new `Проверка` column.

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

## Files already relevant to the task

- `Controls/KnowledgeBaseNetworkScreenControl.cs`
- `Forms/MainForm.Network.cs`
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

Current local package validation in this chat:

- focused Network tests: `41/41`;
- full Release tests: `433/433`;
- `git diff --check`: passed;
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed;
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed;
- app build: passed;
- isolated Release artifact: `C:\Users\Olga\AKB5\artifacts\build-check\network-manual-entry-hints-20260520-132919\asutpKB.exe`;
- non-invasive/offscreen layout smoke: `artifacts\layout-smoke\network-manual-entry-hints`, passed.

The offscreen smoke project restore/build emitted pre-existing analyzer warnings when run through `dotnet run`; the smoke itself completed successfully. The official app format/build/test commands above were run with the documented analyzer suppression where applicable.

## Known risks / open questions

- The current local package needs manual review before commit/push.
- `Добавить похожее` for connections opens a copied endpoint pair as a draft; if the user saves without changing endpoints, the existing duplicate-pair validation will reject it.
- If a default `Release` build output is locked by a running `asutpKB.exe`, use an isolated output path for verification.
- Keep diagnostics compact and avoid broad log scans unless a specific failure requires them.

## Recommended next step

Hand off the isolated executable for manual review:

- `C:\Users\Olga\AKB5\artifacts\build-check\network-manual-entry-hints-20260520-132919\asutpKB.exe`

After manual acceptance, wait for an explicit current-chat request before `git commit` / `git push`. If manual review finds layout or behavior issues, fix them in this same coherent package before moving to the next Net direction.

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
