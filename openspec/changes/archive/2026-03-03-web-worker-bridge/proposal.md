## Why

Bridge calls from Web Workers are not supported — only the main thread can access the bridge. Heavy computation (data processing, crypto, image manipulation) blocks the main thread and UI. Extending bridge access to Web Workers enables computation offload while maintaining type safety. Goal: Phase 12 M12.3.

## What Changes

- Extend @agibuild/bridge npm package with a WorkerBridgeClient that proxies bridge calls from a Web Worker to the main thread via MessagePort
- Main thread BridgeClient registers as a relay: Worker → Main → Host C# → Main → Worker
- WorkerBridgeClient has the same typed API as main-thread BridgeClient
- Bridge source generator emits worker-compatible stubs alongside main-thread stubs
- Worker bridge calls are transparently async (same as main thread)

## Capabilities

### New Capabilities
- `web-worker-bridge`: Bridge call support from Web Workers via main-thread relay

### Modified Capabilities
- `bridge-npm-distribution`: Add WorkerBridgeClient export to @agibuild/bridge package

## Non-goals

- SharedWorker support in v1, ServiceWorker bridge, direct Worker→Host bypass (must relay through main thread)

## Impact

- Modified: packages/bridge/ (add WorkerBridgeClient, relay logic)
- Modified: bridge source generator (worker stub emission)
- New tests: worker bridge integration
