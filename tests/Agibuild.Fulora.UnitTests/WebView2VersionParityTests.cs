using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

/// <summary>
/// Enforces the single-source-of-truth invariant for Microsoft.Web.WebView2:
/// the CPM version in Directory.Packages.props and the buildTransitive/*.targets template
/// must stay synchronized via the @WEBVIEW2_VERSION@ token. A regression here is what
/// shipped as the 1.6.0 drift bug (CPM 1.0.3856.49 vs hardcoded 1.0.3719.77), which
/// silently broke Microsoft.Web.WebView2.Core.dll HintPath resolution on Windows.
/// </summary>
public class WebView2VersionParityTests
{
    private const string CpmPropertyName = "AgibuildFuloraWebView2Version";
    private const string CpmPackageId = "Microsoft.Web.WebView2";
    private const string TemplateToken = "@WEBVIEW2_VERSION@";

    [Fact]
    public void CPM_defines_AgibuildFuloraWebView2Version_as_concrete_version()
    {
        var repoRoot = FindRepoRoot();
        var propsPath = Path.Combine(repoRoot, "Directory.Packages.props");
        var doc = XDocument.Load(propsPath);

        var cpmVersion = ReadCpmWebView2Version(doc);
        Assert.NotNull(cpmVersion);
        Assert.Matches(@"^\d+\.\d+\.\d+(\.\d+)?(-[A-Za-z0-9.+-]+)?$", cpmVersion);
    }

    [Fact]
    public void CPM_PackageVersion_for_WebView2_references_the_property()
    {
        var repoRoot = FindRepoRoot();
        var propsPath = Path.Combine(repoRoot, "Directory.Packages.props");
        var doc = XDocument.Load(propsPath);

        var versionAttr = doc.Descendants()
            .Where(e => e.Name.LocalName == "PackageVersion"
                        && string.Equals((string?)e.Attribute("Include"), CpmPackageId, StringComparison.Ordinal))
            .Select(e => (string?)e.Attribute("Version"))
            .FirstOrDefault();

        Assert.NotNull(versionAttr);
        Assert.Equal($"$({CpmPropertyName})", versionAttr);
    }

    [Fact]
    public void buildTransitive_template_uses_token_and_has_no_hardcoded_version()
    {
        var repoRoot = FindRepoRoot();
        var templatePath = Path.Combine(
            repoRoot,
            "src",
            "Agibuild.Fulora.Avalonia",
            "buildTransitive",
            "Agibuild.Fulora.Avalonia.targets.in");

        Assert.True(File.Exists(templatePath),
            $"Template file not found at {templatePath}. If the template was renamed or moved, update WebView2VersionParityTests.");

        var contents = File.ReadAllText(templatePath);

        Assert.Contains(TemplateToken, contents);

        // No concrete WebView2 version string may appear inside the template body — only the token.
        // Pattern: numeric version that looks like Microsoft.Web.WebView2 (e.g. 1.0.3856.49 or 1.0.3719.77).
        // If this regex matches, someone reintroduced a hardcoded version instead of using the token.
        var concreteVersion = Regex.Match(contents, @"1\.0\.\d{3,4}\.\d{1,3}");
        Assert.False(concreteVersion.Success,
            $"Template contains hardcoded WebView2 version '{concreteVersion.Value}'. " +
            $"Use '{TemplateToken}' instead; GeneratePackBuildTransitive will substitute " +
            $"$({CpmPropertyName}) at pack time.");
    }

    [Fact]
    public void buildTransitive_static_targets_file_is_absent()
    {
        var repoRoot = FindRepoRoot();
        var staticTargetsPath = Path.Combine(
            repoRoot,
            "src",
            "Agibuild.Fulora.Avalonia",
            "buildTransitive",
            "Agibuild.Fulora.Avalonia.targets");

        Assert.False(File.Exists(staticTargetsPath),
            $"A static '{staticTargetsPath}' exists alongside the .targets.in template. " +
            $"The generated file must live under obj/buildTransitive/ only — committing a static .targets " +
            $"would let it ship stale and reintroduce the version drift bug.");
    }

    private static string? ReadCpmWebView2Version(XDocument doc) =>
        doc.Descendants()
            .Where(e => e.Name.LocalName == CpmPropertyName)
            .Select(e => e.Value.Trim())
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Agibuild.Fulora.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
