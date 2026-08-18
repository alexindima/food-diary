[CmdletBinding()]
param(
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}

$stages = [Collections.Generic.List[object]]::new()
$failedStage = $null
function Invoke-FinalizationStage([string]$Id, [scriptblock]$Operation) {
    if ($null -ne $script:failedStage) { return $null }
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    try {
        $output = & $Operation
        $stopwatch.Stop()
        $valid = $null -ne $output -and [bool]$output.valid
        $stage = [pscustomobject][ordered]@{
            id = $Id
            status = $(if ($valid) { 'passed' } else { 'failed' })
            valid = $valid
            durationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 2)
            error = ''
            result = $output
        }
        $stages.Add($stage)
        if (-not $valid) { $script:failedStage = $Id }
        return $output
    } catch {
        $stopwatch.Stop()
        $stages.Add([pscustomobject][ordered]@{
            id = $Id
            status = 'failed'
            valid = $false
            durationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 2)
            error = $_.Exception.Message
            result = $null
        })
        $script:failedStage = $Id
        return $null
    }
}

$seal = Invoke-FinalizationStage 'seal' {
    $requirements = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRequirementModel.ps1') create `
        -WorkspacePath $workspace -AsOfUtc $AsOfUtc -Format Json | ConvertFrom-Json
    if (-not $requirements.valid) { return [pscustomobject]@{ valid = $false; requirements = $requirements; conformance = $null } }
    $conformance = & (Join-Path $PSScriptRoot 'Manage-LlmWikiPlanConformance.ps1') create `
        -WorkspacePath $workspace -AsOfUtc $AsOfUtc -Format Json | ConvertFrom-Json
    [pscustomobject][ordered]@{ valid = [bool]$requirements.valid -and [bool]$conformance.valid; requirements = $requirements; conformance = $conformance }
}
$proof = Invoke-FinalizationStage 'proof' {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiProofOfChange.ps1') create `
        -WorkspacePath $workspace -AsOfUtc $AsOfUtc -Format Json | ConvertFrom-Json
}
$security = Invoke-FinalizationStage 'context-security' {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextSecurity.ps1') create `
        -WorkspacePath $workspace -AsOfUtc $AsOfUtc -Format Json | ConvertFrom-Json
}
$validation = Invoke-FinalizationStage 'validate' {
    & (Join-Path $PSScriptRoot 'Invoke-LlmWikiDeliveryWorkflow.ps1') validate `
        -WorkspacePath $workspace -Format Json | ConvertFrom-Json
}
$critique = Invoke-FinalizationStage 'critique' {
    & (Join-Path $PSScriptRoot 'Invoke-LlmWikiDeliveryWorkflow.ps1') critique `
        -WorkspacePath $workspace -AssessmentInput $validation.assessment -Format Json | ConvertFrom-Json
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    action = 'finalize'
    workspace = $workspace
    valid = $null -eq $failedStage -and $stages.Count -eq 5
    failedStage = $failedStage
    completedStageCount = @($stages | Where-Object status -eq 'passed').Count
    stages = @($stages)
    nextAction = $(if ($null -eq $failedStage) { 'Delivery is finalized.' } else { "Resolve stage '$failedStage', then rerun: ./.llm-wiki/wiki.ps1 delivery-finalize -WorkspacePath $workspace -FailOnInvalid" })
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 50 } else {
    Write-Host "Delivery finalize: valid=$($result.valid), completed=$($result.completedStageCount)/5, failedStage=$(if ($failedStage) { $failedStage } else { 'none' })"
    foreach ($stage in $stages) {
        Write-Host " - [$($stage.status)] $($stage.id) ($($stage.durationSeconds)s)$(if ($stage.error) { ": $($stage.error)" })"
    }
    Write-Host $result.nextAction
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
