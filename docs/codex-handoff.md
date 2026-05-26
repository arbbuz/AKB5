# Current State

Last updated: `2026-05-26`

## Current objective

Current startup diagnostics / fast publish / Network topology link-type selector package is accepted, committed, and pushed on `Net`. There is no active implementation step waiting after this package.

Committed/pushed package includes:

- Startup timing checkpoints are logged through the existing `IAppLogger` as `StartupTiming` events.
- Timing checkpoints cover `Program.Main`, `MainForm` construction, startup storage selection, synchronous data load, SQLite schema/load/normalize phases, and file workflow storage load.
- Existing startup behavior remains synchronous; this package measures where time is spent and provides a faster package shape, but does not yet move database loading off the UI startup path.
- `scripts\publish.ps1` now supports `-PublishMode SingleFile|Folder` and `-ReadyToRun`.
- New `scripts\publish-fast.ps1` / `scripts\publish-fast.cmd` build a self-contained folder publish with `PublishSingleFile=false` and `PublishReadyToRun=true`.
- Fast publish output is `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe`.
- Startup logs are written to `%LocalAppData%\AKB5\logs\app-yyyy-MM-dd.ndjson`; filter event name `StartupTiming`.
- Network topology link-type selection no longer uses a long ComboBox in the equipment toolbar; it is now a grouped floating palette inside the topology canvas.
- The link-type palette groups `Profibus`, `MPI`, and `Profinet`; medium buttons use `опт.` / `медь` labels and draw the real line color/dash sample.
- Clicking an existing link selects it, synchronizes the palette to that link kind, and clicking a palette button immediately changes the selected link kind. With no selected link, the palette still chooses the kind for the next new link.

Do not commit, push, merge, rebase, or create/remove remote branches without explicit approval in the current chat.

## Current repo state

- Main worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Latest pushed code package commit: `d468e12 Improve startup publish and network link selector`.
- No real `.akb` or JSON user data files were edited.
- Current fast-publish review executable: `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe`.
- Current Network UI review executable: `C:\Users\Olga\AKB5\artifacts\build-check\network-link-kind-palette\asutpKB.exe`.
- Fast publish is a folder package with 273 files, not a single-file exe.

## Current package

Committed package files:

- `Program.cs`
- `Controls/KnowledgeBaseNetworkTopologyScreenControl.cs`
- `Forms/MainForm.cs`
- `Services/KnowledgeBaseFileWorkflowService.cs`
- `Services/SqliteKnowledgeBaseStorageService.cs`
- `scripts/publish.ps1`
- `scripts/publish-fast.ps1`
- `scripts/publish-fast.cmd`
- `docs/codex-handoff.md`
- `docs/decision-log.md`
- `docs/plans.md`

Ignored validation artifacts:

- `artifacts\build-check\startup-timing\asutpKB.exe`
- `artifacts\build-check\network-link-kind-palette\asutpKB.exe`
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

## Decisions already made

- Work only on `Net / origin/Net`.
- Do not commit or push without fresh direct approval in the current chat.
- Keep the default `scripts\publish.ps1` mode backward compatible as `SingleFile`.
- Use `scripts\publish-fast.ps1` as the accepted faster working package: folder publish, self-contained, ReadyToRun, no single-file extraction.
- Keep startup diagnostics in file logs, not visible UI, so normal operators are not interrupted.
- For Network topology, choose and edit link types through the grouped floating palette on the canvas rather than through a toolbar ComboBox; do not add hover/popup tooltips.

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
- The floating Network link-type palette intentionally overlays the top-right of the topology canvas. Manual review should check that this placement feels acceptable on real diagrams and window sizes.

## Recommended next step

Wait for the next user request. If startup still feels slow after a truly cold launch, inspect another `StartupTiming` run and add narrower checkpoints around UI session application/binding before changing startup architecture.

## Commands to run before finishing future implementation work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\startup-timing /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-kind-palette /p:RunAnalyzers=false /p:WarningLevel=0
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-fast.ps1
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0
dotnet run --project artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj --configuration Release /p:RunAnalyzers=false /p:WarningLevel=0
git diff --check
```
