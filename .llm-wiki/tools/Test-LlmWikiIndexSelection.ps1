[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$pipelinePath = Join-Path $PSScriptRoot 'Invoke-LlmWikiIndexPipeline.ps1'

function Get-IndexPlan([string[]]$ChangedPath, [switch]$RequiredOnly) {
    return (& $pipelinePath -AffectedOnly -Plan -ChangedPath $ChangedPath -RequiredOnly:$RequiredOnly) -join [Environment]::NewLine
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

$frontendTestPlan = Get-IndexPlan 'FoodDiary.Web.Client/src/app/example/example.spec.ts'
Assert-Plan ($frontendTestPlan -match 'Build-LlmWikiQualityIndex.ps1' -and $frontendTestPlan -notmatch 'Build-LlmWikiArchitectureHealthIndex.ps1') 'Frontend tests should update quality without architecture health.'

$csharpTestPlan = Get-IndexPlan 'tests/FoodDiary.Infrastructure.Tests/Persistence/EmailOutboxTests.cs'
Assert-Plan ($csharpTestPlan -match 'Build-LlmWikiQualityIndex.ps1') 'C# tests should update the quality index.'
foreach ($unexpectedTool in @(
    'Build-LlmWikiCatalog.ps1',
    'Build-LlmWikiSymbolIndex.ps1',
    'Build-LlmWikiBackendContractIndex.ps1',
    'Build-LlmWikiSensitiveDataIndex.ps1',
    'Build-LlmWikiModulePages.ps1',
    'Build-LlmWikiArchitectureHealthIndex.ps1'
)) {
    Assert-Plan ($csharpTestPlan -notmatch [regex]::Escape($unexpectedTool)) "C# test-only change selected unrelated index: $unexpectedTool"
}

$changedPathFile = Join-Path $repositoryRoot '.artifacts/llm-wiki/index-selection-staged-paths.txt'
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $changedPathFile) -Force
try {
    [IO.File]::WriteAllLines($changedPathFile, @(
        'tests/FoodDiary.Infrastructure.Tests/Persistence/EmailOutboxTests.cs',
        '.llm-wiki/generated/quality-index.json',
        '.llm-wiki/reviews/source-impact-reviews.json'
    ))
    $stagedPlan = (& $pipelinePath -AffectedOnly -Plan -ChangedPathFile $changedPathFile) -join [Environment]::NewLine
    Assert-Plan ($stagedPlan -match 'Build-LlmWikiQualityIndex.ps1') 'ChangedPathFile did not preserve the staged test-only delta.'
    Assert-Plan ($stagedPlan -notmatch 'Build-LlmWikiBackendContractIndex.ps1') 'Generated/review paths widened the staged test-only index plan.'
} finally {
    Remove-Item -LiteralPath $changedPathFile -Force -ErrorAction SilentlyContinue
}

$productionCSharpPlan = Get-IndexPlan 'FoodDiary.Infrastructure/Persistence/EmailOutbox.cs'
Assert-Plan ($productionCSharpPlan -match 'Build-LlmWikiCatalog.ps1' -and $productionCSharpPlan -match 'Build-LlmWikiSymbolIndex.ps1') 'Production C# changes lost conservative index coverage.'
$requiredProductionPlan = Get-IndexPlan 'FoodDiary.Infrastructure/Persistence/EmailOutbox.cs' -RequiredOnly
Assert-Plan ($requiredProductionPlan -match 'Build-LlmWikiCatalog.ps1' -and $requiredProductionPlan -match 'Build-LlmWikiBackendContractIndex.ps1') 'Required-only mode lost contract/navigation generators.'
foreach ($deferredTool in @('Build-LlmWikiQualityIndex.ps1', 'Build-LlmWikiModulePages.ps1', 'Build-LlmWikiArchitectureHealthIndex.ps1')) {
    Assert-Plan ($requiredProductionPlan -notmatch [regex]::Escape($deferredTool)) "Required-only mode retained analytical generator: $deferredTool"
}

$moduleManifestPlan = Get-IndexPlan 'docs/architecture/backend-modules.json'
foreach ($expectedTool in @('Build-LlmWikiCatalog.ps1', 'Build-LlmWikiModulePages.ps1', 'Build-LlmWikiArchitectureHealthIndex.ps1')) {
    Assert-Plan ($moduleManifestPlan -match [regex]::Escape($expectedTool)) "Backend module manifest omitted generator: $expectedTool"
}

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
