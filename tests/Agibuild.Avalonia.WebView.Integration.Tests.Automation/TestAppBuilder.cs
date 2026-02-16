using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Themes.Fluent;
using Xunit;

[assembly: global::Avalonia.Headless.XUnit.AvaloniaTestApplication(typeof(Agibuild.Avalonia.WebView.Integration.Tests.Automation.TestAppBuilder))]
[assembly: AssemblyFixture(typeof(global::Avalonia.Headless.XUnit.AvaloniaHeadlessFixture))]
[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

public sealed class TestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }
}

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
