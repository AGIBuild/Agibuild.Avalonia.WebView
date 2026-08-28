namespace Agibuild.Fulora;

/// <summary>
/// Collapses the three interleaved lifecycle flags previously held directly on
/// <see cref="WebViewCore"/> — "is disposed", "has the adapter-destroyed event already fired", and
/// the named <see cref="WebViewLifecycleState"/> transition marker — into a single owner with named
/// transition methods and a single admission predicate.
/// </summary>
/// <remarks>
/// Callers interact with the state machine exclusively through named mutators.
/// They must not branch on <see cref="CurrentState"/>; that property exists only for diagnostics.
/// Illegal transitions leave state unchanged. Repeated detach/dispose is idempotent.
/// </remarks>
internal sealed class WebViewLifecycleStateMachine
{
    private int _disposed;
    private bool _adapterDestroyed;
    private volatile WebViewLifecycleState _state = WebViewLifecycleState.Created;
    private int _generation;

    /// <summary>Gets a value indicating whether <see cref="TryTransitionToDisposed"/> has succeeded.</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <summary>Gets a value indicating whether <see cref="MarkAdapterDestroyedOnce(System.Action)"/> has run.</summary>
    public bool IsAdapterDestroyed => _adapterDestroyed;

    /// <summary>Gets the current lifecycle phase. For diagnostics only — do not branch on this.</summary>
    public WebViewLifecycleState CurrentState => _state;

    /// <summary>Monotonic attach-attempt identity. Incremented when entering <see cref="WebViewLifecycleState.Attaching"/> from Created.</summary>
    public int Generation => _generation;

    /// <summary>Gets the diagnostic name of <see cref="CurrentState"/> for error messages and logs.</summary>
    public string CurrentStateName => _state.ToString();

    /// <summary>
    /// Gets a value indicating whether a new operation may be enqueued in the current state.
    /// Single source of truth for the admission rule consumed by <see cref="WebViewCoreOperationQueue"/>.
    /// </summary>
    public bool IsOperationAccepted
        => _state is WebViewLifecycleState.Created
            or WebViewLifecycleState.Attaching
            or WebViewLifecycleState.Ready;

    /// <summary>True only after a successful attach has published Ready.</summary>
    public bool IsAttached => _state is WebViewLifecycleState.Ready;

    /// <summary>True when Dispose has completed the terminal transition.</summary>
    public bool IsTerminal => _state is WebViewLifecycleState.Disposed;

    /// <summary>True after attach failed without reaching Ready.</summary>
    public bool IsFaulted => _state is WebViewLifecycleState.Faulted;

    public bool TryTransitionToAttaching()
    {
        if (IsDisposed)
        {
            return false;
        }

        if (_state is WebViewLifecycleState.Attaching)
        {
            return true;
        }

        if (_state is not WebViewLifecycleState.Created)
        {
            return false;
        }

        _generation++;
        _state = WebViewLifecycleState.Attaching;
        return true;
    }

    public bool TryTransitionToReady()
    {
        if (IsDisposed)
        {
            return false;
        }

        if (_state is WebViewLifecycleState.Ready)
        {
            return true;
        }

        if (_state is not WebViewLifecycleState.Attaching)
        {
            return false;
        }

        _state = WebViewLifecycleState.Ready;
        return true;
    }

    public bool TryTransitionToFaulted()
    {
        if (IsDisposed)
        {
            return false;
        }

        if (_state is WebViewLifecycleState.Faulted)
        {
            return true;
        }

        if (_state is not WebViewLifecycleState.Attaching)
        {
            return false;
        }

        _state = WebViewLifecycleState.Faulted;
        return true;
    }

    /// <summary>
    /// Begins detach. Returns <see langword="true"/> when the caller may proceed with native teardown
    /// (first entry into Detaching). Returns <see langword="true"/> without implying a second teardown
    /// when already Detaching or Detached — callers must snapshot <see cref="CurrentState"/> first.
    /// </summary>
    public bool TryTransitionToDetaching()
    {
        if (IsDisposed)
        {
            return false;
        }

        if (_state is WebViewLifecycleState.Detaching or WebViewLifecycleState.Detached)
        {
            return true;
        }

        if (_state is WebViewLifecycleState.Created
            or WebViewLifecycleState.Attaching
            or WebViewLifecycleState.Ready
            or WebViewLifecycleState.Faulted)
        {
            _state = WebViewLifecycleState.Detaching;
            return true;
        }

        return false;
    }

    public bool TryTransitionToDetached()
    {
        if (IsDisposed)
        {
            return false;
        }

        if (_state is WebViewLifecycleState.Detached)
        {
            return true;
        }

        if (_state is not WebViewLifecycleState.Detaching)
        {
            return false;
        }

        _state = WebViewLifecycleState.Detached;
        return true;
    }

    /// <summary>
    /// Attempts to transition into the terminal <see cref="WebViewLifecycleState.Disposed"/> state.
    /// Returns <see langword="false"/> when disposal has already occurred.
    /// </summary>
    public bool TryTransitionToDisposed()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return false;
        }

        _state = WebViewLifecycleState.Disposed;
        return true;
    }

    public void TransitionToAttaching() => ThrowIfIllegal(TryTransitionToAttaching(), WebViewLifecycleState.Attaching);

    public void TransitionToReady() => ThrowIfIllegal(TryTransitionToReady(), WebViewLifecycleState.Ready);

    public void TransitionToFaulted() => ThrowIfIllegal(TryTransitionToFaulted(), WebViewLifecycleState.Faulted);

    public void TransitionToDetaching() => ThrowIfIllegal(TryTransitionToDetaching(), WebViewLifecycleState.Detaching);

    public void TransitionToDetached() => ThrowIfIllegal(TryTransitionToDetached(), WebViewLifecycleState.Detached);

    public void MarkAdapterDestroyedOnce(System.Action raise)
    {
        ArgumentNullException.ThrowIfNull(raise);

        if (_adapterDestroyed)
        {
            return;
        }

        _adapterDestroyed = true;
        raise();
    }

    public void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(IsDisposed, nameof(WebViewCore));

    private void ThrowIfIllegal(bool succeeded, WebViewLifecycleState target)
    {
        if (!succeeded)
        {
            throw new InvalidOperationException($"Cannot transition to {target} from {CurrentStateName}.");
        }
    }
}
