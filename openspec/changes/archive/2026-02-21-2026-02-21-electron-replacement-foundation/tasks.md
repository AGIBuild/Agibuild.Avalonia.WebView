## P0 (Must Ship): Web-First Pain-point Closure Baseline

### 1. Target Lock and KPI Definition
- [x] 1.1 Freeze the top-5 pain points and assign measurable KPIs: typed IPC safety, capability governance, deterministic diagnostics, automation pass-rate, template DX time-to-first-feature.
- [x] 1.2 Create release evidence checklist entries for each KPI with explicit pass/fail criteria.

### 2. Typed Capability Gateway Core
- [x] 2.1 Consolidate desktop capability entry points into one typed capability gateway (no scattered host API calls from app layer).
- [x] 2.2 Define unified capability outcome model (`allow` / `deny` / `failure`) with stable metadata schema.
- [x] 2.3 Add contract tests for gateway routing, capability matrix coverage, and failure isolation.

### 3. Policy-first Runtime Enforcement
- [x] 3.1 Define and implement strict policy evaluation order before provider execution.
- [x] 3.2 Enforce zero-execution guarantee when policy returns deny.
- [x] 3.3 Add governance tests that fail on policy bypass, contract drift, or unauthorized direct capability calls.

## P1 (Should Ship): AI Agent Friendly Developer Flow

### 4. Contract and Codegen Operability
- [x] 4.1 Ensure bridge and capability contracts remain strongly typed and codegen-friendly for TypeScript + C#.
- [x] 4.2 Add architecture guardrails to prevent duplicated capability logic across services/components.
- [x] 4.3 Add deterministic error taxonomy and machine-checkable diagnostic payloads for critical runtime flows.

### 5. Web-first Template Path
- [x] 5.1 Update template path to demonstrate the recommended flow: web call -> typed bridge -> capability gateway -> policy -> typed result.
- [x] 5.2 Add end-to-end automation scenario for the above flow with deterministic assertions.
- [x] 5.3 Validate minimal host-side glue requirement in template acceptance tests.

## P2 (Could Ship): Production Governance Hardening

### 6. Governance and Rollout
- [x] 6.1 Sync `openspec/ROADMAP.md` and release-readiness artifacts with web-first objective framing.
- [x] 6.2 Add CI governance checks for diagnostic schema stability and capability contract compatibility.
- [x] 6.3 Produce archive-ready verification evidence mapped to each P0/P1 KPI.

## Exit Criteria

- [x] E1 P0 tasks complete with green contract/integration/automation evidence.
- [x] E2 No policy-bypass path exists for capability execution.
- [x] E3 Template demonstrates web-first desktop delivery with typed contracts and deterministic runtime behavior.
