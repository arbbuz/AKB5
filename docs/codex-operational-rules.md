# Codex Operational Rules

Last updated: `2026-05-12`

This document records mandatory operating rules for future Codex work on AKB5.
It exists to prevent repeated silent turn stalls and excessive context growth.

## Turn stall handling

- Do not assume every quiet long-running turn is still doing useful work.
- If the last tool result has already completed and there is no new tool output,
  token-count movement, or visible progress for about 2-3 minutes, treat the
  current Codex turn as probably stuck.
- The recovery action is to interrupt/resume or start a fresh session, then
  inspect the working tree with compact checks.
- After recovery, verify disk state before continuing: changed files, tests or
  verification already completed, whether git was touched, and whether the
  latest user request authorized any irreversible action.
- Do not keep waiting for many minutes just because a task is open when there
  is no evidence of command execution or model progress.

## Context budget

- Treat context as a hard budget, not as free scratch space.
- Use the context watcher during long work:
  `powershell.exe -ExecutionPolicy Bypass -File C:\Users\Olga\.codex\scripts\codex-context-watch.ps1`.
- At 85% context usage, checkpoint and reduce scope.
- At 92% context usage, fork/resume or start a fresh session before continuing.
- After a large investigation, verification checkpoint, or `WAITING_REVIEW`
  handoff, prefer a fresh session for substantial new work.

## Tool output discipline

- Never run broad log scans or full session-tree searches unless the output is
  aggregated first.
- Prefer counts, top-N tables, exact `thread_id` / `turn_id` filters, narrow
  time windows, and `Select-Object -First` / `Select-Object -Last`.
- Do not use `Get-Content -Raw` on large docs, services, logs, or session files
  when a marker search or line range is enough.
- Do not paste full diagnostic logs, full command transcripts, or large diffs
  into chat unless explicitly requested.
- Put large diagnostics in an artifact file and report only the result, failure
  summary, and artifact path when the path is useful to the user.

## Diff and status checks

- Default to `git status --short`, `git diff --stat`, and `git diff --name-only`.
- Use full `git diff` only when explicitly requested or narrowed to a small
  file/range needed for the current decision.
- For documentation-only updates, do not run a full diff after the patch.
  Verify with `git diff --stat` and `git status --short`, then finalize.

## Handoff and distillation workflow

- For `дистиллируй знания из сессии`, update only durable project docs:
  current state in `codex-handoff`, next work in `plans`, repeated operating
  lessons in `lessons-learned`, and decisions in `decision-log`.
- Read only the relevant sections before editing.
- After documentation edits, stop at the verification boundary and wait for
  review or explicit commit/push authorization.
- Do not continue from distillation into unrelated implementation work in the
  same turn.

## Reasoning depth

- Current local Codex configuration can use high reasoning depth. That is
  appropriate for risky code changes, architecture changes, and difficult bug
  analysis.
- For routine documentation, status, handoff, and log-summary work, keep the
  reasoning and tool output deliberately small.
- If a lower-reasoning or fresh-session option is available for routine work,
  prefer it over continuing a large xhigh context.
