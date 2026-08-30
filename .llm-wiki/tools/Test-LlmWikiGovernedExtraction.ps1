[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiExtractionPlanning.ps1')

$objective = 'Strengthen and extract Dashboard into an isolated application module while preserving its composition dependencies'
$plan = Get-LlmWikiExtractionPlan $objective $repositoryRoot
if ($null -eq $plan -or $plan.module -ne 'Dashboard') { throw 'Extraction planning did not identify Dashboard from the governed intent.' }
if (@($plan.criteria).Count -ne 5) { throw 'Extraction planning did not produce five atomic acceptance outcomes.' }
. (Join-Path $PSScriptRoot 'LlmWikiRequirementCriteria.ps1')
$requirementPolicy = (Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/policies/workspace-policies.json') -Raw | ConvertFrom-Json).requirementModel
if (@($plan.criteria | Where-Object { -not (Test-LlmWikiCriterionAtomic ([string]$_) $requirementPolicy) }).Count -gt 0) {
    throw 'Extraction planning generated a compound acceptance criterion.'
}
$fastingObjective = 'Continue the Fasting modular-monolith extraction by moving its domain model and persistence implementation'
$fastingPlan = Get-LlmWikiExtractionPlan $fastingObjective $repositoryRoot
if ($null -eq $fastingPlan -or $fastingPlan.module -ne 'Fasting') {
    throw 'Extraction planning mistook prose after the word extraction for a module name.'
}

$hydrationObjective = 'Выполнить перенос Hydration в Modules/Hydration с сохранением контрактов'
$hydrationPlan = Get-LlmWikiExtractionPlan $hydrationObjective $repositoryRoot
if ($null -eq $hydrationPlan -or $hydrationPlan.module -ne 'Hydration') {
    throw 'Russian module-transfer intent did not resolve Hydration extraction planning.'
}
if (@($hydrationPlan.paths) -notcontains 'Modules/Hydration') {
    throw 'Hydration extraction planning omitted the canonical Modules/Hydration logical root.'
}
$weeklyGoalsObjective = 'Выполни полный Wiki-first перенос вертикального модуля WeeklyGoals в Modules/WeeklyGoals с сохранением CLR API'
$weeklyGoalsPlan = Get-LlmWikiExtractionPlan $weeklyGoalsObjective $repositoryRoot
if ($null -eq $weeklyGoalsPlan -or $weeklyGoalsPlan.module -ne 'WeeklyGoals') {
    throw 'Russian vertical-module transfer intent mistook the Modules path segment for the module name.'
}
if (@($weeklyGoalsPlan.criteria | Where-Object { $_ -match 'FoodDiary\.Application\.Modules|AddModulesModule' }).Count -gt 0) {
    throw 'WeeklyGoals extraction planning generated acceptance criteria for the generic Modules path segment.'
}
if ($weeklyGoalsPlan.criteria[0] -ne 'WeeklyGoals application source lives in Modules/WeeklyGoals/Application.') {
    throw 'Explicit Modules/WeeklyGoals extraction intent retained the legacy application project as its target.'
}
$hydrationJourneys = & (Join-Path $PSScriptRoot 'Find-LlmWikiProductJourney.ps1') `
    -Query $hydrationObjective `
    -ChangedPath @(
        'Modules/Hydration/Application/Commands/CreateHydrationEntry/CreateHydrationEntryCommandHandler.cs'
        'FoodDiary.Application.Dashboard/FoodDiary.Application.Dashboard.csproj'
    ) `
    -Format Json | ConvertFrom-Json
if (@($hydrationJourneys.journeys | Where-Object id -eq 'FD-MEAL').Count -gt 0) {
    throw 'Hydration extraction inherited the Meal journey from a changed projection consumer.'
}
$supplementalCriteria = @(Get-LlmWikiSupplementalAcceptanceCriteria `
    $hydrationObjective `
    @('Backend', 'Api', 'Contracts', 'Database', 'Frontend', 'Localization') `
    @('FoodDiary.JobManager/HydrationRecurringJob.cs', 'Modules/Hydration/Application/NotificationService.cs'))
if (@($supplementalCriteria | Where-Object { -not (Test-LlmWikiCriterionAtomic ([string]$_) $requirementPolicy) }).Count -gt 0) {
    throw 'Development start generated a compound supplemental acceptance criterion.'
}
$compositionOnlyCriteria = @(Get-LlmWikiSupplementalAcceptanceCriteria `
    'Extract WeeklyCheckIn application ownership without changing background processing' `
    @('Backend') `
    @('FoodDiary.JobManager/FoodDiary.JobManager.csproj', 'Modules/WeeklyCheckIn/Application/DependencyInjection.cs'))
if (@($compositionOnlyCriteria | Where-Object { $_ -match '(?i)background job' }).Count -gt 0) {
    throw 'A composition-only JobManager project reference generated false background-job acceptance criteria.'
}
foreach ($expectedCriterion in @(
    'HTTP routes match the intended behavior.'
    'HTTP payloads match the intended behavior.'
    'HTTP status codes match the intended behavior.'
    'Persistence mappings match the intended model.'
    'The project dependency graph remains acyclic.'
    'Russian text renders without corruption.'
)) {
    if ($expectedCriterion -notin $supplementalCriteria) {
        throw "Development start omitted atomic criterion: $expectedCriterion"
    }
}
if (@($fastingPlan.criteria | Where-Object { $_ -match '(?i)\bby\b' }).Count -gt 0 -or
    $fastingPlan.criteria[0] -notmatch '^Fasting application source lives in Modules/Fasting/Application\.$') {
    throw 'Logical-root extraction criteria do not use the canonical Fasting application source mapping.'
}
foreach ($requiredPath in @(
    'FoodDiary.Application.Dashboard'
    'FoodDiary.Application.Abstractions/Dashboard'
    'FoodDiary.Initializer/Program.cs'
    'FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs'
    'FoodDiary.Web.Api/FoodDiary.Web.Api.csproj'
    'FoodDiary.slnx'
    'tests/FoodDiary.ArchitectureTests/DashboardModuleExtractionTests.cs'
    'tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs'
    'docs/architecture/backend-modules.json'
    'docs/architecture/module-dependencies.json'
    'docs/backend/BACKEND_MODULE_OWNERSHIP.md'
)) {
    if ($requiredPath -notin @($plan.paths)) { throw "Extraction planning omitted required boundary path: $requiredPath" }
}

. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$criteriaWorkspace = New-LlmWikiSmokeFixtureRepositoryPath -RepositoryRoot $repositoryRoot -Name 'governed-generated-criteria'
$criteriaWorkspaceAbsolute = Join-Path $repositoryRoot $criteriaWorkspace
try {
    $legacyGeneratedCriteria = @(
        'HTTP routes, payloads, and status codes match the intended behavior.'
        'Persistence mappings and schema changes are verified; every migration includes its Designer and model snapshot updates when applicable.'
        'Cross-module and project dependencies remain allowed, acyclic, and covered by architecture checks.'
    )
    & (Join-Path $PSScriptRoot 'Initialize-LlmWikiTaskWorkspace.ps1') `
        -Objective 'Verify generated acceptance expansion' `
        -Criterion $legacyGeneratedCriteria `
        -WorkspacePath $criteriaWorkspace `
        -ChangedPath 'FoodDiary.slnx' `
        -PlannedPath 'FoodDiary.slnx' | Out-Null
    $expanded = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRequirementModel.ps1') expand `
        -WorkspacePath $criteriaWorkspace `
        -Reason 'Regression coverage for legacy generated criteria.' `
        -Format Json | ConvertFrom-Json
    if (-not $expanded.valid -or $expanded.addedCount -ne 7) {
        throw 'Legacy generated criteria did not expand into ten atomic outcomes.'
    }
} finally {
    if (Test-Path -LiteralPath $criteriaWorkspaceAbsolute) { Remove-Item -LiteralPath $criteriaWorkspaceAbsolute -Recurse -Force }
}

$workspace = New-LlmWikiSmokeFixtureRepositoryPath -RepositoryRoot $repositoryRoot -Name 'governed-extraction'
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
$changedPath = 'FoodDiary.Application.Dashboard/FoodDiary.Application.Dashboard.csproj'
$compound = 'Dashboard is extracted and isolated, composition roots remain compatible, and existing behavior stays unchanged.'
try {
    & (Join-Path $PSScriptRoot 'Initialize-LlmWikiTaskWorkspace.ps1') `
        -Objective $objective `
        -Criterion $compound `
        -WorkspacePath $workspace `
        -ChangedPath $changedPath `
        -PlannedPath $changedPath | Out-Null
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiAcceptanceMatrix.ps1') map `
        -Path "$workspace/acceptance-matrix.json" `
        -CriterionId AC-001 `
        -ChangedPath $changedPath | Out-Null
    $resolveError = $null
    try {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiAcceptanceMatrix.ps1') resolve `
            -Path "$workspace/acceptance-matrix.json" `
            -CriterionId AC-001 `
            -AcceptanceStatus satisfied `
            -EvidenceNote 'Synthetic extraction proof.' | Out-Null
    } catch { $resolveError = $_.Exception.Message }
    if ($resolveError -notmatch 'compound' -or $resolveError -notmatch 'task-requirements-expand') {
        throw 'Acceptance resolve did not reject a compound criterion with an actionable expansion command.'
    }
    $matrixPath = Join-Path $absoluteWorkspace 'acceptance-matrix.json'
    $matrix = Get-Content -LiteralPath $matrixPath -Raw | ConvertFrom-Json
    $matrix.criteria[0].status = 'satisfied'
    $matrix.criteria[0].resolution.evidenceNote = 'Legacy workspace bypass fixture.'
    [IO.File]::WriteAllText($matrixPath, (($matrix | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $validation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAcceptanceMatrix.ps1') validate `
        -Path "$workspace/acceptance-matrix.json" `
        -EvidencePath "$workspace/evidence.json" `
        -Format Json | ConvertFrom-Json
    if ($validation.valid -or 'AC-001' -notin @($validation.nonAtomic)) { throw 'Acceptance validation approved a satisfied compound criterion.' }
    $proof = & (Join-Path $PSScriptRoot 'Manage-LlmWikiProofOfChange.ps1') assess -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    if ($proof.valid -or @($proof.proof.findings | Where-Object id -eq 'criterion-compound').Count -ne 1) {
        throw 'Proof of change did not reject the same compound criterion.'
    }

    $matrix = Get-Content -LiteralPath $matrixPath -Raw | ConvertFrom-Json
    $matrix.criteria[0].text = 'Dashboard source lives in the extracted application project.'
    $matrix.criteria[0].status = 'pending'
    $matrix.criteria[0].mapping.changedPaths = @()
    $matrix.criteria[0].resolution.evidenceNote = $null
    $matrix.availableEvidence | Add-Member -NotePropertyName renames -NotePropertyValue @(
        [pscustomobject]@{
            status = 'R100'
            from = $changedPath
            to = 'FoodDiary.Application.Dashboard.Renamed/FoodDiary.Application.Dashboard.csproj'
        }
    ) -Force
    [IO.File]::WriteAllText($matrixPath, (($matrix | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $renamedPath = 'FoodDiary.Application.Dashboard.Renamed/FoodDiary.Application.Dashboard.csproj'
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiAcceptanceMatrix.ps1') map `
        -Path "$workspace/acceptance-matrix.json" `
        -CriterionId AC-001 `
        -ChangedPath $renamedPath | Out-Null
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiAcceptanceMatrix.ps1') resolve `
        -Path "$workspace/acceptance-matrix.json" `
        -CriterionId AC-001 `
        -AcceptanceStatus satisfied `
        -EvidenceNote 'The Git rename preserves direct change provenance.' | Out-Null
    $renameProof = & (Join-Path $PSScriptRoot 'Manage-LlmWikiProofOfChange.ps1') assess -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    if (-not $renameProof.valid -or @($renameProof.proof.findings | Where-Object id -in @('missing-change-link', 'mapped-path-outside-diff')).Count -gt 0) {
        throw 'Proof of change did not recognize a mapped Git rename destination as direct change provenance.'
    }
    $newSiblingPath = 'FoodDiary.Application.Dashboard.Renamed/DependencyInjection.cs'
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiAcceptanceMatrix.ps1') map `
        -Path "$workspace/acceptance-matrix.json" `
        -CriterionId AC-001 `
        -ChangedPath $newSiblingPath | Out-Null
    $renamedMatrix = Get-Content -LiteralPath $matrixPath -Raw | ConvertFrom-Json
    if ($newSiblingPath -notin @($renamedMatrix.criteria[0].mapping.changedPaths)) {
        throw 'Acceptance mapping rejected a new file inside the proven rename destination directory.'
    }
} finally {
    if (Test-Path -LiteralPath $absoluteWorkspace) { Remove-Item -LiteralPath $absoluteWorkspace -Recurse -Force }
}

Write-Host 'LLM Wiki governed extraction regression passed: discovery, atomic criteria, and rename-aware proof remain aligned.'
