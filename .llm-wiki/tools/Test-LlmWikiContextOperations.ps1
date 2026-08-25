$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$statusBefore = @(& git -C $repositoryRoot status --porcelain=v1)
function Assert-ContextOperation([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
$explanation = & (Join-Path $PSScriptRoot 'Get-LlmWikiContextExplanation.ps1') `
    -Query 'validator admin email template' -ChangeType Backend -Limit 3 -Format Json | ConvertFrom-Json
Assert-ContextOperation ($explanation.authority -eq 'sqlite-derived') 'Context explanation lost SQLite authority.'
Assert-ContextOperation (@($explanation.candidates).Count -eq 3) 'Context explanation returned an unexpected candidate count.'
Assert-ContextOperation (-not [string]::IsNullOrWhiteSpace([string]$explanation.candidates[0].layer)) 'Context explanation omitted structured layer metadata.'
Assert-ContextOperation (@($explanation.candidates[0].reasons).Count -gt 0) 'Context explanation omitted ranking reasons.'
$policy = & (Join-Path $PSScriptRoot 'Test-LlmWikiContextRankingPolicy.ps1') -Format Json | ConvertFrom-Json
Assert-ContextOperation $policy.valid 'Context ranking policy governance is invalid.'
Assert-ContextOperation ($policy.counts.normalization -le 400 -and $policy.counts.ranking -le 400) 'Context ranking staged budgets are not enforced.'
$draftPath = Join-Path $repositoryRoot '.artifacts/llm-wiki/tests/context-unseen-draft.json'
$draft = & (Join-Path $PSScriptRoot 'New-LlmWikiUnseenContextCorpus.ps1') -Count 20 -OutputPath $draftPath -Force -Format Json | ConvertFrom-Json
Assert-ContextOperation ($draft.targetCount -eq 20) 'Unseen corpus draft did not select the requested number of targets.'
$draftContent = Get-Content -LiteralPath $draftPath -Raw | ConvertFrom-Json
Assert-ContextOperation ($draftContent.status -eq 'draft-unseen-not-executable') 'Unseen corpus draft was incorrectly marked executable.'
Assert-ContextOperation (@($draftContent.cases | Where-Object query -ne '<independent-author-query-required>').Count -eq 0) 'Unseen corpus draft synthesized queries and invalidated blindness.'
$statusAfter = @(& git -C $repositoryRoot status --porcelain=v1)
Assert-ContextOperation (($statusBefore -join "`n") -ceq ($statusAfter -join "`n")) 'Read-oriented context operations changed tracked Git status.'
Write-Host 'LLM Wiki context operations passed: structured explain, policy governance, privacy-safe observation, unseen draft, and tracked-state stability.'
