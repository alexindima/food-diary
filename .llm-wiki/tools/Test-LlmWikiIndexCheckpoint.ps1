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
    "status = 'initializing'"
    'Write-IndexTransactionState'
    'Removing incomplete markerless LLM Wiki index transaction'
    'Enter-IndexUpdateLock'
    'Another LLM Wiki index update is running; waiting to reuse its result when inputs match.'
    'Concurrent LLM Wiki index result reused'
    'Test-PipelineCacheReceipt'
    'Write-PipelineCacheReceipt'
)) {
    if (-not $pipelineText.Contains($requiredFragment)) { throw "Index pipeline resumability contract is missing: $requiredFragment" }
}
if ($pipelineText -notmatch '\$recoveryPath\s*=\s*if\s*\(Test-Path[^\r\n]+checkpointPath') {
    throw 'Interrupted update recovery does not prefer the last completed-stage checkpoint.'
}
if ($pipelineText.IndexOf('Restore-OrphanedIndexTransaction -TransactionStateRoot', [StringComparison]::Ordinal) -lt
    $pipelineText.IndexOf('$lock = Enter-IndexUpdateLock $lockPath', [StringComparison]::Ordinal)) {
    throw 'Interrupted update recovery must run only after the exclusive update lock is acquired.'
}
if ($pipelineText.IndexOf('Write-PipelineCacheReceipt $finalPipelineCacheState', [StringComparison]::Ordinal) -gt
    $pipelineText.IndexOf('if ($updateLock) { $updateLock.Dispose() }', [StringComparison]::Ordinal)) {
    throw 'Concurrent update evidence must be published before the exclusive update lock is released.'
}
Write-Host 'LLM Wiki index checkpoint regression passed: interrupted updates preserve the latest completed stage.'
