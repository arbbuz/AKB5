# Current objective

- Keep Windows CI aligned with the repository branch model after the logging rollout.
- Limit the current GitHub automation/scripts task to a minimal hardening diff: correct `push` branches, set explicit artifact retention, and preserve the existing `win-x64` publish flow.

# Current repo state

- Main working repository: `/Users/home/ASUTP/AKB5` on branch `development`.
- Parallel worktree `/Users/home/ASUTP/AKB5-ci-hardening` still exists, but this rollout was done in the main repository only.
- The working tree still contains unrelated logging-rollout changes in `Program.cs`, `Forms/MainForm.cs`, several `Services/*` files, related tests, and new logger files. Do not mix or revert them unintentionally while handling this CI/docs diff.
- This session updated:
  - `.github/workflows/windows-build.yml`
  - `README.md`
  - `docs/deployment.md`
  - `docs/codex-handoff.md`
- The Windows workflow now:
  - keeps `pull_request` for `build-and-test`
  - listens to `push` on `development` and `main`
  - keeps `workflow_dispatch`, `permissions`, and `concurrency`
  - uses `actions/checkout@v6`, `actions/setup-dotnet@v5`, and `actions/upload-artifact@v7`
  - uploads `asutpkb-win-x64-single-file` from `artifacts/publish/win-x64` with `retention-days: 14`
- `scripts/publish.cmd` and `scripts/publish.ps1` were inspected only. No changes were required; they still enforce `win-x64` and `artifacts/publish/win-x64`.

# Decisions already made

- `master` support was removed from the workflow trigger because there is no local or `origin/*` `master` branch, while repository rules in `AGENTS.md` explicitly use `development` as the integration branch and `main` as the stable branch.
- Artifact retention was set to `14` days because no repository-wide alternative standard was found and this is a reasonable CI artifact lifetime.
- No path filters, build matrix, additional OS targets, or extra runtime targets were added.
- Documentation was updated only where it directly described the changed workflow behavior. Application code and publish scripts were left untouched.

# Files already relevant to the task

- `AGENTS.md`
- `.github/workflows/windows-build.yml`
- `README.md`
- `docs/deployment.md`
- `docs/codex-handoff.md`
- `scripts/publish.cmd`
- `scripts/publish.ps1`

# Validation performed in this session

Actually executed:

- `git status --short`
- `git branch -a --no-color`
- `rg -n "\bmaster\b|\bdevelopment\b|\bmain\b" -g '!bin' -g '!obj' -g '!artifacts' .`
- `sed -n '1,220p' scripts/publish.cmd`
- `sed -n '1,260p' scripts/publish.ps1`
- `/Users/home/.dotnet/dotnet restore asutpKB.csproj`
- `/Users/home/.dotnet/dotnet restore tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj`
- `/Users/home/.dotnet/dotnet format asutpKB.csproj --verify-no-changes --severity warn --no-restore`
- `/Users/home/.dotnet/dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity warn --no-restore`
- `/Users/home/.dotnet/dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity warn --no-restore`
- `/Users/home/.dotnet/dotnet build asutpKB.csproj --configuration Release --no-restore`
- `/Users/home/.dotnet/dotnet build tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore`
- `/Users/home/.dotnet/dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore`
- `/Users/home/.dotnet/dotnet publish asutpKB.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish/win-x64`
- verified `artifacts/publish/win-x64/asutpKB.exe`
- `git diff --check`

Observed results:

- all restore / format / build / test commands succeeded
- tests: `93` passed, `0` failed
- direct `dotnet publish` succeeded and produced `artifacts/publish/win-x64/asutpKB.exe`
- publish still emits the existing analyzer-warning baseline during the publish build step
- `git diff --check` is clean
- neither `pwsh` nor `powershell` is installed in this environment, so the wrapper path through `scripts/publish.cmd` / `scripts/publish.ps1` was not executed end-to-end here

# Known risks / open questions

- The wrapper execution path through `scripts/publish.cmd` / `scripts/publish.ps1` still needs a Windows or PowerShell-capable environment for direct end-to-end confirmation.
- Unrelated logging-rollout changes remain in the working tree and should stay isolated from this CI/docs diff unless the user explicitly wants them combined.
- This rollout intentionally did not broaden scope into other CI changes beyond branch alignment, action-version refresh on the touched workflow file, and artifact retention.

# Recommended next step

- Let GitHub Actions run on `development` (or use `workflow_dispatch`) to confirm the updated hosted-Windows path for checkout/upload-artifact versions and the new artifact retention setting.
- If the user wants to ship this diff separately, isolate or commit the CI/docs files apart from the existing logging-rollout changes.

# Commands to run before finishing future implementation work

```bash
git status --short
/Users/home/.dotnet/dotnet restore asutpKB.csproj
/Users/home/.dotnet/dotnet restore tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj
/Users/home/.dotnet/dotnet format asutpKB.csproj --verify-no-changes --severity warn --no-restore
/Users/home/.dotnet/dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity warn --no-restore
/Users/home/.dotnet/dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity warn --no-restore
/Users/home/.dotnet/dotnet build asutpKB.csproj --configuration Release --no-restore
/Users/home/.dotnet/dotnet build tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
/Users/home/.dotnet/dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
/Users/home/.dotnet/dotnet publish asutpKB.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish/win-x64
git diff --check
```
