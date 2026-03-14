using Nuke.Common;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

internal partial class BuildTask
{
    internal Target Format => _ => _
        .Description("Verifies that code formatting matches .editorconfig rules. Fails if any files would be changed.")
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNet($"format {SolutionFile} --verify-no-changes", workingDirectory: RootDirectory);
        });
}
