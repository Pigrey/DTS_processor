param([Parameter(Mandatory=$true)][string]$ExePath)
$ErrorActionPreference = 'Stop'
$temporaryData = Join-Path ([IO.Path]::GetTempPath()) ('shixiang-startup-' + [Guid]::NewGuid())
$process = $null
try {
    $info = [Diagnostics.ProcessStartInfo]::new((Resolve-Path $ExePath).Path)
    $info.UseShellExecute = $false
    $info.Environment['TASK_TRACKER_DATA_DIR'] = $temporaryData
    $process = [Diagnostics.Process]::Start($info)
    if (-not $process.WaitForInputIdle(20000)) { throw 'Application did not become idle within 20 seconds.' }
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    do {
        $process.Refresh()
        if ($process.HasExited) { throw "Application exited during startup with code $($process.ExitCode)." }
        if ($process.MainWindowTitle -like '事项簿 ·*') { break }
        Start-Sleep -Milliseconds 200
    } while ([DateTime]::UtcNow -lt $deadline)
    if ($process.MainWindowTitle -notlike '事项簿 ·*') {
        throw "Expected the main workspace, received '$($process.MainWindowTitle)' (possibly a startup error dialog)."
    }
    if (-not $process.CloseMainWindow()) { throw 'Could not close main window normally.' }
    if (-not $process.WaitForExit(15000)) { throw 'Application did not exit after closing its window.' }
    if ($process.ExitCode -ne 0) { throw "Application exited with code $($process.ExitCode)." }
    Write-Host 'PASS: native WPF workspace opens and closes normally with isolated local data.'
} finally {
    if ($null -ne $process -and -not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if (Test-Path $temporaryData) { Remove-Item -Recurse -Force $temporaryData }
}
