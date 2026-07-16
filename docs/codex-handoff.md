# Current State

Last updated: `2026-07-16`

## Current Objective

Текущая работа ведется в `C:\Users\Olga\AKB5`, ветка `act`: Stage 12H.5 выполнен локально; Stage 12H.4 вручную принят по пунктам 1-5, а проверка жизненного цикла истории при переименовании/удалении цеха отложена до появления второго цеха.

## Current Repo State

1. Active branch: `act`.
2. Tracking branch: `origin/act`.
3. Latest accepted and pushed commit: `5df8696 Add act input history`.
4. Local changes currently expected:
   - `M AGENTS.md` - unrelated local rule/doc change, do not stage without a direct command.
   - Local Stage 12H.4-12H.5 code/tests and acts documentation - do not commit without a direct command.
5. `AGENTS.md` remains unrelated and must not be staged, reverted, or included with Stage 12H.

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
6. Release build and the complete test suite passed: 505/505 tests.

## Accepted Acts State

1. The acts MVP implementation is complete through model, storage, draft workflow, form, numbering/path, DOCX generation, journal, and filters.
2. `Templates\Acts\equipment_failure_act.docx` is accepted and should not be changed without a separate request.
3. `Templates\Acts\inspection_act.docx` was accepted through DOCX gates; the current local edit is a later manual template adjustment.
4. `FaultCriterion` handling is accepted in code: label `Критерий неисправности`, disabled/cleared for `Осмотр / выполненные работы`.
5. Stage 11 is accepted: statuses and transition history are stored; generated acts can be regenerated only with overwrite confirmation; signed acts are protected; cancelling an act removes its DOCX and document record; cancellation reason is not collected or stored; journal actions are available through buttons and the row context menu.
6. Stage 12H.1-12H.3 are accepted and pushed: six act fields keep editable workshop-scoped input history without hardcoded people/positions or importing old acts.
7. Stage 12H.4 is implemented: deletion by `×` persists immediately, survives form cancellation/restart, does not alter old acts, and deleted values return only after later manual input plus successful act save.
8. Manual checks 1-5 for Stage 12H.4 passed. Workshop rename/delete history behavior remains unverified manually until a second workshop is available; automated coverage is present.
9. Stage 12H.5 adds hardening tests and synchronizes the acts documentation; it introduces no new UI behavior.

## Deferred Backlog

Это не текущие задачи. Каждый пункт требует отдельного разрешения:

1. The remaining broad Stage 12 settings/reference-data ideas are not active. The accepted Stage 12H solution is input history rather than a separate people/position directory.
2. `Stage 14. Статистика и отчеты` - аналитика по актам и экспорты после стабилизации модуля.

## Recommended Next Step

1. After explicit acceptance, commit/push the Stage 12H.4-12H.5 package and updated acts documentation; exclude `AGENTS.md`.
2. When a second workshop exists, complete the deferred manual rename/delete history check.
3. Select the next acts stage only by direct user instruction.

## Do Not Do Without Fresh Approval

1. Do not stage/commit/push `AGENTS.md`.
2. Do not change `equipment_failure_act.docx`.
3. Do not add statistics, settings screens, document versioning, or overwrite-protection behavior. Import from ActsManager is excluded from the roadmap.
4. Do not run a new publish unless requested or needed after another accepted change.
