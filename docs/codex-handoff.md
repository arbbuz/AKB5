# Current State

Last updated: `2026-06-18`

## Current objective

Current active work is the PDF production-calendar import split on `Net`: the PDF parser was removed from the main app/core dependency graph and moved to a separate `AsutpKnowledgeBase.PdfImport` module loaded on demand from `pdf-import\AsutpKnowledgeBase.PdfImport.dll`.

The implementation is local and validated. Commit/push/merge/rebase is not authorized in the current chat.

## Current repo state

- Main worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Local git state after implementation: code/docs changed for the PDF split, plus the pre-existing local `AGENTS.md` change that was not made for this task.
- No real `.akb` or JSON user data files were edited.
- Review publish artifact: `C:\Users\Olga\AKB5\artifacts\publish-pdf-split\win-x64\asutpKB.exe`
- PDF module artifact: `C:\Users\Olga\AKB5\artifacts\publish-pdf-split\win-x64\pdf-import\AsutpKnowledgeBase.PdfImport.dll`

## Current package

Tracked/source changes for the PDF split:

- `Forms\MainForm.cs`: replaces the eager PDF-import service field with a plugin loader field.
- `Forms\MainForm.ProductionCalendar.cs`: checks for `pdf-import` before PDF import and calls the loader instead of direct `PdfPig` code.
- `Services\KnowledgeBaseProductionCalendarPdfImportContracts.cs`: keeps the shared importer contract and result DTO in core-linked code.
- `UiServices\KnowledgeBaseProductionCalendarPdfImportPluginLoader.cs`: loads `AsutpKnowledgeBase.PdfImport.dll` on demand and resolves its dependencies from the `pdf-import` folder.
- `src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj`: removes the direct `PdfPig` package reference.
- `src\AsutpKnowledgeBase.PdfImport\AsutpKnowledgeBase.PdfImport.csproj`: new PDF-import module project with the `PdfPig` dependency.
- `src\AsutpKnowledgeBase.PdfImport\KnowledgeBaseProductionCalendarPdfImportService.cs`: moved PDF parser implementation.
- `tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj`: test project now references the PDF module to keep parser tests.
- `scripts\publish.ps1`: publishes the main single-file app, then publishes the PDF module into `pdf-import`, then removes non-PDF OpenXML/SQLite runtime files from that module folder; `SingleFile` mode now enables `EnableCompressionInSingleFile=true`.
- `docs\codex-handoff.md`, `docs\plans.md`, `docs\decision-log.md`: refreshed current task state/decision.

Ignored validation artifacts:

- `artifacts\publish-pdf-split\win-x64\asutpKB.exe`
- `artifacts\publish-pdf-split\win-x64\pdf-import\AsutpKnowledgeBase.PdfImport.dll`

## Validation status

Validation completed in `C:\Users\Olga\AKB5`:

- `dotnet build src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --configuration Release --no-restore -v:minimal`: passed; `PdfPig` no longer appears in core/common service search.
- `dotnet build src\AsutpKnowledgeBase.PdfImport\AsutpKnowledgeBase.PdfImport.csproj --configuration Release -v:minimal`: passed.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --filter KnowledgeBaseProductionCalendarPdfImportServiceTests -v:minimal`: passed, 3 tests.
- `dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore`: passed.
- `dotnet format src\AsutpKnowledgeBase.PdfImport\AsutpKnowledgeBase.PdfImport.csproj --verify-no-changes --severity error --no-restore`: passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish.ps1 -Configuration Release -RuntimeIdentifier win-x64 -PublishMode SingleFile -OutputDirectory artifacts\publish-pdf-split\win-x64`: passed.
- `dotnet list asutpKB.csproj package --include-transitive`: passed; the main app package graph does not include `PdfPig`.
- `dotnet list src\AsutpKnowledgeBase.PdfImport\AsutpKnowledgeBase.PdfImport.csproj package --include-transitive`: passed; `PdfPig 0.1.14` is present in the PDF module.
- `dotnet build asutpKB.csproj --configuration Release --no-restore -v:minimal`: passed with existing warnings, 0 errors.
- `dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore -v:minimal`: passed, 410 tests.
- `dotnet run --project artifacts\pdf-import-load-smoke\PdfImportLoadSmoke.csproj --configuration Release --no-restore -p:RunAnalyzers=false -p:WarningLevel=0 -- artifacts\publish-pdf-split\win-x64\pdf-import\AsutpKnowledgeBase.PdfImport.dll`: passed; the cleaned published module loads, successfully parses valid calendar text through `ImportText`, and reaches `PdfPig` through `ImportPdf` with invalid bytes without missing-dependency failures.

Published size check:

- Main review exe: `71.81 MB`
- Full review folder including `pdf-import`: `87.75 MB`
- `pdf-import` folder: `5.46 MB`
- `pdf-import` file count: `10`

Manual GUI/PDF review was not run by Codex. No real PDF file was available in the repository, so final import-through-menu validation still needs a real production-calendar PDF and an interactive app session.

## Decisions already made

- The main app/core must not reference `PdfPig` directly.
- PDF import is an optional on-demand module under `pdf-import`.
- If `pdf-import\AsutpKnowledgeBase.PdfImport.dll` is missing, the app shows a user-facing warning before opening the PDF file dialog.
- The test project may reference the PDF module so existing parser tests continue to run.
- `SingleFile` publish should keep compression enabled; the faster-starting package remains the folder/ReadyToRun publish path.
- A separate abstractions project is not needed for the current PDF split because post-publish cleanup safely removes the OpenXML/SQLite files from `pdf-import` and the cleaned module load smoke passes.

## Files already relevant to the task

- `Forms\MainForm.cs`
- `Forms\MainForm.ProductionCalendar.cs`
- `Forms\KnowledgeBaseProductionCalendarPdfImportPreviewForm.cs`
- `Services\KnowledgeBaseProductionCalendarPdfImportContracts.cs`
- `UiServices\KnowledgeBaseProductionCalendarPdfImportPluginLoader.cs`
- `src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj`
- `src\AsutpKnowledgeBase.PdfImport\AsutpKnowledgeBase.PdfImport.csproj`
- `src\AsutpKnowledgeBase.PdfImport\KnowledgeBaseProductionCalendarPdfImportService.cs`
- `tests\AsutpKnowledgeBase.Core.Tests\KnowledgeBaseProductionCalendarPdfImportServiceTests.cs`
- `tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj`
- `scripts\publish.ps1`

## Known risks / open questions

- The main `exe` is smaller only by the compressed size contribution of `PdfPig`; the big startup/runtime wins still come from folder publish, lazy startup work, or removing other large assets/dependencies.
- The PDF module publish still has core transitive dependencies in its dependency graph because it references core models/services, but `scripts\publish.ps1` removes the unused OpenXML/SQLite runtime files from `pdf-import` after publish.
- The missing-module path is covered by code/build review, not by a GUI smoke.
- The real PDF import path is covered by existing parser unit tests plus the cleaned-module load smoke, not by importing an actual PDF through the WinForms menu in this session.

## Recommended next step

Manual review from `C:\Users\Olga\AKB5\artifacts\publish-pdf-split\win-x64\asutpKB.exe`:

1. Start the app with the `pdf-import` folder present and import a real production-calendar PDF.
2. Temporarily run/copy the package without `pdf-import` and confirm PDF import reports that the module is missing.
3. Confirm ordinary non-PDF app startup and work do not require `pdf-import`.

If accepted, request fresh explicit commit/push approval in the active chat.

## Commands to run before finishing future implementation work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet format src\AsutpKnowledgeBase.PdfImport\AsutpKnowledgeBase.PdfImport.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish.ps1 -Configuration Release -RuntimeIdentifier win-x64 -PublishMode SingleFile -OutputDirectory artifacts\publish-pdf-split\win-x64
```
