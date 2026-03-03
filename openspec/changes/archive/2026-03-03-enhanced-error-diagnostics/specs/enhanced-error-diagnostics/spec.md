# Enhanced Error Diagnostics — Spec

## Purpose

Define BDD-style requirements for enhanced bridge error diagnostics: `BridgeErrorDiagnostic` enum, actionable hints in error responses, source generator parameter validation, and runtime diagnostic context. Ensures developers receive actionable error information to reduce debugging time.

## Requirements

### Requirement: BridgeErrorDiagnostic enum defines specific error codes

The runtime SHALL define a `BridgeErrorDiagnostic` enum (or equivalent) with specific error codes for bridge call failures.

#### Scenario: Enum includes ServiceNotFound and MethodNotFound
- **GIVEN** the `BridgeErrorDiagnostic` enum
- **WHEN** the enum is inspected
- **THEN** it SHALL include `ServiceNotFound` (for unregistered service)
- **AND** SHALL include `MethodNotFound` (for missing method on service)

#### Scenario: Enum includes ParameterMismatch, SerializationError, TimeoutError
- **GIVEN** the `BridgeErrorDiagnostic` enum
- **THEN** it SHALL include `ParameterMismatch` (invalid params for method)
- **AND** SHALL include `SerializationError` (JSON serialize/deserialize failure)
- **AND** SHALL include `TimeoutError` (call exceeded timeout)

#### Scenario: Enum includes ServiceNotExposed, Cancelled, InternalError
- **GIVEN** the `BridgeErrorDiagnostic` enum
- **THEN** it SHALL include `ServiceNotExposed` (service not yet exposed)
- **AND** SHALL include `Cancelled` (call was cancelled)
- **AND** SHALL include `InternalError` (unexpected exception)

### Requirement: Error responses include diagnostic code in data

JSON-RPC error responses SHALL include the diagnostic code in the `data` field when available.

#### Scenario: Error data includes diagnosticCode
- **GIVEN** a bridge call that fails with `ServiceNotFound`
- **WHEN** the JSON-RPC error response is returned
- **THEN** the `error.data` field SHALL include `diagnosticCode: "ServiceNotFound"` (or equivalent string)
- **AND** the standard `error.code` SHALL remain a valid JSON-RPC code

#### Scenario: Error data may include serviceName and methodName
- **GIVEN** a bridge call that fails with `MethodNotFound` for `AppService.GetData`
- **WHEN** the error response is returned
- **THEN** `error.data` MAY include `serviceName: "AppService"` and `methodName: "GetData"`
- **AND** this SHALL aid debugging and logging

### Requirement: Actionable hints only when DevTools diagnostics enabled

Actionable hint text SHALL be included in error responses only when DevTools diagnostics are enabled.

#### Scenario: Hint included when EnableDevToolsDiagnostics is true
- **GIVEN** `BridgeOptions.EnableDevToolsDiagnostics` (or equivalent) is true
- **AND** a bridge call fails with `ServiceNotFound` for service "AppService"
- **WHEN** the error response is returned
- **THEN** `error.data.hint` SHALL include actionable text (e.g., "Did you call Expose<IAppService>()?")
- **AND** the hint SHALL reference the service or method where applicable

#### Scenario: Hint omitted when DevTools diagnostics disabled
- **GIVEN** `EnableDevToolsDiagnostics` is false (default)
- **AND** a bridge call fails
- **WHEN** the error response is returned
- **THEN** `error.data.hint` SHALL NOT be present (or SHALL be a generic message only)
- **AND** the response SHALL NOT reveal internal structure beyond what is necessary for the error code

### Requirement: RuntimeBridgeService maps exceptions to BridgeErrorDiagnostic

`RuntimeBridgeService` SHALL map exceptions and failure conditions to appropriate `BridgeErrorDiagnostic` values.

#### Scenario: Unregistered service returns ServiceNotFound
- **GIVEN** a bridge client invokes `NonExistentService.GetData`
- **AND** no service named "NonExistentService" is registered
- **WHEN** the call is processed
- **THEN** the error SHALL have diagnostic code `ServiceNotFound`
- **AND** when DevTools enabled, the hint SHALL suggest registering the service

#### Scenario: Unknown method returns MethodNotFound
- **GIVEN** a registered service "AppService" that does not have method "InvalidMethod"
- **WHEN** the client invokes `AppService.InvalidMethod`
- **THEN** the error SHALL have diagnostic code `MethodNotFound`
- **AND** when DevTools enabled, the hint MAY list available methods

#### Scenario: Serialization failure returns SerializationError
- **GIVEN** a bridge call with parameters that cannot be JSON-serialized or deserialized
- **WHEN** serialization fails
- **THEN** the error SHALL have diagnostic code `SerializationError`
- **AND** the message SHALL indicate serialization failure

#### Scenario: Timeout returns TimeoutError
- **GIVEN** a bridge call that exceeds the configured timeout
- **WHEN** the timeout occurs
- **THEN** the error SHALL have diagnostic code `TimeoutError`
- **AND** when DevTools enabled, `data` MAY include elapsed time

### Requirement: Source generator emits parameter validation diagnostics

The bridge source generator SHALL emit diagnostics for parameter types that are not supported or may cause runtime serialization issues.

#### Scenario: Unsupported parameter type emits diagnostic
- **GIVEN** a `[JsExport]` interface with a parameter type that cannot be serialized (e.g., `ref`, `out`, `in` — AGBR003, or a new check for unsupported types)
- **WHEN** the source generator processes the interface
- **THEN** the generator SHALL emit a diagnostic with an actionable message
- **AND** the message SHALL suggest a fix (e.g., use a serializable DTO)

#### Scenario: Diagnostic uses AGBR ID range
- **GIVEN** a new parameter validation diagnostic
- **WHEN** the diagnostic is emitted
- **THEN** it SHALL use an ID in the AGBR range (e.g., AGBR007, AGBR008)
- **AND** the severity SHALL be Error for blocking issues, Warning for advisory

### Requirement: Errors remain JSON-RPC compliant

All error responses SHALL remain compliant with JSON-RPC 2.0 specification.

#### Scenario: Error structure is valid JSON-RPC
- **GIVEN** any bridge error response
- **WHEN** the response is inspected
- **THEN** it SHALL have structure `{ jsonrpc: "2.0", id, error: { code: number, message: string, data?: object } }`
- **AND** the `code` SHALL be a number (standard or implementation-defined range)
- **AND** the `message` SHALL be a non-empty string
