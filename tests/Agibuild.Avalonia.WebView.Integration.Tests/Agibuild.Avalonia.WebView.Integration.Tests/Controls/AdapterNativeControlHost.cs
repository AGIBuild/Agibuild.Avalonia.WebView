using System;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Controls;

public sealed class AdapterNativeControlHost : NativeControlHost
{
    public event Action<IPlatformHandle>? HandleCreated;
    public event Action? HandleDestroyed;

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var handle = base.CreateNativeControlCore(parent);
        HandleCreated?.Invoke(handle);
        return handle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        HandleDestroyed?.Invoke();
        base.DestroyNativeControlCore(control);
    }
}

