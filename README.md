# AKB5

## Что это

`AKB5` — WinForms-приложение на C# / .NET 8 для ведения древовидной базы знаний по АСУТП.

Базовый source of truth приложения — локальный JSON-файл с:

- `SchemaVersion`
- `Config`
- `Workshops`
- `LastWorkshop`

Excel нужен как редактируемый exchange-формат для выгрузки, ручной правки и обратного импорта.

## Структура репозитория

- [asutpKB.csproj](./asutpKB.csproj) — root WinForms-проект, который нужно `restore/build/publish`
- [Program.cs](./Program.cs) — точка входа
- [Forms/MainForm.cs](./Forms/MainForm.cs) — главный UI shell
- [Models](./Models) — shared domain models
- [Services](./Services) — не-UI логика, включая JSON storage и Excel exchange
- [UiServices](./UiServices) — WinForms-специфичные workflow/services
- [src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj](./src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj) — core library project для тестируемой логики
- [tests/AsutpKnowledgeBase.Core.Tests](./tests/AsutpKnowledgeBase.Core.Tests) — regression/unit tests
- [scripts/publish.ps1](./scripts/publish.ps1) и [scripts/publish.cmd](./scripts/publish.cmd) — reproducible publish flow
- [.github/workflows/windows-build.yml](./.github/workflows/windows-build.yml) — Windows CI

## Excel Editable Format v3

Единственный поддерживаемый Excel exchange-формат сейчас — workbook `v3`.

Workbook состоит из:

- `Meta`
- `Levels`
- `Workshops`
- отдельного листа узлов для каждого цеха

Подробный контракт описан в [docs/workbook-v3.md](./docs/workbook-v3.md).

Коротко по редактированию:

- редактируемые поля: `Levels.LevelName`, `Workshops.WorkshopName`, `Workshops.IsLastSelected`, `Nodes.NodeName`
- технические поля: `WorkshopOrder`, `WorkshopId`, `NodesSheetKey`, `NodeId`, `ParentNodeId`, `SiblingOrder`, `LevelIndex`
- производные/display-only поля: `Meta.LastWorkshop`, `Nodes.LevelName`, `Nodes.Path`
- поддерживаемые ручные изменения: rename уровней, rename цехов, rename узлов, смена выбранного цеха, перестановка колонок, добавление лишних пользовательских колонок, rename tab у листа узлов без поломки sheet metadata
- не поддерживаются: правки `FormatId/FormatVersion`, ручная коррекция технических идентификаторов и порядков, удаление обязательных листов/колонок, поломка связи `WorkshopId`/`NodesSheetKey`, возврат к legacy `v1/v2`

Примеры пользовательских ошибок, которые import должен ловить явно:

- `FormatVersion = 2` или другой неподдерживаемый формат
- duplicate `WorkshopName`, `WorkshopId` или `NodesSheetKey`
- невалидный `WorkshopOrder` / `LevelIndex`
- missing sheet узлов для строки из `Workshops`
- неизвестный `ParentNodeId` или broken node-sheet metadata

## Publish / Deployment

Поддерживаемый publish target только один:

- `win-x64`

`arm64` / `win-arm64` publish flow в проекте не поддерживается и не документируется.

Собрать self-contained single-file publish можно так:

```powershell
scripts\publish.cmd
```

или напрямую:

```bash
dotnet publish asutpKB.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish/win-x64
```

Дополнительные правила publish:

- publish идёт из root-проекта [asutpKB.csproj](./asutpKB.csproj), не из core project
- output лежит в `artifacts/publish/win-x64`
- основной исполняемый файл: `artifacts/publish/win-x64/asutpKB.exe`
- trimming и AOT намеренно не включаются
- обычный `build/debug` не становится global self-contained

Как запускать на пользовательском ПК:

1. Собрать publish flow на машине разработчика или взять CI artifact `win-x64`.
2. Скопировать содержимое `artifacts/publish/win-x64` на 64-битный Windows ПК.
3. Запустить `asutpKB.exe`.
4. Дополнительная установка .NET runtime на пользовательском ПК не требуется, потому что publish self-contained.

Automation сейчас работает так:

- `pull_request` запускает только `build-and-test`
- `push` в `main/master` запускает `build-and-test` и `publish-win-x64`
- ручной запуск через `workflow_dispatch` тоже собирает publish artifact
- job `build-and-test` теперь дополнительно валидирует форматирование и базовый code style через `dotnet format` + root `.editorconfig` до `dotnet build` и `dotnet test`

Детали по deployment и CI artifact собраны в [docs/deployment.md](./docs/deployment.md).

## Build / Test

Минимальная локальная верификация перед завершением изменений:

```bash
dotnet restore asutpKB.csproj
dotnet restore tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj
dotnet format asutpKB.csproj --verify-no-changes --severity warn --no-restore
dotnet format src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj --verify-no-changes --severity warn --no-restore
dotnet format tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --verify-no-changes --severity warn --no-restore
dotnet build asutpKB.csproj -c Release --no-restore
dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj -c Release --no-restore
dotnet publish asutpKB.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish/win-x64
```

Lint baseline намеренно минимальный:

- analyzer baseline задаётся через root `Directory.Build.props`
- formatting/code style baseline задаётся через root `.editorconfig`
- CI не превращает все analyzer warnings в build errors на этом шаге

## AI Handoff

- persistent guide для новой AI-сессии: [AGENTS.md](./AGENTS.md)
- текущее состояние задачи: [docs/codex-handoff.md](./docs/codex-handoff.md)
- reusable стартовый prompt для чистого диалога: [docs/codex-start-prompt.md](./docs/codex-start-prompt.md)
