using Agibuild.Fulora.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Agibuild.Fulora.Telemetry;

/// <summary>
/// Extension methods for registering OpenTelemetry telemetry with Fulora.
/// </summary>
public static class FuloraOpenTelemetryExtensions
{
    /// <summary>
    /// Registers OpenTelemetry-based telemetry and bridge tracing for Fulora.
    /// Registers <see cref="OpenTelemetryTelemetryProvider"/> as <see cref="ITelemetryProvider"/>
    /// and <see cref="OpenTelemetryBridgeTracer"/> as <see cref="IBridgeTracer"/>.
    /// </summary>
    /// <param name="builder">The Fulora service builder to configure.</param>
    /// <returns>The builder for chaining.</returns>
    public static FuloraServiceBuilder AddOpenTelemetry(this FuloraServiceBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var tracer = new OpenTelemetryBridgeTracer();
        var telemetry = new OpenTelemetryTelemetryProvider();

        builder.AddTelemetry(telemetry);
        builder.Services.AddSingleton<IBridgeTracer>(tracer);

        return builder;
    }
}
