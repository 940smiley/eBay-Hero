[CmdletBinding()]
param([switch]$SelfTest, [switch]$SelfTestClick)

. (Join-Path $PSScriptRoot 'common.ps1')
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$repo = Get-RepoRoot
$manifestPath = Join-Path $repo 'scripts\scripts.manifest.json'
$logRoot = Join-Path $repo 'artifacts\logs\control-center'
New-Item -ItemType Directory -Force -Path $logRoot | Out-Null

$script:currentProcess = $null
$script:processTimer = $null
$script:lastCommand = ''
$script:lastStdOut = ''
$script:lastStdErr = ''
$script:lastLog = ''
$script:stdoutPath = ''
$script:stderrPath = ''
$script:stdoutPosition = 0L
$script:stderrPosition = 0L
$actionsList = $null
$buttons = $null
$outputBox = $null
$errorBox = $null
$statusLabel = $null
$commandBox = $null

[System.Windows.Forms.Application]::SetUnhandledExceptionMode([System.Windows.Forms.UnhandledExceptionMode]::CatchException)

function Write-ControlCenterLog {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($script:lastLog)) { return }

    try {
        Add-Content -LiteralPath $script:lastLog -Value (Redact-Text $Text) -ErrorAction Stop
    } catch {
        # Logging must never bring down the Control Center.
    }
}

function Report-ControlCenterError {
    param([string]$Message)
    $safeMessage = Redact-Text $Message
    $script:lastStdErr += $safeMessage + [Environment]::NewLine
    Write-ControlCenterLog ('ERR: ' + $safeMessage)

    if ($statusLabel -and -not $statusLabel.IsDisposed) {
        try { $statusLabel.Text = "Control Center error: $safeMessage" } catch { }
    }

    if ($errorBox -and -not $errorBox.IsDisposed) {
        try { $errorBox.AppendText("Control Center error: $safeMessage" + [Environment]::NewLine) } catch { }
    }
}

[System.Windows.Forms.Application]::add_ThreadException({
    param($sender, [System.Threading.ThreadExceptionEventArgs]$eventArgs)
    Report-ControlCenterError $eventArgs.Exception.Message
})

[AppDomain]::CurrentDomain.add_UnhandledException({
    param($sender, [UnhandledExceptionEventArgs]$eventArgs)
    if ($eventArgs.ExceptionObject -is [Exception]) {
        Report-ControlCenterError $eventArgs.ExceptionObject.Message
    } else {
        Report-ControlCenterError ([string]$eventArgs.ExceptionObject)
    }
})

function Load-Actions {
    if (-not (Test-Path -LiteralPath $manifestPath)) { throw "Missing manifest: $manifestPath" }
    return (Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json).actions
}

function Get-ActionCommand {
    param($Action, [switch]$DryRun)
    $scriptPath = Join-Path $repo $Action.script
    $args = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$scriptPath)
    foreach ($arg in @($Action.arguments)) { $args += $arg }
    if ($DryRun -and $Action.supportsDryRun -and @($Action.arguments) -notcontains '-DryRun') { $args += '-DryRun' }
    $shell = if ($PSVersionTable.PSEdition -eq 'Core') { 'pwsh' } else { 'powershell.exe' }
    return [pscustomobject]@{
        File = $shell
        Args = $args
        Text = ($shell + ' ' + (($args | ForEach-Object { Quote-Argument $_ }) -join ' '))
    }
}

function Quote-Argument {
    param([string]$Value)
    if ($Value -notmatch '[\s"]') { return $Value }
    return '"' + ($Value.Replace('"', '\"')) + '"'
}

function Append-Output {
    param([System.Windows.Forms.TextBox]$TextBox, [string]$Text)
    if ([string]::IsNullOrEmpty($Text)) { return }
    $safe = Redact-Text $Text
    if ($null -eq $TextBox -or $TextBox.IsDisposed) {
        return
    }

    try {
        if ($null -ne $TextBox -and -not $TextBox.IsDisposed) {
            $TextBox.AppendText($safe + [Environment]::NewLine)
        }
    } catch {
        Report-ControlCenterError $_.Exception.Message
    }
}

function Populate-List {
    param([System.Windows.Forms.ListView]$List)
    $List.Items.Clear()
    foreach ($action in Load-Actions | Sort-Object category, name) {
        $item = New-Object System.Windows.Forms.ListViewItem($action.category)
        [void]$item.SubItems.Add($action.name)
        [void]$item.SubItems.Add($action.description)
        [void]$item.SubItems.Add($action.id)
        $item.Tag = $action
        [void]$List.Items.Add($item)
    }
}

function Invoke-SafeUiAction {
    param([scriptblock]$Action)
    try {
        & $Action
    } catch {
        $message = $_.Exception.Message
        Report-ControlCenterError $message
        [System.Windows.Forms.MessageBox]::Show(
            $message,
            'eBay-Hero Control Center',
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error
        ) | Out-Null
    }
}

function Run-Action {
    param($Action, [switch]$DryRun)
    if ($script:currentProcess -and -not $script:currentProcess.HasExited) {
        [System.Windows.Forms.MessageBox]::Show('A command is already running.')
        return
    }

    $command = Get-ActionCommand -Action $Action -DryRun:$DryRun
    $script:lastCommand = $command.Text
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    $script:lastLog = Join-Path $logRoot "$($Action.id)-$timestamp.log"
    $outputBox.Clear()
    $errorBox.Clear()
    $script:lastStdOut = ''
    $script:lastStdErr = ''
    $statusLabel.Text = "Running: $($Action.name)"
    $commandBox.Text = $command.Text
    Write-ControlCenterLog "COMMAND: $($command.Text)"

    try {
        $script:stdoutPath = "$($script:lastLog).stdout.tmp"
        $script:stderrPath = "$($script:lastLog).stderr.tmp"
        $script:stdoutPosition = 0L
        $script:stderrPosition = 0L
        Remove-Item -LiteralPath $script:stdoutPath, $script:stderrPath -Force -ErrorAction SilentlyContinue

        $argumentText = ($command.Args | ForEach-Object { Quote-Argument $_ }) -join ' '
        $cmdLine = '"' + $command.File + '" ' + $argumentText + ' 1> "' + $script:stdoutPath + '" 2> "' + $script:stderrPath + '"'
        $cmdPath = if ([string]::IsNullOrWhiteSpace($env:ComSpec)) { 'cmd.exe' } else { $env:ComSpec }
        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName = $cmdPath
        $psi.Arguments = '/d /s /c "' + $cmdLine + '"'
        $psi.WorkingDirectory = $repo
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true

        $process = [System.Diagnostics.Process]::new()
        $process.StartInfo = $psi
        [void]$process.Start()
        $script:currentProcess = $process

        if ($null -eq $script:processTimer) {
            $script:processTimer = New-Object System.Windows.Forms.Timer
            $script:processTimer.Interval = 250
            $script:processTimer.Add_Tick({ Update-RunningProcess }.GetNewClosure())
        }

        $script:processTimer.Start()
    } catch {
        $statusLabel.Text = "Failed to start: $($_.Exception.Message)"
        $errorBox.AppendText("Failed to start command: $($_.Exception.Message)" + [Environment]::NewLine)
        throw
    }
}

function Read-NewProcessText {
    param([string]$Path, [long]$StartPosition)
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) {
        return [pscustomobject]@{ Text = ''; Position = $StartPosition }
    }

    try {
        $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        try {
            if ($StartPosition -gt $stream.Length) { $StartPosition = 0L }
            [void]$stream.Seek($StartPosition, [System.IO.SeekOrigin]::Begin)
            $reader = New-Object System.IO.StreamReader($stream)
            try {
                $text = $reader.ReadToEnd()
                return [pscustomobject]@{ Text = $text; Position = $stream.Position }
            } finally {
                $reader.Dispose()
            }
        } finally {
            $stream.Dispose()
        }
    } catch {
        Report-ControlCenterError $_.Exception.Message
        return [pscustomobject]@{ Text = ''; Position = $StartPosition }
    }
}

function Append-ProcessText {
    param([System.Windows.Forms.TextBox]$TextBox, [string]$Text, [switch]$IsError)
    if ([string]::IsNullOrEmpty($Text)) { return }

    $normalized = $Text.Replace("`r`n", "`n").Replace("`r", "`n")
    foreach ($line in $normalized.Split("`n")) {
        if ([string]::IsNullOrEmpty($line)) { continue }
        if ($IsError) {
            $script:lastStdErr += $line + [Environment]::NewLine
            Append-Output $TextBox $line
            Write-ControlCenterLog ('ERR: ' + $line)
        } else {
            $script:lastStdOut += $line + [Environment]::NewLine
            Append-Output $TextBox $line
            Write-ControlCenterLog $line
        }
    }
}

function Drain-ProcessOutput {
    $stdout = Read-NewProcessText -Path $script:stdoutPath -StartPosition $script:stdoutPosition
    $script:stdoutPosition = $stdout.Position
    Append-ProcessText -TextBox $outputBox -Text $stdout.Text

    $stderr = Read-NewProcessText -Path $script:stderrPath -StartPosition $script:stderrPosition
    $script:stderrPosition = $stderr.Position
    Append-ProcessText -TextBox $errorBox -Text $stderr.Text -IsError
}

function Update-RunningProcess {
    try {
        Drain-ProcessOutput
        if ($script:currentProcess -and $script:currentProcess.HasExited) {
            if ($script:processTimer) { $script:processTimer.Stop() }
            Drain-ProcessOutput
            $exit = $script:currentProcess.ExitCode
            $statusLabel.Text = "Exit code: $exit"
            Write-ControlCenterLog "EXIT: $exit"
            if ($exit -ne 0) {
                $bundle = New-FailureBundle -Command $script:lastCommand -ExitCode $exit -StdOut $script:lastStdOut -StdErr $script:lastStdErr
                $errorBox.AppendText("Failure bundle: $bundle" + [Environment]::NewLine)
            }
        }
    } catch {
        Report-ControlCenterError $_.Exception.Message
    }
}

function Get-SelectedAction {
    if ($actionsList.SelectedItems.Count -eq 0) {
        [System.Windows.Forms.MessageBox]::Show('Select an action first.')
        return $null
    }

    return $actionsList.SelectedItems[0].Tag
}

function Add-ControlButton {
    param([string]$Text, [scriptblock]$OnClick)
    $button = New-Object System.Windows.Forms.Button
    $button.Text = $Text
    $button.AutoSize = $true
    $handler = $OnClick.GetNewClosure()
    $button.Add_Click({ Invoke-SafeUiAction -Action $handler }.GetNewClosure())
    [void]$buttons.Controls.Add($button)
    return $button
}

$form = New-Object System.Windows.Forms.Form
$form.Text = 'eBay-Hero Control Center'
$form.Width = 1250
$form.Height = 820
$form.StartPosition = 'CenterScreen'

$main = New-Object System.Windows.Forms.SplitContainer
$main.Dock = 'Fill'
$main.Orientation = 'Vertical'
$main.SplitterDistance = 560
$form.Controls.Add($main)

$actionsList = New-Object System.Windows.Forms.ListView
$actionsList.Dock = 'Fill'
$actionsList.View = 'Details'
$actionsList.FullRowSelect = $true
$actionsList.MultiSelect = $false
$actionsList.HideSelection = $false
[void]$actionsList.Columns.Add('Category', 130)
[void]$actionsList.Columns.Add('Action', 170)
[void]$actionsList.Columns.Add('Description', 360)
[void]$actionsList.Columns.Add('ID', 120)
$main.Panel1.Controls.Add($actionsList)

$rightPanel = New-Object System.Windows.Forms.TableLayoutPanel
$rightPanel.Dock = 'Fill'
$rightPanel.RowCount = 5
$rightPanel.ColumnCount = 1
$rightPanel.RowStyles.Add((New-Object System.Windows.Forms.RowStyle([System.Windows.Forms.SizeType]::Absolute, 42))) | Out-Null
$rightPanel.RowStyles.Add((New-Object System.Windows.Forms.RowStyle([System.Windows.Forms.SizeType]::Absolute, 48))) | Out-Null
$rightPanel.RowStyles.Add((New-Object System.Windows.Forms.RowStyle([System.Windows.Forms.SizeType]::Percent, 45))) | Out-Null
$rightPanel.RowStyles.Add((New-Object System.Windows.Forms.RowStyle([System.Windows.Forms.SizeType]::Percent, 45))) | Out-Null
$rightPanel.RowStyles.Add((New-Object System.Windows.Forms.RowStyle([System.Windows.Forms.SizeType]::Absolute, 28))) | Out-Null
$main.Panel2.Controls.Add($rightPanel)

$buttons = New-Object System.Windows.Forms.FlowLayoutPanel
$buttons.Dock = 'Fill'
$rightPanel.Controls.Add($buttons, 0, 0)

$runSelectedButton = Add-ControlButton 'Run selected' {
    $action = Get-SelectedAction
    if ($null -ne $action) { Run-Action $action }
}

[void](Add-ControlButton 'Run dry run' {
    $action = Get-SelectedAction
    if ($null -ne $action) { Run-Action $action -DryRun }
})

[void](Add-ControlButton 'Cancel' {
    if ($script:currentProcess -and -not $script:currentProcess.HasExited) { $script:currentProcess.Kill() }
})

[void](Add-ControlButton 'Open log' {
    if ($script:lastLog -and (Test-Path -LiteralPath $script:lastLog)) { Start-Process notepad.exe $script:lastLog }
})

[void](Add-ControlButton 'Open output folder' {
    Start-Process explorer.exe (Join-Path $repo 'artifacts')
})

[void](Add-ControlButton 'Copy command' {
    [System.Windows.Forms.Clipboard]::SetText($script:lastCommand)
})

[void](Add-ControlButton 'Copy failure details' {
    [System.Windows.Forms.Clipboard]::SetText($script:lastStdErr)
})

[void](Add-ControlButton 'Generate Codex repair prompt' {
    if ($script:lastCommand) {
        $bundle = New-FailureBundle -Command $script:lastCommand -ExitCode 1 -StdOut '' -StdErr $script:lastStdErr
        Start-Process explorer.exe $bundle
    }
})

[void](Add-ControlButton 'Refresh script list' {
    Populate-List $actionsList
})

$commandBox = New-Object System.Windows.Forms.TextBox
$commandBox.Dock = 'Fill'
$commandBox.Multiline = $true
$commandBox.ReadOnly = $true
$rightPanel.Controls.Add($commandBox, 0, 1)

$outputBox = New-Object System.Windows.Forms.TextBox
$outputBox.Dock = 'Fill'
$outputBox.Multiline = $true
$outputBox.ScrollBars = 'Both'
$outputBox.ReadOnly = $true
$rightPanel.Controls.Add($outputBox, 0, 2)

$errorBox = New-Object System.Windows.Forms.TextBox
$errorBox.Dock = 'Fill'
$errorBox.Multiline = $true
$errorBox.ScrollBars = 'Both'
$errorBox.ReadOnly = $true
$errorBox.ForeColor = [System.Drawing.Color]::DarkRed
$rightPanel.Controls.Add($errorBox, 0, 3)

$statusLabel = New-Object System.Windows.Forms.Label
$statusLabel.Dock = 'Fill'
$statusLabel.Text = 'Ready.'
$rightPanel.Controls.Add($statusLabel, 0, 4)

Populate-List $actionsList
if ($SelfTest -or $SelfTestClick) {
    $action = [pscustomobject]@{
        id = 'self-test'
        name = 'Self Test'
        script = 'scripts/control-center-selftest-child.ps1'
        arguments = @()
        supportsDryRun = $false
    }
    if ($SelfTestClick) {
        $form.StartPosition = 'Manual'
        $form.ShowInTaskbar = $false
        $form.Opacity = 0
        $form.Location = New-Object System.Drawing.Point(-32000, -32000)
        $form.Show()
        [System.Windows.Forms.Application]::DoEvents()
        $actionsList.Items.Clear()
        $item = New-Object System.Windows.Forms.ListViewItem('Self Test')
        [void]$item.SubItems.Add($action.name)
        [void]$item.SubItems.Add('Exercises the Run selected button handler.')
        [void]$item.SubItems.Add($action.id)
        $item.Tag = $action
        [void]$actionsList.Items.Add($item)
        $form.CreateControl()
        $actionsList.CreateControl()
        $actionsList.Items[0].Selected = $true
        $actionsList.Items[0].Focused = $true
        $actionsList.Select()
        $runSelectedButton.PerformClick()
        [System.Windows.Forms.Application]::DoEvents()
    } else {
        Run-Action $action -DryRun
    }

    if ($null -eq $script:currentProcess) { throw 'Self-test did not start a command.' }
    while ($script:currentProcess -and -not $script:currentProcess.HasExited) {
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 100
    }
    for ($i = 0; $i -lt 20 -and $script:lastStdOut -notmatch 'control-center-child-ok'; $i++) {
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 100
    }
    if ($script:currentProcess.ExitCode -ne 0) { throw "Self-test command failed with exit code $($script:currentProcess.ExitCode)." }
    if (-not [string]::IsNullOrWhiteSpace($script:lastStdErr)) { throw "Self-test logged an internal error: $script:lastStdErr" }
    if ($script:lastStdOut -notmatch 'control-center-child-ok') { throw 'Self-test did not capture child stdout.' }
    if ($SelfTestClick) { $form.Close() }
    Write-Host 'control-center-self-test-ok'
    [Environment]::Exit(0)
}
[void]$form.ShowDialog()
