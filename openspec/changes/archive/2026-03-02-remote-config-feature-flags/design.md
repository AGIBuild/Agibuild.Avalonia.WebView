## Context

Fulora apps may need config values (API URLs, feature flags, theme defaults) that change without redeployment. Today, developers use appsettings.json, environment variables, or custom HTTP fetches. JS has no direct access; it must call C# bridge methods to get config. A standard provider with local + remote support and bridge exposure would simplify this.

**Gap**: No `IConfigProvider`; no bridge-exposed config; no standard remote + fallback pattern.

## Goals / Non-Goals

**Goals:**
- `IConfigProvider` with `GetValue<T>(key)`, `GetSection(key)`, `RefreshAsync()`
- Local JSON file as default fallback
- Optional remote HTTP endpoint; merge remote over local when available
- Bridge-exposed config so JS can read values (e.g., `config.get('featureX')`)

**Non-Goals:**
- Full Microsoft.Extensions.Configuration compatibility
- Real-time push; polling or manual refresh only
- Secret management (config values assumed non-sensitive)

## Decisions

### D1: Config structure

**Choice**: Hierarchical key-value (e.g., `"Features:NewDashboard"` or `config.getSection("Features")`). Values are JSON-serializable. Remote endpoint returns JSON object; merge with local by key.

**Rationale**: Simple, flexible. Matches common config patterns.

### D2: Refresh strategy

**Choice**: `RefreshAsync()` triggers fetch from remote. Optional background polling (configurable interval). On fetch failure, keep last known remote or fall back to local. No automatic refresh on startup unless explicitly called.

**Rationale**: Explicit refresh keeps behavior predictable; polling is optional for freshness.

### D3: Bridge exposure

**Choice**: Expose `config.getValue(key)`, `config.getSection(key)` (returns JSON object) to JS. Read-only. C# remains source of truth; bridge proxies to `IConfigProvider`.

**Rationale**: JS can drive feature flags and UI based on config without C# round-trips for each value.

## Risks / Trade-offs

- **[Risk] Remote endpoint availability** → Must handle offline, timeout, errors. Fallback to local is critical.
- **[Risk] Stale config** → Without polling, config may be stale until app restart or manual refresh. Document.
- **[Trade-off] Read-only from JS** → Writing config from JS would require C# API; out of scope for v1.
