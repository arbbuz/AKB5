param(
    [string]$ExePath = 'C:\Users\Olga\AKB5\bin\Release\net8.0-windows\asutpKB.exe',
    [string]$WorkspaceRoot = 'C:\Users\Olga\AKB5',
    [string]$LogPath = 'C:\Users\Olga\AKB5\ui-smoke-docs-software.log'
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$shell = New-Object -ComObject WScript.Shell
$dataPath = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'ASUTP_KnowledgeBase.json'
$backupPath = "$dataPath.codex-ui-smoke-backup"
$markerCommandPath = Join-Path $WorkspaceRoot 'ui-smoke-open.cmd'
$markerResultPath = Join-Path $WorkspaceRoot 'ui-smoke-opened.txt'
$schemeTitle = 'codex-scheme-smoke'
$editedSchemeTitle = 'codex-scheme-edited'
$softwareTitle = 'codex-software-smoke'
$mainWindow = $null

Set-Content -LiteralPath $LogPath -Encoding UTF8 -Value ''

function Write-Log {
    param([string]$Message)

    $timestamped = "[{0}] {1}" -f (Get-Date -Format 'HH:mm:ss'), $Message
    Write-Host $timestamped
    Add-Content -LiteralPath $LogPath -Encoding UTF8 -Value $timestamped
}

function Write-LogBlock {
    param([string]$Message)

    Write-Host $Message
    Add-Content -LiteralPath $LogPath -Encoding UTF8 -Value $Message
}

function Wait-Until {
    param(
        [scriptblock]$Condition,
        [int]$TimeoutSeconds = 15,
        [string]$FailureMessage = 'Condition was not satisfied.'
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $result = & $Condition
        if ($result) {
            return $result
        }

        Start-Sleep -Milliseconds 250
    }

    throw $FailureMessage
}

function Get-WindowElement {
    param([System.Diagnostics.Process]$Process)

    return Wait-Until -TimeoutSeconds 20 -FailureMessage 'Main window handle was not created.' -Condition {
        $Process.Refresh()
        if ($Process.HasExited) {
            throw 'Application exited before the main window appeared.'
        }

        if ($Process.MainWindowHandle -ne 0) {
            return [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$Process.MainWindowHandle)
        }

        return $null
    }
}

function Get-Descendants {
    param([System.Windows.Automation.AutomationElement]$Root)

    if ($null -eq $Root) {
        throw 'Automation root is null.'
    }

    return $Root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
}

function Find-Element {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [System.Windows.Automation.ControlType]$ControlType,
        [string]$NamePattern,
        [int]$TimeoutSeconds = 10
    )

    return Wait-Until -TimeoutSeconds $TimeoutSeconds -FailureMessage "Could not find '$NamePattern'." -Condition {
        $elements = Get-Descendants -Root $Root
        foreach ($element in $elements) {
            if ($element.Current.ControlType -ne $ControlType) {
                continue
            }

            $name = $element.Current.Name
            if ($name -like $NamePattern) {
                return $element
            }
        }

        return $null
    }
}

function Find-FirstFocusableEdit {
    param([System.Windows.Automation.AutomationElement]$Root)

    return Wait-Until -TimeoutSeconds 10 -FailureMessage 'Could not find a focusable edit control.' -Condition {
        $elements = Get-Descendants -Root $Root
        foreach ($element in $elements) {
            if ($element.Current.ControlType -eq [System.Windows.Automation.ControlType]::Edit -and
                $element.Current.IsKeyboardFocusable) {
                return $element
            }
        }

        return $null
    }
}

function Set-ElementValue {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [string]$Value
    )

    $pattern = $null
    if (-not $Element.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$pattern)) {
        $pattern = $null
    }

    if ($null -eq $pattern) {
        throw "Element '$($Element.Current.Name)' does not support ValuePattern."
    }

    $pattern.SetValue($Value)
}

function Invoke-Element {
    param([System.Windows.Automation.AutomationElement]$Element)

    $invoke = $null
    if (-not $Element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$invoke)) {
        $invoke = $null
    }

    if ($invoke -ne $null) {
        $invoke.Invoke()
        return
    }

    $select = $null
    if (-not $Element.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$select)) {
        $select = $null
    }

    if ($select -ne $null) {
        $select.Select()
        return
    }

    throw "Element '$($Element.Current.Name)' does not support InvokePattern or SelectionItemPattern."
}

function Select-Item {
    param([System.Windows.Automation.AutomationElement]$Element)

    $select = $null
    if (-not $Element.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$select)) {
        $select = $null
    }

    if ($select -eq $null) {
        throw "Element '$($Element.Current.Name)' does not support SelectionItemPattern."
    }

    $select.Select()
}

function Wait-ForNamedWindow {
    param(
        [int]$ProcessId,
        [string]$Name,
        [int]$TimeoutSeconds = 10
    )

    return Wait-Until -TimeoutSeconds $TimeoutSeconds -FailureMessage "Window '$Name' did not appear." -Condition {
        $root = [System.Windows.Automation.AutomationElement]::RootElement
        $windows = $root.FindAll(
            [System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.Condition]::TrueCondition)
        foreach ($window in $windows) {
            if ($window.Current.ProcessId -eq $ProcessId -and $window.Current.Name -eq $Name) {
                return $window
            }
        }

        return $null
    }
}

function Wait-ForDialogToClose {
    param(
        [int]$ProcessId,
        [string]$Name,
        [int]$TimeoutSeconds = 10
    )

    $null = Wait-Until -TimeoutSeconds $TimeoutSeconds -FailureMessage "Dialog '$Name' did not close." -Condition {
        $root = [System.Windows.Automation.AutomationElement]::RootElement
        $windows = $root.FindAll(
            [System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.Condition]::TrueCondition)
        foreach ($window in $windows) {
            if ($window.Current.ProcessId -eq $ProcessId -and $window.Current.Name -eq $Name) {
                return $null
            }
        }

        return $true
    }
}

function Activate-Window {
    param([int]$ProcessId)

    $null = $shell.AppActivate($ProcessId)
    Start-Sleep -Milliseconds 300
}

function Send-KeysToProcess {
    param(
        [int]$ProcessId,
        [string]$Keys
    )

    Activate-Window -ProcessId $ProcessId
    $shell.SendKeys($Keys)
    Start-Sleep -Milliseconds 350
}

function Dump-ControlNames {
    param([System.Windows.Automation.AutomationElement]$Root)

    $lines = foreach ($element in Get-Descendants -Root $Root) {
        '{0} :: {1}' -f $element.Current.ControlType.ProgrammaticName, $element.Current.Name
    }

    return $lines -join [Environment]::NewLine
}

function Get-FindButtonName {
    return (
        ([string][char]0x041D) +
        ([string][char]0x0430) +
        ([string][char]0x0439) +
        ([string][char]0x0442) +
        ([string][char]0x0438))
}

function Get-SearchNodeName {
    param([string]$JsonPath)

    $json = Get-Content -Raw -LiteralPath $JsonPath | ConvertFrom-Json
    $allCounts = @{}

    function Collect-Cabinets {
        param($Nodes)

        foreach ($node in $Nodes) {
            if ([int]$node.NodeType -eq 4) {
                if ($allCounts.ContainsKey($node.Name)) {
                    $allCounts[$node.Name]++
                }
                else {
                    $allCounts[$node.Name] = 1
                }
            }

            Collect-Cabinets -Nodes $node.Children
        }
    }

    foreach ($workshop in $json.Workshops.PSObject.Properties) {
        Collect-Cabinets -Nodes $workshop.Value
    }

    $roots = $json.Workshops.($json.LastWorkshop)

    function Find-UniqueCabinet {
        param($Nodes)

        foreach ($node in $Nodes) {
            if ([int]$node.NodeType -eq 4 -and $allCounts[$node.Name] -eq 1) {
                return $node.Name
            }

            $found = Find-UniqueCabinet -Nodes $node.Children
            if ($found) {
                return $found
            }
        }

        return $null
    }

    $name = Find-UniqueCabinet -Nodes $roots
    if ([string]::IsNullOrWhiteSpace($name)) {
        throw 'Could not determine a unique cabinet name for UI search.'
    }

    return $name
}

if (-not (Test-Path -LiteralPath $dataPath)) {
    throw "Knowledge base file was not found at '$dataPath'."
}

$searchText = Get-SearchNodeName -JsonPath $dataPath
Write-Log "Prepared search target: $searchText"

if (Test-Path -LiteralPath $backupPath) {
    Remove-Item -LiteralPath $backupPath -Force
}

Copy-Item -LiteralPath $dataPath -Destination $backupPath -Force
Set-Content -LiteralPath $markerCommandPath -Encoding ASCII -Value "@echo off`r`n> `"$markerResultPath`" echo opened`r`n"
if (Test-Path -LiteralPath $markerResultPath) {
    Remove-Item -LiteralPath $markerResultPath -Force
}

$process = $null
$savedSnapshot = $null

try {
    Write-Log 'Starting WinForms application.'
    $process = Start-Process -FilePath $ExePath -PassThru
    $mainWindow = Get-WindowElement -Process $process
    Activate-Window -ProcessId $process.Id

    Write-Log 'Selecting target cabinet through search.'
    $searchEdit = Find-FirstFocusableEdit -Root $mainWindow
    Set-ElementValue -Element $searchEdit -Value $searchText
    Start-Sleep -Milliseconds 300
    $searchButton = Find-Element `
        -Root $mainWindow `
        -ControlType ([System.Windows.Automation.ControlType]::Button) `
        -NamePattern (Get-FindButtonName)
    Invoke-Element -Element $searchButton
    Start-Sleep -Seconds 1

    Write-Log 'Opening Documentation and Software tab.'
    $docsTab = Find-Element `
        -Root $mainWindow `
        -ControlType ([System.Windows.Automation.ControlType]::TabItem) `
        -NamePattern 'Documentation and Software' `
        -TimeoutSeconds 15
    Invoke-Element -Element $docsTab
    Start-Sleep -Seconds 1

    Write-Log 'Adding a scheme link.'
    $addSchemeButton = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::Button) -NamePattern 'Add Scheme...'
    Invoke-Element -Element $addSchemeButton
    $null = Wait-ForNamedWindow -ProcessId $process.Id -Name 'Add Scheme Link'
    Send-KeysToProcess -ProcessId $process.Id -Keys "{TAB}$schemeTitle{TAB}$markerCommandPath{ENTER}"
    Wait-ForDialogToClose -ProcessId $process.Id -Name 'Add Scheme Link'

    Write-Log 'Adding a software record.'
    $addSoftwareButton = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::Button) -NamePattern 'Add Software...'
    Invoke-Element -Element $addSoftwareButton
    $null = Wait-ForNamedWindow -ProcessId $process.Id -Name 'Add Software Record'
    Send-KeysToProcess -ProcessId $process.Id -Keys "$softwareTitle{TAB}$markerCommandPath{ENTER}"
    Wait-ForDialogToClose -ProcessId $process.Id -Name 'Add Software Record'

    Write-Log 'Opening the software record path.'
    $softwareItem = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::DataItem) -NamePattern "*$softwareTitle*"
    Select-Item -Element $softwareItem
    Start-Sleep -Milliseconds 300
    $openButton = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::Button) -NamePattern 'Open Selected'
    Invoke-Element -Element $openButton
    $null = Wait-Until -TimeoutSeconds 10 -FailureMessage 'Open action did not create the expected marker file.' -Condition {
        Test-Path -LiteralPath $markerResultPath
    }

    Write-Log 'Editing the scheme link title.'
    $schemeItem = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::DataItem) -NamePattern "*$schemeTitle*"
    Select-Item -Element $schemeItem
    Start-Sleep -Milliseconds 300
    $editButton = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::Button) -NamePattern 'Edit Selected...'
    Invoke-Element -Element $editButton
    $null = Wait-ForNamedWindow -ProcessId $process.Id -Name 'Edit Document Link'
    Send-KeysToProcess -ProcessId $process.Id -Keys "{TAB}^a$editedSchemeTitle{TAB}{ENTER}"
    Wait-ForDialogToClose -ProcessId $process.Id -Name 'Edit Document Link'

    Write-Log 'Deleting the edited scheme link.'
    $editedSchemeItem = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::DataItem) -NamePattern "*$editedSchemeTitle*"
    Select-Item -Element $editedSchemeItem
    Start-Sleep -Milliseconds 300
    $deleteButton = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::Button) -NamePattern 'Delete Selected'
    Invoke-Element -Element $deleteButton
    $null = Wait-ForNamedWindow -ProcessId $process.Id -Name 'Documentation and Software'
    Send-KeysToProcess -ProcessId $process.Id -Keys '{ENTER}'
    Start-Sleep -Seconds 1

    Write-Log 'Closing the application and confirming save.'
    Send-KeysToProcess -ProcessId $process.Id -Keys '%{F4}'
    Start-Sleep -Milliseconds 700
    Send-KeysToProcess -ProcessId $process.Id -Keys '{ENTER}'
    $null = Wait-Until -TimeoutSeconds 15 -FailureMessage 'Application did not exit after Alt+F4.' -Condition {
        $process.Refresh()
        return $process.HasExited
    }

    Write-Log 'Inspecting saved JSON snapshot before restore.'
    $savedSnapshot = Get-Content -Raw -LiteralPath $dataPath | ConvertFrom-Json
    $softwareRecords = @($savedSnapshot.SoftwareRecords)
    $documentLinks = @($savedSnapshot.DocumentLinks)

    $summary = [pscustomobject]@{
        SearchNode = $searchText
        DocumentLinksCount = $documentLinks.Count
        SoftwareRecordsCount = $softwareRecords.Count
        SavedSoftwareTitles = ($softwareRecords | ForEach-Object { $_.Title }) -join '; '
        MarkerCreated = (Test-Path -LiteralPath $markerResultPath)
    } | ConvertTo-Json -Compress

    Write-Log "Smoke test succeeded: $summary"
}
catch {
    Write-Log "Smoke test failed: $($_.Exception.Message)"

    if ($mainWindow) {
        Write-LogBlock '--- UI dump start ---'
        Write-LogBlock (Dump-ControlNames -Root $mainWindow)
        Write-LogBlock '--- UI dump end ---'
    }

    throw
}
finally {
    if ($process -and -not $process.HasExited) {
        try {
            $process.CloseMainWindow() | Out-Null
            if (-not $process.WaitForExit(5000)) {
                $process.Kill()
                $process.WaitForExit()
            }
        }
        catch {
        }
    }

    if (Test-Path -LiteralPath $backupPath) {
        Copy-Item -LiteralPath $backupPath -Destination $dataPath -Force
        Remove-Item -LiteralPath $backupPath -Force
    }

    foreach ($path in @($markerCommandPath, $markerResultPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }

    Write-Log "Smoke log written to $LogPath"
}
