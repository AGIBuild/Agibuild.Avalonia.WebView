## ADDED Requirements

### Requirement: Bridge generated JS/TS contracts SHALL support typed binary payload ergonomics
Bridge-generated client contracts MUST support `byte[]` payloads as typed binary values on JavaScript side while preserving deterministic JSON-RPC transport.

#### Scenario: Binary parameter is encoded deterministically before invoke
- **WHEN** a generated bridge JS method receives a `Uint8Array` argument for a `byte[]` parameter
- **THEN** the method encodes it to base64 string payload before `rpc.invoke`
- **AND** transport payload shape remains valid JSON

#### Scenario: Binary return value is decoded deterministically for binary-returning methods
- **WHEN** a generated bridge JS method receives base64 string result for a `byte[]` return
- **THEN** the method returns a `Uint8Array` to caller
- **AND** non-binary methods are unaffected

### Requirement: Runtime transport compatibility SHALL remain JSON-RPC compliant
Binary ergonomics MUST NOT change envelope protocol semantics or require non-JSON side channels.

#### Scenario: Existing JSON-RPC envelope remains unchanged
- **WHEN** bridge methods send/receive binary payloads
- **THEN** runtime still exchanges standard JSON-RPC request/response envelopes
- **AND** envelope-level behavior remains backward-compatible within current runtime contracts
