using Agibuild.Fulora.Security;

namespace Agibuild.Fulora.UnitTests.TestDoubles;

internal sealed class RecordingNavigationSecurityHooks : INavigationSecurityHooks
{
    private readonly List<ServerCertificateErrorContext> _received = new();

    public IReadOnlyList<ServerCertificateErrorContext> Received => _received;

    public NavigationSecurityDecision OnServerCertificateError(ServerCertificateErrorContext context)
    {
        _received.Add(context);
        return NavigationSecurityDecision.Reject;
    }
}
