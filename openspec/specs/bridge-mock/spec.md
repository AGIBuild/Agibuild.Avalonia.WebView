# Bridge Mock Spec

## Overview
MockBridgeService for unit testing ViewModels and services that depend on IBridgeService.

## Requirements

### RM-1: MockBridgeService implements IBridgeService
- `Expose<T>`: records implementation in ConcurrentDictionary
- `GetProxy<T>`: returns pre-configured proxy (via SetupProxy)
- `Remove<T>`: removes recorded implementation

### RM-2: Setup helpers
- `SetupProxy<T>(T proxy)`: configures proxy returned by GetProxy

### RM-3: Assertion helpers
- `WasExposed<T>()`: returns true if Expose was called
- `GetExposedImplementation<T>()`: returns stored implementation
- `ExposedCount`: number of exposed services
- `Reset()`: clears all state

### RM-4: Lifecycle
- `Dispose()` makes all operations throw ObjectDisposedException

## Test Coverage
- 8 CTs in `MockBridgeServiceTests`
