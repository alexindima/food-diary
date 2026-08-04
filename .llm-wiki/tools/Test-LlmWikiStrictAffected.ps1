[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$facadeText = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') -Raw
$fullVerificationText = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/tools/Invoke-LlmWikiFullVerification.ps1') -Raw

if ($facadeText -notmatch "'verify-strict-affected'") { throw 'Wiki facade does not expose verify-strict-affected.' }
$caseMatch = [regex]::Match($facadeText, "(?s)'verify-strict-affected'\s*\{(?<body>.*?)\n\s*\}\n\s*'verify-full'")
if (-not $caseMatch.Success) { throw 'Unable to isolate verify-strict-affected implementation.' }
$body = $caseMatch.Groups['body'].Value
foreach ($required in @('AffectedOnly = $true', 'Invoke-LlmWikiAffectedSmoke.ps1', 'FailOnViolation = $true', 'FailOnUnreviewed = $true')) {
    if (-not $body.Contains($required)) { throw "Strict affected verification omitted '$required'." }
}
if ($body -match 'ReuseUnchangedChecks|DeferPossiblyConcurrentStale') {
    throw 'Strict affected verification unexpectedly enables cache reuse or stale deferral.'
}

$frontendSmoke = @(& (Join-Path $repositoryRoot '.llm-wiki/tools/Invoke-LlmWikiAffectedSmoke.ps1') `
    -Plan -ChangedPath 'FoodDiary.Web.Client/src/app/example/example.ts' 6>&1 | ForEach-Object { $_.ToString() })
if (($frontendSmoke -join "`n") -notmatch 'no LLM Wiki implementation paths changed' -or ($frontendSmoke -join "`n") -match 'full-tools') {
    throw 'Strict affected verification expanded a product-only change into the monolithic Wiki tools smoke.'
}
if ($fullVerificationText -notmatch 'still running' -or $fullVerificationText -notmatch 'groupStopwatch') {
    throw 'Full verification does not expose periodic per-group progress and duration.'
}

Write-Host 'LLM Wiki strict-affected smoke passed: scoped publication checks are uncached and non-deferred.'
