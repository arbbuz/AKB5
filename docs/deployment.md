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

Он вызывает [scripts/publish.ps1](/Users/home/ASUTP/AKB5/scripts/publish.ps1), который публикует root-проект [asutpKB.csproj](/Users/home/ASUTP/AKB5/asutpKB.csproj) как:

- `Release`
- `win-x64`
- `self-contained`
- `single-file`
- без trimming
- без AOT

Эквивалентная команда:

```bash
dotnet publish asutpKB.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish/win-x64
```

Output directory:

- `artifacts/publish/win-x64`

Главный файл:

- `artifacts/publish/win-x64/asutpKB.exe`

## Почему publish настроен именно так

- publish идёт из root WinForms-проекта, а не из `src/AsutpKnowledgeBase.Core`
- `self-contained` нужен только для publish артефакта конечному пользователю
- single-file нужен для более простого распространения
- trimming и AOT не включаются, потому что их безопасность для WinForms/Open XML сценария не доказана
- ordinary `build/debug` не переводится в global self-contained mode

## CI

Windows workflow [windows-build.yml](/Users/home/ASUTP/AKB5/.github/workflows/windows-build.yml) сохраняет текущий `build-and-test` path и дополнительно запускает publish job, который:

- выполняет `scripts\publish.cmd`
- проверяет наличие `artifacts/publish/win-x64/asutpKB.exe`
- загружает publish output как artifact `asutpkb-win-x64-single-file`

## Как запускать на пользовательском ПК

1. Получить папку `artifacts/publish/win-x64` локально или скачать CI artifact.
2. Перенести папку на 64-битный Windows ПК.
3. Запустить `asutpKB.exe`.
4. Для запуска не нужен отдельный установленный .NET runtime, потому что publish self-contained.

Практическое правило распространения: переносить на пользовательский ПК всю папку publish output, а не только `exe`.
