## Purpose
Define the JS bridge contract for export/import attributes, service registration, and proxy behavior across runtime implementations.

## Requirements

### Requirement: JsExport attribute exists in Core assembly
The Core assembly SHALL define a `[JsExport]` attribute applicable to interfaces.
The attribute SHALL accept an optional `Name` property (string) to override the default service name.
The default service name SHALL be the interface name with the leading `I` removed (e.g., `IAppService` → `AppService`).

#### Scenario: JsExport attribute is resolvable
- **WHEN** a consumer applies `[JsExport]` to an interface
- **THEN** it compiles without errors in the `Agibuild.Fulora` namespace

#### Scenario: JsExport with custom name
- **WHEN** `[JsExport(Name = "app")]` is applied to an interface
- **THEN** the bridge service name is `"app"` instead of the derived default

### Requirement: JsImport attribute exists in Core assembly
The Core assembly SHALL define a `[JsImport]` attribute applicable to interfaces.
The attribute SHALL accept an optional `Name` property (string) to override the default service name.

#### Scenario: JsImport attribute is resolvable
- **WHEN** a consumer applies `[JsImport]` to an interface
- **THEN** it compiles without errors in the `Agibuild.Fulora` namespace

### Requirement: IBridgeService contract exists in Core assembly
The Core assembly SHALL define an `IBridgeService` interface with:
- `void Expose<T>(T implementation, BridgeOptions? options = null)` — registers a `[JsExport]` service implementation
- `T GetProxy<T>()` — returns a proxy for a `[JsImport]` interface
- `void Remove<T>()` — unregisters a previously exposed service and removes the JS-side stub

#### Scenario: IBridgeService is resolvable
- **WHEN** a consumer references `IBridgeService`
- **THEN** it compiles without missing type errors

#### Scenario: Remove cleans up JS stub
- **WHEN** `Remove<T>()` is called for a previously exposed service
- **THEN** RPC handlers for that service are unregistered
- **AND** `window.agWebView.bridge.<ServiceName>` is deleted via script execution
- **AND** the service name is logged via the bridge tracer

#### Scenario: Remove cleanup tolerates script execution failure
- **WHEN** `Remove<T>()` is called and the JS stub cleanup script fails (e.g., page not loaded)
- **THEN** the RPC handlers are still unregistered
- **AND** the error is logged but does not propagate as an exception

### Requirement: BridgeOptions type exists in Core assembly
The Core assembly SHALL define a `BridgeOptions` class with:
- `IReadOnlySet<string>? AllowedOrigins { get; init; }` — origin allowlist (null = inherit from WebMessageBridgeOptions)

#### Scenario: BridgeOptions is resolvable
- **WHEN** a consumer creates a `BridgeOptions` instance
- **THEN** it compiles without missing type errors

### Requirement: WebViewCore exposes Bridge property
`WebViewCore` SHALL expose an `IBridgeService? Bridge` property.
The property SHALL return `null` before the WebMessage bridge is enabled.
When `Bridge.Expose<T>()` is called before `EnableWebMessageBridge()`, the bridge SHALL auto-enable with default options.

#### Scenario: Bridge is null before enable
- **WHEN** a `WebViewCore` is created and the bridge has not been enabled
- **THEN** `Bridge` returns `null`

#### Scenario: Bridge auto-enables on first Expose
- **WHEN** `Bridge.Expose<T>(impl)` is called without prior `EnableWebMessageBridge()`
- **THEN** the bridge auto-enables with default `WebMessageBridgeOptions` (empty AllowedOrigins = allow all)
- **AND** the RPC service is created and JS stub is injected

#### Scenario: Bridge uses existing bridge options when pre-enabled
- **WHEN** `EnableWebMessageBridge(customOptions)` is called first
- **AND** then `Bridge.Expose<T>(impl)` is called
- **THEN** the bridge uses the custom options already configured

### Requirement: Expose registers RPC handlers for JsExport interface methods
The runtime SHALL register RPC handlers when `Bridge.Expose<T>(impl)` is called with a `[JsExport]` interface `T`:
- Each method on `T` SHALL be registered as an RPC handler via `IWebViewRpcService.Handle()`
- The RPC method name SHALL be `"{ServiceName}.{camelCaseMethodName}"`
- Parameters SHALL be deserialized from JSON-RPC named params (object format)
- Return values SHALL be serialized back as JSON-RPC result

#### Scenario: Exposed method is callable via RPC
- **WHEN** `Bridge.Expose<IAppService>(impl)` is called
- **AND** a JSON-RPC request `{ "method": "AppService.getCurrentUser", "params": {} }` is received
- **THEN** the runtime calls `impl.GetCurrentUser()` and returns the serialized result

#### Scenario: Method name uses camelCase
- **WHEN** a C# method is named `GetCurrentUser`
- **THEN** the RPC method name is `"ServiceName.getCurrentUser"` (camelCase)

#### Scenario: Named parameters are deserialized
- **WHEN** a method `SearchItems(string query, int limit)` receives params `{ "query": "test", "limit": 10 }`
- **THEN** the parameters are correctly deserialized and passed to the implementation

### Requirement: Expose injects JS client stub
When `Bridge.Expose<T>(impl)` is called, the runtime SHALL inject a JavaScript client stub that:
- Creates `window.agWebView.bridge.{ServiceName}` object
- Each method on the object calls `agWebView.rpc.invoke("{ServiceName}.{methodName}", params)`
- The stub is injected via `InvokeScriptAsync` (and optionally via Preload Script for subsequent navigations)

#### Scenario: JS stub creates service proxy
- **WHEN** `Bridge.Expose<IAppService>(impl)` is called
- **THEN** `window.agWebView.bridge.AppService.getCurrentUser()` is available in JavaScript

### Requirement: GetProxy returns a typed proxy for JsImport interfaces
The runtime SHALL return a typed proxy when `Bridge.GetProxy<T>()` is called with a `[JsImport]` interface `T`:
- The runtime SHALL return an object implementing `T`
- Each method call SHALL invoke `IWebViewRpcService.InvokeAsync("{ServiceName}.{camelCaseMethodName}", params)`
- The result SHALL be deserialized to the method's return type

#### Scenario: Proxy method calls JS via RPC
- **WHEN** `var ui = Bridge.GetProxy<IUiController>()` is obtained
- **AND** `await ui.ShowNotification("hello")` is called
- **THEN** the RPC layer sends `{ "method": "UiController.showNotification", "params": { "message": "hello" } }` to JS

### Requirement: Remove unregisters a previously exposed service
The runtime SHALL unregister service handlers when `Bridge.Remove<T>()` is called:
- All RPC handlers for the service SHALL be removed
- The JS client stub SHALL NOT be automatically removed (JS cleanup is the web content's responsibility)

#### Scenario: Removed service returns method-not-found
- **WHEN** `Bridge.Remove<IAppService>()` is called
- **AND** a JSON-RPC request for `"AppService.getCurrentUser"` is received
- **THEN** a JSON-RPC error with code `-32601` (method not found) is returned

### Requirement: Expose on non-JsExport interface throws
`Bridge.Expose<T>()` SHALL throw `InvalidOperationException` if `T` is not decorated with `[JsExport]`.

#### Scenario: Expose without JsExport attribute throws
- **WHEN** `Bridge.Expose<INotDecorated>(impl)` is called
- **AND** `INotDecorated` does not have `[JsExport]`
- **THEN** `InvalidOperationException` is thrown

### Requirement: GetProxy on non-JsImport interface throws
`Bridge.GetProxy<T>()` SHALL throw `InvalidOperationException` if `T` is not decorated with `[JsImport]`.

#### Scenario: GetProxy without JsImport attribute throws
- **WHEN** `Bridge.GetProxy<INotDecorated>()` is called
- **AND** `INotDecorated` does not have `[JsImport]`
- **THEN** `InvalidOperationException` is thrown

### Requirement: Bridge is disposed with WebViewCore
The runtime SHALL dispose bridge state when `WebViewCore.Dispose()` is called:
- All exposed services SHALL be unregistered
- Subsequent calls to `Bridge.Expose<T>()` or `Bridge.GetProxy<T>()` SHALL throw `ObjectDisposedException`

#### Scenario: Bridge operations after dispose throw
- **WHEN** `WebViewCore` is disposed
- **AND** `Bridge.Expose<T>(impl)` is called
- **THEN** `ObjectDisposedException` is thrown

### Requirement: Exception in exposed method returns JSON-RPC error
The runtime SHALL return a JSON-RPC error response when an exposed method throws an exception:
- The runtime SHALL return a JSON-RPC error response with code `-32603`
- The `message` field SHALL contain the exception message
- When `EnableDevTools` is true, a `data.type` field SHALL contain the exception type name

#### Scenario: Exception produces JSON-RPC error
- **WHEN** an exposed method throws `ArgumentException("id must be positive")`
- **THEN** the JSON-RPC error response has `code: -32603` and `message: "id must be positive"`

### Requirement: Duplicate Expose for same interface throws
`Bridge.Expose<T>()` SHALL throw `InvalidOperationException` if `T` has already been exposed and not removed.

#### Scenario: Double expose throws
- **WHEN** `Bridge.Expose<IAppService>(impl1)` is called
- **AND** `Bridge.Expose<IAppService>(impl2)` is called without removing first
- **THEN** `InvalidOperationException` is thrown

### Requirement: WebView control exposes Bridge property
The `WebView` Avalonia control SHALL expose an `IBridgeService? Bridge` property that delegates to the underlying `WebViewCore.Bridge`.

#### Scenario: WebView.Bridge delegates to core
- **WHEN** the WebView control is attached
- **THEN** `WebView.Bridge` returns the same instance as `WebViewCore.Bridge`

### Requirement: IWebViewRpcService SHALL support CancellationToken-aware handlers
The `IWebViewRpcService` interface SHALL expose methods for registering and retrieving CancellationTokenSources keyed by request ID, enabling cancellation-aware bridge handlers.

#### Scenario: Register and retrieve CTS
- **WHEN** a bridge handler registers a CTS for an active request
- **THEN** the CTS is retrievable by request ID for cancellation

### Requirement: Overloaded export methods register with unique RPC names
When `Bridge.Expose<T>(impl)` is called with an interface that has overloaded methods, each overload SHALL be registered with a unique RPC method name based on its visible parameter count.

#### Scenario: Overloaded method RPC handlers are individually addressable
- **WHEN** `Bridge.Expose<ISearchService>(impl)` is called where `ISearchService` has `Search(string q)` and `Search(string q, int limit)`
- **THEN** RPC handlers for `SearchService.search` and `SearchService.search$2` are registered
- **AND** each handler correctly deserializes and invokes its respective overload

### Requirement: Overloaded import proxy dispatches to correct RPC name
When `Bridge.GetProxy<T>()` is called with an interface that has overloaded methods, each proxy method SHALL invoke its specific RPC method name.

#### Scenario: Proxy overload calls correct RPC endpoint
- **WHEN** a proxy for an overloaded import interface is obtained
- **AND** the 2-param overload is called
- **THEN** the proxy sends an RPC request to `Service.method$2`
