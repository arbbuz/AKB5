# Current State

Last updated: `2026-06-18`

## Current objective

Current active work is the OpenXML/Excel exchange split on `Net`: Excel import/export and maintenance workbook exchange were removed from the main app/core dependency graph and moved to a separate `AsutpKnowledgeBase.ExcelExchange` module loaded on demand from `excel-exchange\AsutpKnowledgeBase.ExcelExchange.dll`.

The implementation is local and validated. Commit/push/merge/rebase is not authorized in the current chat.

## Current repo state

- Main worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Local git state after implementation: code/docs changed for the Excel split, plus the pre-existing local `AGENTS.md` change that was not made for this task.
- No real `.akb` or JSON user data files were edited.
- Review publish artifact: `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`
- Excel module artifact: `C:\Users\Olga\AKB5\artifacts\publish\win-x64\excel-exchange\AsutpKnowledgeBase.ExcelExchange.dll`
- PDF module artifact remains present: `C:\Users\Olga\AKB5\artifacts\publish\win-x64\pdf-import\AsutpKnowledgeBase.PdfImport.dll`

## Current package

Tracked/source changes for the Excel split:

- `Services\KnowledgeBaseExcelExchangeContracts.cs`: shared database Excel export/import contract and DTOs kept in core-linked code.
- `Services\KnowledgeBaseMaintenanceScheduleNormImportContracts.cs`: shared maintenance norm import contract and DTOs.
- `Services\KnowledgeBaseMaintenanceWorkbookGenerationContracts.cs`: shared maintenance workbook generation contract and DTOs, including production-calendar years.
- `Services\KnowledgeBaseMaintenanceYearScheduleSourceContracts.cs`: shared year-schedule source contracts, DTOs, and row clone helper.
- `UiServices\KnowledgeBaseExcelExchangePluginLoader.cs`: loads `AsutpKnowledgeBase.ExcelExchange.dll` on demand and resolves dependencies from the `excel-exchange` folder.
- `UiServices\KnowledgeBaseExcelUiWorkflowService.cs`: routes database Excel export/import through the loader and checks module availability before file dialogs.
- `UiServices\KnowledgeBaseMaintenanceWorkbookUiWorkflowService.cs`: routes maintenance workbook generation through the loader while preserving optional injected generator behavior for tests.
- `Forms\MainForm.cs`: creates one Excel exchange loader and passes it to Excel/maintenance UI workflows.
- `Forms\MainForm.Maintenance.cs`: routes maintenance norm import and year-schedule source Excel workflows through the loader.
- `Forms\KnowledgeBaseMaintenanceYearScheduleSourceDialog.cs`: uses the shared row clone helper after moving the old service implementation.
- `src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj`: removes the direct `DocumentFormat.OpenXml` package reference and Excel template embedded resources from core.
- `src\AsutpKnowledgeBase.ExcelExchange\AsutpKnowledgeBase.ExcelExchange.csproj`: new Excel module project with the `DocumentFormat.OpenXml` dependency and embedded maintenance templates.
- `src\AsutpKnowledgeBase.ExcelExchange\*.cs`: moved Excel/OpenXML implementations and added the `KnowledgeBaseExcelExchangePlugin` facade.
- `tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj`: test project now references the Excel module to keep existing Excel/OpenXML tests.
- `scripts\publish.ps1`: publishes the main single-file app, then publishes `pdf-import` and `excel-exchange`, then removes unused SQLite/core runtime files from both module folders.
- `docs\codex-handoff.md`, `docs\plans.md`, `docs\decision-log.md`: refreshed current task state/decision.

Ignored validation artifacts:

- `artifacts\publish\win-x64\asutpKB.exe`
- `artifacts\publish\win-x64\excel-exchange\AsutpKnowledgeBase.ExcelExchange.dll`
- `artifacts\smoke\excel-exchange-loader\`

## Validation status

Validation completed in `C:\Users\Olga\AKB5`:

- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter KnowledgeBaseExcelExchangeServiceTests -v:minimal`: passed, 37 tests.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter KnowledgeBaseMaintenanceScheduleNormImportServiceTests -v:minimal`: passed, 20 tests.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~KnowledgeBaseMaintenanceWorkbook" -v:minimal`: passed, 35 tests.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~KnowledgeBaseMaintenanceYearScheduleSource" -v:minimal`: passed, 10 tests.
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore -v:quiet`: passed.
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore -v:quiet`: passed.
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore -v:quiet`: passed.
- `dotnet format src\AsutpKnowledgeBase.ExcelExchange\AsutpKnowledgeBase.ExcelExchange.csproj --verify-no-changes --severity error --no-restore -v:quiet`: passed.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -v:minimal -clp:ErrorsOnly`: passed, 0 errors.
- `dotnet build src\AsutpKnowledgeBase.ExcelExchange\AsutpKnowledgeBase.ExcelExchange.csproj --configuration Release --no-restore -v:minimal -clp:ErrorsOnly`: passed, 0 errors.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore -v:minimal`: passed, 410 tests.
- `dotnet list asutpKB.csproj package --include-transitive`: passed; the main app package graph does not include `DocumentFormat.OpenXml`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish.ps1`: passed and produced the main app, `pdf-import`, and `excel-exchange`.
- `dotnet run --project artifacts\smoke\excel-exchange-loader\ExcelExchangeSmoke.csproj --configuration Release --no-restore -- C:\Users\Olga\AKB5`: passed; the cleaned published Excel module loads, implements all expected contracts, and completes a basic export/import round-trip on an empty model.

Published size check:

- Main review exe: `73,141,213` bytes (`69.75 MiB`).
- `excel-exchange` folder: `7,749,319` bytes (`7.39 MiB`), 6 files.
- `pdf-import` folder: `5,718,416` bytes (`5.45 MiB`), 10 files.
- Full review folder including resources and both modules: `97,527,270` bytes (`93.01 MiB`).

Manual GUI/Excel review was not run by Codex. A real Windows interactive app session and, ideally, real edited workbooks are still needed before accepting the package.

## Decisions already made

- The main app/core must not reference `DocumentFormat.OpenXml` directly.
- Excel exchange is an optional on-demand module under `excel-exchange`.
- If `excel-exchange\AsutpKnowledgeBase.ExcelExchange.dll` is missing, the app shows a user-facing warning before opening Excel-related dialogs.
- The test project may reference the Excel module so existing Excel/OpenXML tests continue to run.
- `SingleFile` publish should keep compression enabled; the faster-starting package remains the folder/ReadyToRun publish path.
- A separate abstractions project is not needed for the current Excel split because shared contracts in core plus post-publish cleanup keep the published module lean and the cleaned-module load smoke passes.

## Files already relevant to the task

- `Forms\MainForm.cs`
- `Forms\MainForm.Maintenance.cs`
- `Forms\KnowledgeBaseMaintenanceYearScheduleSourceDialog.cs`
- `Services\KnowledgeBaseExcelExchangeContracts.cs`
- `Services\KnowledgeBaseMaintenanceScheduleNormImportContracts.cs`
- `Services\KnowledgeBaseMaintenanceWorkbookGenerationContracts.cs`
- `Services\KnowledgeBaseMaintenanceYearScheduleSourceContracts.cs`
- `UiServices\KnowledgeBaseExcelExchangePluginLoader.cs`
- `UiServices\KnowledgeBaseExcelUiWorkflowService.cs`
- `UiServices\KnowledgeBaseMaintenanceWorkbookUiWorkflowService.cs`
- `src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj`
- `src\AsutpKnowledgeBase.ExcelExchange\AsutpKnowledgeBase.ExcelExchange.csproj`
- `src\AsutpKnowledgeBase.ExcelExchange\KnowledgeBaseExcelExchangePlugin.cs`
- `tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj`
- `scripts\publish.ps1`

## Known risks / open questions

- The main `exe` is smaller only by the compressed size contribution of OpenXML and template resources; self-contained .NET runtime and the Material Symbols font still dominate the single-file package size.
- The Excel module publish still has core transitive dependencies in its dependency graph because it references core models/services, but `scripts\publish.ps1` removes unused SQLite runtime files from `excel-exchange` after publish.
- The missing-module path is covered by code/build review, not by a GUI smoke.
- Real Excel open/edit/save/import behavior is covered by unit tests and module smoke, not by Microsoft Excel desktop automation or an interactive WinForms session in this run.

## Recommended next step

Manual review from `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`:

1. Start the app with the `excel-exchange` folder present and confirm ordinary startup/work.
2. Run database Excel export/import.
3. Run maintenance norm import and maintenance workbook generation/export flows that use Excel.
4. Run year-schedule source export/import.
5. Temporarily run/copy the package without `excel-exchange` and confirm Excel actions report that the module is missing before opening file dialogs.

If accepted, request fresh explicit commit/push approval in the active chat.

## Commands to run before finishing future implementation work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.ExcelExchange\AsutpKnowledgeBase.ExcelExchange.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish.ps1
```
