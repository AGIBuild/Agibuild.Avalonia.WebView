[CmdletBinding()]
param(
    [string]$Target = "Compile",
    [string]$Configuration = "Debug",
    [switch]$IncludeAndroid,
    [switch]$IncludeGtk,
    [switch]$IncludeAllAdapters
)

$parameters = @("--target", $Target, "--configuration", $Configuration)

if ($IncludeAndroid) { $parameters += "--include-android" }
if ($IncludeGtk) { $parameters += "--include-gtk" }
if ($IncludeAllAdapters) { $parameters += "--include-all-adapters" }

dotnet run --project "build/Build.csproj" -- @parameters
