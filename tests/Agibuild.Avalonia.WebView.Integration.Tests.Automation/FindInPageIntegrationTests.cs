using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

/// <summary>
/// Integration tests for the Find-in-Page feature.
///
/// HOW IT WORKS (for newcomers):
///   1. We create a MockWebViewAdapterWithFind — it returns a preset FindInPageResult.
///   2. We wrap it in a WebDialog (same as a real app would).
///   3. We call FindInPageAsync("text") and verify the returned result.
///   4. We call StopFindInPage() and verify the adapter was notified.
///   5. We also test that a basic adapter (no find support) throws NotSupportedException.
/// </summary>
public sealed class FindInPageIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    // ──────────────────── Test 1: Find returns match result ────────────────────

    [AvaloniaFact]
    public async Task Find_returns_match_result()
    {
        // Arrange: adapter that supports find-in-page
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithFind();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        // Act
        var result = await dialog.FindInPageAsync("hello");

        // Assert
        Assert.Equal(0, result.ActiveMatchIndex);
        Assert.Equal(3, result.TotalMatches);
    }

    // ──────────────────── Test 2: Find passes options to adapter ────────────────────

    [AvaloniaFact]
    public async Task Find_passes_options_to_adapter()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithFind();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        var opts = new FindInPageOptions { CaseSensitive = true, Forward = false };
        await dialog.FindInPageAsync("test", opts);

        var findAdapter = (MockWebViewAdapterWithFind)adapter;
        Assert.Equal("test", findAdapter.LastSearchText);
        Assert.True(findAdapter.LastOptions?.CaseSensitive);
        Assert.False(findAdapter.LastOptions?.Forward);
    }

    // ──────────────────── Test 3: StopFind notifies adapter ────────────────────

    [AvaloniaFact]
    public void StopFind_notifies_adapter()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithFind();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        dialog.StopFindInPage(false);

        var findAdapter = (MockWebViewAdapterWithFind)adapter;
        Assert.True(findAdapter.StopFindCalled);
        Assert.False(findAdapter.LastClearHighlights);
    }

    // ──────────────────── Test 4: Find without adapter throws ────────────────────

    [AvaloniaFact]
    public async Task Find_without_adapter_throws_NotSupportedException()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create(); // basic — no find support
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        await Assert.ThrowsAsync<NotSupportedException>(() => dialog.FindInPageAsync("x"));
    }

    // ──────────────────── Test 5: StopFind without adapter throws ────────────────────

    [AvaloniaFact]
    public void StopFind_without_adapter_throws_NotSupportedException()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        Assert.Throws<NotSupportedException>(() => dialog.StopFindInPage());
    }

    // ──────────────────── Test 6: Find after dispose throws ────────────────────

    [AvaloniaFact]
    public async Task Find_after_dispose_throws_ObjectDisposedException()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithFind();
        var dialog = new WebDialog(host, adapter, _dispatcher);
        dialog.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => dialog.FindInPageAsync("test"));
    }

    // ──────────────────── Test 7: StopFind after dispose throws ────────────────────

    [AvaloniaFact]
    public void StopFind_after_dispose_throws_ObjectDisposedException()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithFind();
        var dialog = new WebDialog(host, adapter, _dispatcher);
        dialog.Dispose();

        Assert.Throws<ObjectDisposedException>(() => dialog.StopFindInPage());
    }

    // ──────────────────── Test 8: Find with null text throws ArgumentException ────────────────────

    [AvaloniaFact]
    public async Task Find_with_null_text_throws_ArgumentException()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithFind();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        await Assert.ThrowsAsync<ArgumentException>(() => dialog.FindInPageAsync(null!));
    }

    // ──────────────────── Test 9: Find with empty text throws ArgumentException ────────────────────

    [AvaloniaFact]
    public async Task Find_with_empty_text_throws_ArgumentException()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithFind();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        await Assert.ThrowsAsync<ArgumentException>(() => dialog.FindInPageAsync(""));
    }

    // ──────────────────── Test 10: Find → Stop → Find again works ────────────────────

    [AvaloniaFact]
    public async Task Find_stop_find_again_works()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithFind();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        await dialog.FindInPageAsync("first");
        dialog.StopFindInPage();
        var result = await dialog.FindInPageAsync("second");

        var findAdapter = (MockWebViewAdapterWithFind)adapter;
        Assert.Equal("second", findAdapter.LastSearchText);
        Assert.Equal(3, result.TotalMatches);
    }
}
