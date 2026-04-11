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

Главная архитектурная проблема теперь локализована в `Forms/MainForm.cs`: форма стала тоньше, но всё ещё остаётся крупным UI orchestration-классом. После текущего набора рефакторингов из неё уже вынесены tree-mutation workflow, undo/redo coordination для дерева, привязка `TreeView` и поиск, а также file/session UI workflow (`Open`, `Reload`, `Save`, `Save As`, `Close`, prompts и error messaging). В форме по-прежнему остаются wiring событий, часть screen-level orchestration, workshop/config dialogs и node-level UI-сценарии.

Важно: `src/AsutpKnowledgeBase.Core` пока не содержит физически перенесённые исходники. Он использует `Models/*.cs` и `Services/*.cs` как linked files. Это значит, что граница между UI и core уже выстроена концептуально и тестируется, но физическое разделение кода ещё не завершено. Папка `UiServices/` в `Core` не линкуется и остаётся WinForms-слоем.

## Принятые ориентиры

На текущем этапе для проекта зафиксированы такие решения:

- архитектурный курс: прагматичный рефакторинг без переписывания WinForms UI
- целевое состояние: минимально чистая архитектура, в которой `MainForm` постепенно становится thin shell над UI-facing coordinator/service слоями
- ближайший приоритет: сначала стабилизировать `build` и `test`, затем продолжать дробление `MainForm`
- хранилище: локальный JSON остаётся основным и единственным source of truth
- обмен данными: импорт/экспорт в Excel реализуется отдельно от JSON persistence layer, без смешивания с `JsonStorageService`
- первая реализация Excel exchange сделана в dependency-free формате SpreadsheetML 2003 (`*.xml`), который открывается в Excel и не требует внешних NuGet-пакетов

## Краткое обновление

- вынесены file/session UI workflow из `MainForm`, добавлены отдельные Excel import/export сервисы и UI-команды
- добавлены unit-тесты на SpreadsheetML contract, иерархию узлов и замену текущей базы импортированными данными
- следующая задача: подтвердить `dotnet build/test` и провести Windows/Excel smoke-проверку `export/import`, затем при необходимости скорректировать SpreadsheetML contract без ломки `WorkbookFormatVersion = 1`

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
- [Services/KnowledgeBaseExcelExchangeService.cs](/Users/home/ASUTP/AKB5/Services/KnowledgeBaseExcelExchangeService.cs) — импорт/экспорт базы в Excel-compatible SpreadsheetML workbook и фиксированный контракт листов `Meta/Levels/Workshops/Nodes`
- [Services/UndoRedoService.cs](/Users/home/ASUTP/AKB5/Services/UndoRedoService.cs) — история undo/redo
- [UiServices/KnowledgeBaseExcelUiWorkflowService.cs](/Users/home/ASUTP/AKB5/UiServices/KnowledgeBaseExcelUiWorkflowService.cs) — WinForms-специфичные сценарии `Экспорт в Excel...` и `Импорт из Excel...`
- [UiServices/KnowledgeBaseFileUiWorkflowService.cs](/Users/home/ASUTP/AKB5/UiServices/KnowledgeBaseFileUiWorkflowService.cs) — WinForms-специфичный coordinator для file/session dialogs, prompt'ов и close handling
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
- Excel import/export contract и SpreadsheetML workbook generation
- UI-команды `Экспорт в Excel...` и `Импорт из Excel...`
- WinForms-специфичная привязка `TreeView`, восстановление expanded-state и поиск
- отдельные диалоги `InputDialog` и `SetupForm`

## Что сделано в последнем рефакторинге

- `MainForm` сокращён примерно до `768` строк, из неё убрана значимая часть tree/search и file/session orchestration
- добавлен `KnowledgeBaseTreeMutationWorkflowService` для tree-mutation сценариев и undo/redo
- добавлен `KnowledgeBaseTreeViewService` для работы с `TreeView`, поиском и expanded-state
- добавлен `KnowledgeBaseFileUiWorkflowService` для WinForms-специфичных file/session сценариев
- добавлен `KnowledgeBaseExcelExchangeService` с фиксированным Excel contract: `Meta`, `Levels`, `Workshops`, `Nodes`, а также с import/export для SpreadsheetML 2003
- добавлен `KnowledgeBaseExcelUiWorkflowService` и UI-команды `Экспорт в Excel...` / `Импорт из Excel...`
- добавлены unit-тесты на Excel export/import contract, пустые цеха, иерархию узлов и применение импортированных данных
- проверка циклического `drag-and-drop` перемещения перенесена из формы в core-слой
- добавлены unit-тесты на новый tree-mutation workflow
- расширены unit-тесты `KnowledgeBaseTreeController` проверкой циклического move

## Основные хвосты

- `MainForm` всё ещё совмещает screen-level orchestration, workshop/config dialogs, node-level `MessageBox`/`InputDialog`-сценарии и wiring UI-событий
- Excel exchange реализован в первом приближении через SpreadsheetML 2003 (`*.xml`), но требует подтверждения реальным открытием/сохранением в Excel и Windows smoke-проверкой
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
4. При необходимости скорректировать SpreadsheetML contract под реальные ограничения Excel и сохранить обратную совместимость формата `WorkbookFormatVersion = 1`.
5. Продолжать дробление `MainForm`: вынести оставшиеся workshop/config UI workflow и node-level dialog orchestration.
6. Добавить Windows smoke-checklist или UI automation для базовых пользовательских сценариев, включая JSON и Excel exchange.
7. Физически перенести `Models` и `Services` в `src/AsutpKnowledgeBase.Core`, когда граница слоёв стабилизируется и сборка станет предсказуемой.

## Что сознательно не делаем сейчас

- не переписываем WinForms UI под MVP/MVVM
- не заменяем JSON другим форматом хранения
- не смешиваем будущий Excel import/export с текущим JSON storage workflow
