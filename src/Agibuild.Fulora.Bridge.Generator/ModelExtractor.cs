using System.Collections.Generic;
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
        var referencedDtos = DiscoverReferencedDtos(interfaceSymbol);

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
            ReferencedDtos = referencedDtos,
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

            var payloadSymbol = ((INamedTypeSymbol)prop.Type).TypeArguments[0];
            var payloadType = payloadSymbol.ToDisplayString();

            builder.Add(new BridgeEventModel
            {
                PropertyName = prop.Name,
                CamelCaseName = ToCamelCase(prop.Name),
                PayloadTypeFullName = payloadType,
                PayloadTypeRef = BuildTypeRef(payloadSymbol),
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
            BridgeTypeRef? innerReturnTypeRef = null;
            var isAsyncEnumerable = IsAsyncEnumerableType(method.ReturnType);
            string? asyncEnumerableInnerType = null;
            BridgeTypeRef? asyncEnumerableInnerTypeRef = null;

            if (isAsyncEnumerable && method.ReturnType is INamedTypeSymbol asyncEnumReturn && asyncEnumReturn.IsGenericType)
            {
                asyncEnumerableInnerType = asyncEnumReturn.TypeArguments[0].ToDisplayString();
                asyncEnumerableInnerTypeRef = BuildTypeRef(asyncEnumReturn.TypeArguments[0]);
                hasReturnValue = true;
                innerReturnType = asyncEnumerableInnerType;
                innerReturnTypeRef = asyncEnumerableInnerTypeRef;
            }
            else if (isAsync && method.ReturnType is INamedTypeSymbol namedReturn && namedReturn.IsGenericType)
            {
                hasReturnValue = true;
                innerReturnType = namedReturn.TypeArguments[0].ToDisplayString();
                innerReturnTypeRef = BuildTypeRef(namedReturn.TypeArguments[0]);
            }
            else if (!isAsync && !method.ReturnsVoid && !isAsyncEnumerable)
            {
                hasReturnValue = true;
                innerReturnType = method.ReturnType.ToDisplayString();
                innerReturnTypeRef = BuildTypeRef(method.ReturnType);
            }

            var parameters = ExtractParameters(method);

            builder.Add(new BridgeMethodModel
            {
                Name = method.Name,
                CamelCaseName = camelName,
                RpcMethodName = rpcMethodName,
                ReturnTypeFullName = method.ReturnType.ToDisplayString(),
                ReturnTypeRef = BuildTypeRef(method.ReturnType),
                IsAsync = isAsync,
                HasReturnValue = hasReturnValue,
                InnerReturnTypeFullName = innerReturnType,
                InnerReturnTypeRef = innerReturnTypeRef,
                Parameters = parameters,
                IsAsyncEnumerable = isAsyncEnumerable,
                AsyncEnumerableInnerType = asyncEnumerableInnerType,
                AsyncEnumerableInnerTypeRef = asyncEnumerableInnerTypeRef,
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
                TypeRef = BuildTypeRef(param.Type),
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

    // ==================== DTO discovery ====================

    private static ImmutableArray<BridgeDtoModel> DiscoverReferencedDtos(INamedTypeSymbol interfaceSymbol)
    {
        var visited = new HashSet<string>();
        var dtos = new List<BridgeDtoModel>();

        foreach (var member in interfaceSymbol.GetMembers())
        {
            if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
            {
                foreach (var param in method.Parameters)
                {
                    if (!IsCancellationTokenType(param.Type))
                        WalkType(param.Type, visited, dtos);
                }

                var returnType = UnwrapReturnType(method.ReturnType);
                if (returnType != null)
                    WalkType(returnType, visited, dtos);
            }
            else if (member is IPropertySymbol prop && IsBridgeEventType(prop.Type))
            {
                var payloadType = ((INamedTypeSymbol)prop.Type).TypeArguments[0];
                WalkType(payloadType, visited, dtos);
            }
        }

        return dtos.ToImmutableArray();
    }

    private static ITypeSymbol? UnwrapReturnType(ITypeSymbol returnType)
    {
        if (returnType.SpecialType == SpecialType.System_Void)
            return null;

        if (returnType is INamedTypeSymbol named)
        {
            if (IsTaskType(named))
            {
                return named.IsGenericType && named.TypeArguments.Length > 0
                    ? named.TypeArguments[0]
                    : null;
            }

            if (IsAsyncEnumerableType(named))
                return named.TypeArguments[0];
        }

        return returnType;
    }

    private static void WalkType(ITypeSymbol type, HashSet<string> visited, List<BridgeDtoModel> dtos)
    {
        type = UnwrapNullableType(type);

        if (IsPrimitiveOrWellKnown(type))
            return;

        if (type is IArrayTypeSymbol arrayType)
        {
            if (arrayType.ElementType.SpecialType != SpecialType.System_Byte)
                WalkType(arrayType.ElementType, visited, dtos);
            return;
        }

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var origName = namedType.OriginalDefinition.Name;
            var origNs = namedType.OriginalDefinition.ContainingNamespace?.ToDisplayString() ?? "";

            if (origNs == "Agibuild.Fulora" && origName == "IBridgeEvent")
            {
                WalkType(namedType.TypeArguments[0], visited, dtos);
                return;
            }

            if (origNs == "System.Collections.Generic" && origName == "IAsyncEnumerable")
            {
                WalkType(namedType.TypeArguments[0], visited, dtos);
                return;
            }

            if (origNs == "System.Collections.Generic" &&
                origName is "List" or "IList" or "IReadOnlyList" or "IEnumerable" or "ICollection" or "IReadOnlyCollection")
            {
                WalkType(namedType.TypeArguments[0], visited, dtos);
                return;
            }

            if (origNs == "System.Collections.Generic" &&
                origName is "Dictionary" or "IDictionary" or "IReadOnlyDictionary")
            {
                WalkType(namedType.TypeArguments[0], visited, dtos);
                WalkType(namedType.TypeArguments[1], visited, dtos);
                return;
            }

            if (origNs == "System.Threading.Tasks" && origName is "Task" or "ValueTask")
            {
                if (namedType.TypeArguments.Length > 0)
                    WalkType(namedType.TypeArguments[0], visited, dtos);
                return;
            }
        }

        var fullName = type.ToDisplayString().TrimEnd('?');
        if (visited.Contains(fullName))
            return;
        visited.Add(fullName);

        if (type.TypeKind == TypeKind.Enum)
        {
            var enumMembers = ImmutableArray.CreateBuilder<BridgeEnumMemberModel>();
            foreach (var m in type.GetMembers())
            {
                if (m is IFieldSymbol field && field.IsConst && field.HasConstantValue)
                {
                    enumMembers.Add(new BridgeEnumMemberModel
                    {
                        Name = field.Name,
                        CamelCaseName = ToCamelCase(field.Name),
                        ValueLiteral = field.ConstantValue?.ToString() ?? "0",
                    });
                }
            }

            dtos.Add(new BridgeDtoModel
            {
                FullName = fullName,
                Name = type.Name,
                IsEnum = true,
                EnumMembers = enumMembers.ToImmutable(),
            });
            return;
        }

        if (type is INamedTypeSymbol dtoType &&
            type.TypeKind is TypeKind.Class or TypeKind.Struct)
        {
            var properties = ImmutableArray.CreateBuilder<BridgeDtoPropertyModel>();
            var seenPropNames = new HashSet<string>();
            var current = dtoType;

            while (current != null && current.SpecialType != SpecialType.System_Object)
            {
                foreach (var m in current.GetMembers())
                {
                    if (m is IPropertySymbol prop &&
                        prop.DeclaredAccessibility == Accessibility.Public &&
                        !prop.IsStatic &&
                        !prop.IsIndexer &&
                        seenPropNames.Add(prop.Name))
                    {
                        WalkType(prop.Type, visited, dtos);

                        properties.Add(new BridgeDtoPropertyModel
                        {
                            Name = prop.Name,
                            CamelCaseName = ToCamelCase(prop.Name),
                            TypeRef = BuildTypeRef(prop.Type),
                            IsNullable = prop.NullableAnnotation == NullableAnnotation.Annotated,
                        });
                    }
                }

                current = current.BaseType;
            }

            dtos.Add(new BridgeDtoModel
            {
                FullName = fullName,
                Name = type.Name,
                IsEnum = false,
                Properties = properties.ToImmutable(),
            });
        }
    }

    private static ITypeSymbol UnwrapNullableType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named &&
            named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            named.TypeArguments.Length > 0)
        {
            return named.TypeArguments[0];
        }

        return type;
    }

    private static bool IsPrimitiveOrWellKnown(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_String:
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_Int16:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_Byte:
            case SpecialType.System_Boolean:
            case SpecialType.System_Void:
            case SpecialType.System_Object:
                return true;
        }

        var name = type.Name;
        var ns = type.ContainingNamespace?.ToDisplayString() ?? "";

        if (ns == "System" && name is "DateTime" or "DateTimeOffset" or "Guid")
            return true;

        if (ns == "System.Threading" && name == "CancellationToken")
            return true;

        if (type is IArrayTypeSymbol arr && arr.ElementType.SpecialType == SpecialType.System_Byte)
            return true;

        return false;
    }

    // ==================== Type reference builder ====================

    internal static BridgeTypeRef BuildTypeRef(ITypeSymbol type)
    {
        bool isNullable = type.NullableAnnotation == NullableAnnotation.Annotated;

        if (type is INamedTypeSymbol nullableValueType &&
            nullableValueType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            nullableValueType.TypeArguments.Length > 0)
        {
            var inner = BuildTypeRef(nullableValueType.TypeArguments[0]);
            return inner with { IsNullable = true };
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_String:
                return new BridgeTypeRef { Kind = BridgeTypeKind.String, FullName = "string", Name = "string", IsNullable = isNullable };
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_Int16:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_Byte:
                return new BridgeTypeRef { Kind = BridgeTypeKind.Number, FullName = type.ToDisplayString(), Name = type.Name, IsNullable = isNullable };
            case SpecialType.System_Boolean:
                return new BridgeTypeRef { Kind = BridgeTypeKind.Boolean, FullName = "bool", Name = "bool", IsNullable = isNullable };
            case SpecialType.System_Void:
                return new BridgeTypeRef { Kind = BridgeTypeKind.Void, FullName = "void", Name = "void" };
            case SpecialType.System_Object:
                return new BridgeTypeRef { Kind = BridgeTypeKind.Unknown, FullName = "object", Name = "object", IsNullable = isNullable };
        }

        var fullName = type.ToDisplayString().TrimEnd('?');
        var name = type.Name;
        var ns = type.ContainingNamespace?.ToDisplayString() ?? "";

        if (ns == "System" && name is "DateTime" or "DateTimeOffset")
            return new BridgeTypeRef { Kind = BridgeTypeKind.DateTime, FullName = fullName, Name = name, IsNullable = isNullable };

        if (ns == "System" && name == "Guid")
            return new BridgeTypeRef { Kind = BridgeTypeKind.Guid, FullName = fullName, Name = name, IsNullable = isNullable };

        if (type is IArrayTypeSymbol arrayType)
        {
            if (arrayType.ElementType.SpecialType == SpecialType.System_Byte)
                return new BridgeTypeRef { Kind = BridgeTypeKind.Binary, FullName = "byte[]", Name = "byte[]", IsNullable = isNullable };

            return new BridgeTypeRef
            {
                Kind = BridgeTypeKind.Array,
                FullName = fullName,
                Name = name,
                ElementType = BuildTypeRef(arrayType.ElementType),
                IsNullable = isNullable,
            };
        }

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var origName = namedType.OriginalDefinition.Name;
            var origNs = namedType.OriginalDefinition.ContainingNamespace?.ToDisplayString() ?? "";

            if (origNs == "Agibuild.Fulora" && origName == "IBridgeEvent")
            {
                return new BridgeTypeRef
                {
                    Kind = BridgeTypeKind.BridgeEvent,
                    FullName = fullName,
                    Name = name,
                    TypeArguments = ImmutableArray.Create(BuildTypeRef(namedType.TypeArguments[0])),
                    IsNullable = isNullable,
                };
            }

            if (origNs == "System.Collections.Generic" && origName == "IAsyncEnumerable")
            {
                return new BridgeTypeRef
                {
                    Kind = BridgeTypeKind.AsyncEnumerable,
                    FullName = fullName,
                    Name = name,
                    ElementType = BuildTypeRef(namedType.TypeArguments[0]),
                    IsNullable = isNullable,
                };
            }

            if (origNs == "System.Collections.Generic" &&
                origName is "List" or "IList" or "IReadOnlyList" or "IEnumerable" or "ICollection" or "IReadOnlyCollection")
            {
                return new BridgeTypeRef
                {
                    Kind = BridgeTypeKind.Array,
                    FullName = fullName,
                    Name = name,
                    ElementType = BuildTypeRef(namedType.TypeArguments[0]),
                    IsNullable = isNullable,
                };
            }

            if (origNs == "System.Collections.Generic" &&
                origName is "Dictionary" or "IDictionary" or "IReadOnlyDictionary")
            {
                return new BridgeTypeRef
                {
                    Kind = BridgeTypeKind.Dictionary,
                    FullName = fullName,
                    Name = name,
                    TypeArguments = ImmutableArray.Create(
                        BuildTypeRef(namedType.TypeArguments[0]),
                        BuildTypeRef(namedType.TypeArguments[1])),
                    IsNullable = isNullable,
                };
            }
        }

        if (type.TypeKind == TypeKind.Enum)
            return new BridgeTypeRef { Kind = BridgeTypeKind.Enum, FullName = fullName, Name = name, IsNullable = isNullable };

        if (type.TypeKind is TypeKind.Class or TypeKind.Struct)
            return new BridgeTypeRef { Kind = BridgeTypeKind.Dto, FullName = fullName, Name = name, IsNullable = isNullable };

        return new BridgeTypeRef { Kind = BridgeTypeKind.Unknown, FullName = fullName, Name = name, IsNullable = isNullable };
    }
}
