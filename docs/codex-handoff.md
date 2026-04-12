# Current objective

- Зафиксировать постоянный git-workflow для дальнейшей работы через Codex:
  - обычные завершённые задачи пушатся в `development`
  - `main` обновляется только через отдельный PR `development -> main` по явному запросу пользователя
- Держать `main` чистым и стабильным, а `development` использовать как основную интеграционную ветку для следующих задач.
- Не смешивать текущую незавершённую UX-работу в `codex/main-ux-pass` с новым `development`, пока пользователь отдельно не решит её судьбу.

# Current repo state

- `origin/main` находится на коммите `597085e` (`[codex] Harden Windows CI automation (#2)`).
- Новая ветка `development` создана от текущего `main` на том же коммите `597085e` и запушена в `origin/development`.
- Локальные worktree сейчас такие:
  - `/Users/home/ASUTP/AKB5` -> `codex/main-ux-pass`
  - `/Users/home/ASUTP/AKB5-ci-hardening` -> `main`
  - `/Users/home/ASUTP/AKB5-development` -> `development`
- `/Users/home/ASUTP/AKB5` остаётся грязным working tree с незавершёнными локальными изменениями в:
  - `Forms/MainForm.WorkflowContexts.cs`
  - `Services/KnowledgeBaseFileWorkflowService.cs`
  - `UiServices/KnowledgeBaseFileUiWorkflowService.cs`
  - `docs/codex-handoff.md`
  - `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseFileWorkflowServiceTests.cs`
- `codex/main-ux-pass` синхронизирована со своим `origin/codex/main-ux-pass` по последнему commit, но имеет незакоммиченные правки поверх него.
- CI-hardening из предыдущей задачи уже находится в `main` и, следовательно, в новой `development`.

# Decisions already made

- `development` является основной веткой для дальнейшей интеграции завершённых задач.
- `main` не используется как обычная рабочая ветка для новых задач.
- По умолчанию Codex должен работать в чистом worktree `/Users/home/ASUTP/AKB5-development`.
- После завершения очередной задачи Codex пушит изменения в `development`.
- Только по явному запросу пользователя "push в main" Codex делает PR из `development` в `main`.
- Незавершённую ветку `codex/main-ux-pass` пока не вливать в `development` автоматически.
- Publish/CI decisions из предыдущей задачи остаются в силе:
  - publish target только `win-x64`
  - output path `artifacts/publish/win-x64`
  - artifact name `asutpkb-win-x64-single-file`
  - built-in NuGet cache пока не включается без отдельного решения по lock files

# Files already relevant to the task

- `AGENTS.md`
- `docs/codex-handoff.md`
- `.github/workflows/windows-build.yml`
- `.github/dependabot.yml`
- `README.md`
- `docs/deployment.md`

# Validation performed in this session

Фактически выполнено:

- inspected current local branches, remote branches, and worktrees;
- confirmed:
  - `main` is clean in `/Users/home/ASUTP/AKB5-ci-hardening`
  - `codex/main-ux-pass` is dirty in `/Users/home/ASUTP/AKB5`
  - `development` did not exist before this session
- created new clean worktree `/Users/home/ASUTP/AKB5-development`
- created local branch `development` from current `main`
- pushed `development` to `origin/development`

What was not re-run in this session:

- `dotnet restore/build/test/publish`
- GitHub Actions workflow runs
- PR validation against `main`

# Known risks / open questions

- Незавершённые локальные правки в `codex/main-ux-pass` всё ещё не разложены: их нужно либо завершить отдельно, либо позже осознанно перенести в `development`.
- Пока default branch на GitHub остаётся `main`; это нормально, но важно помнить, что обычная рабочая интеграция теперь должна идти через `development`.
- Если пользователь позже захочет включить старую UX-ветку в новый workflow, лучше делать это отдельным контролируемым merge/rebase шагом.

# Recommended next step

- Все новые обычные задачи начинать в `/Users/home/ASUTP/AKB5-development` на ветке `development`.
- Текущую незавершённую работу в `codex/main-ux-pass` не трогать, пока пользователь отдельно не попросит её закончить, разложить или перенести.
- Когда пользователь скажет "push в main", готовить PR именно из `development` в `main`.

# Commands to run before finishing future implementation work

```bash
git status --short
git diff --check
git fetch --prune origin
git branch -vv
git worktree list --porcelain
```
