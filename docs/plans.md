# Plans

Last updated: `2026-07-15`

## Active Plan

1. Work in `C:\Users\Olga\AKB5` on branch `act`, tracking `origin/act`.
2. Active implementation direction: module `Акты`.
3. Current accepted remote baseline: `580c9ec Finalize inspection act template and roadmap`.
4. The main acts implementation is accepted through Stage 11: model, storage, draft creation, form, numbering/path, DOCX generation, journal, filters, two DOCX forms, statuses, document protection, and status history.
5. `Templates\Acts\inspection_act.docx` was manually edited, manually checked through the published exe, accepted, committed, and pushed in `580c9ec`.
6. Last review publish: `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`.
7. The published `inspection_act.docx` copy matched `Templates\Acts\inspection_act.docx` by SHA256 after the latest publish.
8. `AGENTS.md` has unrelated local changes and must not be staged, committed, reverted, or pushed without a fresh direct command.

## Current Open Work

1. Активный этап не выбран. Stage 12 и Stage 14 остаются отложенными до отдельного прямого указания.

## Completed Acts Packages

1. Acts roadmap and implementation control guide were added.
2. Acts model, storage, session lifecycle, and draft workflow were implemented.
3. Act creation from `Lvl3 -> Состав` was implemented.
4. Act form, numbering, document path, DOCX generation, journal, and filters were implemented.
5. `equipment_failure_act.docx` was accepted and should not be changed without a separate request.
6. `inspection_act.docx` was accepted through DOCX gates and later manually adjusted by the user.
7. `FaultCriterion` handling was corrected: the UI label is `Критерий неисправности`, and the field is disabled/cleared for `Осмотр / выполненные работы`.
8. Stage 11 was accepted: statuses and history, overwrite confirmation, signed-act protection, cancellation with deletion of DOCX and its document record, and journal row context actions.

## Deferred Backlog

Это не текущая работа. Это только кандидаты на будущие отдельные этапы после прямого разрешения пользователя.

1. `Stage 12. Настройки и справочники`: editable directories/settings for executors, signers, positions, typical texts, document folder, templates, and numbering.
2. `Stage 14. Статистика и отчеты`: analytics and exports after stabilization, including counts, labor hours, top objects/models, failure criteria, executors, `.xlsx`, and summary `.docx` reports.

## Not Active / Out Of Scope

1. Do not continue old `Net` / Global Search work in this `act` task unless explicitly requested.
2. Do not add statistics, extra settings screens, versioning, or overwrite-protection behavior without a separate approved stage. Import from ActsManager is excluded from the roadmap.
3. Do not change `equipment_failure_act.docx` while reviewing the current inspection-act template tweak.

## Validation Baseline

Последняя релевантная проверка:

1. Complete test suite: 492/492 passed after the final Stage 11 changes.
2. Release build completed with 0 errors.
3. Latest publish completed successfully.
4. Latest published exe smoke-test passed: app started hidden and was stopped.

## Next Command Candidates

Использовать только после прямой команды пользователя:

1. Commit/push the accepted Stage 11 implementation and updated acts documentation, explicitly excluding `AGENTS.md`.

2. Re-publish after another accepted code or template edit:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish.ps1
```

## Update Rule

Обновлять этот файл, когда меняется принятое состояние `act`, артефакт проверки, ближайший шаг или отложенный backlog.
