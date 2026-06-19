# Plans

Last updated: `2026-06-18`

## Active plan

- Work in `C:\Users\Olga\AKB5` on branch `Net`, tracking `origin/Net`.
- Current task: OpenXML/Excel exchange has been split out of the main `asutpKB.exe` into an optional `excel-exchange` module. Implementation and validation are complete locally; manual review is pending.
- Do not commit or push without fresh direct approval in the current chat.
- Do not reintroduce hover/popup tooltips; guidance must be visible, inline, status-based, or modal.
- Treat old AKB5 worktrees and snapshots under `C:\Users\Olga\Documents\Codex\...` as historical references only, not as source of truth.

## Completed package

- Removed the direct `DocumentFormat.OpenXml` package reference and Excel template resources from `src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj`.
- Added shared core-linked contracts for database Excel exchange, maintenance norm import, maintenance workbook generation, and year-schedule source exchange.
- Moved the existing OpenXML implementations into the new `src\AsutpKnowledgeBase.ExcelExchange` project.
- Added one plugin facade, `KnowledgeBaseExcelExchangePlugin`, for all Excel-related contracts.
- Added on-demand plugin loading from `excel-exchange\AsutpKnowledgeBase.ExcelExchange.dll`.
- Updated database Excel UI and maintenance Excel UI flows to use the loader and warn before file dialogs when the module is missing.
- Updated `scripts\publish.ps1` so `SingleFile` publish creates the main app plus sibling `pdf-import` and `excel-exchange` module folders.
- Added post-publish cleanup so `excel-exchange` keeps only `AsutpKnowledgeBase.ExcelExchange.*`, `DocumentFormat.OpenXml*`, and `System.IO.Packaging.dll` files instead of SQLite/core runtime files.
- Kept existing Excel/OpenXML tests by referencing the Excel module from the test project.
- Verified that a separate shared abstractions project is not needed for the current split.

## Current implementation plan

- No further code implementation is active until manual review feedback.
- Manual review should use `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe` with adjacent `excel-exchange` and `pdf-import` folders.
- Check these scenarios manually:
  - ordinary app startup/work without using Excel import/export;
  - database Excel export/import with `excel-exchange` present;
  - maintenance norm import and maintenance workbook generation/export;
  - year-schedule source export/import;
  - clear warning when `excel-exchange` is absent.
- If the review is accepted, request explicit commit/push approval before any git action.

## Validation summary

- App/core/tests/Excel-module format checks passed.
- Release app build passed with 0 errors.
- Full core test suite passed: 410 tests.
- Targeted Excel tests passed: database exchange 37, maintenance norm import 20, maintenance workbook 35, year-schedule source 10.
- `scripts\publish.ps1` single-file review publish passed.
- Main app package graph no longer contains `DocumentFormat.OpenXml`.
- Cleaned published Excel module load smoke passed: the published module loads, implements all expected contracts, and completes a basic export/import round-trip on an empty model.
- Published review sizes: `asutpKB.exe` 73,141,213 bytes (`69.75 MiB`), `excel-exchange` 7.39 MiB with 6 files, full folder 93.01 MiB.

## Not active / out of scope

- Committing, pushing, merging, rebasing, or reverting `AGENTS.md`.
- Reworking startup into asynchronous/deferred database loading.
- Removing or changing SQLite `.akb` storage behavior.
- Adding OCR or embedded PDF preview/rendering.
- Adding a separate shared abstractions project unless a future check proves the post-publish cleanup is insufficient.

## Update rule

- Keep only active and near-term plans here.
- Remove completed or rejected items instead of growing a history log.
