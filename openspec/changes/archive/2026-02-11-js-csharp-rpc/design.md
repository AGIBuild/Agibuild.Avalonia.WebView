# JS ↔ C# RPC — Design

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│                      JS Side                              │
│                                                          │
│  const result = await window.agWebView.rpc              │
│      .invoke("calculator.add", { a: 1, b: 2 });        │
│                                                          │
│  // Register JS handler callable from C#:               │
│  window.agWebView.rpc.handle("ui.getTheme", () => {    │
│      return document.body.dataset.theme;                │
│  });                                                     │
└──────────────┬───────────────────────────┬───────────────┘
               │ WebMessage (JSON)          │
               ▼                            ▲
┌──────────────────────────────────────────────────────────┐
│                    WebMessage Bridge                       │
│              (existing infrastructure)                     │
└──────────────┬───────────────────────────┬───────────────┘
               │                            │
               ▼                            ▲
┌──────────────────────────────────────────────────────────┐
│                      C# Side                              │
│                                                          │
│  webView.Rpc.Handle("calculator.add", (args) => {       │
│      var a = args["a"].GetInt32();                       │
│      var b = args["b"].GetInt32();                       │
│      return a + b;                                       │
│  });                                                     │
│                                                          │
│  // Call JS from C#:                                     │
│  var theme = await webView.Rpc.InvokeAsync<string>(     │
│      "ui.getTheme");                                     │
└──────────────────────────────────────────────────────────┘
```

## Protocol (JSON over WebMessage)

```json
// Request (JS→C# or C#→JS):
{
  "jsonrpc": "2.0",
  "id": "uuid",
  "method": "calculator.add",
  "params": { "a": 1, "b": 2 }
}

// Success response:
{
  "jsonrpc": "2.0",
  "id": "uuid",
  "result": 3
}

// Error response:
{
  "jsonrpc": "2.0",
  "id": "uuid",
  "error": { "code": -32603, "message": "Division by zero" }
}
```

## Core Contracts

```csharp
public interface IWebViewRpcService
{
    // Register C# handler callable from JS
    void Handle(string method, Func<JsonElement?, Task<object?>> handler);
    void Handle(string method, Func<JsonElement?, object?> handler);
    void RemoveHandler(string method);

    // Call JS handler from C#
    Task<JsonElement> InvokeAsync(string method, object? args = null);
    Task<T?> InvokeAsync<T>(string method, object? args = null);
}
```

## Design Decisions

1. **JSON-RPC 2.0 protocol** — Industry standard, well-understood, minimal overhead.
2. **Built on existing WebMessage bridge** — No native changes needed. RPC is a pure C#/JS layer.
3. **Auto-inject JS runtime** — The RPC JS stub is injected automatically when `Rpc` is first accessed or when `EnableWebMessageBridge` is called.
4. **System.Text.Json** — Use STJ for serialization, avoiding external dependencies.
5. **Namespace-style methods** — `"calculator.add"` style naming for organization.
6. **Bidirectional** — Both JS→C# and C#→JS calls supported with the same protocol.
7. **Pending call tracking** — `ConcurrentDictionary<string, TaskCompletionSource>` for correlating responses.
