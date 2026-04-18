namespace Agibuild.Fulora;

/// <summary>
/// Bridge service implementation that exposes drag-and-drop events to JavaScript.
/// <para>
/// Consumers interact with this service via the <see cref="IDragDropBridgeService"/> contract;
/// the concrete type is <see langword="internal"/> because it intentionally couples to the
/// internal <c>WebViewCore</c> drop event surface.
/// </para>
/// </summary>
internal sealed class DragDropBridgeService : IDragDropBridgeService
{
    private DragDropPayload? _lastPayload;
    private readonly bool _isSupported;

    /// <summary>
    /// Creates a new <see cref="DragDropBridgeService"/> that subscribes to the given core's drop events.
    /// </summary>
    internal DragDropBridgeService(WebViewCore core)
    {
        ArgumentNullException.ThrowIfNull(core);
        _isSupported = core.HasDragDropSupport;
        core.DropCompleted += (_, e) => _lastPayload = e.Payload;
    }

    /// <inheritdoc />
    public Task<DragDropPayload?> GetLastDropPayloadAsync(CancellationToken ct = default)
        => Task.FromResult(_lastPayload);

    /// <inheritdoc />
    public Task<bool> IsDragDropSupportedAsync(CancellationToken ct = default)
        => Task.FromResult(_isSupported);
}
