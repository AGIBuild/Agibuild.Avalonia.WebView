# Bridge Guide

## Overview

The Bridge provides type-safe, bidirectional C# ↔ JavaScript interop using `[JsExport]` and `[JsImport]` attributes with compile-time code generation.

## Concepts

| Concept | Description |
|---------|-------------|
| `[JsExport]` | Marks a C# interface whose implementation is exposed to JavaScript |
| `[JsImport]` | Marks a C# interface that represents a JavaScript service callable from C# |
| `IBridgeService` | Core service: `Expose<T>()`, `GetProxy<T>()`, `Remove<T>()` |
| Source Generator | Roslyn-based compile-time code gen for AOT compatibility |

## Exposing C# Services

```csharp
[JsExport(Name = "Calculator")]  // Custom JS name (optional)
public interface ICalculator
{
    Task<double> Add(double a, double b);
    Task<double> Multiply(double a, double b);
}

public class Calculator : ICalculator
{
    public Task<double> Add(double a, double b) => Task.FromResult(a + b);
    public Task<double> Multiply(double a, double b) => Task.FromResult(a * b);
}

// Expose
webView.Bridge.Expose<ICalculator>(new Calculator());
```

JavaScript calls:
```javascript
const sum = await window.agWebView.rpc.invoke('Calculator.Add', { a: 3, b: 4 });
```

## Calling JavaScript from C#

```csharp
[JsImport(Name = "UI")]
public interface IUiService
{
    Task ShowToast(string message, int durationMs);
    Task<bool> Confirm(string question);
}

// JavaScript must register handlers:
// window.agWebView.rpc.handle('UI.ShowToast', (params) => { ... });
// window.agWebView.rpc.handle('UI.Confirm', (params) => { ... });

var ui = webView.Bridge.GetProxy<IUiService>();
bool ok = await ui.Confirm("Delete this item?");
```

## Rate Limiting

```csharp
webView.Bridge.Expose<ICalculator>(new Calculator(), new BridgeOptions
{
    RateLimit = new RateLimit(maxCalls: 100, window: TimeSpan.FromSeconds(10))
});
```

Exceeding the limit returns JSON-RPC error code `-32029`.

## Tracing

```csharp
// Attach a tracer for debugging
var tracer = new LoggingBridgeTracer(logger);
// Pass to WebViewCore constructor or configure via DI
```

Custom tracer:
```csharp
public class MyTracer : IBridgeTracer
{
    public void OnExportCallStart(string svc, string method, string? paramsJson)
        => Console.WriteLine($"→ {svc}.{method}");
    // ... implement other methods
}
```

## Testing with MockBridgeService

```csharp
var mock = new MockBridgeService();

// Test that your ViewModel exposes the right service
mock.Expose<ICalculator>(new Calculator());
Assert.True(mock.WasExposed<ICalculator>());

// Test that your code calls the proxy correctly
mock.SetupProxy<IUiService>(new FakeUiService());
var proxy = mock.GetProxy<IUiService>();
```

## Source Generator

The Roslyn Source Generator produces:
- `*BridgeRegistration` classes for `[JsExport]` interfaces
- `*BridgeProxy` classes for `[JsImport]` interfaces
- `BridgeTypeScriptDeclarations` with `.d.ts` string constants
- `BridgeGeneratedJsonOptions` for AOT-safe serialization

Add to your `.csproj`:
```xml
<PackageReference Include="Agibuild.Avalonia.WebView.Bridge.Generator"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```
