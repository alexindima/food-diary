[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [Alias('Intent')]
    [string]$Objective,
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [Alias('PlannedPath')]
    [string[]]$ProposedPath,
    [string[]]$Decision,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 30)]
    [int]$Limit = 12
)

$ErrorActionPreference = 'Stop'
$common = @{ Objective = $Objective; BaseRef = $BaseRef; Format = 'Json'; Limit = $Limit }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $common.ChangedPath = $ChangedPath }
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $common.ProposedPath = $ProposedPath }
$research = & (Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1') @common | ConvertFrom-Json

$planArguments = @{ BaseRef = $BaseRef; Objective = $Objective; Format = 'Json'; Limit = $Limit }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $planArguments.HeadRef = $HeadRef }
$planPaths = @($ProposedPath + $ChangedPath + $research.discovery.groundedPaths | Where-Object { $_ } | Sort-Object -Unique)
if ($planPaths.Count -gt 0) { $planArguments.ChangedPath = $planPaths }
$plan = & (Join-Path $PSScriptRoot 'Get-LlmWikiImplementationPlan.ps1') @planArguments | ConvertFrom-Json

$decisionEvidence = @($Decision | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$decisionQuestions = @($research.openQuestions | ForEach-Object {
    $resolvedByInput = $_.id -eq 'resolve-design-boundary' -and $decisionEvidence.Count -gt 0
    [pscustomobject][ordered]@{
        id = $_.id
        question = $_.question
        blocking = [bool]$_.blocking -and -not $resolvedByInput
        resolution = if ($resolvedByInput) { $decisionEvidence } else { @() }
        resolutionEvidence = $_.evidenceNeeded
    }
})
$invariantCandidates = @($plan.phases | ForEach-Object { $_.stopConditions }) +
    @($plan.finalGates | ForEach-Object { "Gate $($_.id): $($_.command)" })
$invariants = @($invariantCandidates | Where-Object { $_ } | Select-Object -Unique)
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    objective = $Objective
    profile = $research.workflow.profile
    currentStateEvidence = @($research.discovery.groundedPaths)
    targetBehavior = $Objective
    invariants = $invariants
    compatibility = [pscustomobject][ordered]@{
        scopes = @($plan.scopes)
        modules = @($plan.modules.direct + $plan.modules.downstream | Select-Object -Unique)
        finalGates = @($plan.finalGates)
    }
    decisionQuestions = $decisionQuestions
    alternatives = @(
        [pscustomobject][ordered]@{ id = 'existing-pattern'; description = 'Extend the closest current implementation pattern.'; evaluation = 'Preferred when current code and contracts prove it satisfies the target behavior.' }
        [pscustomobject][ordered]@{ id = 'bounded-change'; description = 'Introduce the smallest bounded change that preserves current consumers.'; evaluation = 'Preferred for bugs when no durable architecture decision is required.' }
        [pscustomobject][ordered]@{ id = 'structural-change'; description = 'Introduce a new abstraction or boundary.'; evaluation = 'Use only when the existing pattern cannot satisfy an explicit invariant; record the decision.' }
    )
    implementationPhases = @($plan.phases)
    ready = @($decisionQuestions | Where-Object blocking).Count -eq 0 -and @($research.discovery.groundedPaths).Count -gt 0
    nextAction = if (@($decisionQuestions | Where-Object blocking).Count -gt 0) {
        'Resolve blocking decision questions with current-source evidence and record the selected alternative before editing.'
    } else {
        'Confirm target behavior and acceptance criteria, then implement the generated phases in order.'
    }
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 14; exit 0 }
Write-Host "Design checkpoint: $($result.profile), ready=$($result.ready)"
Write-Host "Objective: $Objective"
foreach ($question in $result.decisionQuestions) { Write-Host "OPEN [$($question.id)]: $($question.question)" }
foreach ($phase in $result.implementationPhases) { Write-Host "Phase $($phase.order): $($phase.title) - $($phase.outcome)" }
Write-Host "Next: $($result.nextAction)"
