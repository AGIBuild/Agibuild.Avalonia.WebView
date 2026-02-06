using System;
using System.Threading.Tasks;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;
using Agibuild.Avalonia.WebView.Testing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

public partial class WebViewTestViewModel : ViewModelBase
{
    private readonly IWebViewAdapter _adapter;

    [ObservableProperty]
    private string _address = "https://example.test";

    [ObservableProperty]
    private string _htmlContent = "<html><body><h1>WebView Test</h1></body></html>";

    [ObservableProperty]
    private string _script = "return 'ok';";

    [ObservableProperty]
    private string? _scriptResult;

    [ObservableProperty]
    private string? _lastNavigation;

    [ObservableProperty]
    private string? _lastEvent;

    internal WebViewTestViewModel(IWebViewAdapter adapter)
    {
        _adapter = adapter;
        _adapter.NavigationCompleted += (_, _) => LastEvent = "NavigationCompleted";
        _adapter.NewWindowRequested += (_, _) => LastEvent = "NewWindowRequested";
        _adapter.WebMessageReceived += (_, _) => LastEvent = "WebMessageReceived";
        _adapter.WebResourceRequested += (_, _) => LastEvent = "WebResourceRequested";
        _adapter.EnvironmentRequested += (_, _) => LastEvent = "EnvironmentRequested";

        _adapter.Initialize(new DummyAdapterHost());
        _adapter.Attach(new TestPlatformHandle(IntPtr.Zero, "test"));
    }

    [RelayCommand]
    private async Task NavigateAsync()
    {
        var uri = new Uri(Address, UriKind.Absolute);
        await _adapter.NavigateAsync(Guid.NewGuid(), uri);
        LastNavigation = uri.ToString();
    }

    [RelayCommand]
    private async Task NavigateHtmlAsync()
    {
        await _adapter.NavigateToStringAsync(Guid.NewGuid(), HtmlContent);
        LastNavigation = "html";
    }

    [RelayCommand]
    private async Task InvokeScriptAsync()
    {
        ScriptResult = await _adapter.InvokeScriptAsync(Script);
    }

    private sealed class DummyAdapterHost : IWebViewAdapterHost
    {
        public Guid ChannelId { get; } = Guid.NewGuid();

        public ValueTask<NativeNavigationStartingDecision> OnNativeNavigationStartingAsync(NativeNavigationStartingInfo info)
            => ValueTask.FromResult(new NativeNavigationStartingDecision(IsAllowed: true, NavigationId: Guid.NewGuid()));
    }
}
