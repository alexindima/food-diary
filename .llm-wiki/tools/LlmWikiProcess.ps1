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
