param(
    [string]$ExePath = 'C:\Users\Olga\AKB5\bin\Release\net8.0-windows\asutpKB.exe',
    [string]$WorkspaceRoot = 'C:\Users\Olga\AKB5',
    [string]$LogPath = 'C:\Users\Olga\AKB5\ui-smoke-network-passport.log'
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class CodexMouseClicker
{
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(int flags, int dx, int dy, int data, UIntPtr extraInfo);

    private const int LeftDown = 0x0002;
    private const int LeftUp = 0x0004;

    public static void Click(int x, int y)
    {
        SetCursorPos(x, y);
        mouse_event(LeftDown, x, y, 0, UIntPtr.Zero);
        mouse_event(LeftUp, x, y, 0, UIntPtr.Zero);
    }
}
"@

$shell = New-Object -ComObject WScript.Shell
$exeDirectory = Split-Path -Parent $ExePath
$settingsPath = Join-Path $exeDirectory 'akb5.settings.json'
if (Test-Path -LiteralPath $settingsPath) {
    $settings = Get-Content -Raw -LiteralPath $settingsPath | ConvertFrom-Json
    $configuredDatabasePath = [string]$settings.DatabasePath
    if ([IO.Path]::IsPathRooted($configuredDatabasePath)) {
        $dataPath = $configuredDatabasePath
    }
    else {
        $dataPath = Join-Path $exeDirectory $configuredDatabasePath
    }
}
else {
    $dataPath = Join-Path $exeDirectory 'database\knowledge-base.akb'
}
$backupPath = "$dataPath.codex-network-smoke-backup"
$deviceName = 'codex-net-plc'
$interfaceA = 'X1'
$interfaceB = 'X2'
$ipA = '10.250.0.10'
$ipB = '10.250.0.11'
$cableLabel = 'codex-net-w1'
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

            if ($element.Current.Name -like $NamePattern) {
                return $element
            }
        }

        return $null
    }
}

function Find-FirstFocusableEdit {
    param([System.Windows.Automation.AutomationElement]$Root)

    return Wait-Until -TimeoutSeconds 10 -FailureMessage 'Could not find a focusable edit control.' -Condition {
        foreach ($element in Get-Descendants -Root $Root) {
            if ($element.Current.ControlType -eq [System.Windows.Automation.ControlType]::Edit -and
                $element.Current.IsKeyboardFocusable) {
                return $element
            }
        }

        return $null
    }
}

function Find-Edits {
    param([System.Windows.Automation.AutomationElement]$Root)

    $edits = New-Object System.Collections.Generic.List[System.Windows.Automation.AutomationElement]
    foreach ($element in Get-Descendants -Root $Root) {
        if ($element.Current.ControlType -eq [System.Windows.Automation.ControlType]::Edit -and
            $element.Current.IsKeyboardFocusable) {
            $edits.Add($element)
        }
    }

    return $edits
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

function Wait-ForNamedWindow {
    param(
        [int]$ProcessId,
        [string]$Name,
        [int]$TimeoutSeconds = 10
    )

    return Wait-Until -TimeoutSeconds $TimeoutSeconds -FailureMessage "Window '$Name' did not appear." -Condition {
        $root = [System.Windows.Automation.AutomationElement]::RootElement
        $windows = $root.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.Condition]::TrueCondition)
        foreach ($window in $windows) {
            if ($window.Current.ProcessId -eq $ProcessId -and
                $window.Current.ControlType -eq [System.Windows.Automation.ControlType]::Window -and
                $window.Current.Name -eq $Name) {
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
            [System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.Condition]::TrueCondition)
        foreach ($window in $windows) {
            if ($window.Current.ProcessId -eq $ProcessId -and
                $window.Current.ControlType -eq [System.Windows.Automation.ControlType]::Window -and
                $window.Current.Name -eq $Name) {
                return $null
            }
        }

        return $true
    }
}

function Close-OptionalMessageBox {
    param(
        [int]$ProcessId,
        [string]$Name,
        [int]$TimeoutSeconds = 2
    )

    try {
        $messageBox = Wait-ForNamedWindow -ProcessId $ProcessId -Name $Name -TimeoutSeconds $TimeoutSeconds
    }
    catch {
        return $false
    }

    $okName = Get-OkButtonName
    $okElement = Wait-Until -TimeoutSeconds 2 -FailureMessage "Could not find '$okName'." -Condition {
        foreach ($element in Get-Descendants -Root $messageBox) {
            if ($element.Current.Name -eq $okName -or $element.Current.Name -eq 'OK') {
                return $element
            }
        }

        return $null
    }

    Press-Element -Element $okElement -ProcessId $ProcessId
    return $true
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

function Press-Element {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [int]$ProcessId
    )

    $point = $null
    try {
        $point = $Element.GetClickablePoint()
    }
    catch {
        $point = $null
    }

    if ($point) {
        [CodexMouseClicker]::Click([int]$point.X, [int]$point.Y)
        Start-Sleep -Milliseconds 350
        return
    }

    $rect = $Element.Current.BoundingRectangle
    if ($rect.Width -gt 0 -and $rect.Height -gt 0) {
        [CodexMouseClicker]::Click(
            [int]($rect.Left + ($rect.Width / 2)),
            [int]($rect.Top + ($rect.Height / 2)))
        Start-Sleep -Milliseconds 350
        return
    }

    $Element.SetFocus()
    Send-KeysToProcess -ProcessId $ProcessId -Keys '{ENTER}'
}

function Press-ElementByKeyboard {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [int]$ProcessId
    )

    $Element.SetFocus()
    Send-KeysToProcess -ProcessId $ProcessId -Keys ' '
    Start-Sleep -Milliseconds 350
}

function Dump-ControlNames {
    param([System.Windows.Automation.AutomationElement]$Root)

    $lines = foreach ($element in Get-Descendants -Root $Root) {
        '{0} :: {1}' -f $element.Current.ControlType.ProgrammaticName, $element.Current.Name
    }

    return $lines -join [Environment]::NewLine
}

function Get-SearchSystemName {
    param([string]$DataPath)

    if (-not [string]::Equals([IO.Path]::GetExtension($DataPath), '.json', [StringComparison]::OrdinalIgnoreCase)) {
        return Get-DefaultSearchSystemName
    }

    $json = Get-Content -Raw -LiteralPath $DataPath | ConvertFrom-Json
    $allCounts = @{}

    function Collect-Systems {
        param($Nodes)

        foreach ($node in $Nodes) {
            if ([int]$node.NodeType -eq 3) {
                if ($allCounts.ContainsKey($node.Name)) {
                    $allCounts[$node.Name]++
                }
                else {
                    $allCounts[$node.Name] = 1
                }
            }

            Collect-Systems -Nodes $node.Children
        }
    }

    foreach ($workshop in $json.Workshops.PSObject.Properties) {
        Collect-Systems -Nodes $workshop.Value
    }

    $roots = $json.Workshops.($json.LastWorkshop)

    function Find-UniqueSystem {
        param($Nodes)

        foreach ($node in $Nodes) {
            if ([int]$node.NodeType -eq 3 -and $allCounts[$node.Name] -eq 1) {
                return $node.Name
            }

            $found = Find-UniqueSystem -Nodes $node.Children
            if ($found) {
                return $found
            }
        }

        return $null
    }

    $name = Find-UniqueSystem -Nodes $roots
    if ([string]::IsNullOrWhiteSpace($name)) {
        throw 'Could not determine a unique system name for UI search.'
    }

    return $name
}

function Get-DefaultSearchSystemName {
    return New-UiText @(
        0x0410, 0x0421, 0x0423, 0x0020, 0x0434, 0x043E, 0x0437, 0x0430, 0x0442, 0x043E, 0x0440, 0x043E, 0x043C,
        0x0020, 0x043D, 0x0438, 0x043A, 0x0435, 0x043B, 0x0435, 0x0432, 0x043E, 0x0433, 0x043E,
        0x0020, 0x043A, 0x0443, 0x043F, 0x043E, 0x0440, 0x043E, 0x0441, 0x0430)
}

function Test-FileContainsUtf8Text {
    param(
        [string]$Path,
        [string]$Text
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return $false
    }

    $bytes = [IO.File]::ReadAllBytes($Path)
    $needle = [Text.Encoding]::UTF8.GetBytes($Text)
    if ($needle.Length -eq 0 -or $bytes.Length -lt $needle.Length) {
        return $false
    }

    for ($i = 0; $i -le $bytes.Length - $needle.Length; $i++) {
        $matched = $true
        for ($j = 0; $j -lt $needle.Length; $j++) {
            if ($bytes[$i + $j] -ne $needle[$j]) {
                $matched = $false
                break
            }
        }

        if ($matched) {
            return $true
        }
    }

    return $false
}

function Get-FindButtonName {
    return (
        ([string][char]0x041D) +
        ([string][char]0x0430) +
        ([string][char]0x0439) +
        ([string][char]0x0442) +
        ([string][char]0x0438))
}

function New-UiText {
    param([int[]]$Codes)

    return -join ($Codes | ForEach-Object { [string][char]$_ })
}

function Get-SaveButtonName {
    return New-UiText @(0x0421, 0x043E, 0x0445, 0x0440, 0x0430, 0x043D, 0x0438, 0x0442, 0x044C)
}

function Get-SaveCompletedDialogName {
    return New-UiText @(0x0421, 0x043E, 0x0445, 0x0440, 0x0430, 0x043D, 0x0435, 0x043D, 0x0438, 0x0435)
}

function Get-OkButtonName {
    return New-UiText @(0x041E, 0x041A)
}

function Get-NetworkTabName {
    return New-UiText @(0x0421, 0x0435, 0x0442, 0x044C)
}

function Get-PassportTabName {
    return New-UiText @(0x041F, 0x0430, 0x0441, 0x043F, 0x043E, 0x0440, 0x0442)
}

function Get-AddDeviceButtonName {
    return New-UiText @(0x0414, 0x043E, 0x0431, 0x0430, 0x0432, 0x0438, 0x0442, 0x044C, 0x0020, 0x0443, 0x0441, 0x0442, 0x0440, 0x043E, 0x0439, 0x0441, 0x0442, 0x0432, 0x043E)
}

function Get-AddInterfaceButtonName {
    return New-UiText @(0x0414, 0x043E, 0x0431, 0x0430, 0x0432, 0x0438, 0x0442, 0x044C, 0x0020, 0x0438, 0x043D, 0x0442, 0x0435, 0x0440, 0x0444, 0x0435, 0x0439, 0x0441)
}

function Get-AddConnectionButtonName {
    return New-UiText @(0x0414, 0x043E, 0x0431, 0x0430, 0x0432, 0x0438, 0x0442, 0x044C, 0x0020, 0x0441, 0x043E, 0x0435, 0x0434, 0x0438, 0x043D, 0x0435, 0x043D, 0x0438, 0x0435)
}

function Get-AddDeviceDialogName {
    return New-UiText @(0x0414, 0x043E, 0x0431, 0x0430, 0x0432, 0x0438, 0x0442, 0x044C, 0x0020, 0x0441, 0x0435, 0x0442, 0x0435, 0x0432, 0x043E, 0x0435, 0x0020, 0x0443, 0x0441, 0x0442, 0x0440, 0x043E, 0x0439, 0x0441, 0x0442, 0x0432, 0x043E)
}

function Get-AddInterfaceDialogName {
    return New-UiText @(0x0414, 0x043E, 0x0431, 0x0430, 0x0432, 0x0438, 0x0442, 0x044C, 0x0020, 0x0441, 0x0435, 0x0442, 0x0435, 0x0432, 0x043E, 0x0439, 0x0020, 0x0438, 0x043D, 0x0442, 0x0435, 0x0440, 0x0444, 0x0435, 0x0439, 0x0441)
}

function Get-AddConnectionDialogName {
    return New-UiText @(0x0414, 0x043E, 0x0431, 0x0430, 0x0432, 0x0438, 0x0442, 0x044C, 0x0020, 0x0441, 0x0435, 0x0442, 0x0435, 0x0432, 0x043E, 0x0435, 0x0020, 0x0441, 0x043E, 0x0435, 0x0434, 0x0438, 0x043D, 0x0435, 0x043D, 0x0438, 0x0435)
}

function Click-SaveButton {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [int]$ProcessId
    )

    $saveButton = Find-Element `
        -Root $Dialog `
        -ControlType ([System.Windows.Automation.ControlType]::Button) `
        -NamePattern (Get-SaveButtonName) `
        -TimeoutSeconds 5
    $saveButton.SetFocus()
    Send-KeysToProcess -ProcessId $ProcessId -Keys '{ENTER}'
}

if (-not (Test-Path -LiteralPath $dataPath)) {
    throw "Knowledge base file was not found at '$dataPath'."
}

$searchText = Get-SearchSystemName -DataPath $dataPath
Write-Log "Prepared Lvl2 search target: $searchText"

if (Test-Path -LiteralPath $backupPath) {
    Remove-Item -LiteralPath $backupPath -Force
}

Copy-Item -LiteralPath $dataPath -Destination $backupPath -Force
$process = $null

try {
    Write-Log 'Starting WinForms application.'
    $process = Start-Process -FilePath $ExePath -PassThru
    $mainWindow = Get-WindowElement -Process $process
    Activate-Window -ProcessId $process.Id

    Write-Log 'Selecting target system through search.'
    $searchEdit = Find-FirstFocusableEdit -Root $mainWindow
    Set-ElementValue -Element $searchEdit -Value $searchText
    Start-Sleep -Milliseconds 300
    $searchButton = Find-Element `
        -Root $mainWindow `
        -ControlType ([System.Windows.Automation.ControlType]::Button) `
        -NamePattern (Get-FindButtonName)
    Invoke-Element -Element $searchButton
    Start-Sleep -Seconds 1

    Write-Log 'Opening Network passport tab.'
    $networkTab = Find-Element `
        -Root $mainWindow `
        -ControlType ([System.Windows.Automation.ControlType]::TabItem) `
        -NamePattern ("*" + (Get-NetworkTabName) + "*") `
        -TimeoutSeconds 15
    Invoke-Element -Element $networkTab
    Start-Sleep -Milliseconds 600

    $passportTab = Find-Element `
        -Root $mainWindow `
        -ControlType ([System.Windows.Automation.ControlType]::TabItem) `
        -NamePattern (Get-PassportTabName) `
        -TimeoutSeconds 10
    Invoke-Element -Element $passportTab
    Start-Sleep -Milliseconds 300

    Write-Log 'Adding network device.'
    $addDeviceButton = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::Button) -NamePattern (Get-AddDeviceButtonName)
    Press-Element -Element $addDeviceButton -ProcessId $process.Id
    $deviceDialogName = Get-AddDeviceDialogName
    $deviceDialog = Wait-ForNamedWindow -ProcessId $process.Id -Name $deviceDialogName
    $deviceEdits = Find-Edits -Root $deviceDialog
    Set-ElementValue -Element $deviceEdits[0] -Value $deviceName
    Set-ElementValue -Element $deviceEdits[1] -Value 'Controller'
    Set-ElementValue -Element $deviceEdits[3] -Value 'CPU smoke'
    Set-ElementValue -Element $deviceEdits[7] -Value 'codex-net-plc'
    Set-ElementValue -Element $deviceEdits[8] -Value '00-00-00-25-00-10'
    Click-SaveButton -Dialog $deviceDialog -ProcessId $process.Id
    Wait-ForDialogToClose -ProcessId $process.Id -Name $deviceDialogName
    Start-Sleep -Milliseconds 500

    Write-Log 'Adding first interface.'
    $addInterfaceButton = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::Button) -NamePattern (Get-AddInterfaceButtonName)
    Press-Element -Element $addInterfaceButton -ProcessId $process.Id
    $interfaceDialogName = Get-AddInterfaceDialogName
    $interfaceDialog = Wait-ForNamedWindow -ProcessId $process.Id -Name $interfaceDialogName
    $interfaceEdits = Find-Edits -Root $interfaceDialog
    Set-ElementValue -Element $interfaceEdits[0] -Value $interfaceA
    Set-ElementValue -Element $interfaceEdits[1] -Value '1'
    Set-ElementValue -Element $interfaceEdits[2] -Value '00-00-00-25-00-11'
    Set-ElementValue -Element $interfaceEdits[3] -Value $ipA
    Set-ElementValue -Element $interfaceEdits[4] -Value '255.255.255.0'
    Set-ElementValue -Element $interfaceEdits[7] -Value 'PROFINET'
    Click-SaveButton -Dialog $interfaceDialog -ProcessId $process.Id
    Wait-ForDialogToClose -ProcessId $process.Id -Name $interfaceDialogName
    Start-Sleep -Milliseconds 500

    Write-Log 'Adding second interface.'
    Press-Element -Element $addInterfaceButton -ProcessId $process.Id
    $interfaceDialog = Wait-ForNamedWindow -ProcessId $process.Id -Name $interfaceDialogName
    $interfaceEdits = Find-Edits -Root $interfaceDialog
    Set-ElementValue -Element $interfaceEdits[0] -Value $interfaceB
    Set-ElementValue -Element $interfaceEdits[1] -Value '2'
    Set-ElementValue -Element $interfaceEdits[2] -Value '00-00-00-25-00-12'
    Set-ElementValue -Element $interfaceEdits[3] -Value $ipB
    Set-ElementValue -Element $interfaceEdits[4] -Value '255.255.255.0'
    Set-ElementValue -Element $interfaceEdits[7] -Value 'PROFINET'
    Click-SaveButton -Dialog $interfaceDialog -ProcessId $process.Id
    Wait-ForDialogToClose -ProcessId $process.Id -Name $interfaceDialogName
    Start-Sleep -Milliseconds 500

    Write-Log 'Adding connection.'
    $addConnectionButton = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::Button) -NamePattern (Get-AddConnectionButtonName)
    Press-Element -Element $addConnectionButton -ProcessId $process.Id
    $connectionDialogName = Get-AddConnectionDialogName
    $connectionDialog = Wait-ForNamedWindow -ProcessId $process.Id -Name $connectionDialogName
    $connectionEdits = Find-Edits -Root $connectionDialog
    Set-ElementValue -Element $connectionEdits[0] -Value $cableLabel
    Set-ElementValue -Element $connectionEdits[1] -Value 'PROFINET'
    Set-ElementValue -Element $connectionEdits[3] -Value 'active'
    Click-SaveButton -Dialog $connectionDialog -ProcessId $process.Id
    Wait-ForDialogToClose -ProcessId $process.Id -Name $connectionDialogName
    Start-Sleep -Milliseconds 700
    $null = Close-OptionalMessageBox -ProcessId $process.Id -Name (Get-NetworkTabName)

    $null = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::ListItem) -NamePattern "*$deviceName*" -TimeoutSeconds 10
    $null = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::Text) -NamePattern "*$ipA*" -TimeoutSeconds 10
    $null = Find-Element -Root $mainWindow -ControlType ([System.Windows.Automation.ControlType]::Text) -NamePattern "*$cableLabel*" -TimeoutSeconds 10

    Write-Log 'Saving changes through toolbar.'
    $null = Close-OptionalMessageBox -ProcessId $process.Id -Name (Get-NetworkTabName)
    $mainSaveButton = Find-Element `
        -Root $mainWindow `
        -ControlType ([System.Windows.Automation.ControlType]::Button) `
        -NamePattern ("*" + (Get-SaveButtonName) + "*") `
        -TimeoutSeconds 5
    Press-Element -Element $mainSaveButton -ProcessId $process.Id
    $null = Wait-Until -TimeoutSeconds 10 -FailureMessage 'Saved data did not contain smoke records.' -Condition {
        return (Test-FileContainsUtf8Text -Path $dataPath -Text $deviceName) -and
            (Test-FileContainsUtf8Text -Path $dataPath -Text $ipA) -and
            (Test-FileContainsUtf8Text -Path $dataPath -Text $cableLabel)
    }
    $null = Close-OptionalMessageBox -ProcessId $process.Id -Name (Get-SaveCompletedDialogName)

    Write-Log 'Closing the application.'
    if (-not $process.CloseMainWindow()) {
        Send-KeysToProcess -ProcessId $process.Id -Keys '%{F4}'
    }
    $null = Wait-Until -TimeoutSeconds 15 -FailureMessage 'Application did not exit after close request.' -Condition {
        $process.Refresh()
        return $process.HasExited
    }

    $summary = [pscustomobject]@{
        SearchNode = $searchText
        DataPath = $dataPath
        DeviceSeen = Test-FileContainsUtf8Text -Path $dataPath -Text $deviceName
        InterfaceSeen = Test-FileContainsUtf8Text -Path $dataPath -Text $ipA
        ConnectionSeen = Test-FileContainsUtf8Text -Path $dataPath -Text $cableLabel
    } | ConvertTo-Json -Compress

    Write-Log "Network smoke succeeded: $summary"
}
catch {
    Write-Log "Network smoke failed: $($_.Exception.Message)"

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

    Write-Log "Smoke log written to $LogPath"
}
