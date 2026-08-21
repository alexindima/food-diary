[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$research = & (Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1') `
    -Objective 'Assess remaining IUserContextService extraction blockers' `
    -SkipHistory `
    -Purpose Assessment `
    -Limit 5 `
    -Format Json | ConvertFrom-Json

if ($research.workflow.confidence -ne 'high') { throw 'Grounded assessment discovery did not raise overall research confidence.' }
if ($research.workflow.confidenceDimensions.discovery -ne 'high') { throw 'Research did not expose high grounded discovery confidence.' }
if ($research.workflow.confidenceDimensions.blockerCount -ne 'high') { throw 'Extraction assessment did not expose high blocker-count confidence.' }
if ($research.workflow.confidenceDimensions.implementationScope -ne 'not-required') { throw 'Read-only assessment incorrectly rated an implementation scope.' }
if (@($research.workflow.confidenceReasons).Count -lt 3) { throw 'Research confidence does not explain each dimension.' }
if ($research.readiness.designCheckpoint -ne 'not-required') { throw 'Read-only assessment incorrectly requires a design checkpoint.' }

$compactJson = & (Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1') `
    -Objective 'Extract Dietologist application module into a separate project' `
    -Module Dietologist `
    -Compact `
    -Purpose Assessment `
    -Limit 6 `
    -Format Json
$compact = $compactJson | ConvertFrom-Json
if (@($compact.discovery.groundedPaths | Where-Object { $_ -match 'Dietologist' }).Count -eq 0) { throw 'Compact module research did not stay grounded in the selected module.' }
if (@($compact.precedents).Count -ne 0) { throw 'Compact research unexpectedly ran historical precedent analysis.' }
if (([string]$compactJson).Length -gt 30000) { throw "Compact research exceeded 30000 characters: $(([string]$compactJson).Length)." }
if (@($compact.discovery.runtimeFlow.downstreamConsumers).Count -gt 6 -or @($compact.discovery.runtimeFlow.dependencies).Count -gt 6) {
    throw 'Compact research returned graph evidence beyond the requested limit.'
}
if (@($compact.discovery.routes).Count -gt 6 -or @($compact.discovery.guides).Count -gt 6 -or @($compact.knownFailures).Count -gt 6) {
    throw 'Compact research returned an externally visible array beyond the requested limit.'
}

$testOnly = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
    -Objective 'Add focused coverage for uncovered user administration branches' `
    -ProposedPath 'tests/FoodDiary.Application.Tests/Admin/UserAdministrationMutationServiceTests.cs' `
    -Limit 5 `
    -Format Json | ConvertFrom-Json
if ($testOnly.profile -ne 'test-only') { throw 'Focused coverage research did not select the test-only profile.' }

$testOnlyFlow = & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphResearch.ps1') `
    -Objective 'Add focused coverage for uncovered user administration branches' `
    -ProposedPath 'tests/FoodDiary.Application.Tests/Admin/UserAdministrationMutationServiceTests.cs' `
    -Limit 20 `
    -Format Json | ConvertFrom-Json
if (@($testOnlyFlow.dependencies).Count -eq 0) { throw 'Graph-backed runtime-flow evidence did not identify code referenced by the focused test.' }
if (@($testOnlyFlow.downstreamConsumers).Count -gt 20 -or @($testOnlyFlow.dependencies).Count -gt 20) { throw 'Graph research did not enforce its public result limit.' }
if (@($testOnlyFlow.downstreamConsumers.symbols) + @($testOnlyFlow.dependencies.symbols) | Where-Object { $_ -in @('Unit', 'DependencyInjection', 'Result') }) {
    throw 'Graph research exposed generic low-signal symbols.'
}
$researchSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1'))
if ($researchSource -notmatch 'runtimeFlowEvidence' -or $researchSource -notmatch 'Get-LlmWikiGraphResearch') { throw 'Ordinary research no longer attaches graph-backed runtime-flow evidence for PlannedPath.' }
if ($researchSource -notmatch '\$SkipHistory' -or $researchSource -notmatch "workflow\.profile\s*-eq\s*'test-only'") { throw 'Research no longer defers Git history for explicit or test-only fast paths.' }
if ($researchSource -notmatch 'failureStopwords' -or $researchSource -notmatch '\$matches\.Count\s*-lt\s*2') { throw 'Research failure matching no longer requires meaningful multi-token or path evidence.' }
$taskBriefSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Get-LlmWikiTaskBrief.ps1'))
$graphDependency = '.artifacts/llm-wiki/code-graph/code-graph.fingerprint'
if (-not $researchSource.Contains($graphDependency) -or -not $taskBriefSource.Contains($graphDependency)) {
    throw 'Research and task-brief caches do not depend on the graph fingerprint sidecar.'
}
if ($researchSource.Contains('.llm-wiki/generated/code-graph.sqlite') -or
    $taskBriefSource.Contains('.llm-wiki/generated/code-graph.sqlite') -or
    $researchSource.Contains('.artifacts/llm-wiki/code-graph/code-graph.sqlite') -or
    $taskBriefSource.Contains('.artifacts/llm-wiki/code-graph/code-graph.sqlite')) {
    throw 'Research or task-brief cache still hashes a live SQLite graph instead of its fingerprint sidecar.'
}

Write-Host 'LLM Wiki research confidence tests passed.'
