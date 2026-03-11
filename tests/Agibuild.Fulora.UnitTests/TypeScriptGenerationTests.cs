using System.Collections.Immutable;
using System.Reflection;
using Agibuild.Fulora.Bridge.Generator;
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
    public void All_field_includes_DTO_interfaces()
    {
        var content = GetAllContent();
        Assert.NotNull(content);

        Assert.Contains("export interface UserProfile {", content);
        Assert.Contains("export interface AppSettings {", content);
        Assert.Contains("export interface Item {", content);
    }

    [Fact]
    public void DTO_properties_use_camelCase()
    {
        var content = GetAllContent();
        Assert.NotNull(content);

        Assert.Contains("name: string;", content);
        Assert.Contains("darkMode: boolean;", content);
    }

    [Fact]
    public void Client_field_contains_typed_proxies()
    {
        var content = GetFieldContent("Client");
        Assert.NotNull(content);

        Assert.Contains("Auto-generated", content);
        Assert.Contains("export const appService", content);
        Assert.Contains("getCurrentUser", content);
        Assert.Contains("saveSettings", content);
        Assert.Contains("AppService.getCurrentUser", content);
    }

    [Fact]
    public void Mock_field_contains_mock_installer()
    {
        var content = GetFieldContent("Mock");
        Assert.NotNull(content);

        Assert.Contains("installBridgeMock", content);
        Assert.Contains("handlers", content);
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

    [Fact]
    public void EmitDeclarations_emits_Client_and_Mock_even_without_DTOs()
    {
        var export = new BridgeInterfaceModel
        {
            Namespace = "TestNs",
            InterfaceName = "IPingService",
            ServiceName = "PingService",
            Direction = BridgeDirection.Export,
            Methods = ImmutableArray.Create(new BridgeMethodModel
            {
                Name = "Ping",
                CamelCaseName = "ping",
                RpcMethodName = "PingService.ping",
                ReturnTypeFullName = "System.Threading.Tasks.Task",
                IsAsync = true,
                HasReturnValue = false,
                Parameters = ImmutableArray<BridgeParameterModel>.Empty,
            }),
        };

        var source = TypeScriptEmitter.EmitDeclarations([export], []);
        Assert.Contains("public const string Client", source);
        Assert.Contains("public const string Mock", source);
    }

    [Fact]
    public void Client_emission_prefers_TypeRef_semantics_over_raw_type_strings()
    {
        var stringRef = new BridgeTypeRef
        {
            Kind = BridgeTypeKind.String,
            FullName = "System.String",
            Name = "string",
        };

        var method = new BridgeMethodModel
        {
            Name = "Echo",
            CamelCaseName = "echo",
            RpcMethodName = "TypeRefService.echo",
            ReturnTypeFullName = "System.Threading.Tasks.Task<System.Int32>",
            IsAsync = true,
            HasReturnValue = true,
            InnerReturnTypeFullName = "System.Int32",
            InnerReturnTypeRef = stringRef, // Intentional mismatch with raw type string.
            Parameters = ImmutableArray.Create(new BridgeParameterModel
            {
                Name = "value",
                CamelCaseName = "value",
                TypeFullName = "System.Int32",
                TypeRef = stringRef, // Intentional mismatch with raw type string.
            }),
        };

        var ir = new BridgeContractModel
        {
            Services = ImmutableArray.Create(new BridgeInterfaceModel
            {
                ServiceName = "TypeRefService",
                Direction = BridgeDirection.Export,
                Methods = ImmutableArray.Create(method),
            }),
            Dtos = ImmutableArray<BridgeDtoModel>.Empty,
        };

        var clientSource = TypeScriptClientEmitter.EmitClient(ir);
        Assert.Contains("value: string", clientSource);
        Assert.Contains("Promise<string>", clientSource);
        Assert.DoesNotContain("value: number", clientSource);
    }

    private static string? GetTsConstant(string serviceName)
    {
        var type = Assembly.GetExecutingAssembly()
            .GetTypes()
            .First(t => t.Name == "BridgeTypeScriptDeclarations");

        var field = type.GetField(serviceName, BindingFlags.Public | BindingFlags.Static);
        return field?.GetValue(null) as string;
    }

    private static string? GetAllContent() => GetFieldContent("All");

    private static string? GetFieldContent(string fieldName)
    {
        var type = Assembly.GetExecutingAssembly()
            .GetTypes()
            .First(t => t.Name == "BridgeTypeScriptDeclarations");

        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        return field?.GetValue(null) as string;
    }
}
