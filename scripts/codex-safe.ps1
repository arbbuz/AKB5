param(
    [Parameter(Mandatory = $true)]
    [string]$Command,

    [int]$MaxOutputLines = 120,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Add-DenyReason {
    param(
        [System.Collections.Generic.List[string]]$Reasons,
        [string]$Reason
    )

    $Reasons.Add($Reason) | Out-Null
}

function Test-CodexCommand {
    param([string]$RawCommand)

    $reasons = [System.Collections.Generic.List[string]]::new()
    $normalized = ($RawCommand -replace '\s+', ' ').Trim()

    if ([string]::IsNullOrWhiteSpace($normalized)) {
        Add-DenyReason $reasons "empty command"
    }

    if ($normalized -match '(?i)\b(Get-Content|gc|type)\b.*\b-Raw\b') {
        Add-DenyReason $reasons "raw file reads are blocked; use -TotalCount, -Tail, Select-String, or a path-limited parser"
    }

    if ($normalized -match '(?i)\b(Get-Content|gc|type)\b(?!.*\b(-TotalCount|-Tail)\b).*docs[\\/]+decision-log\.md') {
        Add-DenyReason $reasons "full decision-log reads are blocked; use Select-String or line-limited reads"
    }

    if ($normalized -match '(?i)\b(Get-Content|gc|type)\b.*docs[\\/]+\*') {
        Add-DenyReason $reasons "wildcard docs reads are blocked"
    }

    if ($normalized -match '(?i)^git\s+diff(\s|$)' -and
        $normalized -notmatch '(?i)\s--(stat|check|name-only|name-status|numstat)\b' -and
        $normalized -notmatch '\s--\s+\S+') {
        Add-DenyReason $reasons "full git diff is blocked; use --stat, --check, --name-only, or path-limited hunks"
    }

    if ($normalized -match '(?i)^rg(\.exe)?\b.*\s(\.|\*)\s*$') {
        Add-DenyReason $reasons "repo-wide rg is blocked; provide a narrow file or directory path"
    }

    if ($normalized -match '(?i)\bGet-ChildItem\b.*\b-Recurse\b' -and
        $normalized -notmatch '(?i)\b-(Filter|Include)\b') {
        Add-DenyReason $reasons "recursive file enumeration without -Filter or -Include is blocked"
    }

    if ($normalized -match '(?i)^git\s+(reset\s+--hard|clean\b|checkout\s+--)') {
        Add-DenyReason $reasons "destructive git commands are blocked"
    }

    if ($normalized -match '(?i)\bRemove-Item\b.*\b-Recurse\b') {
        Add-DenyReason $reasons "recursive delete is blocked"
    }

    return $reasons
}

if ($MaxOutputLines -lt 20) {
    $MaxOutputLines = 20
}

$denyReasons = [string[]]@(Test-CodexCommand $Command)
if ($denyReasons.Length -gt 0) {
    Write-Host "BLOCKED by scripts/codex-safe.ps1:"
    foreach ($reason in $denyReasons) {
        Write-Host "- $reason"
    }
    exit 64
}

if ($DryRun) {
    Write-Host "ALLOWED: $Command"
    exit 0
}

$global:LASTEXITCODE = 0
$previousErrorActionPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = "Continue"
    $output = Invoke-Expression $Command 2>&1
    $exitCode = if ($null -ne $global:LASTEXITCODE) { [int]$global:LASTEXITCODE } else { 0 }
}
catch {
    Write-Host $_.Exception.Message
    exit 1
}
finally {
    $ErrorActionPreference = $previousErrorActionPreference
}

$text = ($output | Out-String -Width 220).TrimEnd()
if (-not [string]::IsNullOrWhiteSpace($text)) {
    $lines = $text -split "`r?`n"
    if ($lines.Count -le $MaxOutputLines) {
        $lines | ForEach-Object { Write-Host $_ }
    }
    else {
        $headCount = [Math]::Floor($MaxOutputLines / 2)
        $tailCount = $MaxOutputLines - $headCount
        $lines | Select-Object -First $headCount | ForEach-Object { Write-Host $_ }
        Write-Host ("... output truncated by scripts/codex-safe.ps1 ({0} lines omitted) ..." -f ($lines.Count - $MaxOutputLines))
        $lines | Select-Object -Last $tailCount | ForEach-Object { Write-Host $_ }
    }
}

exit $exitCode
