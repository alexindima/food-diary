[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiQualityText.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$unorderedPaths = [string[]]@('z.ps1', 'A.cs', 'a.cs', 'A.cs', 'путь.ts', "$([char]0xe000).cs", "$([char]::ConvertFromUtf32(0x1f600)).cs")
$expectedPaths = @($unorderedPaths | Sort-Object { Get-LlmWikiOrdinalSortKey $_ } -Unique)
if (([LlmWiki.QualityText]::OrderPaths($unorderedPaths) | ConvertTo-Json -Compress) -cne ($expectedPaths | ConvertTo-Json -Compress)) {
    throw 'Compiled path ordering differs from UTF-8 sort keys or duplicate handling.'
}
$names = [string[]]@('Handler', 'QueryHandler', 'handler', 'Имя', 'a.b', 'Missing')
$paths = [string[]]@('first.cs', 'second.cs', 'empty.cs')
$contents = [string[]]@('QueryHandler QueryHandler Имя a.b', 'handler Handler', '')
$references = [LlmWiki.QualityText]::FindReferences($names, $paths, $contents)
foreach ($name in $names) {
    $expected = @(for ($i = 0; $i -lt $paths.Count; $i++) {
        if ($contents[$i].IndexOf($name, [StringComparison]::Ordinal) -ge 0) { $paths[$i] }
    })
    if (($references[$name] | ConvertTo-Json -Compress) -cne ($expected | ConvertTo-Json -Compress)) {
        throw "Compiled quality references differ for '$name'."
    }
}
if ([LlmWiki.QualityText]::FindReferences([string[]]@(), [string[]]@(), [string[]]@()).Count -ne 0) {
    throw 'Empty quality input produced references.'
}
foreach ($content in @('', "`n", "`r`n", "a`r`nb`n", "a`rb", " `t`r`n", "a`n`n b", ([string][char]0x00a0), "x$([char]0x2028)y")) {
    $expected = @($content -split '\r?\n' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
    if ([LlmWiki.QualityText]::CountNonBlankLines($content) -ne $expected) {
        throw 'Compiled nonblank line count differs from the original whitespace semantics.'
    }
}
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
