[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$qualityPath = Join-Path $repositoryRoot '.llm-wiki/generated/quality-index.json'
if (-not (Test-Path -LiteralPath $qualityPath -PathType Leaf)) {
    throw 'Quality index is absent.'
}
$quality = Get-Content -LiteralPath $qualityPath -Raw | ConvertFrom-Json
$wikiToolSymbols = @($quality.criticalSymbols | Where-Object role -eq 'WikiTool')
$wikiFiles = @($quality.files | Where-Object path -match '^\.llm-wiki/(?:tools/|wiki\.ps1$)')
if ([int]$quality.schemaVersion -ne 2 -or [int]$quality.summary.wikiToolFiles -lt 150 -or
    $wikiFiles.Count -ne [int]$quality.summary.wikiToolFiles -or $wikiToolSymbols.Count -lt 150) {
    throw 'Quality index does not measure the Wiki tool surface.'
}
if (@($quality.hotspots | Where-Object path -eq '.llm-wiki/wiki.ps1').Count -ne 1) {
    throw 'Quality hotspots do not expose the Wiki facade maintenance risk.'
}
if (@($wikiFiles | Where-Object { [IO.Path]::GetFileName($_.path) -match '^Test-' }).Count -ne 0) {
    throw 'Wiki regression scripts were incorrectly classified as production tools.'
}
if (@($wikiToolSymbols | Where-Object testReferenceCount -eq 0).Count -ne [int]$quality.summary.wikiToolsWithoutTestReferences) {
    throw 'Wiki tool test-reference gaps do not match the quality summary.'
}
Write-Host "LLM Wiki self-quality regression passed: $($wikiFiles.Count) tools are represented in hotspots and test-reference coverage."
