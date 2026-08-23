. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')

function Get-LlmWikiIndexTimingPath([string]$RepositoryRoot) {
    $gitDirectory = (Invoke-LlmWikiGitCommand -RepositoryRoot $RepositoryRoot -Arguments @('rev-parse', '--absolute-git-dir') -FailureMessage 'Unable to resolve Git directory for index timings.').Lines[0]
    return Join-Path $gitDirectory 'llm-wiki/index-timings.json'
}

function Read-LlmWikiIndexTimings([string]$RepositoryRoot) {
    $path = Get-LlmWikiIndexTimingPath $RepositoryRoot
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return @() }
    try { return @((Get-Content -LiteralPath $path -Raw | ConvertFrom-Json).samples) } catch { return @() }
}

function Get-LlmWikiMedian([double[]]$Values) {
    $ordered = @($Values | Sort-Object)
    if ($ordered.Count -eq 0) { return 0.0 }
    $middle = [Math]::Floor($ordered.Count / 2)
    if ($ordered.Count % 2 -eq 1) { return [double]$ordered[$middle] }
    return ([double]$ordered[$middle - 1] + [double]$ordered[$middle]) / 2.0
}

function Get-LlmWikiIndexTimingStats([string]$RepositoryRoot, [string]$Mode) {
    return @(Read-LlmWikiIndexTimings $RepositoryRoot | Where-Object mode -eq $Mode | Group-Object tool | ForEach-Object {
        $recent = @($_.Group | Sort-Object sequence -Descending | Select-Object -First 5)
        [pscustomobject]@{
            tool = $_.Name
            sampleCount = $recent.Count
            medianSeconds = [Math]::Round((Get-LlmWikiMedian @($recent.durationSeconds)), 2)
        }
    })
}

function Add-LlmWikiIndexTimings([string]$RepositoryRoot, [string]$Mode, [object[]]$Timings) {
    if (@($Timings).Count -eq 0) { return }
    $samples = [Collections.Generic.List[object]]::new()
    [long]$sequence = 0
    foreach ($sample in @(Read-LlmWikiIndexTimings $RepositoryRoot)) {
        $sequence++
        if (-not $sample.PSObject.Properties['sequence']) {
            $sample | Add-Member -NotePropertyName sequence -NotePropertyValue $sequence
        } else {
            $sequence = [Math]::Max($sequence, [long]$sample.sequence)
        }
        $samples.Add($sample)
    }
    $lastRecordedAt = [DateTimeOffset]::MinValue
    foreach ($sample in $samples) {
        $parsed = [DateTimeOffset]::MinValue
        if ([DateTimeOffset]::TryParse([string]$sample.recordedAtUtc, [ref]$parsed) -and $parsed -gt $lastRecordedAt) {
            $lastRecordedAt = $parsed
        }
    }
    foreach ($timing in $Timings) {
        $recordedAt = [DateTimeOffset]::UtcNow
        if ($recordedAt -le $lastRecordedAt) { $recordedAt = $lastRecordedAt.AddTicks(1) }
        $lastRecordedAt = $recordedAt
        $sequence++
        $samples.Add([pscustomobject][ordered]@{
            tool = [string]$timing.tool
            mode = $Mode
            durationSeconds = [Math]::Round([double]$timing.durationSeconds, 2)
            recordedAtUtc = $recordedAt.ToString('o')
            sequence = $sequence
        })
    }
    $retained = @($samples | Group-Object { "$($_.mode)|$($_.tool)" } | ForEach-Object {
        @($_.Group | Sort-Object sequence -Descending | Select-Object -First 5)
    } | Sort-Object { [long]$_.sequence })
    $path = Get-LlmWikiIndexTimingPath $RepositoryRoot
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force
    $temporaryPath = "$path.$PID.tmp"
    [IO.File]::WriteAllText($temporaryPath, (([ordered]@{ schemaVersion = 1; samples = $retained } | ConvertTo-Json -Depth 6) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $temporaryPath -Destination $path -Force
}
