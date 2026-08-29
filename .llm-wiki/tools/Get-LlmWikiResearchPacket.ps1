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
    [int]$Limit = 10,
    [string]$Module,
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite',
    [switch]$Compact,
    [switch]$SkipHistory
)

$ErrorActionPreference = 'Stop'
$researchStopwatch = [Diagnostics.Stopwatch]::StartNew()
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$workspacePolicy = Get-Content -LiteralPath (Join-Path $wikiRoot 'policies/workspace-policies.json') -Raw | ConvertFrom-Json
$researchPlanningPolicy = $workspacePolicy.scheduler.researchPlanning
if ($Compact) { $Limit = [Math]::Min($Limit, 6) }
$moduleScope = @()
if (-not [string]::IsNullOrWhiteSpace($Module)) {
    foreach ($candidate in @("FoodDiary.Application/$Module", "FoodDiary.Application.$Module")) {
        if (Test-Path -LiteralPath (Join-Path $repositoryRoot $candidate) -PathType Container) { $moduleScope += $candidate }
    }
    if ($moduleScope.Count -eq 0) { throw "Research module not found: $Module" }
    if (-not $PSBoundParameters.ContainsKey('ProposedPath')) { $ProposedPath = $moduleScope }
}
Write-Host "Research [1/3]: classify and scan current sources$(if ($Module) { " for module $Module" } else { '' })..."
. (Join-Path $PSScriptRoot 'LlmWikiQueryCache.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiResearchPrimitives.ps1')
$queryCacheEntry = $null
$queryCacheEntry = Get-LlmWikiQueryCacheEntry -RepositoryRoot $repositoryRoot -Namespace 'research' -Arguments @{
    Objective = $Objective; BaseRef = $BaseRef; HeadRef = $HeadRef
    ChangedPath = @($ChangedPath); ProposedPath = @($ProposedPath); Purpose = $Purpose; Limit = $Limit; Module = $Module; CompiledIndexSource = $CompiledIndexSource; Compact = [bool]$Compact; SkipHistory = [bool]$SkipHistory
} -RelevantPath @($(if (@($ProposedPath).Count -gt 0) { $ProposedPath } else { $ChangedPath })) -DependencyPath @(
    '.llm-wiki/policies/query-indexes.json'
    '.llm-wiki/policies/workspace-policies.json'
    $(if ($CompiledIndexSource -eq 'Sqlite') { '.artifacts/llm-wiki/code-graph/code-graph.fingerprint' } else { '.llm-wiki/generated/repository-catalog.json' })
)
$cachedResearch = Read-LlmWikiQueryCache -Entry $queryCacheEntry
if ($null -ne $cachedResearch) {
    if ($Format -eq 'Json') { Write-Output $cachedResearch } else {
        $cached = $cachedResearch | ConvertFrom-Json
        Write-Host "Research cache hit: $($cached.workflow.profile), confidence=$($cached.workflow.confidence), grounded=$(@($cached.discovery.groundedPaths).Count)."
        $cachedRankedPaths = if ($cached.discovery.PSObject.Properties['rankedPaths']) {
            @($cached.discovery.rankedPaths)
        } else {
            @($cached.discovery.implementationFiles)
        }
        foreach ($item in @($cachedRankedPaths | Select-Object -First 5)) {
            Write-Host "  Ranked: $($item.path) (score=$($item.score), source=$($item.source), reason=$($item.reason))"
        }
        Write-Host "Next: $($cached.nextAction)"
    }
    exit 0
}
Write-Host "Research cache miss: $($queryCacheEntry.missReason); relevant-workspace-paths=$($queryCacheEntry.workspacePathCount)."
$common = @{ Objective = $Objective; BaseRef = $BaseRef; CompiledIndexSource = $CompiledIndexSource; Format = 'Json'; Limit = $Limit }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $common.ChangedPath = $ChangedPath }
if ($PSBoundParameters.ContainsKey('ProposedPath')) { $common.ProposedPath = $ProposedPath }
$workflow = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') @common | ConvertFrom-Json
$classificationDurationMs = [Math]::Round($researchStopwatch.Elapsed.TotalMilliseconds, 2)
$assessmentIntent = $Objective -match '(?i)\b(assess|assessment|audit|evaluate|evaluation|review|remaining blockers?|readiness|status)\b|\u043e\u0446\u0435\u043d|\u0430\u0443\u0434\u0438\u0442|\u0433\u043e\u0442\u043e\u0432\u043d|\u043e\u0441\u0442\u0430\u0432\u0448'
$implementationIntent = $Objective -match '(?i)\b(fix|implement|change|update|improve|add|remove|replace|refactor|optimize)\b|\u0438\u0441\u043f\u0440\u0430\u0432|\u0440\u0435\u0430\u043b\u0438\u0437|\u0434\u043e\u0431\u0430\u0432|\u0438\u0437\u043c\u0435\u043d|\u043e\u0431\u043d\u043e\u0432|\u0443\u043b\u0443\u0447\u0448|\u0443\u0434\u0430\u043b|\u0437\u0430\u043c\u0435\u043d|\u043e\u043f\u0442\u0438\u043c\u0438\u0437'
$effectivePurpose = if ($Purpose -eq 'Auto') { $(if ($assessmentIntent -and -not $implementationIntent) { 'Assessment' } else { 'Implementation' }) } else { $Purpose }

$scopePaths = @($workflow.inferred.paths)
function New-ResearchPlan([object[]]$Lanes) {
    $minimumSharedPaths = [Math]::Max(1, [int]$researchPlanningPolicy.minimumSharedPathsForGrouping)
    $maximumGroups = [Math]::Max(1, [int]$researchPlanningPolicy.maximumGroups)
    $maximumLanesPerGroup = [Math]::Max(1, [int]$researchPlanningPolicy.maximumLanesPerGroup)
    $groups = [Collections.Generic.List[object]]::new()
    $allReadAssignments = [Collections.Generic.List[string]]::new()

    foreach ($lane in $Lanes) {
        $readPaths = @(Get-NormalizedResearchPaths $lane.sources | Where-Object { Test-RepositoryReadPath $_ })
        foreach ($readPath in $readPaths) { $allReadAssignments.Add($readPath) }
        $lane | Add-Member -NotePropertyName readPaths -NotePropertyValue $readPaths -Force
        $bestGroup = $null
        $bestOverlap = 0
        foreach ($group in $groups) {
            if (@($group.laneIds).Count -ge $maximumLanesPerGroup) { continue }
            $overlap = Get-SharedPathCount $readPaths @($group.readPaths)
            if ($overlap -ge $minimumSharedPaths -and $overlap -gt $bestOverlap) {
                $bestGroup = $group
                $bestOverlap = $overlap
            }
        }
        if ($null -eq $bestGroup) {
            $groups.Add([pscustomobject][ordered]@{
                laneIds = @([string]$lane.id)
                readPaths = $readPaths
                evidenceCount = [int]$lane.evidenceCount
                groupingReason = 'independent-source-set'
            })
            continue
        }
        $bestGroup.laneIds = @($bestGroup.laneIds + [string]$lane.id)
        $bestGroup.readPaths = @(Get-NormalizedResearchPaths @($bestGroup.readPaths + $readPaths))
        $bestGroup.evidenceCount = [int]$bestGroup.evidenceCount + [int]$lane.evidenceCount
        $bestGroup.groupingReason = "shared-at-least-$minimumSharedPaths-paths"
    }

    if ($groups.Count -gt $maximumGroups) {
        $kept = @($groups | Select-Object -First ($maximumGroups - 1))
        $overflow = @($groups | Select-Object -Skip ($maximumGroups - 1))
        $kept += [pscustomobject][ordered]@{
            laneIds = @($overflow.laneIds | ForEach-Object { @($_) } | Sort-Object -Unique)
            readPaths = @(Get-NormalizedResearchPaths @($overflow.readPaths | ForEach-Object { @($_) }))
            evidenceCount = [int](($overflow | Measure-Object -Property evidenceCount -Sum).Sum)
            groupingReason = 'policy-cap-overflow'
        }
        $groups = [Collections.Generic.List[object]]::new()
        foreach ($group in $kept) { $groups.Add($group) }
    }

    $readSet = @(Get-NormalizedResearchPaths @($allReadAssignments))
    $compiledGroups = @(
        for ($index = 0; $index -lt $groups.Count; $index++) {
            $group = $groups[$index]
            $otherPaths = @($groups | Where-Object { $_ -ne $group } | ForEach-Object { @($_.readPaths) })
            [pscustomobject][ordered]@{
                id = 'RG-{0:D3}' -f ($index + 1)
                laneIds = @($group.laneIds)
                readPaths = @($group.readPaths)
                evidenceCount = [int]$group.evidenceCount
                parallelEligible = (Get-SharedPathCount @($group.readPaths) $otherPaths) -eq 0
                groupingReason = [string]$group.groupingReason
            }
        }
    )
    [pscustomobject][ordered]@{
        schemaVersion = 1
        policy = [pscustomobject][ordered]@{
            minimumSharedPathsForGrouping = $minimumSharedPaths
            maximumGroups = $maximumGroups
            maximumLanesPerGroup = $maximumLanesPerGroup
        }
        groups = $compiledGroups
        readSet = $readSet
        laneCount = @($Lanes).Count
        groupCount = $compiledGroups.Count
        totalReadAssignments = $allReadAssignments.Count
        uniqueReadPathCount = $readSet.Count
        duplicateReadSavings = [Math]::Max(0, $allReadAssignments.Count - $readSet.Count)
        executionHint = 'Groups describe reusable read sets. Executors may run them sequentially or in parallel without changing the contract.'
    }
}
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
$contextArguments = @{ Query = $contextQuery; CompiledIndexSource = $CompiledIndexSource; Format = 'Json'; Limit = $Limit }
if ($scopePaths.Count -gt 0) { $contextArguments.ScopePath = $scopePaths }
$context = & (Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1') @contextArguments | ConvertFrom-Json
$contextReadyDurationMs = [Math]::Round($researchStopwatch.Elapsed.TotalMilliseconds, 2)
Write-Host 'Research [2/3]: current-source context ready.'
$contextHttpClients = @(if ($context.PSObject.Properties['httpClients']) { @($context.httpClients) } else { @() })
$contextHostedServices = @(if ($context.PSObject.Properties['hostedServices']) { @($context.hostedServices) } else { @() })
$contextWebhooks = @(if ($context.PSObject.Properties['webhooks']) { @($context.webhooks) } else { @() })
$contextDependencyInjection = @(if ($context.PSObject.Properties['dependencyInjection']) { @($context.dependencyInjection) } else { @() })
$contextTests = @(if ($context.PSObject.Properties['tests']) { @($context.tests) } else { @() })
$contextAgentGuides = @(if ($context.PSObject.Properties['agentGuides']) { @($context.agentGuides) } else { @() })
$contextWikiPages = @(if ($context.PSObject.Properties['wikiPages']) { @($context.wikiPages) } else { @() })
$contextFrontendRoutes = @(if ($context.PSObject.Properties['frontendRoutes']) { @($context.frontendRoutes) } else { @() })
$precedentScopeCandidates = $scopePaths +
    @(Get-ObjectPropertyValues @($context.implementationFiles) 'path' | Select-Object -First $Limit) +
    @(Get-ObjectPropertyValues @($context.symbols) 'path' | Select-Object -First $Limit) +
    @(Get-ObjectPropertyValues @($context.frontendSymbols) 'path' | Select-Object -First $Limit)
$precedentScopePaths = @($precedentScopeCandidates | Where-Object { $_ } | Sort-Object -Unique)
$repositoryAssessmentResearch = [string]$workflow.profile -eq 'repository-assessment'
$wikiInternalResearch = $Objective -match '(?i)\b(llm[- ]?wiki|wiki\.ps1|wiki tooling|development mcp)\b' -or
    (@($ProposedPath).Count -gt 0 -and @($ProposedPath | Where-Object { ([string]$_).Replace('\', '/') -notmatch '^\.llm-wiki/' }).Count -eq 0)
$historyDeferred = $Compact -or $SkipHistory -or [string]$workflow.profile -eq 'test-only' -or $repositoryAssessmentResearch -or $wikiInternalResearch
$precedents = if ($historyDeferred) {
    $deferredReason = if ($SkipHistory) { 'the explicit -SkipHistory option' } elseif ([string]$workflow.profile -eq 'test-only') { 'the test-only fast path' } elseif ($repositoryAssessmentResearch) { 'the repository-assessment fast path' } elseif ($wikiInternalResearch) { 'the Wiki-tooling fast path' } else { 'compact research' }
    Write-Host "Research [3/3]: Git precedents deferred by $deferredReason."
    [pscustomobject]@{ precedents = @(); confidence = 'deferred'; authority = "Historical precedent analysis was deferred by $deferredReason; use wiki.ps1 precedents when history is materially useful." }
} else {
    Write-Host 'Research [3/3]: scan bounded Git precedents...'
    & (Join-Path $PSScriptRoot 'Get-LlmWikiGitPrecedents.ps1') `
        -Objective $Objective `
        -ScopePath $precedentScopePaths `
        -Limit ([Math]::Min($Limit, 8)) `
        -Format Json | ConvertFrom-Json
}

$failureKnowledgePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'knowledge/failures.json'
$failureKnowledge = Get-Content -LiteralPath $failureKnowledgePath -Raw | ConvertFrom-Json
$failureStopwords = @('fooddiary', 'project', 'service', 'repository', 'current', 'change', 'changes')
$objectiveTokens = @([regex]::Matches($Objective.ToLowerInvariant(), '[\p{L}\p{Nd}]{4,}') | ForEach-Object Value | Where-Object { $_ -notin $failureStopwords } | Sort-Object -Unique)
$failureMatches = @(
    foreach ($entry in @($failureKnowledge.entries)) {
        $text = ($entry | ConvertTo-Json -Depth 6 -Compress).ToLowerInvariant()
        $matches = @($objectiveTokens | Where-Object { $text.Contains($_) })
        $pathMatches = @($entry.pathPatterns | Where-Object {
            $pattern = $_
            @($scopePaths | Where-Object { $_ -match $pattern }).Count -gt 0
        })
        if ($matches.Count -lt 2 -and $pathMatches.Count -eq 0) { continue }
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

$explicitPlannedFiles = @($ProposedPath | ForEach-Object {
    if ([string]::IsNullOrWhiteSpace([string]$_)) { return }
    $normalized = ([string]$_).Replace('\', '/').TrimEnd('/')
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot $normalized) -PathType Leaf)) { return }
    [pscustomobject][ordered]@{ path = $normalized; score = 1000; provenance = 'explicit-planned-path'; reason = 'Caller supplied this exact file as a planned path.' }
})
$implementationFiles = @(@($explicitPlannedFiles) + @($context.implementationFiles | ForEach-Object {
    if (-not $_.PSObject.Properties['path']) { return }
    [pscustomobject][ordered]@{
        path = $_.path
        score = $(if ($_.PSObject.Properties['score']) { $_.score } else { 0 })
        provenance = $(if ($_.PSObject.Properties['provenance'] -and $_.provenance) { $_.provenance } else { 'compiled-index' })
        reason = $(if ($_.PSObject.Properties['reason'] -and $_.reason) { $_.reason } else { "Current-source context ranking score $($(if ($_.PSObject.Properties['score']) { $_.score } else { 0 }))." })
    }
}) | Group-Object path | ForEach-Object { $_.Group | Sort-Object score -Descending | Select-Object -First 1 } | Select-Object -First $Limit)
$symbolFiles = @($context.symbols | Select-Object -First $Limit | ForEach-Object {
    if (-not $_.PSObject.Properties['path']) { return }
    [pscustomobject][ordered]@{ path = $_.path; symbol = $(if ($_.PSObject.Properties['name']) { $_.name } else { '' }); line = $(if ($_.PSObject.Properties['line']) { $_.line } else { $null }); score = $(if ($_.PSObject.Properties['score']) { $_.score } else { 0 }); provenance = 'compiled-symbol-index'; reason = 'Backend symbol name/path matched the expanded research query.' }
})
$frontendFiles = @($context.frontendSymbols | Select-Object -First $Limit | ForEach-Object {
    if (-not $_.PSObject.Properties['path']) { return }
    [pscustomobject][ordered]@{ path = $_.path; symbol = $(if ($_.PSObject.Properties['name']) { $_.name } else { '' }); line = $(if ($_.PSObject.Properties['line']) { $_.line } else { $null }); score = $(if ($_.PSObject.Properties['score']) { $_.score } else { 0 }); provenance = 'compiled-frontend-index'; reason = 'Frontend symbol name/path matched the expanded research query.' }
})
$runtimeFlowEvidence = [pscustomobject][ordered]@{
    status = 'not-requested'
    sourcePaths = @()
    downstreamConsumers = @()
    dependencies = @()
    confidence = 'not-rated'
}
if (($repositoryAssessmentResearch -or $wikiInternalResearch) -and @($ProposedPath).Count -gt 0) {
    $runtimeFlowEvidence = [pscustomobject][ordered]@{
        status = $(if ($repositoryAssessmentResearch) { 'deferred-repository-assessment' } else { 'deferred-wiki-tooling' })
        sourcePaths = @($ProposedPath)
        downstreamConsumers = @()
        dependencies = @()
        confidence = 'not-rated'
        diagnostic = 'Generic runtime graph expansion was skipped because it is not a reliable evidence source for repository-assessment or Wiki-tooling objectives.'
    }
} elseif (@($ProposedPath).Count -gt 0 -and $CompiledIndexSource -eq 'Sqlite') {
    try {
        $graphResearch = & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphResearch.ps1') `
            -Objective $Objective `
            -ProposedPath @($ProposedPath) `
            -Limit $Limit `
            -Format Json | ConvertFrom-Json
        $runtimeFlowEvidence = [pscustomobject][ordered]@{
            status = 'available'
            sourcePaths = @($graphResearch.matchedPaths)
            downstreamConsumers = @($graphResearch.downstreamConsumers | Select-Object -First $Limit)
            dependencies = @($graphResearch.dependencies | Select-Object -First $Limit)
            confidence = [string]$graphResearch.confidence
        }
    } catch {
        $runtimeFlowEvidence = [pscustomobject][ordered]@{
            status = 'unavailable'
            sourcePaths = @($ProposedPath)
            downstreamConsumers = @()
            dependencies = @()
            confidence = 'low'
            diagnostic = $_.Exception.Message
            recoveryCommand = './.llm-wiki/wiki.ps1 graph-build; rerun research with the same -PlannedPath'
        }
    }
} elseif (@($ProposedPath).Count -gt 0) {
    $runtimeFlowEvidence = [pscustomobject][ordered]@{
        status = 'not-requested-json-baseline'
        sourcePaths = @($ProposedPath)
        downstreamConsumers = @()
        dependencies = @()
        confidence = 'not-rated'
        diagnostic = 'Runtime graph expansion is intentionally skipped for the explicit JSON baseline.'
    }
}

if (@($ProposedPath).Count -gt 0) {
    $normalizedPlannedPaths = @($ProposedPath | Where-Object { $_ } | ForEach-Object { ([string]$_).Replace('\', '/').TrimEnd('/') })
    $graphEvidencePaths = @(
        @($runtimeFlowEvidence.sourcePaths) +
        @(Get-ObjectPropertyValues @($runtimeFlowEvidence.downstreamConsumers) 'path') +
        @(Get-ObjectPropertyValues @($runtimeFlowEvidence.dependencies) 'path') |
            Where-Object { $_ } |
            ForEach-Object { ([string]$_).Replace('\', '/') } |
            Sort-Object -Unique
    )
    function Test-ScopedResearchPath([string]$CandidatePath, [switch]$AllowAncestor) {
        if ([string]::IsNullOrWhiteSpace($CandidatePath)) { return $false }
        $candidate = $CandidatePath.Replace('\', '/').TrimEnd('/')
        if ($candidate -in $graphEvidencePaths) { return $true }
        foreach ($plannedPath in $normalizedPlannedPaths) {
            if ($candidate -eq $plannedPath -or $candidate.StartsWith("$plannedPath/", [StringComparison]::OrdinalIgnoreCase)) { return $true }
            if ($AllowAncestor -and ($candidate -eq 'AGENTS.md' -or $plannedPath.StartsWith("$candidate/", [StringComparison]::OrdinalIgnoreCase))) { return $true }
        }
        return $false
    }
    $contextFrontendRoutes = @($contextFrontendRoutes | Where-Object {
        $_.PSObject.Properties['path'] -and (Test-ScopedResearchPath ([string]$_.path))
    })
    $implementationFiles = @($implementationFiles | Where-Object {
        $_.PSObject.Properties['path'] -and (Test-ScopedResearchPath ([string]$_.path))
    })
    $symbolFiles = @($symbolFiles | Where-Object {
        $_.PSObject.Properties['path'] -and (Test-ScopedResearchPath ([string]$_.path))
    })
    $frontendFiles = @($frontendFiles | Where-Object {
        $_.PSObject.Properties['path'] -and (Test-ScopedResearchPath ([string]$_.path))
    })
    $contextTests = @($contextTests | Where-Object {
        $_.PSObject.Properties['path'] -and (Test-ScopedResearchPath ([string]$_.path))
    })
    $contextDependencyInjection = @($contextDependencyInjection | Where-Object {
        $_.PSObject.Properties['path'] -and (Test-ScopedResearchPath ([string]$_.path))
    })
    $contextHttpClients = @($contextHttpClients | Where-Object {
        $_.PSObject.Properties['path'] -and (Test-ScopedResearchPath ([string]$_.path))
    })
    $contextHostedServices = @($contextHostedServices | Where-Object {
        $_.PSObject.Properties['path'] -and (Test-ScopedResearchPath ([string]$_.path))
    })
    $contextWebhooks = @($contextWebhooks | Where-Object {
        $_.PSObject.Properties['path'] -and (Test-ScopedResearchPath ([string]$_.path))
    })
    $contextAgentGuides = @($contextAgentGuides | Where-Object {
        $_.PSObject.Properties['path'] -and (Test-ScopedResearchPath ([string]$_.path) -AllowAncestor)
    })
}
$rankedPaths = @(
    @($implementationFiles | ForEach-Object {
        [pscustomobject][ordered]@{ path = $_.path; score = $_.score; source = $_.provenance; reason = $_.reason }
    }) +
    @($symbolFiles | ForEach-Object {
        [pscustomobject][ordered]@{ path = $_.path; score = $_.score; source = $_.provenance; reason = $_.reason }
    }) +
    @($frontendFiles | ForEach-Object {
        [pscustomobject][ordered]@{ path = $_.path; score = $_.score; source = $_.provenance; reason = $_.reason }
    }) |
        Group-Object path |
        ForEach-Object { $_.Group | Sort-Object score -Descending | Select-Object -First 1 } |
        Sort-Object @{ Expression = 'score'; Descending = $true }, path |
        Select-Object -First $Limit
)
$groundedPaths = @(Get-NormalizedResearchPaths @(
    $scopePaths +
    (Get-ObjectPropertyValues $implementationFiles 'path') +
    (Get-ObjectPropertyValues $symbolFiles 'path') +
    (Get-ObjectPropertyValues $frontendFiles 'path') +
    @($runtimeFlowEvidence.sourcePaths)
))

$extractionDelta = $null
if ($Objective -match '(?i)IUserContextService|extraction|profile.{0,20}boundar|\u043f\u0440\u043e\u0435\u043a\u0446') {
    $currentExtraction = & (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Contract IUserContextService -Format Json | ConvertFrom-Json
    $baselineShowResult = Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('show', 'HEAD^:.llm-wiki/generated/modules/users.md') -AllowedExitCode @(0, 128)
    $baselineAvailable = $baselineShowResult.ExitCode -eq 0
    $baselineText = $baselineShowResult.StandardOutput
    $initialConsumers = if ($baselineAvailable -and $baselineText -match 'Implementation-owned IUserContextService consumers: (\d+)') { [int]$Matches[1] } else { $null }
    $initialAggregate = if ($baselineAvailable -and $baselineText -match 'Consumers receiving the User aggregate: (\d+)') { [int]$Matches[1] } else { $null }
    $baselineGroups = @(if ($baselineAvailable) { @([regex]::Matches($baselineText, '(?m)^\| ([^|]+) \| IUserContextService \|') | ForEach-Object { $_.Groups[1].Value.Trim() } | Sort-Object -Unique) } else { @() })
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

function Get-QuestionAnchor([switch]$AllowMissing) {
    $candidate = @($symbolFiles + $frontendFiles | Where-Object { $_.path } | Select-Object -First 1)
    if ($candidate.Count -eq 0) {
        $candidate = @($implementationFiles | Where-Object { $_.path } | Select-Object -First 1)
    }
    if ($candidate.Count -eq 0) {
        if ($AllowMissing) { return [pscustomobject][ordered]@{ status = 'missing'; path = $null; line = $null; symbol = $null } }
        return $null
    }
    $item = $candidate[0]
    $line = if ($item.PSObject.Properties['line'] -and $null -ne $item.line -and [int]$item.line -gt 0) { [int]$item.line } else { $null }
    [pscustomobject][ordered]@{
        status = $(if ($null -ne $line) { 'line' } else { 'path' })
        path = [string]$item.path
        line = $line
        symbol = $(if ($item.PSObject.Properties['symbol'] -and $item.symbol) { [string]$item.symbol } else { $null })
    }
}
$openQuestions = [Collections.Generic.List[object]]::new()
if (-not $workflow.scopeKnown -and $effectivePurpose -eq 'Implementation') {
    $openQuestions.Add((New-GroundedQuestion `
        -Id 'confirm-edit-boundary' `
        -Blocking $true `
        -Question 'Which ranked implementation paths form the actual edit boundary?' `
        -EvidenceNeeded 'Read current source and confirm the entry point, implementation, and focused tests.' `
        -WhyUserInputIsRequired 'The repository evidence identifies candidates, but choosing the intended edit boundary changes implementation scope.' `
        -Anchor (Get-QuestionAnchor -AllowMissing) `
        -ResolutionCommand "./.llm-wiki/wiki.ps1 research -Intent '$($Objective.Replace("'", "''"))' -ResearchPurpose Implementation -PlannedPath '<confirmed paths>'"))
}
if ($workflow.requiresDecisionCheckpoint) {
    $escapedObjective = $Objective.Replace("'", "''")
    $plannedArgument = if ($groundedPaths.Count -gt 0) { " -PlannedPath '$(($groundedPaths | Select-Object -First $Limit) -join ';')'" } else { '' }
    $openQuestions.Add((New-GroundedQuestion `
        -Id 'resolve-design-boundary' `
        -Blocking ($effectivePurpose -eq 'Implementation') `
        -Question 'Select and record the compatibility, privacy, provider, persistence, or architecture boundary that the implementation must preserve.' `
        -EvidenceNeeded 'A source-grounded decision naming the selected boundary, rejected alternative, and affected consumers.' `
        -WhyUserInputIsRequired 'Repository evidence can expose the boundary and alternatives but cannot choose the product or compatibility tradeoff.' `
        -Anchor (Get-QuestionAnchor -AllowMissing) `
        -ResolutionCommand "./.llm-wiki/wiki.ps1 design -Intent '$escapedObjective'$plannedArgument -Decision '<selected boundary; rejected alternative; affected consumers>'"))
}
if ($groundedPaths.Count -eq 0 -and -not $repositoryAssessmentResearch) {
    $openQuestions.Add((New-GroundedQuestion `
        -Id 'locate-implementation' `
        -Blocking $true `
        -Question 'What exact symbol, route, command, or component names the flow?' `
        -EvidenceNeeded 'Use trace or source search and rerun research with PlannedPath.' `
        -WhyUserInputIsRequired 'No current repository path was grounded, so asking for a concrete entry point is safer than inferring one.' `
        -Anchor $null `
        -ResolutionCommand "./.llm-wiki/wiki.ps1 trace -Query '<exact symbol or route>'"))
}
$nextQuestion = @($openQuestions | Sort-Object @{ Expression = { if ($_.blocking) { 0 } else { 1 } } }, id | Select-Object -First 1)
$researchDiscoveryConfidence = if ($repositoryAssessmentResearch) { 'medium' } elseif ($groundedPaths.Count -gt 0) { 'high' } else { 'low' }
$researchBlockerConfidence = if ($groundedPaths.Count -gt 0 -and $extractionDelta) { 'high' } elseif ($groundedPaths.Count -gt 0) { 'medium' } else { 'low' }
$researchImplementationScopeConfidence = if ($effectivePurpose -eq 'Assessment') { 'not-required' } elseif ($workflow.scopeKnown) { 'high' } elseif ($groundedPaths.Count -gt 0) { 'medium' } else { 'low' }
$researchConfidence = if ($effectivePurpose -eq 'Assessment') { $researchDiscoveryConfidence } elseif ($researchImplementationScopeConfidence -eq 'high') { 'high' } elseif ($groundedPaths.Count -gt 0) { 'medium' } else { 'low' }
$researchConfidenceReasons = [Collections.Generic.List[string]]::new()
if ($repositoryAssessmentResearch) { $researchConfidenceReasons.Add('Repository assessment intentionally preserves multiple evidence lanes instead of inventing one feature edit boundary.') }
elseif ($researchDiscoveryConfidence -eq 'high') { $researchConfidenceReasons.Add("Discovery is grounded in $($groundedPaths.Count) current repository path(s).") }
else { $researchConfidenceReasons.Add('Discovery is low because no current repository path was grounded.') }
if ($researchBlockerConfidence -eq 'high') { $researchConfidenceReasons.Add('Blocker count is backed by contract extraction analysis plus grounded source paths.') }
elseif ($researchBlockerConfidence -eq 'medium') { $researchConfidenceReasons.Add('Blocker count is provisional because research found the flow but did not run a boundary-specific blocker analyzer.') }
if ($researchImplementationScopeConfidence -eq 'not-required') { $researchConfidenceReasons.Add('Implementation scope is not rated because this is a read-only assessment.') }
elseif ($researchImplementationScopeConfidence -ne 'high') { $researchConfidenceReasons.Add('Implementation scope is not high because the discovered paths were not confirmed as the future edit boundary.') }

$guidanceSources = @(Get-NormalizedResearchPaths @(
    Get-ObjectPropertyValues @($explicitPlannedFiles | Where-Object {
        $_.path -match '(^|/)AGENTS\.md$' -or $_.path -match '^\.llm-wiki/'
    }) 'path'
    Get-ObjectPropertyValues @(@($contextAgentGuides) + @($contextWikiPages)) 'path'
) | Select-Object -First $Limit)
$researchLanes = @(
    [pscustomobject][ordered]@{ id = 'flow'; purpose = 'Current implementation and entry points'; evidenceCount = @($implementationFiles).Count + @($symbolFiles).Count + @($frontendFiles).Count; sources = @(Get-NormalizedResearchPaths @((Get-ObjectPropertyValues @($implementationFiles) 'path') + (Get-ObjectPropertyValues @($symbolFiles) 'path') + (Get-ObjectPropertyValues @($frontendFiles) 'path'))) }
    [pscustomobject][ordered]@{ id = 'tests'; purpose = 'Focused regression and contract evidence'; evidenceCount = @($contextTests).Count; sources = @(Get-NormalizedResearchPaths (Get-ObjectPropertyValues @($contextTests) 'path') | Select-Object -First $Limit) }
    [pscustomobject][ordered]@{ id = 'integrations'; purpose = 'Runtime, provider, DI, and delivery boundaries'; evidenceCount = @(@($contextHttpClients) + @($contextHostedServices) + @($contextWebhooks) + @($contextDependencyInjection) | Where-Object { $null -ne $_ }).Count; sources = @(Get-NormalizedResearchPaths (Get-ObjectPropertyValues @(@($contextHttpClients) + @($contextHostedServices) + @($contextWebhooks) + @($contextDependencyInjection)) 'path') | Select-Object -First $Limit) }
    [pscustomobject][ordered]@{ id = 'precedents'; purpose = 'Git precedents and verified failure knowledge'; evidenceCount = @($precedents.precedents).Count + @($failureMatches).Count; sources = @((Get-ObjectPropertyValues @($precedents.precedents) 'shortHash') + (Get-ObjectPropertyValues @($failureMatches) 'id') | Where-Object { $_ } | Sort-Object -Unique) }
    [pscustomobject][ordered]@{ id = 'guidance'; purpose = 'Scoped instructions and governed Wiki context'; evidenceCount = $guidanceSources.Count; sources = $guidanceSources }
) | Where-Object { [int]$_.evidenceCount -gt 0 -or @($_.sources).Count -gt 0 }
$researchPlan = New-ResearchPlan $researchLanes

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    objective = $Objective
    expandedQuery = $contextQuery
    workflow = [pscustomobject][ordered]@{
        purpose = $effectivePurpose
        profile = $workflow.profile
        confidence = $researchConfidence
        routingConfidence = $workflow.confidence
        confidenceDimensions = [pscustomobject][ordered]@{
            discovery = $researchDiscoveryConfidence
            blockerCount = $researchBlockerConfidence
            implementationScope = $researchImplementationScopeConfidence
        }
        confidenceReasons = @($researchConfidenceReasons)
        requiresDecisionCheckpoint = $workflow.requiresDecisionCheckpoint
        requiresDesign = $workflow.requiresDesign
        requiresWorkspace = $workflow.requiresWorkspace
    }
    discovery = [pscustomobject][ordered]@{
        groundedPaths = $groundedPaths
        rankedPaths = $rankedPaths
        implementationFiles = $implementationFiles
        backendSymbols = $symbolFiles
        frontendSymbols = $frontendFiles
        routes = @($contextFrontendRoutes | Select-Object -First $Limit)
        dependencyInjection = @($contextDependencyInjection | Select-Object -First $Limit)
        focusedTests = @($contextTests | Select-Object -First $Limit)
        guides = @($contextAgentGuides | Select-Object -First $Limit)
        wikiPages = @($contextWikiPages | Select-Object -First $Limit)
        runtimeFlow = $runtimeFlowEvidence
    }
    researchLanes = $researchLanes
    researchPlan = $researchPlan
    boundaries = [pscustomobject][ordered]@{
        runtime = @(@($contextHttpClients) + @($contextHostedServices) + @($contextWebhooks) | Where-Object { $null -ne $_ } | Select-Object -First $Limit)
        privacy = [pscustomobject][ordered]@{
            fields = @($workflow.inferred.risk.reasons | Where-Object { $_ -match 'sensitive|privacy|credential|identity|health|financial' })
            requiresReview = $workflow.profile -eq 'critical'
        }
    }
    precedents = @($precedents.precedents | Select-Object -First $Limit)
    knownFailures = @($failureMatches | Select-Object -First $Limit)
    extractionDelta = $extractionDelta
    openQuestions = @($openQuestions)
    nextQuestion = $(if ($nextQuestion.Count -gt 0) { $nextQuestion[0] } else { $null })
    readiness = [pscustomobject][ordered]@{
        assessmentStatus = $(if ($repositoryAssessmentResearch -or $groundedPaths.Count -gt 0) { 'complete' } else { 'incomplete' })
        designCheckpoint = $(if ($effectivePurpose -eq 'Assessment') { 'not-required' } elseif ($workflow.requiresDecisionCheckpoint) { 'required' } else { 'not-required' })
        implementationStatus = $(if ($effectivePurpose -eq 'Assessment') { 'not-applicable' } elseif ($groundedPaths.Count -gt 0 -and -not $workflow.requiresDecisionCheckpoint) { 'ready' } else { 'blocked' })
        assessmentComplete = $repositoryAssessmentResearch -or $groundedPaths.Count -gt 0
        readyToDesign = $effectivePurpose -eq 'Assessment' -or ($groundedPaths.Count -gt 0 -and @($openQuestions | Where-Object blocking).Count -eq 0)
        readyToImplement = $effectivePurpose -ne 'Assessment' -and $groundedPaths.Count -gt 0 -and -not $workflow.requiresDecisionCheckpoint
        blockers = @($openQuestions | Where-Object blocking | Select-Object -ExpandProperty id)
    }
    authority = @(
        'Current code, tests, accepted ADRs, current docs, and scoped AGENTS.md remain authoritative.'
        'Compiled indexes and Git history are navigation evidence and must be verified before implementation.'
        'Open questions are intentionally not answered by heuristics.'
    )
    diagnostics = [pscustomobject][ordered]@{
        classificationDurationMs = $classificationDurationMs
        contextReadyDurationMs = $contextReadyDurationMs
        totalDurationMs = [Math]::Round($researchStopwatch.Elapsed.TotalMilliseconds, 2)
        historyDeferred = $historyDeferred
        runtimeGraphDeferred = $runtimeFlowEvidence.status -like 'deferred-*'
        progressContract = 'Text progress milestones are emitted after classification/context and before optional Git history; JSON is emitted atomically when the packet is complete.'
    }
    nextAction = if ($repositoryAssessmentResearch) {
        'Continue the repository assessment with topology, privacy, security, health, quality, dependency, journey, and test-plan readers; validate every reportable lead in current source and tests.'
    } elseif ($groundedPaths.Count -eq 0) {
        "Run ./.llm-wiki/wiki.ps1 trace -Query '<exact command, handler, route, or component symbol>', then rerun research with -PlannedPath."
    } elseif ($effectivePurpose -eq 'Assessment') {
        'Assessment is complete. Use the blocker and boundary summary to choose the next package; no design checkpoint is required until implementation planning starts.'
    } elseif ($workflow.requiresDecisionCheckpoint) {
        "Run ./.llm-wiki/wiki.ps1 design -Intent '$($Objective.Replace("'", "''"))' -PlannedPath '$(($groundedPaths | Select-Object -First $Limit) -join ';')'."
    } else {
        "Read the ranked current-source files, confirm the edit boundary, and follow the adaptive workflow."
    }
}

$result | Add-Member -NotePropertyName outputContract -NotePropertyValue ([pscustomobject][ordered]@{
    compact = [bool]$Compact
    limit = $Limit
    maxCharacters = $(if ($Compact) { 30000 } else { $null })
    truncated = $false
})
$resultJson = ConvertTo-LlmWikiJsonSafeObject $result | ConvertTo-Json -Depth 12
if ($Compact -and $resultJson.Length -gt 30000) {
    $result.outputContract.truncated = $true
    $compactLimit = [Math]::Min(3, $Limit)
    $result.discovery.runtimeFlow.downstreamConsumers = @($result.discovery.runtimeFlow.downstreamConsumers | Select-Object -First $compactLimit)
    $result.discovery.runtimeFlow.dependencies = @($result.discovery.runtimeFlow.dependencies | Select-Object -First $compactLimit)
    $result.discovery.routes = @($result.discovery.routes | Select-Object -First $compactLimit)
    $result.discovery.dependencyInjection = @($result.discovery.dependencyInjection | Select-Object -First $compactLimit)
    $result.discovery.focusedTests = @($result.discovery.focusedTests | Select-Object -First $compactLimit)
    $result.discovery.guides = @($result.discovery.guides | Select-Object -First $compactLimit)
    $result.discovery.wikiPages = @($result.discovery.wikiPages | Select-Object -First $compactLimit)
    $result.discovery.rankedPaths = @($result.discovery.rankedPaths | Select-Object -First $compactLimit)
    $result.precedents = @($result.precedents | Select-Object -First $compactLimit)
    $result.knownFailures = @($result.knownFailures | Select-Object -First $compactLimit)
    foreach ($lane in $result.researchLanes) { $lane.sources = @($lane.sources | Select-Object -First $compactLimit) }
    $result.researchPlan.readSet = @($result.researchPlan.readSet | Select-Object -First ($compactLimit * 3))
    foreach ($group in $result.researchPlan.groups) { $group.readPaths = @($group.readPaths | Select-Object -First $compactLimit) }
    $result.boundaries.runtime = @($result.boundaries.runtime | Select-Object -First $compactLimit)
    $result.extractionDelta = $null
    $resultJson = ConvertTo-LlmWikiJsonSafeObject $result | ConvertTo-Json -Depth 12
}
if ($Compact -and $resultJson.Length -gt 30000) {
    $result.discovery.runtimeFlow.downstreamConsumers = @($result.discovery.runtimeFlow.downstreamConsumers | Select-Object -First 1)
    $result.discovery.runtimeFlow.dependencies = @($result.discovery.runtimeFlow.dependencies | Select-Object -First 1)
    $result.precedents = @()
    $result.knownFailures = @()
    $result.boundaries.runtime = @()
    $resultJson = ConvertTo-LlmWikiJsonSafeObject $result | ConvertTo-Json -Depth 12
}
if ($Compact -and $resultJson.Length -gt 30000) { throw "Compact research exceeded its 30000-character output contract: $($resultJson.Length)." }
Write-LlmWikiQueryCache -Entry $queryCacheEntry -Content $resultJson
if ($Format -eq 'Json') {
    Write-Output $resultJson
    exit 0
}
Write-Host "Research packet: $($result.workflow.profile) workflow, $($result.workflow.confidence) confidence"
Write-Host "Confidence: discovery=$($result.workflow.confidenceDimensions.discovery); blocker-count=$($result.workflow.confidenceDimensions.blockerCount); implementation-scope=$($result.workflow.confidenceDimensions.implementationScope)"
foreach ($confidenceReason in $result.workflow.confidenceReasons) { Write-Host "Confidence reason: $confidenceReason" }
Write-Host "Purpose: $($result.workflow.purpose); assessment complete: $($result.readiness.assessmentComplete)"
Write-Host "Assessment status: $($result.readiness.assessmentStatus); design checkpoint: $($result.readiness.designCheckpoint); implementation readiness: $($result.readiness.implementationStatus)"
Write-Host "Objective: $Objective"
Write-Host "Grounded paths: $($result.discovery.groundedPaths.Count)"
foreach ($rankedPath in @($result.discovery.rankedPaths | Select-Object -First 5)) {
    Write-Host "  Ranked: $($rankedPath.path) (score=$($rankedPath.score), source=$($rankedPath.source), reason=$($rankedPath.reason))"
}
if ($result.discovery.runtimeFlow.status -ne 'not-requested') {
    Write-Host "Runtime flow: $($result.discovery.runtimeFlow.status), confidence=$($result.discovery.runtimeFlow.confidence), downstream=$(@($result.discovery.runtimeFlow.downstreamConsumers).Count), dependencies=$(@($result.discovery.runtimeFlow.dependencies).Count)"
}
foreach ($item in @($result.discovery.implementationFiles | Select-Object -First 5)) { Write-Host "  Source: $($item.path) (score=$($item.score), $($item.provenance))" }
foreach ($item in @($result.precedents | Select-Object -First 3)) { Write-Host "  Precedent: $($item.shortHash) $($item.subject)" }
foreach ($item in $result.knownFailures) { Write-Host "  Known failure: $($item.id) - $($item.symptom)" }
foreach ($lane in $result.researchLanes) { Write-Host "  Lane $($lane.id): $($lane.evidenceCount) evidence item(s) - $($lane.purpose)" }
Write-Host "Research plan: groups=$($result.researchPlan.groupCount), unique reads=$($result.researchPlan.uniqueReadPathCount), duplicate reads avoided=$($result.researchPlan.duplicateReadSavings)"
if ($result.extractionDelta) {
    $delta = $result.extractionDelta
    Write-Host "Extraction delta: $($delta.contract) consumers $($delta.initialConsumers) -> $($delta.currentConsumers) (resolved=$($delta.resolvedConsumers)); aggregate blockers $($delta.initialAggregateBlockers) -> $($delta.currentAggregateBlockers)."
    if (@($delta.removedConsumerGroups).Count -gt 0) { Write-Host "  Removed groups: $($delta.removedConsumerGroups -join ', ')" }
    if (@($delta.nextOwner).Count -gt 0) { Write-Host "  Next owner: $($delta.nextOwner -join ', ')" }
    foreach ($cluster in $delta.capabilityClusters) { Write-Host "  Capability $($cluster.capability): $($cluster.count) path(s), consumers=$($cluster.consumers -join ', ')" }
}
if ($result.PSObject.Properties['nextQuestion'] -and $null -ne $result.nextQuestion) {
    $anchorText = if ($result.nextQuestion.anchorStatus -eq 'line') { "$($result.nextQuestion.anchor.path):$($result.nextQuestion.anchor.line)" } elseif ($result.nextQuestion.anchorStatus -eq 'path') { [string]$result.nextQuestion.anchor.path } else { 'anchor missing' }
    Write-Host "  NEXT QUESTION [$($result.nextQuestion.id)] ($anchorText): $($result.nextQuestion.question)"
    if (@($result.openQuestions).Count -gt 1) { Write-Host "  Additional questions deferred: $(@($result.openQuestions).Count - 1)" }
}
Write-Host "Ready to design: $($result.readiness.readyToDesign)"
Write-Host "Ready to implement: $($result.readiness.readyToImplement)"
Write-Host "Next: $($result.nextAction)"
