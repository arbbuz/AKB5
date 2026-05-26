# Current State

Last updated: `2026-05-26`

## Current objective

Current Network topology canvas zoom/viewport/full-label package on `Net` is accepted for commit/push by the user in the current chat. After this package is pushed, there is no active implementation step waiting.

Previous committed/pushed package includes:

- Startup timing checkpoints are logged through the existing `IAppLogger` as `StartupTiming` events.
- Timing checkpoints cover `Program.Main`, `MainForm` construction, startup storage selection, synchronous data load, SQLite schema/load/normalize phases, and file workflow storage load.
- Existing startup behavior remains synchronous; this package measures where time is spent and provides a faster package shape, but does not yet move database loading off the UI startup path.
- `scripts\publish.ps1` now supports `-PublishMode SingleFile|Folder` and `-ReadyToRun`.
- New `scripts\publish-fast.ps1` / `scripts\publish-fast.cmd` build a self-contained folder publish with `PublishSingleFile=false` and `PublishReadyToRun=true`.
- Fast publish output is `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe`.
- Startup logs are written to `%LocalAppData%\AKB5\logs\app-yyyy-MM-dd.ndjson`; filter event name `StartupTiming`.
- Network topology link-type selection no longer uses a long ComboBox in the equipment toolbar; it is now a bottom strip below the topology viewport.
- The link-type strip uses full button labels such as `Profibus, оптоволокно`, `Profibus, медь`, `MPI, медь`, `Profinet, оптоволокно`, and `Profinet, медь`, with the real line color/dash sample. Button widths are measured from the full regular/bold text instead of fixed short widths, so labels must not be ellipsized.
- Clicking an existing link selects it, synchronizes the strip to that link kind, and clicking a strip button immediately changes the selected link kind. With no selected link, the strip still chooses the kind for the next new link.

Current uncommitted follow-up:

- The Network topology field now uses a scrollable viewport around a logical canvas instead of clipping content to the current screen size.
- `Ctrl + mouse wheel` zooms the topology canvas around the mouse pointer; normal wheel scrolling still moves the viewport vertically.
- Element/link hit testing, dragging, context-menu add position, link endpoint dragging, and link-kind strip behavior are translated through the current zoom.
- Node IP/name text is drawn through the same scaled graphics pipeline as cards/icons, so labels scale with the topology instead of staying at screen size.
- The link-kind selector is now a thin fixed strip under the scrollable viewport, so it stays visible without covering topology nodes or links.

Do not commit, push, merge, rebase, or create/remove remote branches without explicit approval in the current chat.

## Current repo state

- Main worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Base commit before this follow-up: `40be905 Update handoff after startup publish package`.
- No real `.akb` or JSON user data files were edited.
- Current fast-publish review executable: `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe`.
- Current Network full-label build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\network-topology-labels\asutpKB.exe`.
- Fast publish is a folder package with 273 files, not a single-file exe.

## Current package

Current changed tracked files:

- `Controls/KnowledgeBaseNetworkTopologyScreenControl.cs`
- `docs/codex-handoff.md`
- `docs/decision-log.md`
- `docs/plans.md`

Ignored validation artifacts:

- `artifacts\build-check\startup-timing\asutpKB.exe`
- `artifacts\build-check\network-link-kind-palette\asutpKB.exe`
- `artifacts\build-check\network-topology-zoom\asutpKB.exe`
- `artifacts\build-check\network-topology-labels\asutpKB.exe`
- `artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`
- `artifacts\publish-fast\win-x64\asutpKB.exe`

## Validation status

Validation completed in `C:\Users\Olga\AKB5`:

- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\startup-timing /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-fast.ps1`: passed; output `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe`.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed, 407 tests.
- `git diff --check`: passed with CRLF normalization warnings only.
- Manual fast-publish review: accepted. User launched `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe` several times and reported that startup feels faster.
- Startup log review in `%LocalAppData%\AKB5\logs\app-2026-05-26.ndjson`: latest warm launches reached `mainform-constructor-completed` in about `670-717 ms`; SQLite `sqlite-load-total` was about `173-183 ms`. One earlier cold launch reached `mainform-data-loaded` in about `12.5 s`, so repeated cold-start slowness should be investigated inside UI session application/binding rather than SQLite read itself.
- Network link-type palette follow-up: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore` passed.
- Network link-type palette follow-up: `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-kind-palette /p:RunAnalyzers=false /p:WarningLevel=0` passed with 0 warnings and 0 errors.
- Network link-type palette follow-up: `dotnet run --project artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj --configuration Release /p:RunAnalyzers=false /p:WarningLevel=0` passed and produced `artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`; the smoke now verifies selecting a link and changing it through the palette.
- Network link-type palette follow-up: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-fast.ps1` re-ran successfully after the UI change; fast publish output now includes the new palette.
- Network link-type palette follow-up: `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0` passed, 407 tests.
- Network link-type palette follow-up: `git diff --check` passed with CRLF normalization warnings only.
- Network zoom/viewport follow-up: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore` passed.
- Network zoom/viewport follow-up: `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore` passed.
- Network zoom/viewport follow-up: `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore` passed.
- Network zoom/viewport follow-up: `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom /p:RunAnalyzers=false /p:WarningLevel=0` passed with 0 warnings and 0 errors.
- Network zoom/viewport follow-up: `dotnet run --project artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj --configuration Release /p:RunAnalyzers=false /p:WarningLevel=0` passed; smoke now verifies link-kind palette interaction, a small viewport with a low element needing vertical scroll, and zoom-out behavior.
- Network zoom/viewport follow-up: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-fast.ps1` passed; output `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe`.
- Network zoom/viewport follow-up: `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0` passed, 407 tests.
- Network zoom/viewport follow-up: `git diff --check` passed with CRLF normalization warnings only.
- Text scaling fix follow-up: after replacing topology-card `TextRenderer` calls with scaled `Graphics.DrawString`, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom /p:RunAnalyzers=false /p:WarningLevel=0`, layout-smoke, `dotnet test` 407 tests, and `scripts\publish-fast.ps1` passed. One earlier parallel build attempt failed only because `obj\Release\net8.0-windows\asutpKB.dll` was temporarily locked; a sequential rerun passed.
- Bottom link-kind strip follow-up: after moving the link-type selector out of the canvas overlay and into a fixed bottom strip, `dotnet format` for app/core/tests passed, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom /p:RunAnalyzers=false /p:WarningLevel=0` passed with 0 warnings and 0 errors, layout-smoke passed and refreshed `artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`, `dotnet test` passed 407 tests, `scripts\publish-fast.ps1` passed, and `git diff --check` passed with CRLF normalization warnings only.
- Full link-kind labels follow-up: `опт.` abbreviations were removed from the bottom strip and selected-link status; the strip now uses full labels such as `Profibus, оптоволокно` and `Profinet, оптоволокно`. After manual review found remaining ellipses inside fixed-width buttons, the button width calculation was changed to measure the full regular and selected bold label text plus the line sample area. `rg -n "опт\." Controls\KnowledgeBaseNetworkTopologyScreenControl.cs docs\codex-handoff.md docs\plans.md docs\decision-log.md` found no matches. `dotnet format` for app/core/tests passed, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-labels /p:RunAnalyzers=false /p:WarningLevel=0` passed with 0 warnings and 0 errors, layout-smoke passed and refreshed the PNG with full labels visible, `dotnet test` passed 407 tests, and `scripts\publish-fast.ps1` passed.
- Final label-width follow-up: after manual review still showed a clipped last character in the selected `Profinet, медь` button, the link-kind button sizing added a measured-text safety margin while keeping the total strip narrow enough for ordinary widths. App format, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-labels /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false`, layout-smoke, and `scripts\publish-fast.ps1` passed. The refreshed smoke PNG shows all five link-kind buttons in one row without clipped labels or an internal strip scrollbar at the smoke width. Full core tests were not rerun after this final padding-only paint/layout adjustment.

## Decisions already made

- Work only on `Net / origin/Net`.
- Do not commit or push without fresh direct approval in the current chat.
- Keep the default `scripts\publish.ps1` mode backward compatible as `SingleFile`.
- Use `scripts\publish-fast.ps1` as the accepted faster working package: folder publish, self-contained, ReadyToRun, no single-file extraction.
- Keep startup diagnostics in file logs, not visible UI, so normal operators are not interrupted.
- For Network topology, choose and edit link types through the full-label bottom strip below the topology viewport rather than through a toolbar ComboBox or a floating overlay; do not add hover/popup tooltips.
- For Network topology, keep stored element coordinates logical and resolution-independent; adapt to screen size with viewport scrolling and zoom instead of rewriting coordinates.

## Files already relevant to the task

- `Program.cs`
- `Controls\KnowledgeBaseNetworkTopologyScreenControl.cs`
- `Forms/MainForm.cs`
- `Services\KnowledgeBaseFileWorkflowService.cs`
- `Services\SqliteKnowledgeBaseStorageService.cs`
- `Services\FileAppLogger.cs`
- `scripts\publish.ps1`
- `scripts\publish-fast.ps1`
- `scripts\publish-fast.cmd`
- `docs/codex-handoff.md`
- `docs/plans.md`
- `docs/decision-log.md`

## Known risks / open questions

- Manual comparison is qualitative so far: fast publish feels faster to the user, but no external stopwatch trace compares old single-file vs fast folder publish.
- Fast publish has many files. It should be copied/launched as a whole folder, not as a lone `asutpKB.exe`.
- Startup timing logs show elapsed checkpoints after the app starts; they do not measure Windows Defender/SmartScreen time before managed code begins.
- If `mainform-data-loaded` repeatedly dominates cold startup while SQLite remains fast, first add narrower timing around loaded-session UI application/binding; then consider deferred/asynchronous database load after first form display.
- Manual review should check that the bottom Network link-type strip with full labels is readable, does not feel too tall, and does not hide needed viewport space on the user's real diagram/window size.
- Manual review should check `Ctrl + wheel` zoom, ordinary wheel/scrollbar access to low elements, dragging elements while zoomed, right-click add position while zoomed/scrolled, and selected-link editing through the bottom strip.

## Recommended next step

After the authorized commit/push completes, wait for the next user request. If startup still feels slow after a truly cold launch, inspect another `StartupTiming` run and add narrower checkpoints around UI session application/binding before changing startup architecture.

## Commands to run before finishing future implementation work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\startup-timing /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-kind-palette /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-labels /p:RunAnalyzers=false /p:WarningLevel=0
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-fast.ps1
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0
dotnet run --project artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj --configuration Release /p:RunAnalyzers=false /p:WarningLevel=0
git diff --check
```
