[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$tool = Join-Path $PSScriptRoot 'Get-LlmWikiTestPlan.ps1'
$changedPaths = @(
    'FoodDiary.Application.Cycles/Commands/UpdateMenstrualEpisode/UpdateMenstrualEpisodeCommand.cs'
    'FoodDiary.Domain/Entities/Tracking/MenstrualEpisode.cs'
    'FoodDiary.Presentation.Api/Features/Cycles/Requests/UpdateMenstrualEpisodeHttpRequest.cs'
    'FoodDiary.Web.Client/src/app/features/cycle-tracking/api/cycles.service.ts'
)
$plan = & (Join-Path $PSScriptRoot 'Get-LlmWikiTestPlan.ps1') `
    -Intent 'Edit menstrual episode history' `
    -ChangedPath $changedPaths `
    -Format Json | ConvertFrom-Json

$ids = @($plan.commands | ForEach-Object { [string]$_.id })
if ($ids.Count -ne @($ids | Sort-Object -Unique).Count) {
    throw "Test-plan command IDs are not unique: $($ids -join ', ')."
}
$unrelated = @($plan.focusedTestFiles | Where-Object {
    $_ -match '/features/(?:dashboard|dietologist)/'
})
if ($unrelated.Count -gt 0) {
    throw "Unrelated cross-feature frontend tests leaked into the cycle plan: $($unrelated -join ', ')."
}
$cycleTests = @($plan.focusedTestFiles | Where-Object { $_ -match '(?i)cycle|menstrual' })
if ($cycleTests.Count -eq 0) {
    throw 'Cycle test-plan precision regression found no cycle-focused tests.'
}

$coveredPlan = & (Join-Path $PSScriptRoot 'Get-LlmWikiTestPlan.ps1') `
    -Intent 'Edit menstrual episode history' `
    -ChangedPath $changedPaths `
    -ExecutedCheck 'cd FoodDiary.Web.Client && npm run verify' `
    -Format Json | ConvertFrom-Json
$focusedFrontend = @($coveredPlan.commands | Where-Object id -eq 'focused-frontend')[0]
if ($null -eq $focusedFrontend -or $focusedFrontend.status -ne 'satisfied' -or
    $focusedFrontend.receipt.source -ne 'executedChecksCoverage') {
    throw 'Full frontend verification did not satisfy the focused frontend test command.'
}

Write-Host "LLM Wiki test-plan precision passed: $($ids.Count) unique command IDs, $($cycleTests.Count) cycle-focused tests."

$idempotencyPlan = & $tool `
    -Intent 'Harden billing renewal idempotency and retry behavior' `
    -ProposedPath 'FoodDiary.Application.Billing/Services/BillingRenewalService.cs' `
    -NoBaseline `
    -Format Json | ConvertFrom-Json
if (@($idempotencyPlan.focusedTestFiles) -notcontains 'tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.RenewalAndAccessServiceTests.cs') {
    throw 'Planned idempotency work did not select the symbol-adjacent renewal tests.'
}
$rootPlan = & $tool `
    -Intent 'Replace fixed repository root parent traversal' `
    -ProposedPath '.llm-wiki/tools/Invoke-LlmWikiParallelSmoke.ps1' `
    -NoBaseline `
    -Format Json | ConvertFrom-Json
if (@($rootPlan.repositoryAntipatterns | Where-Object id -eq 'fixed-parent-repository-root').Count -lt 2) {
    throw 'Test plan did not discover repeated fixed-depth repository-root traversal.'
}
$repositoryPlan = & $tool `
    -ChangedPath 'FoodDiary.Infrastructure/Persistence/Products/ProductRepository.cs' `
    -Limit 15 `
    -Format Json | ConvertFrom-Json
$declaredTypeTest = @($repositoryPlan.focusedTestDetails | Where-Object {
    $_.path -eq 'tests/FoodDiary.Infrastructure.IntegrationTests/Integration/PersistenceRepositoryCoverageIntegrationTests.cs'
})
if ($declaredTypeTest.Count -ne 1 -or $declaredTypeTest[0].reason -ne 'references-changed-declared-type') {
    throw 'A test referencing the changed repository type was displaced by common member-name matches.'
}
Write-Host 'LLM Wiki test-plan semantic selection passed: planned symbols, idempotency tests, neighboring tests, and repeated antipatterns are visible.'

$assessmentPlan = & $tool `
    -Intent 'Независимый аудит всего проекта на уязвимости и проблемы.' `
    -NoBaseline `
    -Limit 12 `
    -Format Json | ConvertFrom-Json
if ($assessmentPlan.selectionMode -ne 'repository-assessment') {
    throw 'Repository audit test plan did not activate assessment selection.'
}
if (@($assessmentPlan.scenarios.id | Where-Object { $_ -like 'assessment-*' }).Count -lt 9 -or
    @($assessmentPlan.scenarios.id) -notcontains 'assessment-webhook-authenticity' -or
    @($assessmentPlan.scenarios.id) -notcontains 'assessment-migration-safety' -or
    @($assessmentPlan.scenarios.id) -notcontains 'assessment-deployment-supply-chain' -or
    @($assessmentPlan.scenarios.id) -notcontains 'assessment-dependency-inventory') {
    throw 'Repository audit test plan omitted one or more risk-lane scenarios.'
}
if (@($assessmentPlan.focusedTestFiles | Where-Object { $_ -match 'RedisIdempotencyConcurrency|SideEffectReliability|auth\.service\.spec' }).Count -lt 3) {
    throw 'Repository audit test plan omitted representative concurrency, reliability, or frontend tests.'
}
if (@($assessmentPlan.commands | Where-Object source -eq 'repository-assessment').Count -lt 3) {
    throw 'Repository audit test plan omitted repository-wide verification commands.'
}
