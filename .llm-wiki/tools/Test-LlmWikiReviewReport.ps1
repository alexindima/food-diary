[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$report = & (Join-Path $PSScriptRoot 'Get-LlmWikiReviewReport.ps1') `
    -ChangedPath 'FoodDiary.Web.Client/src/app/features/dashboard/pages/dashboard.spec.ts' `
    -Format Markdown
$content = $report -join [Environment]::NewLine
foreach ($heading in @('### Check execution', '### Additional verification tiers', '### Generated artifacts')) {
    if ($content -notmatch [regex]::Escape($heading)) { throw "Review report omitted '$heading'." }
}
if ($content -notmatch 'recommendations, not proof of execution' -and $content -notmatch '\*\*(?:passed|failed|pending|not-applicable)\*\*') {
    throw 'Review report did not distinguish recommendations from executed evidence.'
}
$jsonReport = & (Join-Path $PSScriptRoot 'Get-LlmWikiReviewReport.ps1') `
    -ChangedPath 'FoodDiary.Web.Client/src/app/features/dashboard/pages/dashboard.spec.ts' `
    -Format Json | ConvertFrom-Json
if ($jsonReport.schemaVersion -ne 2) { throw 'Review report did not publish the normalized finding contract.' }
if ($jsonReport.freshness.state -ne 'assessment-snapshot' -or @($jsonReport.freshness.artifactHashes).Count -ne 3 -or -not $jsonReport.freshness.assessedAtUtc) { throw 'Review report omitted its assessment inputs and time.' }
if ($content -notmatch 'LLM_WIKI_REPORT_FRESHNESS' -or $content -notmatch 'later validation supersedes') { throw 'Markdown readiness was presented without snapshot provenance.' }
foreach ($field in @('id', 'severity', 'kind', 'area', 'blocking', 'anchorStatus', 'location', 'trigger', 'consequence', 'testGap', 'remediation', 'evidence')) {
    if ($field -notin @($jsonReport.findingContract.requiredFields)) { throw "Review finding contract omitted '$field'." }
}
foreach ($finding in @($jsonReport.findings)) {
    if ($finding.severity -notin @('critical', 'major', 'warning', 'info')) { throw "Review finding emitted invalid severity '$($finding.severity)'." }
    if ($finding.kind -notin @('defect', 'suggestion', 'question')) { throw "Review finding emitted invalid kind '$($finding.kind)'." }
    if ([string]::IsNullOrWhiteSpace([string]$finding.testGap) -or [string]::IsNullOrWhiteSpace([string]$finding.remediation)) { throw 'Review finding omitted test-gap or remediation detail.' }
}
Write-Host 'LLM Wiki review-report regression passed.'
