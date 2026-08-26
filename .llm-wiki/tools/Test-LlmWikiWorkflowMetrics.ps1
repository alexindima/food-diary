[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'workflow-metrics'
$metricsRoot = Join-Path $fixtureRoot 'workflow-metrics'
$tasksRoot = Join-Path $fixtureRoot 'tasks'
$relativeTasksPath = [IO.Path]::GetRelativePath($repositoryRoot, $tasksRoot).Replace('\', '/')
$contextRegistryPath = Join-Path $fixtureRoot 'context-outcomes.json'
$modelRegistryPath = Join-Path $fixtureRoot 'model-outcomes.json'
$previousMetricsRoot = $env:LLM_WIKI_WORKFLOW_METRICS_ROOT
$previousContextRegistry = $env:LLM_WIKI_CONTEXT_OUTCOME_REGISTRY_PATH
$previousModelRegistry = $env:LLM_WIKI_MODEL_ROUTE_OUTCOME_REGISTRY_PATH

try {
    $null = New-Item -ItemType Directory -Path $metricsRoot -Force
    $null = New-Item -ItemType Directory -Path $tasksRoot -Force
    $env:LLM_WIKI_WORKFLOW_METRICS_ROOT = $metricsRoot
    $env:LLM_WIKI_CONTEXT_OUTCOME_REGISTRY_PATH = $contextRegistryPath
    $env:LLM_WIKI_MODEL_ROUTE_OUTCOME_REGISTRY_PATH = $modelRegistryPath
    $emptyRegistry = [pscustomobject][ordered]@{ schemaVersion = 1; events = @() }
    [IO.File]::WriteAllText($contextRegistryPath, (($emptyRegistry | ConvertTo-Json -Depth 4) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($modelRegistryPath, (($emptyRegistry | ConvertTo-Json -Depth 4) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))

    foreach ($sample in @(
        [pscustomobject]@{ operation = 'research'; outcome = 'passed'; duration = 1.0; phase = $null; profile = $null; runId = $null }
        [pscustomobject]@{ operation = 'research'; outcome = 'failed'; duration = 2.0; phase = $null; profile = $null; runId = $null }
        [pscustomobject]@{ operation = 'verify'; outcome = 'timed-out'; duration = 3.0; phase = $null; profile = $null; runId = $null }
        [pscustomobject]@{ operation = 'verify-full'; outcome = 'interrupted'; duration = 4.0; phase = $null; profile = $null; runId = $null }
        [pscustomobject]@{ operation = 'verify-full-group'; outcome = 'passed'; duration = 5.0; phase = 'indexes'; profile = 'Focused'; runId = 'run-1' }
    )) {
        & (Join-Path $PSScriptRoot 'Write-LlmWikiWorkflowMetric.ps1') `
            -Operation $sample.operation `
            -Outcome $sample.outcome `
            -DurationSeconds $sample.duration `
            -Phase $sample.phase `
            -Profile $sample.profile `
            -RunId $sample.runId
    }
    $metrics = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkflowMetrics.ps1') -TasksPath $relativeTasksPath -Format Json | ConvertFrom-Json
    $research = @($metrics.adaptive.byOperation | Where-Object operation -eq 'research')[0]
    $verify = @($metrics.adaptive.byOperation | Where-Object operation -eq 'verify')[0]
    $fullGroup = @($metrics.adaptive.recent | Where-Object operation -eq 'verify-full-group')[0]
    if ([int]$metrics.schemaVersion -ne 4 -or [int]$metrics.adaptive.runCount -ne 5 -or
        [int]$metrics.adaptive.passedCount -ne 2 -or [int]$metrics.adaptive.failedCount -ne 3 -or
        [string]$metrics.adaptive.health -ne 'attention' -or [double]$metrics.adaptive.successRatePercent -ne 40 -or
        [string]$metrics.ceremony.adoptionStatus -ne 'insufficient-data' -or $null -ne $metrics.ceremony.manifestAdoptionPercent -or
        [int]$research.failedCount -ne 1 -or [double]$research.successRatePercent -ne 50 -or
        [double]$research.medianDurationSeconds -ne 1 -or [double]$research.p95DurationSeconds -ne 2 -or
        [int]$verify.timedOutCount -ne 1 -or [string]$fullGroup.phase -ne 'indexes' -or
        [string]$fullGroup.profile -ne 'Focused' -or [string]$fullGroup.runId -ne 'run-1') {
        throw 'Workflow metrics did not retain failed, timed-out, and interrupted outcomes honestly.'
    }

    $contextHealth = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextOutcome.ps1') health -Format Json | ConvertFrom-Json
    $modelHealth = & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRoutingOutcome.ps1') health -Format Json | ConvertFrom-Json
    if ([string]$contextHealth.health -ne 'insufficient-data' -or [int]$contextHealth.sampleCount -ne 0 -or
        [string]$modelHealth.health -ne 'insufficient-data' -or [int]$modelHealth.sampleCount -ne 0) {
        throw 'Outcome health did not distinguish an empty registry from a healthy evidence set.'
    }
    Write-Host 'LLM Wiki workflow metrics regression passed: failures are counted and empty outcome registries report insufficient-data.'
} finally {
    foreach ($entry in @(
        [pscustomobject]@{ name = 'LLM_WIKI_WORKFLOW_METRICS_ROOT'; value = $previousMetricsRoot }
        [pscustomobject]@{ name = 'LLM_WIKI_CONTEXT_OUTCOME_REGISTRY_PATH'; value = $previousContextRegistry }
        [pscustomobject]@{ name = 'LLM_WIKI_MODEL_ROUTE_OUTCOME_REGISTRY_PATH'; value = $previousModelRegistry }
    )) {
        if ([string]::IsNullOrWhiteSpace([string]$entry.value)) { Remove-Item -LiteralPath "Env:$($entry.name)" -ErrorAction SilentlyContinue }
        else { Set-Item -LiteralPath "Env:$($entry.name)" -Value ([string]$entry.value) }
    }
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}
