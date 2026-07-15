# Plans

Last updated: `2026-07-15`

## Active Plan

1. Work in `C:\Users\Olga\AKB5` on branch `act`, tracking `origin/act`.
2. Active implementation direction: module `Акты`.
3. Current accepted remote baseline: `9537f95 Adjust fault criterion handling by act type`.
4. The main acts MVP implementation is complete through model, storage, draft creation, form, numbering/path, DOCX generation, journal, filters, and the two accepted DOCX forms.
5. Current local review change: `Templates\Acts\inspection_act.docx` was manually edited by the user after the last commit.
6. Current review publish: `C:\Users\Olga\AKB5\artifacts\publish\win-x64\asutpKB.exe`.
7. The published `inspection_act.docx` copy matches `Templates\Acts\inspection_act.docx` by SHA256 after the latest publish.
8. `AGENTS.md` has unrelated local changes and must not be staged, committed, reverted, or pushed without a fresh direct command.

## Current Open Work

1. Manual review of the updated `inspection_act.docx` behavior through the latest published exe.
2. If the template change is accepted, commit/push only `Templates\Acts\inspection_act.docx`.
3. No code, model, storage, generator, or test changes are currently required for this manual template tweak unless a new defect is found.

## Completed Acts Packages

1. Acts roadmap and implementation control guide were added.
2. Acts model, storage, session lifecycle, and draft workflow were implemented.
3. Act creation from `Lvl3 -> Состав` was implemented.
4. Act form, numbering, document path, DOCX generation, journal, and filters were implemented.
5. `equipment_failure_act.docx` was accepted and should not be changed without a separate request.
6. `inspection_act.docx` was accepted through DOCX gates and later manually adjusted by the user.
7. `FaultCriterion` handling was corrected: the UI label is `Критерий неисправности`, and the field is disabled/cleared for `Осмотр / выполненные работы`.

## Deferred Backlog

Это не текущая работа. Это только кандидаты на будущие отдельные этапы после прямого разрешения пользователя.

1. `Stage 11. Статусы и защита документов`: rules for preventing accidental overwrite of generated/signed/cancelled acts.
2. `Stage 12. Настройки и справочники`: editable directories/settings for executors, signers, positions, typical texts, document folder, templates, and numbering.
3. `Stage 13. Импорт из ActsManager`: one-time or controlled import of historical acts from `C:\Users\Olga\Downloads\ActsManager V1.4\Data\2026.db`, starting with a dry-run report.
4. `Stage 14. Статистика и отчеты`: analytics and exports after stabilization, including counts, labor hours, top objects/models, failure criteria, executors, `.xlsx`, and summary `.docx` reports.

## Not Active / Out Of Scope

1. Do not continue old `Net` / Global Search work in this `act` task unless explicitly requested.
2. Do not add ActsManager import, statistics, extra settings screens, versioning, or overwrite-protection behavior without a separate approved stage.
3. Do not change `equipment_failure_act.docx` while reviewing the current inspection-act template tweak.

## Validation Baseline

Последняя релевантная проверка:

1. `KnowledgeBaseActEditorServiceTests`: 16/16 passed before commit `9537f95`.
2. Latest publish completed successfully after the manual `inspection_act.docx` edit.
3. Latest published exe smoke-test passed: app started hidden and was stopped.
4. Published `inspection_act.docx` matches the source template by SHA256.

## Next Command Candidates

Использовать только после прямой команды пользователя:

1. Commit accepted inspection template tweak:

```powershell
git add -- Templates\Acts\inspection_act.docx
git commit -m "Update inspection act template"
git push origin act
```

2. Re-publish after another template edit:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish.ps1
```

## Update Rule

Обновлять этот файл, когда меняется принятое состояние `act`, артефакт проверки, ближайший шаг или отложенный backlog.
