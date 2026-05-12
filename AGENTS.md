# AGENTS.md

## Project snapshot

- `AKB5` is a WinForms knowledge-base app for ASUTP on `.NET 8`.
- Root app project: `asutpKB.csproj` with `TargetFramework=net8.0-windows` and `UseWindowsForms=true`.
- Entry point: `Program.cs`, which boots `MainForm`.
- Current engineering mode: pragmatic refactoring and stabilization, not rewrite.
- The active roadmap implementation branch is currently `to`; `main` remains the stable branch.
- Roadmap phases `0` through `7F.1` are implemented on `to`; the narrow `phase7g-annual-norm-hidden-rows` fix is committed/pushed on `to` as `7a4895d Fix annual maintenance norm import totals`.
- `Phase 11. Object templates and equipment catalog` is accepted through `Phase 11G`; `Phase 11A. Equipment catalog model` passed manual review, `Phase 11B. Equipment catalog UI` is committed/pushed on `to`, `Phase 11C` / `Phase 11D` are accepted and committed/pushed on `to`, `Phase 11E` / `Phase 11F` are accepted and committed/pushed on `to`, and `Phase 11G. Template import/export` is accepted and committed/pushed on `to`. `Phase 12. Storage redesign, backups, snapshots, and change history` is accepted through `Phase 12S8. Change history` and committed/pushed on `to`.
- Latest accepted follow-ups: `phase7e-annual-norm-import` supports importing maintenance norms from annual workbooks with the same structure as `456.xlsx`; `phase7g-annual-norm-hidden-rows` skips hidden annual rows for retired equipment; the first menu-rework iteration is committed/pushed on `to` as `8dfffbd Rework menu structure and safety prompts`.
- Current builds use portable-first SQLite single-file `.akb` storage: `akb5.settings.json` is stored next to `asutpKB.exe`, the default database is `database\knowledge-base.akb` next to the program, and JSON remains import/export plus first-launch migration compatibility. Excel exchange is a separate import/export layer.
- Current Excel implementation uses `DocumentFormat.OpenXml` and `WorkbookFormatVersion = 3`. Legacy `v1/v2` import is no longer supported.
- CI now enforces `dotnet format --verify-no-changes` for the WinForms app, core library, and tests before `build`/`test`.
- The active task context is always kept in `docs/codex-handoff.md`. Read it before planning changes; it is the source of truth for current completed phase, validation status, and next step.
- Chat progress must stay semantic and short. Do not add separate narrative after transcript/tool items such as `Ran`, `Running`, or `Edited`; without an explicit request, only send brief progress statements and the final result.
- Current-task authorization is mandatory: never carry an older user approval for `git commit`, `git push`, destructive file operations, or broad database edits into a new request. Those actions require direct approval in the latest user request.
- Before editing a real `.akb`/JSON data file, state the exact file path, target object count, and fields/records that will be changed. If a visible tree object is not found, check console encoding, the active data file, process state, and alternate storage before concluding that the object is absent.
- Before final response, verify and report only the essential facts: changed files/data, validation result, whether git was touched, and whether the latest user request authorized it.
- The session knowledge harness is split by role:
  - `docs/codex-handoff.md` for current state
  - `docs/plans.md` for active plans
  - `docs/lessons-learned.md` for reusable patterns and insights
  - `docs/decision-log.md` for durable decisions and working agreements

## Repository map

- `Program.cs`: application entry point.
- `Forms/`: WinForms screens. `Forms/MainForm.cs` is the main shell and still contains screen-level orchestration.
- `Controls/`: reusable WinForms controls, including the typed right-panel screens and the custom `KnowledgeBaseTreeView`.
- `UiServices/`: WinForms-only workflow/services for dialogs, tree view binding, Excel UI actions, workshop/config flows.
- `Models/`: domain models shared by app and tests.
- `Services/`: non-UI logic, JSON storage, session/file workflows, tree workflows, Excel workbook parsing/reading/writing.
- `src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj`: core library project. It currently links `../../Models/**/*.cs` and `../../Services/**/*.cs`; source files are not physically moved there yet.
- `tests/AsutpKnowledgeBase.Core.Tests/`: xUnit tests for core logic.
- `.github/workflows/windows-build.yml`: the only CI workflow currently in repo.
- `docs/`: AI handoff and knowledge harness. Use `docs/codex-handoff.md` as the current task state, `docs/plans.md` for active plans, `docs/lessons-learned.md` for distilled insights, `docs/decision-log.md` for durable decisions, `docs/codex-handoff-template.md` for new handoffs, and `docs/codex-start-prompt.md` to start a clean AI session.
- `scripts/`: repository automation entrypoints, including publish flow for the root WinForms app.

## Architecture boundaries

- Do not rewrite WinForms into MVP/MVVM unless the task explicitly requires it.
- Portable-first storage and external `.akb` backups are locally verified and awaiting review; do not start an unrelated coding phase until this follow-up is accepted.
- The SQLite plan is approved with choices `1A, 2B, 3A, 4A`: use `.akb`, confirm first-launch migration, create a post-migration JSON safety export, and do not support simultaneous multi-user editing in the first SQLite version. The later approved storage follow-up makes the app portable-first and creates external timestamped backups under `backups\yyyy-MM-dd\` before overwriting an existing `.akb`.
- Replace direct `JsonStorageService` dependencies through a storage abstraction before adding SQLite code; do not move Excel logic into storage services.
- Keep WinForms-specific behavior in `Forms/` and `UiServices/`.
- Keep testable non-UI logic in `Models/` and `Services/` / core-linked code.
- Treat `MainForm` as a thin-shell target. Extract behavior gradually with small diffs.
- Respect the live Excel contract unless the task explicitly changes it: sheets `Meta`, `Levels`, `Workshops`, plus one worksheet of nodes per workshop; `WorkbookFormatId = AKB5.ExcelExchange`; export/import version `3` only.
- Do not claim Open XML SDK or self-contained single-file publish already exists unless you actually add and verify it.

## Mandatory read order for a new session

1. `AGENTS.md`
2. `docs/codex-handoff.md`
3. `docs/decision-log.md`
4. `docs/plans.md`
5. `docs/lessons-learned.md`
6. `Roadmap.md`
7. `README.md`
8. `asutpKB.csproj`
9. `src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj`
10. `tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj`
11. `.github/workflows/windows-build.yml`
12. Relevant implementation files for the active task:
   `Forms/MainForm.cs`,
   `Forms/MainForm.Layout.cs`,
   `Forms/MainForm.WorkspaceHost.cs`,
   `Controls/KnowledgeBaseTreeView.cs`,
   `UiServices/KnowledgeBaseTreeNodeVisuals.cs`,
   `UiServices/KnowledgeBaseTreeViewService.cs`,
   `Services/KnowledgeBaseCompositionStateService.cs`,
   `Services/KnowledgeBaseCompositionMutationService.cs`,
   plus task-specific files listed in `docs/codex-handoff.md`
13. Run `git status --short` before planning edits.

## Build / test / publish commands

Use the same commands as the existing CI workflow when `.NET SDK` is available:

```bash
git status --short
dotnet restore asutpKB.csproj
dotnet restore tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity error --no-restore
dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore
dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
dotnet publish asutpKB.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish/win-x64
```

Publish:

- Use `scripts/publish.ps1` or `scripts/publish.cmd` for the supported end-user publish flow.
- Supported publish target is only `win-x64`; do not add `arm64` / `win-arm64` flows unless the task explicitly changes that decision.
- Keep `SelfContained`, `PublishSingleFile`, trimming, and AOT scoped to publish only; do not enable them globally for ordinary build/debug.

## Git branch workflow

- `main` is the stable branch. Do not use it as the default working branch for ordinary task implementation.
- `to` is the current active integration branch for the typed-workspace and maintenance-planning roadmap work tracked in `docs/codex-handoff.md` and `Roadmap.md`.
- For tasks that continue the current roadmap stream, stay on `to` unless the user explicitly redirects the work to another branch.
- Only when the user explicitly asks to "push to main" should Codex prepare a PR or handoff from the active working branch to `main`.
- Do not push task branches directly to `main` unless the user explicitly overrides this workflow.
- If another local branch/worktree contains unfinished changes, keep them isolated and do not mix them into `to` without the user's approval.

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

## User-visible communication and context budget

- During implementation, user-visible updates must be short stage reports: current step, result, and next step.
- Do not paste diagnostic commands, raw command transcripts, internal ids, owner mappings, large diffs, or code excerpts into chat unless the user explicitly asks for them.
- Keep verbose logs, traces, and diagnostic details in `artifacts`; summarize only pass/fail status, key counts, changed files, and artifact paths.
- If context usage is rising or a task needs heavy diagnostics, checkpoint the state and continue in a fresh/forked session instead of filling the chat.

## End-of-session handoff update rules

- At the end of every session, check whether `docs/codex-handoff.md` is still current.
- Update `docs/codex-handoff.md` in the same session whenever the system state, constraints, decisions, validated status, or task direction changed.
- Refresh the sections `Current objective`, `Current repo state`, `Decisions already made`, `Files already relevant to the task`, `Known risks / open questions`, `Recommended next step`, and `Commands to run before finishing future implementation work`.
- Keep the handoff concise and current. Replace stale statements instead of appending a transcript.
- If validation was not run, say so in the handoff.
- If nothing relevant changed, leave the handoff consistent and do not add noise.

## Session knowledge distillation

- On the explicit user command `дистиллируй знания из сессии`, distill reusable knowledge from the current session into the fixed harness files in `docs/`.
- Sort information by role instead of chronology:
  - current status -> `docs/codex-handoff.md`
  - active next steps -> `docs/plans.md`
  - reusable patterns/insights -> `docs/lessons-learned.md`
  - durable decisions/agreements -> `docs/decision-log.md`
- Update existing files in place. Do not create duplicate session notes for the same purpose.
- Replace stale information instead of appending obsolete history.
