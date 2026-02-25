## Why

After Phase 5 closeout and quality hardening, the largest remaining adoption gap is web-first developer ergonomics: npm-consumable bridge runtime, frontend sample parity, and deterministic TypeScript contract validation.  
This change advances PROJECT experience goals E1/E2/E3 and ROADMAP deliverables 2.5 and 3.1-3.2.

## What Changes

- Introduce publishable `@agibuild/bridge` npm package source in-repo with typed bridge client runtime.
- Add Vue sample parity with generated `bridge.d.ts` workflow and bridge roundtrip smoke.
- Add deterministic TypeScript declaration validation tests for generated `bridge.d.ts`.
- Extend template/debug workflow checks so default hybrid template has reproducible web-debug startup behavior.

## Capabilities

### New Capabilities

- `bridge-npm-distribution`: govern npm package source, build, and consumption guarantees for `@agibuild/bridge`.
- `vue-sample-parity`: define acceptance criteria for Vue sample hybrid flow parity with React sample.

### Modified Capabilities

- `bridge-typescript-generation`: add declaration quality and compile validation requirements.
- `project-template`: add deterministic web-debug startup and bridge typing validation requirements.

## Non-goals

- Publishing to public npm registry in this change.
- Replacing existing React sample or template architecture.
- Expanding runtime bridge protocol semantics.

## Impact

- New npm package source under repository package workspace.
- Sample app additions for Vue.
- Build/test governance updates for TypeScript declaration validation and template debug checks.
