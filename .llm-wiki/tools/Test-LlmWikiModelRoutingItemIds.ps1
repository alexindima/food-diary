[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$toolPath = Join-Path $PSScriptRoot 'Manage-LlmWikiModelRouting.ps1'
$source = Get-Content -LiteralPath $toolPath -Raw
$functionMatch = [regex]::Match(
    $source,
    '(?s)function Get-ItemIds\(\[object\[\]\]\$Items\) \{.*?^\}',
    [Text.RegularExpressions.RegexOptions]::Multiline)

if (-not $functionMatch.Success) {
    throw 'Get-ItemIds helper was not found in Manage-LlmWikiModelRouting.ps1.'
}

Invoke-Expression $functionMatch.Value

$mixed = @(
    [pscustomobject]@{ id = 'privacy-review' },
    [pscustomobject]@{ description = 'legacy item without an id' },
    'security-review',
    ' ',
    $null,
    [pscustomobject]@{ id = 'privacy-review' }
)

$actual = @(Get-ItemIds $mixed)
$expected = @('privacy-review', 'security-review')
if ($actual.Count -ne $expected.Count -or @(Compare-Object $expected $actual).Count -ne 0) {
    throw "Mixed item ID projection was incorrect. Expected '$($expected -join ', ')', got '$($actual -join ', ')'."
}

$normal = @(Get-ItemIds @([pscustomobject]@{ id = 'adr-review' }, [pscustomobject]@{ id = 'rollout-review' }))
if (@(Compare-Object @('adr-review', 'rollout-review') $normal).Count -ne 0) {
    throw 'Normal object ID projection regressed.'
}

Write-Host 'LLM Wiki model-routing item ID regression passed.'
