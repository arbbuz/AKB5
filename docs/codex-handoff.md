# Current objective

- После фикса data-integrity regressions следующий практический шаг: Windows smoke-check реального UX-потока `open/save JSON` и `export/import Excel`, включая проверку новых fail-fast ошибок для невалидной схемы и конфликтующих имён цехов.

# Current repo state

- Основной рабочий репозиторий: `/Users/home/ASUTP/AKB5`.
- JSON schema приложения: `2`; legacy `SchemaVersion = 1` по-прежнему загружается и сохраняется уже в текущем формате.
- JSON/XLSX с `SchemaVersion > SavedData.CurrentSchemaVersion` теперь отклоняются на загрузке/импорте явной ошибкой.
- Имена цехов теперь считаются по единому правилу во всех основных путях: canonical name = `Trim()`, сравнение case-insensitive.
- Конфликты имён цехов вида `"Цех 1"`, `" Цех 1 "` и `"цех 1"` больше не нормализуются молча и не теряют данные: JSON load, Excel import и `ReplaceAllData` теперь fail-fast.
- `KbNode.Details` и workbook `v3` остаются активным контрактом; layout-фикс карточки справа остаётся в силе.

# Decisions already made

- Не принимать future schema в read-only режиме и не пытаться сохранять неизвестные поля: policy только reject-on-load/import.
- Не делать silent merge/rename для конфликтующих имён цехов: policy только explicit error.
- Единый инвариант для имён цехов: `Trim()` + `StringComparer.OrdinalIgnoreCase`.
- JSON остаётся source of truth, Excel остаётся exchange-слоем с явной валидацией.

# Files already relevant to the task

- `Models/SavedData.cs`
- `Services/KnowledgeBaseDataService.cs`
- `Services/JsonStorageService.cs`
- `Services/KnowledgeBaseSessionService.cs`
- `Services/KnowledgeBaseFileWorkflowService.cs`
- `Services/KnowledgeBaseExcelWorkbookParser.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseDataServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/JsonStorageServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseSessionServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseFileWorkflowServiceTests.cs`
- `tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseExcelExchangeServiceTests.cs`

# Validation performed in this session

Фактически выполнено:

- `/Users/home/.dotnet/dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore`
- `/Users/home/.dotnet/dotnet build asutpKB.csproj --configuration Release --no-restore`

Observed results:

- core test suite: success, `105` passed, `0` failed
- root WinForms build: success
- новых build/test regressions после ужесточения schema gating и workshop validation не обнаружено

# Known risks / open questions

- Не было ручного Windows smoke-test, который подтвердил бы UX текста ошибок при открытии/импорте невалидных JSON/XLSX файлов.
- Не было реального Excel open-edit-save smoke-check в desktop Excel после новых ограничений на schema/workshop names.
- Если у пользователей уже есть старые JSON с конфликтами имён цехов, они теперь будут корректно отклоняться, но может понадобиться отдельная миграционная инструкция.

# Recommended next step

- На Windows проверить пользовательский сценарий:
  - открыть legacy JSON `SchemaVersion = 1`
  - убедиться, что future-schema JSON/XLSX отклоняются понятным сообщением
  - убедиться, что JSON с trim/case-конфликтом имён цехов не загружается частично
  - прогнать `export/import workbook v3` и обычный `save/reload JSON`

# Commands to run before finishing future implementation work

```bash
git status --short
/Users/home/.dotnet/dotnet test tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
/Users/home/.dotnet/dotnet build asutpKB.csproj --configuration Release --no-restore
```
