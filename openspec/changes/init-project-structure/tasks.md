## 1. Solution scaffolding (.NET 10, naming, references)

- [x] 1.1 Create solution file and projects using `Agibuild.Avalonia.WebView.*` naming
- [x] 1.2 Set TFMs: Core/Abstractions/DependencyInjection/Tests => `net10.0`; Windows => `net10.0-windows`; macOS => `net10.0-macos`; Android => `net10.0-android`; Gtk => `net10.0`
- [x] 1.3 Add project references to enforce dependency direction (Core -> Abstractions -> Adapters; Tests reference Core+Abstractions; DI references Core+Abstractions)
- [x] 1.4 Add minimal public types and namespaces to each project so all assemblies compile

## 2. Parameterized build (OS default; Android/Gtk optional)

- [x] 2.1 Implement default build to include only the current OS adapter project
- [x] 2.2 Add explicit build parameters to enable Android and/or Gtk adapter projects
- [x] 2.3 Add CI build targets/scripts to build the current OS adapter only (no Android/Gtk workloads required)

## 3. Core contracts (public API surface)

- [x] 3.1 Implement `IWebView` with all members and events defined in the spec (signatures only)
- [x] 3.2 Implement `IWebDialog`, `IWebAuthBroker`, `IWebViewEnvironmentOptions`, `INativeWebViewHandleProvider` (signatures only)
- [x] 3.3 Implement event args types (`NavigationStartingEventArgs`, `NavigationCompletedEventArgs`, `NewWindowRequestedEventArgs`, `WebMessageReceivedEventArgs`, `WebResourceRequestedEventArgs`, `EnvironmentRequestedEventArgs`)
- [x] 3.4 Add placeholder types `ICookieManager`, `ICommandManager`, `AuthOptions`, `WebAuthResult` so consumers compile

## 4. Adapter abstractions (lifecycle, parity, isolation)

- [x] 4.1 Implement `IWebViewAdapter` interface with lifecycle (`Initialize`, `Attach`, `Detach`), navigation, scripting, commands, state, and events
- [x] 4.2 Add contract assertions/tests for lifecycle sequencing (Initialize -> Attach -> Detach; no events after Detach)
- [x] 4.3 Ensure adapter event args types use Core event args types (compile-time)

## 5. Platform adapter skeletons

- [x] 5.1 Implement `WindowsWebViewAdapter` skeleton implementing `IWebViewAdapter` (may throw `NotSupportedException` for unimplemented behavior)
- [x] 5.2 Implement `MacOSWebViewAdapter` skeleton implementing `IWebViewAdapter` (may throw `NotSupportedException` for unimplemented behavior)
- [x] 5.3 Implement `AndroidWebViewAdapter` skeleton implementing `IWebViewAdapter` (excluded from default build)
- [x] 5.4 Implement `GtkWebViewAdapter` skeleton implementing `IWebViewAdapter` (excluded from default build)

## 6. Dependency injection integration

- [x] 6.1 Add dependency on `Microsoft.Extensions.DependencyInjection.Abstractions`
- [x] 6.2 Implement `AddAgibuildAvaloniaWebView(IServiceCollection, Func<IServiceProvider, IWebViewAdapter>)` extension method
- [x] 6.3 Register and validate resolving the adapter factory (NOT a shared adapter instance) in a unit test

## 7. TDD harness and contract tests

- [x] 7.1 Implement `MockWebViewAdapter` supporting last navigation tracking, configurable script results, and deterministic event raising
- [x] 7.2 Add contract test examples: navigation-start cancelable, script invocation returns configured value
- [x] 7.3 Add build verification: solution builds with default settings (current OS only) and tests pass
