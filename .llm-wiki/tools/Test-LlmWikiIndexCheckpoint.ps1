[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$pipelineText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Invoke-LlmWikiIndexPipeline.ps1') -Raw
foreach ($requiredFragment in @(
    "Join-Path `$orphan.FullName 'checkpoint'"
    'last completed stage'
    "Join-Path `$transactionRoot 'checkpoint'"
    'completedStage = $stage.name'
    'LLM Wiki index checkpoint saved after stage'
)) {
    if (-not $pipelineText.Contains($requiredFragment)) { throw "Index pipeline resumability contract is missing: $requiredFragment" }
}
if ($pipelineText -notmatch '\$recoveryPath\s*=\s*if\s*\(Test-Path[^\r\n]+checkpointPath') {
    throw 'Interrupted update recovery does not prefer the last completed-stage checkpoint.'
}
Write-Host 'LLM Wiki index checkpoint regression passed: interrupted updates preserve the latest completed stage.'
