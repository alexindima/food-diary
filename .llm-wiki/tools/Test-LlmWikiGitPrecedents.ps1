[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'git-precedents' -IndependentRepository
try {
    $tools = Join-Path $fixture '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $tools -Force
    foreach ($name in @('Get-LlmWikiGitPrecedents.ps1', 'LlmWikiGitPaths.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $tools
    }
    & git -C $fixture config user.name 'Wiki Tests'
    & git -C $fixture config user.email 'wiki@example.invalid'
    & git -C $fixture config core.quotepath false
    [IO.File]::WriteAllText((Join-Path $fixture 'README.md'), 'baseline')
    & git -C $fixture add --all
    & git -C $fixture commit --quiet -m baseline
    $feature = Join-Path $fixture 'Alpha/Feature'
    $null = New-Item -ItemType Directory -Path $feature -Force
    foreach ($index in 0..13) {
        [IO.File]::WriteAllText((Join-Path $feature ('Evidence{0:d2}.cs' -f $index)), 'source')
    }
    & git -C $fixture add --all
    & git -C $fixture commit --quiet -m 'retry idempotency'
    if ($LASTEXITCODE -ne 0) { throw 'Unable to commit precedent fixture evidence.' }
    $tool = Join-Path $tools 'Get-LlmWikiGitPrecedents.ps1'
    $scoped = & $tool -Objective 'retry idempotency' -ScopePath @('alpha/feature/Handler.cs;Alpha/Feature', 'Alpha/Feature') -Limit 8 -Format Json | ConvertFrom-Json
    if ($scoped.searchedCommitCount -ne 2 -or @($scoped.precedents).Count -ne 1 -or
        $scoped.precedents[0].score -ne 23 -or
        ($scoped.precedents[0].matchedIntentTokens -join ',') -cne 'idempotency,retry' -or
        ($scoped.precedents[0].matchedPaths -join ',') -cne ((0..11 | ForEach-Object { 'Alpha/Feature/Evidence{0:d2}.cs' -f $_ }) -join ',')) {
        throw 'Precedent search changed whole-directory evidence, score caps, token order or the twelve-path output boundary.'
    }
    $rootFile = & $tool -Objective unmatched -ScopePath README.md -Format Json | ConvertFrom-Json
    if (@($rootFile.precedents).Count -ne 1 -or $rootFile.precedents[0].score -ne 3 -or
        ($rootFile.precedents[0].matchedPaths -join ',') -cne 'README.md') { throw 'Root file scope expanded beyond its exact path.' }
    $intentOnly = & $tool -Objective 'retry idempotency' -Format Json | ConvertFrom-Json
    if ($intentOnly.precedents[0].score -ne 8 -or @($intentOnly.precedents[0].matchedPaths).Count -ne 0) { throw 'Intent-only precedent search changed.' }
    [IO.File]::WriteAllText((Join-Path $feature 'Новая.cs'), 'new source')
    & git -C $fixture add --all
    & git -C $fixture commit --quiet -m 'fresh evidence'
    if ($LASTEXITCODE -ne 0) { throw 'Unable to advance precedent fixture history.' }
    $fresh = & $tool -Objective unmatched -ScopePath 'Alpha/Feature/Новая.cs' -Format Json | ConvertFrom-Json
    if ($fresh.searchedCommitCount -ne 3 -or
        @($fresh.precedents | Where-Object { $_.subject -eq 'fresh evidence' -and $_.matchedPaths -contains 'Alpha/Feature/Новая.cs' }).Count -ne 1) {
        throw 'Precedent search lost current Git history or Unicode path evidence.'
    }
    Write-Host 'Git precedent regression passed: scope boundaries, score/output caps, tokens, intent-only search, fresh commits and Unicode.'
} finally {
    $parent = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot -IndependentRepository)).TrimEnd('\', '/')
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture)) -cne $parent -or [IO.Path]::GetFileName($fixture) -notmatch '^git-precedents-[a-f0-9]{32}$') { throw 'Unowned precedent fixture cleanup.' }
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
