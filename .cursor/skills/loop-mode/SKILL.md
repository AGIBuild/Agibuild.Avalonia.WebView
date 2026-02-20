---
name: loop-mode
description: Executes tasks in continuous loop mode with strict test coverage and no legacy compatibility. Use when the user says "进入loop 模式, 目标: XXXX" or asks for nonstop implementation until done.
---

# Loop Mode

Run in continuous execution mode for a single concrete goal with minimal interruption.

## Trigger

Apply this skill when the user provides:
- `进入loop 模式, 目标: XXXX`
- or equivalent intent such as "keep going without stopping until done"

If goal text is missing, ask exactly one concise follow-up question to obtain it.

## Core Contract

1. Implementation first:
   - Do not stop at high-level planning unless explicitly requested.
   - Move directly into code changes after a short analysis.

2. Continuous loop:
   - Analyze -> implement -> test -> fix -> retest -> summarize.
   - Continue until acceptance criteria are met or a hard blocker is encountered.

3. Minimal interruption:
   - Do not ask for step-by-step confirmation.
   - Escalate only when truly blocked.

## Test and Quality Requirements (Strict)

For every functional change:
- Add/update tests for main flow, edge cases, failure paths, and key branches.
- Run relevant tests/build checks after changes.
- Do not claim completion unless all related tests pass.
- Never skip execution-based verification.

If tests fail:
- Investigate root cause.
- Fix implementation or tests to match intended new behavior.
- Rerun until passing.

## Compatibility Policy (Strict)

The product has no legacy-user burden. Therefore:
- Do not keep old-design compatibility paths.
- Do not add fallback/adapter/dual-path migration logic.
- If new design conflicts with old behavior, keep only the new design path.
- Rewrite outdated tests to assert the new design, not legacy semantics.

## Retry and Escalation

Before escalating:
- Attempt up to 3 materially different fixes for the same class of failure.
- Record for each attempt:
  - failure reason
  - change made
  - result

Escalate only after repeated failure, with up to 3 concrete options.

## Execution Guardrails

- Maintain clean architecture boundaries and single ownership of each responsibility.
- Avoid defensive fallback clutter; fix timing/order in the main path instead.
- Do not auto-commit unless the user explicitly requests commit.
- Do not perform destructive git operations.

## Completion Output

At completion (or blocker), provide:
- Objective
- Changes
- Verification (commands and outcomes)
- Status
- Next
