## Purpose
Define bridge security contracts for rate limiting and deterministic RPC error semantics.

## Requirements

### Requirement: RateLimit configuration is validated
Bridge security contracts SHALL define a `RateLimit(maxCalls, window)` configuration that rejects invalid values and supports optional per-service configuration via `BridgeOptions`.

#### Scenario: Invalid rate-limit configuration is rejected
- **WHEN** `maxCalls <= 0` or `window <= TimeSpan.Zero`
- **THEN** rate-limit configuration is rejected deterministically

### Requirement: Sliding-window rate limiting is enforced
When rate limiting is configured, the runtime SHALL enforce a sliding-window limit and SHALL reject over-limit calls with `WebViewRpcException(-32029, "Rate limit exceeded")`.

#### Scenario: Over-limit call returns deterministic RPC error
- **WHEN** call volume exceeds configured window limits
- **THEN** the request fails with RPC error code `-32029`

### Requirement: Source-generated and reflection paths apply equivalent rate-limiting semantics
The runtime SHALL apply rate-limiting semantics consistently to both source-generated and reflection-based bridge handler registration paths.

#### Scenario: Both registration paths are rate-limited
- **WHEN** handlers are registered through either generation path
- **THEN** over-limit behavior is equivalent and deterministic

### Requirement: RPC error code preservation is maintained
`WebViewRpcService.DispatchRequestAsync` SHALL preserve `WebViewRpcException.Code`, and non-RPC exceptions SHALL map to code `-32603`.

#### Scenario: RPC and non-RPC exception codes are distinguishable
- **WHEN** dispatch handles mixed exception categories
- **THEN** RPC exception codes are preserved and non-RPC exceptions use `-32603`
