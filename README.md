# Agibuild.Avalonia.WebView

[![CI](https://github.com/AgibuilDev/Agibuild.Avalonia.WebView/actions/workflows/ci.yml/badge.svg)](https://github.com/AgibuilDev/Agibuild.Avalonia.WebView/actions/workflows/ci.yml)
[![Release](https://github.com/AgibuilDev/Agibuild.Avalonia.WebView/actions/workflows/release.yml/badge.svg)](https://github.com/AgibuilDev/Agibuild.Avalonia.WebView/actions/workflows/release.yml)
[![NuGet](https://img.shields.io/nuget/v/Agibuild.Avalonia.WebView?logo=nuget&color=blue)](https://www.nuget.org/packages/Agibuild.Avalonia.WebView)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Agibuild.Avalonia.WebView?logo=nuget&color=green)](https://www.nuget.org/packages/Agibuild.Avalonia.WebView)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)

Cross-platform WebView control for [Avalonia UI](https://avaloniaui.net/) with native platform adapters.

| Platform | Engine | Status |
|----------|--------|--------|
| macOS | WKWebView | ![Stable](https://img.shields.io/badge/Stable-brightgreen) |
| Windows | WebView2 | ![Preview](https://img.shields.io/badge/Preview-orange) |
| Linux | WebKitGTK | ![Preview](https://img.shields.io/badge/Preview-orange) |
| Android | Android WebView | ![Planned](https://img.shields.io/badge/Planned-lightgrey) |

## Installation

```bash
dotnet add package Agibuild.Avalonia.WebView
```

The correct native adapter is automatically selected at runtime — no per-platform packages needed.

## Quick Start

### Minimal Setup

Add `.UseAgibuildWebView()` to your existing `AppBuilder` chain:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UseAgibuildWebView() // <-- add this line
    .StartWithClassicDesktopLifetime(args);
```

With logging:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UseAgibuildWebView(loggerFactory)
    .StartWithClassicDesktopLifetime(args);
```

### XAML

```xml
<webview:WebView Source="https://example.com" />
```

Where `webview` is:

```xml
xmlns:webview="using:Agibuild.Avalonia.WebView"
```

### With Dependency Injection

```csharp
var services = new ServiceCollection();
services.AddWebView();
services.AddWebViewDialogServices(); // Optional: dialogs + OAuth

var provider = services.BuildServiceProvider();
provider.UseAgibuildWebView();
```

## Features

### Navigation

```csharp
await webView.NavigateAsync(new Uri("https://example.com"));
await webView.NavigateToStringAsync("<h1>Hello</h1>");

webView.GoBack();
webView.GoForward();
webView.Refresh();
webView.Stop();
```

### JavaScript Interop

```csharp
string? result = await webView.InvokeScriptAsync("document.title");
```

### Events

```csharp
webView.NavigationStarted += (s, e) =>
{
    // e.RequestUri, e.Cancel
};

webView.NavigationCompleted += (s, e) =>
{
    // e.Status: Success, Failure, Canceled, Superseded
};

webView.NewWindowRequested += (s, e) =>
{
    e.Handled = true; // prevent popup, handle yourself
};

webView.WebMessageReceived += (s, e) =>
{
    // e.Body, e.Origin, e.ChannelId
};
```

### OAuth Authentication

```csharp
var broker = serviceProvider.GetRequiredService<IWebAuthBroker>();

var result = await broker.AuthenticateAsync(window, new AuthOptions
{
    AuthorizeUri = new Uri("https://provider.com/oauth/authorize?..."),
    CallbackUri = new Uri("myapp://callback"),
    UseEphemeralSession = true,
    Timeout = TimeSpan.FromMinutes(5)
});

if (result.Status == WebAuthStatus.Success)
{
    // result.CallbackUri contains the OAuth callback with tokens
}
```

### Web Dialog (Popup Window)

```csharp
var factory = serviceProvider.GetRequiredService<IWebDialogFactory>();
var dialog = factory.Create();

dialog.Title = "Sign In";
dialog.Resize(800, 600);
dialog.Show();
await dialog.NavigateAsync(new Uri("https://example.com/login"));
```

### Cookie Management (Experimental)

```csharp
var cookieManager = webView.TryGetCookieManager();
if (cookieManager is not null)
{
    var cookies = await cookieManager.GetCookiesAsync(new Uri("https://example.com"));
    await cookieManager.ClearAllCookiesAsync();
}
```

### Environment Options

```csharp
WebViewEnvironment.Initialize(loggerFactory, new WebViewEnvironmentOptions
{
    EnableDevTools = true,
    CustomUserAgent = "MyApp/1.0",
    UseEphemeralSession = false
});
```

## License

[MIT](LICENSE.txt)
