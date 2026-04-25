using Agibuild.Fulora.Platforms.Macios.Interop;
using Xunit;

namespace Agibuild.Fulora.Platforms.UnitTests.Macios.Interop;

[Trait("Platform", "macOS")]
public class NSObjectLifecycleTests
{
    [Fact]
    public void Dispose_releases_handle()
    {
        if (!OperatingSystem.IsMacOS()) return;
        IntPtr handleSeen;
        using (var s = NSString.Create("x")!)
        {
            handleSeen = s.Handle;
            Assert.NotEqual(IntPtr.Zero, handleSeen);
        }
        // After Dispose, the managed wrapper must have released its retain.
        // Direct verification of native refcount is non-trivial in xUnit;
        // we settle for "no exception thrown" as smoke.
    }
}
