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
    [ValidateRange(1, 30)]
    [int]$Limit = 10
)

$ErrorActionPreference = 'Stop'
$common = @{ Objective = $Objective; BaseRef = $BaseRef; Format = 'Json'; Limit = $Limit }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $common.ChangedPath = $ChangedPath }
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $common.ProposedPath = $ProposedPath }
$workflow = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') @common | ConvertFrom-Json

$scopePaths = @($workflow.inferred.paths)
function ConvertFrom-UnicodeEscape([string]$Value) { ('"' + $Value + '"') | ConvertFrom-Json }
$foodDiaryIntentAliases = @(
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u0434\u0438\u0435\u0442\u043e\u043b\u043e\u0433'); target = 'dietologist' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u043f\u0440\u0438\u0433\u043b\u0430\u0448'); target = 'invitation invite' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u043f\u0438\u0441\u044c\u043c'); target = 'email' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u0441\u0441\u044b\u043b\u043a'); target = 'link url route' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u0430\u0432\u0442\u043e\u0440\u0438\u0437'); target = 'auth authentication login' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442'); target = 'user account' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u0435\u0434\u0430'); target = 'food meal' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u0444\u043e\u0442\u043e'); target = 'photo image' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u043f\u0440\u043e\u0434\u0443\u043a\u0442'); target = 'product' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u0440\u0435\u0446\u0435\u043f\u0442'); target = 'recipe' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u043f\u043e\u0434\u043f\u0438\u0441\u043a'); target = 'subscription billing' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u043f\u043b\u0430\u0442\u0435\u0436'); target = 'payment billing' }
    [pscustomobject]@{ source = (ConvertFrom-UnicodeEscape '\u0442\u0435\u043b\u0435\u0433\u0440\u0430\u043c'); target = 'telegram' }
)
$expandedTerms = @($foodDiaryIntentAliases | Where-Object { $Objective.ToLowerInvariant().Contains($_.source) } | Select-Object -ExpandProperty target)
$contextQuery = @($Objective; $expandedTerms) -join ' '
$contextArguments = @{ Query = $contextQuery; Format = 'Json'; Limit = $Limit }
if ($scopePaths.Count -gt 0) { $contextArguments.ScopePath = $scopePaths }
$context = & (Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1') @contextArguments | ConvertFrom-Json
$precedentScopeCandidates = $scopePaths +
    @($context.implementationFiles.path | Select-Object -First $Limit) +
    @($context.symbols.path | Select-Object -First $Limit) +
    @($context.frontendSymbols.path | Select-Object -First $Limit)
$precedentScopePaths = @($precedentScopeCandidates | Where-Object { $_ } | Sort-Object -Unique)
$precedents = & (Join-Path $PSScriptRoot 'Get-LlmWikiGitPrecedents.ps1') `
    -Objective $Objective `
    -ScopePath $precedentScopePaths `
    -Limit ([Math]::Min($Limit, 8)) `
    -Format Json | ConvertFrom-Json

$failureKnowledgePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'knowledge/failures.json'
$failureKnowledge = Get-Content -LiteralPath $failureKnowledgePath -Raw | ConvertFrom-Json
$objectiveTokens = @([regex]::Matches($Objective.ToLowerInvariant(), '[\p{L}\p{Nd}]{4,}') | ForEach-Object Value | Sort-Object -Unique)
$failureMatches = @(
    foreach ($entry in @($failureKnowledge.entries)) {
        $text = ($entry | ConvertTo-Json -Depth 6 -Compress).ToLowerInvariant()
        $matches = @($objectiveTokens | Where-Object { $text.Contains($_) })
        $pathMatches = @($entry.pathPatterns | Where-Object {
            $pattern = $_
            @($scopePaths | Where-Object { $_ -match $pattern }).Count -gt 0
        })
        if ($matches.Count -eq 0 -and $pathMatches.Count -eq 0) { continue }
        [pscustomobject][ordered]@{
            id = $entry.id
            symptom = $entry.symptom
            cause = $entry.cause
            fix = $entry.fix
            verification = @($entry.verification)
            matchedTokens = $matches
            provenance = 'verified-failure-knowledge'
        }
    }
)

$implementationFiles = @($context.implementationFiles | Select-Object -First $Limit | ForEach-Object {
    [pscustomobject][ordered]@{
        path = $_.path
        score = $_.score
        provenance = if ($_.provenance) { $_.provenance } else { 'compiled-index' }
    }
})
$symbolFiles = @($context.symbols | Select-Object -First $Limit | ForEach-Object {
    [pscustomobject][ordered]@{ path = $_.path; symbol = $_.name; line = $_.line; provenance = 'compiled-symbol-index' }
})
$frontendFiles = @($context.frontendSymbols | Select-Object -First $Limit | ForEach-Object {
    [pscustomobject][ordered]@{ path = $_.path; symbol = $_.name; line = $_.line; provenance = 'compiled-frontend-index' }
})
$groundedPaths = @($scopePaths + $implementationFiles.path + $symbolFiles.path + $frontendFiles.path | Where-Object { $_ } | Sort-Object -Unique)

$openQuestions = [Collections.Generic.List[object]]::new()
if (-not $workflow.scopeKnown) {
    $openQuestions.Add([pscustomobject][ordered]@{ id = 'confirm-edit-boundary'; blocking = $true; question = 'Which ranked implementation paths form the actual edit boundary?'; evidenceNeeded = 'Read current source and confirm the entry point, implementation, and focused tests.' })
}
if ($workflow.requiresDecisionCheckpoint) {
    $openQuestions.Add([pscustomobject][ordered]@{ id = 'resolve-design-boundary'; blocking = $true; question = 'Which compatibility, privacy, provider, persistence, or architecture choice must be fixed before editing?'; evidenceNeeded = 'Record a source-grounded decision or explicit assumption in the task journal.' })
}
if (@($context.implementationFiles).Count -eq 0 -and @($context.symbols).Count -eq 0 -and @($context.frontendSymbols).Count -eq 0) {
    $openQuestions.Add([pscustomobject][ordered]@{ id = 'locate-implementation'; blocking = $true; question = 'The context index found no ranked implementation file. What exact symbol or route names the flow?'; evidenceNeeded = 'Use trace or source search and rerun research with PlannedPath.' })
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    objective = $Objective
    expandedQuery = $contextQuery
    workflow = [pscustomobject][ordered]@{
        profile = $workflow.profile
        confidence = $workflow.confidence
        requiresDecisionCheckpoint = $workflow.requiresDecisionCheckpoint
        requiresDesign = $workflow.requiresDesign
        requiresWorkspace = $workflow.requiresWorkspace
    }
    discovery = [pscustomobject][ordered]@{
        groundedPaths = $groundedPaths
        implementationFiles = $implementationFiles
        backendSymbols = $symbolFiles
        frontendSymbols = $frontendFiles
        routes = @($context.frontendRoutes | Select-Object -First $Limit)
        dependencyInjection = @($context.dependencyInjection | Select-Object -First $Limit)
        focusedTests = @($context.tests | Select-Object -First $Limit)
        guides = @($context.agentGuides | Select-Object -First $Limit)
        wikiPages = @($context.wikiPages | Select-Object -First $Limit)
    }
    boundaries = [pscustomobject][ordered]@{
        runtime = @($context.httpClients + $context.hostedServices + $context.webhooks | Where-Object { $null -ne $_ } | Select-Object -First $Limit)
        privacy = [pscustomobject][ordered]@{
            fields = @($workflow.inferred.risk.reasons | Where-Object { $_ -match 'sensitive|privacy|credential|identity|health|financial' })
            requiresReview = $workflow.profile -eq 'critical'
        }
    }
    precedents = @($precedents.precedents)
    knownFailures = $failureMatches
    openQuestions = @($openQuestions)
    readiness = [pscustomobject][ordered]@{
        readyToDesign = $groundedPaths.Count -gt 0 -and @($openQuestions | Where-Object blocking).Count -eq 0
        readyToImplement = $groundedPaths.Count -gt 0 -and -not $workflow.requiresDecisionCheckpoint
        blockers = @($openQuestions | Where-Object blocking | Select-Object -ExpandProperty id)
    }
    authority = @(
        'Current code, tests, accepted ADRs, current docs, and scoped AGENTS.md remain authoritative.'
        'Compiled indexes and Git history are navigation evidence and must be verified before implementation.'
        'Open questions are intentionally not answered by heuristics.'
    )
    nextAction = if ($groundedPaths.Count -eq 0) {
        "Run ./.llm-wiki/wiki.ps1 trace -Query '<exact command, handler, route, or component symbol>', then rerun research with -PlannedPath."
    } elseif ($workflow.requiresDecisionCheckpoint) {
        "Run ./.llm-wiki/wiki.ps1 design -Intent '$($Objective.Replace("'", "''"))' -PlannedPath '$(($groundedPaths | Select-Object -First $Limit) -join ';')'."
    } else {
        "Read the ranked current-source files, confirm the edit boundary, and follow the adaptive workflow."
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; exit 0 }
Write-Host "Research packet: $($result.workflow.profile) workflow, $($result.workflow.confidence) confidence"
Write-Host "Objective: $Objective"
Write-Host "Grounded paths: $($result.discovery.groundedPaths.Count)"
foreach ($item in $result.discovery.implementationFiles) { Write-Host "  Source: $($item.path) (score=$($item.score), $($item.provenance))" }
foreach ($item in $result.precedents) { Write-Host "  Precedent: $($item.shortHash) $($item.subject)" }
foreach ($item in $result.knownFailures) { Write-Host "  Known failure: $($item.id) - $($item.symptom)" }
foreach ($item in $result.openQuestions) { Write-Host "  OPEN [$($item.id)]: $($item.question)" }
Write-Host "Ready to design: $($result.readiness.readyToDesign)"
Write-Host "Ready to implement: $($result.readiness.readyToImplement)"
Write-Host "Next: $($result.nextAction)"
