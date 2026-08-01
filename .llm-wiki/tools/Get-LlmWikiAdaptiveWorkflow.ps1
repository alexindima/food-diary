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
}
if ($PSBoundParameters.ContainsKey('HeadRef')) { $briefArguments.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $briefArguments.ChangedPath = $ChangedPath }
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $briefArguments.ProposedPath = $ProposedPath }
$brief = & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskBrief.ps1') @briefArguments | ConvertFrom-Json

$normalized = $Objective.ToLowerInvariant()
$paths = @($brief.change.paths | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
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
if ($boundaryNegated -and $visualVocabulary) {
    $criticalIntent = $false
    $architecturalIntent = $false
}
$presentationOnly = [string]$brief.risk.profile -eq 'frontend-presentation-only'
$scopeKnown = $paths.Count -gt 0 -and $scopes.Count -gt 0 -and [string]$brief.analysis.mode -ne 'intent-inferred'
$productionScopes = @($scopes | Where-Object { $_ -notin @('Tests', 'Documentation', 'Localization') })
$visualIntent = $visualVocabulary
$boundaryChangeIntent = $normalized -match '\b(change|modify|add|remove|replace|migrate|integrate|send|store|persist|log|expose)\w*\s+(api|contract|provider|privacy|security|auth|token|credential|database|migration|webhook|payload|data)\b'
$criticalUiSurfaceReference = $normalized -match '\b(auth|authentication|login|oauth|payment|billing|privacy|security)\s+(dialog|modal|page|form|button|panel|screen)\b'
$explicitCriticalBoundaryIntent = -not $criticalUiSurfaceReference -and $normalized -match '\b(fix|change|modify|add|remove|replace|migrate|integrate|link|send|store|persist|expose)\w*\b.{0,48}\b(auth|authentication|login|password|credential|token|secret|oauth|payment|billing|subscription|migration|database|provider|webhook|privacy|security)\b'
$uiDiscovery = $visualIntent -and -not $scopeKnown -and -not $explicitCriticalBoundaryIntent
$scopeDiscovery = -not $visualIntent -and -not $scopeKnown -and ($featureIntent -or $bugIntent) -and -not $explicitCriticalBoundaryIntent
$frontendOnly = $productionScopes.Count -gt 0 -and @($productionScopes | Where-Object { $_ -ne 'Frontend' }).Count -eq 0
$visualUiChange = $visualIntent -and $scopeKnown -and $frontendOnly -and -not $boundaryChangeIntent -and
    -not $flags.databaseMigration -and -not $flags.externalIntegrations -and -not $flags.configuration -and
    @($brief.architectureHealthImpact.dependencyViolations).Count -eq 0
$sensitiveBoundaryChange = $privacyCount -gt 0 -and ($criticalIntent -or $boundaryChangeIntent)
$hasCriticalEvidence = -not $visualUiChange -and -not $uiDiscovery -and -not $scopeDiscovery -and ($criticalIntent -or $sensitiveBoundaryChange -or $flags.databaseMigration -or $flags.externalIntegrations -or $flags.configuration)
$hasArchitecturalEvidence = -not $visualUiChange -and ($architecturalIntent -or [bool]$brief.decisionContext.reviewRequired -or @($brief.architectureHealthImpact.dependencyViolations).Count -gt 0)
$crossCutting = $productionScopes.Count -gt 1 -or @($brief.change.directModules + $brief.change.downstreamModules | Select-Object -Unique).Count -gt 2
$directModuleCount = @($brief.change.directModules | Select-Object -Unique).Count
$boundedCrossLayerBug = $bugIntent -and $scopeKnown -and -not $hasCriticalEvidence -and -not $hasArchitecturalEvidence -and
    $directModuleCount -le 1 -and @($productionScopes | Where-Object { $_ -notin @('Backend', 'Api', 'Frontend', 'Contracts') }).Count -eq 0 -and
    -not $flags.databaseMigration -and -not $flags.externalIntegrations -and -not $flags.configuration
$wikiInternal = @($paths | Where-Object { $_ -match '^\.llm-wiki/' }).Count -gt 0

$profile = 'feature'
if ($uiDiscovery) { $profile = 'ui-discovery' }
elseif ($scopeDiscovery) { $profile = 'scope-discovery' }
elseif ($hasCriticalEvidence) { $profile = 'critical' }
elseif ($hasArchitecturalEvidence) { $profile = 'architectural' }
elseif ($visualUiChange) { $profile = 'visual-ui-change' }
elseif ($bugIntent -and (-not $crossCutting -or $boundedCrossLayerBug)) { $profile = 'bug' }
elseif (-not $featureIntent -and [int]$brief.risk.score -le 2 -and $scopeKnown -and -not $crossCutting -and -not $wikiInternal) { $profile = 'tiny' }

$confidence = if (-not $scopeKnown) { 'low' } elseif ([string]$brief.analysis.confidence -eq 'high') { 'high' } else { 'medium' }
$requiresPathDiscovery = -not $scopeKnown
$requiresDecisionCheckpoint = $profile -in @('critical', 'architectural') -or ($profile -notin @('ui-discovery', 'scope-discovery') -and [bool]$brief.decisionContext.reviewRequired)
$requiresDesign = $profile -in @('feature', 'critical', 'architectural')
$boundedFeatureScopes = $profile -eq 'feature' -and $scopeKnown -and $directModuleCount -le 1 -and
    @($productionScopes | Where-Object { $_ -notin @('Backend', 'Api', 'Frontend', 'Contracts') }).Count -eq 0 -and
    -not $flags.databaseMigration -and -not $flags.externalIntegrations -and -not $flags.configuration
$requiresWorkspace = $profile -notin @('ui-discovery', 'scope-discovery') -and ($profile -in @('critical', 'architectural') -or ($crossCutting -and -not $boundedFeatureScopes -and -not $boundedCrossLayerBug))
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
if ($profile -eq 'ui-discovery') {
    Add-Stage 'research' 'Trace the rendered UI path and confirm the runtime-owning component before risk classification.' "./.llm-wiki/wiki.ps1 ui-trace -Query '$escapedObjective'; ./.llm-wiki/wiki.ps1 research -Intent '$escapedObjective'" $true 'Runtime owner and concrete frontend paths are confirmed.'
    Add-Stage 'reclassify' 'Re-run adaptive classification with grounded paths; do not edit while scope is heuristic.' "./.llm-wiki/wiki.ps1 develop -Intent '$escapedObjective' -PlannedPath '<confirmed frontend path(s)>'" $true 'The grounded route is visual-ui-change or evidence explicitly justifies escalation.'
} elseif ($profile -eq 'scope-discovery') {
    Add-Stage 'scope-research' 'Compile a compact brief, trace the existing data flow, and confirm whether storage, provider, privacy, or architecture boundaries actually change.' "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective' -Compact; ./.llm-wiki/wiki.ps1 research -Intent '$escapedObjective'" $true 'Concrete paths, the existing producer-to-consumer flow, and any real critical boundary changes are confirmed.'
    Add-Stage 'reclassify' 'Re-run adaptive classification with evidence-refined intent and grounded paths before creating a workspace or editing.' "./.llm-wiki/wiki.ps1 develop -Intent '<evidence-refined intent>' -PlannedPath '<confirmed path(s)>'" $true 'The grounded route is feature or bug, unless evidence explicitly proves a critical or architectural boundary.'
} elseif ($boundedCrossLayerBug) {
    Add-Stage 'bug-brief' 'Confirm the root cause, one existing flow, direct module owner, downstream consumers, and additive API boundary.' "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective'$pathArgument -Compact; ./.llm-wiki/wiki.ps1 trace -Query '$escapedObjective'" $true 'The root cause and bounded edit surface are explicit; no migration, storage, provider, privacy-lifecycle, or architecture change is present.'
    Add-Stage 'implementation' 'Apply the smallest compatible fix inside the confirmed flow and add regression coverage.' '# edit the confirmed source and regression tests' $true 'The root cause is fixed without broadening the existing flow or contract.'
    Add-Stage 'focused-verification' 'Derive and run only checks covering the changed flow and known downstream consumers.' "./.llm-wiki/wiki.ps1 test-plan -Intent '$escapedObjective'$pathArgument; # run focused tests from the plan" $true 'Focused producer, transport, consumer, and regression tests pass.'
    Add-Stage 'completion' 'Confirm the actual diff remains bounded and run the fast local gate.' './.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 verify-fast' $true 'The diff matches the confirmed bug boundary and fast verification passes; full verification remains the publication gate.'
} else {
Add-Stage $(if ($profile -eq 'visual-ui-change') { 'visual-brief' } else { 'research' }) $(if ($profile -eq 'visual-ui-change') { 'Compile a compact constraint brief and confirm whether the runtime owner belongs to the application shell or a reusable UI-kit surface.' } else { 'Compile current code paths, open questions, provider boundaries, failures, and Git precedents.' }) $(if ($profile -eq 'visual-ui-change') { "./.llm-wiki/wiki.ps1 brief -Intent '$escapedObjective'$pathArgument -Compact; ./.llm-wiki/wiki.ps1 ui-trace -Query '$escapedObjective'$pathArgument" } else { "./.llm-wiki/wiki.ps1 research -Intent '$escapedObjective'$pathArgument" }) $true $(if ($profile -eq 'visual-ui-change') { 'The runtime owner, UI-kit versus application boundary, scoped instructions, design-system constraints, and browser-verifiable outcomes are explicit.' } else { 'Research packet has grounded paths and the runtime owner is confirmed, or explicitly reports unresolved discovery.' })
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
    Add-Stage 'focused-verification' 'Derive and run focused component checks, then compile the frontend.' "./.llm-wiki/wiki.ps1 test-plan -Intent '$escapedObjective'; # run the focused component tests; cd FoodDiary.Web.Client && npm run build" $true 'Focused component tests and the production frontend build pass.'
    Add-Stage 'browser-evidence' 'Verify the changed rendering at the viewport(s) affected by the requested scope.' "./.llm-wiki/wiki.ps1 visual-qa -Url '<local URL>' -FixturePath '<upload fixture>' -TriggerSelector '<optional trigger>' -ResultSelector '<expected result>' -Run" $true 'Automated browser evidence proves upload, the affected viewport, visible result, screenshot, and console health; omitted viewports are explicitly out of scope.'
    Add-Stage 'completion' 'Confirm the final diff stays inside the visual owner slice and run the local completion gate.' "./.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 verify-fast -VisualUiCompletion" $true 'Actual paths remain visual-only and the local completion gate passes after focused checks and browser evidence; full verification is deferred to the enforced publication gate.'
} else {
    Add-Stage 'change-review' 'Recompute actual impact and compare the diff with intent, plan, and journeys.' $(if ($requiresWorkspace) { "./.llm-wiki/wiki.ps1 delivery-status -WorkspacePath .artifacts/llm-wiki/tasks/<task-name>; ./.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 test-plan -Intent '$escapedObjective'" } else { "./.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 test-plan -Intent '$escapedObjective'" }) $true 'Actual paths, checks, journey impact, and review obligations are known; intentional drift is replanned with a reason.'
    $verifyCommand = if ($profile -eq 'tiny') { './.llm-wiki/wiki.ps1 verify-fast' } else { './.llm-wiki/wiki.ps1 verify' }
    Add-Stage 'verification' 'Run risk-proportional deterministic verification.' $verifyCommand $true 'Required checks and Wiki verification pass.'
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
if ($visualUiChange) { $reasons.Add('Visual frontend scope has no API, provider, persistence, privacy, security, or architecture boundary change.') }
if ($uiDiscovery) { $reasons.Add('Visual intent is not grounded in concrete paths; runtime-owner discovery must precede risk classification.') }
if ($scopeDiscovery) { $reasons.Add('Feature or bug intent is not grounded in concrete paths; existing-flow research must precede critical classification and workspace creation.') }
if ($boundedCrossLayerBug) { $reasons.Add('The confirmed bug crosses layers inside one existing module flow without migration, provider, sensitive-data lifecycle, or architecture changes.') }
if ($hasCriticalEvidence) { $reasons.Add('Sensitive, provider, persistence, configuration, or delivery evidence requires the critical workflow.') }
if ($hasArchitecturalEvidence) { $reasons.Add('Architecture or durable decision evidence requires the architectural workflow.') }
if ($crossCutting) { $reasons.Add('The inferred change crosses multiple scopes or modules.') }
if ($requiresPathDiscovery) { $reasons.Add('Concrete implementation paths are not grounded yet; research must discover them before risk can be trusted.') }

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    objective = $Objective
    profile = $profile
    confidence = $confidence
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
Write-Host "Adaptive workflow: $profile ($confidence confidence)"
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
