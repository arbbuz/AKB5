# Current objective

- Минимальный lint/analyzer baseline для `AKB5` уже внедрён.
- Дальше нужно сохранять зелёный `dotnet format` gate и не расширять rollout в большой analyzer-cleanup без отдельной задачи.
- Excel exchange contract `v3`, publish target `win-x64` и существующий publish flow остались без изменений.

# Current repo state

- Основной рабочий репозиторий: `/Users/home/ASUTP/AKB5` на ветке `development`.
- Параллельный worktree `/Users/home/ASUTP/AKB5-ci-hardening` на `main` в этот rollout не входит.
- В репозитории теперь добавлены:
  - root `Directory.Build.props` с baseline для встроенных analyzers `.NET 8`
  - root `.editorconfig` с минимальными formatting/code-style правилами для `*.cs`
- `.github/workflows/windows-build.yml` теперь запускает три шага `dotnet format --verify-no-changes --severity warn --no-restore` до `dotnet build` и `dotnet test`.
- `README.md` отражает новый порядок локальной верификации: `restore -> format verify -> build -> test`.
- Минимальный cleanup под gate выполнен через `dotnet format`; он затронул только low-risk правки:
  - удаление unused `using`
  - whitespace/newline cleanup
  - одна безопасная simplification в тесте (`StartsWith("/")` -> `StartsWith('/')`)

# Decisions already made

- Используем только встроенный analyzer baseline SDK .NET 8:
  - `AnalysisLevel = 8.0-recommended`
  - `EnforceCodeStyleInBuild = true`
  - `CodeAnalysisTreatWarningsAsErrors = false`
- Не добавляем `StyleCop.Analyzers`.
- Не добавляем `Microsoft.CodeAnalysis.NetAnalyzers` как NuGet package.
- Не включаем глобально `TreatWarningsAsErrors=true`.
- Не вводим `.ruleset`.
- CI должен выполнять `dotnet format --verify-no-changes --severity warn --no-restore` отдельно для:
  - `asutpKB.csproj`
  - `src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj`
  - `tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj`
- `publish-win-x64` job, publish scripts и publish target `win-x64` не меняются.
- Новые analyzer diagnostics, появившиеся после `AnalysisLevel = 8.0-recommended`, остаются warnings на этом шаге rollout и не блокируют build/test.

# Files already relevant to the task

- `Directory.Build.props`
- `.editorconfig`
- `.github/workflows/windows-build.yml`
- `README.md`
- `docs/codex-handoff.md`
- `Services/KnowledgeBaseService.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseExcelExchangeServiceTests.cs`
- `asutpKB.csproj`
- `src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj`
- `tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj`

# Validation performed in this session

Фактически выполнено:

- `git status --short`
- `/Users/home/.dotnet/dotnet restore asutpKB.csproj`
- `/Users/home/.dotnet/dotnet restore tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj`
- pre-change dry-run baseline check показал format-нарушение в `Services/KnowledgeBaseService.cs`
- `/Users/home/.dotnet/dotnet format asutpKB.csproj --severity warn --no-restore`
- `/Users/home/.dotnet/dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --severity warn --no-restore`
- `/Users/home/.dotnet/dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --severity warn --no-restore`
- `/Users/home/.dotnet/dotnet format asutpKB.csproj --verify-no-changes --severity warn --no-restore`
- `/Users/home/.dotnet/dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity warn --no-restore`
- `/Users/home/.dotnet/dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity warn --no-restore`
- `/Users/home/.dotnet/dotnet build asutpKB.csproj -c Release --no-restore`
- `/Users/home/.dotnet/dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj -c Release --no-restore`
- `git diff --check`

Observed results:

- все три `dotnet format --verify-no-changes` прошли успешно
- release build: success, `47` warnings, `0` errors
- core test suite: success, `90` passed, `0` failed
- `git diff --check`: clean
- дополнительный `dotnet publish` не запускался, так как lint baseline не показал признаков влияния на publish path

# Known risks / open questions

- Build и test теперь показывают analyzer warnings из baseline `.NET 8`:
  - примеры: `CA1822`, `CA1707`, `CA1305`, `CA1859`, `CA1861`
  - они не эскалированы в ошибки по дизайну первого rollout
- Если команда захочет уменьшить warning noise, это стоит делать отдельной задачей, а не внутри минимального lint baseline.

# Recommended next step

- Сохранять новый `dotnet format` gate зелёным для всех трёх проектов.
- При отдельном запросе можно сделать следующий шаг: целевой cleanup analyzer warnings без изменения publish/deployment flow.

# Commands to run before finishing future implementation work

```bash
git status --short
/Users/home/.dotnet/dotnet restore asutpKB.csproj
/Users/home/.dotnet/dotnet restore tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj
/Users/home/.dotnet/dotnet format asutpKB.csproj --verify-no-changes --severity warn --no-restore
/Users/home/.dotnet/dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity warn --no-restore
/Users/home/.dotnet/dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity warn --no-restore
/Users/home/.dotnet/dotnet build asutpKB.csproj -c Release --no-restore
/Users/home/.dotnet/dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj -c Release --no-restore
git diff --check
```
