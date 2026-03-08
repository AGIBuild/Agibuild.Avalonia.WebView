namespace Agibuild.Fulora.Adapters.Abstractions;

internal static class WebViewBridgeScriptFactory
{
    public static string CreateWindowsBridgeBootstrapScript(Guid channelId)
    {
        return $$"""
            window.__agibuildWebView = window.__agibuildWebView || {};
            window.__agibuildWebView.channelId = '{{channelId}}';
            window.__agibuildWebView.postMessage = function(body) {
                window.chrome.webview.postMessage(JSON.stringify({
                    channelId: '{{channelId}}',
                    protocolVersion: 1,
                    body: body
                }));
            };
            """;
    }

    public static string CreateAndroidBridgeBootstrapScript(Guid channelId)
    {
        return $$"""
            (function() {
                window.__agibuildWebView = window.__agibuildWebView || {};
                window.__agibuildWebView.channelId = '{{channelId}}';
                window.__agibuildWebView.postMessage = function(body) {
                    if (window.__agibuildBridge) {
                        window.__agibuildBridge.postMessage(body);
                    }
                };
                if (!window.chrome) window.chrome = {};
                if (!window.chrome.webview) window.chrome.webview = {};
                window.chrome.webview.postMessage = window.__agibuildWebView.postMessage;
            })();
            """;
    }
}
