# Current State

Last updated: `2026-05-27`

## Current objective

Current active work is a follow-up on `Net` after pushed commit `2c9d598 Improve network topology viewport controls`: add compact visible Network topology zoom controls, show Network topology ET objects generically as `ET`, keep one optional additional IP inside the existing Network element dialog with duplicate checks, add a text-card Network topology object for external system links, keep `docs/decision-log.md` compact, and add a repo-level shell guard for small/localized tasks. The user manually accepted the current UI package and explicitly requested commit/push in the current chat.

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

Current follow-up package:

- The Network topology field now uses a scrollable viewport around a logical canvas instead of clipping content to the current screen size.
- `Ctrl + mouse wheel` zooms the topology canvas around the mouse pointer; normal wheel scrolling still moves the viewport vertically.
- The bottom strip now includes compact fixed-right zoom controls: `-`, an editable/drop-down percent field, and `+`; choosing/typing `100%` returns to the original scale. The separate `Масштаб` label and the internal link-type strip scrollbar were removed so the row stays compact.
- In the Network tab only, visible object labels and new-object default prefixes changed from `ET200` to `ET`; the stored enum remains `KbNetworkElementKind.Et200` for compatibility with existing saved topology data.
- The existing Network element dialog now has an optional `Доп. IP` row. Leaving it empty removes the additional IP; entering a value keeps duplicate checks across primary and additional IP addresses on all topology objects. The object context menu no longer exposes separate add/edit additional-IP dialogs.
- Network topology now has a `Внешняя связь` object kind for external systems. It stores its visible text in the existing element `Name` field, appears in the toolbar/context add menu/type dropdown, draws as a card with an internal text field instead of an IP/device-icon card, and uses a text-entry dialog mode without `IP` / `Доп. IP` fields. New external-link elements open with an empty `Текст` field instead of the old default `Внешняя связь-01` text.
- `docs/decision-log.md` was pruned from a dated project archive into 65 lines of domain-grouped durable decisions; build/test history and temporary phase status were removed from that file.
- `scripts/codex-safe.ps1` was added as a repo-level shell guard: it blocks broad `git diff`, full `decision-log` reads, raw/wildcard docs reads, broad repo `rg`, recursive unfiltered enumeration, and destructive git/delete patterns; it also truncates command output.
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
- Current Network ET-label build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\network-topology-et-label\asutpKB.exe`.
- Current Network external-connection build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\network-topology-external-connection\asutpKB.exe`.
- Current Network external-connection text-dialog review executable: `C:\Users\Olga\AKB5\artifacts\publish-fast-external-text\win-x64\asutpKB.exe`.
- Current Network final build-check executable: `C:\Users\Olga\AKB5\artifacts\build-check\network-topology-final\asutpKB.exe`.
- Standard `artifacts\publish-fast\win-x64` was refreshed successfully after the final text-entry follow-up.
- Fast publish is a folder package with 273 files, not a single-file exe.

## Current package

Current changed tracked files:

- `Controls/KnowledgeBaseNetworkTopologyScreenControl.cs`
- `Models/KbNetworkTopology.cs`
- `Services/KnowledgeBaseDataService.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseDataServiceTests.cs`
- `docs/codex-handoff.md`
- `docs/decision-log.md`
- `docs/plans.md`

Ignored validation artifacts:

- `artifacts\build-check\startup-timing\asutpKB.exe`
- `artifacts\build-check\network-link-kind-palette\asutpKB.exe`
- `artifacts\build-check\network-topology-zoom\asutpKB.exe`
- `artifacts\build-check\network-topology-labels\asutpKB.exe`
- `artifacts\build-check\network-topology-zoom-controls\asutpKB.exe`
- `artifacts\build-check\network-topology-et-label\asutpKB.exe`
- `artifacts\build-check\network-topology-additional-ip\asutpKB.exe`
- `artifacts\build-check\network-topology-additional-ip-layout-fix\asutpKB.exe`
- `artifacts\build-check\network-topology-edit-additional-ip\asutpKB.exe`
- `artifacts\build-check\network-topology-additional-ip-inline\asutpKB.exe`
- `artifacts\build-check\network-topology-external-connection\asutpKB.exe`
- `artifacts\build-check\network-topology-external-text-dialog\asutpKB.exe`
- `artifacts\build-check\network-topology-final\asutpKB.exe`
- `artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`
- `artifacts\publish-fast\win-x64\asutpKB.exe`
- `artifacts\publish-fast-external-text\win-x64\asutpKB.exe`

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
- Visible zoom-control follow-up: the bottom strip now adds compact fixed-right zoom controls (`-`, percent combo field, `+`) so zoom can be changed without the hotkey. The separate `Масштаб` label and the internal legend scrollbar were removed after review so the link-type row stays compact. `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom-controls /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false`, layout-smoke, and `scripts\publish-fast.ps1` passed. Full core tests were not rerun after this UI-only follow-up.
- ET visible-name follow-up: Network tab UI strings for adding/editing ET objects and the new-object default prefix were changed from `ET200` to `ET`; `rg -n '"ET200"|ET200' Controls\KnowledgeBaseNetworkTopologyScreenControl.cs` found no visible `ET200` text in the Network control, `git diff --check` passed with CRLF normalization warnings only, `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore` passed, `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-et-label /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false` passed with 0 warnings and 0 errors, layout-smoke passed and refreshed `artifacts\layout-smoke\network-topology-extensions\network-topology-extensions-smoke.png`, and `scripts\publish-fast.ps1` passed. Full core tests were not rerun after this label-only UI change.
- Additional-IP follow-up: added `AdditionalIpAddresses` on Network topology elements, a right-click `Add IP address` workflow for selected objects, duplicate checking across primary and additional addresses, and a compact secondary IP display on topology cards. Validation passed: app/core/tests `dotnet format`, Release build to `artifacts\build-check\network-topology-additional-ip`, targeted normalization test, Network topology layout-smoke, `scripts\publish-fast.ps1`, and full core tests (`408/408`).
- Additional-IP layout fix: after screenshots showed a clipped add-IP dialog and overlapping card contents, the add-IP dialog was widened, topology cards were made taller, and the secondary IP badge spacing/icon/name positions were adjusted. App format passed, Release build to `artifacts\build-check\network-topology-additional-ip-layout-fix` passed with 0 warnings and 0 errors, layout-smoke passed, and a separate fast folder publish was created at `artifacts\publish-fast-layout-fix\win-x64`. The old `artifacts\build-check\network-topology-additional-ip\asutpKB.exe` was not overwritten because it was open as process `asutpKB (5768)`.
- Additional-IP edit follow-up: right-clicking an object with an additional IP now offers editing existing additional IP addresses. The edit dialog is prefilled with the selected address and duplicate checking excludes only that same address on the same object. App format passed, Release build to `artifacts\build-check\network-topology-edit-additional-ip` passed with 0 warnings and 0 errors, layout-smoke passed, and standard `scripts\publish-fast.ps1` passed after a sequential `dotnet restore asutpKB.csproj -r win-x64`.
- Additional-IP inline follow-up: the separate add/edit additional-IP UI was replaced by the existing `Элемент сети` dialog with a `Доп. IP` row. Empty `Доп. IP` deletes the additional address; filled `Доп. IP` is validated against the same duplicate map. Fixed the octet handlers so primary and additional IP fields focus and advance independently. App format passed, Release build to `artifacts\build-check\network-topology-additional-ip-inline` passed with 0 warnings and 0 errors, layout-smoke passed, and standard `scripts\publish-fast.ps1` passed.
- External-connection object follow-up: added `KbNetworkElementKind.ExternalConnection = 9`, default text `Внешняя связь`, toolbar/context menu/type-dropdown entries, toolbar icon, and a topology card that renders the element name inside an internal text-field rectangle for external system labels. Validation passed: app/core/tests `dotnet format`, Release build to `artifacts\build-check\network-topology-external-connection`, full core tests (`409/409`), layout-smoke with a sample `MES / LIMS` external card, and standard `scripts\publish-fast.ps1`.
- External-connection text-dialog follow-up: when `Внешняя связь` is selected in `Элемент сети`, the dialog hides `IP` and `Доп. IP`, relabels `Название` to `Текст`, and uses a multiline text box for the card text. App format passed, Release build to `artifacts\build-check\network-topology-external-text-dialog` passed with 0 warnings and 0 errors, and layout-smoke passed. Standard `scripts\publish-fast.ps1` failed because a running `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe` process locked a font file; a separate review publish succeeded at `artifacts\publish-fast-external-text\win-x64`.
- Final accepted Network topology package: new `Внешняя связь` elements now start with an empty `Текст` field so users can enter labels such as `КСПД`, `АКТ`, or `ВВК` immediately. Validation passed: app/core/tests `dotnet format`, Release build to `artifacts\build-check\network-topology-final` with 0 warnings and 0 errors, full core tests (`409/409`), layout-smoke, standard `scripts\publish-fast.ps1`, and `git diff --check` with CRLF normalization warnings only.
- Decision-log cleanup: `docs\decision-log.md` was reduced from 216 lines of dated history to 65 lines of durable domain decisions. No app build/test was run for this documentation-only cleanup.
- Shell guard follow-up: `scripts\codex-safe.ps1` was added and `AGENTS.md` now requires using it by default for small/localized shell diagnostics. Verified allowed `git status --short --branch`; verified blocked full `git diff`; verified blocked full `Get-Content docs\decision-log.md`. No app build/test was run for this guard/documentation-only change.

## Decisions already made

- Work only on `Net / origin/Net`.
- Do not commit or push without fresh direct approval in the current chat.
- Keep the default `scripts\publish.ps1` mode backward compatible as `SingleFile`.
- Use `scripts\publish-fast.ps1` as the accepted faster working package: folder publish, self-contained, ReadyToRun, no single-file extraction.
- Keep startup diagnostics in file logs, not visible UI, so normal operators are not interrupted.
- For Network topology, choose and edit link types through the full-label bottom strip below the topology viewport rather than through a toolbar ComboBox or a floating overlay; do not add hover/popup tooltips.
- Keep compact visible zoom controls in the bottom strip: `Ctrl + mouse wheel` remains supported, but users must also be able to change/reset zoom through the percent field and `-` / `+` buttons. Do not add a separate `Масштаб` label unless the layout is redesigned again.
- In the Network tab, display the ET object kind as `ET` rather than `ET200`; keep the stored `Et200` enum name/value unchanged for compatibility.
- In the Network topology, represent external system relationships with the `Внешняя связь` object kind and draw its `Name` as text inside the card.
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
- `scripts\codex-safe.ps1`
- `docs/codex-handoff.md`
- `docs/plans.md`
- `docs/decision-log.md`

## Known risks / open questions

- Manual comparison is qualitative so far: fast publish feels faster to the user, but no external stopwatch trace compares old single-file vs fast folder publish.
- Fast publish has many files. It should be copied/launched as a whole folder, not as a lone `asutpKB.exe`.
- Startup timing logs show elapsed checkpoints after the app starts; they do not measure Windows Defender/SmartScreen time before managed code begins.
- If `mainform-data-loaded` repeatedly dominates cold startup while SQLite remains fast, first add narrower timing around loaded-session UI application/binding; then consider deferred/asynchronous database load after first form display.
- Manual review should check that the bottom Network link-type strip with full labels and compact fixed-right zoom controls is readable, does not feel too tall, and does not hide needed viewport space on the user's real diagram/window size.
- Manual review should check `Ctrl + wheel` zoom, ordinary wheel/scrollbar access to low elements, dragging elements while zoomed, right-click add position while zoomed/scrolled, and selected-link editing through the bottom strip.
- Targeted inspection of `C:\Users\Olga\.codex\config.toml` did not reveal an active command/tool policy key, so `scripts\codex-safe.ps1` is a repo-level guard rather than a dispatcher-level hard guarantee.

## Recommended next step

After commit/push, continue from the next user request on `Net`. Use `C:\Users\Olga\AKB5\artifacts\publish-fast\win-x64\asutpKB.exe` for review, and keep using `scripts\codex-safe.ps1` for future small-task diagnostics.

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
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-zoom-controls /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-external-connection /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-topology-final /p:RunAnalyzers=false /p:WarningLevel=0 /nodeReuse:false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-fast.ps1
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0
dotnet run --project artifacts\layout-smoke\network-topology-extensions\NetworkTopologyExtensionsSmoke.csproj --configuration Release /p:RunAnalyzers=false /p:WarningLevel=0
git diff --check
```
