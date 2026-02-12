using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Agibuild.Avalonia.WebView.Bridge.Generator;

/// <summary>
/// Extracts <see cref="BridgeInterfaceModel"/> from Roslyn semantic model.
/// </summary>
internal static class ModelExtractor
{
    public static BridgeInterfaceModel? Extract(
        INamedTypeSymbol interfaceSymbol,
        AttributeData attribute,
        BridgeDirection direction)
    {
        if (interfaceSymbol.TypeKind != TypeKind.Interface)
            return null;

        var serviceName = GetServiceName(attribute, interfaceSymbol);
        var methods = ExtractMethods(interfaceSymbol, serviceName);

        return new BridgeInterfaceModel
        {
            Namespace = interfaceSymbol.ContainingNamespace.IsGlobalNamespace
                ? ""
                : interfaceSymbol.ContainingNamespace.ToDisplayString(),
            InterfaceFullName = interfaceSymbol.ToDisplayString(),
            InterfaceName = interfaceSymbol.Name,
            ServiceName = serviceName,
            Direction = direction,
            Methods = methods,
        };
    }

    private static string GetServiceName(AttributeData attribute, INamedTypeSymbol interfaceSymbol)
    {
        foreach (var namedArg in attribute.NamedArguments)
        {
            if (namedArg.Key == "Name" && namedArg.Value.Value is string name && name.Length > 0)
                return name;
        }

        var ifaceName = interfaceSymbol.Name;
        if (ifaceName.Length > 1 && ifaceName[0] == 'I' && char.IsUpper(ifaceName[1]))
            return ifaceName.Substring(1);
        return ifaceName;
    }

    private static ImmutableArray<BridgeMethodModel> ExtractMethods(INamedTypeSymbol interfaceSymbol, string serviceName)
    {
        var builder = ImmutableArray.CreateBuilder<BridgeMethodModel>();

        foreach (var member in interfaceSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;
            if (method.MethodKind != MethodKind.Ordinary) continue;

            var camelName = ToCamelCase(method.Name);
            var rpcMethodName = $"{serviceName}.{camelName}";

            var isAsync = IsTaskType(method.ReturnType);
            var hasReturnValue = false;
            string? innerReturnType = null;

            if (isAsync && method.ReturnType is INamedTypeSymbol namedReturn && namedReturn.IsGenericType)
            {
                hasReturnValue = true;
                innerReturnType = namedReturn.TypeArguments[0].ToDisplayString();
            }
            else if (!isAsync && !method.ReturnsVoid)
            {
                hasReturnValue = true;
                innerReturnType = method.ReturnType.ToDisplayString();
            }

            var parameters = ExtractParameters(method);

            builder.Add(new BridgeMethodModel
            {
                Name = method.Name,
                CamelCaseName = camelName,
                RpcMethodName = rpcMethodName,
                ReturnTypeFullName = method.ReturnType.ToDisplayString(),
                IsAsync = isAsync,
                HasReturnValue = hasReturnValue,
                InnerReturnTypeFullName = innerReturnType,
                Parameters = parameters,
            });
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<BridgeParameterModel> ExtractParameters(IMethodSymbol method)
    {
        var builder = ImmutableArray.CreateBuilder<BridgeParameterModel>();

        foreach (var param in method.Parameters)
        {
            builder.Add(new BridgeParameterModel
            {
                Name = param.Name,
                CamelCaseName = ToCamelCase(param.Name),
                TypeFullName = param.Type.ToDisplayString(),
                IsNullable = param.NullableAnnotation == NullableAnnotation.Annotated,
                HasDefaultValue = param.HasExplicitDefaultValue,
                DefaultValueLiteral = param.HasExplicitDefaultValue
                    ? GetDefaultValueLiteral(param)
                    : null,
            });
        }

        return builder.ToImmutable();
    }

    private static string? GetDefaultValueLiteral(IParameterSymbol param)
    {
        if (!param.HasExplicitDefaultValue) return null;
        var value = param.ExplicitDefaultValue;
        if (value is null) return "null";
        if (value is string s) return $"\"{s}\"";
        if (value is bool b) return b ? "true" : "false";
        return value.ToString();
    }

    private static bool IsTaskType(ITypeSymbol type)
    {
        return type.Name == "Task" &&
               type.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";
    }

    internal static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (char.IsLower(name[0])) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
