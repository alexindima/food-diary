[CmdletBinding()]
param(
    [string]$TasksPath = '.artifacts/llm-wiki/tasks',
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$root = Join-Path $repositoryRoot $TasksPath
$items = [Collections.Generic.List[object]]::new()
if (Test-Path -LiteralPath $root -PathType Container) {
    foreach ($directory in @(Get-ChildItem -LiteralPath $root -Directory)) {
        if ($directory.Name.StartsWith('.')) { continue }
        $workspacePath = Join-Path $directory.FullName 'workspace.json'
        if (-not (Test-Path -LiteralPath $workspacePath -PathType Leaf)) { continue }
        $workspace = Get-Content -LiteralPath $workspacePath -Raw | ConvertFrom-Json
        $evidencePath = Join-Path $directory.FullName 'evidence.json'
        $manifestPath = Join-Path $directory.FullName 'change-manifest.json'
        $acceptancePath = Join-Path $directory.FullName 'acceptance-matrix.json'
        $completionPath = Join-Path $directory.FullName 'completion.json'
        $evidence = if (Test-Path -LiteralPath $evidencePath) { Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json } else { $null }
        $items.Add([pscustomobject][ordered]@{
            id = $directory.Name
            objective = [string]$workspace.objective
            state = $(if (Test-Path -LiteralPath $completionPath -PathType Leaf) { 'sealed' } elseif (-not [string]::IsNullOrWhiteSpace([string]$workspace.state)) { [string]$workspace.state } else { 'in-progress' })
            hasManifest = Test-Path -LiteralPath $manifestPath
            hasAcceptance = Test-Path -LiteralPath $acceptancePath
            resolvedChecks = @($evidence.checks | Where-Object status -in @('passed', 'passed-with-known-baseline-failures', 'not-applicable')).Count
            failedChecks = @($evidence.checks | Where-Object status -eq 'failed').Count
            resolvedReviews = @($evidence.reviews | Where-Object status -in @('completed', 'not-applicable')).Count
        })
    }
}
$failedCheckMeasure = $items | Measure-Object failedChecks -Sum
$failedCheckSum = if ($null -ne $failedCheckMeasure -and $failedCheckMeasure.PSObject.Properties['Sum'] -and $null -ne $failedCheckMeasure.Sum) { [int]$failedCheckMeasure.Sum } else { 0 }
$gitDirectoryResult = Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', '--absolute-git-dir') -AllowedExitCode @(0..128)
$gitDirectory = if (@($gitDirectoryResult.Lines).Count -gt 0) { $gitDirectoryResult.Lines[0].Trim() } else { '' }
$adaptiveItems = @()
if ($gitDirectoryResult.ExitCode -eq 0 -or -not [string]::IsNullOrWhiteSpace([string]$env:LLM_WIKI_WORKFLOW_METRICS_ROOT)) {
    $adaptiveRoot = if (-not [string]::IsNullOrWhiteSpace([string]$env:LLM_WIKI_WORKFLOW_METRICS_ROOT)) {
        [IO.Path]::GetFullPath([string]$env:LLM_WIKI_WORKFLOW_METRICS_ROOT)
    } else {
        Join-Path $gitDirectory 'llm-wiki/workflow-metrics'
    }
    if (Test-Path -LiteralPath $adaptiveRoot -PathType Container) {
        $adaptiveItems = @(Get-ChildItem -LiteralPath $adaptiveRoot -Filter '*.json' -File | ForEach-Object {
            try {
                $metric = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
                if ([int]$metric.schemaVersion -in @(1, 2)) { $metric }
            } catch { }
        } | Sort-Object recordedAtUtc -Descending)
    }
}
$adaptiveDuration = $adaptiveItems | Measure-Object durationSeconds -Sum
$adaptiveDurationSum = if ($null -ne $adaptiveDuration -and $adaptiveDuration.PSObject.Properties['Sum'] -and $null -ne $adaptiveDuration.Sum) {
    [Math]::Round([double]$adaptiveDuration.Sum, 2)
} else { 0 }
$passedAdaptiveCount = @($adaptiveItems | Where-Object outcome -eq 'passed').Count
$failedAdaptiveCount = @($adaptiveItems | Where-Object outcome -in @('failed', 'timed-out', 'interrupted')).Count
$result = [pscustomobject][ordered]@{
    schemaVersion = 3
    workspaceCount = $items.Count
    readyOrSealedCount = @($items | Where-Object state -in @('ready', 'sealed', 'complete')).Count
    failedCheckCount = $failedCheckSum
    ceremony = [pscustomobject][ordered]@{
        manifestAdoptionPercent = $(if ($items.Count -eq 0) { 0 } else { [Math]::Round(100 * @($items | Where-Object hasManifest).Count / $items.Count, 1) })
        acceptanceAdoptionPercent = $(if ($items.Count -eq 0) { 0 } else { [Math]::Round(100 * @($items | Where-Object hasAcceptance).Count / $items.Count, 1) })
    }
    adaptive = [pscustomobject][ordered]@{
        runCount = $adaptiveItems.Count
        passedCount = $passedAdaptiveCount
        failedCount = $failedAdaptiveCount
        successRatePercent = $(if ($adaptiveItems.Count -eq 0) { $null } else { [Math]::Round(100.0 * $passedAdaptiveCount / $adaptiveItems.Count, 2) })
        health = $(if ($adaptiveItems.Count -eq 0) { 'insufficient-data' } elseif ($failedAdaptiveCount -gt 0) { 'attention' } else { 'healthy' })
        totalDurationSeconds = $adaptiveDurationSum
        byOperation = @($adaptiveItems | Group-Object operation | ForEach-Object {
            $passedCount = @($_.Group | Where-Object outcome -eq 'passed').Count
            $failedCount = @($_.Group | Where-Object outcome -in @('failed', 'timed-out', 'interrupted')).Count
            [pscustomobject]@{
                operation = $_.Name
                runCount = $_.Count
                passedCount = $passedCount
                failedCount = $failedCount
                timedOutCount = @($_.Group | Where-Object outcome -eq 'timed-out').Count
                interruptedCount = @($_.Group | Where-Object outcome -eq 'interrupted').Count
                successRatePercent = [Math]::Round(100.0 * $passedCount / $_.Count, 2)
            }
        })
        recent = @($adaptiveItems | Select-Object -First 20)
    }
    workspaces = @($items)
    note = 'These are local workflow adoption and outcome signals, not a quality score. Use retrospectives and CI evidence for causal conclusions.'
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 8; exit 0 }
Write-Host "Wiki workflow metrics: $($result.workspaceCount) workspace(s), $($result.readyOrSealedCount) ready/sealed, $($result.failedCheckCount) failed check(s)"
Write-Host "Ceremony adoption: manifest=$($result.ceremony.manifestAdoptionPercent)%, acceptance=$($result.ceremony.acceptanceAdoptionPercent)%"
Write-Host "Adaptive runs: $($result.adaptive.runCount) total, $($result.adaptive.passedCount) passed, $($result.adaptive.failedCount) failed"
Write-Host $result.note
