# Current State

Last updated: `2026-07-15`

## Current Objective

Текущая работа ведется в `C:\Users\Olga\AKB5`, ветка `act`: этап 11 принят пользователем; следующий этап не выбран.

## Current Repo State

1. Active branch: `act`.
2. Tracking branch: `origin/act`.
3. Latest accepted and pushed commit: `580c9ec Finalize inspection act template and roadmap`.
4. Local changes currently expected:
   - `M AGENTS.md` - unrelated local rule/doc change, do not stage without a direct command.
   - Принятые файлы реализации этапа 11 и `M docs\acts-roadmap.md`, `M docs\codex-handoff.md`, `M docs\plans.md` - не коммитить без прямой команды.
5. The inspection template was accepted, committed, and pushed in `580c9ec`.

## Current Review Artifact

1. Latest review executable:

```text
C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe
```

2. Source template under review:

```text
C:\Users\Olga\AKB5\Templates\Acts\inspection_act.docx
```

3. Published template copy:

```text
C:\Users\Olga\AKB5\artifacts\publish\win-x64\Templates\Acts\inspection_act.docx
```

4. The published inspection template matched the source template by SHA256 after the latest publish.
5. Latest smoke-test passed: `asutpKB.exe` started hidden and was stopped.
6. Release build and the complete test suite passed: 492/492 tests.

## Accepted Acts State

1. The acts MVP implementation is complete through model, storage, draft workflow, form, numbering/path, DOCX generation, journal, and filters.
2. `Templates\Acts\equipment_failure_act.docx` is accepted and should not be changed without a separate request.
3. `Templates\Acts\inspection_act.docx` was accepted through DOCX gates; the current local edit is a later manual template adjustment.
4. `FaultCriterion` handling is accepted in code: label `Критерий неисправности`, disabled/cleared for `Осмотр / выполненные работы`.
5. Stage 11 is accepted: statuses and transition history are stored; generated acts can be regenerated only with overwrite confirmation; signed acts are protected; cancelling an act removes its DOCX and document record; cancellation reason is not collected or stored; journal actions are available through buttons and the row context menu.

## Deferred Backlog

Это не текущие задачи. Каждый пункт требует отдельного разрешения:

1. `Stage 12. Настройки и справочники` - вынести исполнителей, подписантов, должности, типовые тексты, шаблоны, папку документов и формат номера в редактируемые настройки.
2. `Stage 14. Статистика и отчеты` - аналитика по актам и экспорты после стабилизации модуля.

## Recommended Next Step

1. Коммитить и отправлять принятую реализацию этапа 11 и документацию только после прямой команды на commit/push; `AGENTS.md` не включать.
2. После фиксации этапа 11 выбрать следующий отдельный этап только по прямому указанию пользователя.

## Do Not Do Without Fresh Approval

1. Do not stage/commit/push `AGENTS.md`.
2. Do not change `equipment_failure_act.docx`.
3. Do not add statistics, settings screens, document versioning, or overwrite-protection behavior. Import from ActsManager is excluded from the roadmap.
4. Do not run a new publish unless requested or needed after another accepted change.
