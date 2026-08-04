[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [Alias('Intent')]
    [string]$Objective,
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [Alias('PlannedPath')]
    [string[]]$ProposedPath,
    [object]$WorkflowInput,
    [object]$ResearchInput,
    [object]$BriefInput,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12
)

$ErrorActionPreference = 'Stop'
$common = @{ Objective = $Objective; BaseRef = $BaseRef; Format = 'Json'; Limit = $Limit }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $common.ChangedPath = $ChangedPath }
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $common.ProposedPath = $ProposedPath }

$workflow = if ($null -ne $WorkflowInput) { $WorkflowInput } else {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') @common | ConvertFrom-Json
}
$research = if ($null -ne $ResearchInput) { $ResearchInput } else {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1') @common | ConvertFrom-Json
}
$briefArguments = @{ Intent = $Objective; BaseRef = $BaseRef; Format = 'Json'; Limit = [Math]::Min($Limit, 20) }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $briefArguments.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $briefArguments.ChangedPath = $ChangedPath }
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $briefArguments.ProposedPath = $ProposedPath }
$brief = if ($null -ne $BriefInput) { $BriefInput } else {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskBrief.ps1') @briefArguments | ConvertFrom-Json
}

$scopes = @($brief.change.scopes | Where-Object { $_ } | Sort-Object -Unique)
$runtimeItems = @(
    @($brief.runtimeImpact.hostedServices) +
    @($brief.runtimeImpact.httpClients) +
    @($brief.runtimeImpact.webhooks) +
    @($brief.runtimeImpact.recurringJobs) +
    @($brief.runtimeImpact.composeServices) |
        Where-Object { $null -ne $_ }
)
$externalBoundaries = @(
    @($brief.runtimeImpact.httpClients) +
    @($brief.runtimeImpact.webhooks) +
    @($brief.privacyImpact.externalTransfers) +
    @($research.boundaries.runtime) |
        Where-Object { $null -ne $_ } |
        Select-Object -First $Limit
)
$inboundConsumers = @(
    @($brief.backendContractImpact.productionConsumers | ForEach-Object {
        [pscustomobject][ordered]@{ kind = 'backend-contract-consumer'; path = $_.consumerPath; contract = $_.contract; evidence = 'backend-contract-index' }
    })
    @($brief.frontendContractImpact.downstreamConsumers | ForEach-Object {
        [pscustomobject][ordered]@{ kind = 'frontend-component-consumer'; path = $_.consumerPath; contract = $_.selector; evidence = 'frontend-contract-index' }
    })
    @($brief.frontendContractImpact.changedConsumers | ForEach-Object {
        [pscustomobject][ordered]@{ kind = 'changed-frontend-consumer'; path = $_.consumerPath; contract = $_.selector; evidence = 'frontend-contract-index' }
    })
) | Where-Object { $_.path } | Sort-Object kind, path -Unique | Select-Object -First $Limit

$outboundDependencies = @(
    @($brief.frontendContractImpact.apiCalls | ForEach-Object {
        [pscustomobject][ordered]@{ kind = 'frontend-api-call'; path = $_.path; target = $_.url; evidence = 'frontend-contract-index' }
    })
    @($research.discovery.dependencyInjection | ForEach-Object {
        [pscustomobject][ordered]@{ kind = 'dependency-injection'; path = $_.path; target = $_.service; evidence = 'compiled-context' }
    })
    @($brief.change.downstreamModules | ForEach-Object {
        [pscustomobject][ordered]@{ kind = 'downstream-module'; path = $null; target = $_; evidence = 'module-dependency-graph' }
    })
) | Where-Object { $_.path -or $_.target } | Sort-Object kind, path, target -Unique | Select-Object -First $Limit

$sideEffects = @(
    @($brief.runtimeImpact.webhooks | ForEach-Object { [pscustomobject][ordered]@{ kind = 'webhook'; evidence = $_ } })
    @($brief.runtimeImpact.recurringJobs | ForEach-Object { [pscustomobject][ordered]@{ kind = 'recurring-job'; evidence = $_ } })
    @($brief.runtimeImpact.hostedServices | ForEach-Object { [pscustomobject][ordered]@{ kind = 'hosted-service'; evidence = $_ } })
    @($brief.privacyImpact.potentialLogging | ForEach-Object { [pscustomobject][ordered]@{ kind = 'logging'; evidence = $_ } })
) | Select-Object -First $Limit

$asyncContinuations = @(
    @($brief.runtimeImpact.recurringJobs) +
    @($brief.runtimeImpact.hostedServices) +
    @($brief.runtimeImpact.webhooks) |
        Where-Object { $null -ne $_ } |
        Select-Object -First $Limit
)
$profile = [string]$workflow.profile
$isCrossLayer = $scopes.Count -ge 2
$hasContractBoundary = @($brief.backendContractImpact.contracts).Count -gt 0 -or @($brief.frontendContractImpact.apiCalls).Count -gt 0
$recommended = $profile -in @('critical', 'architectural') -or
    $scopes -contains 'Api' -or
    $isCrossLayer -or
    $runtimeItems.Count -gt 0 -or
    @($brief.privacyImpact.externalTransfers).Count -gt 0 -or
    $hasContractBoundary
$reasons = @(
    if ($profile -in @('critical', 'architectural')) { "$profile workflow" }
    if ($scopes -contains 'Api') { 'API scope' }
    if ($isCrossLayer) { "cross-layer scope: $($scopes -join ', ')" }
    if ($runtimeItems.Count -gt 0) { 'runtime or asynchronous boundary evidence' }
    if (@($brief.privacyImpact.externalTransfers).Count -gt 0) { 'external data transfer evidence' }
    if ($hasContractBoundary) { 'consumer-visible contract evidence' }
)
$gaps = @(
    if (@($inboundConsumers).Count -eq 0) { 'No indexed inbound consumer was found; verify dynamic and external callers when the contract is public.' }
    if (@($outboundDependencies).Count -eq 0) { 'No indexed outbound dependency was found; verify runtime calls in current source.' }
    if (@($sideEffects).Count -eq 0) { 'No indexed side effect was found; confirm that the flow does not publish, notify, log, schedule, or invalidate state.' }
    if (@($brief.focusedTests).Count -eq 0) { 'No focused test file was identified.' }
)

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    objective = $Objective
    recommended = $recommended
    recommendationReasons = $reasons
    scope = [pscustomobject][ordered]@{
        profile = $profile
        scopes = $scopes
        directModules = @($brief.change.directModules)
        downstreamModules = @($brief.change.downstreamModules)
        groundedPaths = @($research.discovery.groundedPaths | Select-Object -First $Limit)
    }
    inboundConsumers = @($inboundConsumers)
    outboundDependencies = @($outboundDependencies)
    sideEffects = @($sideEffects)
    asyncContinuations = @($asyncContinuations)
    externalBoundaries = @($externalBoundaries)
    verification = [pscustomobject][ordered]@{
        focusedTests = @($brief.focusedTests | Select-Object -First $Limit)
        scenarios = @($brief.testScenarios | Select-Object -First $Limit)
        requiredChecks = @($brief.requiredChecks | Select-Object -First $Limit)
    }
    gaps = $gaps
    authority = 'This scan composes compiled navigation evidence. Verify every reported edge and every empty category in current source before editing.'
    nextAction = if ($recommended) {
        'Confirm or reject every reported edge and gap in current source, then carry applicable consumers, side effects, and verification into design or implementation scope.'
    } else {
        'No integration scan is recommended for this bounded profile; continue with the routed focused workflow unless concrete evidence expands the boundary.'
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; exit 0 }
Write-Host "Integration scan: recommended=$($result.recommended), profile=$profile, scopes=$($scopes -join ', ')"
foreach ($reason in $result.recommendationReasons) { Write-Host " - Reason: $reason" }
Write-Host "Inbound consumers: $(@($result.inboundConsumers).Count)"
Write-Host "Outbound dependencies: $(@($result.outboundDependencies).Count)"
Write-Host "Side effects: $(@($result.sideEffects).Count)"
Write-Host "Async continuations: $(@($result.asyncContinuations).Count)"
Write-Host "External boundaries: $(@($result.externalBoundaries).Count)"
foreach ($gap in $result.gaps) { Write-Host " - GAP: $gap" }
Write-Host "Next: $($result.nextAction)"
