# AGENTS.md

## Project snapshot

- `AKB5` is a WinForms knowledge-base app for ASUTP on `.NET 8`.
- Root app project: `asutpKB.csproj` with `TargetFramework=net8.0-windows` and `UseWindowsForms=true`.
- Entry point: `Program.cs`, which boots `MainForm`.
- Current engineering mode: pragmatic refactoring and stabilization, not rewrite.
- JSON remains the source of truth. Excel exchange is a separate import/export layer.
- Current Excel implementation uses `DocumentFormat.OpenXml` and `WorkbookFormatVersion = 3`. Legacy `v1/v2` import is no longer supported.
- The active task context is always kept in `docs/codex-handoff.md`. Read it before planning changes.

## Repository map

- `Program.cs`: application entry point.
- `Forms/`: WinForms screens. `Forms/MainForm.cs` is the main shell and still contains screen-level orchestration.
- `UiServices/`: WinForms-only workflow/services for dialogs, tree view binding, Excel UI actions, workshop/config flows.
- `Models/`: domain models shared by app and tests.
- `Services/`: non-UI logic, JSON storage, session/file workflows, tree workflows, Excel workbook parsing/reading/writing.
- `src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj`: core library project. It currently links `../../Models/**/*.cs` and `../../Services/**/*.cs`; source files are not physically moved there yet.
- `tests/AsutpKnowledgeBase.Core.Tests/`: xUnit tests for core logic.
- `.github/workflows/windows-build.yml`: the only CI workflow currently in repo.
- `docs/`: AI handoff system. Use `docs/codex-handoff.md` as the current task state, `docs/codex-handoff-template.md` for new handoffs, and `docs/codex-start-prompt.md` to start a clean AI session.
- `scripts/`: repository automation entrypoints, including publish flow for the root WinForms app.

## Architecture boundaries

- Do not rewrite WinForms into MVP/MVVM unless the task explicitly requires it.
- Do not replace JSON storage or move Excel logic into `JsonStorageService`.
- Keep WinForms-specific behavior in `Forms/` and `UiServices/`.
- Keep testable non-UI logic in `Models/` and `Services/` / core-linked code.
- Treat `MainForm` as a thin-shell target. Extract behavior gradually with small diffs.
- Respect the live Excel contract unless the task explicitly changes it: sheets `Meta`, `Levels`, `Workshops`, plus one worksheet of nodes per workshop; `WorkbookFormatId = AKB5.ExcelExchange`; export/import version `3` only.
- Do not claim Open XML SDK or self-contained single-file publish already exists unless you actually add and verify it.

## Mandatory read order for a new session

1. `AGENTS.md`
2. `docs/codex-handoff.md`
3. `README.md`
4. `asutpKB.csproj`
5. `src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj`
6. `tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj`
7. `.github/workflows/windows-build.yml`
8. Relevant implementation files for the active task:
   `Services/KnowledgeBaseExcelExchangeService.cs`,
   `Services/KnowledgeBaseXlsxWriter.cs`,
   `Services/KnowledgeBaseXlsxReader.cs`,
   `Services/KnowledgeBaseExcelWorkbookParser.cs`,
   `UiServices/KnowledgeBaseExcelUiWorkflowService.cs`,
   `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseExcelExchangeServiceTests.cs`
9. Run `git status --short` before planning edits.

## Build / test / publish commands

Use the same commands as the existing CI workflow when `.NET SDK` is available:

```bash
git status --short
dotnet restore asutpKB.csproj
dotnet restore tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj
dotnet build asutpKB.csproj --configuration Release --no-restore
dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
dotnet publish asutpKB.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish/win-x64
```

Publish:

- Use `scripts/publish.ps1` or `scripts/publish.cmd` for the supported end-user publish flow.
- Supported publish target is only `win-x64`; do not add `arm64` / `win-arm64` flows unless the task explicitly changes that decision.
- Keep `SelfContained`, `PublishSingleFile`, trimming, and AOT scoped to publish only; do not enable them globally for ordinary build/debug.

## Validation policy before completion

- Never claim `build` or `test` passed unless you actually ran the commands or have explicit CI evidence for the exact code under discussion.
- Never claim Excel round-trip is validated unless a real Windows + Excel open/edit/save/import smoke check was executed.
- If `dotnet` is missing locally, state that explicitly and limit claims to repository inspection, diff checks, and static reasoning.
- When touching Excel logic, prefer preserving or expanding the existing unit-test coverage in `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseExcelExchangeServiceTests.cs`.
- If publish flow is part of the task, validation is incomplete until the actual publish command and resulting artifact behavior are checked.

## Change-scope rules

- Default to the smallest coherent diff.
- Do not change application code for documentation-only tasks.
- Do not rename projects, workflow files, or workbook contract sheets without a direct task requirement.
- Do not physically move linked core files into `src/AsutpKnowledgeBase.Core` unless that migration is the task.
- For Excel redesign work, preserve backward-compatible import behavior for old workbook formats unless the user explicitly approves a breaking change.
- If the repo already contains useful AI-context files, merge and refresh them; do not overwrite blindly.

## Reporting format

Use a short final report with these sections:

- `What changed`
- `Why it matters`
- `Validation run`
- `Unresolved gaps`
- `Handoff updated`

Be explicit about what was inspected, what was executed, and what was not verified.

## End-of-session handoff update rules

- At the end of every session, check whether `docs/codex-handoff.md` is still current.
- Update `docs/codex-handoff.md` in the same session whenever the system state, constraints, decisions, validated status, or task direction changed.
- Refresh the sections `Current objective`, `Current repo state`, `Decisions already made`, `Files already relevant to the task`, `Known risks / open questions`, `Recommended next step`, and `Commands to run before finishing future implementation work`.
- Keep the handoff concise and current. Replace stale statements instead of appending a transcript.
- If validation was not run, say so in the handoff.
- If nothing relevant changed, leave the handoff consistent and do not add noise.
