## API Surface Review Checklist (Refactor Hardening)

### 1) Async-Boundary Status

- **Item:** Native handle access has an async-first path (`IWebView.TryGetWebViewHandleAsync`).
- **Status:** PASS
- **Evidence:**
  - Contract surface updated in `src/Agibuild.Avalonia.WebView.Core/WebViewContracts.cs`.
  - Runtime/control delegation implemented in:
    - `src/Agibuild.Avalonia.WebView.Runtime/WebViewCore.cs`
    - `src/Agibuild.Avalonia.WebView.Runtime/WebDialog.cs`
    - `src/Agibuild.Avalonia.WebView/WebView.cs`
    - `src/Agibuild.Avalonia.WebView/AvaloniaWebDialog.cs`
  - Validation tests:
    - `ContractSemanticsV1DispatcherMarshalingTests.TryGetWebViewHandleAsync_off_thread_dispatches_to_ui_thread`
    - `ContractSemanticsV1AdapterLifecycleEventsTests.TryGetWebViewHandleAsync_returns_null_after_adapter_destroyed`

### 2) Pre-Attach Event Semantics Verification

- **Item:** `ContextMenuRequested` subscribe/unsubscribe before attach is deterministic.
- **Status:** PASS
- **Evidence:**
  - Control-level buffering/replay in `src/Agibuild.Avalonia.WebView/WebView.cs` using retained handler field and bind/unbind on core lifecycle.
  - Verification tests:
    - `WebViewControlEventWiringIntegrationTests.ContextMenuRequested_subscribe_before_core_attach_is_replayed`
    - `WebViewControlEventWiringIntegrationTests.ContextMenuRequested_unsubscribe_before_core_attach_is_honored`

### 3) Blocking-Wait Audit Ownership

- **Item:** Production blocking waits are allowlisted with explicit owner and rationale.
- **Status:** PASS
- **Evidence:**
  - Governance test with owner/rationale metadata:
    - `tests/Agibuild.Avalonia.WebView.UnitTests/GetAwaiterGetResultUsageTests.cs`
  - Test-side guard to prevent unbounded spread:
    - `tests/Agibuild.Avalonia.WebView.UnitTests/TestGetAwaiterGetResultUsageTests.cs`
  - Current owners:
    - `WebViewCore` (sync compatibility wrapper boundary)
    - `WindowsWebViewAdapter` (native callback sync decision boundary)
    - `AndroidWebViewAdapter` (native callback / UI-thread boundary)
