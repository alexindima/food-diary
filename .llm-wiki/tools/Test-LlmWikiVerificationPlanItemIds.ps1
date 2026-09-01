[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$toolPath = Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationPlan.ps1'
$source = Get-Content -LiteralPath $toolPath -Raw
$functionMatch = [regex]::Match(
    $source,
    '(?s)function Get-ItemIds\(\[object\[\]\]\$Items\) \{.*?^\}',
    [Text.RegularExpressions.RegexOptions]::Multiline)

if (-not $functionMatch.Success) {
    throw 'Get-ItemIds helper was not found in Manage-LlmWikiVerificationPlan.ps1.'
}

Invoke-Expression $functionMatch.Value

$empty = @(Get-ItemIds @())
if ($empty.Count -ne 0) {
    throw "Empty item ID projection returned $($empty.Count) values."
}

$mixed = @(
    [pscustomobject]@{ id = 'architecture-tests' },
    [pscustomobject]@{ description = 'legacy item without an id' },
    'domain-tests',
    ' ',
    $null,
    [pscustomobject]@{ id = 'architecture-tests' }
)
$actual = @(Get-ItemIds $mixed)
$expected = @('architecture-tests', 'domain-tests')
if ($actual.Count -ne $expected.Count -or @(Compare-Object $expected $actual).Count -ne 0) {
    throw "Mixed item ID projection was incorrect. Expected '$($expected -join ', ')', got '$($actual -join ', ')' ."
}

Write-Host 'LLM Wiki verification-plan item ID regression passed.'
