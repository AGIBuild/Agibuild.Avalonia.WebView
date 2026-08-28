using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Testing;

namespace Agibuild.Fulora.UnitTests.TestDoubles;

internal sealed class HeadlessWebViewAdapter : StubWebViewAdapter
{
    private readonly HeadlessWebViewBackend _backend;
    private IWebViewAdapterHost? _host;
    private bool _detached;
    private bool _messagingEnabled = true;

    public HeadlessWebViewAdapter(HeadlessWebViewBackend backend)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _backend.Completed += OnCompleted;
        _backend.Redirected += OnRedirected;
        _backend.MessageEmitted += OnMessageEmitted;
    }

    public int AttachCount { get; private set; }
    public int DetachCount { get; private set; }
    public Guid? LastNavigationId { get; private set; }
    public Uri? LastNavigationUri { get; private set; }

    public override void Initialize(IWebViewAdapterHost host)
        => _host = host ?? throw new ArgumentNullException(nameof(host));

    public override Task AttachAsync(INativeHandle parentHandle, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AttachCount++;
        return Task.CompletedTask;
    }

    public override void Detach()
    {
        _detached = true;
        DetachCount++;
    }

    public override Task NavigateAsync(Guid navigationId, Uri uri)
    {
        LastNavigationId = navigationId;
        LastNavigationUri = uri;
        _backend.BeginNavigation(navigationId, uri);
        return Task.CompletedTask;
    }

    public override Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)
        => NavigateAsync(navigationId, baseUrl ?? new Uri("about:blank"));

    public override Task<string?> InvokeScriptAsync(string script)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _backend.EnqueueScript(script, tcs);
        return tcs.Task;
    }

    public void SetMessagingEnabled(bool enabled) => _messagingEnabled = enabled;

    private void OnCompleted(HeadlessWebViewBackend.HeadlessNavigation navigation, NavigationCompletedStatus status, Exception? error)
    {
        if (_detached)
        {
            return;
        }

        RaiseNavigationCompleted(
            new NavigationCompletedEventArgs(
                navigation.Id,
                navigation.Uri,
                status,
                status == NavigationCompletedStatus.Failure
                    ? error ?? new InvalidOperationException("Navigation failed.")
                    : null));
    }

    private void OnRedirected(Guid correlationId, Uri uri)
    {
        if (_detached || _host is null)
        {
            return;
        }

        _ = _host.OnNativeNavigationStartingAsync(new NativeNavigationStartingInfo(correlationId, uri, IsMainFrame: true));
    }

    private void OnMessageEmitted(WebMessageEnvelope envelope)
    {
        if (_detached || !_messagingEnabled)
        {
            return;
        }

        RaiseWebMessageReceived(new WebMessageReceivedEventArgs(
            envelope.Body,
            envelope.Origin,
            envelope.ChannelId,
            envelope.ProtocolVersion));
    }
}
