using System.Reflection;
using Agibuild.Fulora;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class ShellCollaboratorCoverageTests
{
    [Fact]
    public void WebViewManagedWindowManager_ctor_null_guards()
    {
        using var webView = CreateCore();
        var options = new WebViewShellExperienceOptions();
        var executor = CreateExecutor(webView, options);

        Assert.Throws<ArgumentNullException>(() =>
            new WebViewManagedWindowManager(
                options: null!,
                Guid.NewGuid(),
                null,
                null,
                executor,
                (_, _) => { },
                (_, _, _, _, _, _, _) => { },
                _ => { }));

        Assert.Throws<ArgumentNullException>(() =>
            new WebViewManagedWindowManager(
                options,
                Guid.NewGuid(),
                null,
                null,
                policyExecutor: null!,
                (_, _) => { },
                (_, _, _, _, _, _, _) => { },
                _ => { }));

        Assert.Throws<ArgumentNullException>(() =>
            new WebViewManagedWindowManager(
                options,
                Guid.NewGuid(),
                null,
                null,
                executor,
                reportPolicyFailure: null!,
                (_, _, _, _, _, _, _) => { },
                _ => { }));

        Assert.Throws<ArgumentNullException>(() =>
            new WebViewManagedWindowManager(
                options,
                Guid.NewGuid(),
                null,
                null,
                executor,
                (_, _) => { },
                raiseSessionPermissionProfileDiagnostic: null!,
                _ => { }));

        Assert.Throws<ArgumentNullException>(() =>
            new WebViewManagedWindowManager(
                options,
                Guid.NewGuid(),
                null,
                null,
                executor,
                (_, _) => { },
                (_, _, _, _, _, _, _) => { },
                onManagedWindowLifecycleChanged: null!));
    }

    [Fact]
    public void WebViewNewWindowHandler_ctor_null_guards()
    {
        using var webView = CreateCore();
        var options = new WebViewShellExperienceOptions();
        var executor = CreateExecutor(webView, options);
        var manager = CreateManager(options, executor);

        Assert.Throws<ArgumentNullException>(() =>
            new WebViewNewWindowHandler(
                webView: null!,
                options,
                Guid.NewGuid(),
                manager,
                executor,
                (_, _) => { }));

        Assert.Throws<ArgumentNullException>(() =>
            new WebViewNewWindowHandler(
                webView,
                options: null!,
                Guid.NewGuid(),
                manager,
                executor,
                (_, _) => { }));

        Assert.Throws<ArgumentNullException>(() =>
            new WebViewNewWindowHandler(
                webView,
                options,
                Guid.NewGuid(),
                managedWindowManager: null!,
                executor,
                (_, _) => { }));

        Assert.Throws<ArgumentNullException>(() =>
            new WebViewNewWindowHandler(
                webView,
                options,
                Guid.NewGuid(),
                manager,
                policyExecutor: null!,
                (_, _) => { }));

        Assert.Throws<ArgumentNullException>(() =>
            new WebViewNewWindowHandler(
                webView,
                options,
                Guid.NewGuid(),
                manager,
                executor,
                reportPolicyFailure: null!));
    }

    [Fact]
    public void WebViewManagedWindowManager_IsTransitionAllowed_branch_matrix()
    {
        var method = typeof(WebViewManagedWindowManager)
            .GetMethod("IsTransitionAllowed", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        Assert.True(InvokeIsTransitionAllowed(method!, null, WebViewManagedWindowLifecycleState.Created));
        Assert.False(InvokeIsTransitionAllowed(method!, null, WebViewManagedWindowLifecycleState.Ready));

        Assert.True(InvokeIsTransitionAllowed(method!, WebViewManagedWindowLifecycleState.Created, WebViewManagedWindowLifecycleState.Attached));
        Assert.True(InvokeIsTransitionAllowed(method!, WebViewManagedWindowLifecycleState.Created, WebViewManagedWindowLifecycleState.Closing));
        Assert.False(InvokeIsTransitionAllowed(method!, WebViewManagedWindowLifecycleState.Created, WebViewManagedWindowLifecycleState.Ready));

        Assert.True(InvokeIsTransitionAllowed(method!, WebViewManagedWindowLifecycleState.Attached, WebViewManagedWindowLifecycleState.Ready));
        Assert.True(InvokeIsTransitionAllowed(method!, WebViewManagedWindowLifecycleState.Attached, WebViewManagedWindowLifecycleState.Closing));
        Assert.False(InvokeIsTransitionAllowed(method!, WebViewManagedWindowLifecycleState.Attached, WebViewManagedWindowLifecycleState.Created));

        Assert.True(InvokeIsTransitionAllowed(method!, WebViewManagedWindowLifecycleState.Ready, WebViewManagedWindowLifecycleState.Closing));
        Assert.False(InvokeIsTransitionAllowed(method!, WebViewManagedWindowLifecycleState.Ready, WebViewManagedWindowLifecycleState.Attached));

        Assert.True(InvokeIsTransitionAllowed(method!, WebViewManagedWindowLifecycleState.Closing, WebViewManagedWindowLifecycleState.Closed));
        Assert.False(InvokeIsTransitionAllowed(method!, WebViewManagedWindowLifecycleState.Closing, WebViewManagedWindowLifecycleState.Ready));

        Assert.False(InvokeIsTransitionAllowed(method!, WebViewManagedWindowLifecycleState.Closed, WebViewManagedWindowLifecycleState.Created));
    }

    [Fact]
    public void WebViewManagedWindowManager_TryTransitionManagedWindowState_invalid_transition_reports_failure()
    {
        using var webView = CreateCore();
        var options = new WebViewShellExperienceOptions();
        var executor = CreateExecutor(webView, options);

        var reported = new List<Exception>();
        var manager = new WebViewManagedWindowManager(
            options,
            Guid.NewGuid(),
            null,
            null,
            executor,
            (_, ex) => reported.Add(ex),
            (_, _, _, _, _, _, _) => { },
            _ => { });

        var entryType = typeof(WebViewManagedWindowManager).GetNestedType("ManagedWindowEntry", BindingFlags.NonPublic);
        Assert.NotNull(entryType);
        var ctor = entryType!.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
        var entry = ctor.Invoke([Guid.NewGuid(), Guid.NewGuid(), webView, null!, null!]);
        entryType.GetProperty("State")!.SetValue(entry, WebViewManagedWindowLifecycleState.Ready);

        var method = typeof(WebViewManagedWindowManager)
            .GetMethod("TryTransitionManagedWindowState", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var ok = (bool)method!.Invoke(manager, [entry, WebViewManagedWindowLifecycleState.Attached])!;
        Assert.False(ok);
        Assert.NotEmpty(reported);
    }

    private static bool InvokeIsTransitionAllowed(MethodInfo method, WebViewManagedWindowLifecycleState? from, WebViewManagedWindowLifecycleState to)
        => (bool)method.Invoke(null, [from, to])!;

    private static WebViewCore CreateCore()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateFull();
        var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));
        return core;
    }

    private static WebViewHostCapabilityExecutor CreateExecutor(IWebView webView, WebViewShellExperienceOptions options)
        => new(
            webView,
            options,
            Guid.NewGuid(),
            normalizeMenuModel: model => model,
            tryPruneMenuModel: _ => (null, null),
            updateEffectiveMenuModel: _ => { },
            isSystemActionWhitelisted: _ => true,
            reportPolicyFailure: (_, _) => { });

    private static WebViewManagedWindowManager CreateManager(
        WebViewShellExperienceOptions options,
        WebViewHostCapabilityExecutor executor)
        => new(
            options,
            Guid.NewGuid(),
            null,
            null,
            executor,
            (_, _) => { },
            (_, _, _, _, _, _, _) => { },
            _ => { });
}
