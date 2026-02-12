<p align="center">
  <img src="https://img.shields.io/nuget/v/Agibuild.Avalonia.WebView?logo=nuget&label=NuGet&color=004880&style=flat-square" />
  <img src="https://img.shields.io/nuget/dt/Agibuild.Avalonia.WebView?logo=nuget&label=Downloads&color=00a86b&style=flat-square" />
  <a href="https://github.com/AGIBuild/Agibuild.Avalonia.WebView/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/AGIBuild/Agibuild.Avalonia.WebView/ci.yml?label=CI&logo=github&style=flat-square" /></a>
  <img src="https://img.shields.io/badge/License-MIT-yellow?style=flat-square" />
</p>

<h1 align="center">Agibuild.Avalonia.WebView</h1>

<p align="center">
  <strong>One package. Five platforms. Native WebView in Avalonia — with a type-safe C# &harr; JS bridge.</strong>
</p>

---

## The Problem

Building hybrid desktop/mobile apps with Avalonia? You'll quickly hit these walls:

- **No built-in WebView** &mdash; Avalonia doesn't ship one.
- **Platform fragmentation** &mdash; WebView2 on Windows, WKWebView on macOS/iOS, WebKitGTK on Linux, Android WebView... each has a different API.
- **JS interop is painful** &mdash; Passing strings through `InvokeScriptAsync` and parsing `WebMessageReceived` gets messy fast.
- **SPA hosting is DIY** &mdash; Serving your React/Vue/Svelte app from embedded resources requires custom plumbing.

## The Solution

**One NuGet package.** Drop it in, call `.UseAgibuildWebView()`, and you get a native WebView on every platform &mdash; plus a type-safe bridge that lets C# and JavaScript call each other like they're in the same process.

```
dotnet add package Agibuild.Avalonia.WebView
```

| Platform | Engine | Status |
|----------|--------|--------|
| macOS | WKWebView | Preview |
| Windows | WebView2 | Preview |
| Linux | WebKitGTK | Preview |
| iOS | WKWebView | Preview |
| Android | Android WebView | Preview |

---

## 30 Seconds to Your First WebView

**1. Initialize** (one line in `Program.cs`):

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UseAgibuildWebView()   // <-- this is it
    .StartWithClassicDesktopLifetime(args);
```

**2. Add the control** (XAML):

```xml
<Window xmlns:wv="using:Agibuild.Avalonia.WebView">
    <wv:WebView x:Name="WebView" Source="https://example.com" />
</Window>
```

**That's it.** You have a native WebView running on macOS, Windows, Linux, iOS, and Android.

---

## The Bridge: Type-Safe C# &harr; JS

This is where it gets interesting. No more string-based messaging. Define a C# interface, and it just works on both sides.

### Expose C# to JavaScript

```csharp
// 1. Define a contract
[JsExport]
public interface IGreeterService
{
    Task<string> Greet(string name);
}

// 2. Implement it
public class GreeterService : IGreeterService
{
    public Task<string> Greet(string name)
        => Task.FromResult($"Hello, {name}!");
}

// 3. Expose it (one line)
webView.Bridge.Expose<IGreeterService>(new GreeterService());
```

Now JavaScript can call it directly:

```javascript
const msg = await window.agWebView.rpc.invoke('GreeterService.greet', { name: 'World' });
// => "Hello, World!"
```

### Call JavaScript from C#

```csharp
[JsImport]
public interface INotificationService
{
    Task ShowNotification(string message);
}

// Get a typed proxy — calls are forwarded to JS via RPC
var notifications = webView.Bridge.GetProxy<INotificationService>();
await notifications.ShowNotification("File saved!");
```

JavaScript registers the handler:

```javascript
window.agWebView.rpc.handle('NotificationService.showNotification', (params) => {
    showToast(params.message);
});
```

The bridge is powered by JSON-RPC 2.0 under the hood. A Roslyn source generator produces AOT-safe, reflection-free proxy code at compile time.

---

## SPA Hosting: Ship Your Frontend Inside the App

Embed your React / Vue / Svelte / vanilla build output as resources. No external web server needed.

```csharp
webView.EnableSpaHosting(new SpaHostingOptions
{
    EmbeddedResourcePrefix = "wwwroot",
    ResourceAssembly = typeof(MainWindow).Assembly,
});

await webView.NavigateAsync(new Uri("app://localhost/index.html"));
```

During development, proxy to your dev server with HMR:

```csharp
webView.EnableSpaHosting(new SpaHostingOptions
{
    DevServerUrl = "http://localhost:5173",  // Vite, webpack, etc.
});
```

The custom `app://` scheme handles MIME types, SPA fallback routing, immutable asset caching, and default security headers automatically.

---

## More Capabilities

<details>
<summary><strong>OAuth / Web Authentication</strong></summary>

```csharp
var broker = serviceProvider.GetRequiredService<IWebAuthBroker>();
var result = await broker.AuthenticateAsync(window, new AuthOptions
{
    AuthorizeUri = new Uri("https://provider.com/oauth/authorize?..."),
    CallbackUri  = new Uri("myapp://callback"),
    Timeout      = TimeSpan.FromMinutes(5),
});

if (result.Status == WebAuthStatus.Success)
{
    // result.CallbackUri contains the tokens
}
```
</details>

<details>
<summary><strong>Web Dialog (Popup Window)</strong></summary>

```csharp
var dialog = factory.Create();
dialog.Title = "Sign In";
dialog.Resize(800, 600);
dialog.Show();
await dialog.NavigateAsync(new Uri("https://example.com/login"));
```
</details>

<details>
<summary><strong>Screenshot & PDF Export</strong></summary>

```csharp
byte[] png = await webView.CaptureScreenshotAsync();
byte[] pdf = await webView.PrintToPdfAsync(new PdfPrintOptions { Landscape = true });
```
</details>

<details>
<summary><strong>Cookie Management</strong></summary>

```csharp
var cookies = webView.TryGetCookieManager();
var all = await cookies!.GetCookiesAsync(new Uri("https://example.com"));
await cookies.ClearAllCookiesAsync();
```
</details>

<details>
<summary><strong>Clipboard / Command Manager</strong></summary>

```csharp
var cmd = webView.TryGetCommandManager();
cmd?.Copy(); cmd?.Paste(); cmd?.SelectAll(); cmd?.Undo(); cmd?.Redo();
```
</details>

<details>
<summary><strong>Environment Options</strong></summary>

```csharp
WebViewEnvironment.Initialize(loggerFactory, new WebViewEnvironmentOptions
{
    EnableDevTools    = true,
    CustomUserAgent   = "MyApp/1.0",
    UseEphemeralSession = false,
});
```
</details>

<details>
<summary><strong>Dependency Injection</strong></summary>

```csharp
var services = new ServiceCollection();
services.AddWebView();
services.AddWebViewDialogServices(); // dialogs + OAuth

var provider = services.BuildServiceProvider();
provider.UseAgibuildWebView();
```
</details>

---

## Architecture at a Glance

```
┌──────────────────────────────────────────────┐
│              Your Avalonia App                │
│                                              │
│  ┌──────────┐  ┌────────┐  ┌─────────────┐  │
│  │ WebView  │  │ Bridge │  │ SPA Hosting │  │
│  │ Control  │  │C# ↔ JS │  │ app://      │  │
│  └────┬─────┘  └───┬────┘  └──────┬──────┘  │
│       │            │               │         │
│  ┌────┴────────────┴───────────────┴──────┐  │
│  │          WebViewCore (Runtime)         │  │
│  │   Navigation · RPC · Events · Policy  │  │
│  └────────────────┬──────────────────────┘  │
│                   │ IWebViewAdapter          │
│  ┌────────────────┴──────────────────────┐  │
│  │     Platform Adapter (auto-selected)   │  │
│  │  WKWebView│WebView2│WebKitGTK│Android │  │
│  └────────────────────────────────────────┘  │
└──────────────────────────────────────────────┘
```

---

## Project Template

Scaffold a hybrid app with the built-in template:

```bash
dotnet new install Agibuild.Avalonia.WebView.Templates
dotnet new agibuild-hybrid -n MyApp
cd MyApp && dotnet run --project MyApp.Desktop
```

---

## Testing

| Metric | Value |
|--------|-------|
| Unit tests | 626 |
| Integration tests | 112 |
| Line coverage | **95.4%** |
| Branch coverage | **88.2%** |
| Method coverage | **97.7%** |

```bash
nuke Test            # Unit + Integration (738 tests)
nuke Coverage        # Coverage report + threshold enforcement
nuke NugetPackageTest  # Pack → install → run smoke test
nuke TemplateE2E     # Template end-to-end test
```

---

## License

[MIT](LICENSE.txt)
