[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'contract-reference'
$fixtureRootRelative = $fixtureRoot.Substring($repositoryRoot.Length + 1).Replace('\', '/')
$definitionRelative = "$fixtureRootRelative/Definition.cs"
$consumerRelative = "$fixtureRootRelative/Consumer.cs"
$definitionPath = Join-Path $repositoryRoot $definitionRelative
$consumerPath = Join-Path $repositoryRoot $consumerRelative
try {
    $null = New-Item -ItemType Directory -Path $fixtureRoot -Force
    [IO.File]::WriteAllText($definitionPath, 'public sealed class Foo { }', [Text.UTF8Encoding]::new($false))
    $content = @'
Foo FooBar Foo1 _Foo XFoo "Foo" // Foo
IThing IThing2 (IThing)
ÅContract XÅContract éÅContract
'@
    [IO.File]::WriteAllText($consumerPath, $content, [Text.UTF8Encoding]::new($false))
    $names = @('Foo', 'FooBar', 'IThing', 'ÅContract')

    $actualResult = @(& (Join-Path $PSScriptRoot 'Invoke-LlmWikiContractReferenceExtractor.ps1') `
        -RepositoryRoot $repositoryRoot `
        -Path @($definitionRelative, $consumerRelative) `
        -Name $names)
    $actualConsumer = @($actualResult | Where-Object path -eq $consumerRelative)[0]
    $actualCounts = @{}
    foreach ($reference in @($actualConsumer.references)) { $actualCounts[[string]$reference.name] = [int]$reference.count }

    $alternation = @($names | Sort-Object Length -Descending | ForEach-Object { [regex]::Escape($_) }) -join '|'
    $expectedCounts = @{}
    foreach ($group in @([regex]::Matches($content, "(?<![A-Za-z0-9_])(?<name>$alternation)(?![A-Za-z0-9_])") | Group-Object { $_.Groups['name'].Value })) {
        $expectedCounts[$group.Name] = $group.Count
    }
    foreach ($name in $names) {
        if ([int]$actualCounts[$name] -ne [int]$expectedCounts[$name]) {
            throw "Compiled reference count for '$name' differs from the compatibility regex: actual=$($actualCounts[$name]), expected=$($expectedCounts[$name])."
        }
    }

    $contracts = @(
        [pscustomobject][ordered]@{ name = 'Foo'; roles = @('Request'); kinds = @('class'); areas = @('FoodDiary'); definitionPaths = @($definitionRelative); ambiguous = $false }
        [pscustomobject][ordered]@{ name = 'FooBar'; roles = @('Response'); kinds = @('class'); areas = @('FoodDiary'); definitionPaths = @('Contracts/FooBar.cs'); ambiguous = $false }
        [pscustomobject][ordered]@{ name = 'IThing'; roles = @('Other'); kinds = @('interface'); areas = @('FoodDiary'); definitionPaths = @('Contracts/IThing.cs'); ambiguous = $false }
        [pscustomobject][ordered]@{ name = 'ÅContract'; roles = @('Command'); kinds = @('class'); areas = @('FoodDiary'); definitionPaths = @('Contracts/Unicode.cs'); ambiguous = $false }
    )
    $compiledIndex = & (Join-Path $PSScriptRoot 'Invoke-LlmWikiContractReferenceExtractor.ps1') `
        -RepositoryRoot $repositoryRoot `
        -Path @($definitionRelative, $consumerRelative) `
        -Contract $contracts `
        -BuildBackendIndex
    $index = $compiledIndex.indexJson | ConvertFrom-Json
    if ($index.summary.contracts -ne 4 -or $index.summary.consumerEdges -ne 4) {
        throw "Compiled backend fixture summary is incorrect: contracts=$($index.summary.contracts), edges=$($index.summary.consumerEdges)."
    }
    if (@($index.consumerEdges | Where-Object consumerPath -eq $definitionRelative).Count -ne 0) {
        throw 'Compiled backend index retained a reference from the contract definition file.'
    }

    & (Join-Path $PSScriptRoot 'Build-LlmWikiBackendContractIndex.ps1') -Check -RequireCompiledScanner
    if (-not $?) { throw 'Repository backend-contract index did not match the compiled scanner output.' }
    Write-Host 'Contract-reference extractor regression passed: compatibility counts, canonical index, definition exclusion, and repository equivalence are exact.'
} finally {
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
}
