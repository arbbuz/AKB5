# asutpKB

Локальное WinForms-приложение на C# / .NET 8 для ведения древовидной базы знаний по АСУТП.

## Назначение

Приложение хранит структуру по цехам, отделениям, оборудованию и связанным объектам. Данные сохраняются в JSON и редактируются через древовидный интерфейс.

Основные возможности:

- несколько цехов в одной базе
- поиск по дереву
- добавление, удаление и переименование узлов
- копирование и вставка поддеревьев
- drag-and-drop перемещение
- undo/redo
- настройка максимальной глубины и названий уровней

## Технологии

- C#
- .NET 8
- WinForms
- JSON-хранение

## Структура проекта

- `Program.cs` — точка входа
- `Forms/MainForm.cs` — главная форма и координация UI
- `Forms/InputDialog.cs` — диалог ввода строки
- `Forms/SetupForm.cs` — форма настройки уровней
- `Models/` — модели конфигурации и данных, собираются через `AsutpKnowledgeBase.Core`
- `Services/` — работа с JSON, деревом знаний и undo/redo, собираются через `AsutpKnowledgeBase.Core`
- `src/AsutpKnowledgeBase.Core/` — библиотека с не-UI логикой для переиспользования и тестов
- `tests/AsutpKnowledgeBase.Core.Tests/` — тесты для `Models` и `Services`
- `.github/workflows/windows-build.yml` — минимальный Windows CI-контур для сборки и тестов

## Запуск и сборка

Требования:

- Windows
- .NET SDK 8.0
- Desktop workload с поддержкой WinForms

Сборка:

```bash
dotnet build asutpKB.csproj
```

Запуск тестов для не-UI части:

```bash
dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj
```

Запуск:

```bash
dotnet run --project asutpKB.csproj
```

## Хранение данных

По умолчанию рабочая база сохраняется в:

`%USERPROFILE%\\Documents\\ASUTP_KnowledgeBase.json`

При успешном сохранении рядом создаётся резервная копия:

`%USERPROFILE%\\Documents\\ASUTP_KnowledgeBase.json.bak`

В репозитории файл `ASUTP_KnowledgeBase.json` используется как пример структуры данных.

## Ограничения среды

Полноценную сборку и запуск нужно выполнять на Windows. На Linux/macOS можно анализировать код и запускать тесты для `AsutpKnowledgeBase.Core`, но WinForms-контур здесь не является целевой средой исполнения.
