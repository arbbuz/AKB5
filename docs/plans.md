# Plans

Last updated: `2026-05-21`

## Active plan

- Treat branch `Net` in `C:\Users\Olga\AKB5` as the active branch for accepted AKB5 Network topology-overview work.
- Current accepted baseline is `5348f4f Add network topology overview` on `HEAD` and `origin/Net`.
- The latest Net topology-overview package was accepted manually, committed, and pushed.
- User approved bringing the safe UI/design package from `C:\Users\Olga\AKB5-design` into `Net` together with `resources\fonts\MaterialSymbolsOutlined.ttf`.
- Current `Net` working tree contains the uncommitted design-port package: Material toolbar/menu icons, light-surface layout polish, thin splitter/tree visual polish, Network panel polish, warning-row highlighting, and state warning-count support.
- Manual review passed for the design-port package: main functionality works; small UI blemishes are accepted as deferred follow-up work.
- The old design-branch review-filter checkbox/buttons were intentionally not ported because the accepted `5348f4f` overview removed the top passport filter/copy panel.
- Do not keep working in `C:\Users\Olga\AKB5-design` unless the user asks for another comparison.
- Do not commit, push, merge, rebase, delete stash entries, or remove worktrees unless the user explicitly asks in the current chat.
- Keep the first `Сеть` screen topology-overview first. The accepted overview uses already created AKB5 tree objects plus existing network records and shows only `Объекты / устройства` and `Топология`.
- Do not reintroduce the old top filter/copy panel, overview `Файлы и снимки`, or overview `Проверка` block unless the user explicitly asks.
- Do not run interactive UI-smoke unless the user explicitly asks. Prefer non-invasive/offscreen layout-smoke for Network UI changes.
- Keep all new user-facing UI strings Russian-only.
- Follow `docs/codex-operational-rules.md` for every future Codex turn to control silent stalls and context growth.

## Near-term follow-up

The next Net task is either committing/pushing the accepted uncommitted design-port package after explicit approval, or addressing deferred small UI blemishes as a follow-up package.

- ask explicitly before committing/pushing;
- keep small UI blemish fixes scoped and separate from new Network functionality;
- if changes are requested, keep them in `Net` and do not read/merge `AKB5-design` again unless the user asks.

## Not active / out of scope

- OCR or PDF auto-import for network schemes.
- PRONETA/CSV import.
- Live network scan.
- Plan/fact comparison.
- Separate data-quality issue/problem panel or overview `Проверка` block.
- Assigning IP addresses or PROFINET names from AKB5.
- Embedded PDF preview/rendering dependency.
- Phase 8 through Phase 10 remain discussed candidate directions, not active implementation phases.

## Update rule

- Keep only active and near-term plans here.
- Remove completed items instead of growing a history log.
