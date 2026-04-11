# AKB5

## Что это

`AKB5` — WinForms-приложение на C# / .NET 8 для ведения древовидной базы знаний по АСУТП.

Текущая модель хранения:

- JSON-файл с `SchemaVersion`, `Config`, `Workshops`, `LastWorkshop`
- несколько цехов в одной базе
- настраиваемая глубина дерева и названия уровней

## Текущее состояние проекта

Проект уже доведён до рабочего инженерного минимума:

- приложение собирается как `net8.0-windows` в [asutpKB.csproj](/Users/home/ASUTP/AKB5/asutpKB.csproj)
- большая часть не-UI логики вынесена из формы в отдельные сервисы
- есть отдельный тестовый проект для core-логики
- есть Windows CI workflow для `build` и `test`
- `Program.cs` остаётся чистым bootstrap-файлом

Главная архитектурная проблема по-прежнему локализована в `Forms/MainForm.cs`, но теперь форма заметно ближе к роли thin shell. После последних рефакторингов из неё вынесены file/session UI workflow, workshop/config UI workflow, node-level tree-mutation UI workflow, drag-and-drop/undo-redo orchestration для дерева, а также привязка `TreeView` и поиск. Сейчас `MainForm` в основном содержит сборку формы, wiring событий, создание UI-контекстов, screen-level glue между сервисами и обновление состояния экрана.

Важно: `src/AsutpKnowledgeBase.Core` пока не содержит физически перенесённые исходники. Он использует `Models/*.cs` и `Services/*.cs` как linked files. Это значит, что граница между UI и core уже выстроена концептуально и тестируется, но физическое разделение кода ещё не завершено. Папка `UiServices/` в `Core` не линкуется и остаётся WinForms-слоем.

## Принятые ориентиры

На текущем этапе для проекта зафиксированы такие решения:

- архитектурный курс: прагматичный рефакторинг без переписывания WinForms UI
- целевое состояние: минимально чистая архитектура, в которой `MainForm` постепенно становится thin shell над UI-facing coordinator/service слоями
- ближайший приоритет: сначала стабилизировать `build` и `test`, затем продолжать дробление `MainForm`
- хранилище: локальный JSON остаётся основным и единственным source of truth
- обмен данными: импорт/экспорт в Excel реализуется отдельно от JSON persistence layer, без смешивания с `JsonStorageService`
- текущая реализация Excel exchange экспортирует dependency-free `*.xlsx` без внешних NuGet-пакетов и сохраняет совместимый листовой контракт `Meta/Levels/Workshops/Nodes`

## Краткое обновление

- `MainForm` сокращён примерно до `560` строк и теперь ближе к thin-shell роли
- вынесены отдельные WinForms-coordinator'ы для workshop/config UI и tree-mutation UI
- Excel exchange больше не живёт в одном крупном классе: фасад сохранён в `KnowledgeBaseExcelExchangeService`, а writer, reader и workbook parser разнесены по отдельным сервисам
- следующая задача: подтвердить `dotnet build/test` и провести Windows/Excel smoke-проверку `export/import`, затем при необходимости корректировать xlsx contract без ломки `WorkbookFormatVersion = 1`

## Структура

- [Program.cs](/Users/home/ASUTP/AKB5/Program.cs) — точка входа
- [Forms/MainForm.cs](/Users/home/ASUTP/AKB5/Forms/MainForm.cs) — главная форма и оставшийся UI orchestration
- [Forms/InputDialog.cs](/Users/home/ASUTP/AKB5/Forms/InputDialog.cs) — строковый диалог ввода
- [Forms/SetupForm.cs](/Users/home/ASUTP/AKB5/Forms/SetupForm.cs) — настройка уровней
- [Models/KbConfig.cs](/Users/home/ASUTP/AKB5/Models/KbConfig.cs) — конфигурация уровней
- [Models/KbNode.cs](/Users/home/ASUTP/AKB5/Models/KbNode.cs) — узел дерева
- [Models/SavedData.cs](/Users/home/ASUTP/AKB5/Models/SavedData.cs) — корневая модель сохранения
- [Services/KnowledgeBaseDataService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseDataService.cs) — нормализация конфигурации, цехов и snapshot serialization
- [Services/KnowledgeBaseService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseService.cs) — базовые операции над деревом знаний
- [Services/KnowledgeBaseTreeController.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseTreeController.cs) — прикладные tree-операции без UI
- [Services/KnowledgeBaseTreeMutationWorkflowService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseTreeMutationWorkflowService.cs) — mutating tree-workflow и undo/redo coordination без WinForms
- [Services/KnowledgeBaseSessionService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseSessionService.cs) — session-state приложения
- [Services/KnowledgeBaseSessionWorkflowService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseSessionWorkflowService.cs) — переходы session-state
- [Services/KnowledgeBaseFileWorkflowService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseFileWorkflowService.cs) — файловый workflow `load/save`
- [Services/KnowledgeBaseConfigurationWorkflowService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseConfigurationWorkflowService.cs) — изменение конфигурации уровней
- [Services/KnowledgeBaseFormStateService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseFormStateService.cs) — правила состояния формы
- [Services/JsonStorageService.cs](/Users/home/ASUTP/AKB5/Services/JsonStorageService.cs) — чтение, запись, backup и валидация JSON
- [Services/KnowledgeBaseExcelExchangeService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseExcelExchangeService.cs) — thin facade для import/export базы в Excel workbook формата `xlsx` с fallback-импортом legacy XML
- [Services/KnowledgeBaseXlsxWriter.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseXlsxWriter.cs) — генерация `xlsx` workbook и числовых/boolean cell types для контракта `Meta/Levels/Workshops/Nodes`
- [Services/KnowledgeBaseXlsxReader.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseXlsxReader.cs) — чтение `xlsx` workbook, sheet relationships, inline/shared strings и numeric/boolean cells
- [Services/KnowledgeBaseSpreadsheetMlWriter.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseSpreadsheetMlWriter.cs) — генерация SpreadsheetML workbook и фиксированного контракта листов `Meta/Levels/Workshops/Nodes`
- [Services/KnowledgeBaseSpreadsheetMlReader.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseSpreadsheetMlReader.cs) — XML-level чтение legacy SpreadsheetML workbook для обратной совместимости импорта
- [Services/KnowledgeBaseExcelWorkbookParser.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseExcelWorkbookParser.cs) — валидация Excel workbook contract и сборка `SavedData`
- [Services/UndoRedoService.cs](/Users/home/ASUTP/AKB5/Services/UndoRedoService.cs) — история undo/redo
- [UiServices/KnowledgeBaseExcelUiWorkflowService.cs](/Users/home/ASUTP/AKB5/UiServices/KnowledgeBaseExcelUiWorkflowService.cs) — WinForms-специфичные сценарии `Экспорт в Excel...` и `Импорт из Excel...`
- [UiServices/KnowledgeBaseFileUiWorkflowService.cs](/Users/home/ASUTP/AKB5/UiServices/KnowledgeBaseFileUiWorkflowService.cs) — WinForms-специфичный coordinator для file/session dialogs, prompt'ов и close handling
- [UiServices/KnowledgeBaseWorkshopUiWorkflowService.cs](/Users/home/ASUTP/AKB5/UiServices/KnowledgeBaseWorkshopUiWorkflowService.cs) — WinForms-специфичные сценарии переключения/добавления цехов и настройки уровней
- [UiServices/KnowledgeBaseTreeMutationUiWorkflowService.cs](/Users/home/ASUTP/AKB5/UiServices/KnowledgeBaseTreeMutationUiWorkflowService.cs) — WinForms-специфичные tree-mutation сценарии, drag-and-drop feedback и undo/redo orchestration
- [UiServices/KnowledgeBaseTreeViewService.cs](/Users/home/ASUTP/AKB5/UiServices/KnowledgeBaseTreeViewService.cs) — WinForms-специфичная привязка дерева, expanded-state и поиск
- [src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj](/Users/home/ASUTP/AKB5/src/AsutpKnowledgeBase.Core/AsutpKnowledgeBase.Core.csproj) — библиотека для тестируемой не-UI логики
- [tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj](/Users/home/ASUTP/AKB5/tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj) — unit-тесты
- [.github/workflows/windows-build.yml](/Users/home/ASUTP/AKB5/.github/workflows/windows-build.yml) — Windows CI

## Что уже вынесено из MainForm

- session-state, dirty-state и snapshot bookkeeping
- file workflow: `load`, `save`, backup/fallback, default-data paths
- session workflow: смена цеха, добавление цеха, восстановление snapshot
- валидация и нормализация конфигурации уровней
- вычисление состояния формы
- чистые tree-операции над моделью
- mutating tree-workflow для `add/delete/paste/rename/move`
- undo/redo orchestration для tree-сценариев
- file/session UI workflow: `Open`, `Reload`, `Save`, `Save As`, unsaved-changes prompt, close handling и error messaging
- workshop/config UI workflow
- node-level UI orchestration для `add/delete/copy/paste/rename/move/undo/redo`
- Excel import/export contract, `xlsx` workbook generation, `xlsx`/legacy XML reading и workbook parsing
- UI-команды `Экспорт в Excel...` и `Импорт из Excel...`
- WinForms-специфичная привязка `TreeView`, восстановление expanded-state и поиск
- отдельные диалоги `InputDialog` и `SetupForm`

## Что сделано в последнем рефакторинге

- `MainForm` сокращён примерно до `560` строк и очищен от workshop/config UI orchestration и от большей части node-level tree-mutation UI-сценариев
- добавлен `KnowledgeBaseWorkshopUiWorkflowService` для screen-level сценариев по цехам и настройке уровней
- добавлен `KnowledgeBaseTreeMutationUiWorkflowService` для WinForms-диалогов, drag-and-drop feedback и undo/redo orchestration поверх core tree workflow
- `KnowledgeBaseExcelExchangeService` превращён в thin facade
- генерация `xlsx` workbook вынесена в `KnowledgeBaseXlsxWriter`
- чтение `xlsx` workbook вынесено в `KnowledgeBaseXlsxReader`, а legacy XML import оставлен в `KnowledgeBaseSpreadsheetMlReader`
- валидация workbook contract и сборка `SavedData` вынесены в `KnowledgeBaseExcelWorkbookParser`
- fixed Excel contract по-прежнему остаётся `Meta`, `Levels`, `Workshops`, `Nodes`, а `WorkbookFormatVersion = 1` не менялся

## Основные хвосты

- `MainForm` всё ещё совмещает layout/bootstrap формы, wiring UI-событий, создание UI-контекстов и часть screen-level glue между сервисами
- Excel exchange уже декомпозирован на facade/writer/reader/parser, но `KnowledgeBaseExcelWorkbookParser` остаётся сравнительно крупным и требует дальнейшего дробления только при реальной необходимости
- Excel exchange через `*.xlsx` всё ещё требует подтверждения реальным открытием/сохранением в Excel и Windows smoke-проверкой
- `src/AsutpKnowledgeBase.Core` всё ещё использует linked-file схему вместо физически выделенного core-кода
- полноценная UI-проверка остаётся Windows-only задачей
- после последнего рефакторинга требуется переподтверждение `dotnet build` и `dotnet test` в рабочем Windows/.NET окружении

## Проверка

По структуре проекта сейчас:

- в тестовом проекте `tests/AsutpKnowledgeBase.Core.Tests` — `58` unit-тестов
- добавлены unit-тесты на Excel import/export, но здесь они не запускались из-за отсутствия `dotnet`
- `git diff --check` по текущему рефакторингу чистый

Важно: в этой среде `dotnet` недоступен, поэтому локально здесь я не переподтверждал `dotnet build` и `dotnet test` после последнего рефакторинга. Полная проверка сборки и тестов должна выполняться в Windows-среде или в окружении с установленным .NET SDK.

## Актуальный план

1. Подтвердить `dotnet build` и `dotnet test` в Windows или другом окружении с установленным .NET SDK.
2. Исправить только те проблемы, которые всплывут при сборке и тестах после последнего рефакторинга, не расширяя объём изменений.
3. Подтвердить `Excel export/import` в реальном Excel/Windows сценарии: открыть workbook, проверить листы, выполнить import обратно в JSON.
4. При необходимости скорректировать `xlsx` contract под реальные ограничения Excel и сохранить обратную совместимость формата `WorkbookFormatVersion = 1`.
5. Если Excel-подсистема продолжит расти, дробить дальше уже `KnowledgeBaseExcelWorkbookParser`, а не возвращать логику в facade.
6. Добавить Windows smoke-checklist или UI automation для базовых пользовательских сценариев, включая JSON и Excel exchange.
7. Физически перенести `Models` и `Services` в `src/AsutpKnowledgeBase.Core`, когда граница слоёв стабилизируется и сборка станет предсказуемой.

## Что сознательно не делаем сейчас

- не переписываем WinForms UI под MVP/MVVM
- не заменяем JSON другим форматом хранения
- не смешиваем будущий Excel import/export с текущим JSON storage workflow
