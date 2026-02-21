## 1. Bridge Budget Contract v2 (Deliverable 5.3)

- [x] 1.1 Add bounded host-configurable aggregate metadata budget options in `WebViewHostCapabilityBridge` and enforce deterministic configuration validation (acceptance: invalid min/max out-of-range configuration is rejected deterministically).
- [x] 1.2 Update inbound metadata boundary validation to use effective configured budget while preserving deny-before-policy/dispatch semantics (acceptance: equivalent payloads yield deterministic allow/deny under same effective budget).

## 2. Profile Revision Diagnostics Normalization (Deliverable 5.3)

- [x] 2.1 Implement canonical normalization for profile revision diagnostics (`profileVersion` trim-or-null, `profileHash` canonical `sha256:<64-lower-hex>` or null) in shell diagnostic emission path (acceptance: invalid hash never changes policy outcome).
- [x] 2.2 Add/update unit and integration assertions for valid and invalid revision metadata branches (acceptance: tests verify canonical propagation and null-normalization branches deterministically).

## 3. Template and Governance Marker Alignment (Deliverable 5.4)

- [x] 3.1 Update app-shell preset markers to reflect contract v2 guidance while preserving ShowAbout default deny behavior (acceptance: opt-in snippet remains explicit and disabled by default).
- [x] 3.2 Update governance tests to assert contract v2 markers and bridge/shell source contract tokens (acceptance: governance lane passes without raw bypass regressions).

## 4. Evidence and Closeout (Deliverable 5.5)

- [x] 4.1 Update CT matrix/evidence entries for configurable budget and profile revision normalization branches (acceptance: evidence maps to concrete unit/integration test methods).
- [x] 4.2 Run focused unit/integration verification, strict OpenSpec validation, and mark tasks complete (acceptance: target commands pass and change reaches `isComplete: true`).
