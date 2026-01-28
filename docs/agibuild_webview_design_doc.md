# Agibuild.Avalonia.WebView 设计文档（TDD 优先）

---

## 1. 目标与设计原则

### 🎯 目标

构建一个完全模拟 Avalonia.Controls.WebView 功能的可替代控件库，具备一致的 API、跨平台行为，并支持：
- 嵌入式浏览器控件（主 UI）
- 弹窗/对话框模式 WebView
- Web 认证流（OAuth 等）
- 深度 JavaScript ↔ C# 双向交互
- 环境配置与原生扩展
- 易于单元测试（TDD 支持）

### 🧠 设计原则（TDD 优先）

1. **契约优先**：先定义接口和契约，无平台依赖，写测试驱动契约行为。
2. **跨平台隔离与适配器模式**：通过适配器抽象层隔离平台差异，使用 DI 容器注入平台实现。
3. **可测试 & Mockable**：所有公开 API 均基于接口，事件与回调提供模拟触发机制。

---

## 2. 核心模块架构（分层）

```
Agibuild.WebView
│
├── Core
│   ├─ IWebView
│   ├─ IWebDialog
│   ├─ IWebAuthBroker
│   ├─ IWebViewEnvironmentOptions
│   └─ NativeInterop
│
├── PlatformAdapters
│   ├─ WindowsWebView2Adapter
│   ├─ WKWebViewAdapter
│   ├─ AndroidWebViewAdapter
│   └─ GtkWebViewAdapter
│
├── Services
│   ├─ ScriptInvoker
│   ├─ MessageSerializer
│   ├─ NavigationManager
│   └─ CookieManager
│
└── Tests
    ├─ ContractTests
    ├─ MockAdapters
    └─ IntegrationSpecs
```

---

## 3. 公共契约层（核心 API & 接口）

### 3.1 IWebView — 主浏览器控件合同

```csharp
public interface IWebView
{
    Uri Source { get; set; }
    bool CanGoBack { get; }
    bool CanGoForward { get; }

    Task NavigateAsync(Uri uri);
    Task NavigateToStringAsync(string html);

    Task<string?> InvokeScriptAsync(string script);

    bool GoBack();
    bool GoForward();
    bool Refresh();
    bool Stop();

    ICookieManager? TryGetCookieManager();
    ICommandManager? TryGetCommandManager();

    event EventHandler<NavigationStartingEventArgs> NavigationStarted;
    event EventHandler<NavigationCompletedEventArgs> NavigationCompleted;
    event EventHandler<NewWindowRequestedEventArgs> NewWindowRequested;
    event EventHandler<WebMessageReceivedEventArgs> WebMessageReceived;
    event EventHandler<WebResourceRequestedEventArgs> WebResourceRequested;
    event EventHandler<EnvironmentRequestedEventArgs> EnvironmentRequested;
}
```

### 3.2 IWebDialog — 弹窗 Web 模式

```csharp
public interface IWebDialog : IWebView
{
    string? Title { get; set; }
    bool CanUserResize { get; set; }

    void Show();
    bool Show(IPlatformHandle owner);
    void Close();

    bool Resize(int width, int height);
    bool Move(int x, int y);

    event EventHandler Closing;
}
```

### 3.3 IWebAuthBroker — Web 认证流程

```csharp
public interface IWebAuthBroker
{
    Task<WebAuthResult> AuthenticateAsync(
        ITopLevelWindow owner,
        AuthOptions options);
}
```

### 3.4 IWebViewEnvironmentOptions

```csharp
public interface IWebViewEnvironmentOptions
{
    bool EnableDevTools { get; set; }
    // platform-specific options
}
```

### 3.5 INativeWebViewHandleProvider

```csharp
public interface INativeWebViewHandleProvider
{
    IPlatformHandle? TryGetWebViewHandle();
}
```

---

## 4. 事件契约与参数定义

```csharp
public class NavigationStartingEventArgs : EventArgs
{
    public Uri RequestUri { get; }
    public bool Cancel { get; set; }
}
```

其他事件如 `NavigationCompletedEventArgs`、`WebMessageReceivedEventArgs`、`WebResourceRequestedEventArgs` 都严格契合官方。

---

## 5. Platform Adapters 抽象与实现

### 5.1 适配器接口

```csharp
public interface IWebViewAdapter
{
    void Initialize(IWebView host);
    Task NavigateAsync(Uri uri);
    // … (forward/back/stop/refresh)
}
```

### 5.2 Windows WebView2 示例
- 实现 `IWebViewAdapter`
- 处理 WebView2-specific 环境选项（ProfileName / UserDataFolder）

---

## 6. 测试设计（TDD）

### 6.1 Contract Tests
- `NavigateAsync(null)` 抛出异常
- `NavigationStarted` 触发且 Cancel 可阻止导航
- `InvokeScriptAsync` 在 MockAdapter 下返回预设结果

### 6.2 Mock Adapters

```csharp
public class MockWebViewAdapter : IWebViewAdapter
{
    public Task NavigateAsync(Uri uri) => Task.CompletedTask;
    public Uri LastNavigation { get; private set; }
}
```

### 6.3 Event Stub Tests

```csharp
[Test]
public void WebView_OnWebMessageReceived_ShouldPassMessage()
{
    var webView = new WebViewCore(new MockAdapter());
    string received = "";
    webView.WebMessageReceived += (_, e) => received = e.Body;

    webView.MockTriggerWebMessage("{ foo: 42 }");
    Assert.AreEqual("{ foo: 42 }", received);
}
```

---

## 7. 高级能力与扩展

- 环境定制（DevTools、隐私模式、UA定制、离线存储设置）
- Web 资源请求拦截与缓存
- JS 通信桥（JSON ↔ C#）

---

## 8. 弹窗与 Auth 流

### 8.1 WebDialog 流程
- 类似 NativeWebDialog，提供窗口控制，适配多平台。

### 8.2 WebAuthenticationBroker
- 支持 AuthenticateAsync
- CallbackUri 处理
- 可自定义 NativeWebDialog 工厂

---

## 9. FAQ 设计约定 & 限制说明

| 问题 | 约定 |
|------|------|
| 支持 Linux 嵌入式 WebView? | 不支持 fallback Dialog |
| 是否支持离屏渲染？ | 官方不支持，不作为必需 |
| Native 互操作？ | 提供统一抽象句柄接口 |

---

## 10. 实现建议与路线

1. 定义契约接口 + Mock Tests
2. 实现 Adapter 框架 + Mock 运行
3. 实现 Windows Adapter
4. 实现 macOS/iOS Adapter
5. 实现 Android Adapter
6. 扩展 Dialog & AuthFlow

---

**总结**

设计文档制定了一个契约驱动、可测试、跨平台隔离明显、与官方功能完全对应的 WebView 实现方案，覆盖嵌入式浏览器、弹窗 Web 浏览、OAuth / WebAuthFlow、环境配置、JS ↔ C# 交互及原生互操作。

