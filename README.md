<p align="center">
  <img src="https://img.shields.io/nuget/v/Agibuild.Fulora.Avalonia?logo=nuget&label=NuGet&color=004880&style=flat-square" />
  <img src="https://img.shields.io/nuget/dt/Agibuild.Fulora.Avalonia?logo=nuget&label=Downloads&color=00a86b&style=flat-square" />
  <a href="https://github.com/AGIBuild/Agibuild.Fulora/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/AGIBuild/Agibuild.Fulora/ci.yml?label=CI&logo=github&style=flat-square" /></a>
  <img src="https://img.shields.io/badge/License-MIT-yellow?style=flat-square" />
</p>

<h1 align="center">Fulora</h1>

<p align="center">
  <strong>One runtime core for C# + Web. Build fast, stay native, scale confidently.</strong>
</p>

---

## Quick Start

**Recommended (CLI + template):**

```bash
dotnet tool install -g Agibuild.Fulora.Cli
dotnet new install Agibuild.Fulora.Templates
fulora new MyApp --frontend react
cd MyApp
fulora dev
```

**Alternative (template only):**

```bash
dotnet new install Agibuild.Fulora.Templates
dotnet new agibuild-hybrid -n MyApp
cd MyApp
dotnet run --project MyApp.Desktop
```

**Manual (add WebView to an existing Avalonia app):**

```bash
dotnet add package Agibuild.Fulora.Avalonia
```

In `Program.cs`:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UseAgibuildWebView()
    .StartWithClassicDesktopLifetime(args);
```

In XAML:

```xml
<Window xmlns:wv="clr-namespace:Agibuild.Fulora;assembly=Agibuild.Fulora">
    <wv:WebView x:Name="WebView"
                Source="https://example.com" />
</Window>
```

Full guide: [Getting Started](docs/articles/getting-started.md) · [Documentation Index](docs/index.md)

---

## Typed Bridge in Practice

Expose C# to JavaScript:

```csharp
[JsExport]
public interface IGreeterService
{
    Task<string> Greet(string name);
}

webView.Bridge.Expose<IGreeterService>(new GreeterService());
```

Call from JavaScript:

```javascript
const msg = await window.agWebView.rpc.invoke("GreeterService.greet", {
    name: "World"
});
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

**Production (embedded):**

```csharp
webView.EnableSpaHosting(new SpaHostingOptions
{
    EmbeddedResourcePrefix = "wwwroot",
    ResourceAssembly = typeof(MainWindow).Assembly,
});
await webView.NavigateAsync(new Uri("app://localhost/index.html"));
```

**Development (HMR):**

```csharp
webView.EnableSpaHosting(new SpaHostingOptions
{
    DevServerUrl = "http://localhost:5173"
});
```

---

## Demos & Samples

| Sample | Description |
|--------|-------------|
| [samples/avalonia-react](samples/avalonia-react/) | Avalonia + React (Vite), typed bridge, SPA hosting |
| [samples/avalonia-ai-chat](samples/avalonia-ai-chat/) | AI chat with `IAsyncEnumerable` streaming, cancellation, Microsoft.Extensions.AI |
| [samples/showcase-todo](samples/showcase-todo/) | Full-featured reference app (plugins, shell, CLI) |

Walkthrough: [Demo guide](docs/demo/index.md) · [AI Integration](docs/ai-integration-guide.md)

---

## Capability Snapshot

**Core**

- Typed bridge: `[JsExport]` / `[JsImport]`, source-generated C#/JS proxies, AOT-safe; V2: `byte[]`/`Uint8Array`, `CancellationToken`/`AbortSignal`, `IAsyncEnumerable` streaming, overloads
- Typed capability gateway for host/system operations (policy-first execution)
- SPA hosting: embedded assets, dev HMR proxy, shell activation, deep-link, SPA asset hot update with signature verification
- OAuth / Web auth (`IWebAuthBroker`), Web dialog (`IWebDialog`), screenshot & PDF, cookies, command manager
- DevTools, User-Agent, session modes, dependency injection

**Ecosystem**

- Official plugins: Database (SQLite), HTTP Client, File System, Notifications, Auth Token, Biometric, LocalStorage
- CLI: `fulora new`, `dev`, `generate`, `search`, `add plugin`, `list plugins --check`
- Telemetry: OpenTelemetry provider, Sentry crash reporting with bridge breadcrumbs
- AI: Microsoft.Extensions.AI integration, streaming, tool calling, Ollama/OpenAI providers
- Enterprise: OAuth PKCE client, shared state store (cross-WebView), plugin compatibility matrix

Details: [Architecture](docs/articles/architecture.md) · [Bridge guide](docs/articles/bridge-guide.md) · [SPA hosting](docs/articles/spa-hosting.md)

---

## Architecture at a Glance

```
┌──────────────────────────────────────────────────────────┐
│                   Your Avalonia App                       │
│  ┌───────────┐   ┌─────────────┐   ┌─────────────────┐   │
│  │ WebView   │   │ Typed Bridge │   │ Capability Gate │   │
│  │ Control   │   │ C# <-> JS   │   │ Host/System API │   │
│  └─────┬─────┘   └──────┬──────┘   └────────┬────────┘   │
│        └────────────────┴──────────────────┘             │
│  ┌─────────────────────────────────────────────────────┐  │
│  │    Runtime Core (policy-first execution)             │  │
│  │    Navigation · RPC · Shell · Diagnostics · Policy  │  │
│  └──────────────────────────┬──────────────────────────┘  │
│                             │ IWebViewAdapter             │
│  ┌──────────────────────────┴──────────────────────────┐  │
│  │   WKWebView · WebView2 · WebKitGTK · Android        │  │
│  └─────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

---

## Vision

Fulora is a C# + Web application framework: **from first WebView embed to full product platform, without rewriting your foundation**. Two paths, one runtime:

- **Control path**: integrate WebView with minimal coupling
- **Framework path**: adopt bridge, policy, shell, and tooling for faster delivery

We close the gap left by typical WebView wrappers: typed host/web IPC, policy-governed capabilities, machine-checkable diagnostics, and scalable app-shell patterns out of the box.

---

## Roadmap Alignment

**Current status:** Phase 12 (Enterprise & Advanced Scenarios) completed. All roadmap phases through 12 are done.

- [Full Roadmap](openspec/ROADMAP.md)
- [Project Vision & Goals](openspec/PROJECT.md)

## Platform Coverage

| Platform | Engine | Status |
|----------|--------|--------|
| Windows | WebView2 | Supported |
| macOS | WKWebView | Supported |
| Linux | WebKitGTK | Supported |
| iOS | WKWebView | Supported |
| Android | Android WebView | Supported |

Avalonia offers an official commercial WebView option; this project is an open, contract-driven hybrid app platform.

---

## Quality Signals

| Metric | Value |
|--------|-------|
| Unit tests | 1883 |
| Integration tests | 209 |
| Line coverage | **97.03%** |
| Branch coverage | **93.03%** |

```bash
nuke Test              # Unit + Integration (2092 tests)
nuke Coverage          # Coverage report + threshold enforcement
nuke NugetPackageTest  # Pack → install → run smoke test
nuke TemplateE2E       # Template end-to-end test
```

---

## License

[MIT](LICENSE.txt)
