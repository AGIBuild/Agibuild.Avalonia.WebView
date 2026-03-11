using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Agibuild.Fulora.Bridge.Generator;

/// <summary>
/// Builds a <see cref="BridgeContractModel"/> (the canonical Contract IR) from collected interface models.
/// Deduplicates DTOs by <see cref="BridgeDtoModel.FullName"/> across all services.
/// </summary>
internal static class ContractIrBuilder
{
    public static BridgeContractModel Build(
        ImmutableArray<BridgeInterfaceModel> exports,
        ImmutableArray<BridgeInterfaceModel> imports)
    {
        var services = exports.Where(m => m.IsValid)
            .Concat(imports.Where(m => m.IsValid))
            .OrderBy(s => s.Direction)
            .ThenBy(s => s.ServiceName, System.StringComparer.Ordinal)
            .ToImmutableArray();

        var dtos = DeduplicateDtos(services);

        return new BridgeContractModel
        {
            Services = services,
            Dtos = dtos,
        };
    }

    private static ImmutableArray<BridgeDtoModel> DeduplicateDtos(ImmutableArray<BridgeInterfaceModel> services)
    {
        var seen = new Dictionary<string, BridgeDtoModel>();

        foreach (var service in services)
        {
            foreach (var dto in service.ReferencedDtos)
            {
                if (!seen.ContainsKey(dto.FullName))
                    seen[dto.FullName] = dto;
            }
        }

        return seen.Values
            .OrderBy(d => d.FullName, System.StringComparer.Ordinal)
            .ToImmutableArray();
    }
}
