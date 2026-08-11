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
$isExtractionDesign = $Objective -match '(?i)extract|move.+(?:contract|module|abstraction)|module.+boundary|contract.+abstraction'
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
$verticalSliceProfiles = @('feature', 'critical', 'architectural')
$usesVerticalSlices = $verticalSliceProfiles -contains [string]$research.workflow.profile
$implementationPhase = @($plan.phases | Where-Object id -eq 'implementation' | Select-Object -First 1)
$contractPhases = @($plan.phases | Where-Object id -in @('contracts', 'domain-data'))
$verificationPhase = @($plan.phases | Where-Object id -eq 'focused-verification' | Select-Object -First 1)
$publicationPhases = @($plan.phases | Where-Object id -in @('generated-artifacts', 'release-readiness'))
$designSlices = if ($usesVerticalSlices) {
    @(
        [pscustomobject][ordered]@{
            id = 'slice-minimum-behavior'
            title = 'Deliver the smallest observable behavior'
            outcome = 'One acceptance-relevant behavior works end to end through its current runtime owner and closest reliable test.'
            files = @($implementationPhase.files + $verificationPhase.files | Where-Object { $_ } | Select-Object -Unique)
            actions = @('Choose one observable acceptance outcome.', 'Implement only the path required for that outcome.', 'Add and run its closest focused test before expanding scope.')
            evidence = @($verificationPhase.evidence)
            checkpoint = 'Confirm the behavior is observable, tested, and still inside the declared boundary.'
        }
        [pscustomobject][ordered]@{
            id = 'slice-compatibility-and-failure'
            title = 'Complete compatibility and failure behavior'
            outcome = 'Consumers, boundary cases, and failure behavior are compatible and independently verifiable.'
            files = @($contractPhases.files + $implementationPhase.files + $verificationPhase.files | Where-Object { $_ } | Select-Object -Unique)
            actions = @($contractPhases.actions + 'Exercise negative, boundary, and downstream-consumer scenarios.' | Where-Object { $_ } | Select-Object -Unique)
            evidence = @($contractPhases.evidence + $verificationPhase.evidence | Where-Object { $_ } | Select-Object -Unique)
            checkpoint = 'Stop if compatibility, migration, privacy, or error semantics require a different design decision.'
        }
        [pscustomobject][ordered]@{
            id = 'slice-release-proof'
            title = 'Produce publication proof'
            outcome = 'Generated artifacts, rollout checks, and final evidence prove the complete change is publishable.'
            files = @($publicationPhases.files | Where-Object { $_ } | Select-Object -Unique)
            actions = @($publicationPhases.actions | Where-Object { $_ } | Select-Object -Unique)
            evidence = @($publicationPhases.evidence | Where-Object { $_ } | Select-Object -Unique)
            checkpoint = 'Complete only after strict gates and acceptance evidence pass on the final diff.'
        }
    )
} else { @() }
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
    alternatives = if ($isExtractionDesign) { @(
        [pscustomobject][ordered]@{ id = 'move-contract-as-is'; description = 'Move the existing contract to the abstraction assembly without changing its shape.'; evaluation = 'Fastest, but unsafe when it returns a domain aggregate or exposes mutation to unrelated modules.' }
        [pscustomobject][ordered]@{ id = 'consumer-specific-projections'; description = 'Replace the broad contract with consumer-specific read projections and mutation capabilities.'; evaluation = 'Preferred when extraction should remove aggregate and implementation coupling; requires explicit consumer migration.' }
        [pscustomobject][ordered]@{ id = 'adapter-transition'; description = 'Introduce narrow abstraction contracts and retain an implementation adapter during staged migration.'; evaluation = 'Preferred for incremental extraction when all consumers cannot move atomically.' }
    ) } else { @(
        [pscustomobject][ordered]@{ id = 'existing-pattern'; description = 'Extend the closest current implementation pattern.'; evaluation = 'Preferred when current code and contracts prove it satisfies the target behavior.' }
        [pscustomobject][ordered]@{ id = 'bounded-change'; description = 'Introduce the smallest bounded change that preserves current consumers.'; evaluation = 'Preferred for bugs when no durable architecture decision is required.' }
        [pscustomobject][ordered]@{ id = 'structural-change'; description = 'Introduce a new abstraction or boundary.'; evaluation = 'Use only when the existing pattern cannot satisfy an explicit invariant; record the decision.' }
    ) }
    implementationPhases = @($plan.phases)
    sliceStrategy = [pscustomobject][ordered]@{
        enabled = $usesVerticalSlices
        kind = if ($usesVerticalSlices) { 'vertical-outcome' } else { 'none' }
        reason = if ($usesVerticalSlices) { 'Large feature, critical, and architectural profiles benefit from acceptance-oriented end-to-end slices.' } else { 'Tiny, maintenance, and bounded bug work avoids slice-planning ceremony.' }
    }
    designSlices = @($designSlices)
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
if ($result.sliceStrategy.enabled) {
    foreach ($slice in $result.designSlices) { Write-Host "Vertical slice $($slice.id): $($slice.outcome)" }
}
Write-Host "Next: $($result.nextAction)"
