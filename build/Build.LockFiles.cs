using Nuke.Common;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

internal partial class BuildTask
{
    internal Target RefreshLockFiles => _ => _
        .Description("Regenerates all packages.lock.json files after dependency changes.")
        .Executes(async () =>
        {
            var filterPath = await BuildPlatformAwareSolutionFilterAsync("lock-files");
            DotNet($"restore {filterPath} --force-evaluate");
        });
}
