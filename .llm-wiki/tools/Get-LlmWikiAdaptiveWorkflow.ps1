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
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite',
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12
)

$ErrorActionPreference = 'Stop'
$briefArguments = @{
    BaseRef = $BaseRef
    Intent = $Objective
    Format = 'Json'
    Limit = [Math]::Min($Limit, 20)
    CompiledIndexSource = $CompiledIndexSource
    # Routing consumes risk, scope, ownership, rollout, privacy, and decision
    # evidence from the brief, but never its focused tests or scenarios.
    SkipTestPlan = $true
}
if ($PSBoundParameters.ContainsKey('HeadRef')) { $briefArguments.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $briefArguments.ChangedPath = $ChangedPath }
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $briefArguments.ProposedPath = $ProposedPath }
$brief = & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskBrief.ps1') @briefArguments | ConvertFrom-Json

$normalized = $Objective.ToLowerInvariant()
$paths = @($brief.change.paths | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
$routingPaths = @($paths | Where-Object {
    $_ -notmatch '^\.llm-wiki/(?:generated|reviews)/' -and
    $_ -notmatch '^\.artifacts/llm-wiki/'
})
$scopes = @($brief.change.scopes)
$flags = $brief.rolloutFlags
$privacyCount = @($brief.privacyImpact.fields).Count + @($brief.privacyImpact.boundaries).Count + @($brief.privacyImpact.externalTransfers).Count

function ConvertFrom-UnicodeEscape([string]$Value) { ('"' + $Value + '"') | ConvertFrom-Json }
function Test-IntentTerm([string[]]$Terms) {
    foreach ($term in $Terms) { if ($normalized.Contains($term)) { return $true } }
    return $false
}
$ruBugTerms = @(ConvertFrom-UnicodeEscape '\u0438\u0441\u043f\u0440\u0430\u0432'; ConvertFrom-UnicodeEscape '\u0431\u0430\u0433'; ConvertFrom-UnicodeEscape '\u043e\u0448\u0438\u0431'; ConvertFrom-UnicodeEscape '\u043f\u0430\u0434\u0430\u0435\u0442'; ConvertFrom-UnicodeEscape '\u043d\u0435\u0432\u0435\u0440\u043d'; ConvertFrom-UnicodeEscape '\u043d\u0435\u043f\u0440\u0430\u0432\u0438\u043b\u044c\u043d'; ConvertFrom-UnicodeEscape '\u0441\u043b\u043e\u043c')
$ruFeatureTerms = @(ConvertFrom-UnicodeEscape '\u0434\u043e\u0431\u0430\u0432'; ConvertFrom-UnicodeEscape '\u0441\u043e\u0437\u0434\u0430'; ConvertFrom-UnicodeEscape '\u0440\u0435\u0430\u043b\u0438\u0437'; ConvertFrom-UnicodeEscape '\u0444\u0438\u0447')
$ruArchitectureTerms = @(ConvertFrom-UnicodeEscape '\u0430\u0440\u0445\u0438\u0442\u0435\u043a\u0442'; ConvertFrom-UnicodeEscape '\u0440\u0435\u0444\u0430\u043a\u0442\u043e\u0440'; ConvertFrom-UnicodeEscape '\u043c\u043e\u0434\u0443\u043b'; ConvertFrom-UnicodeEscape '\u0437\u0430\u0432\u0438\u0441\u0438\u043c'; ConvertFrom-UnicodeEscape '\u0432\u043d\u0435\u0434\u0440\u0435\u043d')
$ruCriticalTerms = @(ConvertFrom-UnicodeEscape '\u043f\u0435\u0440\u0441\u043e\u043d\u0430\u043b\u044c\u043d'; ConvertFrom-UnicodeEscape '\u043f\u0440\u0438\u0432\u0430\u0442'; ConvertFrom-UnicodeEscape '\u0431\u0435\u0437\u043e\u043f\u0430\u0441'; ConvertFrom-UnicodeEscape '\u0430\u0432\u0442\u043e\u0440\u0438\u0437\u0430\u0446'; ConvertFrom-UnicodeEscape '\u0430\u0443\u0442\u0435\u043d\u0442\u0438\u0444'; ConvertFrom-UnicodeEscape '\u043f\u0430\u0440\u043e\u043b'; ConvertFrom-UnicodeEscape '\u0442\u043e\u043a\u0435\u043d'; ConvertFrom-UnicodeEscape '\u0441\u0435\u043a\u0440\u0435\u0442'; ConvertFrom-UnicodeEscape '\u043f\u043b\u0430\u0442\u0435\u0436'; ConvertFrom-UnicodeEscape '\u043f\u043e\u0434\u043f\u0438\u0441\u043a'; ConvertFrom-UnicodeEscape '\u043c\u0438\u0433\u0440\u0430\u0446'; ConvertFrom-UnicodeEscape '\u0431\u0430\u0437\u0430\u0020\u0434\u0430\u043d\u043d\u044b\u0445'; ConvertFrom-UnicodeEscape '\u043f\u0440\u043e\u0432\u0430\u0439\u0434\u0435\u0440'; ConvertFrom-UnicodeEscape '\u043f\u043e\u0447\u0442'; ConvertFrom-UnicodeEscape '\u043f\u0440\u0438\u0433\u043b\u0430\u0448\u0435\u043d')
$bugIntent = $normalized -match '(\bfix\b|\bbug\b|\berror\b|\bfail|broken|incorrect|wrong|404)' -or (Test-IntentTerm $ruBugTerms)
$featureIntent = $normalized -match '(\badd\b|\bcreate\b|\bimplement\b|\bfeature\b|introduc)' -or (Test-IntentTerm $ruFeatureTerms)
$architecturalIntent = $normalized -match '(architect|refactor|module|dependency|composition root|dependency injection|\bdi\b|ownership)' -or (Test-IntentTerm $ruArchitectureTerms)
$criticalIntent = $normalized -match '\b(auth|authentication|login|password|credential|token|secret|oauth|google|payment|billing|subscription|migration|database|external provider|webhook|email|invite|privacy|security)\b' -or (Test-IntentTerm $ruCriticalTerms)
$boundaryNegated = $normalized -match '\b(without changing|unchanged|no changes? to)\b'
$visualVocabulary = $normalized -match '\b(visual|layout|style|styling|css|scss|html|template|responsive|viewport|spacing|colour|color|icon|label|caption|annotation|overlay|dialog|modal|button|disabled|corner|radius|border)\b'
$localUiInteractionVocabulary = $normalized -match '\b(toggle|switch|selector|tab|dropdown|period|range|expand|collapse|selected|selection|local state|component state|interaction|interactive)\b'
if ($boundaryNegated -and $visualVocabulary) {
    $criticalIntent = $false
    $architecturalIntent = $false
}
$presentationOnly = [string]$brief.risk.profile -eq 'frontend-presentation-only'
$scopeKnown = $paths.Count -gt 0 -and $scopes.Count -gt 0 -and [string]$brief.analysis.mode -ne 'intent-inferred'
$productionScopes = @($scopes | Where-Object { $_ -notin @('Tests', 'Documentation', 'Localization') })
$testSourcePaths = @($routingPaths | Where-Object {
    $_ -match '(?i)(^|/)(tests?|__tests__)/' -or
    $_ -match '(?i)\.Tests?/' -or
    $_ -match '(?i)(?:^|/)[^/]*(?:Tests?|Specs?)\.cs$' -or
    $_ -match '(?i)\.(?:spec|test)\.ts$' -or
    $_ -match '^\.llm-wiki/tools/Test-LlmWiki[^/]*\.ps1$'
})
$testInfrastructurePaths = @($routingPaths | Where-Object {
    $_ -match '(?i)(?:\.csproj|\.fsproj|\.props|\.targets|\.runsettings|package(?:-lock)?\.json|playwright\.config|vitest\.config|jest\.config|Directory\.Build)'
})
$testOnlyChange = $scopeKnown -and $routingPaths.Count -gt 0 -and $testSourcePaths.Count -eq $routingPaths.Count -and $testInfrastructurePaths.Count -eq 0
$visualIntent = $visualVocabulary -or $localUiInteractionVocabulary
$boundaryChangeIntent = $normalized -match '\b(change|modify|add|remove|replace|migrate|integrate|send|store|persist|log|expose)\w*\s+(api|contract|provider|privacy|security|auth|token|credential|database|migration|webhook|payload|data)\b'
$criticalUiSurfaceReference = $normalized -match '\b(auth|authentication|login|oauth|payment|billing|privacy|security)\s+(dialog|modal|page|form|button|panel|screen)\b'
$explicitCriticalBoundaryIntent = -not $criticalUiSurfaceReference -and $normalized -match '\b(fix|change|modify|add|remove|replace|migrate|integrate|link|send|store|persist|expose)\w*\b.{0,48}\b(auth|authentication|login|password|credential|token|secret|oauth|payment|billing|subscription|migration|database|provider|webhook|privacy|security)\b'
$explicitCriticalIncidentIntent = $normalized -match '\b(bypass|unauthori[sz]ed|privilege escalation|credential leak|token leak|secret leak|data leak|exfiltrat|account takeover)\b'
$explicitCriticalMutationIntent = $normalized -match '\b(migrate|send|store|persist|expose|delete|rotate|revoke)\w*\b.{0,48}\b(auth|password|credential|token|secret|payment|billing|subscription|database|provider|webhook|privacy|security|data)\b'
$boundedDataQueryBugIntent = $bugIntent -and
    $normalized -match '\b(split[- ]query|n\+1|duplicate (related )?rows?|query performance|slow query|read model query)\b' -and
    $normalized -notmatch '\b(auth|authentication|password|credential|token|secret|oauth|payment|billing|subscription|webhook|privacy|security)\b'
$ciMaintenanceIntent = $normalized -match '\b(ci|compiler|analy[sz]er|diagnostic|warning|lint|format|ma\d{4})\b'
$dependencyMaintenanceIntent = $normalized -match '\b(storybook|peer dependenc|dependency compatibility|package[- ]lock|dependency bump|npm install|restore dependenc)\b'
$deploymentBuildMaintenanceIntent = $normalized -match '\b(docker|dockerfile|container build|image build|deployment build)\b'
$ruPatternTerms = @(ConvertFrom-UnicodeEscape '\u043f\u043e\u0020\u0430\u043d\u0430\u043b\u043e\u0433\u0438\u0438'; ConvertFrom-UnicodeEscape '\u043a\u0430\u043a\u0020\u0443\u0436\u0435'; ConvertFrom-UnicodeEscape '\u043f\u0435\u0440\u0435\u043d\u0435\u0441\u0442\u0438\u0020\u043f\u0430\u0442\u0442\u0435\u0440\u043d'; ConvertFrom-UnicodeEscape '\u043f\u043e\u0432\u0442\u043e\u0440\u0438\u0442\u044c\u0020\u0434\u043b\u044f')
$patternExtensionIntent = $normalized -match '\b(follow|reuse|mirror|port|replicate|same as|existing pattern|analogous to)\b' -or (Test-IntentTerm $ruPatternTerms)
$backendDomainIntent = $normalized -match '\b(domain|backend|application|infrastructure|aggregate|command|handler|repository|persistence|migration|api|endpoint|cycle tracking|menstrual|period boundary|prediction)\b'
$uiDiscovery = $visualIntent -and -not $backendDomainIntent -and -not $scopeKnown -and -not $explicitCriticalBoundaryIntent
$ungroundedBugDiscovery = $bugIntent -and -not $scopeKnown -and -not $explicitCriticalIncidentIntent -and -not $explicitCriticalMutationIntent
$scopeDiscovery = -not $visualIntent -and -not $scopeKnown -and (($featureIntent -and -not $explicitCriticalBoundaryIntent) -or $ungroundedBugDiscovery)
$frontendOnly = $productionScopes.Count -gt 0 -and @($productionScopes | Where-Object { $_ -ne 'Frontend' }).Count -eq 0
$visualUiChange = $visualIntent -and $scopeKnown -and $frontendOnly -and -not $boundaryChangeIntent -and
    -not $flags.databaseMigration -and -not $flags.externalIntegrations -and -not $flags.configuration -and
    @($brief.architectureHealthImpact.dependencyViolations).Count -eq 0
$visualTiny = $visualUiChange -and $paths.Count -gt 0 -and
    @($paths | Where-Object { $_ -notmatch '\.(?:scss|css)$' }).Count -eq 0
$criticalIntentForRouting = $criticalIntent -and -not $boundedDataQueryBugIntent
$sensitiveBoundaryChange = $privacyCount -gt 0 -and ($criticalIntentForRouting -or $boundaryChangeIntent)
$hasCriticalEvidence = -not $visualUiChange -and -not $uiDiscovery -and -not $scopeDiscovery -and ($criticalIntentForRouting -or $sensitiveBoundaryChange -or $flags.databaseMigration -or $flags.externalIntegrations -or $flags.configuration)
$hasArchitecturalEvidence = -not $visualUiChange -and ($architecturalIntent -or [bool]$brief.decisionContext.reviewRequired -or @($brief.architectureHealthImpact.dependencyViolations).Count -gt 0)
$crossCutting = $productionScopes.Count -gt 1 -or @($brief.change.directModules + $brief.change.downstreamModules | Select-Object -Unique).Count -gt 2
$directModuleCount = @($brief.change.directModules | Select-Object -Unique).Count
$boundedBugScopes = @('Backend', 'Api', 'Frontend', 'Contracts')
if ($boundedDataQueryBugIntent -and -not $flags.databaseMigration) { $boundedBugScopes += 'Database' }
$boundedCrossLayerBug = $bugIntent -and $scopeKnown -and -not $hasCriticalEvidence -and -not $hasArchitecturalEvidence -and
    $directModuleCount -le 1 -and @($productionScopes | Where-Object { $_ -notin $boundedBugScopes }).Count -eq 0 -and
    -not $flags.databaseMigration -and -not $flags.externalIntegrations -and -not $flags.configuration
$maintenanceChange = $scopeKnown -and ($ciMaintenanceIntent -or $dependencyMaintenanceIntent -or $deploymentBuildMaintenanceIntent) -and
    -not $explicitCriticalIncidentIntent -and -not $explicitCriticalMutationIntent -and -not $boundaryChangeIntent -and
    -not $flags.databaseMigration -and -not $flags.externalIntegrations
$maintenanceKind = if ($dependencyMaintenanceIntent) { 'dependency-compatibility' } elseif ($deploymentBuildMaintenanceIntent) { 'deployment-build-fix' } else { 'ci-fix' }
$boundedPatternExtension = $patternExtensionIntent -and $scopeKnown -and -not $explicitCriticalIncidentIntent -and
    -not $flags.externalIntegrations -and -not $flags.configuration -and -not $sensitiveBoundaryChange -and -not $hasArchitecturalEvidence
$wikiInternal = @($paths | Where-Object { $_ -match '^\.llm-wiki/' }).Count -gt 0

$profile = 'feature'
if ($testOnlyChange) { $profile = 'test-only' }
elseif ($uiDiscovery) { $profile = 'ui-discovery' }
elseif ($scopeDiscovery) { $profile = 'scope-discovery' }
elseif ($maintenanceChange) { $profile = 'maintenance' }
elseif ($boundedPatternExtension) { $profile = 'pattern-extension' }
elseif ($hasCriticalEvidence) { $profile = 'critical' }
elseif ($hasArchitecturalEvidence) { $profile = 'architectural' }
elseif ($visualUiChange) { $profile = 'visual-ui-change' }
elseif ($bugIntent -and (-not $crossCutting -or $boundedCrossLayerBug)) { $profile = 'bug' }
elseif (-not $featureIntent -and [int]$brief.risk.score -le 2 -and $scopeKnown -and -not $crossCutting -and -not $wikiInternal) { $profile = 'tiny' }

$confidence = if (-not $scopeKnown) { 'low' } elseif ([string]$brief.analysis.confidence -eq 'high') { 'high' } else { 'medium' }
$confidenceReasons = [Collections.Generic.List[string]]::new()
$discoveryConfidence = if ($scopeKnown) { 'high' } else { 'low' }
$blockerCountConfidence = if ($scopeKnown) { 'medium' } else { 'low' }
$implementationScopeConfidence = switch ([string]$brief.analysis.mode) {
    'git-diff' { 'high' }
    'planned-paths' { 'medium' }
    default { 'low' }
}
if (-not $scopeKnown) { $confidenceReasons.Add("Discovery is low because analysis mode '$($brief.analysis.mode)' does not provide confirmed implementation paths and scopes.") }
if ($blockerCountConfidence -ne 'high') { $confidenceReasons.Add('Blocker-count confidence remains provisional until source-grounded research examines the selected boundary.') }
if ($implementationScopeConfidence -eq 'low') { $confidenceReasons.Add('Implementation-scope confidence is low because no Git diff or caller-confirmed planned path defines the edit boundary.') }
$requiresPathDiscovery = -not $scopeKnown
$requiresDecisionCheckpoint = $profile -in @('critical', 'architectural') -or ($profile -notin @('ui-discovery', 'scope-discovery', 'test-only') -and [bool]$brief.decisionContext.reviewRequired)
$requiresDesign = $profile -in @('feature', 'critical', 'architectural')
$boundedFeatureScopes = $profile -eq 'feature' -and $scopeKnown -and $directModuleCount -le 1 -and
    @($productionScopes | Where-Object { $_ -notin @('Backend', 'Api', 'Frontend', 'Contracts') }).Count -eq 0 -and
    -not $flags.databaseMigration -and -not $flags.externalIntegrations -and -not $flags.configuration
$requiresWorkspace = $profile -notin @('ui-discovery', 'scope-discovery', 'maintenance', 'pattern-extension', 'test-only') -and ($profile -in @('critical', 'architectural') -or ($crossCutting -and -not $boundedFeatureScopes -and -not $boundedCrossLayerBug))
$workflowLevel = if ($requiresWorkspace) { 'governed' } elseif ($profile -in @('feature', 'pattern-extension') -or $requiresDesign) { 'standard' } else { 'small' }
$experiencePolicyPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'policies/experience-policies.json'
$experiencePolicy = Get-Content -LiteralPath $experiencePolicyPath -Raw | ConvertFrom-Json
$ceremonyBudget = $experiencePolicy.ceremonyBudgets.$profile

$stages = [Collections.Generic.List[object]]::new()
function Add-Stage([string]$Id, [string]$Purpose, [string]$Command, [bool]$Required, [string]$CompletionEvidence) {
    $stages.Add([pscustomobject][ordered]@{
        order = $stages.Count + 1
        id = $Id
        purpose = $Purpose
        command = $Command
        required = $Required
        completionEvidence = $CompletionEvidence
    })
}

$escapedObjective = $Objective.Replace("'", "''")
$pathArgument = if ($paths.Count -gt 0) { " -PlannedPath $(($paths | Select-Object -First $Limit | ForEach-Object { "'$($_.Replace("'", "''"))'" }) -join ',')" } else { '' }
$changedPathArgument = if ($paths.Count -gt 0) { " -ChangedPath $(($paths | Select-Object -First $Limit | ForEach-Object { "'$($_.Replace("'", "''"))'" }) -join ',')" } else { '' }
if ($profile -eq 'test-only') {
    Add-Stage 'coverage-brief' 'Identify the production behavior, missing branch, focused test command, and reproducible coverage command without treating referenced critical code as a changed boundary.' "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective'$pathArgument -Compact; ./.llm-wiki/wiki.ps1 test-plan -Intent '$escapedObjective'$pathArgument; ./.llm-wiki/wiki.ps1 coverage-plan$pathArgument -Query '$escapedObjective'" $true 'Every changed path is test-only, the behavior under test is explicit, coverage can be reproduced, and no manifest, configuration, production, API, migration, provider, or architecture file changes.'
    Add-Stage 'test-implementation' 'Add or strengthen assertions and fixtures without weakening existing behavioral guarantees.' '# edit only the grounded test sources and fixtures; review removed or relaxed assertions explicitly' $true 'New assertions prove the intended behavior and no existing assertion, negative case, or invariant is silently weakened.'
    Add-Stage 'focused-verification' 'Run the smallest test project or frontend spec set that exercises the changed tests.' '# run the required focused commands from test-plan' $true 'The changed tests execute and pass, including the new or strengthened assertion.'
    Add-Stage 'completion' 'Confirm the diff remains test-only, refresh only affected compiled artifacts when stale, and run the resumable affected gate.' './.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 update -AffectedOnly -Verify' $true 'The final diff remains test-only, affected indexes are current, and resumable verification passes; publication hooks and CI retain full regression coverage.'
} elseif ($profile -eq 'ui-discovery') {
    Add-Stage 'research' 'Trace the rendered UI path and confirm the runtime-owning component before risk classification.' "./.llm-wiki/wiki.ps1 ui-trace -Query '$escapedObjective'; ./.llm-wiki/wiki.ps1 research -Intent '$escapedObjective'" $true 'Runtime owner and concrete frontend paths are confirmed.'
    Add-Stage 'reclassify' 'Re-run adaptive classification with grounded paths; do not edit while scope is heuristic.' "./.llm-wiki/wiki.ps1 develop -Intent '$escapedObjective' -PlannedPath '<confirmed frontend path(s)>'" $true 'The grounded route is visual-ui-change or evidence explicitly justifies escalation.'
} elseif ($profile -eq 'scope-discovery') {
    Add-Stage 'scope-research' 'Compile a compact brief, trace the existing data flow, and confirm whether storage, provider, privacy, or architecture boundaries actually change.' "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective' -Compact; ./.llm-wiki/wiki.ps1 research -Intent '$escapedObjective'" $true 'Concrete paths, the existing producer-to-consumer flow, and any real critical boundary changes are confirmed.'
    Add-Stage 'reclassify' 'Re-run adaptive classification with evidence-refined intent and grounded paths before creating a workspace or editing.' "./.llm-wiki/wiki.ps1 develop -Intent '<evidence-refined intent>' -PlannedPath '<confirmed path(s)>'" $true 'The grounded route is feature or bug, unless evidence explicitly proves a critical or architectural boundary.'
} elseif ($profile -eq 'maintenance') {
    $maintenanceEvidenceCommand = if ($maintenanceKind -eq 'dependency-compatibility') {
        "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective'$pathArgument -Compact; ./.llm-wiki/wiki.ps1 dependencies"
    } elseif ($maintenanceKind -eq 'deployment-build-fix') {
        "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective'$pathArgument -Compact; ./.llm-wiki/wiki.ps1 rollout$changedPathArgument"
    } else {
        "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective'$pathArgument -Compact"
    }
    Add-Stage 'evidence-brief' 'Use supplied diagnostics and concrete paths as primary evidence; load only scoped instructions and maintenance obligations.' $maintenanceEvidenceCommand $true 'The diagnostic, exact maintenance boundary, and targeted validation command are explicit without heuristic flow expansion.'
    Add-Stage 'implementation' 'Apply the smallest compatibility or build fix inside the confirmed maintenance boundary.' '# edit only the confirmed CI, dependency, build, or diagnostic paths' $true 'The external failure is addressed without changing runtime product behavior or architecture boundaries.'
    Add-Stage 'targeted-verification' 'Re-run the failing analyzer, dependency install/build, container build, or equivalent focused check.' '# run the exact failing command from CI, package metadata, or build evidence' $true 'The original external diagnostic is reproduced as passing.'
    Add-Stage 'completion' 'Confirm the diff remains maintenance-only and run the fast local gate.' './.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 verify-fast' $true 'The maintenance boundary is unchanged and fast verification passes; publication hooks remain strict.'
} elseif ($profile -eq 'pattern-extension') {
    Add-Stage 'precedent-brief' 'Ground the target in one current, tested repository precedent and describe only the intentional delta.' "./.llm-wiki/wiki.ps1 precedents -Intent '$escapedObjective'$pathArgument; ./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective'$pathArgument -Compact" $true 'A current source-and-test precedent, target paths, and the deliberate behavioral or schema delta are explicit.'
    Add-Stage 'compatibility-delta' 'Check API, migration, rollout, and consumer obligations only where the copied pattern crosses those boundaries.' "./.llm-wiki/wiki.ps1 rollout$changedPathArgument; ./.llm-wiki/wiki.ps1 api-compat" $true 'Migration and additive API compatibility are proven when applicable; irrelevant governance is skipped.'
    Add-Stage 'implementation' 'Extend the proven pattern without redesigning its established architecture.' '# mirror the precedent in the grounded target paths and add target-specific regression coverage' $true 'The target follows the precedent and contains only documented deltas.'
    Add-Stage 'focused-verification' 'Run the precedent-equivalent target tests plus changed contract or migration checks.' "./.llm-wiki/wiki.ps1 test-plan -Intent '$escapedObjective'$pathArgument; # run required checks only" $true 'Required target, contract, and migration checks pass.'
    Add-Stage 'completion' 'Confirm bounded parity with the precedent and run the strict affected gate.' './.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 verify-strict-affected' $true 'The implementation remains precedent-bounded and strict affected verification passes.'
} elseif ($boundedCrossLayerBug) {
    $bugBriefCommand = if ($boundedDataQueryBugIntent) {
        "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective'$pathArgument -Compact"
    } else {
        "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective'$pathArgument -Compact; ./.llm-wiki/wiki.ps1 trace -Query '$escapedObjective'"
    }
    Add-Stage 'bug-brief' 'Confirm the root cause, one existing flow, direct module owner, downstream consumers, and additive API boundary.' $bugBriefCommand $true 'The root cause and bounded edit surface are explicit; no migration, storage, provider, privacy-lifecycle, or architecture change is present.'
    Add-Stage 'implementation' 'Apply the smallest compatible fix inside the confirmed flow and add regression coverage.' '# edit the confirmed source and regression tests' $true 'The root cause is fixed without broadening the existing flow or contract.'
    Add-Stage 'focused-verification' 'Derive and run only checks covering the changed flow and known downstream consumers.' "./.llm-wiki/wiki.ps1 test-plan -Intent '$escapedObjective'$pathArgument; # run focused tests from the plan" $true 'Focused producer, transport, consumer, and regression tests pass.'
    Add-Stage 'completion' 'Confirm the actual diff remains bounded and run the fast local gate.' './.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 verify-fast' $true 'The diff matches the confirmed bug boundary and fast verification passes; full verification remains the publication gate.'
} else {
Add-Stage $(if ($profile -eq 'visual-ui-change') { 'visual-brief' } else { 'research' }) $(if ($visualTiny) { 'Load scoped style and design-system constraints without retracing the already grounded component tree.' } elseif ($profile -eq 'visual-ui-change') { 'Compile a compact constraint brief and confirm whether the runtime owner belongs to the application shell or a reusable UI-kit surface.' } else { 'Compile current code paths, open questions, provider boundaries, failures, and Git precedents.' }) $(if ($visualTiny) { "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective'$pathArgument -Compact" } elseif ($profile -eq 'visual-ui-change') { "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective'$pathArgument -Compact; ./.llm-wiki/wiki.ps1 ui-trace -Query '$escapedObjective'$pathArgument" } else { "./.llm-wiki/wiki.ps1 research -Intent '$escapedObjective'$pathArgument" }) $true $(if ($visualTiny) { 'Scoped instructions, design tokens, affected viewport, and browser-verifiable outcome are explicit.' } elseif ($profile -eq 'visual-ui-change') { 'The runtime owner, UI-kit versus application boundary, scoped instructions, design-system constraints, and browser-verifiable outcomes are explicit.' } else { 'Research packet has grounded paths and the runtime owner is confirmed, or explicitly reports unresolved discovery.' })
Add-Stage 'journey-impact' 'Identify affected FoodDiary user journeys and their end-to-end scenarios.' "./.llm-wiki/wiki.ps1 journeys -Intent '$escapedObjective'$pathArgument" ($profile -notin @('tiny', 'visual-ui-change')) 'Relevant journeys, negative paths, and evidence hints are included in scope.'
if ($requiresDecisionCheckpoint) {
    Add-Stage 'checkpoint' 'Resolve architectural, contract, privacy, or rollout choices before editing.' "./.llm-wiki/wiki.ps1 research -Intent '$escapedObjective'$pathArgument -Format Json" $true 'Every blocking decision is resolved or recorded as an explicit assumption.'
}
if ($requiresDesign) {
    Add-Stage 'design' 'Define target behavior, invariants, compatibility, failure behavior, and rejected alternatives.' "./.llm-wiki/wiki.ps1 design -Intent '$escapedObjective'$pathArgument -Decision '<selected choice and source evidence>'" $true 'The design checkpoint has no unresolved blocking question and its implementation phases have explicit outcomes.'
}
if ($requiresWorkspace) {
    Add-Stage 'workspace' 'Create governed scope, acceptance, evidence, and conformance state.' "./.llm-wiki/wiki.ps1 task-start -Intent '$escapedObjective' -Criterion '<acceptance criterion>' -AllowedPath '<path regex>' -WorkspacePath .artifacts/llm-wiki/tasks/<task-name>" $true 'A task workspace exists with acceptance criteria and bounded scope.'
    Add-Stage 'requirements' 'Make every acceptance criterion atomic, mapped, and evidence-addressable before implementation.' './.llm-wiki/wiki.ps1 task-requirements-assess -WorkspacePath .artifacts/llm-wiki/tasks/<task-name> -FailOnInvalid' $true 'The requirement model has no ambiguity findings and every product outcome has an acceptance criterion.'
}
Add-Stage 'implementation' 'Implement only the selected design and declared scope.' '# edit source and tests; use task-note for decisions or blockers' $true 'Behavior and focused tests are implemented.'
if ($profile -eq 'visual-ui-change') {
    if ($visualTiny) {
        Add-Stage 'focused-verification' 'Check only the changed stylesheet during visual iteration.' '# run stylelint for the changed stylesheet; defer the production build to the publication gate' $true 'The changed stylesheet passes stylelint.'
    } else {
        Add-Stage 'focused-verification' 'Derive and run focused component checks, then compile the frontend.' "./.llm-wiki/wiki.ps1 test-plan -Intent '$escapedObjective'; # run the focused component tests; cd FoodDiary.Web.Client && npm run build" $true 'Focused component tests and the production frontend build pass.'
    }
    Add-Stage 'browser-evidence' 'Verify the changed rendering at the viewport(s) affected by the requested scope.' "./.llm-wiki/wiki.ps1 visual-qa -Url '<local URL>' -FixturePath '<upload fixture>' -TriggerSelector '<optional trigger>' -ResultSelector '<expected result>' -Run" $true 'Automated browser evidence proves upload, the affected viewport, visible result, screenshot, and console health; omitted viewports are explicitly out of scope.'
    Add-Stage 'completion' 'Confirm the final diff stays inside the visual owner slice and run the uncached strict affected gate.' "./.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 verify-strict-affected" $true 'Actual paths remain visual-only and the strict affected gate passes after focused checks and browser evidence; full repository verification remains the CI gate.'
} else {
    $changeReviewCommand = if ($requiresWorkspace) {
        "./.llm-wiki/wiki.ps1 delivery-status -WorkspacePath .artifacts/llm-wiki/tasks/<task-name>; ./.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 test-plan -Intent '$escapedObjective'"
    } else {
        "./.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 test-plan -Intent '$escapedObjective'"
    }
    if ($profile -in @('feature', 'bug')) {
        $changeReviewCommand += '; ./.llm-wiki/wiki.ps1 update -AffectedOnly -ContractIndexesOnly'
    }
    Add-Stage 'change-review' 'Recompute actual impact and compare the diff with intent, plan, and journeys.' $changeReviewCommand $true 'Actual paths, checks, journey impact, and review obligations are known; intentional drift is replanned with a reason.'
    $verifyCommand = if ($profile -eq 'tiny') {
        './.llm-wiki/wiki.ps1 verify-fast'
    } elseif ($profile -in @('feature', 'bug')) {
        './.llm-wiki/wiki.ps1 verify -AffectedOnly -ContractIndexesOnly'
    } else {
        './.llm-wiki/wiki.ps1 verify'
    }
    Add-Stage 'verification' 'Run risk-proportional deterministic verification.' $verifyCommand $true 'Required contract/navigation checks and focused product verification pass; successful Wiki stages are resumable.'
    if ($profile -in @('feature', 'bug')) {
        Add-Stage 'publication-finalization' 'Refresh deferred analytical indexes once after implementation stops changing.' './.llm-wiki/wiki.ps1 update -AffectedOnly -Verify' $true 'All affected generated artifacts are current; cached successful stages are reused and publication hooks retain full regression coverage.'
    }
}
if ($requiresWorkspace) {
    Add-Stage 'delivery-validation' 'Prove requirements against mapped implementation and current verification evidence.' './.llm-wiki/wiki.ps1 delivery-validate -WorkspacePath .artifacts/llm-wiki/tasks/<task-name> -FailOnInvalid' $true 'Requirements, acceptance, conformance, proof-of-change, and workspace readiness all pass.'
}
if ($profile -in @('critical', 'architectural')) {
    Add-Stage 'independent-review' 'Attempt to disprove completion from the final evidence rather than trusting implementation reasoning.' './.llm-wiki/wiki.ps1 delivery-critique -WorkspacePath .artifacts/llm-wiki/tasks/<task-name> -FailOnInvalid' $true 'The adverse critique verdict is approve or approve-with-notes, with no unresolved blocking finding.'
}
Add-Stage 'handoff' 'Preserve verified continuity if work crosses a session boundary.' './.llm-wiki/wiki.ps1 pause -WorkspacePath .artifacts/llm-wiki/tasks/<task-name>' $false 'A fingerprinted handoff can be resumed with drift detection.'
}

$reasons = [Collections.Generic.List[string]]::new()
if ($bugIntent) { $reasons.Add('Intent describes corrective behavior.') }
if ($featureIntent) { $reasons.Add('Intent describes new behavior.') }
if ($presentationOnly) { $reasons.Add('Changed or planned paths form a frontend presentation-only slice.') }
if ($visualUiChange) { $reasons.Add('Local visual or interactive frontend scope has no API, route, persisted-state, provider, privacy, security, public-contract, or architecture boundary change.') }
if ($uiDiscovery) { $reasons.Add('Visual intent is not grounded in concrete paths; runtime-owner discovery must precede risk classification.') }
if ($scopeDiscovery) { $reasons.Add('Feature or bug intent is not grounded in concrete paths; existing-flow research must precede critical classification and workspace creation.') }
if ($boundedCrossLayerBug) { $reasons.Add('The confirmed bug crosses layers inside one existing module flow without migration, provider, sensitive-data lifecycle, or architecture changes.') }
if ($maintenanceChange) { $reasons.Add("Concrete diagnostics and paths define a bounded $maintenanceKind maintenance change without runtime contract or architecture changes.") }
if ($boundedPatternExtension) { $reasons.Add('The objective explicitly extends an existing repository pattern through grounded paths, so compatibility delta and focused parity checks replace design-from-scratch ceremony.') }
if ($testOnlyChange) { $reasons.Add('Every grounded path is a test source or fixture, so referenced production risks guide coverage but do not classify unchanged production boundaries as critical.') }
if ($hasCriticalEvidence) { $reasons.Add('Sensitive, provider, persistence, configuration, or delivery evidence requires the critical workflow.') }
if ($hasArchitecturalEvidence) { $reasons.Add('Architecture or durable decision evidence requires the architectural workflow.') }
if ($crossCutting) { $reasons.Add('The inferred change crosses multiple scopes or modules.') }
if ($requiresPathDiscovery) { $reasons.Add('Concrete implementation paths are not grounded yet; research must discover them before risk can be trusted.') }

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    objective = $Objective
    profile = $profile
    workflowVariant = $(if ($visualTiny) { 'visual-tiny' } else { $profile })
    workflowLevel = $workflowLevel
    maintenanceKind = $(if ($profile -eq 'maintenance') { $maintenanceKind } else { $null })
    confidence = $confidence
    confidenceDimensions = [pscustomobject][ordered]@{
        discovery = $discoveryConfidence
        blockerCount = $blockerCountConfidence
        implementationScope = $implementationScopeConfidence
    }
    confidenceReasons = @($confidenceReasons)
    scopeKnown = $scopeKnown
    requiresPathDiscovery = $requiresPathDiscovery
    requiresDecisionCheckpoint = $requiresDecisionCheckpoint
    requiresDesign = $requiresDesign
    requiresWorkspace = $requiresWorkspace
    ceremonyBudget = $ceremonyBudget
    ceremonyPrinciples = @($experiencePolicy.principles)
    reasons = @($reasons)
    inferred = [pscustomobject][ordered]@{
        paths = $paths
        scopes = $scopes
        modules = @($brief.change.directModules + $brief.change.downstreamModules | Select-Object -Unique)
        risk = $brief.risk
    }
    stages = @($stages)
    nextAction = $stages[0]
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 10; exit 0 }
Write-Host "Adaptive workflow: $profile / $workflowLevel ($confidence confidence)"
Write-Host "Confidence: discovery=$discoveryConfidence; blocker-count=$blockerCountConfidence; implementation-scope=$implementationScopeConfidence"
foreach ($confidenceReason in $confidenceReasons) { Write-Host "Confidence reason: $confidenceReason" }
Write-Host "Objective: $Objective"
Write-Host "Ceremony budget: $($ceremonyBudget.label), at most $($ceremonyBudget.maximumRequiredStages) required stage(s)"
foreach ($reason in $result.reasons) { Write-Host "Reason: $reason" }
Write-Host ''
foreach ($stage in $result.stages) {
    $requirement = if ($stage.required) { 'required' } else { 'when needed' }
    Write-Host "$($stage.order). $($stage.id) [$requirement]"
    Write-Host "   $($stage.purpose)"
    Write-Host "   Run: $($stage.command)"
    Write-Host "   Done when: $($stage.completionEvidence)"
}
