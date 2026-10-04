[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$planner = Join-Path $PSScriptRoot 'Invoke-LlmWikiAffectedSmoke.ps1'
function Get-Groups([string[]]$ChangedPath) {
    $plan = & $planner -ChangedPath $ChangedPath -Plan -Format Json | ConvertFrom-Json
    @($plan.groups)
}

$unknownGroups = @(Get-Groups '.llm-wiki/tools/Manage-LlmWikiFutureFeature.ps1')
if ($unknownGroups -notcontains 'tool-contract' -or $unknownGroups -contains 'full-tools') {
    throw 'Unknown Wiki tools must use the bounded tool-contract gate, never the monolithic full-tools fallback.'
}
$monolithGroups = @(Get-Groups '.llm-wiki/tools/Test-LlmWikiTools.ps1')
if ($monolithGroups -notcontains 'tool-contract' -or $monolithGroups -contains 'full-tools') {
    throw 'Changing the legacy monolith must not trigger that same monolith during local affected verification.'
}
$learningGroups = @(Get-Groups '.llm-wiki/tools/Manage-LlmWikiLearningPromotion.ps1')
if ($learningGroups -notcontains 'knowledge-isolation' -or $learningGroups -contains 'full-tools') {
    throw 'Known learning tooling did not select its focused regression group.'
}
$evalGroups = @(Get-Groups '.llm-wiki/tools/Invoke-LlmWikiAdaptiveVerification.ps1')
if ($evalGroups -notcontains 'adaptive-evals' -or $evalGroups -contains 'adaptive-routing') {
    throw 'Adaptive eval orchestration must not replay the workflow-routing regression group.'
}
$contextEvalGroups = @(Get-Groups '.llm-wiki/evals/context-search-unseen-20260826.json')
if ($contextEvalGroups -notcontains 'adaptive-evals' -or $contextEvalGroups -notcontains 'context-search-evals' -or $contextEvalGroups -notcontains 'context-retrieval') {
    throw 'Context-search corpora must run both adaptive evals and the SQL context regression suite.'
}
$contextRankingGroups = @(Get-Groups '.llm-wiki/policies/context-search-ranking.json')
foreach ($consumer in @('Manage-LlmWikiModelRouting', 'Manage-LlmWikiVerificationPlan')) {
    if (@(Get-Groups ".llm-wiki/tools/$consumer.ps1") -notcontains 'strict-shapes') { throw "Item-ID consumer lost its collection regression: $consumer" }
}
foreach ($retired in @('Test-LlmWikiModelRoutingItemIds', 'Test-LlmWikiVerificationPlanItemIds')) {
    $retiredGroups = @(Get-Groups ".llm-wiki/tools/$retired.ps1")
    if ($retiredGroups -notcontains 'strict-shapes' -or $retiredGroups -contains 'tool-contract') { throw "Retired item-ID test must select its replacement rather than require a deleted file: $retired" }
}
if (@(Get-Groups '.llm-wiki/tools/Test-LlmWikiModulePersistencePolicy.ps1') -notcontains 'change-policy') { throw 'Module persistence policy regression is no longer selected by its owning group.' }
$gitPathGroups = @(Get-Groups '.llm-wiki/tools/LlmWikiGitPaths.ps1')
if ($gitPathGroups -notcontains 'git-paths' -or $gitPathGroups -notcontains 'api-compatibility') { throw 'Git path changes must retain API baseline selection regressions.' }
foreach ($poolTool in @('LlmWikiCorpusEvaluation', 'Test-LlmWikiCorpusEvaluation')) {
    $poolGroups = @(Get-Groups ".llm-wiki/tools/$poolTool.ps1")
    if ($poolGroups -notcontains 'context-search-evals' -or $poolGroups -notcontains 'context-retrieval' -or $poolGroups -contains 'tool-contract') {
        throw 'Corpus pool changes must select the existing complete context regression groups.'
    }
}
if ($contextRankingGroups -notcontains 'context-search-evals' -or $contextRankingGroups -notcontains 'context-retrieval') {
    throw 'Context-search ranking policy changes must invalidate the SQL context regression suite.'
}
$combinedAdaptiveGroups = @(Get-Groups @(
    '.llm-wiki/tools/Invoke-LlmWikiAdaptiveVerification.ps1'
    '.llm-wiki/tools/Get-LlmWikiDesignCheckpoint.ps1'
    '.llm-wiki/tools/Get-LlmWikiAdaptiveWorkflow.ps1'
))
if ($combinedAdaptiveGroups -notcontains 'adaptive-evals' -or
    $combinedAdaptiveGroups -contains 'adaptive-routing' -or
    $combinedAdaptiveGroups -contains 'adaptive-experience') {
    throw 'Adaptive eval coverage did not collapse duplicate routing and experience groups.'
}

$parallelRunner = Join-Path $PSScriptRoot 'Invoke-LlmWikiParallelSmoke.ps1'
$wikiText = Get-Content (Join-Path $PSScriptRoot '../wiki.ps1') -Raw
$coldGuardLine = ($wikiText -split '\r?\n' | Where-Object { $_ -match '^\s*\$includesColdCheckoutGuard =' })
foreach ($case in @(
    @{ groups = @('code-graph'); expected = $false },
    @{ groups = @('json-cold-checkout', 'read-only-isolation', 'read-only-retrieval'); expected = $true }
)) {
    $smokeGroups = $case.groups
    Invoke-Expression $coldGuardLine
    if ($includesColdCheckoutGuard -ne $case.expected) { throw 'Expanded cold-checkout groups lost their verification time budget.' }
}
foreach ($path in @('.llm-wiki/tools/wiki-markdown-links.mjs', '.llm-wiki/tools/code-graph-maintenance-recovery.test.mjs',
    '.llm-wiki/tools/code-graph-candidates.mjs', '.llm-wiki/tools/code-graph-candidates.test.mjs')) {
    $maintenanceGroups = @(Get-Groups $path)
    if ($maintenanceGroups -notcontains 'code-graph-core' -or $maintenanceGroups -notcontains 'trace-output') { throw "Maintenance dependency has no graph and trace coverage: $path" }
}
$parallelRunnerText = Get-Content -LiteralPath $parallelRunner -Raw
$plannerText = Get-Content -LiteralPath $planner -Raw
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$catalog = Import-PowerShellDataFile -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/policies/affected-smoke-catalog.psd1')
$implementationPlanGroup = @($catalog.Groups | Where-Object Id -eq 'implementation-plan')
if ($implementationPlanGroup.Count -ne 1 -or -not [bool]$implementationPlanGroup[0].GraphDependent) {
    throw 'Implementation-plan smoke must prewarm the code graph before compiling task briefs and change packets.'
}
if ($plannerText -match 'elseif \(\$path -match ''\^\\\.llm-wiki') {
    throw 'Affected-smoke routing regressed to an imperative regex chain instead of the declarative catalog.'
}
$unmappedTools = @(
    Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '.llm-wiki/tools/*') -FailureMessage 'Unable to enumerate wiki tool paths for affected-smoke planning regression.' |
        Where-Object { $_ -match '\.(?:ps1|mjs)$' } |
        Where-Object {
            $toolPath = $_.Replace('\', '/')
            @($catalog.Groups | Where-Object {
                @($_.Patterns | Where-Object { $toolPath -match $_ }).Count -gt 0
            }).Count -eq 0
        }
)
if ($unmappedTools.Count -gt 0) {
    throw "Affected-smoke catalog leaves Wiki tools without a test group: $($unmappedTools -join ', ')."
}
foreach ($catalogGroup in @($catalog.Groups | Where-Object { -not $_.ContainsKey('ExpandTo') -and -not ($_.ContainsKey('Fallback') -and [bool]$_.Fallback) })) {
    if ($plannerText -notmatch "'$([regex]::Escape([string]$catalogGroup.Id))'\s*\{") {
        throw "Affected-smoke catalog group '$($catalogGroup.Id)' has no execution handler."
    }
}
if (-not $parallelRunnerText.Contains('Parallel affected smoke aggregate cache hit') -or
    -not $parallelRunnerText.Contains("-Stage 'affected smoke'")) {
    throw 'Parallel smoke does not short-circuit an unchanged complete group set with one aggregate fingerprint.'
}
foreach ($observabilityContract in @('Code graph prewarm still running', 'CodeGraphTimeoutSeconds', 'graphPlan.reason', 'Diagnostic log', 'LLM_WIKI_SMOKE_SANDBOX', 'Request-SmokeCancellation', 'changed concurrently outside owned smoke sandboxes')) {
    if (-not $parallelRunnerText.Contains($observabilityContract)) {
        throw "Parallel smoke omitted observability/isolation contract '$observabilityContract'."
    }
}
if ($parallelRunnerText -notmatch "ContainsKey\('RequestedGroup'\).*ChangedPath = @\(\)") {
    throw 'Forced smoke groups still perform an unnecessary full Git diff before planning.'
}
$fullFocusedPlan = & $parallelRunner -AllGroups -Plan -Format Json | ConvertFrom-Json
if (@($fullFocusedPlan.groups) -contains 'full-tools' -or @($fullFocusedPlan.groups) -contains 'tool-contract') {
    throw 'The complete focused catalog contains a fallback-only or monolithic smoke group.'
}
if ([string]$fullFocusedPlan.parallelGroups[0] -ne 'adaptive-evals') {
    throw 'The focused scheduler must start the longest adaptive eval lane first.'
}
if ([string]$fullFocusedPlan.parallelGroups[1] -ne 'code-graph-core') {
    throw 'The long code-graph lane must start in the first worker batch.'
}
foreach ($group in @('read-only-isolation', 'json-cold-checkout')) {
    if (@($fullFocusedPlan.parallelGroups) -notcontains $group) { throw "Private fixture group is not parallel: $group" }
}
if (@($fullFocusedPlan.serialGroups) -notcontains 'read-only-retrieval' -or @($fullFocusedPlan.groups) -contains 'read-only-guard') {
    throw 'Read-only retrieval must stay behind shared graph writers, without replaying the legacy aggregate.'
}
if (@($fullFocusedPlan.serialGroups) -notcontains 'context-retrieval' -or @($fullFocusedPlan.parallelGroups) -notcontains 'context-search-evals' -or @($fullFocusedPlan.groups) -contains 'context-bundle') {
    throw 'The context-cache SLA fixture must remain isolated from parallel smoke groups.'
}
if (@($fullFocusedPlan.serialGroups) -notcontains 'trace-output') {
    throw 'Trace-output snapshot creation must not race the parallel code-graph writer.'
}
if (@($fullFocusedPlan.parallelGroups) -notcontains 'code-graph-core') {
    throw 'The code-graph smoke must remain in the parallel batch ahead of trace-output snapshot creation.'
}
$productGroups = @(Get-Groups 'FoodDiary.Application/Users/Example.cs')
if ($productGroups.Count -ne 0) { throw 'Product-only changes unexpectedly selected Wiki tool smoke.' }

$forcedPlan = & $planner -ChangedPath @() -RequestedGroup strict-shapes -Plan -Format Json | ConvertFrom-Json
if (@($forcedPlan.groups) -notcontains 'strict-shapes') {
    throw 'An explicitly requested focused group was lost when the changed-path collection was empty.'
}

$contextAlias = & $planner -ChangedPath @() -RequestedGroup context-bundle -Plan -Format Json | ConvertFrom-Json
if (($contextAlias.groups -join ',') -cne 'context-retrieval,context-search-evals') {
    throw 'Legacy context-bundle alias must retain both quality and isolated retrieval checks.'
}
$graphGroups = @('code-graph-core', 'trace-output')
foreach ($scope in @(@{ ChangedPath = @() }, @{ ChangedPath = @('.llm-wiki/tools/Manage-LlmWikiCodeGraph.ps1', '.llm-wiki/tools/Find-LlmWikiQualityRisk.ps1') })) {
    $graphAlias = & $planner @scope -RequestedGroup code-graph -Plan -Format Json | ConvertFrom-Json
    if (($graphAlias.groups -join ',') -cne ($graphGroups -join ',')) { throw 'Legacy code-graph alias lost graph, trace, or unique quality-risk checks.' }
}
if ($fullFocusedPlan.groupCount -ne 37 -or @($fullFocusedPlan.groups) -contains 'code-graph') { throw 'Full focused planning must select the 37 canonical groups without replaying the graph alias.' }
$parseErrors = $null
$plannerAst = [Management.Automation.Language.Parser]::ParseInput($plannerText, [ref]$null, [ref]$parseErrors)
if (@($parseErrors).Count -gt 0) { throw 'Affected-smoke execution handlers do not parse.' }
$executionSwitch = $plannerAst.Find({ param($node) $node -is [Management.Automation.Language.SwitchStatementAst] -and $node.Condition.Extent.Text -eq '$group' }, $true)
if ($null -eq $executionSwitch) { throw 'Affected-smoke group execution switch was not found.' }
$graphTestCalls = @(
    foreach ($clause in $executionSwitch.Clauses) {
        if ($clause.Item1.Value -notin $graphGroups) { continue }
        $clause.Item2.FindAll({ param($node) $node -is [Management.Automation.Language.StringConstantExpressionAst] -and $node.Value -match '^Test-LlmWiki.*\.ps1$' }, $true) | ForEach-Object Value
    }
)
$requiredGraphTests = @('Test-LlmWikiRoslynExtractor.ps1', 'Test-LlmWikiTypeScriptExtractor.ps1', 'Test-LlmWikiCodeGraph.ps1', 'Test-LlmWikiTraceOutput.ps1', 'Test-LlmWikiFrontendTraceSqlParity.ps1', 'Test-LlmWikiQualityRisk.ps1')
if ($graphTestCalls.Count -ne $requiredGraphTests.Count) { throw 'Canonical graph and trace handlers changed their frozen six-script coverage.' }
foreach ($name in $requiredGraphTests) {
    if (@($graphTestCalls | Where-Object { $_ -ceq $name }).Count -ne 1) { throw "Graph and trace regression must execute exactly once: $name" }
}
$readOnlyGroups = @('json-cold-checkout', 'read-only-isolation', 'read-only-retrieval')
foreach ($scope in @(@{ ChangedPath = @() }, @{ ChangedPath = @('.llm-wiki/tools/Invoke-LlmWikiReadOnlyTool.ps1') })) {
    $legacyPlan = & $planner @scope -RequestedGroup read-only-guard -Plan -Format Json | ConvertFrom-Json
    if (($legacyPlan.groups -join ',') -cne ($readOnlyGroups -join ',')) { throw 'Legacy read-only group no longer selects all four regression scripts.' }
    foreach ($group in $readOnlyGroups) {
        $childPlan = & $planner @scope -RequestedGroup $group -Plan -Format Json | ConvertFrom-Json
        if (@($childPlan.groups).Count -ne 1 -or $childPlan.groups[0] -ne $group) { throw "Requested child group was dropped: $group" }
    }
}

Write-Host 'LLM Wiki affected-smoke planning regression passed: local routing never falls back to full-tools.'
