using Nuke.Common;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

internal partial class BuildTask
{
    internal Target RefreshLockFiles => _ => _
        .Description("Regenerates all packages.lock.json files after dependency changes.")
        .Executes(() =>
        {
            DotNet($"restore {SolutionFile} --force-evaluate");
        });
}
