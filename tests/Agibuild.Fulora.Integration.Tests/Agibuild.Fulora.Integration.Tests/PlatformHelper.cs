using System;

namespace Agibuild.Fulora.Integration.Tests;

/// <summary>
/// Static platform detection helpers that can be consumed in AXAML via
/// <c>{x:Static local:PlatformHelper.IsMobile}</c> etc.
/// </summary>
internal static class PlatformHelper
{
    public static bool IsMobile { get; } = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
    public static bool IsDesktop { get; } = !IsMobile;
}
