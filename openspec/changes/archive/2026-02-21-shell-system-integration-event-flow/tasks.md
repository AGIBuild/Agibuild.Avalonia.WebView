## 1. Typed Inbound Event Contracts (Phase 5.2 / 5.3)

- [x] 1.1 Add typed system-integration inbound event DTOs and bridge contracts for tray/menu interactions (Acceptance: contracts compile; no stringly-typed event payload path remains).
- [x] 1.2 Route inbound events through policy-first evaluation before web delivery (Acceptance: deny branch blocks delivery and reports deterministic deny reason).
- [x] 1.3 Add structured diagnostics for inbound events with correlation metadata (Acceptance: tests assert stable outcome/correlation fields for allow/deny/failure).

## 2. Dynamic Menu Pruning Pipeline (Phase 5.2 / 5.4)

- [x] 2.1 Add policy/context-driven menu pruning evaluator in shell governance pipeline (Acceptance: equivalent inputs produce identical pruned menu state).
- [x] 2.2 Ensure pruned menu state mutation happens only after policy approval (Acceptance: denied pruning request does not mutate effective menu state).
- [x] 2.3 Add deterministic pruning conflict resolution semantics (Acceptance: repeated updates do not create duplicate or oscillating menu entries).

## 3. System Action Whitelist Hardening (Phase 5.2)

- [x] 3.1 Define explicit typed whitelist for supported system actions (Acceptance: contract declares allowed actions and rejects unknown actions deterministically).
- [x] 3.2 Enforce whitelist before provider execution in runtime flow (Acceptance: non-whitelisted action has provider execution count = 0).
- [x] 3.3 Standardize deny/failure taxonomy for action whitelist and policy failures (Acceptance: tests assert stable deny reason/failure category mapping).

## 4. Template Bidirectional Web-first Flow (Phase 5.4)

- [x] 4.1 Extend app-shell typed bridge service to cover inbound event subscription/handling path (Acceptance: template source contains typed inbound event wiring markers).
- [x] 4.2 Update web demo to show command outbound + inbound event handling in one flow (Acceptance: deterministic UI assertions for both directions).
- [x] 4.3 Keep baseline preset free from bidirectional system-integration wiring (Acceptance: governance test confirms absence of app-shell-only markers in baseline).

## 5. Test Matrix and Automation Hardening (Phase 5.3 / 5.5)

- [x] 5.1 Add CT matrix for inbound/outbound system integration allow/deny/failure branches (Acceptance: matrix includes tray event, menu pruning, system action whitelist rows).
- [x] 5.2 Add integration automation scenario for tray event round-trip and menu pruning determinism (Acceptance: automation lane passes with machine-checkable assertions).
- [x] 5.3 Add governance checks for no-bypass bidirectional flow markers (Acceptance: CI fails when direct platform dispatch bypass markers appear).

## 6. Release Evidence and Handoff (Phase 5.5)

- [x] 6.1 Produce verification evidence mapping KPI -> implementation -> test commands for this change (Acceptance: evidence table includes pass/fail outcomes and correlation to goals).
- [x] 6.2 Update roadmap/change notes with in-scope/out-of-scope boundaries for this increment (Acceptance: boundary note explicitly excludes Electron full API parity).
- [x] 6.3 Prepare archive-ready checklist with unresolved questions and owners (Acceptance: open questions tracked with next-step owner and decision gate).
