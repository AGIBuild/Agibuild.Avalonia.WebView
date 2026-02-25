## Context

Recent archived changes completed Phase 5 hardening areas: contract freeze, template DX panel, and diagnostic export. Roadmap status still indicates in progress, which conflicts with completed milestone evidence.

## Goals / Non-Goals

**Goals:**
- Make Phase 5 status authoritative and explicit.
- Align evidence snapshot with latest full validation baseline.
- Add deterministic evidence-source mapping for governance reviews.

**Non-Goals:**
- No runtime code change.
- No test matrix expansion in this change.
- No archival automation redesign.

## Decisions

### Decision 1: Status switch only after evidence refresh
- Change status to completed together with updated test/coverage values in one atomic documentation edit.

### Decision 2: Add evidence source mapping block
- Include mapping from closeout claims to source artifacts (archived changes and validation commands).

### Decision 3: Keep scope to foundation spec + roadmap
- Update only foundational governance text to avoid broad spec churn.

## Risks / Trade-offs

- [Risk] Snapshot can become stale with future runs.  
  → Mitigation: follow-up change will automate evidence refresh generation.

- [Risk] Readers may treat status as release tag.  
  → Mitigation: keep explicit release line separate from phase status.

## Testing Strategy

- `openspec validate --all --strict` must pass.
- Governance tests continue passing to ensure roadmap/spec references remain consistent.
