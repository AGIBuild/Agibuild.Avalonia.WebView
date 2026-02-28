## ADDED Requirements

### Requirement: CancellationToken maps to AbortSignal options in TypeScript
When a bridge method has a CancellationToken parameter, the TypeScript declaration SHALL emit an `options?: { signal?: AbortSignal }` parameter instead of the CancellationToken.

#### Scenario: Method with CancellationToken generates AbortSignal option
- **WHEN** a `[JsExport]` method is `Task<string> Search(string query, CancellationToken ct)`
- **THEN** the TypeScript declaration is `search(query: string, options?: { signal?: AbortSignal }): Promise<string>`
