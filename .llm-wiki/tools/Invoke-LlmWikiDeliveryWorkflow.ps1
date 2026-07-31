[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('status', 'replan', 'validate', 'critique')]
    [string]$Action = 'status',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$Reason,
    [switch]$DryRun,
    [object]$AssessmentInput,
    [object]$RefreshPreviewInput,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
if (-not (Test-Path -LiteralPath $absoluteWorkspace -PathType Container)) { throw "Task workspace does not exist: $workspace" }

function Invoke-JsonTool([string]$Name, [hashtable]$Arguments) {
    $Arguments.Format = 'Json'
    & (Join-Path $PSScriptRoot $Name) @Arguments | ConvertFrom-Json
}
function Get-DeliveryAssessment {
    $requirements = Invoke-JsonTool 'Manage-LlmWikiRequirementModel.ps1' @{ Action = 'assess'; WorkspacePath = $workspace }
    $conformance = Invoke-JsonTool 'Manage-LlmWikiPlanConformance.ps1' @{ Action = 'assess'; WorkspacePath = $workspace }
    $proof = Invoke-JsonTool 'Manage-LlmWikiProofOfChange.ps1' @{ Action = 'assess'; WorkspacePath = $workspace }
    $acceptance = Invoke-JsonTool 'Manage-LlmWikiAcceptanceMatrix.ps1' @{
        Action = 'validate'
        Path = "$workspace/acceptance-matrix.json"
        EvidencePath = "$workspace/evidence.json"
        RequireEvidence = $true
    }
    $packet = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-packet.json') -Raw | ConvertFrom-Json
    $journeys = Invoke-JsonTool 'Find-LlmWikiProductJourney.ps1' @{
        Query = [string]$packet.objective
        ChangedPath = @($packet.diff.changedPaths)
        Limit = 20
    }
    $criteria = @((Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'acceptance-matrix.json') -Raw | ConvertFrom-Json).criteria)
    $evidence = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'evidence.json') -Raw | ConvertFrom-Json
    $unresolvedChecks = @($evidence.checks | Where-Object status -notin @('passed', 'not-applicable'))
    $unresolvedReviews = @($evidence.reviews | Where-Object status -notin @('completed', 'not-applicable'))
    $coverage = foreach ($criterion in $criteria) {
        $mapping = $criterion.mapping
        $mappedEvidence = @($mapping.changedPaths) + @($mapping.scenarioIds) + @($mapping.checkIds) + @($mapping.reviewIds) + @($mapping.testPaths)
        [pscustomobject][ordered]@{
            id = [string]$criterion.id
            text = [string]$criterion.text
            status = [string]$criterion.status
            mapped = $mappedEvidence.Count -gt 0
            mappingCount = $mappedEvidence.Count
            evidenceNote = [string]$criterion.resolution.evidenceNote
            proven = [string]$criterion.status -in @('satisfied', 'not-applicable') -and (
                [string]$criterion.status -eq 'not-applicable' -or
                -not [string]::IsNullOrWhiteSpace([string]$criterion.resolution.evidenceNote) -or
                @($mapping.checkIds).Count + @($mapping.reviewIds).Count -gt 0)
        }
    }
    $coreGates = @(
        [pscustomobject][ordered]@{ id = 'requirements'; passed = [bool]$requirements.valid; summary = "$($requirements.model.classification.criteriaCount) criteria, $(@($requirements.model.findings).Count) blocking finding(s)" }
        [pscustomobject][ordered]@{ id = 'acceptance'; passed = [bool]$acceptance.valid; summary = "$($acceptance.satisfiedCount)/$($acceptance.criteriaCount) satisfied, $(@($acceptance.unmapped).Count) unmapped, $(@($acceptance.unverified).Count) unverified" }
        [pscustomobject][ordered]@{ id = 'plan-conformance'; passed = [bool]$conformance.valid; summary = "$($conformance.conformance.classification.changedPathCount) changed, $(@($conformance.conformance.classification.unplannedAllowedPaths).Count) unplanned, $(@($conformance.conformance.classification.outOfScopePaths).Count) out of scope" }
        [pscustomobject][ordered]@{ id = 'proof-of-change'; passed = (-not [bool]$proof.applicable -or [bool]$proof.valid); summary = $(if ([bool]$proof.applicable) { "$(@($proof.proof.findings).Count) proof finding(s)" } else { 'not yet applicable' }) }
    )
    $evidencePassed = $unresolvedChecks.Count + $unresolvedReviews.Count -eq 0
    $gates = @($coreGates) + @(
        [pscustomobject][ordered]@{ id = 'evidence'; passed = $evidencePassed; summary = "$($unresolvedChecks.Count) unresolved check(s), $($unresolvedReviews.Count) unresolved review(s)" }
    )
    $nextActions = [Collections.Generic.List[string]]::new()
    foreach ($gate in @($gates | Where-Object { -not $_.passed })) { $nextActions.Add("Resolve delivery gate '$($gate.id)': $($gate.summary)") }
    foreach ($criterion in @($coverage | Where-Object { -not $_.proven })) { $nextActions.Add("Map and prove acceptance criterion $($criterion.id).") }
    foreach ($check in $unresolvedChecks) { $nextActions.Add("Record current evidence for check '$($check.id)'.") }
    foreach ($review in $unresolvedReviews) { $nextActions.Add("Resolve review '$($review.id)'.") }
    [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $workspace
        objective = [string]$packet.objective
        valid = @($gates | Where-Object { -not $_.passed }).Count -eq 0
        gates = $gates
        requirementCoverage = @($coverage)
        journeyImpact = @($journeys.journeys)
        nextActions = @($nextActions)
        provenance = [pscustomobject][ordered]@{
            requirements = "$workspace/acceptance-matrix.json"
            evidence = "$workspace/evidence.json"
            plan = "$workspace/change-manifest.json"
            change = "$workspace/change-packet.json"
            journeys = '.llm-wiki/knowledge/product-journeys.json'
        }
    }
}

if ($Action -eq 'replan') {
    if ([string]::IsNullOrWhiteSpace($Reason)) { throw 'delivery-replan requires Reason.' }
    $before = if ($null -ne $AssessmentInput) { $AssessmentInput } else { Get-DeliveryAssessment }
    $preview = if ($null -ne $RefreshPreviewInput) { $RefreshPreviewInput } else { Invoke-JsonTool 'Manage-LlmWikiTaskWorkspace.ps1' @{ Action = 'refresh'; WorkspacePath = $workspace; DryRun = $true } }
    if (-not $DryRun) {
        Invoke-JsonTool 'Manage-LlmWikiTaskWorkspace.ps1' @{ Action = 'refresh'; WorkspacePath = $workspace } | Out-Null
        Invoke-JsonTool 'Manage-LlmWikiPlanConformance.ps1' @{ Action = 'replan'; WorkspacePath = $workspace; Reason = $Reason } | Out-Null
    }
    $after = if ($DryRun) { $before } else { Get-DeliveryAssessment }
    $result = [pscustomobject][ordered]@{
        schemaVersion = 1
        action = 'replan'
        workspace = $workspace
        applied = -not $DryRun
        reason = $Reason
        invalidationPreview = $preview.invalidation
        before = [pscustomobject][ordered]@{ valid = $before.valid; gates = $before.gates }
        after = [pscustomobject][ordered]@{ valid = $after.valid; gates = $after.gates }
        note = 'Replanning refreshes observed change evidence but does not widen the task contract allowed-path boundary.'
    }
} elseif ($Action -eq 'critique') {
    $assessment = if ($null -ne $AssessmentInput) { $AssessmentInput } else { Get-DeliveryAssessment }
    $critique = Invoke-JsonTool 'Manage-LlmWikiChangeCritique.ps1' @{ Action = 'assess'; WorkspacePath = $workspace }
    $approved = [string]$critique.critique.verdict -in @('approve', 'approve-with-notes')
    $result = [pscustomobject][ordered]@{
        schemaVersion = 1
        action = 'critique'
        workspace = $workspace
        valid = [bool]$critique.valid -and $approved
        verdict = [string]$critique.critique.verdict
        score = [double]$critique.critique.score
        findings = @($critique.critique.findings)
        reviewAreas = @($critique.critique.reviewAreas)
        deliveryGates = @($assessment.gates)
        note = 'Critique is an adverse completion review; integrity-valid output may still reject the change.'
    }
} else {
    $assessment = if ($null -ne $AssessmentInput) { $AssessmentInput } else { Get-DeliveryAssessment }
    $result = [pscustomobject][ordered]@{
        schemaVersion = 1
        action = $Action
        workspace = $workspace
        valid = [bool]$assessment.valid
        assessment = $assessment
    }
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 40
} elseif ($Action -eq 'replan') {
    Write-Host "Delivery replan: applied=$($result.applied), workspace=$workspace"
    Write-Host "Reason: $Reason"
    Write-Host $result.note
} elseif ($Action -eq 'critique') {
    Write-Host "Delivery critique: verdict=$($result.verdict), score=$($result.score)/100, valid=$($result.valid)"
    foreach ($finding in @($result.findings)) { Write-Host " - [$($finding.severity)] $($finding.area)/$($finding.id): $($finding.summary)" }
} else {
    Write-Host "Delivery ${Action}: valid=$($result.valid), workspace=$workspace"
    foreach ($gate in @($result.assessment.gates)) { Write-Host " - $(if ($gate.passed) { 'PASS' } else { 'BLOCK' }) $($gate.id): $($gate.summary)" }
    foreach ($criterion in @($result.assessment.requirementCoverage)) { Write-Host " - $($criterion.id) [$($criterion.status), mapped=$($criterion.mapped), proven=$($criterion.proven)]: $($criterion.text)" }
    foreach ($journey in @($result.assessment.journeyImpact)) { Write-Host " - JOURNEY $($journey.id) [$($journey.risk)]: $($journey.title)" }
    foreach ($next in @($result.assessment.nextActions | Select-Object -First 12)) { Write-Host " - NEXT: $next" }
}
if ($FailOnInvalid -and -not [bool]$result.valid) { exit 1 }
