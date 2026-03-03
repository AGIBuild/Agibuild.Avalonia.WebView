# Enhanced Error Diagnostics — Design

## Context

Bridge call errors currently return generic JSON-RPC error codes (e.g., -32601 Method not found, -32602 Invalid params). Developers must dig into logs to understand root causes such as "service not exposed", "method signature mismatch", or "serialization failure". Enhanced error diagnostics with specific error codes and actionable hints would reduce debugging time. Goal: E2 (Dev Tooling).

**Existing contracts**: `RuntimeBridgeService`, `IBridgeTracer`, JSON-RPC error format `{ code, message, data }`, bridge source generator (compile-time diagnostics AGBR001–AGBR006), `bridge-v1-boundary-diagnostics` spec.

## Goals / Non-Goals

### Goals

- Introduce `BridgeErrorDiagnostic` enum with specific error codes (ServiceNotFound, MethodNotFound, ParameterMismatch, SerializationError, TimeoutError, etc.)
- Include actionable hint text in error responses when DevTools mode is enabled
- Source generator emits parameter validation diagnostics at compile time (e.g., unsupported parameter types)
- Runtime bridge service includes diagnostic context in error responses (error code, hint, optional stack)
- Errors remain JSON-RPC compliant; extended data in `data` field

### Non-Goals

- Auto-fix suggestions in error responses
- IDE quick-fix integration (handled by VS Code extension)
- Changing JSON-RPC error code semantics for standard codes

## Decisions

### D1: BridgeErrorDiagnostic enum

**Decision**: Introduce `BridgeErrorDiagnostic` enum with values: `ServiceNotFound`, `MethodNotFound`, `ParameterMismatch`, `SerializationError`, `TimeoutError`, `ServiceNotExposed`, `Cancelled`, `InternalError`. Each maps to a numeric code in a reserved range (e.g., -32000 to -32099) to avoid conflicting with JSON-RPC standard codes (-32700 to -32603).

**Rationale**: Structured error codes enable programmatic handling and filtering. Reserved range keeps JSON-RPC compliance. Enum provides type safety on C# side; JS receives numeric code and string name.

### D2: Actionable hints only when DevTools enabled

**Decision**: Actionable hint text (e.g., "Did you forget to call Expose<IAppService>()?") SHALL be included in error responses only when `BridgeOptions.EnableDevToolsDiagnostics` (or equivalent) is true. In production, errors SHALL include only the error code and a generic message.

**Rationale**: Hints may reveal internal structure (service names, method names). Restricting to dev mode avoids information leakage. Production errors remain minimal for security.

### D3: Error response structure

**Decision**: JSON-RPC error response SHALL use `{ jsonrpc: "2.0", id, error: { code, message, data } }`. The `data` field SHALL optionally include: `{ diagnosticCode: string, hint?: string, serviceName?: string, methodName?: string }`. Standard JSON-RPC `code` remains for compatibility; `data.diagnosticCode` carries the `BridgeErrorDiagnostic` name.

**Rationale**: Existing clients that only read `code` and `message` continue to work. Extended `data` provides rich context for DevTools and debugging. `diagnosticCode` allows string matching without depending on numeric range.

### D4: Source generator parameter validation diagnostics

**Decision**: The bridge source generator SHALL emit additional diagnostics for parameter validation: unsupported parameter types (e.g., `ref`, `out`, `in` — already AGBR003), complex generic parameters, or types that cannot be serialized. Use diagnostic IDs in the AGBR range (e.g., AGBR007, AGBR008) with actionable messages.

**Rationale**: Compile-time diagnostics catch issues before runtime. Extends `bridge-v1-boundary-diagnostics` with parameter-specific checks. Actionable messages ("Use a concrete DTO instead of T") help developers fix issues quickly.

### D5: RuntimeBridgeService error handling

**Decision**: `RuntimeBridgeService` SHALL catch exceptions during export call handling and map them to `BridgeErrorDiagnostic` values. When a service is not found, use `ServiceNotFound` with hint "Service '{name}' is not registered. Did you call Expose<T>()?" When a method is not found, use `MethodNotFound` with hint including available methods. Serialization failures map to `SerializationError` with hint about parameter types.

**Rationale**: Centralized mapping ensures consistent error responses. Hints are generated from runtime context (registered services, method names). Reduces need for log diving.

### D6: Timeout and cancellation

**Decision**: When a bridge call times out or is cancelled, use `TimeoutError` or `Cancelled` respectively. Include elapsed time in `data` for timeout when DevTools enabled.

**Rationale**: Distinguishing timeout from other failures helps debugging. Cancellation is a distinct case (user-initiated). Elapsed time aids performance investigation.

## Risks / Trade-offs

### R1: Hint text maintenance

**Risk**: Hint messages may become stale or inconsistent as APIs evolve.

**Mitigation**: Centralize hint strings in a resource or constant file. Review during API changes. Keep hints generic enough to remain useful.

### R2: Information leakage in production

**Risk**: Accidentally enabling DevTools diagnostics in production could expose internal structure.

**Mitigation**: Default `EnableDevToolsDiagnostics` to false. Document clearly. Consider build-time removal of hint generation in Release config if needed.

### R3: Diagnostic code range

**Risk**: Custom codes in -32000 range could conflict with future JSON-RPC extensions.

**Mitigation**: Use a sub-range (e.g., -32050 to -32099) and document. JSON-RPC 2.0 spec leaves -32000 to -32099 for "server error" implementation-defined; we use it for our diagnostics.
