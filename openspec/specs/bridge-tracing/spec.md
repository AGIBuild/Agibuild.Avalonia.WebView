# Bridge Tracing Spec

## Overview
Pluggable tracing for all Bridge RPC calls.

## Requirements

### BT-1: IBridgeTracer interface
- 7 event methods covering export calls, import calls, and service lifecycle
- Located in Core (zero dependencies)

### BT-2: LoggingBridgeTracer
- Default ILogger-based implementation in Runtime
- Structured templates, param truncation

### BT-3: NullBridgeTracer
- Singleton no-op, zero overhead in production

### BT-4: RuntimeBridgeService integration
- Optional tracer parameter, fires events on Expose/Remove

## Test Coverage
- 5 CTs in BridgeTracerTests
