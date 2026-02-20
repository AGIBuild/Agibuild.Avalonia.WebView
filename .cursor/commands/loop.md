---
name: /loop
id: loop-mode
category: Workflow
description: Enter continuous execution loop mode for a specific goal
---

Enter continuous execution mode and keep working until delivery criteria are met.

**Input**: `/loop <goal>` or `/loop` followed by a plain-language goal.

**Steps**

1. **Resolve the goal**
   - If `<goal>` is present, use it directly.
   - If missing, ask one focused question to get a concrete goal.

2. **Announce operating contract**
   - Use direct implementation-first execution.
   - Run verification after each meaningful change.
   - Keep going until acceptance criteria are fully satisfied.

3. **Execute the loop**
   - Analyze quickly.
   - Implement code changes directly.
   - Add or update tests to ensure sufficient coverage of:
     - primary flow
     - edge cases
     - failure paths
     - critical branches
   - Run relevant tests/build checks.
   - Fix failures and rerun until all related tests pass.

4. **Mandatory quality gates**
   - Do not claim completion unless all related tests pass.
   - Do not skip test execution.
   - Do not use "theoretically correct" as a substitute for runtime validation.

5. **Compatibility policy (strict)**
   - Do not preserve legacy behavior, fallback paths, or migration branches.
   - If old and new designs conflict, keep only the new design path.
   - Update tests to reflect new design expectations.

6. **Pause only for hard blockers**
   - Missing essential context
   - Destructive or irreversible risk needing explicit confirmation
   - Hard permission/environment constraints
   - Conflicting requirements

7. **Output format**
   - Objective
   - Changes
   - Verification
   - Status
   - Next

**Guardrails**
- Prefer minimal-interruption execution.
- Retry failures with different approaches before escalating.
- Do not auto-commit unless the user explicitly asks.
- Keep architecture boundaries clean; avoid duplicated responsibility across components.
