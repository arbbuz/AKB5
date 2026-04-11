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

Главная архитектурная проблема теперь локализована в `Forms/MainForm.cs`: форма стала тоньше, но всё ещё остаётся крупным UI orchestration-классом. После последнего рефакторинга из неё уже вынесены tree-mutation workflow, undo/redo coordination для дерева, привязка `TreeView` и поиск, но в форме по-прежнему остаются file/session dialogs, `MessageBox`-сценарии, wiring событий и часть screen-level orchestration.

Важно: `src/AsutpKnowledgeBase.Core` пока не содержит физически перенесённые исходники. Он использует `Models/*.cs` и `Services/*.cs` как linked files. Это значит, что граница между UI и core уже выстроена концептуально и тестируется, но физическое разделение кода ещё не завершено. Папка `UiServices/` в `Core` не линкуется и остаётся WinForms-слоем.

## Принятые ориентиры

На текущем этапе для проекта зафиксированы такие решения:

- архитектурный курс: прагматичный рефакторинг без переписывания WinForms UI
- целевое состояние: минимально чистая архитектура, в которой `MainForm` постепенно становится thin shell над UI-facing coordinator/service слоями
- ближайший приоритет: сначала стабилизировать `build` и `test`, затем продолжать дробление `MainForm`
- хранилище: локальный JSON остаётся основным и единственным source of truth
- обмен данными: импорт/экспорт в Excel должен проектироваться отдельно от JSON persistence layer, без смешивания с `JsonStorageService`

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
- [Services/UndoRedoService.cs](/Users/home/ASUTP/AKB5/Services/UndoRedoService.cs) — история undo/redo
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
- WinForms-специфичная привязка `TreeView`, восстановление expanded-state и поиск
- отдельные диалоги `InputDialog` и `SetupForm`

## Что сделано в последнем рефакторинге

- `MainForm` сокращён примерно до `968` строк, из неё убрана значимая часть tree/search orchestration
- добавлен `KnowledgeBaseTreeMutationWorkflowService` для tree-mutation сценариев и undo/redo
- добавлен `KnowledgeBaseTreeViewService` для работы с `TreeView`, поиском и expanded-state
- проверка циклического `drag-and-drop` перемещения перенесена из формы в core-слой
- добавлены unit-тесты на новый tree-mutation workflow
- расширены unit-тесты `KnowledgeBaseTreeController` проверкой циклического move

## Основные хвосты

- `MainForm` всё ещё совмещает screen-level orchestration, file dialogs, `MessageBox`-решения и wiring UI-событий
- file/session UI workflow всё ещё находится в форме: `Open`, `Reload`, `Save`, `Save As`, prompt before continue, close handling
- `src/AsutpKnowledgeBase.Core` всё ещё использует linked-file схему вместо физически выделенного core-кода
- полноценная UI-проверка остаётся Windows-only задачей
- после последнего рефакторинга требуется переподтверждение `dotnet build` и `dotnet test` в рабочем Windows/.NET окружении

## Проверка

По структуре проекта сейчас:

- в тестовом проекте `tests/AsutpKnowledgeBase.Core.Tests` — `51` unit-тест
- `git diff --check` по текущему рефакторингу чистый

Важно: в этой среде `dotnet` недоступен, поэтому локально здесь я не переподтверждал `dotnet build` и `dotnet test` после последнего рефакторинга. Полная проверка сборки и тестов должна выполняться в Windows-среде или в окружении с установленным .NET SDK.

## Актуальный план

1. Подтвердить `dotnet build` и `dotnet test` в Windows или другом окружении с установленным .NET SDK.
2. Исправить только те проблемы, которые всплывут при сборке и тестах после последнего рефакторинга, не расширяя объём изменений.
3. После стабилизации вынести из `MainForm` file/session UI workflow в отдельный coordinator: `Open`, `Reload`, `Save`, `Save As`, `Close`, prompt и error messaging.
4. Продолжать держать `MainForm` как thin shell без переписывания UI и без тяжёлого MVP/MVVM-рефакторинга.
5. Физически перенести `Models` и `Services` в `src/AsutpKnowledgeBase.Core`, когда граница слоёв стабилизируется и сборка станет предсказуемой.
6. Добавить Windows smoke-checklist или UI automation для базовых пользовательских сценариев.
7. Спроектировать импорт/экспорт в Excel как отдельный adapter/service слой поверх domain-модели, не смешивая его с основным JSON persistence layer.

## Что сознательно не делаем сейчас

- не переписываем WinForms UI под MVP/MVVM
- не заменяем JSON другим форматом хранения
- не смешиваем будущий Excel import/export с текущим JSON storage workflow
