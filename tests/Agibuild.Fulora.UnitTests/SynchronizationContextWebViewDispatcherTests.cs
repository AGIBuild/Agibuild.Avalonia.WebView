using System.Threading;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class SynchronizationContextWebViewDispatcherTests
{
    [Fact]
    public void CheckAccess_remains_true_on_captured_thread_when_sync_context_changes()
    {
        var originalContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var dispatcher = new SynchronizationContextWebViewDispatcher();

            // Simulate UI thread execution where SynchronizationContext.Current is not the captured instance.
            SynchronizationContext.SetSynchronizationContext(null);

            Assert.True(dispatcher.CheckAccess());
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }
}
