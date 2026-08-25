[CmdletBinding()]
param(
    [ValidateSet('status', 'next')]
    [string]$Action = 'status',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [Alias('Intent')]
    [string]$Objective,
    [string[]]$ProposedPath,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$absoluteWorkspace = Join-Path $repositoryRoot $WorkspacePath
$hasWorkspace = Test-Path -LiteralPath (Join-Path $absoluteWorkspace 'workspace.json') -PathType Leaf

if ($hasWorkspace) {
    $delivery = & (Join-Path $PSScriptRoot 'Invoke-LlmWikiDeliveryWorkflow.ps1') status -WorkspacePath $WorkspacePath -Format Json | ConvertFrom-Json
    $actions = @($delivery.assessment.nextActions | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    $result = [pscustomobject][ordered]@{
        schemaVersion = 1
        mode = 'governed-workspace'
        objective = [string]$delivery.assessment.objective
        state = $(if ($delivery.valid) { 'ready' } else { 'in-progress' })
        flow = @('Intent', 'Research', 'Decision', 'Plan', 'Implementation', 'Evidence', 'Ready')
        gates = @($delivery.assessment.gates)
        nextAction = $(if ($actions.Count -gt 0) { $actions[0] } else { 'No governance action remains.' })
        additionalActions = @($actions | Select-Object -Skip 1 -First 4)
        sourceOfTruth = $WorkspacePath
    }
} else {
    if ([string]::IsNullOrWhiteSpace($Objective)) {
        $result = [pscustomobject][ordered]@{
            schemaVersion = 1
            mode = 'no-active-workspace'
            objective = ''
            state = 'idle'
            flow = @()
            profile = $null
            ceremonyBudget = $null
            nextAction = 'Supply -Intent to route a new task.'
            additionalActions = @()
            sourceOfTruth = $WorkspacePath
        }
    } else {
        $arguments = @{ Objective = $Objective; Format = 'Json' }
        if ($ProposedPath.Count -gt 0) { $arguments.ProposedPath = $ProposedPath }
        $workflow = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') @arguments | ConvertFrom-Json
        $result = [pscustomobject][ordered]@{
            schemaVersion = 1
            mode = 'adaptive-route'
            objective = $Objective
            state = 'not-started'
            flow = @($workflow.stages.id)
            profile = [string]$workflow.profile
            ceremonyBudget = $workflow.ceremonyBudget
            nextAction = [string]$workflow.nextAction.command
            additionalActions = @($workflow.stages | Select-Object -Skip 1 -First 4 | ForEach-Object command)
            sourceOfTruth = 'adaptive workflow derived from current repository evidence'
        }
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; exit 0 }
Write-Host "Wiki ${Action}: $($result.state) [$($result.mode)]"
$resultProfile = if ($result.PSObject.Properties['profile']) { [string]$result.profile } else { '' }
if (-not [string]::IsNullOrWhiteSpace($resultProfile)) {
    $ceremonyLabel = if ($result.PSObject.Properties['ceremonyBudget'] -and $null -ne $result.ceremonyBudget -and $result.ceremonyBudget.PSObject.Properties['label']) {
        [string]$result.ceremonyBudget.label
    } else { 'governed' }
    Write-Host "Profile: $resultProfile; ceremony: $ceremonyLabel"
}
Write-Host "Flow: $($result.flow -join ' -> ')"
Write-Host "NEXT: $($result.nextAction)"
foreach ($item in @($result.additionalActions)) { Write-Host " Later: $item" }
Write-Host "Source of truth: $($result.sourceOfTruth)"
