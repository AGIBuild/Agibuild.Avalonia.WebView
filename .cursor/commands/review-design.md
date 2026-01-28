---
name: /review-design
id: review-design
category: Architecture
description: Red-team veto review for a design proposal (fatal flaws, blockers, cross-examination)
---

Red-team veto review of the provided design proposal.

**Input**: The text after `/review-design` is the design content (or a link/excerpt) to review.

**Steps**

1. **If input is empty, ask the user to paste the design**

   Ask for:
   - Goals & scope
   - Architecture overview (components + interactions)
   - Data model / storage choices
   - Key flows (happy path + failure handling)
   - NFRs (SLA/SLO, latency, throughput, availability, RPO/RTO)
   - Constraints (stack, infra, timeline, team)

2. **Apply the `architecture-design-review` skill**

   Follow its veto-style report template and focus on:
   - Fatal flaws / blockers (P0/P1/P2)
   - Contradictions / missing invariants / unfalsifiable claims
   - Cross-examination questions that must be answered
   - Security/compliance and business reasonableness risks

3. **Produce the review**

   Output in Chinese, concise but ruthless, prioritizing blockers first. Do not provide improvement plans or alternatives.

**Output**

- A structured review with:
  - Rejection-style conclusion + blocker counts (P0/P1/P2)
  - Blockers with why-fatal + evidence + required proofs (no implementation)
  - Cross-examination question list
  - Missing info list (blocking)
