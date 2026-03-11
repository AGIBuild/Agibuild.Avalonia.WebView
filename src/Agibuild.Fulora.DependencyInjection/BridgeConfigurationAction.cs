namespace Agibuild.Fulora.DependencyInjection;

/// <summary>
/// Holds a bridge configuration action registered via DI.
/// Resolved by <see cref="WebViewBootstrapExtensions.BootstrapSpaAsync"/> to apply
/// plugin-first bridge registrations from the service container.
/// </summary>
public sealed class BridgeConfigurationAction
{
    internal Action<IBridgeService, IServiceProvider?> Configure { get; }

    internal BridgeConfigurationAction(Action<IBridgeService, IServiceProvider?> configure)
    {
        Configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }
}
