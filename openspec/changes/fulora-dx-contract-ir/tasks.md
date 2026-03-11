## 1. Contract IR Foundation

- [ ] 1.1 Define canonical Bridge Contract IR types in generator (services, methods, params, DTO graph, events, error metadata). Deliverable: `bridge-contract-ir-pipeline`; Acceptance: generator compiles and IR unit tests validate complete graph extraction.
- [ ] 1.2 Refactor existing emitters to consume IR instead of ad-hoc model traversal. Deliverable: `bridge-contract-ir-pipeline`; Acceptance: current declaration output remains deterministic in snapshot tests.
- [ ] 1.3 Add deterministic ordering and naming invariants in IR build step. Deliverable: `bridge-contract-ir-pipeline`; Acceptance: repeated generation on unchanged input produces byte-stable artifacts.

## 2. TypeScript Artifact Expansion

- [ ] 2.1 Extend TypeScript generation to emit IR-backed declaration output with DTO coverage parity. Deliverable: `bridge-typescript-generation`; Acceptance: generated declaration includes DTOs used by service signatures and passes updated tests.
- [ ] 2.2 Add generated typed client artifact (`bridge.client.ts`) for service proxies with typed params/results. Deliverable: `bridge-typescript-generation`; Acceptance: sample service calls compile without handwritten wrappers.
- [ ] 2.3 Add generated mock artifact (`bridge.mock.ts`) for standalone browser development. Deliverable: `bridge-typescript-generation`; Acceptance: sample web app can boot outside host using generated mock.

## 3. Ready Handshake and Error Semantics

- [ ] 3.1 Implement sticky ready state + ready event in bridge runtime/client API. Deliverable: `bridge-ready-handshake`; Acceptance: readiness resolves for both early and late subscribers without polling dependency.
- [ ] 3.2 Keep polling as compatibility fallback behind handshake-first logic. Deliverable: `bridge-ready-handshake`; Acceptance: legacy callers still function with explicit timeout behavior.
- [ ] 3.3 Normalize structured bridge errors (`code/message/data`) end-to-end in JS middleware integration. Deliverable: `bridge-js-middleware`; Acceptance: `withErrorNormalization()` preserves structured fields and test suite asserts no message-only degradation.
- [ ] 3.4 Add global middleware error hook contract and tests. Deliverable: `bridge-js-middleware`; Acceptance: normalized errors are observable by global handler before rethrow.

## 4. Host Bootstrap and DI/Productization

- [ ] 4.1 Implement framework bootstrap API for dev/prod SPA navigation and deterministic bridge registration order. Deliverable: `bridge-host-bootstrap`; Acceptance: host setup path reduced to bootstrap call with stable behavior in integration tests.
- [ ] 4.2 Add bootstrap-managed lifecycle/disposal ownership for exposed bridge services. Deliverable: `bridge-host-bootstrap`; Acceptance: disposal is idempotent and no manual per-service dispose wiring is required.
- [ ] 4.3 Formalize DI/plugin-first bridge exposure path in DI integration helpers. Deliverable: `webview-di-integration`; Acceptance: DI registrations can expose bridge services deterministically without reflection-first assembly scan.

## 5. Sample and Template Adoption

- [ ] 5.1 Migrate `samples/avalonia-react` to generated typed client and handshake-ready API. Deliverable: `bridge-typescript-generation` + `bridge-ready-handshake`; Acceptance: remove handwritten `services.ts` ceremony and polling hook usage.
- [ ] 5.2 Migrate `samples/avalonia-ai-chat` to generated mock/bootstrap conventions where applicable. Deliverable: `bridge-host-bootstrap` + `bridge-typescript-generation`; Acceptance: sample host/web startup uses framework-owned orchestration path.
- [ ] 5.3 Update template defaults to use generated artifacts and bootstrap conventions. Deliverable: `bridge-host-bootstrap` + `webview-di-integration`; Acceptance: new template project starts with no manual bridge boilerplate.

## 6. Verification and Governance

- [ ] 6.1 Add/refresh CT coverage for IR determinism, typed generation parity, ready handshake race safety, and structured error propagation. Deliverable: all specs; Acceptance: new CTs pass and cover edge cases from specs.
- [ ] 6.2 Add/refresh IT coverage for host bootstrap ordering and sample startup flows. Deliverable: `bridge-host-bootstrap`; Acceptance: integration suite validates registration timing and lifecycle behavior.
- [ ] 6.3 Run `openspec validate --all --strict`, targeted package tests, and full repository test gate (`nuke Test`). Deliverable: governance closeout; Acceptance: all commands pass with no validation errors.
