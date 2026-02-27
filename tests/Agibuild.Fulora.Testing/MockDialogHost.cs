using Agibuild.Fulora;

namespace Agibuild.Fulora.Testing;

/// <summary>
/// Mock dialog host for unit testing WebDialog without an Avalonia application.
/// </summary>
internal sealed class MockDialogHost : IDialogHost
{
    public string? Title { get; set; }
    public bool CanUserResize { get; set; }
    public bool IsShown { get; private set; }
    public bool IsClosed { get; private set; }
    public int ShowCallCount { get; private set; }
    public int CloseCallCount { get; private set; }
    public int ResizeCallCount { get; private set; }
    public int MoveCallCount { get; private set; }
    public (int Width, int Height)? LastResize { get; private set; }
    public (int X, int Y)? LastMove { get; private set; }

    public event EventHandler? HostClosing;

    public void Show()
    {
        ShowCallCount++;
        IsShown = true;
    }

    public bool ShowWithOwner(INativeHandle owner)
    {
        ShowCallCount++;
        IsShown = true;
        return true;
    }

    public void Close()
    {
        if (IsClosed) return;
        CloseCallCount++;
        IsClosed = true;
        IsShown = false;
        HostClosing?.Invoke(this, EventArgs.Empty);
    }

    public bool Resize(int width, int height)
    {
        ResizeCallCount++;
        LastResize = (width, height);
        return true;
    }

    public bool Move(int x, int y)
    {
        MoveCallCount++;
        LastMove = (x, y);
        return true;
    }

    /// <summary>Simulates the user closing the dialog (raises HostClosing).</summary>
    public void SimulateUserClose()
    {
        if (IsClosed) return;
        IsClosed = true;
        IsShown = false;
        HostClosing?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Mock factory that creates WebDialog instances with MockDialogHost and MockWebViewAdapter.
/// Captures created dialogs for test assertions.
/// </summary>
internal sealed class MockWebDialogFactory : IWebDialogFactory
{
    private readonly TestDispatcher _dispatcher;

    public MockWebDialogFactory(TestDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public List<(WebDialog Dialog, MockDialogHost Host, MockWebViewAdapter Adapter)> CreatedDialogs { get; } = new();

    public IWebDialog Create(IWebViewEnvironmentOptions? options = null)
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        var dialog = new WebDialog(host, adapter, _dispatcher);

        CreatedDialogs.Add((dialog, host, adapter));
        return dialog;
    }
}
