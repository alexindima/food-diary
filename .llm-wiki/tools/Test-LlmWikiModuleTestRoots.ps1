[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiModuleTestRoots.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'module-test-roots'
try {
    if (@(Get-LlmWikiModuleTestRoots -RepositoryRoot $fixture).Count) { throw 'Empty repository has no module test roots.' }
    foreach ($path in @('Modules/Alpha/tests/Alpha.Tests','Modules/Beta/tests/Beta.Tests','Modules/Gamma/Application/tests','Modules/Delta/Domain','ModulesExtra/Wrong/tests','tests')) {
        New-Item -ItemType Directory -Path (Join-Path $fixture $path) -Force | Out-Null
    }
    $actual = @(Get-LlmWikiModuleTestRoots -RepositoryRoot $fixture)
    if (($actual -join '|') -cne 'Modules/Alpha/tests|Modules/Beta/tests') {
        throw "Module test ownership must use an exact module/tests boundary: $($actual -join ', ')"
    }
    $current = @(Get-LlmWikiModuleTestRoots -RepositoryRoot $repositoryRoot)
    foreach ($expected in @('Modules/Billing/tests','Modules/Fasting/tests')) {
        if ($expected -notin $current) { throw "Missing real module test root: $expected" }
    }
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    $allowedRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts')).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedFixture.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture cleanup target.' }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
Write-Host 'Module test roots regression passed: module-owned tests, exact boundaries, empty repository, and legacy roots remain separate.'
