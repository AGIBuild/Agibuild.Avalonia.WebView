## Why

We need a contract-first, testable WebView replacement with API parity to Avalonia.Controls.WebView.
Starting with a clear project structure and core contracts enables TDD, reduces cross-platform risk, and keeps platform implementations decoupled.

## What Changes

- Create the foundational solution/project layout for core, adapters, services, and tests.
- Define core contracts and event args for IWebView, IWebDialog, IWebAuthBroker, environment options, and native handles.
- Introduce adapter abstractions and DI registration points for platform-specific implementations.
- Add a TDD-focused testing harness with mock adapters and contract test scaffolding.
- Create platform adapter project skeletons for Windows, macOS, Android, and Gtk (no runtime integrations yet).
- Add a dedicated DependencyInjection project for DI registration glue.

## Capabilities

### New Capabilities
- `webview-core-contracts`: Core public API surface, events, navigation, script invocation, and native handle access.
- `webview-adapter-abstraction`: Adapter interface, lifecycle contracts, and platform injection points.
- `webview-platform-skeletons`: Platform adapter project scaffolds for Windows, macOS, Android, and Gtk.
- `webview-testing-harness`: Contract tests, mock adapters, and event stubs for TDD.
- `webview-di-integration`: DI integration project and registration entry points.

### Modified Capabilities
- (none)

## Impact

- New projects and namespaces under the WebView solution.
- New public API contracts that downstream consumers will depend on.
- Test infrastructure added to validate contract behavior early.
