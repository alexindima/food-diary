[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$facadeText = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') -Raw
$fullVerificationText = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/tools/Invoke-LlmWikiFullVerification.ps1') -Raw

if ($facadeText -notmatch "'verify-strict-affected'") { throw 'Wiki facade does not expose verify-strict-affected.' }
if (-not $facadeText.Contains("`$Command = 'verify-fast'") -or -not $facadeText.Contains('Compatibility alias: verify -Fast -> verify-fast')) {
    throw 'Wiki facade does not support the verify -Fast compatibility alias.'
}
foreach ($progressContract in @('Starting Wiki verify stage:', 'Wiki verify stage still running:', 'Wiki verify stage timed out:', 'Run separately:')) {
    if (-not $facadeText.Contains($progressContract)) { throw "Observed verify omitted '$progressContract'." }
}
if ($facadeText -match 'Start-Job' -or -not $facadeText.Contains('Invoke-LlmWikiObservedStage.ps1')) {
    throw 'Observed verify must use an inherited-output child process rather than a buffered PowerShell job.'
}
$verifyStart = $facadeText.IndexOf("    'verify' {")
$verifyEnd = $facadeText.IndexOf("    'verify-fast' {", $verifyStart)
$verifyBody = if ($verifyStart -ge 0 -and $verifyEnd -gt $verifyStart) { $facadeText.Substring($verifyStart, $verifyEnd - $verifyStart) } else { '' }
if (@([regex]::Matches($verifyBody, 'Invoke-ObservedWikiStage')).Count -lt 8) {
    throw 'Ordinary verify does not route every verification stage through the observed runner.'
}
$strictStart = $facadeText.IndexOf("    'verify-strict-affected' {")
$strictEnd = $facadeText.IndexOf("    'verify-full' {", $strictStart)
if ($strictStart -lt 0 -or $strictEnd -le $strictStart) { throw 'Unable to isolate verify-strict-affected implementation.' }
$body = $facadeText.Substring($strictStart, $strictEnd - $strictStart)
foreach ($required in @('AffectedOnly = $true', 'Invoke-LlmWikiAffectedSmoke.ps1', 'FailOnViolation = $true', 'FailOnUnreviewed = $true')) {
    if (-not $body.Contains($required)) { throw "Strict affected verification omitted '$required'." }
}
if ($body -match 'ReuseUnchangedChecks|DeferPossiblyConcurrentStale') {
    throw 'Strict affected verification unexpectedly enables cache reuse or stale deferral.'
}
if ($body -match 'Invoke-ObservedWikiStage') { throw 'Strict affected verification accidentally inherited the full observed verify stages.' }

$frontendSmoke = @(& (Join-Path $repositoryRoot '.llm-wiki/tools/Invoke-LlmWikiAffectedSmoke.ps1') `
    -Plan -ChangedPath 'FoodDiary.Web.Client/src/app/example/example.ts' 6>&1 | ForEach-Object { $_.ToString() })
if (($frontendSmoke -join "`n") -notmatch 'no LLM Wiki implementation paths changed' -or ($frontendSmoke -join "`n") -match 'full-tools') {
    throw 'Strict affected verification expanded a product-only change into the monolithic Wiki tools smoke.'
}
if ($fullVerificationText -notmatch 'still running' -or $fullVerificationText -notmatch 'groupStopwatch') {
    throw 'Full verification does not expose periodic per-group progress and duration.'
}

Write-Host 'LLM Wiki strict-affected smoke passed: scoped publication checks are uncached and non-deferred.'
