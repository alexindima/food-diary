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
    if ($item.coverageEvidence.directReference -ne 'absent' -or
        $item.coverageEvidence.indirectCoverage -ne 'unknown' -or
        $item.coverageEvidence.measuredExecutionCoverage -ne 'not-measured') {
        throw 'Test-gap output no longer separates direct references, indirect coverage, and measured execution coverage.'
    }
}
$jsonResult = & (Join-Path $PSScriptRoot 'Find-LlmWikiQualityRisk.ps1') `
    -View test-gaps -Limit 2 -CompiledIndexSource Json -Format Json | ConvertFrom-Json
if (@($jsonResult.items | Where-Object { $_.coverageEvidence.measuredExecutionCoverage -eq 'not-measured' }).Count -ne @($jsonResult.items).Count) {
    throw 'Explicit JSON test-gap output lost its coverage-evidence classification.'
}
$singleton = & (Join-Path $PSScriptRoot 'Find-LlmWikiQualityRisk.ps1') -View hotspots -Query 'Service' -Limit 1 -Format Json | ConvertFrom-Json
if ($singleton.count -ne 1 -or @($singleton.items).Count -ne 1) {
    throw 'Quality-risk query did not preserve a singleton result as an array.'
}
$product = & (Join-Path $PSScriptRoot 'Find-LlmWikiQualityRisk.ps1') -View hotspots -Area Product -Limit 100 -Format Json | ConvertFrom-Json
if (@($product.items | Where-Object path -match '^\.llm-wiki/|^FoodDiary\.Development\.Mcp/|^tests/FoodDiary\.Development\.Mcp\.Tests/').Count -gt 0) {
    throw 'Product quality view included Wiki/MCP implementation noise.'
}
$wiki = & (Join-Path $PSScriptRoot 'Find-LlmWikiQualityRisk.ps1') -View hotspots -Area Wiki -Query 'FoodDiary.Development.Mcp' -Limit 100 -Format Json | ConvertFrom-Json
if (@($wiki.items | Where-Object path -match '^FoodDiary\.Development\.Mcp/').Count -eq 0) {
    throw 'Wiki quality view omitted the Development MCP implementation surface.'
}
Write-Host 'LLM Wiki quality-risk regression passed: test gaps are typed, confidence-calibrated investigation leads.'
