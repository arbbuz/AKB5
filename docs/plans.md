# Plans

Last updated: `2026-06-18`

## Active plan

- Work in `C:\Users\Olga\AKB5` on branch `Net`, tracking `origin/Net`.
- Current task: PDF production-calendar import has been split out of the main `asutpKB.exe` into an optional `pdf-import` module. Implementation and validation are complete locally; manual review is pending.
- Do not commit or push without fresh direct approval in the current chat.
- Do not reintroduce hover/popup tooltips; guidance must be visible, inline, status-based, or modal.
- Treat old AKB5 worktrees and snapshots under `C:\Users\Olga\Documents\Codex\...` as historical references only, not as source of truth.

## Completed package

- Removed the direct `PdfPig` package reference from `src\AsutpKnowledgeBase.Core\AsutpKnowledgeBase.Core.csproj`.
- Moved the existing PDF parser into the new `src\AsutpKnowledgeBase.PdfImport` project.
- Kept shared PDF-import contracts in core-linked code so WinForms preview/app code can use the result DTO without referencing `PdfPig`.
- Added on-demand plugin loading from `pdf-import\AsutpKnowledgeBase.PdfImport.dll`.
- Added a missing-module warning path before the PDF file dialog opens.
- Updated `scripts\publish.ps1` so `SingleFile` publish creates the main app and a sibling `pdf-import` module folder.
- Added post-publish cleanup so `pdf-import` keeps only `AsutpKnowledgeBase.PdfImport.*` and `UglyToad.PdfPig*` files instead of OpenXML/SQLite runtime files.
- Enabled single-file compression for `SingleFile` publish to keep the size comparable to the current compressed review exe.
- Kept existing PDF parser tests by referencing the PDF module from the test project.
- Verified that a separate shared abstractions project is not needed for the current split.

## Current implementation plan

- No further code implementation is active until manual review feedback.
- Manual review should use `C:\Users\Olga\AKB5\artifacts\publish-pdf-split\win-x64\asutpKB.exe` with the adjacent `pdf-import` folder.
- Check these scenarios manually:
  - ordinary app startup/work without using PDF import;
  - production-calendar PDF import with `pdf-import` present;
  - clear warning when `pdf-import` is absent.
- If the review is accepted, request explicit commit/push approval before any git action.

## Validation summary

- App/core/tests/PDF-module format checks passed.
- Release app build passed with existing warnings, 0 errors.
- Full core test suite passed: 410 tests.
- `scripts\publish.ps1` single-file review publish passed.
- Main app package graph no longer contains `PdfPig`; PDF module package graph contains `PdfPig 0.1.14`.
- Cleaned published PDF module load smoke passed: the published module loads, parses valid calendar text, and reaches `PdfPig` without missing-dependency failures.
- Published review sizes: `asutpKB.exe` 71.81 MB, full folder 87.75 MB, `pdf-import` 5.46 MB with 10 files.

## Not active / out of scope

- Committing, pushing, merging, rebasing, or reverting `AGENTS.md`.
- Reworking startup into asynchronous/deferred database loading.
- Splitting OpenXML/Excel dependencies out of the main app.
- Removing or changing SQLite `.akb` storage behavior.
- Adding OCR or embedded PDF preview/rendering.
- Adding a separate shared abstractions project unless a future check proves the post-publish cleanup is insufficient.

## Update rule

- Keep only active and near-term plans here.
- Remove completed or rejected items instead of growing a history log.
