## MODIFIED Requirements

### Requirement: GetProxy returns a typed proxy for JsImport interfaces
When `Bridge.GetProxy<T>()` is called with a `[JsImport]` interface `T`, the runtime SHALL enforce async-only proxy semantics:
- The runtime SHALL return an object implementing `T`
- Each method call SHALL invoke `IWebViewRpcService.InvokeAsync("{ServiceName}.{camelCaseMethodName}", params)`
- The result SHALL be deserialized to the method's return type
- `[JsImport]` methods MUST return `Task` or `Task<T>`; synchronous return types are invalid

#### Scenario: Proxy method calls JS via RPC
- **WHEN** `var ui = Bridge.GetProxy<IUiController>()` is obtained
- **AND** `await ui.ShowNotification("hello")` is called
- **THEN** the RPC layer sends `{ "method": "UiController.showNotification", "params": { "message": "hello" } }` to JS

#### Scenario: Sync-return JsImport method is rejected
- **WHEN** a `[JsImport]` interface declares `string GetName()`
- **THEN** proxy invocation throws `NotSupportedException` with guidance to use `Task`/`Task<T>`
