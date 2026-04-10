using Agibuild.Fulora.Cli.Commands;

namespace Agibuild.Fulora.UnitTests;

internal sealed class FakeEnvironmentContext : IEnvironmentContext
{
    private readonly Dictionary<string, string> _variables = new(StringComparer.Ordinal);

    public string CurrentDirectory { get; set; } = Path.GetFullPath("/virtual/workspace");

    public bool IsWindows { get; set; }

    public bool IsMacOS { get; set; }

    public string TempPath { get; set; } = Path.GetFullPath("/virtual/tmp");

    public string? GetEnvironmentVariable(string variable)
        => _variables.TryGetValue(variable, out var value) ? value : null;

    public void SetEnvironmentVariable(string variable, string value)
        => _variables[variable] = value;
}
