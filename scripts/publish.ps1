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
$excelExchangeProjectPath = Join-Path $repoRoot "src/AsutpKnowledgeBase.ExcelExchange/AsutpKnowledgeBase.ExcelExchange.csproj"
$actDocxProjectPath = Join-Path $repoRoot "src/AsutpKnowledgeBase.ActDocx/AsutpKnowledgeBase.ActDocx.csproj"

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

$excelExchangeOutputDirectory = Join-Path $OutputDirectory "excel-exchange"
New-Item -ItemType Directory -Path $excelExchangeOutputDirectory -Force | Out-Null

$excelExchangePublishArgs = @(
    "publish",
    $excelExchangeProjectPath,
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
    $excelExchangeOutputDirectory
)

Write-Host "Running: $dotnetPath $($excelExchangePublishArgs -join ' ')"
& $dotnetPath @excelExchangePublishArgs

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$actDocxOutputDirectory = Join-Path $OutputDirectory "act-docx"
New-Item -ItemType Directory -Path $actDocxOutputDirectory -Force | Out-Null

$actDocxPublishArgs = @(
    "publish",
    $actDocxProjectPath,
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
    $actDocxOutputDirectory
)

Write-Host "Running: $dotnetPath $($actDocxPublishArgs -join ' ')"
& $dotnetPath @actDocxPublishArgs

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

$allowedExcelExchangeFiles = @(
    "AsutpKnowledgeBase.ExcelExchange.deps.json",
    "AsutpKnowledgeBase.ExcelExchange.dll",
    "AsutpKnowledgeBase.ExcelExchange.pdb",
    "System.IO.Packaging.dll"
)

$removedExcelExchangeFiles = @()
Get-ChildItem -LiteralPath $excelExchangeOutputDirectory -File |
    Where-Object {
        $fileName = $_.Name
        -not ($allowedExcelExchangeFiles -contains $fileName) -and
        -not $fileName.StartsWith("DocumentFormat.OpenXml", [StringComparison]::OrdinalIgnoreCase)
    } |
    ForEach-Object {
        $removedExcelExchangeFiles += $_.Name
        Remove-Item -LiteralPath $_.FullName -Force
    }

$allowedActDocxFiles = @(
    "AsutpKnowledgeBase.ActDocx.deps.json",
    "AsutpKnowledgeBase.ActDocx.dll",
    "AsutpKnowledgeBase.ActDocx.pdb",
    "System.IO.Packaging.dll"
)

$removedActDocxFiles = @()
Get-ChildItem -LiteralPath $actDocxOutputDirectory -File |
    Where-Object {
        $fileName = $_.Name
        -not ($allowedActDocxFiles -contains $fileName) -and
        -not $fileName.StartsWith("DocumentFormat.OpenXml", [StringComparison]::OrdinalIgnoreCase)
    } |
    ForEach-Object {
        $removedActDocxFiles += $_.Name
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

$expectedExcelExchangeAssembly = Join-Path $excelExchangeOutputDirectory "AsutpKnowledgeBase.ExcelExchange.dll"
if (-not (Test-Path $expectedExcelExchangeAssembly)) {
    throw "Publish succeeded but expected Excel exchange module was not found: $expectedExcelExchangeAssembly"
}

$expectedActDocxAssembly = Join-Path $actDocxOutputDirectory "AsutpKnowledgeBase.ActDocx.dll"
if (-not (Test-Path $expectedActDocxAssembly)) {
    throw "Publish succeeded but expected act DOCX module was not found: $expectedActDocxAssembly"
}

Write-Host "Publish output: $expectedExe"
Write-Host "PDF import module output: $expectedPdfImportAssembly"
Write-Host "Excel exchange module output: $expectedExcelExchangeAssembly"
Write-Host "Act DOCX module output: $expectedActDocxAssembly"
if ($removedPdfImportFiles.Count -gt 0) {
    Write-Host "Removed non-PDF module files: $($removedPdfImportFiles -join ', ')"
}
if ($removedExcelExchangeFiles.Count -gt 0) {
    Write-Host "Removed non-Excel module files: $($removedExcelExchangeFiles -join ', ')"
}
if ($removedActDocxFiles.Count -gt 0) {
    Write-Host "Removed non-act-DOCX module files: $($removedActDocxFiles -join ', ')"
}
