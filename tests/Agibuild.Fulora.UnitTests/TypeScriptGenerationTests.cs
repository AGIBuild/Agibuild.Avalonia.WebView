using System.Reflection;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

/// <summary>
/// Tests that the Source Generator produces correct TypeScript declaration constants.
/// Deliverable 1.3.
/// </summary>
public sealed class TypeScriptGenerationTests
{
    [Fact]
    public void BridgeTypeScriptDeclarations_class_exists()
    {
        var type = Assembly.GetExecutingAssembly()
            .GetTypes()
            .FirstOrDefault(t => t.Name == "BridgeTypeScriptDeclarations");

        Assert.NotNull(type);
    }

    [Fact]
    public void All_field_contains_complete_dts_content()
    {
        var type = Assembly.GetExecutingAssembly()
            .GetTypes()
            .First(t => t.Name == "BridgeTypeScriptDeclarations");

        var allField = type.GetField("All", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(allField);

        var content = allField!.GetValue(null) as string;
        Assert.NotNull(content);
        Assert.Contains("Auto-generated", content);
    }

    [Fact]
    public void JsExport_interface_generates_TS_declaration_with_methods()
    {
        var content = GetTsConstant("AppService");
        Assert.NotNull(content);

        // Should contain method signatures in camelCase.
        Assert.Contains("getCurrentUser", content);
        Assert.Contains("saveSettings", content);
        Assert.Contains("C# service exposed to JS", content);
    }

    [Fact]
    public void JsImport_interface_generates_TS_declaration()
    {
        var content = GetTsConstant("UiController");
        Assert.NotNull(content);

        Assert.Contains("showNotification", content);
        Assert.Contains("confirmDialog", content);
        Assert.Contains("JS service callable from C#", content);
    }

    [Fact]
    public void TS_return_types_are_correctly_mapped()
    {
        var content = GetTsConstant("AppService")!;

        // getCurrentUser returns Task<UserProfile> → Promise<UserProfile>
        Assert.Contains("Promise<UserProfile>", content);

        // saveSettings returns Task (void) → Promise<void>
        Assert.Contains("Promise<void>", content);

        // searchItems returns Task<List<Item>> → Promise<Item[]>
        Assert.Contains("Promise<Item[]>", content);
    }

    [Fact]
    public void TS_parameter_types_are_correctly_mapped()
    {
        var content = GetTsConstant("UiController")!;

        // showNotification(string message, string? title) → message: string, title?: string
        Assert.Contains("message: string", content);
        Assert.Contains("title?: string", content);

        // confirmDialog(string prompt) → prompt: string
        Assert.Contains("prompt: string", content);
    }

    [Fact]
    public void All_field_includes_Window_augmentation()
    {
        var type = Assembly.GetExecutingAssembly()
            .GetTypes()
            .First(t => t.Name == "BridgeTypeScriptDeclarations");

        var allField = type.GetField("All", BindingFlags.Public | BindingFlags.Static);
        var content = allField!.GetValue(null) as string;

        Assert.Contains("declare global", content);
        Assert.Contains("interface Window", content);
        Assert.Contains("agWebView", content);
    }

    private static string? GetTsConstant(string serviceName)
    {
        var type = Assembly.GetExecutingAssembly()
            .GetTypes()
            .First(t => t.Name == "BridgeTypeScriptDeclarations");

        var field = type.GetField(serviceName, BindingFlags.Public | BindingFlags.Static);
        return field?.GetValue(null) as string;
    }
}
