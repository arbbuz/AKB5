# Current State

Last updated: `2026-07-15`

## Current Objective

Current work is on `C:\Users\Olga\AKB5`, branch `act`: finish manual review and acceptance of the updated inspection act DOCX template.

## Current Repo State

1. Active branch: `act`.
2. Tracking branch: `origin/act`.
3. Latest accepted and pushed commit: `9537f95 Adjust fault criterion handling by act type`.
4. Local changes currently expected:
   - `M AGENTS.md` - unrelated local rule/doc change, do not stage without a direct command.
   - `M Templates\Acts\inspection_act.docx` - user manual template tweak under review.
5. No local commits are expected after `9537f95` unless the template tweak has been accepted and committed.

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

## Accepted Acts State

1. The acts MVP implementation is complete through model, storage, draft workflow, form, numbering/path, DOCX generation, journal, and filters.
2. `Templates\Acts\equipment_failure_act.docx` is accepted and should not be changed without a separate request.
3. `Templates\Acts\inspection_act.docx` was accepted through DOCX gates; the current local edit is a later manual template adjustment.
4. `FaultCriterion` handling is accepted in code: label `Критерий неисправности`, disabled/cleared for `Осмотр / выполненные работы`.

## Deferred Backlog

Это не текущие задачи. Каждый пункт требует отдельного разрешения:

1. `Stage 11. Статусы и защита документов` - запретить случайную перезапись сформированных/подписанных/отмененных актов.
2. `Stage 12. Настройки и справочники` - вынести исполнителей, подписантов, должности, типовые тексты, шаблоны, папку документов и формат номера в редактируемые настройки.
3. `Stage 13. Импорт из ActsManager` - отдельный перенос исторических актов из `C:\Users\Olga\Downloads\ActsManager V1.4\Data\2026.db`, сначала только dry-run отчет.
4. `Stage 14. Статистика и отчеты` - аналитика по актам и экспорты после стабилизации модуля.

## Recommended Next Step

1. User manually checks the latest exe and generated inspection act.
2. If accepted, commit/push only `Templates\Acts\inspection_act.docx`.
3. If not accepted, continue only the template correction requested by the user.

## Do Not Do Without Fresh Approval

1. Do not stage/commit/push `AGENTS.md`.
2. Do not change `equipment_failure_act.docx`.
3. Do not add ActsManager import, statistics, settings screens, document versioning, or overwrite-protection behavior.
4. Do not run a new publish unless requested or needed after another accepted change.
