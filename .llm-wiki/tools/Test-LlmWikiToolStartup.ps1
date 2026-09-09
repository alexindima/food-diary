[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'tool-startup'
$previousSnapshot = $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT
$previousSource = $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT
$previousGitCount = $env:GIT_CONFIG_COUNT
try {
    $fixtureWiki = Join-Path $fixtureRoot '.llm-wiki'
    $fixtureTools = Join-Path $fixtureWiki 'tools'
    $null = New-Item -ItemType Directory -Path $fixtureTools -Force
    Copy-Item (Join-Path $PSScriptRoot '../wiki.ps1') (Join-Path $fixtureWiki 'wiki.ps1')
    foreach ($helper in @('LlmWikiGitPaths.ps1', 'LlmWikiJson.ps1', 'LlmWikiProcess.ps1')) {
        Copy-Item (Join-Path $PSScriptRoot $helper) (Join-Path $fixtureTools $helper)
    }
    [IO.File]::WriteAllText((Join-Path $fixtureTools 'Get-LlmWikiTaskBrief.ps1'), @'
param($BaseRef,$Format,$Limit,$ChangedPath,$CompiledIndexSource)
@{source=$CompiledIndexSource} | ConvertTo-Json -Compress
'@)
    [IO.File]::WriteAllText((Join-Path $fixtureTools 'Invoke-LlmWikiReadOnlyTool.ps1'), @'
param($ToolPath,$ToolArguments,[switch]$PrepareCodeGraph)
& $ToolPath @ToolArguments
'@)
    $facade = Join-Path $fixtureWiki 'wiki.ps1'
    $dependencyRoot = Join-Path $fixtureRoot 'compiler-source'
    $packageDirectory = Join-Path $dependencyRoot 'FoodDiary.Web.Client/node_modules/typescript'
    $null = New-Item -ItemType Directory -Path $packageDirectory -Force
    [IO.File]::WriteAllText((Join-Path $packageDirectory 'package.json'), '{}')
    $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT = $dependencyRoot
    $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT = $fixtureRoot
    $snapshot = & $facade brief -ChangedPath 'example.cs' -Format Json | ConvertFrom-Json
    if ($snapshot.source -ne 'Sqlite') { throw 'Active snapshot failed to use source compiler dependencies.' }
    $explicitJson = & $facade brief -ChangedPath 'example.cs' -CompiledIndexSource Json -Format Json | ConvertFrom-Json
    if ($explicitJson.source -ne 'Json') { throw 'Snapshot overrode the explicit JSON route.' }
    $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT = Join-Path $fixtureRoot 'another-snapshot'
    $cold = & $facade brief -ChangedPath 'example.cs' -Format Json -WarningAction SilentlyContinue | ConvertFrom-Json
    if ($cold.source -ne 'Json') { throw 'Cold checkout inherited dependencies from an unrelated snapshot.' }
    $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT = $fixtureRoot
    $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT = Join-Path $fixtureRoot 'missing-dependencies'
    $missing = & $facade brief -ChangedPath 'example.cs' -Format Json -WarningAction SilentlyContinue | ConvertFrom-Json
    if ($missing.source -ne 'Json') { throw 'Snapshot without compiler dependencies lost the JSON fallback.' }
    $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT = $previousSnapshot
    $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT = $previousSource

    # Exercise the real planner while rejecting the accidental unscoped walk.
    & {
        function Get-ChildItem {
            [CmdletBinding()]
            param([string[]]$LiteralPath, [string[]]$Path, [string]$Filter,
                [switch]$Recurse, [switch]$File, [switch]$Directory, [switch]$Force)
            if ($Recurse -and @(@($LiteralPath) + @($Path) | Where-Object { $_ -eq $repositoryRoot }).Count -gt 0) {
                throw 'An empty proposed path triggered a recursive repository scan.'
            }
            Microsoft.PowerShell.Management\Get-ChildItem @PSBoundParameters
        }
        $diff = & (Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1') -ChangedPath '.llm-wiki/index.md' -CompiledIndexSource Json -Format Json | ConvertFrom-Json
        $planner = Join-Path $PSScriptRoot 'Get-LlmWikiTestPlan.ps1'
        $baseline = & $planner -DiffInput $diff -Format Json
        foreach ($paths in @(@(''), @('   '), @())) {
            $actual = & $planner -DiffInput $diff -ProposedPath $paths -Format Json
            if (($actual -join "`n") -cne ($baseline -join "`n")) { throw 'Empty proposed paths changed the test plan.' }
        }
    }
    & {
        . (Join-Path $PSScriptRoot 'LlmWikiVerificationReceipts.ps1')
        function Get-LlmWikiVerificationReceiptRoot { param($RepositoryRoot) Join-Path $fixtureRoot 'empty-receipts' }
        function Get-LlmWikiVerificationFingerprint { param($RepositoryRoot) throw 'Empty receipts unnecessarily hashed repository inputs.' }
        if (@(Get-LlmWikiVerificationReceipts $repositoryRoot).Count) { throw 'Missing receipt directory returned results.' }
        $null = New-Item -ItemType Directory (Join-Path $fixtureRoot 'empty-receipts')
        if (@(Get-LlmWikiVerificationReceipts $repositoryRoot).Count) { throw 'Empty receipt directory returned results.' }
    }
    Write-Host 'Wiki startup contracts passed: snapshot routing, empty proposed paths, and empty receipts.'
} finally {
    $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT = $previousSnapshot
    $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT = $previousSource
    for ($index = [int]$previousGitCount; $index -lt [int]$env:GIT_CONFIG_COUNT; $index++) {
        Remove-Item "Env:GIT_CONFIG_KEY_$index", "Env:GIT_CONFIG_VALUE_$index" -ErrorAction SilentlyContinue
    }
    $env:GIT_CONFIG_COUNT = $previousGitCount
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    $artifactPrefix = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts')).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedFixture.StartsWith($artifactPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe startup fixture path.' }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
