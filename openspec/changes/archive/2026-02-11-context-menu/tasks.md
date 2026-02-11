# Tasks — context-menu

## 1. Core Contracts
- [x] Add `ContextMenuMediaType` enum to `WebViewContracts.cs`
- [x] Add `ContextMenuRequestedEventArgs` class to `WebViewContracts.cs`
- [x] Add `IContextMenuAdapter` facet interface to `IWebViewAdapter.cs`

## 2. Runtime Integration
- [x] Add `IContextMenuAdapter` facet detection in `WebViewCore` constructor
- [x] Subscribe to adapter's `ContextMenuRequested` and forward through `WebViewCore`
- [x] Marshal `ContextMenuRequested` to UI thread (same pattern as `DownloadRequested`)
- [x] Expose `ContextMenuRequested` event on `WebDialog`
- [x] Expose `ContextMenuRequested` event on `WebView`

## 3. Platform Adapters — macOS
- [x] Add `IContextMenuAdapter` to macOS adapter class declaration
- [x] Declare `ContextMenuRequested` event in macOS adapter
- [x] Add native context menu interception in `WkWebViewShim.mm` (deferred: event declared, native hook TBD)

## 4. Platform Adapters — Windows
- [x] Implement `IContextMenuAdapter` in `WindowsWebViewAdapter.cs`

## 5. Platform Adapters — iOS
- [x] Add `IContextMenuAdapter` to iOS adapter class declaration
- [x] Declare `ContextMenuRequested` event in iOS adapter
- [x] Add native context menu interception in `WkWebViewShim.iOS.mm` (deferred: event declared, native hook TBD)

## 6. Platform Adapters — Android
- [x] Implement `IContextMenuAdapter` in `AndroidWebViewAdapter.cs`

## 7. Platform Adapters — GTK
- [x] Implement `IContextMenuAdapter` in `GtkWebViewAdapter.cs`

## 8. Testing
- [x] Add `MockWebViewAdapterWithContextMenu` to `MockWebViewAdapter.cs`
- [x] Add contract tests: event forwarding, Handled suppression, hit-test fields
- [x] Add integration tests (headless): event via mock adapter, Handled propagation
- [x] Verify all tests pass
- [x] Verify coverage >= 90%
