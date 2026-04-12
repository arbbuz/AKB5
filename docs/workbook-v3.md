# Excel Workbook v3

## Назначение

Workbook `v3` — это редактируемый `*.xlsx` exchange-формат между JSON storage и пользователем. JSON остаётся source of truth приложения, а workbook нужен для безопасного ручного редактирования в Excel и обратного импорта.

Legacy workbook `v1/v2` больше не поддерживаются.

## Структура workbook

Workbook всегда содержит:

- лист `Meta`
- лист `Levels`
- лист `Workshops`
- один лист узлов на каждый цех

Имена листов узлов пользовательские и читаемые, обычно в виде `Узлы - <Цех>`, но import связывает их не по tab name, а по metadata внутри листа.

## Лист `Meta`

Лист содержит таблицу `Property` / `Value`.

Поля:

- `FormatId` — техническое, должно быть `AKB5.ExcelExchange`
- `FormatVersion` — техническое, должно быть `3`
- `SchemaVersion` — техническое
- `LastWorkshopId` — техническое, основной идентификатор последнего выбранного цеха
- `LastWorkshop` — производное/display-only текстовое имя выбранного цеха

Что можно менять вручную:

- для пользовательского сценария здесь ничего редактировать не нужно

Что нельзя:

- менять `FormatId`
- менять `FormatVersion`
- вручную чинить выбор цеха через `LastWorkshopId`, если можно просто сменить `IsLastSelected` на листе `Workshops`

## Лист `Levels`

Колонки:

- `LevelIndex` — техническая
- `LevelName` — редактируемая

Поддерживаемые ручные изменения:

- rename уровней через `LevelName`

Не поддерживаются:

- правки `LevelIndex`
- пропуски или перестановка последовательности `LevelIndex`

## Лист `Workshops`

Колонки:

- `WorkshopOrder` — техническая
- `WorkshopId` — техническая
- `WorkshopName` — редактируемая
- `IsLastSelected` — редактируемая
- `NodesSheetKey` — техническая

Поддерживаемые ручные изменения:

- rename цеха через `WorkshopName`
- смена выбранного цеха через `IsLastSelected`
- перестановка колонок
- добавление дополнительных пользовательских колонок

Не поддерживаются:

- правки `WorkshopOrder`
- правки `WorkshopId`
- правки `NodesSheetKey`
- duplicate `WorkshopName`
- duplicate `WorkshopId`
- duplicate `NodesSheetKey`

## Листы узлов по цехам

Каждый лист узлов начинается с meta-блока `Property` / `Value`, затем идёт таблица узлов.

Meta-поля листа:

- `SheetKind` — техническое, должно быть `WorkshopNodes`
- `WorkshopId` — техническое
- `NodesSheetKey` — техническое

Колонки таблицы узлов:

- `NodeId` — техническая
- `ParentNodeId` — техническая
- `SiblingOrder` — техническая
- `LevelIndex` — техническая
- `LevelName` — производная/display-only
- `NodeName` — редактируемая
- `Path` — производная/display-only

Поддерживаемые ручные изменения:

- rename узла через `NodeName`
- rename tab листа узлов, если внутренний meta-блок не испорчен
- перестановка колонок
- добавление дополнительных пользовательских колонок

Не поддерживаются:

- правки `NodeId`
- правки `ParentNodeId`
- правки `SiblingOrder`
- правки `LevelIndex`
- ручная синхронизация `LevelName` и `Path` вместо изменения исходных данных

## Что import считает поддерживаемым

Import `v3` ориентирован на реальные Excel-правки конечного пользователя и нормально переносит:

- rename `LevelName`
- rename `WorkshopName`
- rename `NodeName`
- stale `Meta.LastWorkshop`, если `LastWorkshopId` всё ещё валиден
- rename tab листа узлов
- перестановку колонок
- дополнительные неиспользуемые пользовательские колонки
- дополнительные не-AKB5 worksheets, не похожие на листы узлов AKB5

## Что import считает ошибкой

Import должен падать с явной ошибкой, а не делать silent guessing, если обнаружено:

- `FormatVersion` не равен `3`
- отсутствует обязательный лист
- отсутствует обязательная колонка
- дублирующиеся заголовки
- duplicate `WorkshopName`, `WorkshopId`, `NodesSheetKey` или `NodeId`
- `WorkshopOrder` или `LevelIndex` не образуют ожидаемую последовательность
- лист узлов не связан ни с одной строкой `Workshops`
- лист узлов ссылается на неизвестный `WorkshopId`
- broken tree structure: orphan `ParentNodeId`, cross-workshop parent, cycle, duplicate `SiblingOrder`

## Примеры типовых ошибок пользователя

- Пользователь поменял `FormatVersion` на `2` и пытается импортировать старый workbook.
- Пользователь вручную исправил `WorkshopName`, но одновременно сломал `NodesSheetKey`.
- Пользователь удалил лист узлов для одного из цехов.
- Пользователь скопировал строку в `Workshops` и получил duplicate `WorkshopId`.
- Пользователь переименовал узел, но начал вручную править `Path` и `ParentNodeId`.

Правильный сценарий редактирования: менять только бизнес-значения (`LevelName`, `WorkshopName`, `NodeName`, выбор текущего цеха), а технические колонки не трогать.
