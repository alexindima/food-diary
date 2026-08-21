[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

$unicodeName = -join @([char]0x0444, [char]0x0430, [char]0x0439, [char]0x043B)
$unicodePath = "folder with spaces/$unicodeName.cs"
$sample = "$([char]0xFEFF).llm-wiki/generated/index.json`0$unicodePath`0another/path.ts`0"
$parsed = @(ConvertFrom-LlmWikiGitPathOutput $sample)
foreach ($expected in @('.llm-wiki/generated/index.json', $unicodePath, 'another/path.ts')) {
    if ($expected -notin $parsed) { throw "Git path parser lost or corrupted '$expected'." }
}
if (@($parsed | Where-Object { $_.Length -gt 0 -and [int]$_[0] -eq 0xFEFF }).Count -gt 0) { throw 'Git path parser retained a BOM prefix.' }

$tracked = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--', '.llm-wiki/wiki.ps1'))
if ('.llm-wiki/wiki.ps1' -notin $tracked) { throw 'NUL-safe Git path enumeration did not return the known Wiki facade.' }
$failed = $false
try { $null = Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('not-a-real-command') -FailureMessage 'Expected failure.' } catch { $failed = $_.Exception.Message -match 'Expected failure.*Exit code' }
if (-not $failed) { throw 'Git path enumeration did not propagate a required Git failure.' }
$noMatches = @(
    Invoke-LlmWikiGitPathList `
        -RepositoryRoot $repositoryRoot `
        -Arguments @('grep', '-l', '--fixed-strings', 'llm-wiki-regression-value-that-does-not-exist', '--', '*.cs') `
        -AllowedExitCode @(0, 1) `
        -FailureMessage 'Expected an empty Git search result.'
)
if ($noMatches.Count -ne 0) { throw 'Git path enumeration returned paths for an empty search result.' }
$largeAlternatives = @(1..1000 | ForEach-Object { "ChangedType$($_.ToString('0000'))" })
$boundedPatterns = @(Split-LlmWikiGitGrepAlternatives -Alternative $largeAlternatives -MaxPatternLength 512)
if ($boundedPatterns.Count -le 1 -or @($boundedPatterns | Where-Object Length -gt 512).Count -gt 0) {
    throw 'Git grep alternative batching did not respect the command-line pattern bound.'
}
if ((($boundedPatterns -join '|').Split('|') -join '|') -cne ($largeAlternatives -join '|')) {
    throw 'Git grep alternative batching lost or reordered search terms.'
}
$gitPathHelperText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1') -Raw
if (($gitPathHelperText | Select-String -Pattern 'StandardOutput\.ReadToEndAsync\(\)' -AllMatches).Matches.Count -ne 1 -or
    ($gitPathHelperText | Select-String -Pattern 'StandardError\.ReadToEndAsync\(\)' -AllMatches).Matches.Count -ne 1) {
    throw 'Git path enumeration must drain stdout and stderr concurrently.'
}
$warningRepository = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'git-stderr-pressure'
try {
    & git -C $warningRepository init --quiet
    foreach ($index in 1..250) {
        [IO.File]::WriteAllText((Join-Path $warningRepository "warning-$index.txt"), "line $index`n", [Text.UTF8Encoding]::new($false))
    }
    & git -C $warningRepository -c core.autocrlf=false add . 2>$null
    & git -C $warningRepository -c user.name='LLM Wiki' -c user.email='llm-wiki@example.invalid' commit --quiet -m baseline
    foreach ($index in 1..250) {
        [IO.File]::AppendAllText((Join-Path $warningRepository "warning-$index.txt"), "changed`n", [Text.UTF8Encoding]::new($false))
    }
    $warningPaths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $warningRepository -Arguments @('diff', '--name-only', 'HEAD', '--') -FailureMessage 'Concurrent Git stream regression failed.')
    if ($warningPaths.Count -ne 250) { throw "Concurrent Git stream regression returned $($warningPaths.Count)/250 paths." }
} finally {
    Remove-Item -LiteralPath $warningRepository -Recurse -Force -ErrorAction SilentlyContinue
}
$headSha = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ((Resolve-LlmWikiCommitRef -RepositoryRoot $repositoryRoot -Ref HEAD) -cne $headSha) { throw 'Git ref canonicalization did not resolve HEAD to its immutable SHA.' }
$aliasPolicy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') -BaseRef HEAD -ChangedPath 'FoodDiary.Presentation.Api/Controllers/CyclesController.cs' -Format Json | ConvertFrom-Json
$shaPolicy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') -BaseRef $headSha -ChangedPath 'FoodDiary.Presentation.Api/Controllers/CyclesController.cs' -Format Json | ConvertFrom-Json
if ([string]$aliasPolicy.baseRef -cne $headSha -or
    (@($aliasPolicy.requiredChecks.command) -join '|') -cne (@($shaPolicy.requiredChecks.command) -join '|')) {
    throw 'Equivalent HEAD and SHA refs produced different canonical policy definitions.'
}

$overlayRepository = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'headref-overlay'
$overlayFixtureName = 'wiki-headref-overlay.md'
$overlayFixturePath = Join-Path $overlayRepository $overlayFixtureName
try {
    & git -C $overlayRepository init --quiet
    [IO.File]::WriteAllText((Join-Path $overlayRepository 'tracked.md'), "baseline`n", [Text.UTF8Encoding]::new($false))
    & git -C $overlayRepository add tracked.md
    & git -C $overlayRepository -c user.name='LLM Wiki' -c user.email='llm-wiki@example.invalid' commit --quiet -m baseline
    [IO.File]::WriteAllText($overlayFixturePath, "workspace overlay`n", [Text.UTF8Encoding]::new($false))
    $overlayPaths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $overlayRepository -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'Unable to enumerate the isolated workspace overlay.')
    if ($overlayFixtureName -notin $overlayPaths) { throw '-HeadRef HEAD overlay enumeration omitted an untracked working-tree path.' }
    if (-not (Test-LlmWikiWorkspaceHeadRef HEAD)) { throw 'HEAD was not recognized as a workspace-overlay head reference.' }
} finally {
    Remove-Item -LiteralPath $overlayRepository -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki Git path regression passed: BOM, spaces, Unicode, NUL parsing, bounded grep patterns, immutable refs, workspace HEAD overlays, and exit codes are safe.'
