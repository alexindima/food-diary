function Wait-LlmWikiProcessTermination {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory)]
        [Diagnostics.Process]$Process,
        [ValidateRange(100, 30000)]
        [int]$WaitMilliseconds = 5000
    )

    $timer = [Diagnostics.Stopwatch]::StartNew()
    $hasProcState = [Environment]::OSVersion.Platform -eq [PlatformID]::Unix -and [IO.Directory]::Exists('/proc')
    while ($true) {
        $Process.Refresh()
        if ($Process.HasExited) { return $true }
        if ($hasProcState) {
            # HasExited uses kill(pid, 0) for non-child Unix processes. A killed,
            # unreaped descendant still answers that probe even though it cannot run.
            try { $state = [IO.File]::ReadAllText("/proc/$($Process.Id)/stat") }
            catch [IO.FileNotFoundException] { return $true }
            catch [IO.DirectoryNotFoundException] { return $true }
            # The command field can contain spaces, parentheses and newlines.
            if ($state -notmatch '(?s)^\d+ \(.+\) ([A-Za-z]) ') {
                throw "Cannot determine termination state for Wiki PID $($Process.Id)."
            }
            if ($Matches[1] -in @('Z', 'X', 'x')) { return $true }
        }
        if ($timer.ElapsedMilliseconds -ge $WaitMilliseconds) { return $false }
        Start-Sleep -Milliseconds 25
    }
}

function Stop-LlmWikiProcessTree {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [Diagnostics.Process]$Process,
        [ValidateRange(100, 30000)]
        [int]$WaitMilliseconds = 5000
    )

    if ($Process.HasExited) { return }
    $runningOnWindows = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
    try {
        if ($runningOnWindows) {
            $descendants = [Collections.Generic.List[int]]::new()
            $pending = [Collections.Generic.Queue[int]]::new()
            $pending.Enqueue($Process.Id)
            while ($pending.Count -gt 0) {
                $parentId = $pending.Dequeue()
                foreach ($child in @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $parentId" -ErrorAction SilentlyContinue)) {
                    $childId = [int]$child.ProcessId
                    $descendants.Add($childId)
                    $pending.Enqueue($childId)
                }
            }
            foreach ($childId in @($descendants | Select-Object -Last $descendants.Count)) {
                Stop-Process -Id $childId -Force -ErrorAction SilentlyContinue
            }
            & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
        } else {
            $Process.Kill($true)
        }
    } catch {
        if ($runningOnWindows) {
            & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
        } else {
            $Process.Kill()
        }
    }
    if (-not $Process.WaitForExit($WaitMilliseconds)) {
        throw "Unable to stop Wiki process tree rooted at PID $($Process.Id)."
    }
}
