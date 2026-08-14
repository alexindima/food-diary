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

$compact = & (Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1') `
    -Objective 'Extract Dietologist application module into a separate project' `
    -Module Dietologist `
    -Compact `
    -Purpose Assessment `
    -Limit 6 `
    -Format Json | ConvertFrom-Json
if (@($compact.discovery.groundedPaths | Where-Object { $_ -match 'Dietologist' }).Count -eq 0) { throw 'Compact module research did not stay grounded in the selected module.' }
if (@($compact.precedents).Count -ne 0) { throw 'Compact research unexpectedly ran historical precedent analysis.' }

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
$researchSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1'))
if ($researchSource -notmatch 'runtimeFlowEvidence' -or $researchSource -notmatch 'Get-LlmWikiGraphResearch') { throw 'Ordinary research no longer attaches graph-backed runtime-flow evidence for PlannedPath.' }
if ($researchSource -notmatch '\$SkipHistory' -or $researchSource -notmatch "workflow\.profile\s*-eq\s*'test-only'") { throw 'Research no longer defers Git history for explicit or test-only fast paths.' }

Write-Host 'LLM Wiki research confidence tests passed.'
