# Memory

Last updated: `2026-04-28`

## Fits this environment

### Core principles

1. **Action over passive knowledge.**
   If information can improve the result, it should be used in the work, not merely stored.

2. **Separate verified facts from assumptions.**
   Facts from code, files, tests, and direct checks must be distinguished from inference and guesswork.

3. **Name the real cause.**
   Call the problem by its actual source: file lock, config error, UX defect, stale documentation.

4. **Fix the cause, not the symptom.**
   If a failure is repeatable, change the source of the problem: code, rule, test, document, or workflow.

5. **Do not keep dead weight.**
   Stale wording, duplicate knowledge, and outdated documentation should be replaced or removed.

### Knowledge and skills

6. **Use relevant skills and local context.**
   If a skill, document, or prior decision helps the task, load and use it before implementation.

7. **Keep an active plan for multi-step work.**
   Complex tasks should be split into substeps and reflected in the current plan and handoff flow.

### Analysis and communication

8. **Specifics over generalities.**
   Conclusions should be checkable and based on cause-effect links, constraints, and observable facts.

9. **Give a compact operational summary after deep analysis.**
   First the reasoning, then the short conclusion.

10. **Re-read relevant rules before changing external state.**
    For sensitive or batch changes, re-check the active instructions instead of relying on session memory.

11. **Do not mark UI work done until it is visible in the built app.**
    A source diff is not enough for WinForms changes; confirm the control still fits and remains reachable in the actual `exe`.

## Does not fit without adaptation

1. **No file edits without explicit permission.**
   In this environment, local edits are usually part of the direct user request and do not require separate approval once the change request is given.

2. **Holographic memory protocol with `probe`, `fact_feedback`, `fact_store`, `contradict`.**
   Those memory tools do not exist in this environment, so the protocol cannot be applied literally here.

3. **Plans only in `/home/konstantin/docs/plans/`.**
   That path does not match the current Windows environment or project structure; this project uses its own `docs/` files and the built-in execution plan.

4. **Patch skills immediately by default.**
   Skills and system instructions cannot be changed arbitrarily as part of routine project work without a specific need and valid context.
