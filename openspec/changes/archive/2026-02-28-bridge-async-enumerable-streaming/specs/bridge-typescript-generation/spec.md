## ADDED Requirements

### Requirement: IAsyncEnumerable maps to AsyncIterable in TypeScript
When a bridge method returns `IAsyncEnumerable<T>`, the TypeScript declaration SHALL emit `AsyncIterable<T>` as the return type.

#### Scenario: IAsyncEnumerable return generates AsyncIterable declaration
- **WHEN** a `[JsExport]` method is `IAsyncEnumerable<string> StreamChat(string prompt)`
- **THEN** the TypeScript declaration is `streamChat(prompt: string): AsyncIterable<string>`
