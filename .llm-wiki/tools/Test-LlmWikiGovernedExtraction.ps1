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
