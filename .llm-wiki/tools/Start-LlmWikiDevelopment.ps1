[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Objective,
    [string]$BaseRef = 'HEAD',
    [string[]]$ProposedPath,
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
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
$workflowArguments = @{ Objective = $Objective; BaseRef = $BaseRef; Format = 'Json'; Limit = $Limit }
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $workflowArguments.ProposedPath = $ProposedPath }
$workflow = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') @workflowArguments | ConvertFrom-Json
$researchArguments = @{ Objective = $Objective; BaseRef = $BaseRef; Format = 'Json'; Limit = $Limit }
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $researchArguments.ProposedPath = $ProposedPath }
$research = & (Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1') @researchArguments | ConvertFrom-Json
$paths = @(@($ProposedPath) + @($workflow.inferred.paths) | Where-Object { $_ } | Sort-Object -Unique)
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
if ('Api' -in $scopes -or 'Contracts' -in $scopes) { $criteria.Add('HTTP routes, payloads, status codes, compatibility, and the OpenAPI snapshot match the implemented behavior.') }
if ('Database' -in $scopes) { $criteria.Add('Persistence mappings and schema changes are verified; every migration includes its Designer and model snapshot updates when applicable.') }
if (@($paths | Where-Object { $_ -match '(?i)Notification' }).Count -gt 0) { $criteria.Add('Notification delivery is correctly targeted, idempotent, retry-safe, and covered by focused tests.') }
if (@($paths | Where-Object { $_ -match '(?i)JobManager|HostedService|Recurring' }).Count -gt 0) { $criteria.Add('The background job is registered, configured, cancellable, retry-safe, and its direct constructor/configuration consumers compile.') }
if ('Frontend' -in $scopes) { $criteria.Add('Frontend loading, success, empty, validation, and error states behave correctly through the actual runtime owner.') }
if ('Localization' -in $scopes) { $criteria.Add('English and Russian localization keys remain synchronized and Russian text renders without corruption.') }
if (@($scopes | Where-Object { $_ -in @('Backend', 'Api', 'Database', 'Frontend') }).Count -gt 1) { $criteria.Add('Cross-module and project dependencies remain allowed, acyclic, and covered by architecture checks.') }
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
