param(
    [string]$StepName = "",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $PSCommandPath
$repoRoot = Split-Path -Parent $scriptRoot
$projectPath = Join-Path $repoRoot "asutpKB.csproj"
$testsProjectPath = Join-Path $repoRoot "tests/AsutpKnowledgeBase.Core.Tests/AsutpKnowledgeBase.Core.Tests.csproj"

if ([string]::IsNullOrWhiteSpace($StepName)) {
    $StepName = "manual-" + (Get-Date -Format "yyyyMMdd-HHmmss")
}

$safeStepName = ($StepName.Trim() -replace "[^A-Za-z0-9._-]", "-").Trim("-")
if ([string]::IsNullOrWhiteSpace($safeStepName)) {
    throw "StepName must contain at least one letter or digit after sanitization."
}

$dotnetCliHome = Join-Path $repoRoot ".dotnet-cli"
New-Item -ItemType Directory -Path $dotnetCliHome -Force | Out-Null
$env:DOTNET_CLI_HOME = $dotnetCliHome

$dotnetCandidates = @()
if ($env:DOTNET_EXE) {
    $dotnetCandidates += $env:DOTNET_EXE
}

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnetCommand) {
    $dotnetCandidates += $dotnetCommand.Source
}

$homeDotnet = Join-Path $HOME ".dotnet/dotnet"
if (Test-Path $homeDotnet) {
    $dotnetCandidates += $homeDotnet
}

$dotnetPath = $dotnetCandidates |
    Where-Object { $_ -and (Test-Path $_) } |
    Select-Object -First 1

if (-not $dotnetPath) {
    throw "dotnet was not found. Add dotnet to PATH or set DOTNET_EXE."
}

$verifyRoot = Join-Path $repoRoot "artifacts/verify/$safeStepName"
$buildOutputRoot = Join-Path $verifyRoot "build"
$testOutputRoot = Join-Path $verifyRoot "test"
$buildLogPath = Join-Path $verifyRoot "build.log"
$testLogPath = Join-Path $verifyRoot "test.log"
$statusPath = Join-Path $verifyRoot "status.txt"

New-Item -ItemType Directory -Path $verifyRoot -Force | Out-Null

function Invoke-LoggedDotnetCommand {
    param(
        [string]$OperationName,
        [string[]]$Arguments,
        [string]$LogPath
    )

    $commandText = "$dotnetPath $($Arguments -join ' ')"
    "COMMAND: $commandText" | Set-Content -Path $LogPath
    Write-Host "Running [$OperationName]: $commandText"

    & $dotnetPath @Arguments 2>&1 | Tee-Object -FilePath $LogPath -Append
    if ($LASTEXITCODE -ne 0) {
        throw "Verification step '$OperationName' failed with exit code $LASTEXITCODE. See $LogPath"
    }
}

$buildArguments = @(
    "build",
    $projectPath,
    "--configuration",
    $Configuration,
    "-p:BaseOutputPath=$buildOutputRoot\"
)

$testArguments = @(
    "test",
    $testsProjectPath,
    "--configuration",
    $Configuration,
    "-p:BaseOutputPath=$testOutputRoot\"
)

Invoke-LoggedDotnetCommand -OperationName "build" -Arguments $buildArguments -LogPath $buildLogPath
Invoke-LoggedDotnetCommand -OperationName "test" -Arguments $testArguments -LogPath $testLogPath

$statusLines = @(
    "STEP: $StepName",
    "DATE_LOCAL: $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))",
    "BUILD: PASS",
    "TESTS: PASS",
    "STATE: WAITING_REVIEW",
    "NEXT: stop after green tests; wait for manual review before commit or push",
    "BUILD_LOG: $buildLogPath",
    "TEST_LOG: $testLogPath"
)

$statusLines | Set-Content -Path $statusPath

Write-Host "Verification completed successfully."
Write-Host "Status file: $statusPath"
