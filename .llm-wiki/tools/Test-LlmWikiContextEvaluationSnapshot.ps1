[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$snapshotRoot = Join-Path $repositoryRoot ".artifacts/llm-wiki/snapshot-regression-$([guid]::NewGuid().ToString('N'))"
$externalSnapshotRoot = Join-Path ([IO.Path]::GetTempPath()) "llm-wiki-snapshot-regression-$([guid]::NewGuid().ToString('N'))"
$snapshotPath = Join-Path $externalSnapshotRoot 'snapshot.json'
$summaryPath = Join-Path $snapshotRoot 'summary.md'
try {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') build -Format Json | Out-Null
    & (Join-Path $PSScriptRoot 'Write-LlmWikiContextEvaluationSnapshot.ps1') `
        -CorpusPath '.llm-wiki/evals/context-search-probe-2.json' `
        -OutputPath $snapshotPath `
        -SummaryOutputPath $summaryPath `
        -SkipBuild `
        -Iterations 1 `
        -WarmupIterations 0 | Out-Null
    $snapshot = Get-Content -LiteralPath $snapshotPath -Raw | ConvertFrom-Json
    if ($snapshot.schemaVersion -ne 3 -or $snapshot.caseCount -ne 30 -or
        [string]::IsNullOrWhiteSpace([string]$snapshot.corpusSha256) -or
        [string]::IsNullOrWhiteSpace([string]$snapshot.rankingPolicySha256) -or
        [string]::IsNullOrWhiteSpace([string]$snapshot.workingTree.diffSha256) -or
        [string]::IsNullOrWhiteSpace([string]$snapshot.runtime.codeGraphParserVersion) -or
        [string]$snapshot.runtime.reader -ne 'node-sqlite' -or
        [string]::IsNullOrWhiteSpace([string]$snapshot.runtime.codeGraphSourceSha256) -or
        [string]$snapshot.changeSetFingerprint -ne [string]$snapshot.graphChangeSetFingerprint -or
        $snapshot.performance.measuredIterations -ne 1 -or
        $null -eq $snapshot.failureCategoryMetrics) {
        throw 'Context evaluation snapshot omitted provenance, performance, or failure-classification evidence.'
    }
    $summary = Get-Content -LiteralPath $summaryPath -Raw
    if ($summary -notmatch '# Context retrieval evaluation' -or
        $summary -notmatch 'Top-1' -or
        $summary -notmatch 'corpus.*policy' -or
        $summary -notmatch 'node-sqlite' -or
        $summary -notmatch [regex]::Escape($snapshotPath.Replace('\', '/'))) {
        throw 'Context evaluation snapshot did not publish its generated Markdown summary.'
    }
    $staleProbePath = Join-Path $repositoryRoot '.llm-wiki/context-evaluation-stale-probe.tmp'
    [IO.File]::WriteAllText($staleProbePath, 'stale-probe', [Text.UTF8Encoding]::new($false))
    $staleRejected = $false
    try {
        & (Join-Path $PSScriptRoot 'Write-LlmWikiContextEvaluationSnapshot.ps1') `
            -CorpusPath '.llm-wiki/evals/context-search-probe-2.json' `
            -OutputPath (Join-Path $externalSnapshotRoot 'stale.json') `
            -SkipBuild `
            -Iterations 1 `
            -WarmupIterations 0 | Out-Null
    } catch {
        $staleRejected = $_.Exception.Message -match 'stale code graph'
    } finally {
        Remove-Item -LiteralPath $staleProbePath -Force -ErrorAction SilentlyContinue
    }
    if (-not $staleRejected) { throw 'Context evaluation snapshot accepted a stale code graph.' }
    Write-Host 'LLM Wiki context evaluation snapshot regression passed.'
} finally {
    if (Test-Path -LiteralPath $snapshotRoot) { Remove-Item -LiteralPath $snapshotRoot -Recurse -Force }
    if (Test-Path -LiteralPath $externalSnapshotRoot) { Remove-Item -LiteralPath $externalSnapshotRoot -Recurse -Force }
}
