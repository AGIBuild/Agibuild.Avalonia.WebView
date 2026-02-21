[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string[]]$Target,
    [string]$Configuration,
    [string]$PackageVersion,
    [string]$NuGetSource,
    [string]$NuGetApiKey,
    [switch]$Help,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$NukeArgs
)

$parameters = @()

if ($Target)           { $parameters += "--target"; $parameters += $Target }
if ($Configuration)    { $parameters += "--configuration"; $parameters += $Configuration }
if ($PackageVersion)   { $parameters += "--package-version"; $parameters += $PackageVersion }
if ($NuGetSource)      { $parameters += "--nuget-source"; $parameters += $NuGetSource }
$effectiveApiKey = if ($NuGetApiKey) { $NuGetApiKey } else { $env:NUGET_API_KEY }
if ($effectiveApiKey)  { $parameters += "--nuget-api-key"; $parameters += $effectiveApiKey }
if ($Help)             { $parameters += "--help" }
if ($NukeArgs)         { $parameters += $NukeArgs }

dotnet run --project "build/Build.csproj" -- @parameters
