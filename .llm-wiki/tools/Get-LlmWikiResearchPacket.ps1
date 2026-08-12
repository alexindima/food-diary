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
    [ValidateSet('Auto', 'Assessment', 'Implementation')]
    [string]$Purpose = 'Auto',
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 30)]
    [int]$Limit = 10
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiQueryCache.ps1')
$queryCacheEntry = $null
if ($Format -eq 'Json') {
    $queryCacheEntry = Get-LlmWikiQueryCacheEntry -RepositoryRoot $repositoryRoot -Namespace 'research' -Arguments @{
        Objective = $Objective; BaseRef = $BaseRef; HeadRef = $HeadRef
        ChangedPath = @($ChangedPath); ProposedPath = @($ProposedPath); Purpose = $Purpose; Limit = $Limit
    }
    $cachedResearch = Read-LlmWikiQueryCache -Entry $queryCacheEntry
    if ($null -ne $cachedResearch) { Write-Output $cachedResearch; exit 0 }
}
$common = @{ Objective = $Objective; BaseRef = $BaseRef; Format = 'Json'; Limit = $Limit }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $common.ChangedPath = $ChangedPath }
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $common.ProposedPath = $ProposedPath }
$workflow = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') @common | ConvertFrom-Json
$assessmentIntent = $Objective -match '(?i)\b(assess|assessment|audit|evaluate|evaluation|review|remaining blockers?|readiness|status)\b|\u043e\u0446\u0435\u043d|\u0430\u0443\u0434\u0438\u0442|\u0433\u043e\u0442\u043e\u0432\u043d|\u043e\u0441\u0442\u0430\u0432\u0448'
$effectivePurpose = if ($Purpose -eq 'Auto') { $(if ($assessmentIntent) { 'Assessment' } else { 'Implementation' }) } else { $Purpose }

$scopePaths = @($workflow.inferred.paths)
function Get-ObjectPropertyValues([object[]]$InputObject, [string]$Name) {
    @($InputObject | ForEach-Object {
        if ($null -ne $_ -and $_.PSObject.Properties[$Name]) { $_.$Name }
    } | Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_) })
}
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
$contextHttpClients = if ($context.PSObject.Properties['httpClients']) { @($context.httpClients) } else { @() }
$contextHostedServices = if ($context.PSObject.Properties['hostedServices']) { @($context.hostedServices) } else { @() }
$contextWebhooks = if ($context.PSObject.Properties['webhooks']) { @($context.webhooks) } else { @() }
$contextDependencyInjection = if ($context.PSObject.Properties['dependencyInjection']) { @($context.dependencyInjection) } else { @() }
$contextTests = if ($context.PSObject.Properties['tests']) { @($context.tests) } else { @() }
$contextAgentGuides = if ($context.PSObject.Properties['agentGuides']) { @($context.agentGuides) } else { @() }
$contextWikiPages = if ($context.PSObject.Properties['wikiPages']) { @($context.wikiPages) } else { @() }
$contextFrontendRoutes = if ($context.PSObject.Properties['frontendRoutes']) { @($context.frontendRoutes) } else { @() }
$precedentScopeCandidates = $scopePaths +
    @(Get-ObjectPropertyValues @($context.implementationFiles) 'path' | Select-Object -First $Limit) +
    @(Get-ObjectPropertyValues @($context.symbols) 'path' | Select-Object -First $Limit) +
    @(Get-ObjectPropertyValues @($context.frontendSymbols) 'path' | Select-Object -First $Limit)
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
    if (-not $_.PSObject.Properties['path']) { return }
    [pscustomobject][ordered]@{
        path = $_.path
        score = $(if ($_.PSObject.Properties['score']) { $_.score } else { 0 })
        provenance = $(if ($_.PSObject.Properties['provenance'] -and $_.provenance) { $_.provenance } else { 'compiled-index' })
    }
})
$symbolFiles = @($context.symbols | Select-Object -First $Limit | ForEach-Object {
    if (-not $_.PSObject.Properties['path']) { return }
    [pscustomobject][ordered]@{ path = $_.path; symbol = $(if ($_.PSObject.Properties['name']) { $_.name } else { '' }); line = $(if ($_.PSObject.Properties['line']) { $_.line } else { $null }); provenance = 'compiled-symbol-index' }
})
$frontendFiles = @($context.frontendSymbols | Select-Object -First $Limit | ForEach-Object {
    if (-not $_.PSObject.Properties['path']) { return }
    [pscustomobject][ordered]@{ path = $_.path; symbol = $(if ($_.PSObject.Properties['name']) { $_.name } else { '' }); line = $(if ($_.PSObject.Properties['line']) { $_.line } else { $null }); provenance = 'compiled-frontend-index' }
})
$groundedPaths = @($scopePaths + (Get-ObjectPropertyValues $implementationFiles 'path') + (Get-ObjectPropertyValues $symbolFiles 'path') + (Get-ObjectPropertyValues $frontendFiles 'path') | Where-Object { $_ } | Sort-Object -Unique)

$extractionDelta = $null
if ($Objective -match '(?i)IUserContextService|extraction|profile.{0,20}boundar|\u043f\u0440\u043e\u0435\u043a\u0446') {
    $currentExtraction = & (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Contract IUserContextService -Format Json | ConvertFrom-Json
    $baselineModulePage = @(& git -C $repositoryRoot show 'HEAD^:.llm-wiki/generated/modules/users.md' 2>$null)
    $baselineAvailable = $LASTEXITCODE -eq 0
    $baselineText = $baselineModulePage -join "`n"
    $initialConsumers = if ($baselineAvailable -and $baselineText -match 'Implementation-owned IUserContextService consumers: (\d+)') { [int]$Matches[1] } else { $null }
    $initialAggregate = if ($baselineAvailable -and $baselineText -match 'Consumers receiving the User aggregate: (\d+)') { [int]$Matches[1] } else { $null }
    $baselineGroups = if ($baselineAvailable) { @([regex]::Matches($baselineText, '(?m)^\| ([^|]+) \| IUserContextService \|') | ForEach-Object { $_.Groups[1].Value.Trim() } | Sort-Object -Unique) } else { @() }
    $currentGroups = @($currentExtraction.consumers | Where-Object { -not $_.compositionRegistration } | ForEach-Object consumer | Sort-Object -Unique)
    $remainingAggregateGroups = @($currentExtraction.consumers | Where-Object access -eq 'aggregate-read' | ForEach-Object consumer | Sort-Object -Unique)
    $capabilityClusters = @($currentExtraction.consumers | Group-Object {
        if ($_.compositionRegistration -or @($_.methods).Count -eq 0) { 'constructor-or-registration' }
        elseif ($_.access -eq 'mutation') { 'mutation' }
        elseif ($_.access -eq 'aggregate-read') { 'aggregate-read-and-relationship-data' }
        elseif (@($_.methods).Count -eq 1 -and $_.methods[0] -eq 'EnsureCanAccessAsync') { 'access-check-only' }
        else { 'narrow-access' }
    } | ForEach-Object { [pscustomobject][ordered]@{ capability = $_.Name; count = $_.Count; consumers = @(Get-ObjectPropertyValues @($_.Group) 'consumer' | Sort-Object -Unique); paths = @(Get-ObjectPropertyValues @($_.Group) 'path' | Sort-Object -Unique) } })
    $extractionDelta = [pscustomobject][ordered]@{
        contract = 'IUserContextService'
        baselineAvailable = $baselineAvailable
        initialConsumers = $initialConsumers
        currentConsumers = [int]$currentExtraction.readiness.productionConsumers
        resolvedConsumers = $(if ($null -ne $initialConsumers) { [Math]::Max(0, $initialConsumers - [int]$currentExtraction.readiness.productionConsumers) } else { $null })
        initialAggregateBlockers = $initialAggregate
        currentAggregateBlockers = [int]$currentExtraction.readiness.aggregateConsumers
        resolvedAggregateBlockers = $(if ($null -ne $initialAggregate) { [Math]::Max(0, $initialAggregate - [int]$currentExtraction.readiness.aggregateConsumers) } else { $null })
        removedConsumerGroups = @($baselineGroups | Where-Object { $_ -notin $currentGroups })
        nextOwner = @($remainingAggregateGroups | Where-Object { $_ -ne 'Users' } | Select-Object -First 1)
        capabilityClusters = $capabilityClusters
    }
}

$openQuestions = [Collections.Generic.List[object]]::new()
if (-not $workflow.scopeKnown) {
    $openQuestions.Add([pscustomobject][ordered]@{ id = 'confirm-edit-boundary'; blocking = $true; question = 'Which ranked implementation paths form the actual edit boundary?'; evidenceNeeded = 'Read current source and confirm the entry point, implementation, and focused tests.' })
}
if ($workflow.requiresDecisionCheckpoint) {
    $openQuestions.Add([pscustomobject][ordered]@{ id = 'resolve-design-boundary'; blocking = $effectivePurpose -eq 'Implementation'; question = 'Which compatibility, privacy, provider, persistence, or architecture choice must be fixed before editing?'; evidenceNeeded = 'Record a source-grounded decision or explicit assumption in the task journal.' })
}
if ($scopePaths.Count -eq 0 -and @($context.implementationFiles).Count -eq 0 -and @($context.symbols).Count -eq 0 -and @($context.frontendSymbols).Count -eq 0) {
    $openQuestions.Add([pscustomobject][ordered]@{ id = 'locate-implementation'; blocking = $true; question = 'The context index found no ranked implementation file. What exact symbol or route names the flow?'; evidenceNeeded = 'Use trace or source search and rerun research with PlannedPath.' })
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    objective = $Objective
    expandedQuery = $contextQuery
    workflow = [pscustomobject][ordered]@{
        purpose = $effectivePurpose
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
        routes = @($contextFrontendRoutes | Select-Object -First $Limit)
        dependencyInjection = @($contextDependencyInjection | Select-Object -First $Limit)
        focusedTests = @($contextTests | Select-Object -First $Limit)
        guides = @($contextAgentGuides | Select-Object -First $Limit)
        wikiPages = @($contextWikiPages | Select-Object -First $Limit)
    }
    researchLanes = @(
        [pscustomobject][ordered]@{ id = 'flow'; purpose = 'Current implementation and entry points'; evidenceCount = @($implementationFiles).Count + @($symbolFiles).Count + @($frontendFiles).Count; sources = @((Get-ObjectPropertyValues @($implementationFiles) 'path') + (Get-ObjectPropertyValues @($symbolFiles) 'path') + (Get-ObjectPropertyValues @($frontendFiles) 'path') | Sort-Object -Unique) }
        [pscustomobject][ordered]@{ id = 'tests'; purpose = 'Focused regression and contract evidence'; evidenceCount = @($contextTests).Count; sources = @(Get-ObjectPropertyValues @($contextTests) 'path' | Select-Object -First $Limit) }
        [pscustomobject][ordered]@{ id = 'integrations'; purpose = 'Runtime, provider, DI, and delivery boundaries'; evidenceCount = @(@($contextHttpClients) + @($contextHostedServices) + @($contextWebhooks) + @($contextDependencyInjection) | Where-Object { $null -ne $_ }).Count; sources = @(Get-ObjectPropertyValues @(@($contextHttpClients) + @($contextHostedServices) + @($contextWebhooks) + @($contextDependencyInjection)) 'path' | Sort-Object -Unique | Select-Object -First $Limit) }
        [pscustomobject][ordered]@{ id = 'precedents'; purpose = 'Git precedents and verified failure knowledge'; evidenceCount = @($precedents.precedents).Count + @($failureMatches).Count; sources = @((Get-ObjectPropertyValues @($precedents.precedents) 'shortHash') + (Get-ObjectPropertyValues @($failureMatches) 'id')) }
        [pscustomobject][ordered]@{ id = 'guidance'; purpose = 'Scoped instructions and governed Wiki context'; evidenceCount = @($contextAgentGuides).Count + @($contextWikiPages).Count; sources = @(Get-ObjectPropertyValues @(@($contextAgentGuides) + @($contextWikiPages)) 'path' | Sort-Object -Unique | Select-Object -First $Limit) }
    )
    boundaries = [pscustomobject][ordered]@{
        runtime = @(@($contextHttpClients) + @($contextHostedServices) + @($contextWebhooks) | Where-Object { $null -ne $_ } | Select-Object -First $Limit)
        privacy = [pscustomobject][ordered]@{
            fields = @($workflow.inferred.risk.reasons | Where-Object { $_ -match 'sensitive|privacy|credential|identity|health|financial' })
            requiresReview = $workflow.profile -eq 'critical'
        }
    }
    precedents = @($precedents.precedents)
    knownFailures = $failureMatches
    extractionDelta = $extractionDelta
    openQuestions = @($openQuestions)
    readiness = [pscustomobject][ordered]@{
        assessmentComplete = $groundedPaths.Count -gt 0
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
    } elseif ($effectivePurpose -eq 'Assessment') {
        'Assessment is complete. Use the blocker and boundary summary to choose the next package; no design checkpoint is required until implementation planning starts.'
    } elseif ($workflow.requiresDecisionCheckpoint) {
        "Run ./.llm-wiki/wiki.ps1 design -Intent '$($Objective.Replace("'", "''"))' -PlannedPath '$(($groundedPaths | Select-Object -First $Limit) -join ';')'."
    } else {
        "Read the ranked current-source files, confirm the edit boundary, and follow the adaptive workflow."
    }
}

if ($Format -eq 'Json') {
    $resultJson = $result | ConvertTo-Json -Depth 12
    Write-LlmWikiQueryCache -Entry $queryCacheEntry -Content $resultJson
    Write-Output $resultJson
    exit 0
}
Write-Host "Research packet: $($result.workflow.profile) workflow, $($result.workflow.confidence) confidence"
Write-Host "Purpose: $($result.workflow.purpose); assessment complete: $($result.readiness.assessmentComplete)"
Write-Host "Objective: $Objective"
Write-Host "Grounded paths: $($result.discovery.groundedPaths.Count)"
foreach ($item in @($result.discovery.implementationFiles | Select-Object -First 5)) { Write-Host "  Source: $($item.path) (score=$($item.score), $($item.provenance))" }
foreach ($item in @($result.precedents | Select-Object -First 3)) { Write-Host "  Precedent: $($item.shortHash) $($item.subject)" }
foreach ($item in $result.knownFailures) { Write-Host "  Known failure: $($item.id) - $($item.symptom)" }
foreach ($lane in $result.researchLanes) { Write-Host "  Lane $($lane.id): $($lane.evidenceCount) evidence item(s) - $($lane.purpose)" }
if ($result.extractionDelta) {
    $delta = $result.extractionDelta
    Write-Host "Extraction delta: $($delta.contract) consumers $($delta.initialConsumers) -> $($delta.currentConsumers) (resolved=$($delta.resolvedConsumers)); aggregate blockers $($delta.initialAggregateBlockers) -> $($delta.currentAggregateBlockers)."
    if (@($delta.removedConsumerGroups).Count -gt 0) { Write-Host "  Removed groups: $($delta.removedConsumerGroups -join ', ')" }
    if (@($delta.nextOwner).Count -gt 0) { Write-Host "  Next owner: $($delta.nextOwner -join ', ')" }
    foreach ($cluster in $delta.capabilityClusters) { Write-Host "  Capability $($cluster.capability): $($cluster.count) path(s), consumers=$($cluster.consumers -join ', ')" }
}
foreach ($item in $result.openQuestions) { Write-Host "  OPEN [$($item.id)]: $($item.question)" }
Write-Host "Ready to design: $($result.readiness.readyToDesign)"
Write-Host "Ready to implement: $($result.readiness.readyToImplement)"
Write-Host "Next: $($result.nextAction)"
