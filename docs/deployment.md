# Publish and Deployment

## Поддерживаемая архитектура

Поддерживаемый publish target только один:

- `win-x64`

`arm64` и `win-arm64` intentionally не поддерживаются.

## Reproducible publish flow

Основной entrypoint:

```powershell
scripts\publish.cmd
```

Он вызывает [scripts/publish.ps1](../scripts/publish.ps1), который публикует root-проект [asutpKB.csproj](../asutpKB.csproj) как:

- `Release`
- `win-x64`
- `self-contained`
- `single-file` executable with bundled native runtime dependencies
- без trimming
- без AOT

Эквивалентная команда:

```bash
dotnet publish asutpKB.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish/win-x64
```

Output directory:

- `artifacts/publish/win-x64`

Главный файл:

- `artifacts/publish/win-x64/asutpKB.exe`

## Почему publish настроен именно так

- publish идёт из root WinForms-проекта, а не из `src/AsutpKnowledgeBase.Core`
- `self-contained` нужен только для publish артефакта конечному пользователю
- single-file нужен для более простого распространения без обязательных sidecar runtime DLL рядом с `.exe`
- trimming и AOT не включаются, потому что их безопасность для WinForms/Open XML сценария не доказана
- ordinary `build/debug` не переводится в global self-contained mode

## CI

Windows workflow [windows-build.yml](../.github/workflows/windows-build.yml) сохраняет текущий `build-and-test` path и теперь работает так:

- `pull_request` запускает только `build-and-test`
- `push` в `icon` и `interface` запускает `build-and-test` и publish
- manual run через `workflow_dispatch` тоже запускает publish

Publish job при этом:

- выполняет `scripts\publish.cmd`
- проверяет наличие `artifacts/publish/win-x64/asutpKB.exe`
- загружает publish output как artifact `asutpkb-win-x64-single-file`

## Как запускать на пользовательском ПК

1. Получить `artifacts/publish/win-x64/asutpKB.exe` локально или скачать CI artifact.
2. Перенести `asutpKB.exe` на 64-битный Windows ПК.
3. Запустить `asutpKB.exe`.
4. Для запуска не нужен отдельный установленный .NET runtime, потому что publish self-contained.
