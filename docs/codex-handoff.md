# Current State

Last updated: `2026-05-25`

## Current objective

Current active work is maintenance schedule balancing on `Net`: the no-fail route-flow planner is now followed by a conservative rebalance pass that moves already planned visits from high-load days to low-load days only when the move strictly improves daily-hour balance and preserves owner, large-system, and shift-limit constraints. The new review workbook fixes the February KЦ `24.02` low-load case from `7` hours to `13` hours without overwriting the old workbook.

- SCALANCE: `ix:network-wired`;
- HMI: `ix:panel-ipc`.

Implemented accepted behavior:

- when adding a topology element, open the element dialog immediately so the user can enter the IP address before the element is placed;
- make the IP input four digit-only octet fields with automatic focus advance after three digits;
- add a right-click context menu on the topology canvas for common element commands;
- add right-click deletion for the specific clicked link segment;
- route links from device edge/port to device edge/port instead of center-to-center;
- distribute link ports along the chosen card side for every object, based on all links attached to that side;
- allow dragging an existing link endpoint to another object while preserving the existing link record;
- scale topology objects on the Network canvas for readability;
- validate entered topology IP addresses and prevent duplicates on the same Network diagram;
- render the IP address at the top of the element card in a more readable semibold badge instead of below the device image.

Do not commit, push, merge, rebase, or create/remove remote branches without explicit approval in the current chat.

## Current repo state

- Main worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Local `Net` was aligned to `origin/Net` after explicit user approval in the current chat.
- Current upstream base before the scale follow-up: `1f87bb5 Improve network topology editing`.
- User approved commit/push for the maintenance schedule rebalance follow-up on `2026-05-25`; after that push `Net` should be clean against `origin/Net`.
- Global Codex rules were moved/expanded in `C:\Users\Olga\.codex\AGENTS.md` so AKB5 `AGENTS.md` can stay project-specific.
- Historical AKB5/Net/network-topology worktrees and snapshots under `C:\Users\Olga\Documents\Codex\...` were inspected as references only; none were edited or deleted.
- No real `.akb` or JSON user data files were edited.
- No real `.akb` or JSON user data files were edited for this rebalance follow-up.
- Current review executable: `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`.
- Important diagnostic: the existing `C:\Users\Olga\Pictures\Купоросный цех (КЦ)_ГрафикТО_2026_01_rebalanced.xlsx` through `...\_2026_12_rebalanced.xlsx` were generated from the old `C:\Users\Olga\AKB5\bin\Release\net8.0-windows\database\knowledge-base.akb` source and total `3407` h. The actual application database is `C:\Users\Olga\Desktop\asutpKB\proj\database\knowledge-base.akb`, totals `3550` h for KЦ 2026, and must be used for the next review workbook generation.

## Current package

Changed files:

- `Controls/KnowledgeBaseNetworkTopologyScreenControl.cs`
- `Controls/KnowledgeBaseAdditionalEquipmentScreenControl.cs`
- `Controls/KnowledgeBaseCompositionScreenControl.cs`
- `Controls/KnowledgeBaseWorkspaceVisuals.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.cs`
- `Forms/KnowledgeBaseChangeHistoryForm.cs`
- `Forms/KnowledgeBaseEquipmentCatalogForm.cs`
- `Forms/KnowledgeBaseEquipmentCatalogSelectionDialog.cs`
- `Forms/KnowledgeBaseMaintenanceAnnualWorkbookExportDialog.cs`
- `Forms/KnowledgeBaseMaintenanceYearScheduleSourceDialog.cs`
- `Forms/KnowledgeBaseMaintenanceYearWorkbookExportDialog.cs`
- `Forms/KnowledgeBaseMaintenanceYearWorkbookRecalculationDialog.cs`
- `Forms/KnowledgeBaseSnapshotBrowserForm.cs`
- `Forms/KnowledgeBaseSnapshotsAndHistoryForm.cs`
- `Program.cs`
- `Models/KbMaintenanceMonthPlanAssignment.cs`
- `Models/KbMaintenanceMonthWorkItem.cs`
- `Services/KnowledgeBaseMaintenanceMonthWorkResolverService.cs`
- `Services/KnowledgeBaseMaintenanceMonthlyPlannerService.cs`
- `Services/KnowledgeBaseFormStateService.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseFormStateServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceMonthWorkResolverServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceMonthlyPlannerServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceMonthlyPlannerIntegrationTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceWorkbookGenerationServiceTests.cs`
- `C:\Users\Olga\.codex\AGENTS.md`
- `AGENTS.md`
- `docs/codex-handoff.md`
- `docs/decision-log.md`
- `docs/lessons-learned.md`
- `docs/plans.md`
- `Roadmap.md`

Implemented behavior:

- `AddElement` now creates a draft element, opens `NetworkElementDialog`, focuses the IP field, and only adds the element when the dialog returns OK.
- `NetworkElementDialog` now uses four digit-only IP octet boxes. Typing the third digit in an octet moves focus to the next box; typing `.` also moves forward; Backspace at the start moves to the previous box.
- Existing dotted IPv4 values are split into the four octet boxes for editing.
- Right-click on the topology canvas now opens a context menu. It includes `Добавить` with device-type submenu, `Изменить`, `Связать` / `Завершить связь`, `Удалить`, and `Отмена связи` while link mode is active.
- Adding from the context menu places the new element near the clicked point.
- Right-click on a link segment now adds `Удалить связь`, which removes only that `KbNetworkLink` and does not delete either connected object.
- Link hit-testing follows the same three-segment orthogonal path used for drawing, with a small tolerance for easier clicking.
- Context-menu link-start command is named `Связать`; the toolbar button remains `Связь`.
- Link drawing now anchors to a side of each device card, adds a short stub from the card edge, and chooses an orthogonal route that avoids unrelated device rectangles.
- Port-side selection now prioritizes physical reading for every object: devices above/below a card connect to top/bottom, while same-row devices connect to left/right.
- Port coordinates are now allocated per object side from all incident links on that side, so multiple links to any object type use separate side positions instead of one shared center point.
- Left-dragging an existing link starts endpoint reassignment. The nearest endpoint to the click is treated as the moving endpoint; releasing over another object updates that endpoint and preserves the existing `LinkId`.
- Invalid link endpoint drops are ignored: releasing outside an object, onto the fixed endpoint object, or onto an already-existing duplicate connection leaves the link unchanged.
- The initial endpoint-drag implementation cleared drag state before validating the drop, so the target was rejected and the link snapped back. This is fixed: drop validation and endpoint update now happen before drag state is cleared.
- Network canvas element cards are scaled from `112x84` to `134x101`; device icons from `38` to `46`; default placement spacing and link port/stub spacing were scaled to match.
- Element name text was scaled by roughly 20%; IP badge height and IP font were scaled by roughly 30%.
- `NetworkElementDialog` now validates IP on OK for both add and edit. Empty IP is allowed; partial IP and octets outside `0..255` are rejected; normalized duplicate IP addresses already used by another element on the same diagram are rejected with a warning.
- SCALANCE and HMI topology icons were replaced with the user-selected Siemens iX icons: `ix:network-wired` and `ix:panel-ipc`.
- The existing edit dialog kind-retention behavior is preserved for already-created elements.
- The topology canvas card height was slightly increased to fit the top IP badge.
- IP text now renders in a top badge with `Segoe UI Semibold`; the device icon is shifted below it, and the element name remains at the bottom.
- `Program.Main` now creates a per-user named mutex (`Local\AKB5.AsutpKnowledgeBase.SingleInstance`) before starting the WinForms shell. If another instance is already running, the new process shows an informational message and exits without opening a second `MainForm`.
- Maintenance monthly planning now distinguishes large systems (`Lvl2` systems with more than two visible `Lvl3` children) from small systems. A working day may contain at most one large system; small systems can be added as fillers, including third and later systems, only while the resulting day stays within the shift-load limit.
- The planner now computes `AK9`-style balance from actual planned/requested hours per working day, not from a manually entered monthly fund. The entered fund remains the capacity check.
- Day selection now avoids exceeding the shift-load limit when a feasible day exists. This fixes bad local optima where `19 + 8 = 27` or an August day at `22 h` were accepted while other working days still had usable capacity.
- The shift-load limit is `16 h` while `AK9 <= 16`; when `AK9 > 16`, the planner may exceed `16 h` and uses `ceil(AK9) + 1` as a small route-constraint allowance.
- The planner now uses a constrained visit queue instead of scheduling one entire system stream before the next. At each step it places the next visit from the group with the fewest feasible days, which prevents large systems from consuming all day capacity before narrow small-system fillers are considered.
- Candidate day ranking is intentional and should stay in this order: feasible-day scarcity first, shift-load-limit compliance, empty day while the month has not reached the target occupied working-day count, future same-system continuation before calendar rollback, nearest continuation date, closeness to `AK9`, lower current load, small-system filler below target, lower projected total, weak adjacency penalties, earlier date.
- Continuations for the same system now prefer the nearest next feasible working day before other balance criteria, so a split route does not skip a working day solely to avoid same-system adjacency.
- Multiple work assignments from the same `SystemNodeId` may share a date when they belong to different owner nodes. A repeated assignment for the same `OwnerNodeId` on the same date remains blocked, so split work for one object continues on the next working day.
- The planner now carries `SystemLevel3NodeCount` from the tree resolver into work items and planned assignments so route-size rules are based on the actual visible hierarchy, not only the number of assignments due in a month.
- The same system-flow rule applies through all workbook generation paths that use the monthly planner: single month, month update, full year-by-month generation, and partial year recalculation. The annual summary workbook has no day-level placement, so no same-day route conflict can be created there.
- Common Codex operating rules now live in `C:\Users\Olga\.codex\AGENTS.md`; AKB5 `AGENTS.md` now points there and keeps project-specific rules only.
- Dicta's `C:\Users\Olga\Documents\VoiceHelper\AGENTS.md` / `docs` split was used as the pattern for the AKB5 cleanup.
- Historical candidates inspected under `C:\Users\Olga\Documents\Codex\...`: `2026-05-18\akb5-card-git-c-c-users`, `2026-05-19\akb5-net-git-c-c-users-3`, `2026-05-22\net-merge-workspace-resolver\base-d429330`, `2026-05-22\net-merge-workspace-resolver\merged-net`, and `2026-05-23\network-topology-icons`. They were not edited or deleted.
- Hover/popup tooltips have been removed from the main toolbar/search/tree, Network topology toolbar/add buttons, Composition add-rack button, and Additional Equipment rows.
- Automatic WinForms hover tooltips are disabled for ToolStrip/ListView/DataGridView surfaces that were touched by this cleanup; `SaveToolTip` form-state plumbing was removed.
- `AGENTS.md`, `docs/decision-log.md`, `docs/lessons-learned.md`, `docs/plans.md`, and `Roadmap.md` now document that hover/popup tooltips must not be reintroduced.
- Lvl2 inventory-number field visibility is restored in the info screen. The inventory `TextBox` is wrapped in a field-frame panel; the row-height toggle now targets the actual summary `TableLayoutPanel`, so existing Lvl2 inventory data is visible/editable again.
- Maintenance monthly planning no longer fails merely because no day satisfies every soft route/load preference. Repeating the same owner object on one date remains blocked; shift overload and same-day large-system mixing are last-resort penalties so a feasible month still produces a workbook.
- Maintenance monthly planning now runs a post-scheduling rebalance pass. It moves planned visits from above-target days to below-target days only when the two-day squared deviation from `AK9` improves, the target day stays within the current shift-load limit, owner/date duplicates are not created, and a large system is not mixed with another large system.

## Validation status

Validation completed in `C:\Users\Olga\AKB5`:

- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet build asutpKB.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- Later normal Release build to `bin\Release` failed because `C:\Users\Olga\AKB5\bin\Release\net8.0-windows\asutpKB.exe` was locked by running process `asutpKB (15200)`, not because of a compile error.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-context-menu /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- Manual review passed per user message on `2026-05-23` for the current add/IP/context-menu flow.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-selected-icons /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- `dotnet run --project artifacts\icon-review\SelectedIconSmoke\SelectedIconSmoke.csproj --configuration Release`: passed; produced `artifacts\icon-review\selected-icons-smoke.png` with `2600` non-white pixels. It printed an analyzer documentation warning from the temporary smoke project, but the run completed successfully.
- Manual review passed per user message on `2026-05-23` for the selected SCALANCE/HMI icons.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-delete-link /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-context-menu-link-text /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- Tavily research was used for topology/link-routing guidance. Key takeaways: keep network diagrams readable with minimal crossings, use clear connection lines, and prefer object-avoiding orthogonal connectors over routes that pass through or visually merge with objects.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-routing /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- After user reported the first routing revision still produced a false T-junction, port selection was refined.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-routing-ports /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- After user clarified that the multi-port rule must apply to all objects, endpoint port allocation was made universal across every device type.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-universal-ports /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- Manual review passed per user message on `2026-05-23` for universal port-distributed routing.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-reassign /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- Manual review did not pass for the first endpoint-drag build: the link moved visually but returned to the old endpoint after drop.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-reassign-fix /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- Manual review passed per user message on `2026-05-23` for the fixed endpoint-drag build.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-scale-objects /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-ip-duplicates /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- Manual review passed per user message on `2026-05-24` for scaled Network objects and IP duplicate/validity checks.
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed after the single-instance guard change.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\single-instance /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- `git diff --check`: passed with only CRLF normalization warnings.
- `dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0 --filter "FullyQualifiedName~KnowledgeBaseMaintenanceMonthlyPlanner"`: passed, 16 tests before system-flow refinement.
- `dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0 --filter "FullyQualifiedName~KnowledgeBaseMaintenance"`: passed, 111 tests before system-flow refinement.
- `dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\maintenance-same-system-day /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- `dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed, 395 tests.
- Inspected `C:\Users\Olga\Pictures\Купоросный цех (КЦ)_ГрафикТО_2026_01.xlsx`: first working day `12.01.2026` had 5 assignments across 5 systems and 21 hours, confirming the route-mixing problem.
- `dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0 --filter "FullyQualifiedName~KnowledgeBaseMaintenanceMonthlyPlanner"`: passed after system-flow refinement, 16 tests.
- `dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0 --filter "FullyQualifiedName~KnowledgeBaseMaintenance"`: passed after system-flow refinement, 112 tests.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\maintenance-system-flow /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- `dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed after system-flow refinement, 396 tests.
- Inspected `C:\Users\Olga\Pictures\Купоросный цех (КЦ)_ГрафикТО_2026_01.xlsx`, `...\_2026_02.xlsx`, and `...\_2026_03.xlsx`: the rejected outputs still had overloaded days such as January `23` with `39 h`, January `28` with `38 h`, and January `30` with `7 h`; this confirmed that the earlier two-system cap did not express the route logic.
- `dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0 --filter "FullyQualifiedName~KnowledgeBaseMaintenanceMonthlyPlanner"`: passed after refined route-flow fix, 19 tests.
- `dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0 --filter "FullyQualifiedName~KnowledgeBaseMaintenance"`: passed after refined route-flow fix, 115 tests.
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet build asutpKB.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed with 0 warnings and 0 errors.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed, 399 tests.
- `dotnet publish asutpKB.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts\publish\win-x64 /p:RunAnalyzers=false /p:WarningLevel=0`: passed; manual-review executable is `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`.
- `git diff --check`: passed with only CRLF normalization warnings.
- `dotnet build asutpKB.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed after tooltip cleanup with 0 warnings and 0 errors.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~KnowledgeBaseFormStateService" /p:RunAnalyzers=false /p:WarningLevel=0`: passed after removing `SaveToolTip`, 16 tests.
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed after tooltip cleanup.
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed after tooltip cleanup.
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed after tooltip cleanup.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed after tooltip cleanup, 399 tests.
- `dotnet publish asutpKB.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts\publish\win-x64 /p:RunAnalyzers=false /p:WarningLevel=0`: passed after tooltip cleanup; manual-review executable is `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`.
- Inspected the user-provided rejected `C:\Users\Olga\Pictures\Купоросный цех (КЦ)_ГрафикТО_2026_01.xlsx`: `AK8=290`, `AK9=19.333333333333332`, `AL9=15`; day totals included `22.01=27`, `29.01=0`, `30.01=2`, confirming the remaining balance failure.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~KnowledgeBaseMaintenanceMonthlyPlannerServiceTests" /p:RunAnalyzers=false /p:WarningLevel=0`: passed after balance fix, 17 tests.
- Diagnostic read-only run against `C:\Users\Olga\AKB5\bin\Release\net8.0-windows\database\knowledge-base.akb` for KЦ with workbook budgets from the rejected files:
  - January 2026, budget `290`, `AK9=19.33`: `empty=0`, min/max `13..21`, totals `12:20, 13:20, 14:19, 15:19, 16:20, 19:17, 20:16, 21:19, 22:20, 23:19, 26:21, 27:21, 28:16, 29:20, 30:13`.
  - February 2026, budget `285`, `AK9=15.00`: `empty=0`, min/max `10..16`.
  - March 2026, budget `303`, `AK9=14.43`: `empty=0`, min/max `9..17`.
- Diagnostic read-only full-year KЦ run against the current `C:\Users\Olga\AKB5\bin\Release\net8.0-windows\database\knowledge-base.akb` using monthly default budgets (`budget = current month demand`) passed for all 12 months; log: `artifacts\diagnostics\maintenance-full-year-check\full-year-kc-2026.log`.
  - January: requested/budget `280`, working days `15`, `AK9=18.67`, `empty=0`, min/max `15..23`.
  - February: requested/budget `275`, working days `19`, `AK9=14.47`, `empty=0`, min/max `10..16`.
  - March: requested/budget `288`, working days `21`, `AK9=13.71`, `empty=0`, min/max `8..17`.
  - April: requested/budget `289`, working days `22`, `AK9=13.14`, `empty=0`, min/max `8..16`.
  - May: requested/budget `287`, working days `19`, `AK9=15.11`, `empty=0`, min/max `11..22`.
  - June: requested/budget `287`, working days `21`, `AK9=13.67`, `empty=0`, min/max `8..17`.
  - July: requested/budget `274`, working days `23`, `AK9=11.91`, `empty=0`, min/max `6..16`.
  - August: requested/budget `290`, working days `21`, `AK9=13.81`, `empty=0`, min/max `8..22`.
  - September: requested/budget `273`, working days `22`, `AK9=12.41`, `empty=0`, min/max `6..16`.
  - October: requested/budget `282`, working days `22`, `AK9=12.82`, `empty=0`, min/max `6..16`.
  - November: requested/budget `301`, working days `20`, `AK9=15.05`, `empty=0`, min/max `10..24`.
  - December: requested/budget `281`, working days `22`, `AK9=12.77`, `empty=0`, min/max `6..18`.
- Diagnostic read-only full-year KЦ rerun after the shift-limit/visit-queue refinement passed for all 12 months; log: `artifacts\diagnostics\maintenance-full-year-check\full-year-kc-2026-after-ak9-over16-slack.log`.
  - January: requested/budget `280`, working days `15`, `AK9=18.67`, `empty=0`, min/max `9..20`.
  - February: requested/budget `275`, working days `19`, `AK9=14.47`, `empty=0`, min/max `7..16`.
  - March: requested/budget `288`, working days `21`, `AK9=13.71`, `empty=0`, min/max `4..16`.
  - April: requested/budget `289`, working days `22`, `AK9=13.14`, `empty=0`, min/max `2..16`.
  - May: requested/budget `287`, working days `19`, `AK9=15.11`, `empty=0`, min/max `12..16`.
  - June: requested/budget `287`, working days `21`, `AK9=13.67`, `empty=0`, min/max `4..16`.
  - July: requested/budget `274`, working days `23`, `AK9=11.91`, `empty=0`, min/max `2..16`.
  - August: requested/budget `290`, working days `21`, `AK9=13.81`, `empty=0`, min/max `6..16`.
  - September: requested/budget `273`, working days `22`, `AK9=12.41`, `empty=0`, min/max `2..16`.
  - October: requested/budget `282`, working days `22`, `AK9=12.82`, `empty=0`, min/max `6..16`.
  - November: requested/budget `301`, working days `20`, `AK9=15.05`, `empty=0`, min/max `12..16`.
  - December: requested/budget `281`, working days `22`, `AK9=12.77`, `empty=0`, min/max `2..16`.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~KnowledgeBaseMaintenanceMonthlyPlannerServiceTests" /p:RunAnalyzers=false /p:WarningLevel=0`: passed after shift-limit/visit-queue refinement, 18 tests.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~KnowledgeBaseMaintenance" /p:RunAnalyzers=false /p:WarningLevel=0`: passed after shift-limit/visit-queue refinement, 118 tests.
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed after shift-limit/visit-queue refinement.
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed after shift-limit/visit-queue refinement.
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed after shift-limit/visit-queue refinement.
- `dotnet build asutpKB.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed after shift-limit/visit-queue refinement with 0 warnings and 0 errors.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed after shift-limit/visit-queue refinement, 402 tests.
- `dotnet publish asutpKB.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts\publish\win-x64 /p:RunAnalyzers=false /p:WarningLevel=0`: passed after shift-limit/visit-queue refinement; manual-review executable is `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`.
- Final diagnostic read-only full-year KЦ run after publish passed for all 12 months; log: `artifacts\diagnostics\maintenance-full-year-check\full-year-kc-2026-final-shift-limit.log`. Results: January `AK9=18.67`, `empty=0`, min/max `9..20`; February `7..16`; March `4..16`; April `2..16`; May `12..16`; June `4..16`; July `2..16`; August `6..16`; September `2..16`; October `6..16`; November `12..16`; December `2..16`.
- Codex-doc cleanup validation on `2026-05-24`: checked `git status --short --branch` and scoped doc diff before edits; compared Dicta docs in `C:\Users\Olga\Documents\VoiceHelper`; inspected historical AKB5/Net/network-topology candidates under `C:\Users\Olga\Documents\Codex\...`; confirmed common-rule search patterns no longer appear in AKB5 `AGENTS.md` / core continuity docs; `git diff --check` passed with only CRLF normalization warnings.
- Lvl2 inventory field fix validation on `2026-05-24`: `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore` passed; `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~KnowledgeBaseFormStateService" /p:RunAnalyzers=false /p:WarningLevel=0` passed, 16 tests; `dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\lvl2-inventory-field /p:RunAnalyzers=false /p:WarningLevel=0` passed with 0 warnings and 0 errors.
- Maintenance no-fail fallback validation on `2026-05-24`: planner targeted tests passed, 19 tests; maintenance-focused tests passed, 119 tests; app/core/tests format checks passed; Release build passed with 0 warnings/errors; full core tests passed, 403 tests; publish passed to `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`.
- Read-only KЦ full-year diagnostic after fallback passed all 12 months; February `requested=275`, `working=19`, `empty=0`, range `7..16`. Generated February workbook: `C:\Users\Olga\Pictures\Купоросный цех (КЦ)_ГрафикТО_2026_02_fixed.xlsx`.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~KnowledgeBaseMaintenance" /p:RunAnalyzers=false /p:WarningLevel=0`: passed after balance fix, 117 tests.
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed after balance fix.
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed after balance fix.
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed after balance fix.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed after balance fix, 401 tests.
- `dotnet build asutpKB.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0`: passed after balance fix with 0 warnings and 0 errors.
- `dotnet publish asutpKB.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts\publish\win-x64 /p:RunAnalyzers=false /p:WarningLevel=0`: passed after balance fix; manual-review executable is `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`.
- Maintenance rebalance validation on `2026-05-25`: targeted new rebalance test passed; maintenance-focused tests passed, 120 tests; app/core/tests format checks passed; Release build passed with 0 warnings/errors; full core tests passed, 404 tests; publish passed to `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`.
- Read-only KЦ February workbook generation against `C:\Users\Olga\AKB5\bin\Release\net8.0-windows\database\knowledge-base.akb` produced `C:\Users\Olga\Pictures\Купоросный цех (КЦ)_ГрафикТО_2026_02_rebalanced.xlsx`: `requested=275`, `working=19`, `empty=0`, range `10..16`, `AC13=13`, `AC8=13`, `AK9=14.473684210526315`.
- Read-only KЦ all-month workbook generation on `2026-05-25` produced 12 files in `C:\Users\Olga\Pictures` with suffix `_rebalanced.xlsx`. Month ranges: January `16..20`; February `10..16`; March `10..16`; April `8..16`; May `12..16`; June `10..16`; July `6..16`; August `10..16`; September `2..16`; October `8..16`; November `12..16`; December `8..16`; every month had `empty=0`.
- Follow-up read-only source diagnostic on `2026-05-25`: actual database `C:\Users\Olga\Desktop\asutpKB\proj\database\knowledge-base.akb` gives KЦ monthly demands `[290, 285, 303, 299, 297, 302, 284, 300, 289, 292, 311, 298]`, total `3550` h. Existing `_rebalanced.xlsx` files in `C:\Users\Olga\Pictures` match the old bin Release database demands `[280, 275, 288, 289, 287, 287, 274, 290, 273, 282, 301, 281]`, total `3407` h. June loss is source-related: actual DB `302` h vs workbook `287` h.

Not run:

- Manual visual review was not run.
- Manual double-launch behavior was not run.
- Manual Excel open/edit/save smoke was not run.

## Decisions already made

- Work only on `Net / origin/Net`.
- Do not commit or push without fresh direct approval in the current chat.
- Keep the Level 2 Network topology scope; do not broaden into PRONETA/CSV import, live scan, OCR/PDF import, plan/fact comparison, IP assignment automation, or embedded PDF preview.
- Do not reintroduce hover/popup tooltips. Use visible labels, inline validation/status text, or modal validation messages instead.

## Files already relevant to the task

- `Controls/KnowledgeBaseNetworkTopologyScreenControl.cs`
- `Controls/KnowledgeBaseAdditionalEquipmentScreenControl.cs`
- `Controls/KnowledgeBaseCompositionScreenControl.cs`
- `Controls/KnowledgeBaseWorkspaceVisuals.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.cs`
- `Program.cs`
- `Models/KbMaintenanceMonthPlanAssignment.cs`
- `Models/KbMaintenanceMonthWorkItem.cs`
- `Services/KnowledgeBaseMaintenanceMonthWorkResolverService.cs`
- `Services/KnowledgeBaseMaintenanceMonthlyPlannerService.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceMonthWorkResolverServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceMonthlyPlannerServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceMonthlyPlannerIntegrationTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseMaintenanceWorkbookGenerationServiceTests.cs`
- `C:\Users\Olga\.codex\AGENTS.md`
- `AGENTS.md`
- `docs/decision-log.md`
- `docs/codex-handoff.md`
- `docs/plans.md`

## Known risks / open questions

- The single-instance guard is per Windows logon session (`Local\...` mutex), so it blocks duplicate launches by the same user/session without requiring global mutex permissions.
- The rebalance pass moves whole planned visits, not arbitrary Excel cells. This keeps route grouping conservative; it improves the February KЦ `24.02` case to `13` hours, while some days can still remain below `AK9` when further improvement would require splitting a grouped visit.
- User-facing Russian responses must remain gender-neutral; this is now recorded in `C:\Users\Olga\.codex\AGENTS.md` as a global rule.

- The `_rebalanced.xlsx` files currently in `C:\Users\Olga\Pictures` are useful only as algorithm diagnostics for the old bin Release source. Regenerate the KЦ monthly workbooks from the published executable using `C:\Users\Olga\Desktop\asutpKB\proj\database\knowledge-base.akb` before reviewing the 3550 h annual balance.
- The current implementation treats Cancel in the add dialog as "do not add the element"; this matches the "enter IP before adding" flow but should be confirmed during manual use.

## Recommended next step

Current likely next decision: regenerate and manually review 2026 KЦ monthly workbooks from `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe` using the actual database `C:\Users\Olga\Desktop\asutpKB\proj\database\knowledge-base.akb`; expected annual total is `3550` h and June demand is `302` h.

Preview artifact for choosing icons:

- `artifacts/icon-review/network-icon-options.html`
- `artifacts/icon-review/selected-icons-smoke.png`

## Commands to run before finishing future implementation work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-context-menu /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-delete-link /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-context-menu-link-text /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-routing /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-routing-ports /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-universal-ports /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-reassign /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-link-reassign-fix /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-scale-objects /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\network-ip-duplicates /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\single-instance /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\maintenance-same-system-day /p:RunAnalyzers=false /p:WarningLevel=0
dotnet build asutpKB.csproj --configuration Release --no-restore -o artifacts\build-check\maintenance-system-flow /p:RunAnalyzers=false /p:WarningLevel=0
dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore /p:RunAnalyzers=false /p:WarningLevel=0
dotnet publish asutpKB.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts\publish\win-x64 /p:RunAnalyzers=false /p:WarningLevel=0
git diff --check
git status --short --branch
```
