[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string[]]$ProposedPath,
    [object]$DiffInput,
    [object]$PolicyInput,
    [object]$OwnershipInput,
    [object]$TestPlanInput,
    [object]$RolloutInput,
    [object]$DecisionInput,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 20)]
    [int]$Limit = 8
)

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot

$common = @{ BaseRef = $BaseRef; Format = 'Json' }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
$effectivePaths = @(
    @($ChangedPath) + @($ProposedPath) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique
)
if ($effectivePaths.Count -gt 0) { $common.ChangedPath = $effectivePaths }

$diffArguments = @{} + $common
$diffArguments.Limit = $Limit
$diff = if ($null -ne $DiffInput) { $DiffInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiDiffContext.ps1') @diffArguments | ConvertFrom-Json
}
$policy = if ($null -ne $PolicyInput) { $PolicyInput } else {
    & (Join-Path $toolsRoot 'Test-LlmWikiChangePolicy.ps1') @common | ConvertFrom-Json
}
$ownership = if ($null -ne $OwnershipInput) { $OwnershipInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiOwnershipImpact.ps1') @common -DiffInput $diff | ConvertFrom-Json
}
$testPlanArguments = @{} + $common
$testPlanArguments.Limit = $Limit
$testPlan = if ($null -ne $TestPlanInput) { $TestPlanInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiTestPlan.ps1') @testPlanArguments -DiffInput $diff -PolicyInput $policy | ConvertFrom-Json
}
$rollout = if ($null -ne $RolloutInput) { $RolloutInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiRolloutPlan.ps1') @common -DiffInput $diff -PolicyInput $policy | ConvertFrom-Json
}
$decision = if ($null -ne $DecisionInput) { $DecisionInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiDecisionContext.ps1') @common -DiffInput $diff -PolicyInput $policy | ConvertFrom-Json
}
$qualityIndexPath = Join-Path (Split-Path -Parent $toolsRoot) 'generated/quality-index.json'
$qualityIndex = Get-Content -LiteralPath $qualityIndexPath -Raw | ConvertFrom-Json
$runtimeTopologyPath = Join-Path (Split-Path -Parent $toolsRoot) 'generated/runtime-topology.json'
$runtimeTopology = Get-Content -LiteralPath $runtimeTopologyPath -Raw | ConvertFrom-Json
$sensitiveDataPath = Join-Path (Split-Path -Parent $toolsRoot) 'generated/sensitive-data-index.json'
$sensitiveData = Get-Content -LiteralPath $sensitiveDataPath -Raw | ConvertFrom-Json
$frontendContractPath = Join-Path (Split-Path -Parent $toolsRoot) 'generated/frontend-contract-index.json'
$frontendContract = Get-Content -LiteralPath $frontendContractPath -Raw | ConvertFrom-Json
$domainDataPath = Join-Path (Split-Path -Parent $toolsRoot) 'generated/domain-data-index.json'
$domainData = Get-Content -LiteralPath $domainDataPath -Raw | ConvertFrom-Json
$backendContractPath = Join-Path (Split-Path -Parent $toolsRoot) 'generated/backend-contract-index.json'
$backendContract = Get-Content -LiteralPath $backendContractPath -Raw | ConvertFrom-Json
$architectureHealthPath = Join-Path (Split-Path -Parent $toolsRoot) 'generated/architecture-health-index.json'
$architectureHealth = Get-Content -LiteralPath $architectureHealthPath -Raw | ConvertFrom-Json
$changedPathsForQuality = @($diff.changedPaths)
$changedQualityFiles = @($qualityIndex.files | Where-Object { $changedPathsForQuality -contains $_.path })
$changedTestGaps = @(
    $qualityIndex.criticalSymbols |
        Where-Object { $_.testReferenceCount -eq 0 -and $changedPathsForQuality -contains $_.path }
)
$composeImpact = @()
if ($changedPathsForQuality -contains 'docker-compose.yml') {
    $composeImpact = @($runtimeTopology.composeServices)
}
$runtimeImpact = [ordered]@{
    hostedServices = @($runtimeTopology.hostedServices | Where-Object { $changedPathsForQuality -contains $_.path })
    httpClients = @($runtimeTopology.httpClients | Where-Object { $changedPathsForQuality -contains $_.registrationPath })
    webhooks = @($runtimeTopology.webhooks | Where-Object { $changedPathsForQuality -contains $_.path })
    recurringJobs = @($runtimeTopology.recurringJobRegistrations | Where-Object { $changedPathsForQuality -contains $_.path })
    composeServices = $composeImpact
}
$runtimeImpactCount = @(
    $runtimeImpact.hostedServices +
    $runtimeImpact.httpClients +
    $runtimeImpact.webhooks +
    $runtimeImpact.recurringJobs +
    $runtimeImpact.composeServices
).Count
$privacyImpact = [ordered]@{
    fields = @($sensitiveData.fields | Where-Object { $changedPathsForQuality -contains $_.path })
    boundaries = @($sensitiveData.boundaryFiles | Where-Object { $changedPathsForQuality -contains $_.path })
    potentialLogging = @($sensitiveData.potentialLogging | Where-Object { $changedPathsForQuality -contains $_.path })
}
$frontendContractImpact = [ordered]@{
    components = @($frontendContract.components | Where-Object {
        $changedPathsForQuality -contains $_.path -or
        ($null -ne $_.templatePath -and $changedPathsForQuality -contains $_.templatePath)
    })
    apiCalls = @($frontendContract.apiCalls | Where-Object { $changedPathsForQuality -contains $_.path })
    translations = @($frontendContract.translationUsage | Where-Object { $changedPathsForQuality -contains $_.path })
    downstreamConsumers = @($frontendContract.consumerEdges | Where-Object {
        $changedPathsForQuality -contains $_.componentPath
    })
    changedConsumers = @($frontendContract.consumerEdges | Where-Object {
        $changedPathsForQuality -contains $_.consumerPath
    })
}
$domainDataImpact = [ordered]@{
    types = @($domainData.domainTypes | Where-Object { $changedPathsForQuality -contains $_.path })
    invariants = @($domainData.invariants | Where-Object { $changedPathsForQuality -contains $_.path })
    mappings = @($domainData.persistenceMappings | Where-Object { $changedPathsForQuality -contains $_.path })
}
$changedBackendContracts = @($backendContract.contracts | Where-Object {
    @($_.definitionPaths | Where-Object { $changedPathsForQuality -contains $_ }).Count -gt 0
})
$changedBackendContractNames = @($changedBackendContracts.name)
$backendContractImpact = [ordered]@{
    contracts = $changedBackendContracts
    productionConsumers = @($backendContract.consumerEdges | Where-Object {
        $_.contract -in $changedBackendContractNames -and -not $_.isTest
    })
    testConsumers = @($backendContract.consumerEdges | Where-Object {
        $_.contract -in $changedBackendContractNames -and $_.isTest
    })
}
$architectureHealthImpact = [ordered]@{
    dependencyViolations = @($architectureHealth.projectDependencyViolations)
    untrackedProductionProjects = @($architectureHealth.untrackedProductionProjects)
    moduleCycleNodes = @($architectureHealth.moduleCycleNodes)
    selectorUnreferencedComponents = @($architectureHealth.selectorUnreferencedComponents | Where-Object {
        $changedPathsForQuality -contains $_.path -or
        ($null -ne $_.templatePath -and $changedPathsForQuality -contains $_.templatePath)
    })
    componentsWithoutDirectSpecs = @($architectureHealth.componentsWithoutDirectSpecs | Where-Object {
        $changedPathsForQuality -contains $_.path
    })
    criticalSymbolsWithoutTestReferences = @($architectureHealth.criticalSymbolsWithoutTestReferences | Where-Object {
        $changedPathsForQuality -contains $_.path
    })
    debtMarkers = @($architectureHealth.explicitDebtMarkers | Where-Object {
        $changedPathsForQuality -contains $_.path
    })
}
Write-Verbose "Quality match: changed=$(@($diff.changedPaths).Count), indexed=$(@($qualityIndex.files).Count), matched=$($changedQualityFiles.Count)."

$riskScore = 0
$riskReasons = [System.Collections.Generic.List[string]]::new()
if (@($diff.scopes) -contains 'Api') { $riskScore += 2; $riskReasons.Add('public API surface') }
if (@($diff.scopes) -contains 'Database') { $riskScore += 3; $riskReasons.Add('database or migration') }
if (@($diff.scopes) -contains 'Localization') { $riskScore += 1; $riskReasons.Add('paired localization') }
if (@($policy.matchedRules.id) -contains 'security-sensitive') { $riskScore += 3; $riskReasons.Add('security-sensitive flow') }
if (@($policy.matchedRules.id) -contains 'performance-data-access') { $riskScore += 2; $riskReasons.Add('query or persistence shape') }
if (@($policy.matchedRules.id) -contains 'architecture-decision') { $riskScore += 1; $riskReasons.Add('durable architecture decision candidate') }
if (@($policy.matchedRules.id) -contains 'observability-critical-flow') { $riskScore += 1; $riskReasons.Add('critical-flow telemetry') }
if (@($policy.matchedRules.id) -contains 'privacy-data-lifecycle') { $riskScore += 2; $riskReasons.Add('privacy data lifecycle') }
if (@($policy.matchedRules.id) -contains 'dependency-nuget' -or @($policy.matchedRules.id) -contains 'dependency-npm') {
    $riskScore += 2
    $riskReasons.Add('dependency graph change')
}
if (@($diff.scopes) -contains 'Configuration') { $riskScore += 2; $riskReasons.Add('configuration contract') }
if (@($diff.scopes) -contains 'Deployment') { $riskScore += 3; $riskReasons.Add('deployment workflow') }
if (@($changedQualityFiles | Where-Object structuralRiskScore -ge 75).Count -gt 0) {
    $riskScore += 3
    $riskReasons.Add('high structural hotspot')
}
if ($changedTestGaps.Count -gt 0) {
    $riskScore += 1
    $riskReasons.Add('critical symbol without direct test reference')
}
if ($runtimeImpactCount -gt 0) {
    $riskScore += 2
    $riskReasons.Add('runtime or integration topology impact')
}
if (@($privacyImpact.fields).Count -gt 0) {
    $riskScore += 2
    $riskReasons.Add('candidate sensitive-data lifecycle impact')
}
if (@($privacyImpact.fields | Where-Object category -eq 'credential').Count -gt 0 -or
    @($privacyImpact.potentialLogging).Count -gt 0) {
    $riskScore += 2
    $riskReasons.Add('credential or sensitive logging review')
}
if (@($frontendContractImpact.components).Count -gt 0) {
    $riskScore += 2
    $riskReasons.Add('frontend public component contract')
}
if (@($frontendContractImpact.downstreamConsumers).Count -ge 10) {
    $riskScore += 2
    $riskReasons.Add('broad frontend consumer blast radius')
}
elseif (@($frontendContractImpact.downstreamConsumers).Count -gt 0) {
    $riskScore += 1
    $riskReasons.Add('downstream frontend consumers')
}
elseif (@($frontendContractImpact.apiCalls).Count -gt 0) {
    $riskScore += 1
    $riskReasons.Add('frontend API call contract')
}
if (@($domainDataImpact.types).Count -gt 0) {
    $riskScore += 2
    $riskReasons.Add('domain invariant or state transition')
}
if (@($domainDataImpact.mappings).Count -gt 0) {
    $riskScore += 2
    $riskReasons.Add('persistence model contract')
}
if (@($backendContractImpact.contracts).Count -gt 0) {
    $riskScore += 2
    $riskReasons.Add('backend public or application contract')
}
if (@($backendContractImpact.productionConsumers).Count -ge 20) {
    $riskScore += 2
    $riskReasons.Add('broad backend consumer blast radius')
}
elseif (@($backendContractImpact.productionConsumers).Count -gt 0) {
    $riskScore += 1
    $riskReasons.Add('downstream backend consumers')
}
if (@($architectureHealthImpact.dependencyViolations).Count -gt 0 -or
    @($architectureHealthImpact.untrackedProductionProjects).Count -gt 0 -or
    @($architectureHealthImpact.moduleCycleNodes).Count -gt 0) {
    $riskScore += 4
    $riskReasons.Add('enforced architecture drift')
}
if (@($architectureHealthImpact.selectorUnreferencedComponents).Count -gt 0) {
    $riskScore += 1
    $riskReasons.Add('frontend selector without static template consumer')
}
if (@($diff.scopes) -contains 'Api' -and @($policy.matchedRules.id) -contains 'security-sensitive') {
    $riskScore += 1
    $riskReasons.Add('internet-exposed sensitive flow')
}
if (@($ownership.downstreamModules).Count -ge 10) { $riskScore += 2; $riskReasons.Add('broad downstream module impact') }
elseif (@($ownership.downstreamModules).Count -gt 0) { $riskScore += 1; $riskReasons.Add('downstream module impact') }
$policyViolations = @($policy.violations | Where-Object { $null -ne $_ })
if ($policyViolations.Count -gt 0) { $riskScore += 4; $riskReasons.Add('structural policy violation') }
$riskLevel = if ($riskScore -ge 7) { 'high' } elseif ($riskScore -ge 3) { 'medium' } else { 'low' }

$brief = [pscustomobject]@{
    risk = [pscustomobject]@{
        level = $riskLevel
        score = $riskScore
        reasons = @($riskReasons)
    }
    change = [pscustomobject]@{
        paths = @($diff.changedPaths)
        proposedPaths = @($ProposedPath)
        scopes = @($diff.scopes)
        directModules = @($ownership.directModules)
        downstreamModules = @($ownership.downstreamModules)
    }
    instructions = @($ownership.ownershipGuides | Select-Object -ExpandProperty guide -Unique)
    contextPages = @($diff.wikiPages.path)
    focusedTests = @($testPlan.focusedTestFiles)
    testScenarios = @($testPlan.scenarios)
    requiredChecks = @($policy.requiredChecks)
    reviewObligations = @($policy.reviewObligations)
    structuralViolations = $policyViolations
    generatedActions = @($diff.generatedActions)
    rolloutFlags = $rollout.flags
    rolloutPlan = $rollout
    decisionContext = $decision
    quality = [pscustomobject]@{
        changedFiles = $changedQualityFiles
        changedTestGaps = $changedTestGaps
    }
    runtimeImpact = [pscustomobject]$runtimeImpact
    privacyImpact = [pscustomobject]$privacyImpact
    frontendContractImpact = [pscustomobject]$frontendContractImpact
    domainDataImpact = [pscustomobject]$domainDataImpact
    backendContractImpact = [pscustomobject]$backendContractImpact
    architectureHealthImpact = [pscustomobject]$architectureHealthImpact
    warnings = @($diff.warnings)
}

if ($Format -eq 'Json') {
    $brief | ConvertTo-Json -Depth 9
    exit 0
}

Write-Host "Task brief: $($brief.risk.level) risk (score $($brief.risk.score))"
if ($brief.risk.reasons.Count -gt 0) { Write-Host "Risk factors: $($brief.risk.reasons -join ', ')" }
Write-Host "Scopes: $($brief.change.scopes -join ', ')"
Write-Host "Direct modules: $($brief.change.directModules -join ', ')"
Write-Host "Downstream modules: $($brief.change.downstreamModules -join ', ')"
Write-Host ''
Write-Host 'Read first:'
foreach ($path in @($brief.instructions + $brief.contextPages | Select-Object -Unique)) { Write-Host " - $path" }
Write-Host ''
Write-Host 'Focused tests:'
foreach ($path in @($brief.focusedTests | Select-Object -First $Limit)) { Write-Host " - $path" }
Write-Host ''
Write-Host 'Test scenarios:'
foreach ($scenario in $brief.testScenarios) { Write-Host " - $($scenario.id): $($scenario.description)" }
Write-Host ''
Write-Host 'Required checks:'
foreach ($check in $brief.requiredChecks) { Write-Host " - $($check.id): $($check.command)" }
Write-Host ''
Write-Host 'Review obligations:'
foreach ($review in $brief.reviewObligations) { Write-Host " - $($review.id): $($review.description)" }
foreach ($violation in $brief.structuralViolations) { Write-Host " - VIOLATION [$($violation.ruleId)]: $($violation.message)" }
foreach ($warning in $brief.warnings) { Write-Host " - Warning: $warning" }
