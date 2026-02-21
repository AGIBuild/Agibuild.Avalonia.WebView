# Getting Started

Build your first **Electron-replacement-ready** hybrid app with Avalonia + web UI.

This guide follows the current product direction (Roadmap Phase 5):

- typed bridge contracts
- typed capability gateway
- policy-first runtime behavior
- automation-friendly diagnostics
- web-first template architecture

## Prerequisites

- .NET 10 SDK
- Platform runtime:
  - **Windows**: WebView2 (usually installed with Edge)
  - **macOS/iOS**: WKWebView (built-in)
  - **Android**: Android WebView (built-in)
  - **Linux**: WebKitGTK (`libwebkit2gtk-4.1`)

## Recommended Path: Template Workflow

Use this path for most teams. It matches the recommended architecture with minimal host glue.

```bash
# Install template (once)
dotnet new install Agibuild.Avalonia.WebView.Templates

# Create app
dotnet new agibuild-hybrid -n MyApp

# Run desktop host
cd MyApp
dotnet run --project MyApp.Desktop
```

What you get immediately:

- ready-to-run host + web structure
- typed bridge contract wiring
- web-first development flow
- production-oriented project layout

## Manual Path: Build from Scratch

Use this when you need full control over project composition.

### 1) Create an Avalonia app

```bash
dotnet new avalonia.app -n MyApp
cd MyApp
```

### 2) Add package

```bash
dotnet add package Agibuild.Avalonia.WebView
```

### 3) Add WebView control

```xml
<!-- MainWindow.axaml -->
<Window xmlns:wv="clr-namespace:Agibuild.Avalonia.WebView;assembly=Agibuild.Avalonia.WebView">
    <wv:WebView x:Name="WebView" />
</Window>
```

### 4) Navigate to a page

```csharp
// MainWindow.axaml.cs
public MainWindow()
{
    InitializeComponent();
    Loaded += async (_, _) =>
    {
        await WebView.NavigateAsync(new Uri("https://example.com"));
    };
}
```

## First Typed Bridge Contract

Define contracts once, then call across C# and JavaScript with type safety.

```csharp
[JsExport] // C# -> JS
public interface IGreeterService
{
    Task<string> Greet(string name);
}

[JsImport] // JS -> C#
public interface INotificationService
{
    Task ShowNotification(string message);
}
```

Expose C# service:

```csharp
WebView.Bridge.Expose<IGreeterService>(new GreeterServiceImpl());
```

Call from JavaScript:

```javascript
const result = await window.agWebView.rpc.invoke("GreeterService.greet", { name: "World" });
console.log(result);
```

Call JavaScript from C#:

```csharp
var notifier = WebView.Bridge.GetProxy<INotificationService>();
await notifier.ShowNotification("Hello from C#!");
```

## First Web-First SPA Hosting Setup

Production mode (embedded assets):

```csharp
WebView.EnableSpaHosting(new SpaHostingOptions
{
    EmbeddedResourcePrefix = "wwwroot",
    ResourceAssembly = typeof(MainWindow).Assembly,
});

await WebView.NavigateAsync(new Uri("app://localhost/index.html"));
```

Development mode (HMR proxy):

```csharp
WebView.EnableSpaHosting(new SpaHostingOptions
{
    DevServerUrl = "http://localhost:5173",
});
```

## Next Steps

- [Architecture](architecture.md) — Runtime topology and Phase 5 invariants
- [Bridge Guide](bridge-guide.md) — Advanced bridge patterns
- [SPA Hosting](spa-hosting.md) — Detailed hosting configuration
- [Demo walkthrough](../demo/index.md) — End-to-end sample experience
- [Roadmap](../../openspec/ROADMAP.md) — Product direction and milestones
