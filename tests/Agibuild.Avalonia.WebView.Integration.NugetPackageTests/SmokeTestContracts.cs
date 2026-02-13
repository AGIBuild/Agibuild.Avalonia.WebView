using Agibuild.Avalonia.WebView;

namespace Agibuild.Avalonia.WebView.Integration.NugetPackageTests;

/// <summary>
/// Minimal [JsExport] interface to verify the Bridge.Generator source generator
/// produces a <c>SmokeExportServiceBridgeRegistration</c> class at compile time.
/// </summary>
[JsExport]
public interface ISmokeExportService
{
    Task<string> Ping();
}

public class SmokeExportService : ISmokeExportService
{
    public Task<string> Ping() => Task.FromResult("pong");
}

/// <summary>
/// Minimal [JsImport] interface to verify MockBridgeService.GetProxy&lt;T&gt;() works.
/// </summary>
[JsImport]
public interface ISmokeImportService
{
    Task Notify(string message);
}
