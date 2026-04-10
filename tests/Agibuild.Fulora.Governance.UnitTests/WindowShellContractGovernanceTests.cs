using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WindowShellContractGovernanceTests
{
    [Fact]
    public void Ai_chat_titlebar_contract_paths_are_wired_end_to_end()
    {
        var repoRoot = FindRepoRoot();
        var mainWindowAxaml = File.ReadAllText(Path.Combine(
            repoRoot, "samples", "avalonia-ai-chat", "AvaloniAiChat.Desktop", "MainWindow.axaml"));
        var mainWindowCodeBehind = File.ReadAllText(Path.Combine(
            repoRoot, "samples", "avalonia-ai-chat", "AvaloniAiChat.Desktop", "MainWindow.axaml.cs"));
        var settingsStoreCode = File.ReadAllText(Path.Combine(
            repoRoot, "samples", "avalonia-ai-chat", "AvaloniAiChat.Desktop", "WindowShellSettingsStore.cs"));
        var appTsx = File.ReadAllText(Path.Combine(
            repoRoot, "samples", "avalonia-ai-chat", "AvaloniAiChat.Web", "src", "App.tsx"));
        var css = File.ReadAllText(Path.Combine(
            repoRoot, "samples", "avalonia-ai-chat", "AvaloniAiChat.Web", "src", "index.css"));
        var chromeProvider = File.ReadAllText(Path.Combine(
            repoRoot, "src", "Agibuild.Fulora.Avalonia", "Shell", "AvaloniaWindowChromeProvider.cs"));

        Assert.Contains("Title=\"Fulora AI Chat\"", mainWindowAxaml, StringComparison.Ordinal);
        Assert.Contains("CustomChrome = false", mainWindowCodeBehind, StringComparison.Ordinal);
        Assert.Contains("var settingsStore = new WindowShellSettingsStore();", mainWindowCodeBehind, StringComparison.Ordinal);
        Assert.Contains("await shellService.UpdateWindowShellSettings(persistedSettings);", mainWindowCodeBehind, StringComparison.Ordinal);
        Assert.Contains("settingsStore.Save(updated.Settings);", mainWindowCodeBehind, StringComparison.Ordinal);
        Assert.Contains("window-shell-settings.json", settingsStoreCode, StringComparison.Ordinal);
        Assert.Contains("<h1 className=\"chat-header__title\">Fulora AI Chat</h1>", appTsx, StringComparison.Ordinal);
        Assert.Contains("titleBarHeight = appearance?.chromeMetrics?.titleBarHeight ?? 28;", appTsx, StringComparison.Ordinal);
        Assert.Contains("--titlebar-h: 28px;", css, StringComparison.Ordinal);
        Assert.Contains("window.ExtendClientAreaToDecorationsHint = trackedWindow.Options.CustomChrome;", chromeProvider, StringComparison.Ordinal);
        Assert.Contains("ReadTransparencyLevelOnUIThread", chromeProvider, StringComparison.Ordinal);
        Assert.Contains("'--ag-shell-top-inset'", appTsx, StringComparison.Ordinal);
        Assert.Contains("var(--ag-shell-top-inset, 0px)", css, StringComparison.Ordinal);
    }

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
}
