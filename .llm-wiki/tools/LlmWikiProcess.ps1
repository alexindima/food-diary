function Invoke-LlmWikiProcessBatch {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object[]]$Checks,
        [Parameter(Mandatory)][string]$WorkingDirectory,
        [ValidateRange(1, 8)][int]$MaxConcurrency = 4,
        [ValidateRange(1, 3600)][int]$TimeoutSeconds = 1800,
        [ValidateRange(1024, 134217728)][long]$MaximumOutputBytes = 67108864
    )
    $shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
    $workers = [Collections.Generic.List[object]]::new()
    $failures = [Collections.Generic.List[string]]::new()
    $next = 0
    $logRoot = Join-Path $WorkingDirectory ('.artifacts/llm-wiki/process-batches/' + [guid]::NewGuid().ToString('N'))
    $null = [IO.Directory]::CreateDirectory($logRoot)
    try {
        while ($next -lt $Checks.Count -or $workers.Count -gt 0) {
            while ($next -lt $Checks.Count -and $workers.Count -lt $MaxConcurrency) {
                $check = $Checks[$next++]
                if ([string]::IsNullOrWhiteSpace([string]$check.script)) { throw "Wiki check script is required for '$($check.name)'." }
                $startInfo = [Diagnostics.ProcessStartInfo]::new()
                $startInfo.FileName = $shellPath
                $startInfo.WorkingDirectory = $WorkingDirectory
                $startInfo.UseShellExecute = $false
                $startInfo.CreateNoWindow = $true
                $startInfo.RedirectStandardOutput = $true
                $startInfo.RedirectStandardError = $true
                $startInfo.Arguments = "-NoLogo -NoProfile -File `"$($check.script)`" $($check.arguments)"
                $process = [Diagnostics.Process]::new()
                $process.StartInfo = $startInfo
                $stdoutPath = Join-Path $logRoot "$next.stdout.log"
                $stderrPath = Join-Path $logRoot "$next.stderr.log"
                $stdoutFile = $null
                $stderrFile = $null
                $started = $false
                try {
                    $stdoutFile = [IO.File]::Open($stdoutPath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
                    $stderrFile = [IO.File]::Open($stderrPath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
                    if (-not $process.Start()) { throw "Unable to start Wiki check '$($check.name)'." }
                    $started = $true
                    $workers.Add([pscustomobject]@{
                        check = $check; process = $process; stopwatch = [Diagnostics.Stopwatch]::StartNew()
                        stdoutPath = $stdoutPath; stderrPath = $stderrPath; stdoutFile = $stdoutFile; stderrFile = $stderrFile
                        stdout = $process.StandardOutput.BaseStream.CopyToAsync($stdoutFile)
                        stderr = $process.StandardError.BaseStream.CopyToAsync($stderrFile)
                    })
                } catch {
                    try { if ($started -and -not $process.HasExited) { Stop-LlmWikiProcessTree $process } }
                    finally {
                        if ($stdoutFile) { $stdoutFile.Dispose() }
                        if ($stderrFile) { $stderrFile.Dispose() }
                        $process.Dispose()
                    }
                    throw
                }
                Write-Host " - started $($check.name) (pid=$($process.Id)); logs=$stdoutPath, $stderrPath"
            }
            foreach ($worker in @($workers.ToArray())) {
                $drained = $worker.stdout.IsCompleted -and $worker.stderr.IsCompleted
                $timedOut = (-not $worker.process.HasExited -or -not $drained) -and $worker.stopwatch.Elapsed.TotalSeconds -ge $TimeoutSeconds
                $overflow = $worker.stdoutFile.Length + $worker.stderrFile.Length -gt $MaximumOutputBytes
                if ($timedOut -or $overflow) {
                    Stop-LlmWikiProcessTree $worker.process
                    $worker.process.StandardOutput.Dispose(); $worker.process.StandardError.Dispose()
                }
                if (-not $worker.process.HasExited -or (-not $timedOut -and -not $overflow -and -not $drained)) { continue }
                $worker.process.WaitForExit()
                $worker.stopwatch.Stop()
                if ($timedOut) { $failures.Add("$($worker.check.name) (timeout=${TimeoutSeconds}s)") }
                elseif ($overflow) { $failures.Add("$($worker.check.name) (output limit=$MaximumOutputBytes bytes)") }
                elseif ($worker.process.ExitCode -ne 0) { $failures.Add("$($worker.check.name) (exit=$($worker.process.ExitCode))") }
                if (-not $timedOut -and -not $overflow) {
                    $null = $worker.stdout.GetAwaiter().GetResult(); $null = $worker.stderr.GetAwaiter().GetResult()
                }
                $worker.stdoutFile.Dispose(); $worker.stderrFile.Dispose()
                foreach ($path in @($worker.stdoutPath, $worker.stderrPath)) {
                    $reader = [IO.StreamReader]::new($path, [Text.Encoding]::UTF8)
                    try {
                        $buffer = [char[]]::new(8192)
                        $count = $reader.ReadBlock($buffer, 0, $buffer.Length)
                        if ($count) { Write-Host ([string]::new($buffer, 0, $count)) }
                        if (-not $reader.EndOfStream) { Write-Host "Output preview truncated; full log: $path" }
                    } finally { $reader.Dispose() }
                }
                Write-Host " - $($worker.check.name): $([Math]::Round($worker.stopwatch.Elapsed.TotalSeconds, 2))s"
                $null = $workers.Remove($worker)
                $worker.process.Dispose()
            }
            if ($workers.Count -gt 0) { Start-Sleep -Milliseconds 100 }
        }
        if ($failures.Count -gt 0) { throw "Wiki process batch failed: $($failures -join ', ')" }
    } finally {
        foreach ($worker in $workers) {
            try { if (-not $worker.process.HasExited) { Stop-LlmWikiProcessTree $worker.process } }
            catch { Write-Warning "Unable to stop owned Wiki check '$($worker.check.name)': $_" }
            finally { $worker.stdoutFile.Dispose(); $worker.stderrFile.Dispose(); $worker.process.Dispose() }
        }
    }
}

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
    $exitCodeVariable = Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue
    $hadExitCode = $null -ne $exitCodeVariable
    $previousExitCode = if ($hadExitCode) { $exitCodeVariable.Value } else { $null }
    $runningOnWindows = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
    try {
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
    } finally {
        # Cleanup must not replace the result of the caller's native command,
        # including a taskkill race against an already-terminating owned process.
        if ($hadExitCode) {
            Set-Variable -Name LASTEXITCODE -Scope Global -Value $previousExitCode
        } else {
            Remove-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue
        }
    }
}
