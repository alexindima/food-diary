[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('create', 'show', 'verify', 'run')]
    [string]$Action = 'show',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [switch]$IncludePassed,
    [switch]$ContinueOnFailure,
    [switch]$DryRun,
    [switch]$FailOnInvalid,
    [switch]$FailOnFailure,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$normalizedWorkspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $normalizedWorkspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$workspaceAbsolute = Join-Path $repositoryRoot $normalizedWorkspace
$evidencePath = Join-Path $workspaceAbsolute 'evidence.json'
$planPath = Join-Path $workspaceAbsolute 'verification-plan.json'
if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) { throw "Evidence is absent: $normalizedWorkspace/evidence.json" }
$workspacePolicyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$changePolicyPath = Join-Path $wikiRoot 'policies/change-policies.json'
$workspacePolicy = Get-Content -LiteralPath $workspacePolicyPath -Raw | ConvertFrom-Json

function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 30 -Compress
    if ($null -eq $json) { $json = 'null' }
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-PlanPayload([object]$Plan) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Plan.schemaVersion
        workspace = [string]$Plan.workspace
        createdAtUtc = ([DateTimeOffset]$Plan.createdAtUtc).ToUniversalTime().ToString('o')
        changeFingerprint = [string]$Plan.changeFingerprint
        changePolicyFingerprint = [string]$Plan.changePolicyFingerprint
        workspacePolicyFingerprint = [string]$Plan.workspacePolicyFingerprint
        includePassed = [bool]$Plan.includePassed
        requestedIncludePassed = [bool]$Plan.requestedIncludePassed
        riskCalibrationHash = [string]$Plan.riskCalibrationHash
        failurePredictionHash = [string]$Plan.failurePredictionHash
        verificationCostHash = [string]$Plan.verificationCostHash
        riskLevel = [string]$Plan.riskLevel
        riskScore = [int]$Plan.riskScore
        executionMode = [string]$Plan.executionMode
        requiredCheckIds = @($Plan.requiredCheckIds)
        executions = @($Plan.executions)
        coverage = @($Plan.coverage)
        decisions = @($Plan.decisions)
        selectionSummary = $Plan.selectionSummary
    }
}
function Get-Current {
    $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
    $policy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') -ChangedPath @($evidence.change.changedPaths) -Format Json | ConvertFrom-Json
    $risk = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRiskCalibration.ps1') verify -WorkspacePath $normalizedWorkspace -Format Json | ConvertFrom-Json
    $prediction = & (Join-Path $PSScriptRoot 'Manage-LlmWikiFailurePrediction.ps1') verify -WorkspacePath $normalizedWorkspace -Format Json | ConvertFrom-Json
    $cost = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationCost.ps1') verify -WorkspacePath $normalizedWorkspace -Format Json | ConvertFrom-Json
    [pscustomobject]@{
        evidence = $evidence
        policy = $policy
        changeFingerprint = Get-Hash @($evidence.change.changedPaths | Sort-Object -Unique)
        changePolicyFingerprint = (Get-FileHash -LiteralPath $changePolicyPath -Algorithm SHA256).Hash.ToLowerInvariant()
        workspacePolicyFingerprint = (Get-FileHash -LiteralPath $workspacePolicyPath -Algorithm SHA256).Hash.ToLowerInvariant()
        risk = $risk
        prediction = $prediction
        cost = $cost
    }
}
function Get-Priority([string]$Id) {
    foreach ($rule in @($workspacePolicy.scheduler.verificationPlanner.priorityPatterns)) {
        if ($Id -match [string]$rule.pattern) { return [int]$rule.priority }
    }
    [int]$workspacePolicy.scheduler.verificationPlanner.defaultPriority
}
function Get-Selection([object]$Current, [bool]$RequestedIncludePassed) {
    $effectiveIncludePassed = $RequestedIncludePassed -or [bool]$Current.risk.calibration.controls.forceIncludePassed
    $checks = @($Current.policy.requiredChecks | Sort-Object id -Unique)
    $eligible = @($checks | Where-Object {
        $entry = $Current.evidence.checks | Where-Object id -eq $_.id | Select-Object -First 1
        $effectiveIncludePassed -or $null -eq $entry -or [string]$entry.status -notin @('passed', 'not-applicable')
    })
    $primaryById = @{}
    foreach ($check in $eligible) { $primaryById[[string]$check.id] = [string]$check.id }
    if (-not [bool]$Current.risk.calibration.controls.requireSequentialExecution) {
        foreach ($primary in @($workspacePolicy.scheduler.verificationPlanner.supersedes.PSObject.Properties)) {
            if (-not $primaryById.ContainsKey($primary.Name)) { continue }
            foreach ($coveredId in @($primary.Value)) {
                if ($primaryById.ContainsKey([string]$coveredId)) { $primaryById[[string]$coveredId] = $primary.Name }
            }
        }
        foreach ($group in @($eligible | Group-Object command)) {
            $ids = @($group.Group.id | Sort-Object)
            $primary = @($ids | Where-Object { $primaryById[$_] -eq $_ } | Select-Object -First 1)
            if ($primary.Count -eq 0) { $primary = @($ids[0]) }
            foreach ($id in $ids) {
                if ($primaryById[$id] -eq $id) { $primaryById[$id] = $primary[0] }
            }
        }
    }
    $executions = @($eligible | Where-Object { $primaryById[[string]$_.id] -eq [string]$_.id } | ForEach-Object {
        $primaryId = [string]$_.id
        $coveredIds = @($eligible.id | Where-Object { $primaryById[[string]$_] -eq $primaryId } | Sort-Object)
        $failureProbability = [int](($Current.prediction.prediction.predictions | Where-Object checkId -in $coveredIds | Measure-Object probabilityPercent -Maximum).Maximum)
        $costEstimates = @($Current.cost.forecast.estimates | Where-Object checkId -in $coveredIds)
        $basePriority = Get-Priority $primaryId
        $predictionBoost = [int][Math]::Floor($failureProbability / [int]$workspacePolicy.scheduler.verificationPlanner.failurePrediction.priorityBoostDivisor)
        $costBoost = [int](($costEstimates | Measure-Object priorityBoost -Maximum).Maximum)
        [pscustomobject][ordered]@{
            primaryCheckId = $primaryId
            command = [string]$_.command
            priority = [Math]::Max(0, $basePriority - $predictionBoost - $costBoost)
            basePriority = $basePriority
            predictedFailureProbability = $failureProbability
            predictionPriorityBoost = $predictionBoost
            costPriorityBoost = $costBoost
            expectedVerificationSeconds = [double](($costEstimates | Measure-Object verificationSeconds -Maximum).Maximum)
            expectedFailureSeconds = [Math]::Round([double](($costEstimates | Measure-Object expectedFailureSeconds -Sum).Sum), 2)
            expectedTotalSeconds = [Math]::Round([double](($costEstimates | Measure-Object expectedTotalSeconds -Sum).Sum), 2)
            valueDensity = [double](($costEstimates | Measure-Object valueDensity -Maximum).Maximum)
            coversCheckIds = $coveredIds
        }
    } | Sort-Object priority, primaryCheckId)
    $coverage = @($checks | ForEach-Object {
        $entry = $Current.evidence.checks | Where-Object id -eq $_.id | Select-Object -First 1
        $alreadyResolved = -not $effectiveIncludePassed -and $null -ne $entry -and [string]$entry.status -in @('passed', 'not-applicable')
        [pscustomobject][ordered]@{
            checkId = [string]$_.id
            mode = $(if ($alreadyResolved) { 'existing-evidence' } elseif ($primaryById[[string]$_.id] -eq [string]$_.id) { 'execute' } else { 'covered' })
            primaryCheckId = $(if ($alreadyResolved) { $null } else { $primaryById[[string]$_.id] })
        }
    } | Sort-Object checkId)
    $decisions = @($checks | ForEach-Object {
        $check = $_
        $coverageItem = $coverage | Where-Object checkId -eq $check.id | Select-Object -First 1
        $evidenceEntry = $Current.evidence.checks | Where-Object id -eq $check.id | Select-Object -First 1
        $matchedRule = $Current.policy.matchedRules | Where-Object id -eq $check.sourceRule | Select-Object -First 1
        $prediction = $Current.prediction.prediction.predictions | Where-Object checkId -eq $check.id | Select-Object -First 1
        $cost = $Current.cost.forecast.estimates | Where-Object checkId -eq $check.id | Select-Object -First 1
        $rationale = if ($coverageItem.mode -eq 'existing-evidence') {
            "Retained trusted '$($evidenceEntry.status)' evidence; risk controls did not require a fresh run."
        } elseif ($coverageItem.mode -eq 'covered') {
            "Covered by policy-approved supersedence or an identical canonical command executed as '$($coverageItem.primaryCheckId)'."
        } else {
            'Selected as the canonical execution for this requirement and any explicitly covered checks.'
        }
        [pscustomobject][ordered]@{
            checkId = [string]$check.id
            sourceRule = [string]$check.sourceRule
            matchedPaths = @($matchedRule.matchedPaths | Sort-Object -Unique)
            disposition = [string]$coverageItem.mode
            primaryCheckId = $coverageItem.primaryCheckId
            evidenceStatus = $(if ($null -eq $evidenceEntry) { 'absent' } else { [string]$evidenceEntry.status })
            predictedFailureProbability = $(if ($null -eq $prediction) { 0 } else { [int]$prediction.probabilityPercent })
            estimatedVerificationSeconds = $(if ($null -eq $cost) { 0 } else { [double]$cost.verificationSeconds })
            rationale = $rationale
        }
    } | Sort-Object checkId)
    $fullRequiredSeconds = [double](($Current.cost.forecast.estimates | Where-Object checkId -in @($checks.id) | Measure-Object verificationSeconds -Sum).Sum)
    $eligibleSeconds = [double](($Current.cost.forecast.estimates | Where-Object checkId -in @($eligible.id) | Measure-Object verificationSeconds -Sum).Sum)
    $selectedSeconds = [double](($executions | Measure-Object expectedVerificationSeconds -Sum).Sum)
    $selectionSummary = [pscustomobject][ordered]@{
        requiredCheckCount = $checks.Count
        eligibleCheckCount = $eligible.Count
        executionCount = $executions.Count
        reusedEvidenceCount = @($coverage | Where-Object mode -eq 'existing-evidence').Count
        consolidatedCheckCount = @($coverage | Where-Object mode -eq 'covered').Count
        fullRequiredVerificationSeconds = [Math]::Round($fullRequiredSeconds, 2)
        eligibleVerificationSeconds = [Math]::Round($eligibleSeconds, 2)
        plannedExecutionSeconds = [Math]::Round($selectedSeconds, 2)
        evidenceReuseSavingsSeconds = [Math]::Round($fullRequiredSeconds - $eligibleSeconds, 2)
        consolidationSavingsSeconds = [Math]::Round($eligibleSeconds - $selectedSeconds, 2)
        totalSavingsSeconds = [Math]::Round($fullRequiredSeconds - $selectedSeconds, 2)
        totalSavingsPercent = $(if ($fullRequiredSeconds -le 0) { 0.0 } else { [Math]::Round(($fullRequiredSeconds - $selectedSeconds) * 100 / $fullRequiredSeconds, 2) })
        safetyInvariant = 'every-required-check-covered-exactly-once'
    }
    [pscustomobject]@{
        includePassed = $effectiveIncludePassed
        executionMode = $(if ([bool]$Current.risk.calibration.controls.requireSequentialExecution) { 'exhaustive' } else { 'optimized' })
        checks = $checks
        executions = $executions
        coverage = $coverage
        decisions = $decisions
        selectionSummary = $selectionSummary
    }
}
function Test-Plan([object]$Plan) {
    $issues = [Collections.Generic.List[string]]::new()
    $current = Get-Current
    if ([int]$Plan.schemaVersion -ne 2) { $issues.Add('schemaVersion must be 2.') }
    if ([string]$Plan.planHash -cne (Get-Hash (Get-PlanPayload $Plan))) { $issues.Add('Plan hash is invalid.') }
    if ([string]$Plan.workspace -cne $normalizedWorkspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Plan.changeFingerprint -cne [string]$current.changeFingerprint) { $issues.Add('Changed paths drifted.') }
    if ([string]$Plan.changePolicyFingerprint -cne [string]$current.changePolicyFingerprint) { $issues.Add('Change policy drifted.') }
    if ([string]$Plan.workspacePolicyFingerprint -cne [string]$current.workspacePolicyFingerprint) { $issues.Add('Workspace policy drifted.') }
    if (-not $current.risk.valid) { $issues.Add("Risk calibration is invalid: $(@($current.risk.issues) -join ' ')") }
    if ([string]$Plan.riskCalibrationHash -cne [string]$current.risk.calibration.calibrationHash) { $issues.Add('Risk calibration drifted.') }
    if (-not $current.prediction.valid) { $issues.Add("Failure prediction is invalid: $(@($current.prediction.issues) -join ' ')") }
    if ([string]$Plan.failurePredictionHash -cne [string]$current.prediction.prediction.predictionHash) { $issues.Add('Failure prediction drifted.') }
    if (-not $current.cost.valid) { $issues.Add("Verification cost forecast is invalid: $(@($current.cost.issues) -join ' ')") }
    if ([string]$Plan.verificationCostHash -cne [string]$current.cost.forecast.costHash) { $issues.Add('Verification cost forecast drifted.') }
    if ([string]$Plan.riskLevel -cne [string]$current.risk.calibration.level -or [int]$Plan.riskScore -ne [int]$current.risk.calibration.score) { $issues.Add('Risk classification drifted.') }
    if ([bool]$Plan.includePassed -ne ([bool]$Plan.requestedIncludePassed -or [bool]$current.risk.calibration.controls.forceIncludePassed)) { $issues.Add('Risk-driven includePassed control is inconsistent.') }
    $expectedMode = if ([bool]$current.risk.calibration.controls.requireSequentialExecution) { 'exhaustive' } else { 'optimized' }
    if ([string]$Plan.executionMode -cne $expectedMode) { $issues.Add('Risk-driven execution mode is inconsistent.') }
    $expectedSelection = Get-Selection $current ([bool]$Plan.requestedIncludePassed)
    if ((Get-Hash @($Plan.executions)) -cne (Get-Hash @($expectedSelection.executions))) {
        $issues.Add('Verification executions do not match the canonical adaptive selection.')
    }
    if ((Get-Hash @($Plan.coverage)) -cne (Get-Hash @($expectedSelection.coverage))) {
        $issues.Add('Verification coverage does not match the canonical adaptive selection.')
    }
    if ((Get-Hash @($Plan.decisions)) -cne (Get-Hash @($expectedSelection.decisions))) {
        $issues.Add('Verification decision trace does not match the canonical adaptive selection.')
    }
    if ((Get-Hash $Plan.selectionSummary) -cne (Get-Hash $expectedSelection.selectionSummary)) {
        $issues.Add('Verification selection economics are invalid.')
    }
    $required = @($current.policy.requiredChecks.id | Sort-Object -Unique)
    $recorded = @($Plan.requiredCheckIds | Sort-Object -Unique)
    if ($required.Count -ne $recorded.Count -or @(Compare-Object $required $recorded).Count -ne 0) { $issues.Add('Required check set drifted.') }
    $covered = @($Plan.coverage.checkId | Sort-Object -Unique)
    $duplicateCoverage = @($Plan.coverage | Group-Object checkId | Where-Object Count -ne 1)
    if ($covered.Count -ne $required.Count -or @(Compare-Object $covered $required).Count -ne 0 -or $duplicateCoverage.Count -gt 0) {
        $issues.Add('Plan does not cover every required check exactly once.')
    }
    $executionIds = @($Plan.executions.primaryCheckId)
    foreach ($coverageItem in @($Plan.coverage | Where-Object mode -in @('execute', 'covered'))) {
        if ([string]$coverageItem.primaryCheckId -notin $executionIds) { $issues.Add("Coverage for '$($coverageItem.checkId)' references a missing execution.") }
    }
    foreach ($execution in @($Plan.executions)) {
        $canonical = @($current.policy.requiredChecks | Where-Object id -eq $execution.primaryCheckId | Select-Object -First 1)
        if ($canonical.Count -ne 1 -or [string]$canonical[0].command -cne [string]$execution.command) {
            $issues.Add("Execution '$($execution.primaryCheckId)' is not canonical.")
        }
        $expectedCovered = @($Plan.coverage | Where-Object primaryCheckId -eq $execution.primaryCheckId | Select-Object -ExpandProperty checkId | Sort-Object)
        $actualCovered = @($execution.coversCheckIds | Sort-Object)
        if ($expectedCovered.Count -ne $actualCovered.Count -or @(Compare-Object $expectedCovered $actualCovered).Count -ne 0) {
            $issues.Add("Execution '$($execution.primaryCheckId)' coverage is inconsistent.")
        }
    }
    [pscustomobject]@{ valid = $issues.Count -eq 0; issues = @($issues); current = $current }
}
function Write-Plan([object]$Plan) {
    [IO.File]::WriteAllText($planPath, (($Plan | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}

if ($Action -eq 'create') {
    $riskCalibration = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRiskCalibration.ps1') create `
        -WorkspacePath $normalizedWorkspace `
        -AsOfUtc $AsOfUtc `
        -Format Json | ConvertFrom-Json
    $failurePrediction = & (Join-Path $PSScriptRoot 'Manage-LlmWikiFailurePrediction.ps1') create `
        -WorkspacePath $normalizedWorkspace `
        -AsOfUtc $AsOfUtc `
        -Format Json | ConvertFrom-Json
    $verificationCost = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationCost.ps1') create `
        -WorkspacePath $normalizedWorkspace `
        -AsOfUtc $AsOfUtc `
        -Format Json | ConvertFrom-Json
    $current = Get-Current
    if (-not $riskCalibration.valid -or -not $failurePrediction.valid -or -not $verificationCost.valid -or -not $current.risk.valid -or -not $current.prediction.valid -or -not $current.cost.valid) { throw 'Unable to create valid risk, failure prediction, and verification cost inputs.' }
    $selection = Get-Selection $current ([bool]$IncludePassed)
    $plan = [pscustomobject][ordered]@{
        schemaVersion = 2
        workspace = $normalizedWorkspace
        createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        changeFingerprint = $current.changeFingerprint
        changePolicyFingerprint = $current.changePolicyFingerprint
        workspacePolicyFingerprint = $current.workspacePolicyFingerprint
        includePassed = [bool]$selection.includePassed
        requestedIncludePassed = [bool]$IncludePassed
        riskCalibrationHash = [string]$riskCalibration.calibration.calibrationHash
        failurePredictionHash = [string]$failurePrediction.prediction.predictionHash
        verificationCostHash = [string]$verificationCost.forecast.costHash
        riskLevel = [string]$riskCalibration.calibration.level
        riskScore = [int]$riskCalibration.calibration.score
        executionMode = [string]$selection.executionMode
        requiredCheckIds = @($selection.checks.id | Sort-Object -Unique)
        executions = @($selection.executions)
        coverage = @($selection.coverage)
        decisions = @($selection.decisions)
        selectionSummary = $selection.selectionSummary
        planHash = ''
    }
    $plan.planHash = Get-Hash (Get-PlanPayload $plan)
    Write-Plan $plan
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRouting.ps1') create `
        -WorkspacePath $normalizedWorkspace `
        -AsOfUtc $AsOfUtc `
        -Format Json | Out-Null
    $result = [pscustomobject][ordered]@{ action = 'create'; valid = $true; plan = $plan; savedPath = "$normalizedWorkspace/verification-plan.json" }
} else {
    if (-not (Test-Path -LiteralPath $planPath -PathType Leaf)) { throw "Verification plan is absent: $normalizedWorkspace/verification-plan.json" }
    $plan = Get-Content -LiteralPath $planPath -Raw | ConvertFrom-Json
    $validation = Test-Plan $plan
    if ($Action -eq 'verify') {
        $result = [pscustomobject][ordered]@{ action = 'verify'; valid = $validation.valid; issues = @($validation.issues); plan = $plan }
    } elseif ($Action -eq 'run') {
        if (-not $validation.valid) { throw "Verification plan is invalid: $(@($validation.issues) -join ' ')" }
        $runs = [Collections.Generic.List[object]]::new()
        $failed = 0
        foreach ($execution in @($plan.executions)) {
            $run = & (Join-Path $PSScriptRoot 'Invoke-LlmWikiTaskChecks.ps1') -WorkspacePath $normalizedWorkspace -CheckId $execution.primaryCheckId -IncludePassed:([bool]$plan.includePassed) -DryRun:$DryRun -Format Json | ConvertFrom-Json
            $runFailed = -not $DryRun -and [int]$run.failureCount -gt 0
            if ($runFailed) { $failed++ }
            $runs.Add([pscustomobject][ordered]@{ primaryCheckId = $execution.primaryCheckId; coversCheckIds = @($execution.coversCheckIds); result = $run })
            if (-not $DryRun -and -not $runFailed -and [int]$run.executedCount -gt 0) {
                foreach ($coveredId in @($execution.coversCheckIds | Where-Object { $_ -ne $execution.primaryCheckId })) {
                    $coveredEntry = $validation.current.evidence.checks | Where-Object id -eq $coveredId | Select-Object -First 1
                    & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvidence.ps1') check -Path "$normalizedWorkspace/evidence.json" -Id $coveredId -Status passed -Command $coveredEntry.command -Reason "Covered by verification plan execution '$($execution.primaryCheckId)'." | Out-Null
                }
            }
            if ($runFailed -and -not $ContinueOnFailure) { break }
        }
        if (-not $DryRun) { & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') refresh -WorkspacePath $normalizedWorkspace | Out-Null }
        $result = [pscustomobject][ordered]@{ action = 'run'; valid = $failed -eq 0; dryRun = [bool]$DryRun; executionCount = $runs.Count; failureCount = $failed; runs = @($runs); planHash = $plan.planHash }
    } else {
        $result = [pscustomobject][ordered]@{ action = 'show'; valid = $validation.valid; issues = @($validation.issues); plan = $plan }
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    Write-Host "Verification plan: action=$($result.action), valid=$($result.valid)"
    if ($null -ne $result.plan) {
        Write-Host "Checks=$(@($result.plan.requiredCheckIds).Count), executions=$(@($result.plan.executions).Count), savings=$($result.plan.selectionSummary.totalSavingsSeconds)s ($($result.plan.selectionSummary.totalSavingsPercent)%), hash=$($result.plan.planHash)"
        foreach ($execution in @($result.plan.executions)) { Write-Host " - [$($execution.priority)] $($execution.primaryCheckId) covers $(@($execution.coversCheckIds) -join ', ')" }
        foreach ($decision in @($result.plan.decisions)) { Write-Host "   $($decision.checkId): $($decision.disposition) - $($decision.rationale)" }
    }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
if ($FailOnFailure -and $Action -eq 'run' -and [int]$result.failureCount -gt 0) { exit 1 }
