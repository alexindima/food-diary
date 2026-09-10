[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Test-LlmWikiModuleTestRoots.ps1')
& (Join-Path $PSScriptRoot 'Test-LlmWikiProjectLookup.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$tool = Join-Path $PSScriptRoot 'Get-LlmWikiTestPlan.ps1'
$scriptPlan = & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphTestPlan.ps1') -ProposedPath '.llm-wiki/tools/Write-LlmWikiContextEvaluationSnapshot.ps1' -Format Json | ConvertFrom-Json
if ('.llm-wiki/tools/Test-LlmWikiContextEvaluationSnapshot.ps1' -notin $scriptPlan.required) { throw 'Fast plan lost the PowerShell companion test.' }
$ciPlan = & $tool -ChangedPath '.github/workflows/ci-tests.yml' -Format Json | ConvertFrom-Json
if ('tests/FoodDiary.ArchitectureTests/BuildWorkflowGuardrailTests.cs' -notin $ciPlan.focusedTestFiles -or
    @($ciPlan.commands | Where-Object { $_.command -match 'FullyQualifiedName~BuildWorkflowGuardrailTests' -and $_.priority -eq 'required' }).Count -ne 1) {
    throw 'CI workflow changes must select their architecture guardrail and runnable command.'
}
$ciFastPlan = & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphTestPlan.ps1') -ChangedPath '.github/workflows/ci-tests.yml' -Format Json | ConvertFrom-Json
if ('tests/FoodDiary.ArchitectureTests/BuildWorkflowGuardrailTests.cs' -notin $ciFastPlan.required) {
    throw 'Fast CI test plan dropped the required workflow guardrail.'
}
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
    -ProposedPath 'Modules/Billing/Application/Services/BillingRenewalService.cs' `
    -NoBaseline `
    -Format Json | ConvertFrom-Json
if (@($idempotencyPlan.focusedTestFiles) -notcontains 'Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.RenewalAndAccessServiceTests.cs') {
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
    -ChangedPath 'Modules/Products/Infrastructure/Persistence/Products/ProductRepository.cs' `
    -Limit 15 `
    -Format Json | ConvertFrom-Json
$declaredTypeTest = @($repositoryPlan.focusedTestDetails | Where-Object {
    $_.path -eq 'tests/FoodDiary.Infrastructure.IntegrationTests/Integration/PersistenceRepositoryCoverageIntegrationTests.cs'
})
if ($declaredTypeTest.Count -ne 1 -or $declaredTypeTest[0].reason -ne 'references-changed-declared-type') {
    throw 'A test referencing the changed repository type was displaced by common member-name matches.'
}

$moduleTestProject = 'Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/FoodDiary.Modules.Fasting.Domain.Tests.csproj'
$moduleTestPlan = & $tool `
    -Intent 'Raise FoodDiary.Modules.Fasting.Domain statement coverage with module-owned contract tests' `
    -ProposedPath 'Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests' `
    -NoBaseline `
    -CompiledIndexSource Json `
    -Format Json | ConvertFrom-Json
if (@($moduleTestPlan.plannedTestProjects) -notcontains $moduleTestProject) {
    throw 'A planned module-owned test directory did not resolve its test project.'
}
$moduleTestCommand = @($moduleTestPlan.commands | Where-Object {
    $_.command -eq "dotnet test $moduleTestProject --no-restore"
})
if ($moduleTestCommand.Count -ne 1 -or $moduleTestCommand[0].priority -ne 'required' -or
    $moduleTestCommand[0].source -ne 'planned-test-project') {
    throw 'A planned module-owned test project was not selected as a required focused command.'
}
if (@($moduleTestPlan.focusedTestFiles | Where-Object {
    $_ -like 'Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/*.cs' -or
    $_ -like 'Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/*/*.cs'
}).Count -eq 0) {
    throw 'A planned module-owned test directory did not expose its C# tests as focused files.'
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

$databasePlan = & $tool `
    -Intent 'Исправить индексы PostgreSQL и ускорить первый запрос дашборда' `
    -NoBaseline `
    -Format Json | ConvertFrom-Json
if (@($databasePlan.focusedTestFiles).Count -eq 0 -or
    @($databasePlan.focusedTestFiles) -notcontains 'tests/FoodDiary.Infrastructure.IntegrationTests/Integration/QueryPlanIntegrationTests.cs') {
    throw 'Database/index intent produced an empty or non-provider-backed focused test plan.'
}
if (@($databasePlan.scenarios.id) -notcontains 'database-production-consumer' -or
    @($databasePlan.scenarios.id) -notcontains 'database-query-plan') {
    throw 'Database/index intent omitted production-consumer or query-plan validation.'
}
if (@($databasePlan.commands | Where-Object source -eq 'database-intent').Count -lt 2) {
    throw 'Database/index intent omitted EF model-sync or provider-backed verification commands.'
}

$databasePlanWithUnrelatedUiDiff = & $tool `
    -Intent 'Plan PostgreSQL indexes' `
    -NoBaseline `
    -DiffInput ([pscustomobject]@{
        changedPaths = @('FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/button/fd-ui-button.ts')
        scopes = @('frontend')
        focusedTests = @()
        recommendedChecks = @()
        modules = @()
    }) `
    -PolicyInput ([pscustomobject]@{ matchedRules = @(); requiredChecks = @(); reviewObligations = @() }) `
    -Format Json | ConvertFrom-Json
if (@($databasePlanWithUnrelatedUiDiff.focusedTestFiles) -notcontains 'tests/FoodDiary.Infrastructure.IntegrationTests/Integration/QueryPlanIntegrationTests.cs' -or
    @($databasePlanWithUnrelatedUiDiff.focusedTestFiles | Where-Object { $_ -like 'FoodDiary.Web.Client/*' }).Count -gt 0) {
    throw 'An unrelated UI diff displaced the intent-only PostgreSQL regression set.'
}

$sessionIntent = 'Управление активными пользовательскими сессиями и отзыв refresh-токенов'
$sessionJourney = & (Join-Path $PSScriptRoot 'Find-LlmWikiProductJourney.ps1') `
    -Query $sessionIntent `
    -Format Json | ConvertFrom-Json
if (@($sessionJourney.journeys.id) -notcontains 'FD-AUTH') {
    throw 'Identity-session intent did not select the authentication product journey.'
}

$sessionBrief = & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskBrief.ps1') `
    -Intent $sessionIntent `
    -CompiledIndexSource Json `
    -SkipQueryCache `
    -SkipTestPlan `
    -Compact `
    -Format Json | ConvertFrom-Json
$requiredSessionScopes = @('Backend', 'Api', 'Frontend', 'Database', 'Tests')
if (@($requiredSessionScopes | Where-Object { $_ -notin @($sessionBrief.change.scopes) }).Count -gt 0) {
    throw 'Identity-session brief omitted one or more cross-layer scopes.'
}
if (@($sessionBrief.change.directModules | Where-Object { $_ -in @('Fasting', 'Meals') }).Count -gt 0) {
    throw 'Identity-session brief leaked unrelated fasting or meal-session modules.'
}
if (@($sessionBrief.analysis.inferredPaths) -notcontains 'Modules/Identity/Infrastructure/Persistence/Users/RefreshTokenSessionRepository.cs' -or
    @($sessionBrief.analysis.inferredPaths) -notcontains 'FoodDiary.Web.Client/src/app/features/profile/pages/user-manage-sections/security-card/user-manage-security-card.ts') {
    throw 'Identity-session brief omitted the reviewed persistence or frontend route.'
}

$sessionPlan = & $tool `
    -Intent $sessionIntent `
    -NoBaseline `
    -CompiledIndexSource Json `
    -Limit 30 `
    -Format Json | ConvertFrom-Json
$requiredSessionScenarios = @(
    'session-user-scope'
    'session-current-preservation'
    'session-idempotent-revoke'
    'session-concurrent-refresh-revoke'
    'session-secret-minimization'
    'session-logout-server-revoke'
    'session-legacy-access-rollout'
)
if (@($requiredSessionScenarios | Where-Object { $_ -notin @($sessionPlan.scenarios.id) }).Count -gt 0) {
    throw 'Identity-session test plan omitted one or more security or rollout scenarios.'
}
if (@($sessionPlan.focusedTestFiles).Count -eq 0 -or
    @($sessionPlan.commands | Where-Object source -eq 'identity-session-intent').Count -lt 3) {
    throw 'Identity-session test plan omitted focused tests or cross-layer verification commands.'
}
$sessionApplicationProject = 'Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/FoodDiary.Modules.Identity.Application.Tests.csproj'
if (-not (Test-Path (Join-Path $repositoryRoot $sessionApplicationProject)) -or
    @($sessionPlan.commands | Where-Object { $_.id -eq 'session-application-tests' -and $_.command -like "dotnet test $sessionApplicationProject *" }).Count -ne 1) {
    throw 'Identity-session test plan must run the owning module application tests.'
}

$sessionPrivacy = & (Join-Path $PSScriptRoot 'Find-LlmWikiSensitiveData.ps1') `
    -Query 'refresh token session' `
    -CompiledIndexSource Json `
    -Format Json | ConvertFrom-Json
if (@($sessionPrivacy.handlingGuidance.persistedEvidence).Count -eq 0 -or
    @($sessionPrivacy.handlingGuidance.permissibleResponseMetadata).Count -eq 0 -or
    @($sessionPrivacy.handlingGuidance.prohibitedResponseOrTelemetry).Count -eq 0) {
    throw 'Session privacy guidance did not distinguish persisted, permissible, and prohibited data.'
}

Write-Host 'LLM Wiki identity-session routing passed: journey, scoped brief, test plan, and privacy guidance are grounded.'
