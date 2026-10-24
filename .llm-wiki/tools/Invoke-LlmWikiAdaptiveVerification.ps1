[CmdletBinding()]
param(
    [ValidateSet('All', 'Workflow', 'Evals')]
    [string]$Scope = 'All',
    [ValidateRange(1, 8)][int]$MaxConcurrency = 4,
    [ValidateRange(1, 3600)][int]$TimeoutSeconds = 1800
)

$ErrorActionPreference = 'Stop'
$toolsRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolsRoot '../..'))
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
$checks = @(
    if ($Scope -in @('All', 'Workflow')) {
        [pscustomobject]@{ name = 'adaptive routing'; script = Join-Path $toolsRoot 'Test-LlmWikiAdaptiveWorkflow.ps1'; arguments = '-Group Routing' }
        [pscustomobject]@{ name = 'adaptive experience'; script = Join-Path $toolsRoot 'Test-LlmWikiAdaptiveWorkflow.ps1'; arguments = '-Group Experience' }
    }
    if ($Scope -in @('All', 'Evals')) {
        [pscustomobject]@{ name = 'integration scan'; script = Join-Path $toolsRoot 'Test-LlmWikiIntegrationScan.ps1'; arguments = '' }
        [pscustomobject]@{ name = 'evals 1/3'; script = Join-Path $toolsRoot 'Invoke-LlmWikiEvals.ps1'; arguments = '-ShardIndex 0 -ShardCount 3' }
        [pscustomobject]@{ name = 'evals 2/3'; script = Join-Path $toolsRoot 'Invoke-LlmWikiEvals.ps1'; arguments = '-ShardIndex 1 -ShardCount 3' }
        [pscustomobject]@{ name = 'evals 3/3'; script = Join-Path $toolsRoot 'Invoke-LlmWikiEvals.ps1'; arguments = '-ShardIndex 2 -ShardCount 3' }
    }
)
Invoke-LlmWikiProcessBatch -Checks $checks -WorkingDirectory $repositoryRoot -MaxConcurrency $MaxConcurrency -TimeoutSeconds $TimeoutSeconds
Write-Host "LLM Wiki adaptive verification passed in parallel (scope=$Scope)."
