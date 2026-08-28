[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string[]]$ProposedPath,
    [string]$Intent,
    [object]$DiffInput,
    [object]$PolicyInput,
    [object]$OwnershipInput,
    [object]$TestPlanInput,
    [object]$RolloutInput,
    [object]$DecisionInput,
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite',
    [switch]$SkipQueryCache,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [switch]$Compact,
    [switch]$SkipTestPlan,
    [ValidateRange(1, 20)]
    [int]$Limit = 8
)

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$wikiRoot = Split-Path -Parent $toolsRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $toolsRoot 'LlmWikiQueryCache.ps1')

$cacheEligible = $Format -eq 'Json' -and
    -not $SkipQueryCache -and
    $null -eq $DiffInput -and $null -eq $PolicyInput -and $null -eq $OwnershipInput -and
    $null -eq $TestPlanInput -and $null -eq $RolloutInput -and $null -eq $DecisionInput
$queryCacheEntry = $null
if ($cacheEligible) {
    $cacheArguments = @{
        BaseRef = $BaseRef
        HeadRef = $HeadRef
        ChangedPath = @($ChangedPath)
        ProposedPath = @($ProposedPath)
        Intent = $Intent
        Compact = [bool]$Compact
        SkipTestPlan = [bool]$SkipTestPlan
        Limit = $Limit
    }
    $cacheArguments.CompiledIndexSource = $CompiledIndexSource
    $compiledIndexDependencies = if ($CompiledIndexSource -eq 'Json') {
        @(
            '.llm-wiki/generated/repository-catalog.json'
            '.llm-wiki/generated/csharp-symbol-index.json'
            '.llm-wiki/generated/frontend-index.json'
            '.llm-wiki/generated/quality-index.json'
            '.llm-wiki/generated/runtime-topology.json'
            '.llm-wiki/generated/sensitive-data-index.json'
            '.llm-wiki/generated/frontend-contract-index.json'
            '.llm-wiki/generated/domain-data-index.json'
            '.llm-wiki/generated/backend-contract-index.json'
            '.llm-wiki/generated/architecture-health-index.json'
        )
    } else {
        @('.artifacts/llm-wiki/code-graph/code-graph.fingerprint')
    }
    $queryCacheEntry = Get-LlmWikiQueryCacheEntry -RepositoryRoot $repositoryRoot -Namespace 'task-brief' -Arguments $cacheArguments `
        -RelevantPath @($(if (@($ProposedPath).Count -gt 0) { $ProposedPath } else { $ChangedPath })) `
        -DependencyPath @(
            '.llm-wiki/policies/change-policy.json'
            '.llm-wiki/policies/query-indexes.json'
            $compiledIndexDependencies
        )
    $cachedBrief = Read-LlmWikiQueryCache -Entry $queryCacheEntry
    if ($null -ne $cachedBrief) {
        Write-Output $cachedBrief
        exit 0
    }
}

$common = @{ BaseRef = $BaseRef; Format = 'Json' }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
$effectivePaths = @(
    @($ChangedPath) + @($ProposedPath) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique
)
$inferredPaths = @()
$intentCompiledResult = $null
$intentIndexDiagnostics = $null
$normalizedIntent = ([string]$Intent).ToLowerInvariant()
$broadAssessmentDimensionCount = @(
    [regex]::Matches(
        $normalizedIntent,
        '\b(correctness|reliability|concurrency|architecture|privacy|security|ci|operations|operational|project|repository|cross-layer|system-wide)\b') |
        ForEach-Object Value |
        Sort-Object -Unique
).Count
$broadAssessmentIntent = $normalizedIntent -match '\b(audit|assessment|evaluate|review)\b|аудит|оцен' -and
    $broadAssessmentDimensionCount -ge 3
$wikiInternalIntent = $normalizedIntent -match '\b(llm[- ]?wiki|wiki\.ps1|wiki tooling|development mcp)\b'
if ($effectivePaths.Count -eq 0 -and $wikiInternalIntent) {
    # Tooling objectives use a dedicated Wiki code/workflow index instead of
    # coincidental product-symbol matches such as "registry" or "metrics".
    $toolingContext = & (Join-Path $toolsRoot 'Get-LlmWikiToolingContext.ps1') `
        -Query $Intent -Limit 8 -Format Json | ConvertFrom-Json
    $inferredPaths = @($toolingContext.groundedPaths)
    if ($inferredPaths.Count -eq 0) { $inferredPaths = @('.llm-wiki/wiki.ps1', '.llm-wiki/README.md') }
    $effectivePaths = $inferredPaths
}
if ($effectivePaths.Count -eq 0 -and -not $broadAssessmentIntent -and -not [string]::IsNullOrWhiteSpace($Intent)) {
    $ignoredIntentTerms = @(
        'add', 'change', 'changing', 'create', 'feature', 'implement', 'improve', 'make', 'routing', 'support', 'update', 'visual', 'without'
    )
    if ($Intent -match '(?i)\b(without changing|unchanged|no changes? to)\b') {
        $ignoredIntentTerms += @('api', 'architecture', 'contract', 'contracts', 'privacy', 'provider', 'security')
    }
    $intentTokens = @(
        [regex]::Matches($Intent.ToLowerInvariant(), '[\p{L}\p{Nd}]{4,}') |
            ForEach-Object { $_.Value } |
            Where-Object { $_ -notin $ignoredIntentTerms } |
            Sort-Object -Unique
    )
    $frontendIntent = $normalizedIntent -match '\b(frontend|component|template|html|css|scss|svg|style|styling|visual|layout|responsive|viewport|icon|colour|color|animation|button|disabled|corner|radius|border)\b'
    $backendIntent = $normalizedIntent -match '\b(backend|handler|command|query|controller|endpoint|database|migration|repository|service|domain|api)\b'
    $candidates = [System.Collections.Generic.List[object]]::new()
    $symbolIndex = $null
    $frontendIntentIndex = $null
    if ($CompiledIndexSource -eq 'Sqlite') {
        $intentIndexStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $intentCompiledResult = & (Join-Path $toolsRoot 'Manage-LlmWikiCodeGraph.ps1') `
            -Action compiled-context `
            -Query ($intentTokens -join ' ') `
            -SkipRefresh `
            -Format Json | ConvertFrom-Json
        $intentIndexStopwatch.Stop()
        if (-not [bool]$intentCompiledResult.ready) {
            throw "SQLite compiled-index projection is unavailable ($($intentCompiledResult.unavailableReason)). Run ./.llm-wiki/wiki.ps1 graph-build and retry."
        }
        if (-not ($frontendIntent -and -not $backendIntent)) {
            $symbolIndex = [pscustomobject]@{ symbols = @($intentCompiledResult.symbols) }
        }
        $frontendIntentIndex = [pscustomobject]@{ symbols = @($intentCompiledResult.frontendSymbols) }
        $intentIndexDiagnostics = [ordered]@{
            source = [string]$intentCompiledResult.source
            selectionMode = [string]$intentCompiledResult.selectionMode
            sqlDurationMs = [double]$intentCompiledResult.durationMs
            roundTripDurationMs = [Math]::Round($intentIndexStopwatch.Elapsed.TotalMilliseconds, 2)
            scannedRecords = [int]$intentCompiledResult.scannedRecords
            candidateRecords = [int]$intentCompiledResult.returnedRecords
            sourceBytesRead = $null
            sourceHashes = $intentCompiledResult.sourceHashes
            reusedForDiff = $false
        }
    } else {
        if (-not ($frontendIntent -and -not $backendIntent)) {
            $symbolIndexPath = Join-Path $wikiRoot 'generated/csharp-symbol-index.json'
            if (Test-Path -LiteralPath $symbolIndexPath) {
                $symbolIndex = Get-Content -LiteralPath $symbolIndexPath -Raw | ConvertFrom-Json
            }
        }
        $frontendIntentIndexPath = Join-Path $wikiRoot 'generated/frontend-index.json'
        if (Test-Path -LiteralPath $frontendIntentIndexPath) {
            $intentIndexStopwatch = [Diagnostics.Stopwatch]::StartNew()
            $frontendIntentIndexRaw = Get-Content -LiteralPath $frontendIntentIndexPath -Raw
            $frontendIntentIndex = $frontendIntentIndexRaw | ConvertFrom-Json
            $intentIndexStopwatch.Stop()
            $intentIndexDiagnostics = [ordered]@{
                source = 'json-baseline'
                selectionMode = 'intent'
                sqlDurationMs = $null
                roundTripDurationMs = [Math]::Round($intentIndexStopwatch.Elapsed.TotalMilliseconds, 2)
                scannedRecords = @($frontendIntentIndex.symbols).Count
                candidateRecords = @($frontendIntentIndex.symbols).Count
                sourceBytesRead = [Text.Encoding]::UTF8.GetByteCount($frontendIntentIndexRaw)
                sourceHashes = $null
                reusedForDiff = $false
            }
        }
    }
    if ($null -ne $symbolIndex) {
        foreach ($symbol in @($symbolIndex.symbols)) {
            $symbolName = if ($symbol.PSObject.Properties['name']) { [string]$symbol.name } else { '' }
            $symbolPath = if ($symbol.PSObject.Properties['path']) { [string]$symbol.path } else { '' }
            $searchText = "$symbolName $symbolPath".ToLowerInvariant()
            $score = @($intentTokens | Where-Object { $searchText -match [regex]::Escape($_) }).Count
            if ($score -gt 0 -and -not [string]::IsNullOrWhiteSpace($symbolPath)) {
                $candidates.Add([pscustomobject]@{ path = $symbolPath; score = $score; source = 'csharp-symbol-index' })
            }
        }
    }
    if ($null -ne $frontendIntentIndex) {
        foreach ($symbol in @($frontendIntentIndex.symbols)) {
            $symbolName = if ($symbol.PSObject.Properties['name']) { [string]$symbol.name } else { '' }
            $symbolPath = if ($symbol.PSObject.Properties['path']) { [string]$symbol.path } else { '' }
            $symbolRole = if ($symbol.PSObject.Properties['role']) { [string]$symbol.role } else { '' }
            $symbolSelector = if ($symbol.PSObject.Properties['selector']) { [string]$symbol.selector } else { '' }
            $searchText = "$symbolName $symbolPath $symbolRole $symbolSelector".ToLowerInvariant()
            $semanticScore = @($intentTokens | Where-Object { $searchText -match [regex]::Escape($_) }).Count
            if ($semanticScore -gt 0 -and -not [string]::IsNullOrWhiteSpace($symbolPath)) {
                $score = $semanticScore + $(if ($frontendIntent) { 4 } else { 0 })
                $candidates.Add([pscustomobject]@{ path = $symbolPath; score = $score; source = 'frontend-index' })
            }
        }
    }
    $candidateScoreMeasure = @($candidates | Measure-Object score -Maximum)
    $maximumCandidateScore = if ($candidateScoreMeasure.Count -gt 0 -and $candidateScoreMeasure[0].PSObject.Properties['Maximum']) { $candidateScoreMeasure[0].Maximum } else { $null }
    $minimumCandidateScore = if ($null -eq $maximumCandidateScore) { 1 } else { [Math]::Max(1, $maximumCandidateScore - 1) }
    $inferredPaths = @(
        $candidates |
            Where-Object score -ge $minimumCandidateScore |
            Sort-Object @{ Expression = 'score'; Descending = $true }, path |
            Select-Object -ExpandProperty path -Unique |
            Select-Object -First 8
    )
    $effectivePaths = $inferredPaths
}
if ($effectivePaths.Count -gt 0) {
    $common.ChangedPath = $effectivePaths
} elseif (-not [string]::IsNullOrWhiteSpace($Intent)) {
    # Intent-first planning must not silently absorb an unrelated dirty worktree.
    # An explicit empty path list keeps diff collection bounded until discovery
    # grounds the objective in current repository paths.
    $common.ChangedPath = @()
}

$diffArguments = @{} + $common
$diffArguments.Limit = $Limit
$diffArguments.CompiledIndexSource = $CompiledIndexSource
if ($CompiledIndexSource -eq 'Sqlite' -and $null -ne $intentCompiledResult -and
    $effectivePaths.Count -gt 0 -and $null -eq $DiffInput) {
    $diffArguments.CompiledIndexInput = $intentCompiledResult
    $intentIndexDiagnostics['reusedForDiff'] = $true
}
$diff = if ($null -ne $DiffInput) { $DiffInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiDiffContext.ps1') @diffArguments | ConvertFrom-Json
}

if ($effectivePaths.Count -eq 0 -and
    [string]::IsNullOrWhiteSpace($Intent) -and
    @($diff.changedPaths).Count -eq 0) {
    $recommendedCommand = "./.llm-wiki/wiki.ps1 brief -Intent '<task>' -PlannedPath @('path/one','path/two')"
    $emptyBrief = [pscustomobject]@{
        compact = [bool]$Compact
        analysis = [pscustomobject]@{
            mode = 'unscoped'
            confidence = 'low'
            provenance = @('caller-input')
            inferredPaths = @()
        }
        risk = [pscustomobject]@{
            level = 'low'
            score = 0
            reasons = @()
        }
        change = [pscustomobject]@{
            intent = ''
            paths = @()
            proposedPaths = @()
            scopes = @()
            directModules = @()
            downstreamModules = @()
        }
        instructions = @()
        contextPages = @()
        focusedTests = @()
        testScenarios = @()
        requiredChecks = @()
        reviewObligations = @()
        structuralViolations = @()
        generatedActions = @()
        impactCounts = [pscustomobject]@{
            runtime = 0
            privacyFields = 0
            privacyExternalTransfers = 0
            frontendComponents = 0
            frontendConsumers = 0
            backendContracts = 0
            backendConsumers = 0
            backendConsumerKinds = @()
            domainTypes = 0
        }
        privacyExternalTransfers = @()
        warnings = @("No diff, intent, or planned paths were supplied. Run: $recommendedCommand")
        nextSteps = @(
            [pscustomobject]@{
                id = 'supply-task-scope'
                reason = 'A pre-diff brief needs an objective, candidate paths, or both to rank repository evidence.'
                recommendedCommand = $recommendedCommand
                alternatives = @(
                    "./.llm-wiki/wiki.ps1 brief -Intent '<task>'"
                    "./.llm-wiki/wiki.ps1 brief -PlannedPath 'path/one;path/two'"
                )
            }
        )
    }

    if ($Format -eq 'Json') {
        $emptyBrief | ConvertTo-Json -Depth 6
        exit 0
    }

    Write-Host 'Task brief: low risk (score 0)'
    Write-Host "Next step: $recommendedCommand"
    Write-Host 'Warning: no diff, intent, or planned paths were supplied.'
    exit 0
}

$policy = if ($null -ne $PolicyInput) { $PolicyInput } else {
    & (Join-Path $toolsRoot 'Test-LlmWikiChangePolicy.ps1') @common | ConvertFrom-Json
}
$matchedRuleIds = @($policy.matchedRules | ForEach-Object { if ($_.PSObject.Properties['id']) { $_.id } } | Where-Object { $_ })
$ownership = if ($null -ne $OwnershipInput) { $OwnershipInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiOwnershipImpact.ps1') @common -DiffInput $diff | ConvertFrom-Json
}
$testPlanArguments = @{} + $common
$testPlanArguments.Limit = $Limit
$testPlan = if ($null -ne $TestPlanInput) { $TestPlanInput } elseif ($SkipTestPlan) {
    [pscustomobject]@{ focusedTestFiles = @(); scenarios = @() }
} else {
    & (Join-Path $toolsRoot 'Get-LlmWikiTestPlan.ps1') @testPlanArguments -DiffInput $diff -PolicyInput $policy | ConvertFrom-Json
}
$rollout = if ($null -ne $RolloutInput) { $RolloutInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiRolloutPlan.ps1') @common -DiffInput $diff -PolicyInput $policy | ConvertFrom-Json
}
$decision = if ($null -ne $DecisionInput) { $DecisionInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiDecisionContext.ps1') @common -DiffInput $diff -PolicyInput $policy | ConvertFrom-Json
}
$changedPathsForQuality = @($diff.changedPaths)
$impactStopwatch = [Diagnostics.Stopwatch]::StartNew()
$impactIndexDiagnostics = $null
if ($CompiledIndexSource -eq 'Sqlite') {
    $impactResult = & (Join-Path $toolsRoot 'Manage-LlmWikiCodeGraph.ps1') `
        -Action task-brief-impact `
        -ChangedPath $changedPathsForQuality `
        -SkipRefresh `
        -Format Json | ConvertFrom-Json
    $impactStopwatch.Stop()
    if (-not [bool]$impactResult.ready) {
        throw "SQLite task-brief impact projection is unavailable ($($impactResult.unavailableReason)). Run ./.llm-wiki/wiki.ps1 graph-build and retry."
    }
    $qualityIndex = $impactResult.groups.quality
    $runtimeTopology = $impactResult.groups.runtime
    $sensitiveData = $impactResult.groups.sensitiveData
    $frontendContract = $impactResult.groups.frontendContract
    $domainData = $impactResult.groups.domainData
    $backendContract = $impactResult.groups.backendContract
    $architectureHealth = $impactResult.groups.architectureHealth
    $impactIndexDiagnostics = [ordered]@{
        source = [string]$impactResult.source
        selectionMode = [string]$impactResult.selectionMode
        sqlDurationMs = [double]$impactResult.durationMs
        roundTripDurationMs = [Math]::Round($impactStopwatch.Elapsed.TotalMilliseconds, 2)
        scannedRecords = [int]$impactResult.scannedRecords
        candidateRecords = [int]$impactResult.candidateRecords
        returnedRecords = [int]$impactResult.returnedRecords
        sourceBytesVerified = [int64]$impactResult.sourceBytesVerified
        sourceBytesMaterialized = [int64]$impactResult.sourceBytesMaterialized
        sourceHashes = $impactResult.sourceHashes
    }
} else {
    $impactSourceBytesVerified = [int64]0
    $impactSourceBytesMaterialized = [int64]0
    function Read-ImpactIndex([string]$RelativePath) {
        $path = Join-Path $wikiRoot $RelativePath
        $raw = [System.IO.File]::ReadAllText($path)
        $rawBytes = [Text.Encoding]::UTF8.GetByteCount($raw)
        $script:impactSourceBytesVerified += $rawBytes
        foreach ($changedPath in $changedPathsForQuality) {
            if ($raw.IndexOf($changedPath, [System.StringComparison]::Ordinal) -ge 0) {
                $script:impactSourceBytesMaterialized += $rawBytes
                return $raw | ConvertFrom-Json
            }
        }
        return $null
    }
    $qualityIndex = Read-ImpactIndex 'generated/quality-index.json'
    $runtimeTopology = Read-ImpactIndex 'generated/runtime-topology.json'
    $sensitiveData = Read-ImpactIndex 'generated/sensitive-data-index.json'
    $frontendContract = Read-ImpactIndex 'generated/frontend-contract-index.json'
    $domainData = Read-ImpactIndex 'generated/domain-data-index.json'
    $backendContract = Read-ImpactIndex 'generated/backend-contract-index.json'
    if ($null -eq $qualityIndex) { $qualityIndex = [pscustomobject]@{ files = @(); criticalSymbols = @() } }
    if ($null -eq $runtimeTopology) { $runtimeTopology = [pscustomobject]@{ hostedServices = @(); httpClients = @(); webhooks = @(); recurringJobRegistrations = @(); networkPolicies = @(); composeServices = @() } }
    if ($null -eq $sensitiveData) { $sensitiveData = [pscustomobject]@{ fields = @(); boundaryFiles = @(); potentialLogging = @(); externalTransfers = @() } }
    if ($null -eq $frontendContract) { $frontendContract = [pscustomobject]@{ components = @(); apiCalls = @(); translationUsage = @(); consumerEdges = @() } }
    if ($null -eq $domainData) { $domainData = [pscustomobject]@{ domainTypes = @(); invariants = @(); persistenceMappings = @() } }
    if ($null -eq $backendContract) { $backendContract = [pscustomobject]@{ contracts = @(); consumerEdges = @() } }
    $architectureHealthPath = Join-Path $wikiRoot 'generated/architecture-health-index.json'
    $architectureHealthRaw = [System.IO.File]::ReadAllText($architectureHealthPath)
    $architectureHealthBytes = [Text.Encoding]::UTF8.GetByteCount($architectureHealthRaw)
    $impactSourceBytesVerified += $architectureHealthBytes
    $impactSourceBytesMaterialized += $architectureHealthBytes
    $architectureHealth = $architectureHealthRaw | ConvertFrom-Json
    $impactStopwatch.Stop()
    $impactIndexDiagnostics = [ordered]@{
        source = 'json-baseline'
        selectionMode = 'exact-changed-paths'
        sqlDurationMs = $null
        roundTripDurationMs = [Math]::Round($impactStopwatch.Elapsed.TotalMilliseconds, 2)
        scannedRecords = $null
        candidateRecords = $null
        returnedRecords = $null
        sourceBytesVerified = $impactSourceBytesVerified
        sourceBytesMaterialized = $impactSourceBytesMaterialized
        sourceHashes = $null
    }
}
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
    externalTransfers = @($sensitiveData.externalTransfers | Where-Object { $changedPathsForQuality -contains $_.path })
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
$changedBackendContractNames = @($changedBackendContracts | ForEach-Object {
    if ($_.PSObject.Properties['name']) { [string]$_.name }
} | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$changedBackendConsumerEdges = @($backendContract.consumerEdges | Where-Object {
    $_.contract -in $changedBackendContractNames
})
function Get-BackendConsumerKind {
    param($Edge)
    if ($Edge.isTest) { return 'test-fixture' }
    if ($Edge.consumerPath -match 'Mappings?/|Mapping\.cs$') { return 'mapping' }
    if ($Edge.consumerPath -match 'Serializ|Json') { return 'serializer' }
    if ($Edge.consumerPath -match 'Presentation|Controller\.cs$|Http') { return 'http' }
    return 'compile'
}
$backendContractImpact = [ordered]@{
    contracts = $changedBackendContracts
    productionConsumers = @($changedBackendConsumerEdges | Where-Object { -not $_.isTest })
    testConsumers = @($changedBackendConsumerEdges | Where-Object isTest)
    consumerKinds = @(
        $changedBackendConsumerEdges |
            Group-Object { Get-BackendConsumerKind $_ } |
            Sort-Object Name |
            ForEach-Object { [pscustomobject]@{ kind = $_.Name; count = $_.Count } }
    )
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
if ($matchedRuleIds -contains 'security-sensitive') { $riskScore += 3; $riskReasons.Add('security-sensitive flow') }
if ($matchedRuleIds -contains 'performance-data-access') { $riskScore += 2; $riskReasons.Add('query or persistence shape') }
if ($matchedRuleIds -contains 'architecture-decision') { $riskScore += 1; $riskReasons.Add('durable architecture decision candidate') }
if ($matchedRuleIds -contains 'observability-critical-flow') { $riskScore += 1; $riskReasons.Add('critical-flow telemetry') }
if ($matchedRuleIds -contains 'privacy-data-lifecycle') { $riskScore += 2; $riskReasons.Add('privacy data lifecycle') }
if ($matchedRuleIds -contains 'dependency-nuget' -or $matchedRuleIds -contains 'dependency-npm') {
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
if (@($privacyImpact.externalTransfers).Count -gt 0) {
    $riskScore += 3
    $riskReasons.Add('private or sensitive data sent to an external provider')
}
if (@($frontendContractImpact.components).Count -gt 0) {
    $riskScore += 2
    $riskReasons.Add('frontend public component contract')
}
$frontendPaths = @($changedPathsForQuality | Where-Object { $_ -match '^FoodDiary\.Web\.Client/' })
$frontendSource = @(
    foreach ($frontendPath in $frontendPaths) {
        $absoluteFrontendPath = Join-Path $repositoryRoot $frontendPath
        if (Test-Path -LiteralPath $absoluteFrontendPath -PathType Leaf) {
            [System.IO.File]::ReadAllText($absoluteFrontendPath)
        }
    }
) -join [Environment]::NewLine
$frontendIntentText = ([string]$Intent).ToLowerInvariant()
$frontendProductionPaths = @($frontendPaths | Where-Object { $_ -notmatch '\.(spec\.ts|test\.mjs)$' })
$frontendBehaviorPaths = @($frontendProductionPaths | Where-Object { $_ -match '\.ts$' })
$frontendPresentationOnly = $frontendPaths.Count -gt 0 -and
    @($diff.scopes | Where-Object { $_ -notin @('Frontend', 'Tests') }).Count -eq 0 -and
    $frontendBehaviorPaths.Count -eq 0 -and
    @($frontendProductionPaths | Where-Object { $_ -notmatch '\.(html|s?css|svg)$' }).Count -eq 0
if ($frontendPaths.Count -gt 0 -and
    ((@($frontendPaths | Where-Object { $_ -match '(?i)(dialog|modal)' }).Count -gt 0) -or
     $frontendIntentText -match '\b(dialog|modal)\b')) {
    $riskScore += 2
    $riskReasons.Add('modal or dialog interaction flow')
}
if ($frontendPaths.Count -gt 0 -and
    ($frontendIntentText -match '\b(responsive|viewport|mobile|layout)\b' -or
     $frontendSource -match '(?m)@media\s*\(')) {
    $riskScore += 1
    $riskReasons.Add('responsive layout behavior')
}
if ($frontendPaths.Count -gt 0 -and
    ($frontendIntentText -match '\b(accessibility|accessible|a11y|keyboard|focus)\b' -or
     $frontendSource -match '(?i)\b(aria-[a-z-]+|role\s*=|tabindex|focus\()')) {
    $riskScore += 1
    $riskReasons.Add('accessibility interaction contract')
}
if ($frontendPaths.Count -gt 0 -and
    ($frontendIntentText -match '\b(state|loading|error|empty|toggle|open|close|interactive)\b' -or
     (-not $frontendPresentationOnly -and $frontendSource -match '(?i)\b(signal|computed|isLoading|error|toggle|open|close|expanded|visible)\b'))) {
    $riskScore += 1
    $riskReasons.Add('multi-state frontend interaction')
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
if (@($diff.scopes) -contains 'Api' -and $matchedRuleIds -contains 'security-sensitive') {
    $riskScore += 1
    $riskReasons.Add('internet-exposed sensitive flow')
}
if (@($ownership.downstreamModules).Count -ge 10) { $riskScore += 2; $riskReasons.Add('broad downstream module impact') }
elseif (@($ownership.downstreamModules).Count -gt 0) { $riskScore += 1; $riskReasons.Add('downstream module impact') }
$policyViolations = @($policy.violations | Where-Object { $null -ne $_ })
if ($policyViolations.Count -gt 0) { $riskScore += 4; $riskReasons.Add('structural policy violation') }
$rawRiskScore = $riskScore
$riskCalibration = 'none'
if ($frontendPresentationOnly -and $riskScore -gt 4) {
    $riskScore = 4
    $riskCalibration = 'frontend-presentation-only-cap'
}
if ($inferredPaths.Count -gt 0 -and
    $riskScore -gt 4 -and
    @($diff.scopes | Where-Object { $_ -in @('Api', 'Database', 'Deployment', 'Configuration') }).Count -eq 0 -and
    @($matchedRuleIds | Where-Object { $_ -in @('security-sensitive', 'privacy-data-lifecycle') }).Count -eq 0) {
    $riskScore = 4
    $riskCalibration = 'intent-inference-cap'
}
$riskLevel = if ($riskScore -ge 7) { 'high' } elseif ($riskScore -ge 3) { 'medium' } else { 'low' }
$analysisMode = if ($broadAssessmentIntent -and @($ProposedPath).Count -eq 0 -and @($diff.changedPaths).Count -eq 0) {
    'broad-assessment'
} elseif ($inferredPaths.Count -gt 0) {
    'intent-inferred'
} elseif (@($ProposedPath).Count -gt 0) {
    'planned-paths'
} elseif (@($diff.changedPaths).Count -gt 0) {
    'git-diff'
} else {
    'unscoped'
}
$analysisConfidence = switch ($analysisMode) {
    'git-diff' { 'high' }
    'planned-paths' { 'medium' }
    'intent-inferred' { 'low' }
    default { 'low' }
}
$briefWarnings = [System.Collections.Generic.List[string]]::new()
foreach ($warning in @($diff.warnings)) { $briefWarnings.Add([string]$warning) }
if ($analysisMode -eq 'unscoped') {
    if ([string]::IsNullOrWhiteSpace($Intent)) {
        $briefWarnings.Add("No diff, intent, or planned paths were supplied. Run: ./.llm-wiki/wiki.ps1 brief -Intent '<task>' -PlannedPath @('path/one','path/two')")
    } else {
        $briefWarnings.Add("Intent did not ground any repository path. Discover an exact symbol, route, or component, then rerun with -PlannedPath @('path/one','path/two').")
    }
} elseif ($analysisMode -eq 'intent-inferred') {
    $briefWarnings.Add('Paths were inferred heuristically from intent. Confirm them with -PlannedPath before treating risk and test output as authoritative.')
} elseif ($analysisMode -eq 'broad-assessment') {
    $briefWarnings.Add('Broad assessment intent was not reduced to a coincidental feature owner. Use repository-wide topology, privacy, health, hotspots, and test-gaps views, then confirm findings in source.')
}

$brief = [pscustomobject]@{
    analysis = [pscustomobject]@{
        mode = $analysisMode
        confidence = $analysisConfidence
        provenance = @(
            if ($analysisMode -eq 'git-diff') { 'git-diff' }
            if ($analysisMode -eq 'planned-paths') { 'caller-planned-paths' }
            if ($analysisMode -eq 'intent-inferred') { 'intent-keywords'; 'csharp-symbol-index'; 'frontend-index' }
            if ($analysisMode -eq 'broad-assessment') { 'broad-assessment-abstention' }
            'compiled-indexes'
            'change-policy'
        )
        inferredPaths = @($inferredPaths)
        compiledIndex = $(if ($null -eq $intentIndexDiagnostics) { $null } else { [pscustomobject]$intentIndexDiagnostics })
        impactIndex = [pscustomobject]$impactIndexDiagnostics
    }
    risk = [pscustomobject]@{
        level = $riskLevel
        score = $riskScore
        rawScore = $rawRiskScore
        profile = if ($frontendPresentationOnly) { 'frontend-presentation-only' } else { 'general' }
        calibration = $riskCalibration
        reasons = @($riskReasons)
    }
    change = [pscustomobject]@{
        intent = $Intent
        paths = @($diff.changedPaths)
        proposedPaths = @($ProposedPath)
        scopes = @($diff.scopes)
        directModules = @($ownership.directModules)
        downstreamModules = @($ownership.downstreamModules)
    }
    instructions = @($ownership.ownershipGuides |
        Select-Object -ExpandProperty guide -Unique |
        Where-Object {
            @($diff.scopes) -contains 'Backend' -or
            $_ -notmatch '^(FoodDiary\.(Application|Domain|Infrastructure)|MailInbox/|MailRelay/)'
        })
    contextPages = @($diff.wikiPages | ForEach-Object { if ($_.PSObject.Properties['path']) { $_.path } } | Where-Object { $_ })
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
    warnings = @($briefWarnings)
    nextSteps = @(
        if ($analysisMode -eq 'broad-assessment') {
            [pscustomobject]@{
                id = 'run-broad-assessment-readers'
                reason = 'A repository-wide assessment requires complementary evidence views instead of a single inferred module.'
                recommendedCommand = './.llm-wiki/wiki.ps1 topology; ./.llm-wiki/wiki.ps1 privacy -PrivacyCategory all; ./.llm-wiki/wiki.ps1 health -HealthView all; ./.llm-wiki/wiki.ps1 hotspots; ./.llm-wiki/wiki.ps1 test-gaps'
                alternatives = @("./.llm-wiki/wiki.ps1 brief -Intent '<bounded task>' -PlannedPath @('path/one','path/two')")
            }
        } elseif ($analysisMode -eq 'unscoped') {
            [pscustomobject]@{
                id = 'supply-task-scope'
                reason = if ([string]::IsNullOrWhiteSpace($Intent)) { 'A pre-diff brief needs an objective, candidate paths, or both to rank repository evidence.' } else { 'The supplied intent did not match a grounded repository path; do not substitute unrelated working-tree changes.' }
                recommendedCommand = "./.llm-wiki/wiki.ps1 brief -Intent '<task>' -PlannedPath @('path/one','path/two')"
                alternatives = @(
                    "./.llm-wiki/wiki.ps1 brief -Intent '<task>'"
                    "./.llm-wiki/wiki.ps1 brief -PlannedPath 'path/one;path/two'"
                )
            }
        }
    )
}

$briefOutput = if ($Compact) {
    [pscustomobject]@{
        compact = $true
        analysis = $brief.analysis
        risk = $brief.risk
        change = $brief.change
        instructions = $brief.instructions
        contextPages = @($brief.contextPages | Select-Object -First $Limit)
        focusedTests = @($brief.focusedTests | Select-Object -First $Limit)
        testScenarios = @($brief.testScenarios | Select-Object id, description)
        requiredChecks = $brief.requiredChecks
        reviewObligations = $brief.reviewObligations
        structuralViolations = $brief.structuralViolations
        generatedActions = $brief.generatedActions
        impactCounts = [pscustomobject]@{
            runtime = $runtimeImpactCount
            privacyFields = @($privacyImpact.fields).Count
            privacyExternalTransfers = @($privacyImpact.externalTransfers).Count
            frontendComponents = @($frontendContractImpact.components).Count
            frontendConsumers = @($frontendContractImpact.downstreamConsumers).Count
            backendContracts = @($backendContractImpact.contracts).Count
            backendConsumers = @($backendContractImpact.productionConsumers).Count
            backendConsumerKinds = $backendContractImpact.consumerKinds
            domainTypes = @($domainDataImpact.types).Count
        }
        privacyExternalTransfers = @($privacyImpact.externalTransfers | Select-Object -First $Limit)
        warnings = $brief.warnings
        nextSteps = $brief.nextSteps
    }
} else {
    $brief
}

if ($Format -eq 'Json') {
    $briefJson = $briefOutput | ConvertTo-Json -Depth 9
    if ($queryCacheEntry) { Write-LlmWikiQueryCache -Entry $queryCacheEntry -Content $briefJson }
    Write-Output $briefJson
    exit 0
}

Write-Host "Task brief: $($brief.risk.level) risk (score $($brief.risk.score))"
if ($brief.risk.reasons.Count -gt 0) { Write-Host "Risk factors: $($brief.risk.reasons -join ', ')" }
foreach ($nextStep in @($brief.nextSteps)) {
    Write-Host "Next step: $($nextStep.recommendedCommand)"
}
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
