[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testPath = 'tests/FoodDiary.Application.Tests/Authentication/UserAgentParserTests.cs'
$generatedPath = '.llm-wiki/generated/quality-index.json'
$tempRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/test-only-governance'
$manifestPath = '.artifacts/llm-wiki/test-only-governance/change-manifest.json'
$acceptancePath = '.artifacts/llm-wiki/test-only-governance/acceptance-matrix.json'

function Assert-TestOnly([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
$null = New-Item -ItemType Directory -Path $tempRoot -Force
try {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeManifest.ps1') init `
        -Path $manifestPath `
        -Objective 'Add authentication parser coverage without production changes.' `
        -ChangedPath @($testPath, $generatedPath) `
        -AllowedPath '^tests/' | Out-Null
    $manifest = Get-Content -LiteralPath (Join-Path $repositoryRoot $manifestPath) -Raw | ConvertFrom-Json
    Assert-TestOnly (@($manifest.scope.plannedPaths) -contains $testPath) 'The allowed test delta was not planned automatically.'
    Assert-TestOnly (@($manifest.scope.plannedPaths) -notcontains $generatedPath) 'Derived Wiki output leaked into planned product scope.'

    & (Join-Path $PSScriptRoot 'Manage-LlmWikiAcceptanceMatrix.ps1') init `
        -Path $acceptancePath `
        -Objective 'Add authentication parser coverage without production changes.' `
        -Criterion @('Valid agents are parsed.', 'Malformed agents remain safe.') `
        -ChangedPath @($testPath, $generatedPath) | Out-Null
    $acceptance = Get-Content -LiteralPath (Join-Path $repositoryRoot $acceptancePath) -Raw | ConvertFrom-Json
    Assert-TestOnly ([bool]$acceptance.automaticMapping.applied) 'Test-only acceptance mapping was not enabled.'
    foreach ($criterion in @($acceptance.criteria)) {
        Assert-TestOnly (@($criterion.mapping.changedPaths) -contains $testPath) "Criterion $($criterion.id) was not linked to the test delta."
        Assert-TestOnly (@($criterion.mapping.changedPaths) -notcontains $generatedPath) "Criterion $($criterion.id) was linked to derived Wiki output."
    }
    Write-Host 'LLM Wiki test-only governance regression passed: routing scope, manifest, and acceptance mapping stay proportional.'
} finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
