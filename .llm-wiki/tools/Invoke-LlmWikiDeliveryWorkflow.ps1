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
. (Join-Path $PSScriptRoot 'LlmWikiChangePacket.ps1')
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
function Get-WorkspaceSnapshot {
    $snapshot = @{}
    foreach ($file in @(Get-ChildItem -LiteralPath $absoluteWorkspace -File -Recurse -Force)) {
        $relative = $file.FullName.Substring($absoluteWorkspace.Length).TrimStart('\', '/').Replace('\', '/')
        $snapshot[$relative] = [IO.File]::ReadAllBytes($file.FullName)
    }
    $snapshot
}
function Restore-WorkspaceSnapshot([hashtable]$Snapshot) {
    foreach ($file in @(Get-ChildItem -LiteralPath $absoluteWorkspace -File -Recurse -Force)) {
        $relative = $file.FullName.Substring($absoluteWorkspace.Length).TrimStart('\', '/').Replace('\', '/')
        if (-not $Snapshot.ContainsKey($relative)) { [IO.File]::Delete($file.FullName) }
    }
    foreach ($entry in $Snapshot.GetEnumerator()) {
        $target = Join-Path $absoluteWorkspace $entry.Key
        $directory = Split-Path -Parent $target
        if (-not (Test-Path -LiteralPath $directory)) { $null = New-Item -ItemType Directory -Path $directory -Force }
        [IO.File]::WriteAllBytes($target, [byte[]]$entry.Value)
    }
}
function Sync-CompletedAcceptanceChecks {
    $acceptancePath = Join-Path $absoluteWorkspace 'acceptance-matrix.json'
    $evidencePath = Join-Path $absoluteWorkspace 'evidence.json'
    if (-not (Test-Path -LiteralPath $acceptancePath -PathType Leaf) -or -not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        return @()
    }
    $matrix = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
    $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
    $completedChecks = @($evidence.checks | Where-Object { $_.status -in @('passed', 'not-applicable') -and $_.PSObject.Properties['id'] })
    if ($completedChecks.Count -eq 0) { return @() }
    $criteria = @($matrix.criteria)
    $stopWords = @('acceptance', 'behavior', 'change', 'complete', 'correctly', 'implemented', 'observable', 'outcome', 'requested', 'remains', 'verified')
    $links = [Collections.Generic.List[object]]::new()
    foreach ($criterion in $criteria) {
        $existingIds = @($criterion.mapping.checkIds | Where-Object { $_ })
        $criterionText = @(
            [string]$criterion.text
            @($criterion.mapping.changedPaths)
            @($criterion.mapping.testPaths)
        ) -join ' '
        $hasBehavioralAnchor = @($criterion.mapping.changedPaths).Count + @($criterion.mapping.scenarioIds).Count + @($criterion.mapping.testPaths).Count -gt 0
        $tokens = @([regex]::Matches($criterionText.ToLowerInvariant(), '[\p{L}\p{Nd}]{4,}') | ForEach-Object Value | Where-Object { $_ -notin $stopWords } | Sort-Object -Unique)
        $matches = @($completedChecks | Where-Object {
            $checkText = "$($_.id) $($_.command)".ToLowerInvariant()
            $criteria.Count -eq 1 -or $hasBehavioralAnchor -or @($tokens | Where-Object { $checkText.Contains($_) }).Count -gt 0
        })
        $newIds = @($matches | ForEach-Object { [string]$_.id } | Where-Object { $_ -notin $existingIds } | Sort-Object -Unique)
        if ($newIds.Count -eq 0) { continue }
        $criterion.mapping.checkIds = @($existingIds + $newIds | Sort-Object -Unique)
        foreach ($id in $newIds) {
            $linkReason = if ($criteria.Count -eq 1) { 'single-criterion-completed-check' } elseif ($hasBehavioralAnchor) { 'anchored-criterion-required-check' } else { 'semantic-check-affinity' }
            $links.Add([pscustomobject][ordered]@{ criterionId = [string]$criterion.id; checkId = $id; reason = $linkReason })
        }
    }
    if ($links.Count -gt 0) {
        [IO.File]::WriteAllText($acceptancePath, (($matrix | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    }
    return @($links)
}
function Get-DeliveryAssessment {
    $requirements = Invoke-JsonTool 'Manage-LlmWikiRequirementModel.ps1' @{ Action = 'assess'; WorkspacePath = $workspace }
    $conformance = Invoke-JsonTool 'Manage-LlmWikiPlanConformance.ps1' @{ Action = 'assess'; WorkspacePath = $workspace }
    $proof = Invoke-JsonTool 'Manage-LlmWikiProofOfChange.ps1' @{ Action = 'assess'; WorkspacePath = $workspace }
    $automaticCheckLinks = @(Sync-CompletedAcceptanceChecks)
    $acceptance = Invoke-JsonTool 'Manage-LlmWikiAcceptanceMatrix.ps1' @{
        Action = 'validate'
        Path = "$workspace/acceptance-matrix.json"
        EvidencePath = "$workspace/evidence.json"
        RequireEvidence = $true
    }
    $packet = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-packet.json') -Raw | ConvertFrom-Json
    $packetObjective = Get-LlmWikiPacketObjective $packet
    $taskContract = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'task-contract.json') -Raw | ConvertFrom-Json
    $stalePacketPaths = @($packet.diff.changedPaths | Where-Object {
        $candidate = Join-Path $repositoryRoot ([string]$_)
        if (Test-Path -LiteralPath $candidate) { return $false }
        $nameStatus = @(& git -C $repositoryRoot diff --name-status ([string]$taskContract.git.base) -- ([string]$_))
        if ($LASTEXITCODE -ne 0) { throw "Unable to validate packet path freshness for '$_'." }
        return @($nameStatus | Where-Object { $_ -match '^D\s' }).Count -eq 0
    })
    $journeys = Invoke-JsonTool 'Find-LlmWikiProductJourney.ps1' @{
        Query = $packetObjective
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
        [pscustomobject][ordered]@{ id = 'packet-freshness'; passed = $stalePacketPaths.Count -eq 0; summary = $(if ($stalePacketPaths.Count -eq 0) { 'all packet paths exist or are current Git deletions' } else { "$($stalePacketPaths.Count) stale packet path(s): $($stalePacketPaths -join ', ')" }) }
        [pscustomobject][ordered]@{ id = 'requirements'; passed = [bool]$requirements.valid; summary = "$($requirements.model.classification.criteriaCount) criteria, $(@($requirements.model.findings).Count) blocking finding(s)$(if (@($requirements.model.findings).Count -gt 0) { ': ' + (@($requirements.model.findings | ForEach-Object { "$($_.criterionId)/$($_.id)" }) -join ', ') })" }
        [pscustomobject][ordered]@{ id = 'acceptance'; passed = [bool]$acceptance.valid; summary = "$($acceptance.satisfiedCount)/$($acceptance.criteriaCount) satisfied, $(@($acceptance.unmapped).Count) unmapped, $(@($acceptance.unverified).Count) unverified" }
        [pscustomobject][ordered]@{ id = 'plan-conformance'; passed = [bool]$conformance.valid; summary = "$($conformance.conformance.classification.changedPathCount) changed, $(@($conformance.conformance.classification.unplannedAllowedPaths).Count) unplanned, $(@($conformance.conformance.classification.outOfScopePaths).Count) out of scope" }
        [pscustomobject][ordered]@{ id = 'proof-of-change'; passed = (-not [bool]$proof.applicable -or [bool]$proof.valid); summary = $(if ([bool]$proof.applicable) { "$(@($proof.proof.findings).Count) proof finding(s)$(if (@($proof.proof.findings).Count -gt 0) { ': ' + (@($proof.proof.findings | ForEach-Object { "$($_.criterionId)/$($_.id)" }) -join ', ') })" } else { 'not yet applicable' }) }
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
        objective = $packetObjective
        valid = @($gates | Where-Object { -not $_.passed }).Count -eq 0
        engineeringReadiness = [pscustomobject][ordered]@{
            verdict = $(if (@($coreGates | Where-Object { $_.id -in @('packet-freshness', 'requirements', 'plan-conformance', 'proof-of-change') -and -not $_.passed }).Count -eq 0) { 'ready' } else { 'blocked' })
            gates = @($coreGates | Where-Object id -in @('packet-freshness', 'requirements', 'plan-conformance', 'proof-of-change'))
        }
        governanceCompleteness = [pscustomobject][ordered]@{
            verdict = $(if ([bool]$acceptance.valid -and $evidencePassed) { 'complete' } else { 'incomplete' })
            gates = @($gates | Where-Object id -in @('acceptance', 'evidence'))
        }
        gates = $gates
        requirementCoverage = @($coverage)
        automaticCheckLinks = @($automaticCheckLinks)
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
    if ($Format -eq 'Text') { Write-Host '[1/4] Assessing current delivery state...' }
    $before = if ($null -ne $AssessmentInput) { $AssessmentInput } else { Get-DeliveryAssessment }
    $preview = $RefreshPreviewInput
    if ($DryRun) {
        if ($Format -eq 'Text') { Write-Host '[2/3] Previewing task refresh and evidence invalidation...' }
        if ($null -eq $preview) { $preview = Invoke-JsonTool 'Manage-LlmWikiTaskWorkspace.ps1' @{ Action = 'refresh'; WorkspacePath = $workspace; DryRun = $true } }
    } else {
        $snapshot = Get-WorkspaceSnapshot
        try {
            if ($Format -eq 'Text') { Write-Host '[2/3] Atomically refreshing packet, evidence, report, and manifest...' }
            $appliedRefresh = Invoke-JsonTool 'Manage-LlmWikiTaskWorkspace.ps1' @{ Action = 'refresh'; WorkspacePath = $workspace }
            if ($null -eq $preview) { $preview = $appliedRefresh }
            Invoke-JsonTool 'Manage-LlmWikiPlanConformance.ps1' @{ Action = 'replan'; WorkspacePath = $workspace; Reason = $Reason } | Out-Null
        } catch {
            Restore-WorkspaceSnapshot $snapshot
            throw "Delivery replan failed and the workspace was restored atomically: $($_.Exception.Message)"
        }
    }
    if ($Format -eq 'Text') { Write-Host '[3/3] Reassessing synchronized delivery state...' }
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
        note = 'Replanning atomically refreshes observed evidence and rebuilds the manifest from the task-contract boundary; it does not widen that contract, and failures restore the complete workspace snapshot.'
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
