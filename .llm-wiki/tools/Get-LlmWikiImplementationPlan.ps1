[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [object]$BriefInput,
    [string]$Objective,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12
)

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$common = @{ BaseRef = $BaseRef; Format = 'Json' }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $common.ChangedPath = $ChangedPath }

$briefArguments = @{} + $common
$briefArguments.Limit = [Math]::Min($Limit, 20)
$brief = if ($null -ne $BriefInput) { $BriefInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiTaskBrief.ps1') @briefArguments | ConvertFrom-Json
}
$decision = if ($brief.PSObject.Properties['decisionContext'] -and $null -ne $brief.decisionContext) {
    $brief.decisionContext
} else { [pscustomobject]@{ relatedAdrs = @() } }
$rollout = if ($brief.PSObject.Properties['rolloutPlan'] -and $null -ne $brief.rolloutPlan) {
    $brief.rolloutPlan
} else { [pscustomobject]@{ preDeploy = @(); deploy = @(); postDeploy = @(); rollback = @() } }
$phases = [System.Collections.Generic.List[object]]::new()

function Get-PlanPropertyValues([object[]]$InputObject, [string]$PropertyName) {
    return @($InputObject | ForEach-Object {
        if ($null -ne $_ -and $_.PSObject.Properties[$PropertyName]) {
            $value = $_.$PropertyName
            if ($null -ne $value -and -not [string]::IsNullOrWhiteSpace([string]$value)) { $value }
        }
    })
}

function Add-Phase {
    param(
        [string]$Id,
        [string]$Title,
        [string]$Outcome,
        [object[]]$Files,
        [object[]]$Actions,
        [object[]]$Evidence,
        [object[]]$StopConditions = @()
    )
    $phases.Add([pscustomobject][ordered]@{
        order = $phases.Count + 1
        id = $Id
        title = $Title
        outcome = $Outcome
        files = @($Files | Where-Object { $null -ne $_ -and "$_".Length -gt 0 } | Select-Object -Unique)
        actions = @($Actions | Where-Object { $null -ne $_ -and "$_".Length -gt 0 } | Select-Object -Unique)
        evidence = @($Evidence | Where-Object { $null -ne $_ -and "$_".Length -gt 0 } | Select-Object -Unique)
        stopConditions = @($StopConditions | Where-Object { $null -ne $_ -and "$_".Length -gt 0 } | Select-Object -Unique)
    })
}

$preflightStops = @(
    @($brief.structuralViolations | ForEach-Object { "Resolve policy violation [$($_.ruleId)]: $($_.message)" }) +
    @($brief.architectureHealthImpact.dependencyViolations | ForEach-Object { "Resolve forbidden project edge: $($_.source) -> $($_.target)" }) +
    @($brief.architectureHealthImpact.untrackedProductionProjects | ForEach-Object { "Govern production project in dependency matrix: $($_.name)" }) +
    @($brief.architectureHealthImpact.moduleCycleNodes | ForEach-Object { "Resolve module cycle involving: $_" })
)
Add-Phase 'context' 'Establish scope and invariants' `
    'The implementation starts from authoritative instructions, affected modules, contracts, and explicit constraints.' `
    @($brief.instructions + $brief.contextPages + (Get-PlanPropertyValues @($decision.relatedAdrs) 'path')) `
    @(
        "Confirm objective: $(if ([string]::IsNullOrWhiteSpace($Objective)) { 'derive exact acceptance criteria from the task before editing' } else { $Objective })"
        "Read applicable AGENTS.md files and cited source pages."
        "Confirm allowed/excluded paths with a task contract when the change scope is narrow."
        "Record assumptions that cannot be proven from source."
    ) `
    @('Explicit acceptance criteria', 'Confirmed edit boundary', 'Applicable ADR constraints') `
    $preflightStops

$contractFiles = @(
    @(Get-PlanPropertyValues @($brief.backendContractImpact.contracts) 'definitionPaths') +
    @(Get-PlanPropertyValues @($brief.backendContractImpact.productionConsumers) 'consumerPath') +
    @(Get-PlanPropertyValues @($brief.frontendContractImpact.components) 'path') +
    @(Get-PlanPropertyValues @($brief.frontendContractImpact.components) 'templatePath') +
    @(Get-PlanPropertyValues @($brief.frontendContractImpact.downstreamConsumers) 'consumerPath')
) | Select-Object -First ($Limit * 3)
$contractActions = @()
if (@($brief.backendContractImpact.contracts).Count -gt 0) {
    $contractActions += 'Design backend contract evolution before implementation; preserve or explicitly migrate constructors, members, nullability, serialization, and consumers.'
}
if (@($brief.frontendContractImpact.components).Count -gt 0) {
    $contractActions += 'Design selector/input/output evolution and enumerate every indexed template consumer before editing shared UI.'
}
if (@($brief.privacyImpact.fields).Count -gt 0) {
    $contractActions += 'Define data minimization, authorization, redaction, provider sharing, retention, and deletion behavior for sensitive fields.'
}
if ($contractActions.Count -gt 0) {
    Add-Phase 'contracts' 'Design compatibility and consumer migration' `
        'Public and internal consumers have an explicit compatible migration path.' `
        $contractFiles `
        $contractActions `
        @($brief.reviewObligations | ForEach-Object { "$($_.id): $($_.description)" }) `
        @('Stop if an external or dynamically resolved consumer cannot be assessed.', 'Stop if rollout requires an unplanned breaking contract transition.')
}

$domainFiles = @(@(
    @(Get-PlanPropertyValues @($brief.domainDataImpact.types) 'path') +
    @(Get-PlanPropertyValues @($brief.domainDataImpact.invariants) 'path') +
    @(Get-PlanPropertyValues @($brief.domainDataImpact.mappings) 'path')
) | Select-Object -Unique)
if ($domainFiles.Count -gt 0 -or @($brief.change.scopes) -contains 'Database') {
    Add-Phase 'domain-data' 'Preserve domain and persistence invariants' `
        'All construction, mutation, and persistence paths enforce the intended rule and schema contract.' `
        $domainFiles `
        @(
            'Define valid states and boundary values before changing entities or value objects.'
            'Align EF keys, nullability, conversions, uniqueness, relationships, delete behavior, and indexes.'
            'Generate and inspect both migration files when the physical schema changes.'
        ) `
        @($brief.testScenarios | Where-Object id -match 'domain|persistence|migration' | ForEach-Object { "$($_.id): $($_.evidence)" }) `
        @('Stop if existing persisted rows cannot migrate safely.', 'Stop if rollback would destroy or reinterpret data without an approved roll-forward plan.')
}

$implementationFiles = @(
    @($brief.change.paths) +
    @(Get-PlanPropertyValues @($brief.backendContractImpact.productionConsumers) 'consumerPath') +
    @(Get-PlanPropertyValues @($brief.frontendContractImpact.downstreamConsumers) 'consumerPath')
) | Select-Object -Unique -First ($Limit * 4)
Add-Phase 'implementation' 'Implement in dependency order' `
    'Lower-level contracts and rules are implemented before adapters, hosts, and consuming surfaces.' `
    $implementationFiles `
    @(
        'Implement domain/contracts first, then application use cases, infrastructure/providers, presentation adapters, and composition roots.'
        'Keep changes inside the declared task boundary and preserve unrelated working-tree changes.'
        'Propagate CancellationToken, stable errors, authorization scope, and telemetry through changed I/O paths.'
    ) `
    @('Reviewable diff with no unrelated edits', 'No new architecture-health violations') `
    @('Stop if implementation requires a materially different architecture or new external authority.')

$testFiles = @(
    @($brief.focusedTests) +
    @(Get-PlanPropertyValues @($brief.backendContractImpact.testConsumers) 'consumerPath')
) | Select-Object -Unique -First ($Limit * 3)
Add-Phase 'focused-verification' 'Add and run focused verification' `
    'Changed behavior, boundaries, failure paths, and consumers are covered at the closest reliable test level.' `
    $testFiles `
    @($brief.testScenarios | ForEach-Object { "$($_.id): $($_.description)" }) `
    @(
        @($brief.requiredChecks | ForEach-Object { "$($_.id): $($_.command)" }) +
        @($brief.focusedTests | ForEach-Object { "Focused test: $_" })
    ) `
    @('Stop on a failing focused test; diagnose before broad verification.', 'Do not mark a required scenario not-applicable without a recorded reason.')

if (@($brief.generatedActions).Count -gt 0) {
    Add-Phase 'generated-artifacts' 'Refresh deterministic artifacts' `
        'Generated indexes, snapshots, module pages, and contracts match the final source state.' `
        @($brief.generatedActions) `
        @($brief.generatedActions | ForEach-Object { "Run $_" }) `
        @('./.llm-wiki/wiki.ps1 verify') `
        @('Stop if check mode differs after regeneration; resolve nondeterminism or stale sources.')
}

Add-Phase 'release-readiness' 'Verify rollout, observability, and handoff' `
    'The change can be deployed, observed, and recovered with completed evidence.' `
    @() `
    @(
        @($rollout.preDeploy | ForEach-Object { "Pre-deploy: $_" }) +
        @($rollout.deploy | ForEach-Object { "Deploy: $_" }) +
        @($rollout.postDeploy | ForEach-Object { "Post-deploy: $_" }) +
        @($rollout.rollback | ForEach-Object { "Recovery: $_" })
    ) `
    @(
        @($brief.requiredChecks | ForEach-Object { "$($_.id): $($_.command)" }) +
        './.llm-wiki/wiki.ps1 verify'
        'Completed evidence bundle and handoff summary'
    ) `
    @('Do not deploy with unresolved structural violations, failed required checks, or an unsafe data rollback assumption.')

$result = [pscustomobject][ordered]@{
    objective = if ([string]::IsNullOrWhiteSpace($Objective)) { $null } else { $Objective }
    risk = $brief.risk
    scopes = @($brief.change.scopes)
    modules = [pscustomobject]@{
        direct = @($brief.change.directModules)
        downstream = @($brief.change.downstreamModules)
    }
    acceptanceInputs = [pscustomobject]@{
        changedPaths = @($brief.change.paths)
        instructions = @($brief.instructions)
        contextPages = @($brief.contextPages)
        relatedAdrs = @(Get-PlanPropertyValues @($decision.relatedAdrs) 'path')
    }
    phases = @($phases)
    finalGates = @(
        @($brief.requiredChecks | ForEach-Object { [pscustomobject]@{ id = $_.id; command = $_.command } }) +
        [pscustomobject]@{ id = 'llm-wiki-verify'; command = './.llm-wiki/wiki.ps1 verify' }
    ) | Sort-Object command -Unique
    unresolved = [pscustomobject]@{
        structuralViolations = @($brief.structuralViolations)
        warnings = @($brief.warnings)
        decisionReviewRequired = [bool]$decision.reviewRequired
        decisionGuidance = $decision.guidance
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; exit 0 }
Write-Host "Implementation plan: $($result.risk.level) risk, $(@($result.phases).Count) phase(s)"
if ($result.objective) { Write-Host "Objective: $($result.objective)" }
Write-Host "Scopes: $($result.scopes -join ', ')"
Write-Host "Modules: $(@($result.modules.direct + $result.modules.downstream | Select-Object -Unique) -join ', ')"
foreach ($phase in $result.phases) {
    Write-Host ''
    Write-Host "$($phase.order). $($phase.title)"
    Write-Host "   Outcome: $($phase.outcome)"
    foreach ($file in $phase.files) { Write-Host "   File: $file" }
    foreach ($action in $phase.actions) { Write-Host "   Action: $action" }
    foreach ($evidence in $phase.evidence) { Write-Host "   Evidence: $evidence" }
    foreach ($stop in $phase.stopConditions) { Write-Host "   STOP: $stop" }
}
