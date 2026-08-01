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
$criticalIntent = $normalized -match '(auth|login|password|credential|token|secret|oauth|google|payment|billing|subscription|migration|database|external provider|webhook|email|invite|privacy|security)' -or (Test-IntentTerm $ruCriticalTerms)
$boundaryNegated = $normalized -match '\b(without changing|unchanged|no changes? to)\b'
$visualVocabulary = $normalized -match '\b(visual|layout|style|styling|css|scss|html|template|responsive|viewport|spacing|colour|color|icon|label|caption|annotation|overlay|dialog|modal)\b'
if ($boundaryNegated -and $visualVocabulary) {
    $criticalIntent = $false
    $architecturalIntent = $false
}
$presentationOnly = [string]$brief.risk.profile -eq 'frontend-presentation-only'
$scopeKnown = $paths.Count -gt 0 -and $scopes.Count -gt 0
$productionScopes = @($scopes | Where-Object { $_ -notin @('Tests', 'Documentation', 'Localization') })
$visualIntent = $visualVocabulary
$boundaryChangeIntent = $normalized -match '\b(change|modify|add|remove|replace|migrate|integrate|send|store|persist|log|expose)\w*\s+(api|contract|provider|privacy|security|auth|token|credential|database|migration|webhook|payload|data)\b'
$frontendOnly = $productionScopes.Count -gt 0 -and @($productionScopes | Where-Object { $_ -ne 'Frontend' }).Count -eq 0
$visualUiChange = $visualIntent -and $scopeKnown -and $frontendOnly -and -not $boundaryChangeIntent -and
    -not $flags.databaseMigration -and -not $flags.externalIntegrations -and -not $flags.configuration -and
    @($brief.architectureHealthImpact.dependencyViolations).Count -eq 0
$hasCriticalEvidence = -not $visualUiChange -and ($criticalIntent -or $privacyCount -gt 0 -or $flags.databaseMigration -or $flags.externalIntegrations -or $flags.configuration)
$hasArchitecturalEvidence = -not $visualUiChange -and ($architecturalIntent -or [bool]$brief.decisionContext.reviewRequired -or @($brief.architectureHealthImpact.dependencyViolations).Count -gt 0)
$crossCutting = $productionScopes.Count -gt 1 -or @($brief.change.directModules + $brief.change.downstreamModules | Select-Object -Unique).Count -gt 2
$wikiInternal = @($paths | Where-Object { $_ -match '^\.llm-wiki/' }).Count -gt 0

$profile = 'feature'
if ($hasCriticalEvidence) { $profile = 'critical' }
elseif ($hasArchitecturalEvidence) { $profile = 'architectural' }
elseif ($visualUiChange) { $profile = 'visual-ui-change' }
elseif ($bugIntent -and -not $crossCutting) { $profile = 'bug' }
elseif (-not $featureIntent -and [int]$brief.risk.score -le 2 -and $scopeKnown -and -not $crossCutting -and -not $wikiInternal) { $profile = 'tiny' }

$confidence = if (-not $scopeKnown) { 'low' } elseif ([string]$brief.analysis.confidence -eq 'high') { 'high' } else { 'medium' }
$requiresPathDiscovery = -not $scopeKnown
$requiresDecisionCheckpoint = $profile -in @('critical', 'architectural') -or [bool]$brief.decisionContext.reviewRequired
$requiresDesign = $profile -in @('feature', 'critical', 'architectural')
$requiresWorkspace = $profile -in @('critical', 'architectural') -or $crossCutting
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
Add-Stage 'research' $(if ($profile -eq 'visual-ui-change') { 'Trace the rendered UI path and confirm the runtime-owning component before classification.' } else { 'Compile current code paths, open questions, provider boundaries, failures, and Git precedents.' }) $(if ($profile -eq 'visual-ui-change') { "./.llm-wiki/wiki.ps1 ui-trace -Query '$escapedObjective'$pathArgument; ./.llm-wiki/wiki.ps1 research -Intent '$escapedObjective'$pathArgument" } else { "./.llm-wiki/wiki.ps1 research -Intent '$escapedObjective'$pathArgument" }) $true 'Research packet has grounded paths and the runtime owner is confirmed, or explicitly reports unresolved discovery.'
Add-Stage 'journey-impact' 'Identify affected FoodDiary user journeys and their end-to-end scenarios.' "./.llm-wiki/wiki.ps1 journeys -Intent '$escapedObjective'$pathArgument" ($profile -notin @('tiny', 'visual-ui-change')) 'Relevant journeys, negative paths, and evidence hints are included in scope.'
if ($requiresDecisionCheckpoint) {
    Add-Stage 'checkpoint' 'Resolve architectural, contract, privacy, or rollout choices before editing.' "./.llm-wiki/wiki.ps1 research -Intent '$escapedObjective'$pathArgument -Format Json" $true 'Every blocking decision is resolved or recorded as an explicit assumption.'
}
if ($requiresDesign) {
    Add-Stage 'design' 'Define target behavior, invariants, compatibility, failure behavior, and rejected alternatives.' "./.llm-wiki/wiki.ps1 design -Intent '$escapedObjective'$pathArgument -Decision '<selected choice and source evidence>'" $true 'The design checkpoint has no unresolved blocking question and its implementation phases have explicit outcomes.'
}
if ($profile -eq 'visual-ui-change') {
    Add-Stage 'acceptance' 'Define observable layout, responsive, localization, accessibility, and interaction outcomes.' '# record concise visual acceptance criteria before editing' $true 'Each requested visual outcome is explicit and browser-verifiable.'
}
if ($requiresWorkspace) {
    Add-Stage 'workspace' 'Create governed scope, acceptance, evidence, and conformance state.' "./.llm-wiki/wiki.ps1 task-start -Intent '$escapedObjective' -Criterion '<acceptance criterion>' -AllowedPath '<path regex>' -WorkspacePath .artifacts/llm-wiki/tasks/<task-name>" $true 'A task workspace exists with acceptance criteria and bounded scope.'
    Add-Stage 'requirements' 'Make every acceptance criterion atomic, mapped, and evidence-addressable before implementation.' './.llm-wiki/wiki.ps1 task-requirements-assess -WorkspacePath .artifacts/llm-wiki/tasks/<task-name> -FailOnInvalid' $true 'The requirement model has no ambiguity findings and every product outcome has an acceptance criterion.'
}
Add-Stage 'implementation' 'Implement only the selected design and declared scope.' '# edit source and tests; use task-note for decisions or blockers' $true 'Behavior and focused tests are implemented.'
if ($profile -eq 'visual-ui-change') {
    Add-Stage 'change-review' 'Confirm the actual diff stays inside the visual owner slice and derive focused checks.' "./.llm-wiki/wiki.ps1 diff; ./.llm-wiki/wiki.ps1 test-plan -Intent '$escapedObjective'; ./.llm-wiki/wiki.ps1 verify-fast -VisualUiCompletion" $true 'Actual paths remain visual-only and the local completion gate passes; full verification is deferred to the enforced publication gate.'
    Add-Stage 'focused-tests' 'Run the component tests that cover the changed runtime owner and its rendering contract.' '# run focused component test files from the test plan' $true 'Focused component tests pass.'
    Add-Stage 'build' 'Compile the frontend after the visual change.' 'cd FoodDiary.Web.Client && npm run build' $true 'The production frontend build passes.'
    Add-Stage 'browser-evidence' 'Verify the changed rendering at the viewport(s) affected by the requested scope.' '# run focused browser QA and capture visual evidence; add mobile only for responsive or mobile scope' $true 'Browser evidence proves the affected viewport, interaction, and console health; omitted viewports are explicitly out of scope.'
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

$reasons = [Collections.Generic.List[string]]::new()
if ($bugIntent) { $reasons.Add('Intent describes corrective behavior.') }
if ($featureIntent) { $reasons.Add('Intent describes new behavior.') }
if ($presentationOnly) { $reasons.Add('Changed or planned paths form a frontend presentation-only slice.') }
if ($visualUiChange) { $reasons.Add('Visual frontend scope has no API, provider, persistence, privacy, security, or architecture boundary change.') }
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
