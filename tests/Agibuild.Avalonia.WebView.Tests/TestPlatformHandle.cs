using Avalonia.Platform;

namespace Agibuild.Avalonia.WebView.Tests;

internal sealed class TestPlatformHandle : IPlatformHandle
{
    public TestPlatformHandle(IntPtr handle, string handleDescriptor)
    {
        Handle = handle;
        HandleDescriptor = handleDescriptor;
    }

    public IntPtr Handle { get; }
    public string HandleDescriptor { get; }
}
