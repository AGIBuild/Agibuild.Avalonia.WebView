using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    [Parameter] readonly string Configuration = "Debug";
    [Parameter("Include Android adapter project")] readonly bool IncludeAndroid = false;
    [Parameter("Include Gtk adapter project")] readonly bool IncludeGtk = false;
    [Parameter("Include all adapter projects")] readonly bool IncludeAllAdapters = false;

    Target Compile => _ => _
        .Executes(() =>
        {
            foreach (var project in GetProjectsToBuild())
            {
                DotNetBuild(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration));
            }
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(TestsDirectory / "Agibuild.Avalonia.WebView.Tests" / "Agibuild.Avalonia.WebView.Tests.csproj")
                .SetConfiguration(Configuration));
        });

    Target Ci => _ => _
        .DependsOn(Test);

    AbsolutePath SrcDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";

    IEnumerable<AbsolutePath> GetProjectsToBuild()
    {
        var projects = new List<AbsolutePath>
        {
            SrcDirectory / "Agibuild.Avalonia.WebView.Core" / "Agibuild.Avalonia.WebView.Core.csproj",
            SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Abstractions" / "Agibuild.Avalonia.WebView.Adapters.Abstractions.csproj",
            SrcDirectory / "Agibuild.Avalonia.WebView.DependencyInjection" / "Agibuild.Avalonia.WebView.DependencyInjection.csproj",
            TestsDirectory / "Agibuild.Avalonia.WebView.Tests" / "Agibuild.Avalonia.WebView.Tests.csproj"
        };

        if (IncludeAllAdapters)
        {
            projects.Add(SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Windows" / "Agibuild.Avalonia.WebView.Adapters.Windows.csproj");
            projects.Add(SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.MacOS" / "Agibuild.Avalonia.WebView.Adapters.MacOS.csproj");
            projects.Add(SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Android" / "Agibuild.Avalonia.WebView.Adapters.Android.csproj");
            projects.Add(SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Gtk" / "Agibuild.Avalonia.WebView.Adapters.Gtk.csproj");
            return projects;
        }

        if (OperatingSystem.IsWindows())
        {
            projects.Add(SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Windows" / "Agibuild.Avalonia.WebView.Adapters.Windows.csproj");
        }
        else if (OperatingSystem.IsMacOS())
        {
            projects.Add(SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.MacOS" / "Agibuild.Avalonia.WebView.Adapters.MacOS.csproj");
        }
        else
        {
            projects.Add(SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Gtk" / "Agibuild.Avalonia.WebView.Adapters.Gtk.csproj");
        }

        if (IncludeAndroid)
        {
            projects.Add(SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Android" / "Agibuild.Avalonia.WebView.Adapters.Android.csproj");
        }

        if (IncludeGtk)
        {
            projects.Add(SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Gtk" / "Agibuild.Avalonia.WebView.Adapters.Gtk.csproj");
        }

        return projects.Distinct();
    }

    public static int Main() => Execute<Build>(x => x.Compile);
}
