## Why
Bridge call errors currently return generic JSON-RPC error codes. Developers must dig into logs to understand root causes. Enhanced error messages with actionable hints ("did you forget to expose this service?", "method signature mismatch") would reduce debugging time significantly. Goal: E2 (Dev Tooling).

## What Changes
- Introduce BridgeErrorDiagnostic enum with specific error codes (ServiceNotFound, MethodNotFound, ParameterMismatch, SerializationError, TimeoutError, etc.)
- Include actionable hint text in error responses when DevTools mode is enabled
- Source generator emits parameter validation diagnostics at compile time
- Runtime bridge service includes diagnostic context in error responses

## Capabilities
### New Capabilities
- `enhanced-error-diagnostics`: Rich bridge call error diagnostics with actionable hints

### Modified Capabilities
- `bridge-v1-boundary-diagnostics`: Extend with runtime error diagnostics alongside compile-time

## Non-goals
- Auto-fix suggestions, IDE quick-fix integration (that's the VS Code extension)

## Impact
- Modified: RuntimeBridgeService error handling
- Modified: Bridge source generator (compile-time diagnostics)
- New: BridgeErrorDiagnostic enum
