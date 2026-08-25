[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$snapshotRoot = Join-Path $repositoryRoot ".artifacts/llm-wiki/snapshot-regression-$([guid]::NewGuid().ToString('N'))"
$snapshotPath = Join-Path $snapshotRoot 'snapshot.json'
try {
    & (Join-Path $PSScriptRoot 'Write-LlmWikiContextEvaluationSnapshot.ps1') `
        -CorpusPath '.llm-wiki/evals/context-search-probe-2.json' `
        -OutputPath $snapshotPath `
        -SkipBuild `
        -Iterations 1 `
        -WarmupIterations 0 | Out-Null
    $snapshot = Get-Content -LiteralPath $snapshotPath -Raw | ConvertFrom-Json
    if ($snapshot.schemaVersion -ne 2 -or $snapshot.caseCount -ne 30 -or
        [string]::IsNullOrWhiteSpace([string]$snapshot.corpusSha256) -or
        [string]::IsNullOrWhiteSpace([string]$snapshot.rankingPolicySha256) -or
        [string]::IsNullOrWhiteSpace([string]$snapshot.workingTree.diffSha256) -or
        [string]::IsNullOrWhiteSpace([string]$snapshot.runtime.codeGraphParserVersion) -or
        $snapshot.performance.measuredIterations -ne 1 -or
        $null -eq $snapshot.failureCategoryMetrics) {
        throw 'Context evaluation snapshot omitted provenance, performance, or failure-classification evidence.'
    }
    Write-Host 'LLM Wiki context evaluation snapshot regression passed.'
} finally {
    if (Test-Path -LiteralPath $snapshotRoot) { Remove-Item -LiteralPath $snapshotRoot -Recurse -Force }
}
