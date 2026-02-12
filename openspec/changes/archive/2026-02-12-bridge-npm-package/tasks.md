# bridge-npm-package — Tasks

## Task 1: Create package.json + tsconfig.json
**Acceptance**: `packages/bridge/package.json` and `tsconfig.json` with correct name, exports, and build configuration.

## Task 2: Implement src/index.ts
**Acceptance**: `bridge` singleton, `createBridge` factory, `BridgeService` interface; `invoke<T>`, `handle`, `removeHandler`, `ready()`, `getService<T>`.

## Task 3: Build and verify
**Acceptance**: npm build succeeds; package can be consumed as `@agibuild/bridge`.
