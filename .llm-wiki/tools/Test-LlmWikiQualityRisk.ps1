[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$result = & (Join-Path $PSScriptRoot 'Find-LlmWikiQualityRisk.ps1') -View test-gaps -Limit 2 -Format Json | ConvertFrom-Json
foreach ($item in @($result.items)) {
    if ($item.coverageClassification -ne 'direct-test-reference-absent' -or $item.confidence -ne 'medium') {
        throw 'Test-gap output lost its explicit evidence classification or calibrated confidence.'
    }
    if ($item.evidenceType -ne 'static-symbol-name-reference' -or [string]::IsNullOrWhiteSpace([string]$item.caveat)) {
        throw 'Test-gap output no longer explains its static evidence and coverage caveat.'
    }
}
$singleton = & (Join-Path $PSScriptRoot 'Find-LlmWikiQualityRisk.ps1') -View hotspots -Query 'Service' -Limit 1 -Format Json | ConvertFrom-Json
if ($singleton.count -ne 1 -or @($singleton.items).Count -ne 1) {
    throw 'Quality-risk query did not preserve a singleton result as an array.'
}
Write-Host 'LLM Wiki quality-risk regression passed: test gaps are typed, confidence-calibrated investigation leads.'
