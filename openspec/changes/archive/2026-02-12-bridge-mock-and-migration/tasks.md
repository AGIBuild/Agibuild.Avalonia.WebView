# bridge-mock-and-migration — Tasks

## Task 1: Create MockBridgeService
**Acceptance**: `MockBridgeService` in Core implements `IBridgeService`; `ConcurrentDictionary` stores; `SetupProxy`, `WasExposed`, `GetExposedImplementation`, `Reset`, `ExposedCount`; `IDisposable` support.

## Task 2: Write 8 mock tests
**Acceptance**: `MockBridgeServiceTests` — Expose records, WasExposed false when not exposed, GetProxy returns configured, GetProxy without setup throws, Remove clears, Reset clears all, Dispose throws, ExposedCount tracks.

## Task 3: Write 4 migration tests
**Acceptance**: `BridgeMigrationTests` — raw RPC + Bridge coexist, raw RPC alone, Bridge auto-enable + raw RPC, Remove typed does not affect raw handlers.
