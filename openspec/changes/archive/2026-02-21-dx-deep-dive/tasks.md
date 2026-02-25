## 1. Bridge npm Package

- [x] 1.1 Create `packages/bridge` package (`package.json`, `tsconfig.json`, `src/index.ts`) with typed runtime client APIs.
- [x] 1.2 Add unit tests (or deterministic harness checks) for package API shape and ready/invoke behavior.

## 2. TypeScript Declaration Governance

- [x] 2.1 Add TypeScript compile governance for generated `bridge.d.ts`.
- [x] 2.2 Add/update tests asserting declaration validation is wired in build/test pipeline.

## 3. Vue Sample Parity

- [x] 3.1 Add `samples/avalonia-vue` minimal executable sample structure.
- [x] 3.2 Demonstrate one typed bridge roundtrip in Vue sample and ensure TS compile passes.

## 4. Template Debug Determinism

- [x] 4.1 Add governance checks for deterministic debug startup conventions in template artifacts.
- [x] 4.2 Add tests ensuring template bridge typing path remains valid.

## 5. Validation

- [x] 5.1 Run targeted unit/integration checks for new DX governance and sample changes.
- [x] 5.2 Run `npx @fission-ai/openspec validate --all --strict`.
