#r "nuget: System.Reflection.MetadataLoadContext, 9.0.3"
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

var repoRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory()));
var assemblies = new[]
{
    Path.Combine(repoRoot, "src", "Agibuild.Fulora.Core", "bin", "Release", "net10.0", "Agibuild.Fulora.Core.dll"),
    Path.Combine(repoRoot, "src", "Agibuild.Fulora.Runtime", "bin", "Release", "net10.0", "Agibuild.Fulora.Runtime.dll"),
};

var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
var sb = new StringBuilder();
sb.AppendLine($"GeneratedAtUtc: {DateTime.UtcNow:o}");
sb.AppendLine("Configuration: Release");
sb.AppendLine();

foreach (var asmPath in assemblies)
{
    if (!File.Exists(asmPath))
    {
        sb.AppendLine($"MISSING: {asmPath}");
        continue;
    }

    var runtimeAssemblies = Directory.GetFiles(runtimeDir, "*.dll")
        .Concat(Directory.GetFiles(Path.GetDirectoryName(asmPath)!, "*.dll"))
        .Distinct()
        .ToArray();

    var resolver = new PathAssemblyResolver(runtimeAssemblies);
    using var mlc = new MetadataLoadContext(resolver, "System.Runtime");
    var asm = mlc.LoadFromAssemblyPath(asmPath);
    var types = asm.GetExportedTypes().OrderBy(t => t.FullName).ToArray();

    var relPath = Path.GetRelativePath(repoRoot, asmPath).Replace('\\', '/');
    sb.AppendLine($"Assembly: {asm.GetName().Name}");
    sb.AppendLine($"Path: <repo>/{relPath}");
    sb.AppendLine($"PublicTypes: {types.Length}");
    sb.AppendLine();

    foreach (var t in types)
    {
        sb.AppendLine($"type {t.FullName}");
        var members = t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(m => m.Name)
            .ToArray();
        foreach (var m in members)
            sb.AppendLine($"  - {m.MemberType.ToString().ToLowerInvariant()} {m}");
        sb.AppendLine();
    }
    sb.AppendLine(new string('-', 80));
    sb.AppendLine();
}

var outPath = Path.Combine(repoRoot, "docs", "API_SURFACE_INVENTORY.release.txt");
File.WriteAllText(outPath, sb.ToString());
Console.WriteLine($"Written {sb.Length} chars to {outPath}");
