using System.Collections.Immutable;
using System.Text.Json;
using Agibuild.Fulora.Bridge.Generator;
using Agibuild.Fulora.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

// ==================== Test interfaces & DTOs ====================

public record Notification(string Title, string Body);

[JsExport]
public interface INotificationService
{
    Task<int> GetUnreadCount();
    IBridgeEvent<Notification> OnNewNotification { get; }
}

public class FakeNotificationService : INotificationService
{
    private readonly BridgeEvent<Notification> _onNew = new();
    public IBridgeEvent<Notification> OnNewNotification => _onNew;
    public BridgeEvent<Notification> OnNewNotificationInternal => _onNew;
    public Task<int> GetUnreadCount() => Task.FromResult(42);
}

[JsExport]
public interface IMultiEventService
{
    IBridgeEvent<string> OnMessage { get; }
    IBridgeEvent<int> OnCount { get; }
    Task Ping();
}

public class FakeMultiEventService : IMultiEventService
{
    private readonly BridgeEvent<string> _onMessage = new();
    private readonly BridgeEvent<int> _onCount = new();
    public IBridgeEvent<string> OnMessage => _onMessage;
    public IBridgeEvent<int> OnCount => _onCount;
    public BridgeEvent<string> OnMessageInternal => _onMessage;
    public BridgeEvent<int> OnCountInternal => _onCount;
    public Task Ping() => Task.CompletedTask;
}

// ==================== BridgeEvent<T> unit tests ====================

public sealed class BridgeEventUnitTests
{
    [Fact]
    public void Emit_without_Connect_is_noop()
    {
        var evt = new BridgeEvent<string>();
        evt.Emit("test");
    }

    [Fact]
    public void Emit_after_Connect_invokes_handler()
    {
        var evt = new BridgeEvent<string>();
        string? received = null;
        evt.Connect(payload => received = payload);

        evt.Emit("hello");

        Assert.Equal("hello", received);
    }

    [Fact]
    public void Disconnect_stops_delivery()
    {
        var evt = new BridgeEvent<string>();
        int callCount = 0;
        evt.Connect(_ => callCount++);

        evt.Emit("a");
        Assert.Equal(1, callCount);

        evt.Disconnect();
        evt.Emit("b");
        Assert.Equal(1, callCount);
    }
}

// ==================== Source Generator & integration tests ====================

public sealed class BridgeEventGeneratorTests
{
    private readonly TestDispatcher _dispatcher = new();

    private (WebViewCore Core, MockWebViewAdapter Adapter, List<string> Scripts) CreateCore()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string> { "*" }
        });
        var scripts = new List<string>();
        adapter.ScriptCallback = script => { scripts.Add(script); return null; };
        return (core, adapter, scripts);
    }

    // ==================== Assembly attribute discovery ====================

    [Fact]
    public void Assembly_has_BridgeRegistration_for_event_service()
    {
        var assembly = typeof(INotificationService).Assembly;
        var attrs = assembly.GetCustomAttributes(typeof(BridgeRegistrationAttribute), false)
            .Cast<BridgeRegistrationAttribute>();

        var attr = attrs.FirstOrDefault(a => a.InterfaceType == typeof(INotificationService));
        Assert.NotNull(attr);
    }

    // ==================== JS stub tests ====================

    [Fact]
    public void Generated_JS_stub_includes_event_subscribe_protocol()
    {
        var registration = GetRegistration<INotificationService>();
        var stub = registration.GetJsStub();

        Assert.Contains("onNewNotification", stub);
        Assert.Contains("NotificationService.$subscribe.onNewNotification", stub);
        Assert.Contains("NotificationService.$unsubscribe.onNewNotification", stub);
        Assert.Contains("NotificationService.$event.onNewNotification", stub);
    }

    [Fact]
    public void Generated_JS_stub_event_has_on_and_off_methods()
    {
        var registration = GetRegistration<INotificationService>();
        var stub = registration.GetJsStub();

        Assert.Contains("on: function(h)", stub);
        Assert.Contains("off: function(h)", stub);
    }

    [Fact]
    public void Generated_JS_stub_includes_all_events_for_multi_event_service()
    {
        var registration = GetRegistration<IMultiEventService>();
        var stub = registration.GetJsStub();

        Assert.Contains("onMessage", stub);
        Assert.Contains("onCount", stub);
        Assert.Contains("MultiEventService.$subscribe.onMessage", stub);
        Assert.Contains("MultiEventService.$subscribe.onCount", stub);
    }

    // ==================== Subscribe/unsubscribe RPC handlers ====================

    [Fact]
    public void Subscribe_handler_responds_with_true()
    {
        var (core, adapter, scripts) = CreateCore();
        var impl = new FakeNotificationService();
        core.Bridge.Expose<INotificationService>(impl);
        _dispatcher.RunAll();
        scripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"s-1","method":"NotificationService.$subscribe.onNewNotification","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.True(scripts.Any(s => s.Contains("s-1") && s.Contains("true")),
            "Expected subscribe response with result:true");
    }

    [Fact]
    public void Unsubscribe_handler_responds_with_true()
    {
        var (core, adapter, scripts) = CreateCore();
        var impl = new FakeNotificationService();
        core.Bridge.Expose<INotificationService>(impl);
        _dispatcher.RunAll();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"s-1","method":"NotificationService.$subscribe.onNewNotification","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();
        scripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"u-1","method":"NotificationService.$unsubscribe.onNewNotification","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.True(scripts.Any(s => s.Contains("u-1") && s.Contains("true")),
            "Expected unsubscribe response with result:true");
    }

    // ==================== Event emission tests ====================

    [Fact]
    public void Emit_after_subscribe_sends_notification_via_script()
    {
        var (core, adapter, scripts) = CreateCore();
        var impl = new FakeNotificationService();
        core.Bridge.Expose<INotificationService>(impl);
        _dispatcher.RunAll();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"s-1","method":"NotificationService.$subscribe.onNewNotification","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();
        scripts.Clear();

        impl.OnNewNotificationInternal.Emit(new Notification("Hello", "World"));
        _dispatcher.RunAll();

        Assert.True(scripts.Any(s => s.Contains("$event.onNewNotification")),
            "Expected event notification script after Emit");
    }

    [Fact]
    public void Emit_without_subscribe_does_not_send_notification()
    {
        var (core, adapter, scripts) = CreateCore();
        var impl = new FakeNotificationService();
        core.Bridge.Expose<INotificationService>(impl);
        _dispatcher.RunAll();
        scripts.Clear();

        impl.OnNewNotificationInternal.Emit(new Notification("Ignored", "Test"));
        _dispatcher.RunAll();

        Assert.DoesNotContain(scripts, s => s.Contains("$event.onNewNotification"));
    }

    [Fact]
    public void Emit_after_unsubscribe_does_not_send_notification()
    {
        var (core, adapter, scripts) = CreateCore();
        var impl = new FakeNotificationService();
        core.Bridge.Expose<INotificationService>(impl);
        _dispatcher.RunAll();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"s-1","method":"NotificationService.$subscribe.onNewNotification","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"u-1","method":"NotificationService.$unsubscribe.onNewNotification","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();
        scripts.Clear();

        impl.OnNewNotificationInternal.Emit(new Notification("Gone", "Test"));
        _dispatcher.RunAll();

        Assert.DoesNotContain(scripts, s => s.Contains("$event.onNewNotification"));
    }

    // ==================== Remove disconnects events ====================

    [Fact]
    public void Remove_disconnects_event_channels()
    {
        var (core, adapter, scripts) = CreateCore();
        var impl = new FakeNotificationService();
        core.Bridge.Expose<INotificationService>(impl);
        _dispatcher.RunAll();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"s-1","method":"NotificationService.$subscribe.onNewNotification","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        core.Bridge.Remove<INotificationService>();
        scripts.Clear();

        impl.OnNewNotificationInternal.Emit(new Notification("After", "Remove"));
        _dispatcher.RunAll();

        Assert.DoesNotContain(scripts, s => s.Contains("$event"));
    }

    // ==================== NotifyAsync sends notification without id ====================

    [Fact]
    public void NotifyAsync_sends_notification_without_id_field()
    {
        var (core, _, scripts) = CreateCore();
        _dispatcher.RunAll();
        scripts.Clear();

        _ = core.Rpc!.NotifyAsync("test.event", new { value = 42 });
        _dispatcher.RunAll();

        Assert.True(scripts.Count > 0, "Expected script invocation");
        var script = scripts.First(s => s.Contains("test.event"));
        Assert.Contains("_dispatch", script);
        Assert.DoesNotContain("\"id\"", script);
    }

    // ==================== Regular methods still work alongside events ====================

    [Fact]
    public void Regular_methods_work_alongside_event_properties()
    {
        var (core, adapter, scripts) = CreateCore();
        var impl = new FakeNotificationService();
        core.Bridge.Expose<INotificationService>(impl);
        _dispatcher.RunAll();
        scripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"r-1","method":"NotificationService.getUnreadCount","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.True(scripts.Any(s => s.Contains("42")), "Expected getUnreadCount to return 42");
    }

    // ==================== Helpers ====================

    private static IBridgeServiceRegistration<T> GetRegistration<T>() where T : class
    {
        var assembly = typeof(T).Assembly;
        var attr = assembly.GetCustomAttributes(typeof(BridgeRegistrationAttribute), false)
            .Cast<BridgeRegistrationAttribute>()
            .First(a => a.InterfaceType == typeof(T));

        return (IBridgeServiceRegistration<T>)Activator.CreateInstance(attr.RegistrationType)!;
    }
}

// ==================== AGBR007 Diagnostic Tests ====================

public sealed class BridgeEventDiagnosticsTests
{
    private static (ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult Result) RunGenerator(string source)
    {
        var coreAssembly = typeof(JsExportAttribute).Assembly;
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(coreAssembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
        };

        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references =
        [
            .. references,
            MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
        ];

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new WebViewBridgeGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        return (diagnostics, driver.GetRunResult());
    }

    [Fact]
    public void IBridgeEvent_on_JsExport_produces_no_AGBR007()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IMyService
            {
                Task DoWork();
                IBridgeEvent<string> OnDone { get; }
            }
            """;

        var (diagnostics, _) = RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "AGBR007");
    }

    [Fact]
    public void IBridgeEvent_on_JsImport_reports_AGBR007()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsImport]
            public interface IMyImport
            {
                Task DoWork();
                IBridgeEvent<string> OnDone { get; }
            }
            """;

        var (diagnostics, _) = RunGenerator(source);
        Assert.Contains(diagnostics, d => d.Id == "AGBR007");
    }

    [Fact]
    public void AGBR007_message_includes_interface_and_property_names()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsImport]
            public interface IBadImport
            {
                IBridgeEvent<int> OnData { get; }
            }
            """;

        var (diagnostics, _) = RunGenerator(source);
        var diag = diagnostics.First(d => d.Id == "AGBR007");
        var msg = diag.GetMessage();
        Assert.Contains("IBadImport", msg);
        Assert.Contains("OnData", msg);
    }
}

// ==================== TypeScript declaration tests ====================

public sealed class BridgeEventTypeScriptTests
{
    private static (ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult Result) RunGenerator(string source)
    {
        var coreAssembly = typeof(JsExportAttribute).Assembly;
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(coreAssembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
        };

        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references =
        [
            .. references,
            MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
        ];

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new WebViewBridgeGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        return (diagnostics, driver.GetRunResult());
    }

    [Fact]
    public void TypeScript_declarations_include_BridgeEvent_utility_interface()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IEventService
            {
                Task DoWork();
                IBridgeEvent<string> OnDone { get; }
            }
            """;

        var (_, result) = RunGenerator(source);

        var tsFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("BridgeTypeScriptDeclarations"));
        Assert.NotNull(tsFile);

        var text = tsFile!.GetText().ToString();
        Assert.Contains("BridgeEvent<T>", text);
        Assert.Contains("on(handler: (payload: T) => void): () => void", text);
        Assert.Contains("off(handler: (payload: T) => void): void", text);
    }

    [Fact]
    public void TypeScript_service_interface_maps_event_to_BridgeEvent()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IEventService
            {
                Task DoWork();
                IBridgeEvent<string> OnDone { get; }
            }
            """;

        var (_, result) = RunGenerator(source);

        var tsFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("BridgeTypeScriptDeclarations"));
        Assert.NotNull(tsFile);

        var text = tsFile!.GetText().ToString();
        Assert.Contains("onDone: BridgeEvent<string>", text);
    }
}
