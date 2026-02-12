# Getting Started

## Prerequisites

- .NET 10 SDK
- Platform-specific WebView runtime:
  - **Windows**: WebView2 (auto-installs with Edge)
  - **macOS/iOS**: WKWebView (built-in)
  - **Android**: Android WebView (built-in)
  - **Linux**: WebKitGTK (`libwebkit2gtk-4.1`)

## Quick Start (Template)

```bash
# Install the template (once)
dotnet new install Agibuild.Avalonia.WebView.Templates

# Create a new hybrid app
dotnet new agibuild-hybrid -n MyApp

# Run
cd MyApp
dotnet run --project MyApp.Desktop
```

## Manual Setup

### 1. Create an Avalonia Desktop project

```bash
dotnet new avalonia.app -n MyApp
cd MyApp
```

### 2. Add NuGet packages

```bash
dotnet add package Agibuild.Avalonia.WebView
```

### 3. Add WebView to your window

```xml
<!-- MainWindow.axaml -->
<Window xmlns:wv="clr-namespace:Agibuild.Avalonia.WebView;assembly=Agibuild.Avalonia.WebView">
    <wv:WebView x:Name="WebView" />
</Window>
```

### 4. Navigate to a URL

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

## Type-Safe Bridge

### Define bridge interfaces

Create a shared project with your bridge contracts:

```csharp
// IGreeterService.cs
[JsExport]  // C# → JS
public interface IGreeterService
{
    Task<string> Greet(string name);
}

[JsImport]  // JS → C#
public interface INotificationService
{
    Task ShowNotification(string message);
}
```

### Expose a C# service to JavaScript

```csharp
// In your window code-behind
WebView.Bridge.Expose<IGreeterService>(new GreeterServiceImpl());
```

### Call from JavaScript

```javascript
// In your web page
const result = await window.agWebView.rpc.invoke('GreeterService.Greet', { name: 'World' });
console.log(result); // "Hello, World!"
```

### Call JavaScript from C#

```csharp
var notifier = WebView.Bridge.GetProxy<INotificationService>();
await notifier.ShowNotification("Hello from C#!");
```

## SPA Hosting

Serve your React/Vue/vanilla frontend from embedded resources:

```csharp
WebView.EnableSpaHosting(new SpaHostingOptions
{
    EmbeddedResourcePrefix = "wwwroot",
    ResourceAssembly = typeof(MainWindow).Assembly,
});

await WebView.NavigateAsync(new Uri("app://localhost/index.html"));
```

During development, proxy to your dev server for HMR:

```csharp
WebView.EnableSpaHosting(new SpaHostingOptions
{
    DevServerUrl = "http://localhost:5173",
});
```

## Unit Testing with MockBridge

```csharp
[Fact]
public async Task ViewModel_exposes_greeter()
{
    var mock = new MockBridgeService();
    var vm = new MainViewModel(mock);

    Assert.True(mock.WasExposed<IGreeterService>());
}
```

## Next Steps

- [Bridge Guide](bridge-guide.md) — Advanced bridge patterns
- [SPA Hosting](spa-hosting.md) — Detailed SPA hosting configuration
- [Architecture](architecture.md) — Framework internals
- [API Reference](../api/index.md) — Full API documentation
