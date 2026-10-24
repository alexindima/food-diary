function Get-LlmWikiEnvironmentDiagnostics {
    param([Parameter(Mandatory)][string]$RepositoryRoot, [string]$TemporaryPath = ([IO.Path]::GetTempPath()))
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $issues = [Collections.Generic.List[object]]::new()
    $locations = @(
        @{ purpose = 'repository'; path = $RepositoryRoot }
        @{ purpose = 'temporary'; path = $TemporaryPath }
        @{ purpose = 'snapshot-cache'; path = (Join-Path $RepositoryRoot '.artifacts/llm-wiki/read-only-snapshots') }
    )
    $storage = @(foreach ($location in $locations) {
        $root = [IO.Path]::GetPathRoot([IO.Path]::GetFullPath($location.path))
        $free = $null
        try {
            $drive = [IO.DriveInfo]::new($root)
            if ($drive.IsReady) { $free = $drive.AvailableFreeSpace }
        } catch { }
        $status = if ($null -eq $free) { 'unknown' } elseif ($free -lt 256MB) { 'blocked' } elseif ($free -lt 1GB) { 'warning' } else { 'ok' }
        if ($status -in @('blocked', 'warning')) {
            $issues.Add([pscustomobject]@{ kind = 'disk-space'; severity = $status; path = $location.path; action = 'Free space on this volume or move task temporary outputs to a volume with space; inspect cache ownership before cleanup.' })
        }
        [pscustomobject][ordered]@{ purpose = $location.purpose; path = $location.path; volume = $root; freeBytes = $free; status = $status }
    })
    $runtimes = @(foreach ($name in @('git', 'node', 'dotnet')) {
        $probe = [Diagnostics.Stopwatch]::StartNew()
        $command = Get-Command $name -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        $version = $null
        $status = 'missing'
        if ($command) {
            try {
                $version = (& $command.Source --version 2>&1 | Out-String).Trim()
                $status = if ($LASTEXITCODE -eq 0) { 'ok' } else { 'failed' }
            } catch { $status = 'failed' }
        }
        if ($status -ne 'ok') { $issues.Add([pscustomobject]@{ kind = 'runtime'; severity = 'blocked'; path = $name; action = "Restore $name on PATH and verify '$name --version' in this shell." }) }
        [pscustomobject][ordered]@{ name = $name; status = $status; version = $version; durationMs = $probe.ElapsedMilliseconds }
    })
    $snapshotRoot = $locations[2].path
    $snapshotCount = if (Test-Path -LiteralPath $snapshotRoot -PathType Container) { @(Get-ChildItem -LiteralPath $snapshotRoot -Directory -ErrorAction SilentlyContinue).Count } else { 0 }
    [pscustomobject][ordered]@{
        powershellVersion = $PSVersionTable.PSVersion.ToString()
        storage = $storage
        runtimes = $runtimes
        frontendDependencies = [pscustomobject]@{ status = $(if (Test-Path -LiteralPath (Join-Path $RepositoryRoot 'FoodDiary.Web.Client/node_modules') -PathType Container) { 'present-not-validated' } else { 'absent' }); action = 'Install from the frontend lockfile when the selected route needs TypeScript discovery; directory presence alone does not prove dependency readiness.' }
        developmentMcp = [pscustomobject]@{ status = 'not-probed'; diagnosticCommand = 'node scripts/ai/development-mcp-diagnostics.mjs'; note = 'CLI health cannot inspect a chat tool catalog. A successful standalone probe does not refresh an existing chat connection.' }
        cache = [pscustomobject]@{ path = $snapshotRoot; snapshotCount = $snapshotCount; sizeBytes = $null; sizeStatus = 'not-measured'; action = 'Use Clear-LlmWikiReadOnlySnapshotCache.ps1 for owned inactive snapshots; health never deletes cache files.' }
        issues = @($issues)
        valid = @($issues | Where-Object severity -eq 'blocked').Count -eq 0
        durationMs = $timer.ElapsedMilliseconds
        performanceBaselineCommand = './.llm-wiki/wiki.ps1 workflow-metrics -Format Json'
    }
}
