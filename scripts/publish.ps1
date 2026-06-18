param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputDirectory = "",
    [ValidateSet("SingleFile", "Folder")]
    [string]$PublishMode = "SingleFile",
    [switch]$ReadyToRun
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($RuntimeIdentifier -ne "win-x64") {
    throw "Supported publish target is only win-x64. Requested: '$RuntimeIdentifier'."
}

$scriptRoot = Split-Path -Parent $PSCommandPath
$repoRoot = Split-Path -Parent $scriptRoot
$projectPath = Join-Path $repoRoot "asutpKB.csproj"
$pdfImportProjectPath = Join-Path $repoRoot "src/AsutpKnowledgeBase.PdfImport/AsutpKnowledgeBase.PdfImport.csproj"

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

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $artifactName = if ($PublishMode -eq "Folder") { "publish-fast" } else { "publish" }
    $OutputDirectory = Join-Path $repoRoot "artifacts/$artifactName/$RuntimeIdentifier"
}

if (Test-Path $OutputDirectory) {
    Remove-Item $OutputDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$publishArgs = @(
    "publish",
    $projectPath,
    "-c",
    $Configuration,
    "-r",
    $RuntimeIdentifier,
    "--self-contained",
    "true",
    "-p:PublishTrimmed=false",
    "-p:PublishAot=false",
    "-p:RunAnalyzers=false",
    "-p:WarningLevel=0",
    "-o",
    $OutputDirectory
)

if ($PublishMode -eq "SingleFile") {
    $publishArgs += @(
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true"
    )
}
else {
    $publishArgs += @(
        "-p:PublishSingleFile=false",
        "-p:IncludeNativeLibrariesForSelfExtract=false"
    )
}

if ($ReadyToRun) {
    $publishArgs += "-p:PublishReadyToRun=true"
}

Write-Host "Running: $dotnetPath $($publishArgs -join ' ')"
& $dotnetPath @publishArgs

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$pdfImportOutputDirectory = Join-Path $OutputDirectory "pdf-import"
New-Item -ItemType Directory -Path $pdfImportOutputDirectory -Force | Out-Null

$pdfImportPublishArgs = @(
    "publish",
    $pdfImportProjectPath,
    "-c",
    $Configuration,
    "-r",
    $RuntimeIdentifier,
    "--self-contained",
    "false",
    "-p:PublishTrimmed=false",
    "-p:PublishAot=false",
    "-p:PublishSingleFile=false",
    "-p:IncludeNativeLibrariesForSelfExtract=false",
    "-p:RunAnalyzers=false",
    "-p:WarningLevel=0",
    "-o",
    $pdfImportOutputDirectory
)

Write-Host "Running: $dotnetPath $($pdfImportPublishArgs -join ' ')"
& $dotnetPath @pdfImportPublishArgs

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$allowedPdfImportFiles = @(
    "AsutpKnowledgeBase.PdfImport.deps.json",
    "AsutpKnowledgeBase.PdfImport.dll",
    "AsutpKnowledgeBase.PdfImport.pdb"
)

$removedPdfImportFiles = @()
Get-ChildItem -LiteralPath $pdfImportOutputDirectory -File |
    Where-Object {
        $fileName = $_.Name
        -not ($allowedPdfImportFiles -contains $fileName) -and
        -not $fileName.StartsWith("UglyToad.PdfPig", [StringComparison]::OrdinalIgnoreCase)
    } |
    ForEach-Object {
        $removedPdfImportFiles += $_.Name
        Remove-Item -LiteralPath $_.FullName -Force
    }

$expectedExe = Join-Path $OutputDirectory "asutpKB.exe"
if (-not (Test-Path $expectedExe)) {
    throw "Publish succeeded but expected executable was not found: $expectedExe"
}

$expectedPdfImportAssembly = Join-Path $pdfImportOutputDirectory "AsutpKnowledgeBase.PdfImport.dll"
if (-not (Test-Path $expectedPdfImportAssembly)) {
    throw "Publish succeeded but expected PDF import module was not found: $expectedPdfImportAssembly"
}

Write-Host "Publish output: $expectedExe"
Write-Host "PDF import module output: $expectedPdfImportAssembly"
if ($removedPdfImportFiles.Count -gt 0) {
    Write-Host "Removed non-PDF module files: $($removedPdfImportFiles -join ', ')"
}
