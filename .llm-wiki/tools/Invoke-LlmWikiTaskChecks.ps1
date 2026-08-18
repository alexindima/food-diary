[CmdletBinding()]
param(
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string[]]$CheckId,
    [switch]$IncludePassed,
    [switch]$ContinueOnFailure,
    [switch]$DryRun,
    [switch]$FailOnFailure,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path

if ([System.IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+(?:/.*)?$') {
    throw 'WorkspacePath must be inside .artifacts/llm-wiki/tasks/<task-name>.'
}
$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
$evidenceAbsolutePath = Join-Path $absoluteWorkspacePath 'evidence.json'
if (-not (Test-Path -LiteralPath $evidenceAbsolutePath -PathType Leaf)) {
    throw "Task workspace evidence does not exist: $normalizedWorkspacePath/evidence.json"
}

$evidence = Get-Content -LiteralPath $evidenceAbsolutePath -Raw | ConvertFrom-Json
$policy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') `
    -BaseRef ([string]$evidence.git.base) `
    -ChangedPath @($evidence.change.changedPaths) `
    -Format Json | ConvertFrom-Json
$canonicalChecks = @{}
foreach ($canonicalCheck in @($policy.requiredChecks)) {
    $canonicalChecks[[string]$canonicalCheck.id] = [string]$canonicalCheck.command
}

$selectedChecks = @($evidence.checks | Where-Object {
    ($CheckId.Count -eq 0 -or $_.id -in $CheckId) -and
    ($IncludePassed -or $_.status -notin @('passed', 'passed-with-known-baseline-failures', 'not-applicable'))
})
if ($CheckId.Count -gt 0) {
    $unknownIds = @($CheckId | Where-Object { $_ -notin @($evidence.checks.id) })
    if ($unknownIds.Count -gt 0) { throw "Unknown evidence check(s): $($unknownIds -join ', ')" }
}

$plans = [System.Collections.Generic.List[object]]::new()
foreach ($check in $selectedChecks) {
    if (-not $canonicalChecks.ContainsKey([string]$check.id)) {
        throw "Check '$($check.id)' is not required by the current change policy."
    }
    if ([string]$check.command -cne $canonicalChecks[[string]$check.id]) {
        throw "Refusing tampered command for check '$($check.id)'. Reinitialize evidence from the current policy."
    }
    $allowedCommand = [string]$check.command -match '^dotnet (test|format|list) [A-Za-z0-9_./\\-]+(?: [A-Za-z0-9_./\\:-]+)*$' -or
        [string]$check.command -match '^cd FoodDiary\.Web\.Client && npm (run [A-Za-z0-9_:-]+|audit)$' -or
        [string]$check.command -match '^\./\.llm-wiki/wiki\.ps1 [A-Za-z0-9_./\\:-]+(?: [A-Za-z0-9_./\\:-]+)*$'
    if (-not $allowedCommand) {
        throw "Refusing command outside the task execution allowlist: $($check.command)"
    }
    $safeId = ([string]$check.id) -replace '[^A-Za-z0-9_.-]', '_'
    $plans.Add([pscustomobject][ordered]@{
        id = [string]$check.id
        command = [string]$check.command
        statusBefore = [string]$check.status
        logPath = "$normalizedWorkspacePath/logs/$safeId.log"
    })
}

$runs = [System.Collections.Generic.List[object]]::new()
$failureCount = 0
if (-not $DryRun -and $plans.Count -gt 0) {
    $logsAbsolutePath = Join-Path $absoluteWorkspacePath 'logs'
    if (-not (Test-Path -LiteralPath $logsAbsolutePath)) {
        New-Item -ItemType Directory -Path $logsAbsolutePath | Out-Null
    }
    foreach ($plan in $plans) {
        $logAbsolutePath = Join-Path $repositoryRoot $plan.logPath
        $startedAt = [DateTime]::UtcNow
        $global:LASTEXITCODE = 0
        $runOutput = @(& (Join-Path $PSScriptRoot 'Manage-LlmWikiEvidence.ps1') run `
            -Path "$normalizedWorkspacePath/evidence.json" `
            -Id $plan.id `
            -NoExitOnFailure 2>&1)
        $updatedEvidence = Get-Content -LiteralPath $evidenceAbsolutePath -Raw | ConvertFrom-Json
        $recordedResult = & (Join-Path $PSScriptRoot 'Resolve-LlmWikiRecordedCheckResult.ps1') `
            -Evidence $updatedEvidence `
            -Id $plan.id
        $exitCode = [int]$recordedResult.exitCode
        $finishedAt = [DateTime]::UtcNow
        $durationSeconds = [Math]::Round(($finishedAt - $startedAt).TotalSeconds, 2)
        [System.IO.File]::WriteAllLines(
            $logAbsolutePath,
            @($runOutput | ForEach-Object { [string]$_ }),
            [System.Text.UTF8Encoding]::new($false))
        $status = [string]$recordedResult.status
        if ($exitCode -ne 0) { $failureCount++ }

        $updatedEntry = $recordedResult.entry
        if ($null -eq $updatedEntry.PSObject.Properties['logPath']) {
            $updatedEntry | Add-Member -NotePropertyName logPath -NotePropertyValue $plan.logPath
        } else {
            $updatedEntry.logPath = $plan.logPath
        }
        if ($null -eq $updatedEntry.PSObject.Properties['lastRunAtUtc']) {
            $updatedEntry | Add-Member -NotePropertyName lastRunAtUtc -NotePropertyValue $startedAt.ToString('o')
        } else {
            $updatedEntry.lastRunAtUtc = $startedAt.ToString('o')
        }
        $updatedEntry.lineage | Add-Member -NotePropertyName artifact -NotePropertyValue ([pscustomobject][ordered]@{
            path = $plan.logPath
            sha256 = (Get-FileHash -LiteralPath $logAbsolutePath -Algorithm SHA256).Hash.ToLowerInvariant()
        }) -Force
        [System.IO.File]::WriteAllText(
            $evidenceAbsolutePath,
            (($updatedEvidence | ConvertTo-Json -Depth 15) + [Environment]::NewLine),
            [System.Text.UTF8Encoding]::new($false))
        # The Wiki's own verification reads the tracked telemetry registry. Recording
        # that check here would mutate its input after every successful run and make
        # the evidence invalidate itself during the workspace refresh below.
        if ($plan.id -ne 'wiki-verify') {
            & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') record `
                -WorkspacePath $normalizedWorkspacePath `
                -CheckId $plan.id `
                -Status $status `
                -DurationSeconds $durationSeconds `
                -Command $plan.command `
                -AsOfUtc $finishedAt | Out-Null
        }

        $runs.Add([pscustomobject][ordered]@{
            id = $plan.id
            status = $status
            exitCode = $exitCode
            logPath = $plan.logPath
            durationSeconds = $durationSeconds
        })
        if ($exitCode -ne 0 -and -not $ContinueOnFailure) { break }
    }
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') refresh `
        -WorkspacePath $normalizedWorkspacePath | Out-Null
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    workspace = $normalizedWorkspacePath
    dryRun = [bool]$DryRun
    plannedCount = $plans.Count
    executedCount = $runs.Count
    failureCount = $failureCount
    plans = @($plans)
    runs = @($runs)
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 8
} else {
    Write-Host "Task checks: $($result.plannedCount) planned, $($result.executedCount) executed, $($result.failureCount) failed."
    foreach ($plan in $plans) { Write-Host " - [$($plan.statusBefore)] $($plan.id): $($plan.command)" }
    foreach ($run in $runs) { Write-Host " - [$($run.status)] $($run.id) -> $($run.logPath)" }
}
if ($FailOnFailure -and $failureCount -gt 0) { exit 1 }
