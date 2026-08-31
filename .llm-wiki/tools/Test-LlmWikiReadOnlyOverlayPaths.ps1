[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$guardAst = [Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $PSScriptRoot 'Invoke-LlmWikiReadOnlyTool.ps1'), [ref]$null, [ref]$null)
$overlayFunction = $guardAst.Find({
    param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-WorkspaceOverlayPaths'
}, $false)
. ([scriptblock]::Create($overlayFunction.Extent.Text))
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'read-only-overlay-paths'
try {
    $unicodeName = -join @([char]0x0444, [char]0x0430, [char]0x0439, [char]0x043B)
    $unicodePath = "folder with spaces/$unicodeName.txt"
    foreach ($directory in @('alpha', 'folder with spaces', '.llm-wiki')) {
        $null = New-Item -ItemType Directory -Path (Join-Path $fixture $directory) -Force
    }
    foreach ($path in @('alpha/changed.txt', 'alpha/deleted.txt', $unicodePath, 'outside.txt', '.llm-wiki/note.txt')) {
        [IO.File]::WriteAllText((Join-Path $fixture $path), 'baseline', [Text.UTF8Encoding]::new($false))
    }
    $null = Invoke-LlmWikiGitCommand -RepositoryRoot $fixture -Arguments @('init', '--quiet')
    $null = Invoke-LlmWikiGitCommand -RepositoryRoot $fixture -Arguments @('add', '--all')
    $null = Invoke-LlmWikiGitCommand -RepositoryRoot $fixture -Arguments @('-c', 'user.name=Wiki Smoke', '-c', 'user.email=wiki-smoke@example.invalid', 'commit', '--quiet', '-m', 'fixture')
    foreach ($path in @('alpha/changed.txt', $unicodePath, 'outside.txt', '.llm-wiki/note.txt', 'alpha/untracked.txt', 'outside-untracked.txt')) {
        [IO.File]::WriteAllText((Join-Path $fixture $path), 'changed', [Text.UTF8Encoding]::new($false))
    }
    Remove-Item -LiteralPath (Join-Path $fixture 'alpha/deleted.txt') -Force
    $smallScopes = @('alpha', 'folder with spaces', '.llm-wiki')
    $expectedScoped = @(
        @(Invoke-LlmWikiGitPathList -RepositoryRoot $fixture -Arguments (@('diff', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--') + $smallScopes)) +
        @(Invoke-LlmWikiGitPathList -RepositoryRoot $fixture -Arguments (@('ls-files', '--others', '--exclude-standard', '--') + $smallScopes)) |
            Sort-Object -Unique
    )
    $largeScopes = @('alpha', 'alpha/changed.txt', 'alpha', 'folder with spaces') + @(1..1000 | ForEach-Object {
        "absent-scope-$($_.ToString('0000'))/" + ('x' * 70)
    })
    if (($largeScopes -join ' ').Length -le 32767) { throw 'Large-scope regression must exceed the Windows argv limit.' }
    $actualScoped = @(Get-WorkspaceOverlayPaths -RepositoryRoot $fixture -RelevantPath $largeScopes)
    if (($actualScoped -join "`n") -cne ($expectedScoped -join "`n")) { throw 'Batched overlay differs from the complete unbatched scoped result.' }
    [array]::Reverse($largeScopes)
    $reversedScoped = @(Get-WorkspaceOverlayPaths -RepositoryRoot $fixture -RelevantPath $largeScopes)
    if (($actualScoped -join "`n") -cne ($reversedScoped -join "`n")) { throw 'Batched overlay is not stable across duplicate/reordered scopes.' }
    if ($unicodePath -notin $actualScoped -or 'alpha/deleted.txt' -notin $actualScoped -or 'alpha/untracked.txt' -notin $actualScoped) {
        throw 'Batched overlay lost Unicode, deleted, or untracked paths.'
    }
    $expectedAll = @(
        @(Invoke-LlmWikiGitPathList -RepositoryRoot $fixture -Arguments @('diff', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--')) +
        @(Invoke-LlmWikiGitPathList -RepositoryRoot $fixture -Arguments @('ls-files', '--others', '--exclude-standard', '--')) |
            Sort-Object -Unique
    )
    $actualAll = @(Get-WorkspaceOverlayPaths -RepositoryRoot $fixture -RelevantPath @())
    if (($actualAll -join "`n") -cne ($expectedAll -join "`n")) { throw 'Empty overlay scope must enumerate the complete workspace.' }
    $invalidRepository = Join-Path $fixture 'not-a-repository'
    $null = New-Item -ItemType Directory -Path $invalidRepository -Force
    [IO.File]::WriteAllText((Join-Path $invalidRepository '.git'), 'gitdir: missing', [Text.Encoding]::ASCII)
    $gitFailure = $false
    try { $null = Get-WorkspaceOverlayPaths -RepositoryRoot $invalidRepository -RelevantPath $largeScopes } catch {
        $gitFailure = $_.Exception.Message -like '*Unable to enumerate tracked workspace changes*'
    }
    if (-not $gitFailure) { throw 'Batched overlay swallowed a Git enumeration failure.' }
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if (-not $resolvedFixture.StartsWith([IO.Path]::GetFullPath($repositoryRoot) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the regression repository.'
    }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host 'LLM Wiki overlay batching regression passed: large argv, exact scoped results, stable deduplication, Unicode/spaces, empty scope, and Git failures.'
