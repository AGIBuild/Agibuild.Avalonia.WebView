using Agibuild.Fulora;

namespace Agibuild.Fulora.UnitTests;

[JsExport]
public interface ICliGeneratorFixtureService
{
    Task<string> Ping(string name);
}
