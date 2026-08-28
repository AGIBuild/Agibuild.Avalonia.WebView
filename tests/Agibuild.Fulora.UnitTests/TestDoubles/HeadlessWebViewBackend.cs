using Agibuild.Fulora.Adapters.Abstractions;

namespace Agibuild.Fulora.UnitTests.TestDoubles;

internal sealed class HeadlessWebViewBackend
{
    private readonly Dictionary<Guid, HeadlessNavigation> _navigations = new();
    private readonly Dictionary<Guid, bool> _completedNavigations = new();
    private readonly List<HeadlessScript> _scripts = [];
    private Uri? _pendingNavigate;

    public IReadOnlyList<string> DispatchedScripts => _scripts.ConvertAll(s => s.Text);

    public void BeginNavigation(Guid navigationId, Uri uri)
    {
        if (!_navigations.TryAdd(navigationId, new HeadlessNavigation(navigationId, uri)))
        {
            throw new InvalidOperationException($"Duplicate navigation '{navigationId}'.");
        }

        _pendingNavigate = uri;
    }

    public Guid CompleteNavigation(NavigationCompletedStatus status = NavigationCompletedStatus.Success, Exception? error = null)
    {
        var navigation = RequireSingleOpenNavigation();
        return CompleteNavigation(navigation.Id, status, error);
    }

    public Guid CompleteNavigation(Guid navigationId, NavigationCompletedStatus status, Exception? error = null)
    {
        if (!_completedNavigations.TryAdd(navigationId, true))
        {
            throw new InvalidOperationException($"Duplicate completion for '{navigationId}'.");
        }

        if (!_navigations.TryGetValue(navigationId, out var navigation))
        {
            throw new InvalidOperationException($"Unknown navigation '{navigationId}'.");
        }

        Completed?.Invoke(navigation, status, error);
        return navigationId;
    }

    public void Redirect(Guid correlationId, Uri uri)
        => Redirected?.Invoke(correlationId, uri);

    public void EmitMessage(WebMessageEnvelope envelope)
        => MessageEmitted?.Invoke(envelope);

    public void EnqueueScript(string text, TaskCompletionSource<string?> completion)
        => _scripts.Add(new HeadlessScript(text, completion));

    public void CompleteScript(string? result)
        => RequireSingleOpenScript().Completion.TrySetResult(result);

    public void FailScript(Exception error)
        => RequireSingleOpenScript().Completion.TrySetException(error);

    public event Action<HeadlessNavigation, NavigationCompletedStatus, Exception?>? Completed;
    public event Action<Guid, Uri>? Redirected;
    public event Action<WebMessageEnvelope>? MessageEmitted;

    private HeadlessNavigation RequireSingleOpenNavigation()
    {
        HeadlessNavigation? open = null;
        foreach (var navigation in _navigations.Values)
        {
            if (_completedNavigations.ContainsKey(navigation.Id))
            {
                continue;
            }

            if (open is not null)
            {
                throw new InvalidOperationException("Ambiguous open navigation; pass NavigationId.");
            }

            open = navigation;
        }

        return open ?? throw new InvalidOperationException("No open navigation.");
    }

    private HeadlessScript RequireSingleOpenScript()
    {
        var open = _scripts.Find(s => !s.Completion.Task.IsCompleted)
            ?? throw new InvalidOperationException("No open script.");
        return open;
    }

    internal sealed record HeadlessNavigation(Guid Id, Uri Uri);
    private sealed record HeadlessScript(string Text, TaskCompletionSource<string?> Completion);
}
