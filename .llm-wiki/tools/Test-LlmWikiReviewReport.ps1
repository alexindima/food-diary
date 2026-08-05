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
Write-Host 'LLM Wiki review-report regression passed.'
