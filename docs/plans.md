# Plans

Last updated: `2026-07-16`

## Active Plan

1. Work in `C:\Users\Olga\AKB5` on branch `act`, tracking `origin/act`.
2. Active implementation direction: module `Акты`.
3. Current accepted remote baseline: `5df8696 Add act input history`.
4. The main acts implementation is accepted through Stage 11 and Stage 12H.1-12H.3. Stage 12H.4 is manually accepted for checks 1-5; the workshop rename/delete check is deferred until a second workshop is available.
5. `Templates\Acts\inspection_act.docx` was manually edited, manually checked through the published exe, accepted, committed, and pushed in `580c9ec`.
6. Last review publish: `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`.
7. The published `inspection_act.docx` copy matched `Templates\Acts\inspection_act.docx` by SHA256 after the latest publish.
8. `AGENTS.md` has unrelated local changes and must not be staged, committed, reverted, or pushed without a fresh direct command.

## Current Open Work

1. Stage 12H.5 hardening and documentation are implemented locally and await package acceptance.
2. The remaining manual check for Stage 12H.4 is workshop rename/delete history behavior; it is deferred until a second workshop exists.

## Completed Acts Packages

1. Acts roadmap and implementation control guide were added.
2. Acts model, storage, session lifecycle, and draft workflow were implemented.
3. Act creation from `Lvl3 -> Состав` was implemented.
4. Act form, numbering, document path, DOCX generation, journal, and filters were implemented.
5. `equipment_failure_act.docx` was accepted and should not be changed without a separate request.
6. `inspection_act.docx` was accepted through DOCX gates and later manually adjusted by the user.
7. `FaultCriterion` handling was corrected: the UI label is `Критерий неисправности`, and the field is disabled/cleared for `Осмотр / выполненные работы`.
8. Stage 11 was accepted: statuses and history, overwrite confirmation, signed-act protection, cancellation with deletion of DOCX and its document record, and journal row context actions.
9. Stage 12H.1-12H.5 implement workshop-scoped input history for six act fields, SQLite/session persistence, editable suggestions, safe deletion, workshop lifecycle handling, and hardening tests without a separate reference-data screen.

## Deferred Backlog

Это не текущая работа. Это только кандидаты на будущие отдельные этапы после прямого разрешения пользователя.

1. The remaining broad Stage 12 settings/reference-data ideas are not active. Stage 12H uses input history instead of a separate people/position directory.
2. `Stage 14. Статистика и отчеты`: analytics and exports after stabilization, including counts, labor hours, top objects/models, failure criteria, executors, `.xlsx`, and summary `.docx` reports.

## Not Active / Out Of Scope

1. Do not continue old `Net` / Global Search work in this `act` task unless explicitly requested.
2. Do not add statistics, extra settings screens, versioning, or overwrite-protection behavior without a separate approved stage. Import from ActsManager is excluded from the roadmap.
3. Do not change `equipment_failure_act.docx` while reviewing the current inspection-act template tweak.

## Validation Baseline

Последняя релевантная проверка:

1. Complete test suite: 505/505 passed after Stage 12H.5 hardening.
2. Release build completed with 0 errors.
3. Latest publish completed successfully.
4. Latest published exe smoke-test passed: app started hidden and was stopped.

## Next Command Candidates

Использовать только после прямой команды пользователя:

1. After explicit acceptance, commit/push the Stage 12H.4-12H.5 implementation and updated acts documentation, explicitly excluding `AGENTS.md`.
2. When a second workshop is available, manually verify that rename preserves its suggestions and delete removes them.

3. Re-publish after another accepted code or template edit:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish.ps1
```

## Update Rule

Обновлять этот файл, когда меняется принятое состояние `act`, артефакт проверки, ближайший шаг или отложенный backlog.
