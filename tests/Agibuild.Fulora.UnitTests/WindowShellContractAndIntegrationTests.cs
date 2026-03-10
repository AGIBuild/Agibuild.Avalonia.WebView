using System.Reflection;
using Agibuild.Fulora;
using Avalonia.Controls;
using AvaloniAiChat.Bridge.Services;
using AvaloniAiChat.Desktop;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WindowShellContractAndIntegrationTests
{
    [Fact]
    public async Task Contract_update_snapshot_stream_roundtrip_is_deterministic()
    {
        var service = new FakeWindowShellService();
        using var cts = new CancellationTokenSource();
        var stream = service.StreamWindowShellState(cts.Token).GetAsyncEnumerator();
        try
        {
            Assert.True(await stream.MoveNextAsync());
            var initial = stream.Current;

            var updated = await service.UpdateWindowShellSettings(new WindowShellSettings
            {
                ThemePreference = "classic",
                EnableTransparency = false,
                GlassOpacityPercent = 66
            });

            var streamed = await ReadNextWithinAsync(stream, TimeSpan.FromSeconds(1));
            Assert.NotNull(streamed);

            Assert.Equal("classic", updated.EffectiveThemeMode);
            Assert.Equal("classic", streamed!.EffectiveThemeMode);
            Assert.False(streamed.Settings.EnableTransparency);
            Assert.Equal(66, streamed.Settings.GlassOpacityPercent);
            Assert.Equal(66, streamed.Capabilities.AppliedOpacityPercent);
            Assert.NotEqual(initial.Settings.EnableTransparency, streamed.Settings.EnableTransparency);
        }
        finally
        {
            cts.Cancel();
        }
    }

    [Fact]
    public async Task Contract_stream_deduplicates_equivalent_signatures()
    {
        var service = new FakeWindowShellService();
        using var cts = new CancellationTokenSource();
        var stream = service.StreamWindowShellState(cts.Token).GetAsyncEnumerator();
        try
        {
            Assert.True(await stream.MoveNextAsync());
            var first = stream.Current;

            await service.UpdateWindowShellSettings(new WindowShellSettings
            {
                ThemePreference = first.Settings.ThemePreference,
                EnableTransparency = first.Settings.EnableTransparency,
                GlassOpacityPercent = first.Settings.GlassOpacityPercent
            });

            var duplicate = await ReadNextWithinAsync(stream, TimeSpan.FromMilliseconds(280));
            Assert.Null(duplicate);
        }
        finally
        {
            cts.Cancel();
        }
    }

    [Fact]
    public void Drag_region_interactive_exclusion_is_coded_in_host()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "samples",
            "avalonia-ai-chat",
            "AvaloniAiChat.Desktop",
            "MainWindow.axaml.cs"));

        Assert.Contains("IsInteractiveChromeSource", source, StringComparison.Ordinal);
        Assert.Contains("Button or Avalonia.Controls.Primitives.ToggleButton or TextBox or ComboBox or Slider", source, StringComparison.Ordinal);
        Assert.Contains("Name: \"DragRegion\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Mac_chrome_drag_and_safe_inset_paths_are_wired_end_to_end()
    {
        var repoRoot = FindRepoRoot();
        var appTsx = File.ReadAllText(Path.Combine(
            repoRoot,
            "samples",
            "avalonia-ai-chat",
            "AvaloniAiChat.Web",
            "src",
            "App.tsx"));
        var css = File.ReadAllText(Path.Combine(
            repoRoot,
            "samples",
            "avalonia-ai-chat",
            "AvaloniAiChat.Web",
            "src",
            "index.css"));
        var mainWindowAxaml = File.ReadAllText(Path.Combine(
            repoRoot,
            "samples",
            "avalonia-ai-chat",
            "AvaloniAiChat.Desktop",
            "MainWindow.axaml"));

        Assert.Contains("'--ag-shell-top-inset'", appTsx, StringComparison.Ordinal);
        Assert.Contains("var(--ag-shell-top-inset, 0px)", css, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DragRegion\"", mainWindowAxaml, StringComparison.Ordinal);
        Assert.Contains("<Grid.RowDefinitions>", mainWindowAxaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Row=\"0\"", mainWindowAxaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Transparency_mapping_emits_deterministic_validation_messages()
    {
        var method = typeof(AppearanceService).GetMethod(
            "BuildTransparencyValidationMessage",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var disabled = InvokeMessage(method!, enabled: false, supportsTransparency: false, level: "none");
        var active = InvokeMessage(method!, enabled: true, supportsTransparency: true, level: "blur");
        var noneFallback = InvokeMessage(method!, enabled: true, supportsTransparency: false, level: "none");
        var unknownFallback = InvokeMessage(method!, enabled: true, supportsTransparency: false, level: "unknown");

        Assert.Contains("disabled", disabled, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("active", active, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("none", noneFallback, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("inconclusive", unknownFallback, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("AcrylicBlur", "acrylicblur")]
    [InlineData("Mica,Blur", "mica")]
    [InlineData("Blur,Transparent", "blur")]
    [InlineData("Transparent", "transparent")]
    [InlineData("Translucent", "transparent")]
    [InlineData("None", "none")]
    [InlineData("unknown-level", "unknown")]
    public void Transparency_level_normalization_handles_cross_platform_tokens(string raw, string expected)
    {
        var method = typeof(AppearanceService).GetMethod(
            "NormalizeTransparencyLevel",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var normalized = (string)(method!.Invoke(null, [raw]) ?? string.Empty);
        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void Transparency_hint_builder_returns_non_none_levels()
    {
        var method = typeof(AppearanceService).GetMethod(
            "BuildTransparencyLevelHint",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var hints = (Array?)method!.Invoke(null, null);
        Assert.NotNull(hints);
        Assert.NotEmpty(hints!.Cast<object>());
        Assert.DoesNotContain(hints.Cast<object>(), h => h is WindowTransparencyLevel level && level == WindowTransparencyLevel.None);
    }

    private static async Task<WindowShellState?> ReadNextWithinAsync(
        IAsyncEnumerator<WindowShellState> stream,
        TimeSpan timeout)
    {
        var moveTask = stream.MoveNextAsync().AsTask();
        var completed = await Task.WhenAny(moveTask, Task.Delay(timeout));
        if (completed != moveTask)
            return null;

        return await moveTask ? stream.Current : null;
    }

    private static string InvokeMessage(MethodInfo method, bool enabled, bool supportsTransparency, string level)
        => (string)(method.Invoke(null, [enabled, supportsTransparency, level]) ?? string.Empty);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Agibuild.Fulora.sln")))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed class FakeWindowShellService : IWindowShellService
    {
        private readonly object _gate = new();
        private readonly SemaphoreSlim _signal = new(0, 64);
        private WindowShellState _state = BuildState(new WindowShellSettings());

        public Task<WindowShellState> GetWindowShellState()
        {
            lock (_gate)
                return Task.FromResult(_state);
        }

        public Task<WindowShellState> UpdateWindowShellSettings(WindowShellSettings settings)
        {
            lock (_gate)
                _state = BuildState(settings);

            try
            {
                _signal.Release();
            }
            catch (SemaphoreFullException)
            {
                // No-op for test fake.
            }

            lock (_gate)
                return Task.FromResult(_state);
        }

        public async IAsyncEnumerable<WindowShellState> StreamWindowShellState(CancellationToken cancellationToken = default)
        {
            WindowShellState current;
            lock (_gate)
                current = _state;
            var lastSignature = BuildSignature(current);
            yield return current;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var signaled = await _signal.WaitAsync(TimeSpan.FromMilliseconds(800), cancellationToken);
                    if (!signaled)
                        continue;
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }

                lock (_gate)
                    current = _state;
                var signature = BuildSignature(current);
                if (!string.Equals(signature, lastSignature, StringComparison.Ordinal))
                {
                    lastSignature = signature;
                    yield return current;
                }
            }
        }

        private static string BuildSignature(WindowShellState state)
            => $"{state.Settings.ThemePreference}|{state.Settings.EnableTransparency}|{state.Settings.GlassOpacityPercent}|{state.EffectiveThemeMode}|{state.Capabilities.EffectiveTransparencyLevel}|{state.Capabilities.IsTransparencyEffective}|{state.ChromeMetrics.TitleBarHeight}|{state.ChromeMetrics.SafeInsets.Top}|{state.ChromeMetrics.SafeInsets.Right}|{state.ChromeMetrics.SafeInsets.Bottom}|{state.ChromeMetrics.SafeInsets.Left}";

        private static WindowShellState BuildState(WindowShellSettings settings)
        {
            var normalizedTheme = settings.ThemePreference?.Trim().ToLowerInvariant() switch
            {
                "classic" => "classic",
                "liquid" => "liquid",
                _ => "system"
            };
            var effectiveTheme = normalizedTheme == "system" ? "liquid" : normalizedTheme;
            var opacity = Math.Clamp(settings.GlassOpacityPercent, 20, 95);
            var effectiveLevel = settings.EnableTransparency ? "blur" : "none";
            var supportsTransparency = effectiveLevel is "blur" or "transparent" or "acrylicblur" or "mica";

            return new WindowShellState
            {
                Settings = new WindowShellSettings
                {
                    ThemePreference = normalizedTheme,
                    EnableTransparency = settings.EnableTransparency,
                    GlassOpacityPercent = opacity
                },
                EffectiveThemeMode = effectiveTheme,
                Capabilities = new WindowShellCapabilities
                {
                    Platform = "test",
                    SupportsTransparency = supportsTransparency,
                    IsTransparencyEnabled = settings.EnableTransparency,
                    IsTransparencyEffective = settings.EnableTransparency && supportsTransparency,
                    EffectiveTransparencyLevel = effectiveLevel,
                    ValidationMessage = settings.EnableTransparency
                        ? $"Transparency is active. Effective level: {effectiveLevel}."
                        : "Transparency is disabled in appearance settings.",
                    AppliedOpacityPercent = opacity
                },
                ChromeMetrics = new WindowChromeMetrics
                {
                    TitleBarHeight = 44,
                    DragRegionHeight = 44,
                    SafeInsets = new WindowSafeInsets { Top = 0, Right = 0, Bottom = 0, Left = 0 }
                }
            };
        }
    }
}
