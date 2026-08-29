[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Objective,
    [string]$BaseRef = 'HEAD',
    [string[]]$ProposedPath,
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [ValidateSet('Sqlite', 'Json')][string]$CompiledIndexSource = 'Sqlite',
    [ValidateSet('Text', 'Json')][string]$Format = 'Text',
    [ValidateRange(1, 30)][int]$Limit = 12
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiExtractionPlanning.ps1')
$ProposedPath = @(
    @($ProposedPath) |
        Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
        ForEach-Object { [string]$_ -split '[;,]' } |
        ForEach-Object { ([string]$_).Trim().Replace('\', '/').TrimEnd('/') } |
        Where-Object { $_ } |
        Sort-Object -Unique
)
$absoluteWorkspace = Join-Path $repositoryRoot $WorkspacePath
if (Test-Path -LiteralPath $absoluteWorkspace -PathType Container) {
    $descriptorPath = Join-Path $absoluteWorkspace 'workspace.json'
    $existingObjective = if (Test-Path -LiteralPath $descriptorPath -PathType Leaf) {
        [string](Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json).objective
    } else { '' }
    if ([string]::IsNullOrWhiteSpace($existingObjective) -or $existingObjective.Trim() -cne $Objective.Trim()) {
        throw "Existing governed workspace '$WorkspacePath' belongs to a different objective ('$existingObjective'). Supply a new -WorkspacePath for '$Objective'; the workspace was not reused."
    }
}
$workflowArguments = @{
    Objective = $Objective
    BaseRef = $BaseRef
    CompiledIndexSource = $CompiledIndexSource
    Format = 'Json'
    Limit = $Limit
}
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $workflowArguments.ProposedPath = $ProposedPath }
$workflow = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') @workflowArguments | ConvertFrom-Json
$researchArguments = @{
    Objective = $Objective
    BaseRef = $BaseRef
    CompiledIndexSource = $CompiledIndexSource
    Format = 'Json'
    Limit = $Limit
}
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $researchArguments.ProposedPath = $ProposedPath }
$research = & (Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1') @researchArguments | ConvertFrom-Json
$scopeTokens = @($ProposedPath | ForEach-Object {
    [regex]::Matches(([string]$_).ToLowerInvariant(), '[a-z0-9]+') | ForEach-Object Value
} | Where-Object { $_.Length -ge 5 -and $_ -notin @('fooddiary', 'application', 'client', 'features', 'tests') } | Sort-Object -Unique)
$researchPaths = @($research.discovery.groundedPaths | Where-Object {
    $candidate = ([string]$_).ToLowerInvariant()
    $scopeTokens.Count -eq 0 -or @($scopeTokens | Where-Object { $candidate.Contains($_) }).Count -gt 0
} | Select-Object -First ($Limit * 3))
$paths = @(@($ProposedPath) + @($workflow.inferred.paths) + $researchPaths | Where-Object { $_ } | Sort-Object -Unique)
$extractionPlan = Get-LlmWikiExtractionPlan $Objective $repositoryRoot
$isModuleExtraction = $null -ne $extractionPlan
if ($isModuleExtraction) {
    $paths = @($paths + @($extractionPlan.paths) | Sort-Object -Unique)
}
$scopes = @($workflow.inferred.scopes)
$criteria = [Collections.Generic.List[string]]::new()
if ($isModuleExtraction) {
    foreach ($criterion in @($extractionPlan.criteria)) { $criteria.Add([string]$criterion) }
} else {
    $criteria.Add('The primary requested outcome is implemented and observable.')
}
foreach ($criterion in @(Get-LlmWikiSupplementalAcceptanceCriteria $Objective $scopes $paths)) {
    $criteria.Add([string]$criterion)
}
$workspaceCreated = $false
$workspaceMessage = 'not required by adaptive route'
if ([bool]$workflow.requiresWorkspace) {
    if (Test-Path -LiteralPath $absoluteWorkspace -PathType Container) {
        $workspaceMessage = "existing workspace reused: $WorkspacePath"
    }
    elseif ($paths.Count -eq 0) { $workspaceMessage = 'workspace required, but concrete paths are not grounded; complete research/reclassification first' }
    else {
        $allowedPaths = @($paths | ForEach-Object {
            $candidate = Join-Path $repositoryRoot ([string]$_)
            '^' + [regex]::Escape([string]$_) + $(if (Test-Path -LiteralPath $candidate -PathType Container) { '(?:/.*)?$' } else { '$' })
        })
        & (Join-Path $PSScriptRoot 'Initialize-LlmWikiTaskWorkspace.ps1') -Objective $Objective -Criterion @($criteria) -WorkspacePath $WorkspacePath -BaseRef $BaseRef -AllowedPath $allowedPaths -PlannedPath $paths | Out-Null
        $workspaceCreated = $true
        $workspaceMessage = "created: $WorkspacePath"
    }
}
$result = [pscustomobject][ordered]@{
    schemaVersion = 1; objective = $Objective; profile = $workflow.profile; confidence = $workflow.confidence
    requiresWorkspace = [bool]$workflow.requiresWorkspace; workspaceCreated = $workspaceCreated; workspace = $workspaceMessage
    groundedPaths = $paths; scopes = $scopes; acceptanceChecklist = @($criteria); workflow = $workflow; research = $research
    nextAction = @($workflow.stages | Where-Object required | Select-Object -First 1)[0]
    routingNote = 'For governed work, task-verification-plan creates and applies the model-routing recommendation to the executable verification plan.'
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; exit 0 }
Write-Host "LLM Wiki development start: profile=$($result.profile), confidence=$($result.confidence), workspace=$workspaceMessage"
Write-Host "Grounded paths: $($paths.Count); scopes: $($scopes -join ', ')"
Write-Host 'Acceptance checklist:'
for ($index = 0; $index -lt $criteria.Count; $index++) { Write-Host " $($index + 1). $($criteria[$index])" }
Write-Host "Next: $($result.nextAction.command)"
Write-Host $result.routingNote
