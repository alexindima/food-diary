[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$failures = [Collections.Generic.List[string]]::new()
function Assert-Adaptive([bool]$Condition, [string]$Message) {
    if (-not $Condition) { $script:failures.Add($Message) }
}

$visualPaths = @(
    'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result/ai-photo-preview/ai-photo-preview.html'
    'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result/ai-photo-result.scss'
)
$tiny = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Improve photo annotation visibility with clearer SVG connectors and point styling.' `
    -ProposedPath $visualPaths `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($tiny.profile -eq 'tiny') 'Bounded visual work was not routed as tiny.'
Assert-Adaptive (-not $tiny.requiresDesign -and -not $tiny.requiresWorkspace) 'Tiny work retained heavyweight design or workspace requirements.'
Assert-Adaptive (@($tiny.stages.id) -notcontains 'independent-review') 'Tiny work retained critical independent review.'
Assert-Adaptive (@($tiny.stages | Where-Object { $_.id -eq 'verification' -and $_.command -match 'verify-fast' }).Count -eq 1) 'Tiny work did not select verify-fast.'
Assert-Adaptive ($tiny.ceremonyBudget.label -eq 'minimal') 'Tiny work omitted its minimal ceremony budget.'

$critical = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix Google authentication token linking for an existing account.' `
    -ProposedPath 'FoodDiary.Presentation.Api/Features/Auth/AuthSessionController.cs' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($critical.profile -eq 'critical') 'Authentication and credential work was not routed as critical.'
Assert-Adaptive ($critical.requiresDecisionCheckpoint -and $critical.requiresWorkspace) 'Critical work omitted checkpoint or governed workspace.'
Assert-Adaptive (@($critical.stages.id) -contains 'independent-review') 'Critical work omitted independent review.'
Assert-Adaptive (@($critical.stages.id) -contains 'requirements') 'Critical work omitted requirement quality assessment.'
Assert-Adaptive (@($critical.stages.id) -contains 'delivery-validation') 'Critical work omitted evidence-backed delivery validation.'
Assert-Adaptive (@($critical.stages | Where-Object { $_.id -eq 'independent-review' -and $_.command -match 'delivery-critique' }).Count -eq 1) 'Critical work did not route through adverse delivery critique.'

$journeys = & (Join-Path $PSScriptRoot 'Find-LlmWikiProductJourney.ps1') `
    -Query 'Fix the dietologist invitation email link' `
    -ChangedPath 'FoodDiary.Application/Dietologist/Services/DietologistEmailSender.cs' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (@($journeys.journeys.id) -contains 'FD-DIET') 'Journey impact omitted dietologist collaboration.'
Assert-Adaptive (@($journeys.journeys.id) -contains 'FD-MAIL') 'Journey impact omitted transactional email.'
Assert-Adaptive (@($journeys.journeys.id) -notcontains 'FD-MEAL') 'Journey impact produced a broad meal-tracking false positive.'

$ungrounded = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix quasar zephyr nimbus anomaly.' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (-not $ungrounded.scopeKnown -and $ungrounded.requiresPathDiscovery) 'Ungrounded intent silently absorbed working-tree paths.'
Assert-Adaptive ($ungrounded.confidence -eq 'low') 'Ungrounded intent did not expose low confidence.'

$precedents = & (Join-Path $PSScriptRoot 'Get-LlmWikiGitPrecedents.ps1') `
    -Objective 'Improve photo annotation visibility' `
    -ScopePath 'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result' `
    -Limit 5 `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($precedents.searchedCommitCount -gt 0) 'Precedent search inspected no Git history.'
Assert-Adaptive (@($precedents.precedents | Where-Object subject -match 'photo annotation').Count -gt 0) 'Precedent search omitted the known photo-annotation history.'

$research = & (Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1') `
    -Objective 'Improve photo annotation visibility' `
    -ProposedPath 'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result' `
    -Limit 5 `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (@($research.discovery.groundedPaths).Count -gt 0) 'Research did not ground the task in current repository paths.'
Assert-Adaptive (@($research.precedents).Count -gt 0) 'Research omitted Git precedents.'
Assert-Adaptive (@($research.authority).Count -ge 2) 'Research omitted authority and provenance guidance.'
Assert-Adaptive (@($research.researchLanes.id) -contains 'integrations') 'Research packet omitted the integrations investigation lane.'

$solutions = & (Join-Path $PSScriptRoot 'Get-LlmWikiSolutionComparison.ps1') `
    -Objective 'Improve the Wiki developer experience.' `
    -Option 'Extend the existing adaptive flow.','Replace it with a second workflow.' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($solutions.alternatives.Count -eq 2 -and $solutions.recommendedOptionId -eq 'OPT-01') 'Solution comparison did not prefer the bounded existing-flow option.'

$qa = & (Join-Path $PSScriptRoot 'Get-LlmWikiManualQaPlan.ps1') `
    -Objective 'Fix the dietologist invitation email link.' `
    -ProposedPath 'FoodDiary.Application/Dietologist/Services/DietologistEmailSender.cs' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (@($qa.journeys) -contains 'FD-DIET') 'Manual QA plan omitted the matched product journey.'
Assert-Adaptive (@($qa.cases.id) -contains 'QA-ERROR') 'Manual QA plan omitted generic negative coverage.'
Assert-Adaptive (@($qa.cases.id) -notcontains 'QA-MOBILE') 'Backend-only manual QA plan retained irrelevant frontend ceremony.'

$experience = & (Join-Path $PSScriptRoot 'Get-LlmWikiExperience.ps1') `
    -Action next `
    -Objective 'Improve photo annotation visibility with clearer SVG connectors.' `
    -ProposedPath $visualPaths `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (-not [string]::IsNullOrWhiteSpace([string]$experience.nextAction)) 'Compact experience did not return one next action.'
Assert-Adaptive ($experience.ceremonyBudget.label -eq 'minimal') 'Compact experience omitted the routed ceremony budget.'

$metrics = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkflowMetrics.ps1') `
    -TasksPath '.artifacts/llm-wiki/no-such-task-root' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($metrics.schemaVersion -eq 1 -and $metrics.workspaceCount -eq 0) 'Workflow metrics did not handle an empty task history.'

$workspaceName = "adaptive-smoke-$([Guid]::NewGuid().ToString('N'))"
$workspace = ".artifacts/llm-wiki/tasks/$workspaceName"
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
try {
    New-Item -ItemType Directory -Path $absoluteWorkspace -Force | Out-Null
    $descriptor = [pscustomobject]@{ objective = 'Exercise adaptive pause and resume.' }
    [IO.File]::WriteAllText((Join-Path $absoluteWorkspace 'workspace.json'), (($descriptor | ConvertTo-Json) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $status = [pscustomobject]@{
        currentPacketFingerprint = ('a' * 64)
        refreshRequired = $false
        blockingReasons = @()
        nextActions = @('Continue focused implementation.')
    }
    $pause = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAdaptiveSession.ps1') pause `
        -WorkspacePath $workspace `
        -StatusInput $status `
        -HandoffMarkdown @('# AI Task Handoff', '', 'Synthetic bounded smoke handoff.') `
        -Format Json | ConvertFrom-Json
    Assert-Adaptive ($pause.valid -and $pause.session.state -eq 'paused') 'Pause did not create a valid continuity receipt.'
    Assert-Adaptive (Test-Path -LiteralPath (Join-Path $absoluteWorkspace 'adaptive-handoff.md') -PathType Leaf) 'Pause omitted its handoff file.'
    $resume = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAdaptiveSession.ps1') resume `
        -WorkspacePath $workspace `
        -StatusInput $status `
        -DoctorInput ([pscustomobject]@{ valid = $true }) `
        -Format Json | ConvertFrom-Json
    Assert-Adaptive ($resume.valid -and $resume.canContinueWithoutRefresh) 'Resume rejected unchanged valid continuity.'
    Assert-Adaptive (-not $resume.continuity.headChanged -and -not $resume.continuity.packetDrift) 'Resume reported false drift.'
} finally {
    if (Test-Path -LiteralPath $absoluteWorkspace) { Remove-Item -LiteralPath $absoluteWorkspace -Recurse -Force }
}

$deliveryWorkspaceName = "delivery-smoke-$([Guid]::NewGuid().ToString('N'))"
$deliveryWorkspace = ".artifacts/llm-wiki/tasks/$deliveryWorkspaceName"
$deliveryAbsolute = Join-Path $repositoryRoot $deliveryWorkspace
try {
    New-Item -ItemType Directory -Path $deliveryAbsolute -Force | Out-Null
    $deliveryFixture = [pscustomobject]@{
        valid = $false
        gates = @(
            [pscustomobject]@{ id = 'requirements'; passed = $true; summary = '1 criterion, 0 blocking findings' }
            [pscustomobject]@{ id = 'acceptance'; passed = $false; summary = '0/1 satisfied' }
            [pscustomobject]@{ id = 'proof-of-change'; passed = $false; summary = 'not yet applicable' }
        )
        requirementCoverage = @([pscustomobject]@{ id = 'AC-001'; text = 'The invitation email opens the authenticated invitation page.'; status = 'pending'; mapped = $false; mappingCount = 0; evidenceNote = ''; proven = $false })
        journeyImpact = @([pscustomobject]@{ id = 'FD-DIET'; risk = 'critical'; title = 'Dietologist invitation and collaboration' })
        nextActions = @('Map and prove acceptance criterion AC-001.')
    }
    $delivery = & (Join-Path $PSScriptRoot 'Invoke-LlmWikiDeliveryWorkflow.ps1') status `
        -WorkspacePath $deliveryWorkspace `
        -AssessmentInput $deliveryFixture `
        -Format Json | ConvertFrom-Json
    Assert-Adaptive (@($delivery.assessment.gates.id) -contains 'requirements') 'Delivery status omitted the requirement gate.'
    Assert-Adaptive (@($delivery.assessment.gates.id) -contains 'proof-of-change') 'Delivery status omitted proof-of-change.'
    Assert-Adaptive (@($delivery.assessment.journeyImpact.id) -contains 'FD-DIET') 'Delivery status omitted journey impact.'
    Assert-Adaptive (-not $delivery.valid) 'Unresolved acceptance evidence was incorrectly approved.'
    $replan = & (Join-Path $PSScriptRoot 'Invoke-LlmWikiDeliveryWorkflow.ps1') replan `
        -WorkspacePath $deliveryWorkspace `
        -Reason 'Synthetic dry-run validates controlled replanning.' `
        -DryRun `
        -AssessmentInput $deliveryFixture `
        -RefreshPreviewInput ([pscustomobject]@{ invalidation = [pscustomobject]@{ invalidatedChecks = @(); invalidatedReviews = @(); invalidatedCriteria = @() } }) `
        -Format Json | ConvertFrom-Json
    Assert-Adaptive (-not $replan.applied) 'Dry-run replanning mutated the workspace.'
    Assert-Adaptive ($replan.note -match 'does not widen') 'Replanning omitted its scope-boundary invariant.'
} finally {
    if (Test-Path -LiteralPath $deliveryAbsolute) { Remove-Item -LiteralPath $deliveryAbsolute -Recurse -Force }
}

if ($failures.Count -gt 0) {
    Write-Host "Adaptive workflow smoke failed with $($failures.Count) error(s):"
    foreach ($failure in $failures) { Write-Host " - $failure" }
    exit 1
}
Write-Host 'Adaptive workflow smoke passed: routing, ceremony budgets, compact next action, solutions, QA journeys, delivery gates, controlled replanning, research, precedents, and pause/resume continuity.'
