[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$testPath = 'tests/FoodDiary.Application.Tests/Authentication/UserAgentParserTests.cs'
$generatedPath = '.llm-wiki/generated/quality-index.json'
$tempRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'test-only-governance'
$tempRootRelative = $tempRoot.Substring($repositoryRoot.Length + 1).Replace('\', '/')
$manifestPath = "$tempRootRelative/change-manifest.json"
$acceptancePath = "$tempRootRelative/acceptance-matrix.json"

function Assert-TestOnly([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

try {
    Write-Host 'Test-only governance [1/4]: initialize manifest.'
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeManifest.ps1') init `
        -Path $manifestPath `
        -Objective 'Add authentication parser coverage without production changes.' `
        -ChangedPath @($testPath, $generatedPath) `
        -AllowedPath '^tests/' | Out-Null
    $manifest = Get-Content -LiteralPath (Join-Path $repositoryRoot $manifestPath) -Raw | ConvertFrom-Json
    Assert-TestOnly (@($manifest.scope.plannedPaths) -contains $testPath) 'The allowed test delta was not planned automatically.'
    Assert-TestOnly (@($manifest.scope.plannedPaths) -notcontains $generatedPath) 'Derived Wiki output leaked into planned product scope.'

    Write-Host 'Test-only governance [2/4]: initialize acceptance.'
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

    Write-Host 'Test-only governance [3/4]: assess test-only policy.'
    $testOnlyPolicy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') `
        -ChangedPath @(
            'tests/FoodDiary.Application.Tests/Authentication/UserAgentParserTests.cs',
            '.llm-wiki/generated/quality-index.json',
            '.llm-wiki/reviews/source-impact-reviews.json'
        ) `
        -Format Json | ConvertFrom-Json
    $testOnlyRuleIds = @($testOnlyPolicy.matchedRules | ForEach-Object { [string]$_.id })
    Assert-TestOnly (@($testOnlyRuleIds | Where-Object { $_ -ne 'llm-wiki' }).Count -eq 0) 'Test-only authentication coverage triggered production change-policy rules.'
    Assert-TestOnly (@($testOnlyPolicy.reviewObligations).Count -eq 0) 'Test-only coverage triggered production review obligations.'

    Write-Host 'Test-only governance [4/4]: assess production control.'
    $productionPolicy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') `
        -ChangedPath 'FoodDiary.Application/Authentication/Commands/LinkGoogle/LinkGoogleCommandHandler.cs' `
        -Format Json | ConvertFrom-Json
    Assert-TestOnly (@($productionPolicy.matchedRules).Count -gt 0) 'Production authentication changes lost change-policy coverage.'
    Write-Host 'LLM Wiki test-only governance regression passed: routing scope, manifest, and acceptance mapping stay proportional.'
} finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
