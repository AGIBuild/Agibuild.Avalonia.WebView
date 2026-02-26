using System.Text;
using Mono.Cecil;

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Agibuild.Fulora.sln")))
            return dir.FullName;
        dir = dir.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate repository root.");
}

static IEnumerable<TypeDefinition> GetAllTypes(ModuleDefinition module)
{
    var stack = new Stack<TypeDefinition>(module.Types);
    while (stack.Count > 0)
    {
        var t = stack.Pop();
        yield return t;
        foreach (var n in t.NestedTypes)
            stack.Push(n);
    }
}

static bool IsPublicApiType(TypeDefinition t)
    => t.IsPublic || t.IsNestedPublic;

static bool IsPublicApiMethod(MethodDefinition m)
    => m.IsPublic && !m.IsGetter && !m.IsSetter && !m.IsAddOn && !m.IsRemoveOn;

static bool IsPublicApiField(FieldDefinition f)
    => f.IsPublic;

static bool IsPublicApiProperty(PropertyDefinition p)
    => (p.GetMethod?.IsPublic == true) || (p.SetMethod?.IsPublic == true);

static bool IsPublicApiEvent(EventDefinition e)
    => (e.AddMethod?.IsPublic == true) || (e.RemoveMethod?.IsPublic == true);

static IEnumerable<string> DescribeMembers(TypeDefinition t)
{
    foreach (var f in t.Fields.Where(IsPublicApiField).OrderBy(x => x.Name, StringComparer.Ordinal))
        yield return $"field {f.FieldType.FullName} {t.FullName}::{f.Name}";

    foreach (var p in t.Properties.Where(IsPublicApiProperty).OrderBy(x => x.Name, StringComparer.Ordinal))
        yield return $"property {p.PropertyType.FullName} {t.FullName}::{p.Name}";

    foreach (var e in t.Events.Where(IsPublicApiEvent).OrderBy(x => x.Name, StringComparer.Ordinal))
        yield return $"event {e.EventType.FullName} {t.FullName}::{e.Name}";

    foreach (var m in t.Methods.Where(IsPublicApiMethod).OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Parameters.Count))
        yield return $"method {m.FullName}";
}

var repoRoot = FindRepoRoot();
var configuration = args.FirstOrDefault(x => x.StartsWith("--config=", StringComparison.Ordinal))?.Split('=', 2).ElementAtOrDefault(1) ?? "Release";
var outPathArg = args.FirstOrDefault(x => x.StartsWith("--out=", StringComparison.Ordinal))?.Split('=', 2).ElementAtOrDefault(1);

var assemblyPaths = new[]
{
    Path.Combine(repoRoot, "src", "Agibuild.Fulora.Core", "bin", configuration, "net10.0", "Agibuild.Fulora.Core.dll"),
    Path.Combine(repoRoot, "src", "Agibuild.Fulora.Adapters.Abstractions", "bin", configuration, "net10.0", "Agibuild.Fulora.Adapters.Abstractions.dll"),
    Path.Combine(repoRoot, "src", "Agibuild.Fulora.Runtime", "bin", configuration, "net10.0", "Agibuild.Fulora.Runtime.dll"),
    Path.Combine(repoRoot, "src", "Agibuild.Fulora.DependencyInjection", "bin", configuration, "net10.0", "Agibuild.Fulora.DependencyInjection.dll"),
    Path.Combine(repoRoot, "src", "Agibuild.Fulora", "bin", configuration, "net10.0", "Agibuild.Fulora.dll"),
    Path.Combine(repoRoot, "src", "Agibuild.Fulora.Bridge.Generator", "bin", configuration, "netstandard2.0", "Agibuild.Fulora.Bridge.Generator.dll"),
};

var missing = assemblyPaths.Where(p => !File.Exists(p)).ToList();
if (missing.Count > 0)
{
    Console.Error.WriteLine("Missing built assemblies. Build the solution first.");
    foreach (var m in missing)
        Console.Error.WriteLine($"- {m}");
    return 2;
}

var sb = new StringBuilder();
sb.AppendLine($"GeneratedAtUtc: {DateTime.UtcNow:O}");
sb.AppendLine($"Configuration: {configuration}");
sb.AppendLine();

foreach (var path in assemblyPaths.OrderBy(x => x, StringComparer.Ordinal))
{
    var asm = AssemblyDefinition.ReadAssembly(path);
    sb.AppendLine($"Assembly: {asm.Name.Name}");
    sb.AppendLine($"Path: {path.Replace(repoRoot, "<repo>", StringComparison.OrdinalIgnoreCase)}");

    var publicTypes = GetAllTypes(asm.MainModule)
        .Where(IsPublicApiType)
        .OrderBy(t => t.FullName, StringComparer.Ordinal)
        .ToList();

    sb.AppendLine($"PublicTypes: {publicTypes.Count}");
    foreach (var t in publicTypes)
    {
        sb.AppendLine();
        sb.AppendLine($"type {t.FullName}");
        foreach (var member in DescribeMembers(t))
            sb.AppendLine($"  - {member}");
    }

    sb.AppendLine();
    sb.AppendLine(new string('-', 80));
    sb.AppendLine();
}

Console.Write(sb.ToString());
if (!string.IsNullOrWhiteSpace(outPathArg))
{
    var fullPath = Path.IsPathRooted(outPathArg)
        ? outPathArg
        : Path.Combine(repoRoot, outPathArg);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    File.WriteAllText(fullPath, sb.ToString());
}
return 0;

