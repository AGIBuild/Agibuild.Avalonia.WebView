<p align="center">
  <img src="https://img.shields.io/nuget/v/Agibuild.Avalonia.WebView?logo=nuget&label=NuGet&color=004880&style=flat-square" />
  <img src="https://img.shields.io/nuget/dt/Agibuild.Avalonia.WebView?logo=nuget&label=Downloads&color=00a86b&style=flat-square" />
  <a href="https://github.com/AGIBuild/Agibuild.Avalonia.WebView/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/AGIBuild/Agibuild.Avalonia.WebView/ci.yml?label=CI&logo=github&style=flat-square" /></a>
  <img src="https://img.shields.io/badge/License-MIT-yellow?style=flat-square" />
</p>

<h1 align="center">Agibuild.Avalonia.WebView</h1>

<p align="center">
  <strong>Electron-level web productivity. Avalonia-native performance, control, and security.</strong>
</p>

---

> **Product direction update**  
> This project is now driven by the **Electron Replacement Foundation** goal (Roadmap Phase 5).  
> We are building a **hybrid application platform**, not just a WebView control.

## Why This Exists

Teams migrating from Electron usually want two things at the same time:

1. Keep web development speed (React/Vue/Svelte, HMR, fast iteration)
2. Regain native footprint, stronger security boundaries, and deterministic host behavior

Traditional WebView wrappers often solve rendering, but still leave you to hand-build:

- string-based host/web IPC
- policy governance for host capabilities
- diagnosable runtime behavior for CI and automation
- scalable app-shell patterns across platforms

`Agibuild.Avalonia.WebView` targets that full gap.

## What This Is Now (Not Just a Control)

### 1) Typed bridge at the center
- `[JsExport]` / `[JsImport]` contracts
- source-generated C# and JS-facing proxies
- AOT-safe, reflection-free path

### 2) Typed capability gateway for host/system operations
- desktop/system capabilities converge into one typed entry model
- avoid scattered host API calls in app-layer code

### 3) Policy-first runtime semantics
- policy is evaluated before capability/provider execution
- deterministic outcomes: `allow` / `deny` / `failure`

### 4) Agent-friendly diagnostics
- machine-checkable diagnostics for critical runtime flows
- usable by CI, test automation, and AI agents

### 5) Web-first template flow
- starter path optimized for web-first hybrid desktop/mobile delivery
- minimal host glue, typed contracts preserved end-to-end

## Roadmap Alignment

| Phase | Theme | Status |
|---|---|---|
| Phase 0 | Foundation | ✅ Done |
| Phase 1 | Type-Safe Bridge | ✅ Done |
| Phase 2 | SPA Hosting | ✅ Core Done |
| Phase 3 | Polish & GA | ✅ Done |
| Phase 4 | Application Shell | ✅ Done |
| Phase 5 | Electron Replacement Foundation | ✅ Completed |

Read more:
- [Roadmap](openspec/ROADMAP.md)
- [Project Vision & Goals](openspec/PROJECT.md)
- [Electron Replacement Foundation spec](openspec/specs/electron-replacement-foundation/spec.md)

## Platform Coverage

| Platform | Engine | Status |
|----------|--------|--------|
| Windows | WebView2 | Preview |
| macOS | WKWebView | Preview |
| Linux | WebKitGTK | Preview |
| iOS | WKWebView | Preview |
| Android | Android WebView | Preview |

> Avalonia provides an official commercial WebView option.  
> This project focuses on an open, cross-platform, contract-driven hybrid app platform.

---

## 60-Second Start

Install package:

```bash
dotnet add package Agibuild.Avalonia.WebView
```

Enable in `Program.cs`:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UseAgibuildWebView()
    .StartWithClassicDesktopLifetime(args);
```

Add control in XAML:

```xml
<Window xmlns:wv="using:Agibuild.Avalonia.WebView">
    <wv:WebView x:Name="WebView" Source="https://example.com" />
</Window>
```

For full guides:
- [Getting Started](docs/articles/getting-started.md)
- [Architecture](docs/articles/architecture.md)
- [Documentation Index](docs/index.md)

---

## Typed Bridge in Practice

Expose C# service to JavaScript:

```csharp
[JsExport]
public interface IGreeterService
{
    Task<string> Greet(string name);
}

public sealed class GreeterService : IGreeterService
{
    public Task<string> Greet(string name) => Task.FromResult($"Hello, {name}!");
}

webView.Bridge.Expose<IGreeterService>(new GreeterService());
```

Call from JavaScript:

```javascript
const msg = await window.agWebView.rpc.invoke("GreeterService.greet", { name: "World" });
```

Call JavaScript from C#:

```csharp
[JsImport]
public interface INotificationService
{
    Task ShowNotification(string message);
}

var notifications = webView.Bridge.GetProxy<INotificationService>();
await notifications.ShowNotification("File saved!");
```

---

## Web-First SPA Hosting

Production (embedded assets):

```csharp
webView.EnableSpaHosting(new SpaHostingOptions
{
    EmbeddedResourcePrefix = "wwwroot",
    ResourceAssembly = typeof(MainWindow).Assembly,
});

await webView.NavigateAsync(new Uri("app://localhost/index.html"));
```

Development (HMR proxy):

```csharp
webView.EnableSpaHosting(new SpaHostingOptions
{
    DevServerUrl = "http://localhost:5173",  // Vite, webpack, etc.
});
```


---

## Demo: Avalonia + React

Sample project: [`samples/avalonia-react/`](samples/avalonia-react/)

### Dashboard
<p align="center"><img src="docs/demo/images/dashboard.jpg" width="720" /></p>

### Chat
<p align="center"><img src="docs/demo/images/chat.jpg" width="720" /></p>

### Settings
<p align="center"><img src="docs/demo/images/settings.jpg" width="720" /></p>

Deep dive:
- [Demo walkthrough](docs/demo/index.md)

---

## Capability Snapshot

- OAuth / Web authentication (`IWebAuthBroker`)
- Web dialog flows (`IWebDialog`)
- Screenshot and PDF export
- Cookie and command manager support
- Environment options (DevTools, User-Agent, session modes)
- Dependency injection integration

For detailed API usage, see:
- [Architecture notes](docs/articles/architecture.md)
- [Bridge guide](docs/articles/bridge-guide.md)
- [SPA hosting guide](docs/articles/spa-hosting.md)

---

## Architecture at a Glance

```
┌──────────────────────────────────────────────────────────┐
│                   Your Avalonia App                     │
│                                                          │
│  ┌───────────┐   ┌─────────────┐   ┌─────────────────┐  │
│  │ WebView   │   │ Typed Bridge│   │ Capability Gate │  │
│  │ Control   │   │ C# <-> JS   │   │ Host/System API │  │
│  └─────┬─────┘   └──────┬──────┘   └────────┬────────┘  │
│        │                │                    │           │
│  ┌─────┴────────────────┴────────────────────┴────────┐  │
│  │         Runtime Core (policy-first execution)      │  │
│  │  Navigation · RPC · Shell · Diagnostics · Policy   │  │
│  └──────────────────────────┬──────────────────────────┘  │
│                             │ IWebViewAdapter             │
│  ┌──────────────────────────┴──────────────────────────┐  │
│  │      WKWebView · WebView2 · WebKitGTK · Android    │  │
│  └─────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

---

## Template Workflow

```bash
dotnet new install Agibuild.Avalonia.WebView.Templates
dotnet new agibuild-hybrid -n MyApp
cd MyApp
dotnet run --project MyApp.Desktop
```

---

## Quality Signals

| Metric | Value |
|--------|-------|
| Unit tests | 766 |
| Integration tests | 149 |
| Line coverage | **95.87%** |
| Branch coverage | **84.8%** |
| Method coverage | **98.2%** |

```bash
nuke Test            # Unit + Integration (915 tests)
nuke Coverage        # Coverage report + threshold enforcement
nuke NugetPackageTest  # Pack → install → run smoke test
nuke TemplateE2E     # Template end-to-end test
```

## License

[MIT](LICENSE.txt)
