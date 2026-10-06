[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$indexPath = Join-Path $wikiRoot 'generated/architecture-health-index.json'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$cacheFixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'architecture-cache-drift'
$fixtureTools = Join-Path $cacheFixture '.llm-wiki/tools'
try {
    $null = New-Item -ItemType Directory -Path $fixtureTools -Force
    $toolNames = @('Build-LlmWikiArchitectureHealthIndex.ps1','LlmWikiJson.ps1','LlmWikiGitPaths.ps1','LlmWikiIndexCache.ps1')
    foreach ($name in $toolNames) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $fixtureTools $name) }
    & git -C $cacheFixture init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize architecture-cache fixture.' }
    $fixtureOutput = Join-Path $cacheFixture '.llm-wiki/generated/architecture-health-index.json'
    $fixtureReceipt = Join-Path $cacheFixture '.artifacts/llm-wiki/index-cache/architecture-health-index.json'
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $fixtureOutput) -Force
    $fingerprint = Get-LlmWikiIndexInputFingerprint $cacheFixture @($toolNames | ForEach-Object { ".llm-wiki/tools/$_" })
    $shell = (Get-Process -Id $PID).Path
    foreach ($scenario in @('dependencyViolations','untrackedProductionProjects','moduleCycleNodes','clean')) {
        $summary = [ordered]@{dependencyViolations=0;untrackedProductionProjects=0;moduleCycleNodes=0}
        if ($scenario -ne 'clean') { $summary[$scenario] = 1 }
        [IO.File]::WriteAllText($fixtureOutput, ([ordered]@{schemaVersion=1;summary=$summary} | ConvertTo-Json -Depth 4))
        Write-LlmWikiIndexCache $fixtureReceipt $fixtureOutput $fingerprint
        $messages = & $shell -NoLogo -NoProfile -File (Join-Path $fixtureTools 'Build-LlmWikiArchitectureHealthIndex.ps1') -Check -ReuseUnchangedCheck
        $expectedExit = if ($scenario -eq 'clean') { 0 } else { 1 }
        if ($LASTEXITCODE -ne $expectedExit) { throw "Cached architecture gate returned the wrong result for $scenario`: $($messages -join ' ')" }
    }
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($cacheFixture)
    $sandbox = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot $repositoryRoot)).TrimEnd('\','/')
    if ([IO.Path]::GetDirectoryName($resolvedFixture) -cne $sandbox -or
        [IO.Path]::GetFileName($resolvedFixture) -notmatch '^architecture-cache-drift-[a-f0-9]{32}$') { throw 'Unsafe architecture-cache fixture cleanup.' }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}

$index = Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json
$catalog = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/repository-catalog.json') -Raw | ConvertFrom-Json
$symbols = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/csharp-symbol-index.json') -Raw | ConvertFrom-Json
$backendContracts = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/backend-contract-index.json') -Raw | ConvertFrom-Json
$consumerNames = @($backendContracts.consumerEdges.contract)
$expectedUnconsumed = @($backendContracts.contracts | Where-Object { $_.name -notin $consumerNames })
if (($index.unconsumedBackendContracts | ConvertTo-Json -Depth 10 -Compress) -cne ($expectedUnconsumed | ConvertTo-Json -Depth 10 -Compress) -or
    [int]$index.summary.unconsumedBackendContracts -ne $expectedUnconsumed.Count) {
    throw 'Architecture contract lookup changed membership, order or summary counts.'
}
$catalogToolProjects = @(@($catalog.dotnet.projects) | Where-Object { [string]$_.path -match '^\.llm-wiki/tools/' })
$toolSymbols = @(@($symbols.symbols) | Where-Object { [string]$_.path -match '^\.llm-wiki/tools/' })
$toolContractDefinitions = @(@($backendContracts.contracts) | Where-Object { @($_.definitionPaths | Where-Object { [string]$_ -match '^\.llm-wiki/tools/' }).Count -gt 0 })
$toolContractConsumers = @(@($backendContracts.consumerEdges) | Where-Object { [string]$_.consumerPath -match '^\.llm-wiki/tools/' })
if ($catalogToolProjects.Count -ne 0 -or $toolSymbols.Count -ne 0 -or $toolContractDefinitions.Count -ne 0 -or $toolContractConsumers.Count -ne 0) {
    throw 'Internal Wiki tool sources must not make production catalog, symbol, or contract indexes platform-dependent.'
}
$toolProjects = @(
    @($index.untrackedProductionProjects) |
        Where-Object { [string]$_.path -match '^\.llm-wiki/tools/' }
)

if ($toolProjects.Count -ne 0) {
    throw "Internal Wiki tool projects must not be treated as production projects: $($toolProjects.path -join ', ')"
}

$supportPath = 'Tooling/FoodDiary.Testing/FoodDiary.Testing.csproj'
$supportProjects = @($catalog.dotnet.projects | Where-Object { [string]$_.path -eq $supportPath })
if ($supportProjects.Count -ne 1 -or [bool]$supportProjects[0].isTestProject -or $supportPath -in @($catalog.dotnet.testProjects)) {
    throw 'FoodDiary.Testing must remain a cataloged support library, not a runnable test project.'
}
if (@($index.untrackedProductionProjects | Where-Object { [string]$_.path -eq $supportPath }).Count -ne 0 -or
    @($index.projectDependencyViolations | Where-Object { [string]$_.sourcePath -eq $supportPath }).Count -ne 0) {
    throw 'Test-support dependencies must not be compared against the production dependency matrix.'
}

Write-Host 'Architecture health internal-tool exclusion regression passed.'
