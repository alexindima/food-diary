[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$research = & (Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1') `
    -Objective 'Assess remaining IUserContextService extraction blockers' `
    -Purpose Assessment `
    -Limit 5 `
    -Format Json | ConvertFrom-Json

if ($research.workflow.confidence -ne 'high') { throw 'Grounded assessment discovery did not raise overall research confidence.' }
if ($research.workflow.confidenceDimensions.discovery -ne 'high') { throw 'Research did not expose high grounded discovery confidence.' }
if ($research.workflow.confidenceDimensions.blockerCount -ne 'high') { throw 'Extraction assessment did not expose high blocker-count confidence.' }
if ($research.workflow.confidenceDimensions.implementationScope -ne 'not-required') { throw 'Read-only assessment incorrectly rated an implementation scope.' }
if (@($research.workflow.confidenceReasons).Count -lt 3) { throw 'Research confidence does not explain each dimension.' }
if ($research.readiness.designCheckpoint -ne 'not-required') { throw 'Read-only assessment incorrectly requires a design checkpoint.' }

Write-Host 'LLM Wiki research confidence tests passed.'
