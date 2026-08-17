[CmdletBinding()]
param(
    [ValidateSet('All', 'Routing', 'Experience')]
    [string]$Group = 'All'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$failures = [Collections.Generic.List[string]]::new()
function Assert-Adaptive([bool]$Condition, [string]$Message) {
    if (-not $Condition) { $script:failures.Add($Message) }
}
function Get-AdaptiveIds([object[]]$Items) {
    @($Items | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id } } | Where-Object { $_ })
}

$visualPaths = @(
    'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result/ai-photo-preview/ai-photo-preview.html'
    'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result/ai-photo-result.scss'
)
if ($Group -in @('All', 'Routing')) {
$adaptiveWorkflowSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1'))
Assert-Adaptive ($adaptiveWorkflowSource -match 'SkipTestPlan\s*=\s*\$true') 'Adaptive routing rebuilt focused test plans that its classification does not consume.'
$tiny = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Improve photo annotation visibility with clearer SVG connectors and point styling.' `
    -ProposedPath $visualPaths `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($tiny.profile -eq 'visual-ui-change') 'Bounded visual work did not receive the visual UI route.'
Assert-Adaptive (-not $tiny.requiresDesign -and -not $tiny.requiresWorkspace) 'Visual UI work retained heavyweight design or workspace requirements.'
Assert-Adaptive (@(Get-AdaptiveIds $tiny.stages) -notcontains 'independent-review') 'Visual UI work retained critical independent review.'
Assert-Adaptive (@($tiny.stages | Where-Object { $_.id -eq 'visual-brief' -and $_.command -match 'brief' -and $_.command -match 'Compact' -and $_.completionEvidence -match 'UI-kit' }).Count -eq 1) 'Visual UI work did not start with a compact ownership and design-system brief.'
Assert-Adaptive (@($tiny.stages | Where-Object { $_.id -eq 'completion' -and $_.command -match 'verify-strict-affected' }).Count -eq 1) 'Visual UI work did not select strict affected verification as its final local gate.'
Assert-Adaptive (@($tiny.stages | Where-Object { $_.id -eq 'completion' -and $_.completionEvidence -match 'full repository verification remains the CI gate' }).Count -eq 1) 'Visual UI work did not distinguish scoped strict completion from full CI verification.'
Assert-Adaptive (@($tiny.stages | Where-Object { $_.id -eq 'focused-verification' -and $_.command -match 'test-plan' -and $_.command -match 'npm run build' }).Count -eq 1) 'Visual UI work did not combine focused tests and build into one verification stage.'
Assert-Adaptive (@($tiny.stages | Where-Object required).Count -eq 5) 'Visual UI work exceeded its five-stage ceremony budget.'
Assert-Adaptive (@(Get-AdaptiveIds $tiny.stages) -notcontains 'acceptance') 'Visual UI work retained a separate acceptance ceremony instead of using the compact brief.'
Assert-Adaptive (@(Get-AdaptiveIds $tiny.stages) -contains 'browser-evidence') 'Visual UI work omitted browser evidence.'
Assert-Adaptive (@($tiny.stages | Where-Object { $_.id -eq 'browser-evidence' -and $_.purpose -notmatch 'desktop and mobile' -and $_.completionEvidence -match 'omitted viewports' }).Count -eq 1) 'Visual UI work still required unconditional desktop and mobile evidence.'
Assert-Adaptive (@($tiny.stages | Where-Object { $_.id -eq 'browser-evidence' -and $_.command -match 'visual-qa' -and $_.command -match 'FixturePath' }).Count -eq 1) 'Visual UI work did not select automated file-upload browser QA.'
Assert-Adaptive ((@($tiny.stages | Where-Object id -eq 'browser-evidence')[0].order) -lt (@($tiny.stages | Where-Object id -eq 'completion')[0].order)) 'Visual UI work ran verify-fast before browser evidence.'
Assert-Adaptive ($tiny.ceremonyBudget.label -eq 'visual-focused') 'Visual UI work omitted its focused ceremony budget.'

$visualTiny = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Adjust the dashboard glass shape with CSS only.' `
    -ProposedPath 'FoodDiary.Web.Client/src/app/features/dashboard/components/hydration-card/hydration-card.scss' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($visualTiny.profile -eq 'visual-ui-change' -and $visualTiny.workflowVariant -eq 'visual-tiny') 'CSS-only work did not select the visual-tiny variant.'
Assert-Adaptive (@($visualTiny.stages | Where-Object { $_.id -eq 'visual-brief' -and $_.command -notmatch 'ui-trace' }).Count -eq 1) 'Visual-tiny work retraced an already grounded runtime owner.'
Assert-Adaptive (@($visualTiny.stages | Where-Object { $_.id -eq 'focused-verification' -and $_.command -match 'stylelint' -and $_.command -notmatch 'npm run build|test-plan' }).Count -eq 1) 'Visual-tiny work retained component-test or build ceremony during iteration.'

$localInteraction = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Add a 3 or 7 day period selector using local component state without changing API, routes, persistence, or public contracts.' `
    -ProposedPath @(
        'FoodDiary.Web.Client/src/app/features/dashboard/components/nutrition-weekly-trend-card/nutrition-weekly-trend-card.html',
        'FoodDiary.Web.Client/src/app/features/dashboard/components/nutrition-weekly-trend-card/nutrition-weekly-trend-card.ts',
        'FoodDiary.Web.Client/src/app/features/dashboard/components/nutrition-weekly-trend-card/nutrition-weekly-trend-card.spec.ts'
    ) `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($localInteraction.profile -eq 'visual-ui-change') 'Local interaction inside an existing frontend component was elevated to feature.'
Assert-Adaptive (@($localInteraction.stages | Where-Object { $_.id -eq 'journey-impact' -and $_.required }).Count -eq 0) 'Local component interaction retained required feature journey ceremony.'
Assert-Adaptive (@(Get-AdaptiveIds $localInteraction.stages) -notcontains 'design') 'Local component interaction retained feature design ceremony.'
Assert-Adaptive (@(Get-AdaptiveIds $localInteraction.stages) -contains 'focused-verification') 'Local component interaction omitted focused verification.'

$ungroundedInfrastructureBug = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix the cycle database read query because split-query loading is slow and duplicates related rows.' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($ungroundedInfrastructureBug.profile -eq 'scope-discovery') 'Ungrounded database query bug was elevated before its paths and boundary were confirmed.'
Assert-Adaptive (@(Get-AdaptiveIds $ungroundedInfrastructureBug.stages) -contains 'scope-research') 'Ungrounded database query bug omitted compact scope research.'
Assert-Adaptive (@(Get-AdaptiveIds $ungroundedInfrastructureBug.stages) -notcontains 'design') 'Ungrounded database query bug required premature design ceremony.'

$groundedInfrastructureBug = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix the cycle database read query because split-query loading is slow and duplicates related rows.' `
    -ProposedPath @(
        'FoodDiary.Infrastructure/Persistence/Tracking/CycleRepository.cs',
        'tests/FoodDiary.Infrastructure.IntegrationTests/Integration/PersistenceRepositoryCoverageIntegrationTests.cs'
    ) `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($groundedInfrastructureBug.profile -eq 'bug') 'Grounded data-query fix did not use the bounded bug route.'
Assert-Adaptive (@($groundedInfrastructureBug.stages | Where-Object id -eq 'bug-brief')[0].command -notmatch 'trace') 'Grounded repository query fix invoked handler trace despite explicit paths.'

$criticalIncident = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix an authentication bypass that allows unauthorized account access.' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($criticalIncident.profile -eq 'critical') 'Explicit authentication bypass was incorrectly downgraded to scope discovery.'
Assert-Adaptive ($criticalIncident.workflowLevel -eq 'governed') 'Critical work did not expose the governed workflow level.'

$dockerMaintenance = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix the frontend Docker build by copying dependency manifests before npm ci.' `
    -ProposedPath 'FoodDiary.Web.Client/Dockerfile' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($dockerMaintenance.profile -eq 'maintenance' -and $dockerMaintenance.maintenanceKind -eq 'deployment-build-fix') 'Bounded Docker build fix did not use deployment maintenance routing.'
Assert-Adaptive ((@(Get-AdaptiveIds @($dockerMaintenance.stages | Where-Object required)) -join ',') -eq 'evidence-brief,implementation,targeted-verification,completion') 'Docker maintenance retained non-focused ceremony.'
Assert-Adaptive (@($dockerMaintenance.stages.command | Where-Object { $_ -match 'trace|research|design' }).Count -eq 0) 'Docker maintenance invoked heuristic application-flow discovery.'

$dependencyMaintenance = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix Storybook peer dependency compatibility reported by npm install.' `
    -ProposedPath @('FoodDiary.Web.Client/package.json', 'FoodDiary.Web.Client/package-lock.json', 'FoodDiary.Web.Client/.npmrc') `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($dependencyMaintenance.profile -eq 'maintenance' -and $dependencyMaintenance.maintenanceKind -eq 'dependency-compatibility') 'Storybook compatibility fix did not use dependency maintenance routing.'
Assert-Adaptive (@($dependencyMaintenance.stages | Where-Object { $_.id -eq 'evidence-brief' -and $_.command -match 'dependencies' }).Count -eq 1) 'Dependency maintenance omitted manifest analysis.'

$ciMaintenance = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix MA0002 CI diagnostics by adding StringComparer.Ordinal at the reported call sites.' `
    -ProposedPath @('FoodDiary.Application/OpenFoodFacts/Services/OpenFoodFactsCachedProductSearch.cs', 'FoodDiary.Infrastructure/Persistence/OpenFoodFacts/OpenFoodFactsProductCacheRepository.cs') `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($ciMaintenance.profile -eq 'maintenance' -and $ciMaintenance.maintenanceKind -eq 'ci-fix') 'Path-grounded analyzer diagnostics did not use CI maintenance routing.'
Assert-Adaptive (-not $ciMaintenance.requiresDesign -and -not $ciMaintenance.requiresWorkspace) 'CI maintenance retained governed ceremony.'

$runtimeOwner = & (Join-Path $PSScriptRoot 'Get-LlmWikiFrontendRuntimeOwner.ps1') `
    -Query 'Improve AI photo annotation layout on the dashboard result.' `
    -CandidatePath 'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result/ai-photo-preview/ai-photo-preview.html' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($runtimeOwner.confidence -eq 'high' -and @($runtimeOwner.owners).Count -eq 1) 'UI runtime trace did not identify one explicit render owner.'
Assert-Adaptive ($runtimeOwner.owners[0].class -eq 'AiPhotoPreviewComponent') 'UI runtime trace selected the wrong component owner.'
Assert-Adaptive (@($runtimeOwner.owners[0].renderChain.renderedBy) -contains 'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result/ai-photo-result.html') 'UI runtime trace omitted the parent result template.'
$inferredRuntimeOwner = & (Join-Path $PSScriptRoot 'Get-LlmWikiFrontendRuntimeOwner.ps1') `
    -Query 'Move annotation labels outside the AI photo result on dashboard.' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (@($inferredRuntimeOwner.owners.class) -contains 'AiPhotoPreviewComponent') 'Query-only UI runtime trace omitted the rendered AI photo owner.'
Assert-Adaptive (@($inferredRuntimeOwner.owners.class) -notcontains 'DashboardWidgetHeaderComponent') 'Query-only UI runtime trace preferred a generic dashboard shell over AI photo owners.'

$metaVisual = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Improve visual UI routing without changing provider, privacy, security, contracts, or architecture.' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (-not $metaVisual.scopeKnown -and $metaVisual.profile -ne 'critical') 'Ungrounded visual intent was elevated by negated boundary vocabulary.'
Assert-Adaptive (@($metaVisual.inferred.paths).Count -eq 0) 'Frontend vocabulary alone inferred unrelated runtime owners.'

$ungroundedUiSurface = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix disabled buttons and rounded corners in the authentication dialog.' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($ungroundedUiSurface.profile -eq 'ui-discovery') 'Ungrounded local UI surface work was not routed to discovery-only.'
Assert-Adaptive (-not $ungroundedUiSurface.requiresWorkspace -and -not $ungroundedUiSurface.requiresDesign) 'UI discovery retained governed ceremony.'
Assert-Adaptive ((@(Get-AdaptiveIds $ungroundedUiSurface.stages) -join ',') -eq 'research,reclassify') 'UI discovery emitted implementation stages before grounding paths.'
Assert-Adaptive ($ungroundedUiSurface.stages[0].command -match 'ui-trace') 'UI discovery did not start with runtime-owner tracing.'

$ungroundedDashboardFeature = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Add a dashboard nutrition trend using real daily calorie and macro totals.' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($ungroundedDashboardFeature.profile -eq 'scope-discovery') 'Ungrounded cross-layer feature intent was classified before existing-flow research.'
Assert-Adaptive (-not $ungroundedDashboardFeature.requiresWorkspace -and -not $ungroundedDashboardFeature.requiresDesign) 'Scope discovery created feature or critical ceremony before grounding paths.'
Assert-Adaptive ((@(Get-AdaptiveIds $ungroundedDashboardFeature.stages) -join ',') -eq 'scope-research,reclassify') 'Scope discovery emitted implementation stages before reclassification.'
Assert-Adaptive ($ungroundedDashboardFeature.stages[0].command -match 'brief' -and $ungroundedDashboardFeature.stages[0].command -match 'research') 'Scope discovery omitted compact brief or existing-flow research.'

$dashboardFeaturePaths = @(
    'FoodDiary.Application/Dashboard/Models/DailyCaloriesModel.cs'
    'FoodDiary.Application/Dashboard/Services/DashboardStatisticsMapper.cs'
    'FoodDiary.Presentation.Api/Features/Dashboard/Responses/DailyCaloriesHttpResponse.cs'
    'FoodDiary.Web.Client/src/app/features/dashboard/models/dashboard.data.ts'
    'FoodDiary.Web.Client/src/app/features/dashboard/pages/dashboard.ts'
)
$groundedDashboardFeature = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Extend the existing dashboard response and UI with a nutrition trend using existing daily calorie and macro totals.' `
    -ProposedPath $dashboardFeaturePaths `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($groundedDashboardFeature.profile -eq 'feature') 'Grounded existing dashboard contract extension remained critical.'
Assert-Adaptive ($groundedDashboardFeature.workflowLevel -eq 'standard') 'Bounded feature work did not expose the standard workflow level.'
Assert-Adaptive (-not $groundedDashboardFeature.requiresWorkspace) 'Bounded single-module dashboard feature retained governed evidence workspace ceremony.'
Assert-Adaptive ($groundedDashboardFeature.requiresDesign) 'Grounded dashboard feature lost its normal feature design checkpoint.'

$dashboardLocalDayBugPaths = @(
    'FoodDiary.Application/Dashboard/Queries/GetDashboardSnapshot/GetDashboardSnapshotQuery.cs'
    'FoodDiary.Application/Dashboard/Services/DashboardSectionDataLoader.cs'
    'FoodDiary.Presentation.Api/Features/Dashboard/Requests/GetDashboardSnapshotHttpQuery.cs'
    'FoodDiary.Web.Client/src/app/features/dashboard/api/dashboard.service.ts'
    'FoodDiary.Web.Client/src/app/shell/sidebar/sidebar.facade.ts'
)
$dashboardLocalDayBug = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix dashboard meal aggregation using the local calendar day by adding an optional backward-compatible query parameter to the existing dashboard flow.' `
    -ProposedPath $dashboardLocalDayBugPaths `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($dashboardLocalDayBug.profile -eq 'bug') 'Bounded cross-layer Dashboard fix was elevated to feature.'
Assert-Adaptive (-not $dashboardLocalDayBug.requiresDesign -and -not $dashboardLocalDayBug.requiresWorkspace) 'Bounded cross-layer bug retained design or workspace ceremony.'
Assert-Adaptive ((@(Get-AdaptiveIds @($dashboardLocalDayBug.stages | Where-Object required)) -join ',') -eq 'bug-brief,implementation,focused-verification,completion') 'Bounded cross-layer bug did not receive the four-stage compact route.'
Assert-Adaptive (@($dashboardLocalDayBug.stages | Where-Object { $_.id -eq 'bug-brief' -and $_.command -match 'Compact' -and $_.command -match 'trace' }).Count -eq 1) 'Bounded cross-layer bug omitted compact root-cause tracing.'
Assert-Adaptive (@(Get-AdaptiveIds $dashboardLocalDayBug.stages) -notcontains 'journey-impact' -and @(Get-AdaptiveIds $dashboardLocalDayBug.stages) -notcontains 'design') 'Bounded cross-layer bug retained mandatory journey or design stages.'
Assert-Adaptive (@($dashboardLocalDayBug.stages | Where-Object { $_.id -eq 'completion' -and $_.command -match 'verify-fast' }).Count -eq 1) 'Bounded cross-layer bug did not finish with the fast local gate.'

$localReactiveBug = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix an Angular effect that accidentally tracks signal reads inside clearState and reopens the existing result.' `
    -ProposedPath @(
        'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-input-bar.ts',
        'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-input-bar.spec.ts'
    ) `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($localReactiveBug.profile -eq 'bug') 'Local reactive bug was not classified as a bounded bug.'
Assert-Adaptive (@($localReactiveBug.stages | Where-Object { $_.id -eq 'completion' -and $_.command -match 'verify-fast' }).Count -eq 1) 'Local reactive bug did not finish with the fast local gate.'
Assert-Adaptive (@($localReactiveBug.stages.command | Where-Object { $_ -match 'wiki\.ps1 verify$' }).Count -eq 0) 'Local reactive bug retained a mandatory local full Wiki verify.'

$migrationFeature = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Add a database migration to persist daily nutrition trend snapshots.' `
    -ProposedPath 'FoodDiary.Infrastructure/Persistence/Migrations/AddNutritionTrendSnapshots.cs' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($migrationFeature.profile -eq 'critical' -and $migrationFeature.requiresWorkspace) 'Explicit persistence migration was weakened by scope discovery.'

$criticalCoverageOnly = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Add coverage for OpenTelemetry, email outbox, and provider failure behavior without changing production code.' `
    -ProposedPath @(
        'tests/FoodDiary.Web.Api.Tests/Extensions/OpenTelemetryConfigurationTests.cs',
        'tests/FoodDiary.Infrastructure.Tests/Persistence/EmailOutboxTests.cs',
        'tests/FoodDiary.Infrastructure.Tests/Services/AiPromptProviderTests.cs'
    ) `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($criticalCoverageOnly.profile -eq 'test-only') 'Test-only coverage inherited critical risk from unchanged production code.'
Assert-Adaptive ($criticalCoverageOnly.workflowLevel -eq 'small') 'Test-only work did not expose the small workflow level.'
Assert-Adaptive (-not $criticalCoverageOnly.requiresDecisionCheckpoint -and -not $criticalCoverageOnly.requiresDesign -and -not $criticalCoverageOnly.requiresWorkspace) 'Test-only coverage retained governed ceremony.'
Assert-Adaptive ((@(Get-AdaptiveIds @($criticalCoverageOnly.stages | Where-Object required)) -join ',') -eq 'coverage-brief,test-implementation,focused-verification,completion') 'Test-only coverage did not use the four-stage focused route.'
Assert-Adaptive (@($criticalCoverageOnly.stages.command | Where-Object { $_ -match 'journeys|design|privacy|rollout|wiki\.ps1 verify$' }).Count -eq 0) 'Test-only coverage retained unrelated product or critical workflow commands.'

$testOnlyWithWikiBookkeeping = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Add billing and statistics coverage without changing production code.' `
    -ProposedPath @(
        'tests/FoodDiary.Application.Tests/Statistics/StatisticsTests.cs',
        'tests/FoodDiary.Infrastructure.Tests/Billing/BillingTests.cs',
        '.llm-wiki/generated/quality-index.json',
        '.llm-wiki/reviews/source-impact-reviews.json'
    ) `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($testOnlyWithWikiBookkeeping.profile -eq 'test-only') 'Derived Wiki bookkeeping prevented test-only routing.'
Assert-Adaptive (-not $testOnlyWithWikiBookkeeping.requiresWorkspace) 'Test-only work with derived Wiki files retained a governed workspace.'

$testInfrastructureChange = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Update the infrastructure test project dependencies.' `
    -ProposedPath 'tests/FoodDiary.Infrastructure.Tests/FoodDiary.Infrastructure.Tests.csproj' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($testInfrastructureChange.profile -ne 'test-only') 'A test project dependency change was incorrectly treated as test-source-only work.'

$patternExtension = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Port the existing weight-goal pattern to waist goals by following the same repository precedent.' `
    -ProposedPath @(
        'FoodDiary.Domain/Features/WaistGoals/WaistGoal.cs',
        'FoodDiary.Presentation.Api/Features/WaistGoals/WaistGoalsController.cs'
    ) `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($patternExtension.profile -eq 'pattern-extension') 'Grounded extension of an existing repository pattern was routed as design-from-scratch work.'
Assert-Adaptive (-not $patternExtension.requiresDesign -and -not $patternExtension.requiresWorkspace) 'Pattern extension retained governed design ceremony.'
Assert-Adaptive ((@(Get-AdaptiveIds @($patternExtension.stages | Where-Object required)) -join ',') -eq 'precedent-brief,compatibility-delta,implementation,focused-verification,completion') 'Pattern extension did not retain the precedent-focused five-stage route.'

$replanJourney = & (Join-Path $PSScriptRoot 'Find-LlmWikiProductJourney.ps1') `
    -Query 'Preserve acceptance evidence during delivery replan.' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (@(Get-AdaptiveIds $replanJourney.journeys) -notcontains 'FD-BILLING') 'Journey matching treated replan as the billing alias plan.'

$critical = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix Google authentication token linking for an existing account.' `
    -ProposedPath 'FoodDiary.Presentation.Api/Features/Auth/AuthSessionController.cs' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($critical.profile -eq 'critical') 'Authentication and credential work was not routed as critical.'
Assert-Adaptive ($critical.requiresDecisionCheckpoint -and $critical.requiresWorkspace) 'Critical work omitted checkpoint or governed workspace.'
Assert-Adaptive (@(Get-AdaptiveIds $critical.stages) -contains 'independent-review') 'Critical work omitted independent review.'
Assert-Adaptive (@(Get-AdaptiveIds $critical.stages) -contains 'requirements') 'Critical work omitted requirement quality assessment.'
Assert-Adaptive (@(Get-AdaptiveIds $critical.stages) -contains 'delivery-validation') 'Critical work omitted evidence-backed delivery validation.'
Assert-Adaptive (@($critical.stages | Where-Object { $_.id -eq 'independent-review' -and $_.command -match 'delivery-critique' }).Count -eq 1) 'Critical work did not route through adverse delivery critique.'

$journeys = & (Join-Path $PSScriptRoot 'Find-LlmWikiProductJourney.ps1') `
    -Query 'Fix the dietologist invitation email link' `
    -ChangedPath 'FoodDiary.Application/Dietologist/Services/DietologistEmailSender.cs' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (@(Get-AdaptiveIds $journeys.journeys) -contains 'FD-DIET') 'Journey impact omitted dietologist collaboration.'
Assert-Adaptive (@(Get-AdaptiveIds $journeys.journeys) -contains 'FD-MAIL') 'Journey impact omitted transactional email.'
Assert-Adaptive (@(Get-AdaptiveIds $journeys.journeys) -notcontains 'FD-MEAL') 'Journey impact produced a broad meal-tracking false positive.'

$ungrounded = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Fix quasar zephyr nimbus anomaly.' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (-not $ungrounded.scopeKnown -and $ungrounded.requiresPathDiscovery) 'Ungrounded intent silently absorbed working-tree paths.'
Assert-Adaptive ($ungrounded.confidence -eq 'low') 'Ungrounded intent did not expose low confidence.'
}
if ($Group -in @('All', 'Experience')) {
$workspaceReuseName = ".workspace-reuse-$([Guid]::NewGuid().ToString('N'))"
$workspaceReusePath = ".artifacts/llm-wiki/tasks/$workspaceReuseName"
$workspaceReuseAbsolute = Join-Path $repositoryRoot $workspaceReusePath
try {
    New-Item -ItemType Directory -Path $workspaceReuseAbsolute -Force | Out-Null
    [IO.File]::WriteAllText(
        (Join-Path $workspaceReuseAbsolute 'workspace.json'),
        (([pscustomobject]@{ objective = 'Original Dietologist objective' } | ConvertTo-Json) + [Environment]::NewLine),
        [Text.UTF8Encoding]::new($false))
    $reuseError = ''
    try {
        & (Join-Path $PSScriptRoot 'Start-LlmWikiDevelopment.ps1') -Objective 'New Notifications objective' -WorkspacePath $workspaceReusePath | Out-Null
    } catch { $reuseError = $_.Exception.Message }
    Assert-Adaptive ($reuseError -match 'belongs to a different objective') 'Start silently reused a governed workspace from another objective.'
} finally {
    Remove-Item -LiteralPath $workspaceReuseAbsolute -Recurse -Force -ErrorAction SilentlyContinue
}
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
Assert-Adaptive (@(Get-AdaptiveIds $research.researchLanes) -contains 'integrations') 'Research packet omitted the integrations investigation lane.'

$solutions = & (Join-Path $PSScriptRoot 'Get-LlmWikiSolutionComparison.ps1') `
    -Objective 'Improve the Wiki developer experience.' `
    -Option 'Extend the existing adaptive flow.','Replace it with a second workflow.' `
    -ProposedPath '.llm-wiki/tools/Get-LlmWikiAdaptiveWorkflow.ps1' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($solutions.alternatives.Count -eq 2 -and $solutions.recommendedOptionId -eq 'OPT-01') 'Solution comparison did not prefer the bounded existing-flow option.'
Assert-Adaptive ($solutions.schemaVersion -eq 2) 'Solution comparison did not expose the evidence-backed schema.'
Assert-Adaptive ($solutions.alternatives[0].evidenceCoverage.groundedPathCount -gt 0) 'Solution comparison omitted grounded current-source evidence.'
Assert-Adaptive (@($solutions.alternatives[1].rejectionConditions).Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($solutions.alternatives[1].decisionChangesWhen)) 'Solution comparison omitted rejection or decision-change criteria.'
Assert-Adaptive ($solutions.alternatives[1].recommendation -eq 'requires-boundary-evidence' -and $solutions.alternatives[1].evidenceCoverage.status -ne 'grounded') 'Structural alternative was treated as grounded without boundary evidence.'

$groundedStructuralSolutions = & (Join-Path $PSScriptRoot 'Get-LlmWikiSolutionComparison.ps1') `
    -Objective 'Replace a boundary that cannot preserve the required invariant.' `
    -Option 'Extend the existing boundary.','Introduce a separate subsystem.' `
    -ProposedPath '.llm-wiki/tools/Get-LlmWikiSolutionComparison.ps1' `
    -BoundaryEvidence '.llm-wiki/tools/Get-LlmWikiSolutionComparison.ps1: the current option model cannot represent the required boundary invariant.' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($groundedStructuralSolutions.alternatives[1].evidenceCoverage.status -eq 'grounded') 'Explicit structural boundary evidence did not close the evidence gap.'
Assert-Adaptive (@($groundedStructuralSolutions.alternatives[1].evidence | Where-Object kind -eq 'boundary-evidence').Count -eq 1) 'Structural comparison omitted caller-supplied boundary evidence.'

$architecturalDesign = & (Join-Path $PSScriptRoot 'Get-LlmWikiDesignCheckpoint.ps1') `
    -Objective 'Introduce a durable architecture boundary for Wiki planning.' `
    -ProposedPath '.llm-wiki/tools/Get-LlmWikiImplementationPlan.ps1' `
    -Decision 'Preserve existing phase contracts and add acceptance-oriented slices as an additive output.' `
    -Limit 4 `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($architecturalDesign.sliceStrategy.enabled -and $architecturalDesign.sliceStrategy.kind -eq 'vertical-outcome') 'Architectural design did not enable vertical outcome slices.'
Assert-Adaptive (@($architecturalDesign.designSlices).Count -eq 3) 'Architectural design did not produce the bounded three-slice decomposition.'
Assert-Adaptive (@(Get-AdaptiveIds $architecturalDesign.designSlices) -contains 'slice-minimum-behavior') 'Vertical decomposition omitted the minimum observable behavior slice.'

$bugDesign = & (Join-Path $PSScriptRoot 'Get-LlmWikiDesignCheckpoint.ps1') `
    -Objective 'Fix a local Wiki output formatting bug.' `
    -ProposedPath '.llm-wiki/tools/Get-LlmWikiSolutionComparison.ps1' `
    -Limit 4 `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (-not $bugDesign.sliceStrategy.enabled -and @($bugDesign.designSlices).Count -eq 0) 'Bounded bug design gained vertical-slice ceremony.'

$qa = & (Join-Path $PSScriptRoot 'Get-LlmWikiManualQaPlan.ps1') `
    -Objective 'Fix the dietologist invitation email link.' `
    -ProposedPath 'FoodDiary.Application/Dietologist/Services/DietologistEmailSender.cs' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (@($qa.journeys) -contains 'FD-DIET') 'Manual QA plan omitted the matched product journey.'
Assert-Adaptive (@(Get-AdaptiveIds $qa.cases) -contains 'QA-ERROR') 'Manual QA plan omitted generic negative coverage.'
Assert-Adaptive (@(Get-AdaptiveIds $qa.cases) -notcontains 'QA-MOBILE') 'Backend-only manual QA plan retained irrelevant frontend ceremony.'

$experience = & (Join-Path $PSScriptRoot 'Get-LlmWikiExperience.ps1') `
    -Action next `
    -WorkspacePath ".artifacts/llm-wiki/tasks/adaptive-experience-$([Guid]::NewGuid().ToString('N'))" `
    -Objective 'Improve photo annotation visibility with clearer SVG connectors.' `
    -ProposedPath $visualPaths `
    -Format Json | ConvertFrom-Json
Assert-Adaptive (-not [string]::IsNullOrWhiteSpace([string]$experience.nextAction)) 'Compact experience did not return one next action.'
Assert-Adaptive ($experience.ceremonyBudget.label -eq 'visual-focused') 'Compact experience omitted the routed ceremony budget.'

$metrics = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkflowMetrics.ps1') `
    -TasksPath '.artifacts/llm-wiki/no-such-task-root' `
    -Format Json | ConvertFrom-Json
Assert-Adaptive ($metrics.schemaVersion -eq 2 -and $metrics.workspaceCount -eq 0 -and $null -ne $metrics.adaptive) 'Workflow metrics did not handle an empty task history or expose adaptive runs.'

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
    Assert-Adaptive (@(Get-AdaptiveIds $delivery.assessment.gates) -contains 'requirements') 'Delivery status omitted the requirement gate.'
    Assert-Adaptive (@(Get-AdaptiveIds $delivery.assessment.gates) -contains 'proof-of-change') 'Delivery status omitted proof-of-change.'
    Assert-Adaptive (@(Get-AdaptiveIds $delivery.assessment.journeyImpact) -contains 'FD-DIET') 'Delivery status omitted journey impact.'
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
}

if ($failures.Count -gt 0) {
    Write-Host "Adaptive workflow smoke failed with $($failures.Count) error(s):"
    foreach ($failure in $failures) { Write-Host " - $failure" }
    exit 1
}
Write-Host "Adaptive workflow smoke passed ($Group): routing, ceremony budgets, compact next action, solutions, QA journeys, delivery gates, controlled replanning, research, precedents, and pause/resume continuity."
