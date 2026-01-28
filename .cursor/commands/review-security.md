---
name: /review-security
id: review-security
category: Security
description: Review a design/proposal for security risks (attack surface, data protection, compliance) and output a detailed Chinese report with risk ratings.
---

Perform a senior security review of the provided design proposal or business goals.

**Invoke**: `/review-security <input>`

**Input**
- `<input>` can be:
  - A file path (preferred): e.g. `docs/agibuild_webview_design_doc.md`
  - A short description pasted in chat
  - A change name (OpenSpec): e.g. `init-project-structure` (then infer relevant spec/design files)

**Steps**
1. **Collect the target content**
   - If `<input>` looks like a file path, read it.
   - If `<input>` is a change name:
     - Read `openspec/changes/<name>/design.md` if present.
     - Also read any referenced proposal/specs if they exist in that change folder.
   - If content is pasted inline, use it directly.

2. **Identify security-relevant context**
   - Assets (PII/secrets/credentials), actors, entry points, trust boundaries.
   - Deployment assumptions (desktop/web/mobile, on-prem/cloud), key dependencies.
   - Call out unknowns that materially affect the review.

3. **Analyze**
   - Attack surface & threats (STRIDE as a mental model).
   - Data protection: in-transit and at-rest; secrets management; logging/telemetry; retention.
   - Compliance/standards alignment conditionally (do not invent applicability).

4. **Output**
   - Produce the report in **Chinese** using the exact structure from the `security-design-review` skill:
     - Executive summary
     - Assets/boundaries/attack surface
     - Findings (each with risk rating High/Medium/Low)
     - Data transmission & storage evaluation
     - Compliance mapping (conditional)
     - Mitigation roadmap (P0/P1/P2) and residual risks

**Guardrails**
- Do not claim controls exist unless explicitly stated in the design.
- If key info is missing, list the minimum questions needed and treat as risk/unknown.
- Prefer actionable mitigations tied to concrete components/flows.

