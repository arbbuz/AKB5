param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $PSCommandPath
$publishScript = Join-Path $scriptRoot "publish.ps1"
$repoRoot = Split-Path -Parent $scriptRoot

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts/publish-fast/$RuntimeIdentifier"
}

& $publishScript `
    -Configuration $Configuration `
    -RuntimeIdentifier $RuntimeIdentifier `
    -OutputDirectory $OutputDirectory `
    -PublishMode Folder `
    -ReadyToRun
