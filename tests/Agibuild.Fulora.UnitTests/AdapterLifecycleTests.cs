using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class AdapterLifecycleTests
{
    [Fact]
    public async Task Attach_requires_initialize()
    {
        var adapter = new MockWebViewAdapter();
        var handle = new TestPlatformHandle(IntPtr.Zero, "test");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => adapter.AttachAsync(handle, CancellationToken.None));
    }

    [Fact]
    public void Detach_requires_attach()
    {
        var adapter = new MockWebViewAdapter();

        adapter.Initialize(new DummyAdapterHost());
        Assert.Throws<InvalidOperationException>(() => adapter.Detach());
    }

    [Fact]
    public async Task No_events_are_raised_after_detach()
    {
        var adapter = new MockWebViewAdapter();
        var handle = new TestPlatformHandle(IntPtr.Zero, "test");
        var raised = false;

        adapter.Initialize(new DummyAdapterHost());
        await adapter.AttachAsync(handle, CancellationToken.None);
        adapter.Detach();

        adapter.NavigationCompleted += (_, _) => raised = true;
        adapter.RaiseNavigationCompleted();

        Assert.False(raised);
    }

    private sealed class DummyAdapterHost : IWebViewAdapterHost
    {
        public Guid ChannelId { get; } = Guid.NewGuid();

        public ValueTask<NativeNavigationStartingDecision> OnNativeNavigationStartingAsync(NativeNavigationStartingInfo info)
            => ValueTask.FromResult(new NativeNavigationStartingDecision(IsAllowed: true, NavigationId: Guid.NewGuid()));
    }
}
