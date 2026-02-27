using Agibuild.Fulora;

namespace Agibuild.Fulora.Testing;

public sealed class TestPlatformHandle : INativeHandle
{
    public TestPlatformHandle(IntPtr handle, string handleDescriptor)
    {
        Handle = handle;
        HandleDescriptor = handleDescriptor;
    }

    public nint Handle { get; }
    public string HandleDescriptor { get; }
}
