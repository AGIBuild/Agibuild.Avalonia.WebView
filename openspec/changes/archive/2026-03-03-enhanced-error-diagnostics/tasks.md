# Enhanced Error Diagnostics — Tasks

## 1. BridgeErrorDiagnostic Enum

- [x] 1.1 Create `BridgeErrorDiagnostic` enum in `Agibuild.Fulora.Core` (or appropriate project) with values: ServiceNotFound, MethodNotFound, ParameterMismatch, SerializationError, TimeoutError, ServiceNotExposed, Cancelled, InternalError
- [x] 1.2 Define numeric code mapping for each enum value (reserved range -32050 to -32099)
- [x] 1.3 Add `ToJsonRpcCode()` or equivalent to map enum to JSON-RPC error code
- [x] 1.4 Document each diagnostic code and when it is used

## 2. Error Response Structure

- [x] 2.1 Define `BridgeErrorData` (or equivalent) type with: DiagnosticCode, Hint, ServiceName, MethodName, ElapsedMs (optional)
- [x] 2.2 Ensure error responses populate `error.data` with diagnostic context when applicable
- [x] 2.3 Add `EnableDevToolsDiagnostics` (or equivalent) to `BridgeOptions` or `WebMessageBridgeOptions`
- [x] 2.4 Implement hint generation: only include hints when `EnableDevToolsDiagnostics` is true

## 3. RuntimeBridgeService Error Handling

- [x] 3.1 Map "service not registered" to `ServiceNotFound` with appropriate hint
- [x] 3.2 Map "method not found on service" to `MethodNotFound` with hint (optionally list available methods when DevTools enabled)
- [x] 3.3 Map parameter validation failures to `ParameterMismatch` with hint about expected types
- [x] 3.4 Map JSON serialization/deserialization exceptions to `SerializationError` with hint
- [x] 3.5 Map timeout to `TimeoutError`; include elapsed time in data when DevTools enabled
- [x] 3.6 Map cancellation to `Cancelled`
- [x] 3.7 Map unexpected exceptions to `InternalError` with generic message (no stack in production unless DevTools)
- [x] 3.8 Ensure all error paths use the centralized error builder that respects EnableDevToolsDiagnostics

## 4. Source Generator Diagnostics

- [x] 4.1 Add parameter validation pass: detect parameter types that cannot be serialized (beyond existing AGBR003 for ref/out/in)
- [x] 4.2 Emit AGBR007 (or next available) for unsupported parameter types with actionable message
- [x] 4.3 Emit AGBR008 (or next available) for parameter types that may cause runtime serialization issues (e.g., complex generics) as Warning
- [x] 4.4 Ensure diagnostic messages suggest fixes (e.g., "Use a concrete DTO type")
- [x] 4.5 Update `bridge-v1-boundary-diagnostics` spec or delta to document new diagnostics

## 5. Hint Message Catalog

- [x] 5.1 Create hint message catalog (resource file or constants) for each diagnostic
- [x] 5.2 ServiceNotFound: "Service '{0}' is not registered. Did you call Expose<{0}>()?"
- [x] 5.3 MethodNotFound: "Method '{0}.{1}' not found. Available methods: {2}" (when DevTools)
- [x] 5.4 ParameterMismatch: "Parameter mismatch for {0}.{1}. Expected: {2}"
- [x] 5.5 SerializationError: "Failed to serialize/deserialize parameters. Ensure types are JSON-serializable."
- [x] 5.6 TimeoutError: "Call timed out after {0} ms."

## 6. Configuration & DI

- [x] 6.1 Add `EnableDevToolsDiagnostics` to builder/options; default false
- [x] 6.2 Wire option to RuntimeBridgeService (or error handler)
- [x] 6.3 Document in BridgeOptions or Fulora docs when to enable

## 7. Tests

- [x] 7.1 Unit tests: Each BridgeErrorDiagnostic value maps to correct JSON-RPC code
- [x] 7.2 Unit tests: ServiceNotFound error includes hint when EnableDevToolsDiagnostics true, not when false
- [x] 7.3 Unit tests: MethodNotFound error includes hint and optionally available methods when DevTools enabled
- [x] 7.4 Unit tests: SerializationError, TimeoutError, Cancelled produce correct diagnostic codes
- [x] 7.5 Unit tests: Source generator emits AGBR007/AGBR008 for unsupported parameter types
- [x] 7.6 Integration test: Full bridge call failure flow returns correct error structure with data
