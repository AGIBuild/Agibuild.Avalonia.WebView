using System.Collections.Concurrent;

namespace Agibuild.Fulora.Adapters.Abstractions;

internal enum WebViewAdapterPlatform
{
    Windows,
    MacOS,
    Android,
    Gtk,
    iOS
}

internal sealed record WebViewAdapterRegistration(
    WebViewAdapterPlatform Platform,
    string AdapterId,
    Func<IWebViewAdapter> Factory,
    int Priority = 0);

/// <summary>
/// Registry for platform adapter plugins.
/// Intended to be populated by adapter assemblies via module initializers.
/// </summary>
internal static class WebViewAdapterRegistry
{
    private static readonly ConcurrentDictionary<(WebViewAdapterPlatform Platform, string AdapterId), WebViewAdapterRegistration> Registrations = new();
    private static readonly ConcurrentDictionary<string, IWebViewPlatformProvider> Providers = new(StringComparer.Ordinal);
    private sealed record CandidateAdapter(int Priority, Func<IWebViewAdapter> Factory);

    public static void Register(WebViewAdapterRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(registration.AdapterId);
        ArgumentNullException.ThrowIfNull(registration.Factory);

        if (string.IsNullOrWhiteSpace(registration.AdapterId))
        {
            throw new ArgumentException("AdapterId must be non-empty.", nameof(registration));
        }

        Registrations.TryAdd((registration.Platform, registration.AdapterId), registration);
    }

    public static void RegisterProvider(IWebViewPlatformProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider.Id);

        Providers.TryAdd(provider.Id, provider);
    }

    internal static void ResetForTests()
    {
        Providers.Clear();
        Registrations.Clear();
    }

    public static bool HasAnyForCurrentPlatform()
        => Providers.Values.Any(static provider => provider.CanHandleCurrentPlatform())
            || Registrations.Keys.Any(k => k.Platform == GetCurrentPlatform());

    public static bool TryCreateForCurrentPlatform(out IWebViewAdapter adapter, out string? failureReason)
    {
        var platform = GetCurrentPlatform();

        var candidate = Providers.Values
            .Where(static provider => provider.CanHandleCurrentPlatform())
            .Select(static provider => new CandidateAdapter(provider.Priority, provider.CreateAdapter))
            .Concat(Registrations.Values
                .Where(registration => registration.Platform == platform)
                .Select(static registration => new CandidateAdapter(registration.Priority, registration.Factory)))
            .OrderByDescending(static candidate => candidate.Priority)
            .FirstOrDefault();

        if (candidate is not null)
        {
            adapter = candidate.Factory();
            failureReason = null;
            return true;
        }

        adapter = null!;
        failureReason = $"No WebView adapter registered for platform '{platform}'.";
        return false;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static WebViewAdapterPlatform GetCurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return WebViewAdapterPlatform.Windows;
        }

        // iOS check must come before macOS because IsMacOS() returns true on Mac Catalyst.
        if (OperatingSystem.IsIOS())
        {
            return WebViewAdapterPlatform.iOS;
        }

        if (OperatingSystem.IsMacOS())
        {
            return WebViewAdapterPlatform.MacOS;
        }

        if (OperatingSystem.IsAndroid())
        {
            return WebViewAdapterPlatform.Android;
        }

        return WebViewAdapterPlatform.Gtk;
    }
}
