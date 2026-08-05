[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$pipelinePath = Join-Path $PSScriptRoot 'Invoke-LlmWikiIndexPipeline.ps1'

function Get-IndexPlan([string[]]$ChangedPath) {
    return (& $pipelinePath -AffectedOnly -Plan -ChangedPath $ChangedPath) -join [Environment]::NewLine
}

function Assert-Plan([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

foreach ($path in @(
    '.llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1',
    '.llm-wiki/tools/LlmWikiChangeSemantics.ps1',
    '.llm-wiki/tools/Test-LlmWikiTools.ps1'
)) {
    $plan = Get-IndexPlan $path
    Assert-Plan ($plan -match 'Affected index tools:\s*$' -and $plan -notmatch 'Build-LlmWiki') "Routing/test-only path selected indexes: $path"
}

$frontendPlan = Get-IndexPlan '.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1'
Assert-Plan ($frontendPlan -match 'Build-LlmWikiFrontendIndex.ps1' -and $frontendPlan -notmatch 'Build-LlmWikiArchitectureHealthIndex.ps1') 'Frontend builder dependency closure is incorrect.'

$contractPlan = Get-IndexPlan '.llm-wiki/tools/Build-LlmWikiFrontendContractIndex.ps1'
Assert-Plan ($contractPlan -match 'Build-LlmWikiFrontendContractIndex.ps1' -and $contractPlan -match 'Build-LlmWikiArchitectureHealthIndex.ps1' -and
    $contractPlan -notmatch 'Build-LlmWikiQualityIndex.ps1') 'Frontend contract builder dependency closure is incorrect.'

$symbolPlan = Get-IndexPlan '.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1'
foreach ($expectedTool in @(
    'Build-LlmWikiSymbolIndex.ps1',
    'Build-LlmWikiBackendContractIndex.ps1',
    'Build-LlmWikiQualityIndex.ps1',
    'Build-LlmWikiArchitectureHealthIndex.ps1'
)) {
    Assert-Plan ($symbolPlan -match [regex]::Escape($expectedTool)) "Symbol builder omitted dependent tool: $expectedTool"
}

$sharedJsonPlan = Get-IndexPlan '.llm-wiki/tools/LlmWikiJson.ps1'
Assert-Plan ([regex]::Matches($sharedJsonPlan, 'Build-LlmWiki').Count -eq 12) 'Shared JSON helper did not select all indexes.'

Write-Host 'LLM Wiki index-selection regression passed.'
