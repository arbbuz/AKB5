# Current State

Last updated: `2026-06-23`

## Current objective

Current in-progress task on `Net`: Global Search 2.0 roadmap revision and cleanup after the expanded index exposed an unacceptable behavior class: search results can navigate to a tab where the matched value is not visible to the user.

## Current repo state

- Main worktree: `C:\Users\Olga\AKB5`
- Active branch: `Net`
- Tracking branch: `origin/Net`
- Local changes currently include:
  - pre-existing user-approved `AGENTS.md` rule update, not part of implementation work;
  - `docs\plans.md` roadmap revision;
  - in-progress Global Search 2.0 changes in `Services\KnowledgeBaseTreeSearchService.cs`;
  - related tests in `tests\AsutpKnowledgeBase.Core.Tests\KnowledgeBaseTreeSearchServiceTests.cs`.
- No real `.akb` or JSON user data files were edited.
- Latest local review exe: `C:\Users\Olga\AKB5\bin\Release\net8.0-windows\asutpKB.exe`

## Current package

Latest accepted UI package remains `791f066 Stabilize workspace table redraw` on `origin/Net`.

Current in-progress Global Search 2.0 state:

- Package A added a target contract to `KnowledgeBaseTreeSearchMatch`: result kind, owner node id, entity id, field key, and row key.
- The first Package B implementation expanded the searchable index too far. It is not an accepted behavior baseline because it indexed hidden IDs, dialog-only fields, style-only network data, and maintenance row details that are not visible after the result opens.
- Package B cleanup is now implemented in the working tree: searchable text is reduced to visible fields, hidden/internal values remain only as target metadata, and hidden `24`-style values are covered by no-match tests.
- Current package order in `docs\plans.md`: `Пакет A. Контракт результата поиска`, `Пакет B. Очистка индекса до видимых полей`, `Пакет C. Понятный список результатов`, `Пакет D. Точная навигация к видимому месту`, `Пакет E. Расширение поиска на дополнительные поля`, `Пакет F. Сброс кэша поиска`, `Пакет G. Нормализация и стабильная сортировка`.
- Equipment catalog remains out of the nearest Global Search 2.0 scope because it already has its own search and a different workflow.

## Validation status

Validation completed in `C:\Users\Olga\AKB5` after Package B cleanup:

- search-focused tests: `26/26` passed;
- `dotnet format --verify-no-changes` passed for app, core, and tests projects;
- `dotnet build asutpKB.csproj --configuration Release --no-restore -clp:ErrorsOnly -v:minimal`: passed, 0 errors, 47 warnings;
- full core tests: `427/427` passed;
- `git diff --check -- Services/KnowledgeBaseTreeSearchService.cs Services/KnowledgeBaseMaintenanceScheduleStateService.cs tests/AsutpKnowledgeBase.Core.Tests/KnowledgeBaseTreeSearchServiceTests.cs`: passed; only CRLF normalization warnings.

## Decisions already made

- Keep Lvl3 composition columns as direct fixed-width columns for now; do not restore `Fill` behavior for those four user columns without a new decision.
- Do not add a visible empty/filler column to consume leftover width.
- Searchable text must be visible or explainable to the user. Internal IDs, storage keys, hidden row identifiers, and implementation-only values can be target metadata but must not be searchable text.
- Fields visible only in edit dialogs must not be searchable until the search result list shows `field: value` clearly or exact navigation opens/reveals that field.
- Do not add broad normalization/fuzzy matching for short numeric queries until the visible-index cleanup, result explanation, navigation, and cache behavior are stable.

## Known risks / open questions

- Manual GUI check is still needed: searching a short numeric value such as `24` must not open a destination where `24` is invisible.
- The next expansion must not reintroduce dialog-only fields until `Пакет C` result details or `Пакет D` exact navigation makes those fields explainable.

## Recommended next step

Manual-review Package B behavior in the app. If accepted, start `Пакет C. Понятный список результатов`; do not start additional field expansion before that.

## Commands to run before finishing future implementation work

```powershell
git status --short --branch
dotnet format asutpKB.csproj --verify-no-changes --severity error --no-restore
dotnet build asutpKB.csproj --configuration Release --no-restore
dotnet test tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-restore
```
