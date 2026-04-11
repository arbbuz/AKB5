# AKB5

## Что это

`AKB5` — WinForms-приложение на C# / .NET 8 для ведения древовидной базы знаний по АСУТП.

Текущая модель хранения:

- JSON-файл с `SchemaVersion`, `Config`, `Workshops`, `LastWorkshop`
- несколько цехов в одной базе
- настраиваемая глубина дерева и названия уровней

## Текущее состояние проекта

Сейчас проект уже приведён к рабочему инженерному минимуму:

- приложение собирается как `net8.0-windows` в [asutpKB.csproj](/Users/home/ASUTP/AKB5/asutpKB.csproj)
- не-UI логика вынесена в сервисы и покрыта отдельным тестовым проектом
- для сборки и тестов есть Windows CI workflow
- `Program.cs` содержит только bootstrap, основная проблема теперь не в `Main`, а в крупном `MainForm`
- `Forms/MainForm.cs` остаётся большим orchestration-классом: диалоги, привязка `TreeView`, поиск, drag-and-drop, undo/redo coordination

Важно: `src/AsutpKnowledgeBase.Core` пока не содержит физически перенесённые исходники. Он использует `Models/*.cs` и `Services/*.cs` как linked files. Логика уже отделена от UI концептуально и тестируется отдельно, но физическое разделение кода ещё не завершено.

## Структура

- [Program.cs](/Users/home/ASUTP/AKB5/Program.cs) — точка входа
- [Forms/MainForm.cs](/Users/home/ASUTP/AKB5/Forms/MainForm.cs) — главная форма и UI orchestration
- [Forms/InputDialog.cs](/Users/home/ASUTP/AKB5/Forms/InputDialog.cs) — строковый диалог ввода
- [Forms/SetupForm.cs](/Users/home/ASUTP/AKB5/Forms/SetupForm.cs) — настройка уровней
- [Models/KbConfig.cs](/Users/home/ASUTP/AKB5/Models/KbConfig.cs) — конфигурация уровней
- [Models/KbNode.cs](/Users/home/ASUTP/AKB5/Models/KbNode.cs) — узел дерева
- [Models/SavedData.cs](/Users/home/ASUTP/AKB5/Models/SavedData.cs) — корневая модель сохранения
- [Services/KnowledgeBaseService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseService.cs) — операции над деревом знаний
- [Services/KnowledgeBaseTreeController.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseTreeController.cs) — прикладные tree-операции без UI
- [Services/KnowledgeBaseSessionService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseSessionService.cs) — session-state приложения
- [Services/KnowledgeBaseSessionWorkflowService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseSessionWorkflowService.cs) — переходы session-state
- [Services/KnowledgeBaseFileWorkflowService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseFileWorkflowService.cs) — файловый workflow `load/save`
- [Services/KnowledgeBaseConfigurationWorkflowService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseConfigurationWorkflowService.cs) — изменение конфигурации уровней
- [Services/KnowledgeBaseFormStateService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseFormStateService.cs) — правила состояния формы
- [Services/JsonStorageService.cs](/Users/home/ASUTP/AKB5/Services/JsonStorageService.cs) — чтение, запись, backup и валидация JSON
- [Services/UndoRedoService.cs](/Users/home/ASUTP/AKB5/Services/UndoRedoService.cs) — история undo/redo
- [src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj](/Users/home/ASUTP/AKB5/src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj) — библиотека для тестируемой не-UI логики
- [tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj](/Users/home/ASUTP/AKB5/tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj) — unit-тесты
- [.github/workflows/windows-build.yml](/Users/home/ASUTP/AKB5/.github/workflows/windows-build.yml) — Windows CI

## Что уже сделано

- вынесена большая часть бизнес-логики из `MainForm` в сервисы
- добавлен session-state слой для текущего цеха, dirty-state и last-saved snapshot
- вынесены отдельные workflow для:
  - загрузки и сохранения базы
  - переключения цеха и восстановления snapshot
  - изменения конфигурации уровней
  - вычисления состояния формы
- добавлены backup-сценарии и структурная валидация JSON
- исправлены операции `paste` и `drag-and-drop` для поддеревьев и глубины
- добавлены `Open`, `Reload`, `Save`, `Save As`
- `InputDialog` и `SetupForm` вынесены из `MainForm`
- добавлен тестовый проект под `net8.0`
- добавлен Windows CI для `build` и `test`

## Что сделано в текущем review

- подтверждено, что `Program.Main` уже чистый и не дублирует вынесенную логику
- убраны мёртвые хвосты рефакторинга из `MainForm`
- исправлен сценарий, при котором база, загруженная из backup или созданная после ошибки чтения, могла потеряться без prompt при `Open`, `Reload` или `Close`
- `DeleteNode()` теперь не удаляет узел из UI, если модель не подтвердила удаление
- undo-история больше не засоряется no-op снапшотами при неуспешных операциях
- обновлены тесты `KnowledgeBaseFormStateService`

## Проверка

В этой рабочей копии подтверждено:

- сборка приложения проходит: `dotnet build asutpKB.csproj`
- тесты проходят: `dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj`
- зелёных тестов: `46`

Проверка UI остаётся Windows-only задачей. На macOS можно собирать проект, но полноценный smoke WinForms-интерфейса нужно делать в Windows-среде.

## Краткий план улучшений

1. Дробить `MainForm` дальше: вынести orchestration для дерева, поиска и file-dialog сценариев в отдельные UI-facing компоненты.
2. Добавить тесты на сценарии `RequiresSave`, чтобы backup/default-after-error путь был защищён не только косвенно через форму.
3. Физически перенести общие исходники в `src/AsutpKnowledgeBase.Core`, чтобы уйти от linked-file схемы.
4. Добавить Windows smoke-checklist или UI automation для базовых пользовательских сценариев.
5. Рассматривать импорт/экспорт отдельно от основного хранилища, не смешивая его с текущим JSON persistence layer.
