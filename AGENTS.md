# AGENTS.md

Common Codex rules live in `C:\Users\Olga\.codex\AGENTS.md`. Do not duplicate them in this project file.

## Project snapshot

- `AKB5` is a WinForms knowledge-base app for ASUTP on `.NET 8`.
- Root app project: `asutpKB.csproj` with `TargetFramework=net8.0-windows` and `UseWindowsForms=true`.
- Entry point: `Program.cs`, which boots `MainForm`.
- Current engineering mode: pragmatic refactoring and stabilization, not rewrite.
- The active task branch is defined by `docs/codex-handoff.md`; at the 2026-05-20 handoff it is `Net`. `to` remains the completed roadmap/integration history branch, and `main` remains the stable branch.
- Roadmap phases `0` through `7F.1` are implemented on `to`; the narrow `phase7g-annual-norm-hidden-rows` fix is committed/pushed on `to` as `7a4895d Fix annual maintenance norm import totals`.
- `Phase 11. Object templates and equipment catalog` is accepted through `Phase 11G`; `Phase 11A. Equipment catalog model` passed manual review, `Phase 11B. Equipment catalog UI` is committed/pushed on `to`, `Phase 11C` / `Phase 11D` are accepted and committed/pushed on `to`, `Phase 11E` / `Phase 11F` are accepted and committed/pushed on `to`, and `Phase 11G. Template import/export` is accepted and committed/pushed on `to`. `Phase 12. Storage redesign, backups, snapshots, and change history` is accepted through `Phase 12S8. Change history` and committed/pushed on `to`.
- Relevant completed follow-ups: annual maintenance norm import and hidden-row handling are complete on `to`; the first menu-rework iteration is committed/pushed on `to` as `8dfffbd Rework menu structure and safety prompts`; the `Net` branch network passport manual-review ergonomics package is accepted, committed, and pushed as `207b6b1 Improve network passport review ergonomics`; the `Net` manual-entry hints package is accepted and pushed as `a89e593`; the `Net` mutation-service validation package is accepted and pushed as `b2ea12e Enforce network passport mutation validation`.
- Current accepted `Net` baseline is `1f87bb5 Improve network topology editing` on `HEAD` and `origin/Net`. Current UI/UX polishing continues separately in `C:\Users\Olga\AKB5-design` on `design/network-ui-polish`, which remains based on `a89e593` until the user explicitly asks to merge or rebase.
- Current builds use portable-first SQLite single-file `.akb` storage: `akb5.settings.json` is stored next to `asutpKB.exe`, the default database is `database\knowledge-base.akb` next to the program, and JSON remains import/export plus first-launch migration compatibility. Excel exchange is a separate import/export layer.
- Current Excel implementation uses `DocumentFormat.OpenXml` and `WorkbookFormatVersion = 3`. Legacy `v1/v2` import is no longer supported.
- CI now enforces `dotnet format --verify-no-changes` for the WinForms app, core library, and tests before `build`/`test`.
- The active task context is always kept in `docs/codex-handoff.md`. Read it before planning changes; it is the source of truth for current completed phase, validation status, and next step.
- Do not add hover/popup tooltips to the AKB5 UI. Avoid WinForms `ToolTip`, `ToolTipText`, item/row hover tooltips, and automatic grid cell tooltips; use visible labels, inline validation/status text, or modal validation messages instead.
- Before editing a real AKB5 `.akb`/JSON data file, identify the exact file path, target object count, and fields/records that will be changed.
- The session knowledge harness is split by role:
  - `docs/codex-handoff.md` for current state
  - `docs/plans.md` for active plans
  - `docs/lessons-learned.md` for reusable patterns and insights
  - `docs/decision-log.md` for durable decisions and working agreements
- Historical AKB5 worktrees/snapshots under `C:\Users\Olga\Documents\Codex\...` are reference material only. Do not use similarly named files there as the current source of truth for `C:\Users\Olga\AKB5`.

## AKB5 final response contract

- Before every final response after a code/data/doc change, run `C:\Users\Olga\.codex\scripts\codex-context-now.ps1` and include `Контекст: N%`.
- If a build/review executable exists, the first line must be a clickable Markdown link whose label is the full Windows path, for example `[C:\...\asutpKB.exe](<C:/.../asutpKB.exe>)`.
- Report only: what changed, validation run, unresolved gaps, git/data actions, and handoff status. Do not add apology/excuse paragraphs.
- If no build/review executable was produced, state that explicitly instead of omitting the artifact line silently.

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
- Portable-first SQLite `.akb` storage and external backups are the current build baseline. Do not reopen storage behavior unless the active task explicitly asks for it.
- The SQLite plan is approved with choices `1A, 2B, 3A, 4A`: use `.akb`, confirm first-launch migration, create a post-migration JSON safety export, and do not support simultaneous multi-user editing in the first SQLite version. The later approved storage follow-up makes the app portable-first and creates external timestamped backups under `backups\yyyy-MM-dd\` before overwriting an existing `.akb`.
- Replace direct `JsonStorageService` dependencies through a storage abstraction before adding SQLite code; do not move Excel logic into storage services.
- Keep WinForms-specific behavior in `Forms/` and `UiServices/`.
- Keep testable non-UI logic in `Models/` and `Services/` / core-linked code.
- Treat `MainForm` as a thin-shell target. Extract behavior gradually with small diffs.
- Respect the live Excel contract unless the task explicitly changes it: sheets `Meta`, `Levels`, `Workshops`, plus one worksheet of nodes per workshop; `WorkbookFormatId = AKB5.ExcelExchange`; export/import version `3` only.
- For `Net` branch network passport work, stay in manual-entry / manual-review ergonomics unless the user explicitly approves broader automation. Do not start OCR/PDF auto-import, PRONETA/CSV import, live scan, plan/fact comparison, data-quality issue panels, AKB5-driven IP/PROFINET-name assignment, or embedded PDF preview.
- For WinForms `Network` UI changes, use focused tests while developing and a non-invasive/offscreen layout-smoke before handoff. Do not run interactive UI-smoke unless the user explicitly asks.
- Do not claim Open XML SDK or self-contained single-file publish already exists unless you actually add and verify it.

## New-session read policy

Start every new AKB5 session with the light context set only:

1. `AGENTS.md`
2. `docs/codex-handoff.md`
3. `docs/plans.md`
4. Run `git status --short --branch` before planning edits.

Open the larger reference files only when the task needs them:

- `docs/decision-log.md` for durable decisions or scope questions.
- `docs/lessons-learned.md` for recurring pitfalls and validation patterns.
- `Roadmap.md` when choosing or changing a roadmap direction.
- `README.md`, project files, workflow files, and implementation files when they are directly relevant to the current task.

Do not reread a large file in the same session if its relevant sections were already loaded; use targeted `rg` / section reads instead.

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
- For tasks that continue the current documented task stream, stay on the branch named in `docs/codex-handoff.md`; do not fall back to `to` when the handoff names another active branch.
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
