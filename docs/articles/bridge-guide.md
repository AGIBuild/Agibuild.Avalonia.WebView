# Bridge Guide

Typed bridge is the foundation of this project's framework-positioning architecture.
It keeps host/web interaction explicit, testable, and automation-friendly.

## Why It Matters in Phase 5

Bridge contracts are the entry point for:

- type-safe C# <-> JavaScript collaboration
- deterministic runtime behavior over string-based IPC
- policy and diagnostics integration for production governance

## Core Concepts

| Concept | Description |
|---------|-------------|
| `[JsExport]` | Exposes a C# interface implementation to JavaScript |
| `[JsImport]` | Declares a JavaScript-facing contract callable from C# |
| `IBridgeService` | Core API: `Expose<T>()`, `GetProxy<T>()`, `Remove<T>()` |
| Source Generator | Compile-time proxy/stub generation for AOT-safe interop |
| `BridgeOptions` | Security/governance knobs such as rate limiting and origin restrictions |

## Contract-First Flow

### 1) Define and expose C# service (`[JsExport]`)

```csharp
[JsExport(Name = "Calculator")]
public interface ICalculatorService
{
    Task<double> Add(double a, double b);
    Task<double> Multiply(double a, double b);
}

public sealed class CalculatorService : ICalculatorService
{
    public Task<double> Add(double a, double b) => Task.FromResult(a + b);
    public Task<double> Multiply(double a, double b) => Task.FromResult(a * b);
}

webView.Bridge.Expose<ICalculatorService>(new CalculatorService());
```

### 2) Call from JavaScript

```javascript
const sum = await window.agWebView.rpc.invoke("Calculator.add", { a: 3, b: 4 });
```

### 3) Call JavaScript service from C# (`[JsImport]`)

```csharp
[JsImport(Name = "Ui")]
public interface IUiService
{
    Task ShowToast(string message, int durationMs);
    Task<bool> Confirm(string question);
}

var ui = webView.Bridge.GetProxy<IUiService>();
var ok = await ui.Confirm("Delete this item?");
```

JavaScript handler registration example:

```javascript
window.agWebView.rpc.handle("Ui.confirm", async (params) => {
  return window.confirm(params.question);
});
```

## Policy and Rate Limiting

Bridge calls should be policy-governed and bounded.

```csharp
webView.Bridge.Expose<ICalculatorService>(
    new CalculatorService(),
    new BridgeOptions
    {
        AllowedOrigins = ["app://localhost"],
        RateLimit = new RateLimit(maxCalls: 100, window: TimeSpan.FromSeconds(10))
    });
```

When limits are exceeded, calls fail deterministically with JSON-RPC error code `-32029`.

## Diagnostics and Tracing

Use tracing to make bridge behavior machine-checkable in automation:

```csharp
var tracer = new LoggingBridgeTracer(logger);
// Register via DI/runtime configuration
```

Custom tracer sketch:

```csharp
public sealed class MyBridgeTracer : IBridgeTracer
{
    public void OnExportCallStart(string service, string method, string? paramsJson)
        => Console.WriteLine($"bridge:start {service}.{method}");

    public void OnExportCallEnd(string service, string method, bool success, string? error)
        => Console.WriteLine($"bridge:end {service}.{method} success={success}");
}
```

## Testing Strategy

`MockBridgeService` lets you validate bridge behavior without a real WebView runtime:

```csharp
var mock = new MockBridgeService();

mock.Expose<ICalculatorService>(new CalculatorService());
Assert.True(mock.WasExposed<ICalculatorService>());

mock.SetupProxy<IUiService>(new FakeUiService());
var proxy = mock.GetProxy<IUiService>();
```

## Source Generator Outputs

The generator emits:

- `*BridgeRegistration` for `[JsExport]` contracts
- `*BridgeProxy` for `[JsImport]` contracts
- Type declaration artifacts for JavaScript/TypeScript consumption
- AOT-safe JSON serialization helpers

Analyzer package reference:

```xml
<PackageReference Include="Agibuild.Fulora.Bridge.Generator"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

## Related Documents

- [Getting Started](./getting-started.md)
- [Architecture](./architecture.md)
- [SPA Hosting](./spa-hosting.md)
- [Roadmap](../../openspec/ROADMAP.md)
