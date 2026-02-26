# bridge-contracts — Design

**ROADMAP**: Phase 1, Deliverable 1.1a
**Architecture reference**: [design_doc.md](../../../docs/agibuild_webview_design_doc.md) §2 (Runtime layer)

## Overview

This change adds the Bridge contract surface to Core and a manual-registration runtime implementation. It does NOT include the Source Generator — that comes in deliverable 1.1b. The manual runtime validates all semantics so that the Source Generator only needs to emit code conforming to these already-tested contracts.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│  Consumer Code                                           │
│                                                          │
│  [JsExport]                                              │
│  interface IAppService { Task<User> GetUser(int id); }   │
│                                                          │
│  webView.Bridge.Expose<IAppService>(new AppService());   │
│  var ui = webView.Bridge.GetProxy<IUiController>();      │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│  IBridgeService (Core)                                   │
│  ├─ Expose<T>(impl, options?)                            │
│  ├─ GetProxy<T>()                                        │
│  └─ Remove<T>()                                          │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│  RuntimeBridgeService (Runtime)                          │
│  ├─ Validates [JsExport]/[JsImport] on T                │
│  ├─ Expose: reflects interface methods → registers       │
│  │   rpc.Handle("{Name}.{camelMethod}", handler)         │
│  ├─ Expose: injects JS client stub via InvokeScriptAsync│
│  ├─ GetProxy: creates DispatchProxy<T> that calls        │
│  │   rpc.InvokeAsync("{Name}.{camelMethod}", params)     │
│  └─ Remove: unregisters handlers                         │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│  IWebViewRpcService (existing F6)                        │
│  JSON-RPC 2.0 over WebMessage bridge                     │
└─────────────────────────────────────────────────────────┘
```

## Key Design Details

### 1. Attribute Definitions (Core)

```csharp
namespace Agibuild.Fulora;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public sealed class JsExportAttribute : Attribute
{
    /// <summary>
    /// Custom service name. Default: interface name without leading "I".
    /// </summary>
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public sealed class JsImportAttribute : Attribute
{
    public string? Name { get; set; }
}
```

### 2. IBridgeService (Core)

```csharp
public interface IBridgeService
{
    void Expose<T>(T implementation, BridgeOptions? options = null) where T : class;
    T GetProxy<T>() where T : class;
    void Remove<T>() where T : class;
}

public sealed class BridgeOptions
{
    /// <summary>
    /// Origin allowlist override. Null = inherit from WebMessageBridgeOptions.
    /// </summary>
    public IReadOnlySet<string>? AllowedOrigins { get; init; }
}
```

### 3. RuntimeBridgeService (Runtime)

**V1 uses reflection** for method discovery and `DispatchProxy` for JS→C# proxies. This is acceptable because:
- V1 is the contract validation layer (Source Generator replaces reflection in 1.1b)
- `DispatchProxy` is the standard .NET mechanism for interface proxying
- All tests validate behavior, not implementation strategy

**Method name derivation**:
- Service name: `JsExportAttribute.Name ?? interfaceName.TrimStart('I')`
- Method name: `char.ToLowerInvariant(methodName[0]) + methodName[1..]`
- Full RPC method: `"{serviceName}.{methodName}"`

**Parameter serialization**:
- Named params as JSON object: `{ "paramName1": value1, "paramName2": value2 }`
- Deserialization via `System.Text.Json` with default options
- Return value serialized via `JsonSerializer.SerializeToElement()`

**JS stub generation** (per Expose call):
```javascript
(function() {
    if (!window.agWebView) window.agWebView = {};
    if (!window.agWebView.bridge) window.agWebView.bridge = {};
    window.agWebView.bridge.AppService = {
        getUser: function(params) {
            return window.agWebView.rpc.invoke('AppService.getUser', params);
        }
    };
})();
```

### 4. WebViewCore Integration

```csharp
// In WebViewCore:
private RuntimeBridgeService? _bridgeService;

public IBridgeService Bridge
{
    get
    {
        ThrowIfDisposed();
        if (_bridgeService is null)
        {
            // Auto-enable bridge with defaults
            if (!_webMessageBridgeEnabled)
                EnableWebMessageBridge(new WebMessageBridgeOptions());
            _bridgeService = new RuntimeBridgeService(_rpcService!, ...);
        }
        return _bridgeService;
    }
}
```

Note: `Bridge` property is **non-null** (auto-creates on access). This differs from `Rpc` which is null until enabled. The `Bridge` property is the "easy path" that handles initialization automatically.

### 5. Disposal

On `WebViewCore.Dispose()`:
- Unregister all exposed service handlers
- Set `_bridgeService` to disposed state
- Subsequent calls throw `ObjectDisposedException`

## Testing Strategy

**All CT (Contract Tests)** — no real browser needed:

| Category | Tests | Method |
|----------|-------|--------|
| Attribute validation | `Expose<T>` without `[JsExport]` throws | MockAdapter + WebViewCore |
| Handler registration | Exposed method callable via simulated RPC message | MockAdapter + `TriggerWebMessageReceived` |
| Parameter deserialization | Named params correctly mapped to method args | MockAdapter |
| Return serialization | Method return value serialized in JSON-RPC response | MockAdapter + captured `InvokeScriptAsync` |
| JS stub injection | `Expose<T>()` calls `InvokeScriptAsync` with stub | MockAdapter captures script |
| GetProxy routing | Proxy method calls `InvokeScriptAsync` with correct RPC request | MockAdapter |
| Remove / re-expose | Remove then re-expose works; removed service returns -32601 | MockAdapter |
| Lifecycle | Operations after dispose throw `ObjectDisposedException` | MockAdapter |
| Auto-enable | `Bridge` access auto-enables WebMessage bridge | MockAdapter + verify RPC created |
| Duplicate expose | Second `Expose<T>()` for same T throws | MockAdapter |
| camelCase naming | `GetCurrentUser` → `"AppService.getCurrentUser"` | MockAdapter |
| Custom name | `[JsExport(Name = "api")]` → `"api.getCurrentUser"` | MockAdapter |

## Files Changed

| File | Change |
|------|--------|
| `src/Agibuild.Fulora.Core/JsExportAttribute.cs` | NEW |
| `src/Agibuild.Fulora.Core/JsImportAttribute.cs` | NEW |
| `src/Agibuild.Fulora.Core/IBridgeService.cs` | NEW |
| `src/Agibuild.Fulora.Runtime/RuntimeBridgeService.cs` | NEW |
| `src/Agibuild.Fulora.Runtime/WebViewCore.cs` | MODIFIED — add `Bridge` property |
| `src/Agibuild.Fulora/WebView.cs` | MODIFIED — expose `Bridge` |
| `tests/Agibuild.Fulora.UnitTests/BridgeContractTests.cs` | NEW |
| `tests/Agibuild.Fulora.Testing/MockWebViewAdapter.cs` | MODIFIED if needed |
