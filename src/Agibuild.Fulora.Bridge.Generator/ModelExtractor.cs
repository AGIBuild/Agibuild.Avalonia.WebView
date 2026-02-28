using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Agibuild.Fulora.Bridge.Generator;

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
        var events = ExtractEvents(interfaceSymbol);
        var diagnostics = ValidateInterface(interfaceSymbol, methods, events, direction);

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
            Events = events,
            ValidationErrors = diagnostics,
        };
    }

    private static ImmutableArray<BridgeDiagnosticInfo> ValidateInterface(
        INamedTypeSymbol interfaceSymbol,
        ImmutableArray<BridgeMethodModel> methods,
        ImmutableArray<BridgeEventModel> events,
        BridgeDirection direction)
    {
        var builder = ImmutableArray.CreateBuilder<BridgeDiagnosticInfo>();
        var interfaceName = interfaceSymbol.Name;

        if (interfaceSymbol.IsGenericType)
        {
            builder.Add(new BridgeDiagnosticInfo("AGBR006", interfaceName));
        }

        // Check for overloads with same visible parameter count (can't be disambiguated).
        var overloadGroups = methods
            .GroupBy(m => m.Name)
            .Where(g => g.Count() > 1);

        foreach (var group in overloadGroups)
        {
            var paramCountCollision = group
                .GroupBy(m => m.VisibleParameterCount)
                .Any(pc => pc.Count() > 1);

            if (paramCountCollision)
            {
                builder.Add(new BridgeDiagnosticInfo("AGBR002", interfaceName, group.Key));
            }
        }

        foreach (var member in interfaceSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method || method.MethodKind != MethodKind.Ordinary)
                continue;

            if (method.IsGenericMethod)
            {
                builder.Add(new BridgeDiagnosticInfo("AGBR001", method.Name));
            }

            foreach (var param in method.Parameters)
            {
                if (param.RefKind is RefKind.Ref or RefKind.Out or RefKind.In)
                {
                    builder.Add(new BridgeDiagnosticInfo("AGBR003", method.Name, param.RefKind.ToString().ToLowerInvariant(), param.Name));
                }

                // CancellationToken is now supported — no diagnostic needed
            }

            // IAsyncEnumerable is now supported — no diagnostic needed
        }

        if (direction == BridgeDirection.Import && events.Length > 0)
        {
            foreach (var evt in events)
            {
                builder.Add(new BridgeDiagnosticInfo("AGBR007", interfaceSymbol.Name, evt.PropertyName));
            }
        }

        return builder.ToImmutable();
    }

    private static bool IsCancellationTokenType(ITypeSymbol type)
    {
        return type.Name == "CancellationToken" &&
               type.ContainingNamespace?.ToDisplayString() == "System.Threading";
    }

    private static bool IsAsyncEnumerableType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            return named.OriginalDefinition.Name == "IAsyncEnumerable" &&
                   named.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic";
        }
        return false;
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

    private static bool IsBridgeEventType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            return named.OriginalDefinition.Name == "IBridgeEvent" &&
                   named.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "Agibuild.Fulora";
        }
        return false;
    }

    private static ImmutableArray<BridgeEventModel> ExtractEvents(INamedTypeSymbol interfaceSymbol)
    {
        var builder = ImmutableArray.CreateBuilder<BridgeEventModel>();

        foreach (var member in interfaceSymbol.GetMembers())
        {
            if (member is not IPropertySymbol prop) continue;
            if (!IsBridgeEventType(prop.Type)) continue;

            var payloadType = ((INamedTypeSymbol)prop.Type).TypeArguments[0].ToDisplayString();

            builder.Add(new BridgeEventModel
            {
                PropertyName = prop.Name,
                CamelCaseName = ToCamelCase(prop.Name),
                PayloadTypeFullName = payloadType,
            });
        }

        return builder.ToImmutable();
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
            var isAsyncEnumerable = IsAsyncEnumerableType(method.ReturnType);
            string? asyncEnumerableInnerType = null;

            if (isAsyncEnumerable && method.ReturnType is INamedTypeSymbol asyncEnumReturn && asyncEnumReturn.IsGenericType)
            {
                asyncEnumerableInnerType = asyncEnumReturn.TypeArguments[0].ToDisplayString();
                hasReturnValue = true;
                innerReturnType = asyncEnumerableInnerType;
            }
            else if (isAsync && method.ReturnType is INamedTypeSymbol namedReturn && namedReturn.IsGenericType)
            {
                hasReturnValue = true;
                innerReturnType = namedReturn.TypeArguments[0].ToDisplayString();
            }
            else if (!isAsync && !method.ReturnsVoid && !isAsyncEnumerable)
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
                IsAsyncEnumerable = isAsyncEnumerable,
                AsyncEnumerableInnerType = asyncEnumerableInnerType,
            });
        }

        return AssignOverloadRpcNames(builder, serviceName);
    }

    private static ImmutableArray<BridgeMethodModel> AssignOverloadRpcNames(
        ImmutableArray<BridgeMethodModel>.Builder methods,
        string serviceName)
    {
        var groups = methods.GroupBy(m => m.CamelCaseName).ToList();
        var hasOverloads = groups.Any(g => g.Count() > 1);
        if (!hasOverloads)
            return methods.ToImmutable();

        var result = ImmutableArray.CreateBuilder<BridgeMethodModel>(methods.Count);
        foreach (var group in groups)
        {
            var overloads = group.ToList();
            if (overloads.Count == 1)
            {
                result.Add(overloads[0]);
                continue;
            }

            var sorted = overloads.OrderBy(m => m.VisibleParameterCount).ToList();
            var minCount = sorted[0].VisibleParameterCount;

            for (int i = 0; i < sorted.Count; i++)
            {
                var m = sorted[i];
                var rpcName = i == 0
                    ? $"{serviceName}.{m.CamelCaseName}"
                    : $"{serviceName}.{m.CamelCaseName}${m.VisibleParameterCount}";

                result.Add(m with { RpcMethodName = rpcName, IsOverload = true });
            }
        }

        return result.ToImmutable();
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
                IsCancellationToken = IsCancellationTokenType(param.Type),
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
