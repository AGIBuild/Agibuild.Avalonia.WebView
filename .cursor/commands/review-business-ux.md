---
name: /review-business-ux
id: review-business-ux
category: Product
description: Review business goals/PRD/process flows/user journeys/interaction plans for completeness, clarity, logic issues, and alignment. Outputs a detailed Chinese report with prioritized actions.
---

Review the provided business goal or design proposal as a senior Business Analyst and UX expert.

**Input**
- `<input>` can be:
  - A file path (preferred): e.g. `docs/agibuild_webview_design_doc.md`
  - A short description pasted in chat

**Steps**
1. **Collect the target content**
   - If `<input>` looks like a file path, read it.
   - If content is pasted inline, use it directly.

2. **If input is empty, ask the user to paste the content**
   Ask for (as applicable):
   - Business goals and success metrics
   - User roles and primary user jobs-to-be-done
   - End-to-end flows (happy path + exceptions)
   - Business rules (eligibility, permissions, calculations, lifecycle states)
   - UI/page flow and key interaction decisions (copy, confirmations, error handling)

3. **Apply the `business-ux-design-review` skill**
   - Follow its workflow and output template.
   - Output in **Chinese**.

**Output**
- A structured Chinese review including:
  - Flow summary (happy path + key branches)
  - Issues + impact + evidence + actionable recommendations
  - Prioritized action list (P0/P1/P2)
  - Minimal blocking questions only if necessary

