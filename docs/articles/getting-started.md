# Getting Started

Get a hybrid desktop app running in under five minutes — a native Avalonia window hosting your web frontend, with type-safe bridge calls between C# and JavaScript.

## What you'll end up with

A desktop application where:

- The **UI** is a React (or Vue, or vanilla) SPA running inside a native WebView
- The **backend** is C# code running in the same process — no HTTP server, no REST API
- Communication is through **typed bridge contracts** that are code-generated at compile time

```
┌───────────────────────────────────┐
│  Native Avalonia Window           │
│  ┌─────────────────────────────┐  │
│  │  React SPA in WebView       │  │
│  │                             │  │
│  │  await GreeterService       │  │
│  │    .greet("World")          │  │
│  └──────────┬──────────────────┘  │
│             │ type-safe bridge     │
│  ┌──────────▼──────────────────┐  │
│  │  C# GreeterServiceImpl     │  │
│  │  → "Hello, World!"         │  │
│  └─────────────────────────────┘  │
└───────────────────────────────────┘
```

## Prerequisites

- .NET 10 SDK
- Platform WebView runtime:
  - **Windows**: WebView2 (ships with Edge)
  - **macOS/iOS**: WKWebView (built-in)
  - **Android**: Android WebView (built-in)
  - **Linux**: WebKitGTK (`libwebkit2gtk-4.1`)

## Quick Path: Use the CLI Template

The fastest way to start. The CLI scaffolds a project with bridge contracts, SPA hosting, and dev tooling pre-configured.

```bash
# Install CLI and template (once)
dotnet tool install -g Agibuild.Fulora.Cli
dotnet new install Agibuild.Fulora.Templates

# Create app with React frontend
fulora new MyApp --frontend react

# Start both Vite and Avalonia together
cd MyApp
fulora dev
```

You'll see a native window open with your React app inside, bridge-connected and ready for development.

Alternatively, use `dotnet new` directly:

```bash
dotnet new agibuild-hybrid -n MyApp
cd MyApp
dotnet run --project MyApp.Desktop
```

## Manual Path: Add Fulora to an Existing Avalonia App

If you already have an Avalonia project or need full control over the setup.

### 1. Add the NuGet package

```bash
dotnet add package Agibuild.Fulora.Avalonia
```

### 2. Add the WebView control to your window

```xml
<!-- MainWindow.axaml -->
<Window xmlns:wv="clr-namespace:Agibuild.Fulora;assembly=Agibuild.Fulora">
    <wv:WebView x:Name="WebView" />
</Window>
```

### 3. Navigate to a page

```csharp
public MainWindow()
{
    InitializeComponent();
    Loaded += async (_, _) =>
    {
        await WebView.NavigateAsync(new Uri("https://example.com"));
    };
}
```

## Your First Bridge Contract

This is where Fulora's value becomes clear. Define a C# interface, and the source generator creates everything needed for type-safe cross-language calls — no serialization boilerplate, no runtime reflection.

```csharp
[JsExport] // C# implementation, callable from JavaScript
public interface IGreeterService
{
    Task<string> Greet(string name);
}

[JsImport] // JavaScript implementation, callable from C#
public interface INotificationService
{
    Task ShowNotification(string message);
}
```

Expose your C# service to the web frontend:

```csharp
WebView.Bridge.Expose<IGreeterService>(new GreeterServiceImpl());
```

Call it from JavaScript — the bridge client is auto-generated:

```javascript
const result = await GreeterService.greet("World");
// → "Hello, World!"
```

Call JavaScript from C# with the same type safety:

```csharp
var notifier = WebView.Bridge.GetProxy<INotificationService>();
await notifier.ShowNotification("Hello from C#!");
```

## SPA Hosting

Fulora can serve your web frontend from embedded resources (production) or proxy to a dev server (development).

**Production** — embedded assets with `app://` scheme:

```csharp
WebView.EnableSpaHosting(new SpaHostingOptions
{
    EmbeddedResourcePrefix = "wwwroot",
    ResourceAssembly = typeof(MainWindow).Assembly,
});

await WebView.NavigateAsync(new Uri("app://localhost/index.html"));
```

**Development** — Vite/Webpack dev server with HMR:

```csharp
WebView.EnableSpaHosting(new SpaHostingOptions
{
    DevServerUrl = "http://localhost:5173",
});
```

## Next Steps

Now that you have a running app, dive deeper:

- [Bridge Guide](bridge-guide.md) — Advanced patterns: streaming, cancellation, error handling, batch calls
- [SPA Hosting](spa-hosting.md) — Production hosting, dev server proxy, HMR state preservation
- [Architecture](architecture.md) — How the runtime, policy engine, and capability gateway work together
- [Plugin Authoring](../plugin-authoring-guide.md) — Create bridge plugins that ship as NuGet + npm
- [CLI Reference](../cli.md) — `fulora new`, `dev`, `generate types`, `add service`, `search`
- [Demo Walkthrough](../demo/index.md) — A full-featured sample with Dashboard, Chat, Files, and Settings
