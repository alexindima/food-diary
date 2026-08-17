[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
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
$headSha = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ((Resolve-LlmWikiCommitRef -RepositoryRoot $repositoryRoot -Ref HEAD) -cne $headSha) { throw 'Git ref canonicalization did not resolve HEAD to its immutable SHA.' }
$aliasPolicy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') -BaseRef HEAD -ChangedPath 'FoodDiary.Presentation.Api/Controllers/CyclesController.cs' -Format Json | ConvertFrom-Json
$shaPolicy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') -BaseRef $headSha -ChangedPath 'FoodDiary.Presentation.Api/Controllers/CyclesController.cs' -Format Json | ConvertFrom-Json
if ([string]$aliasPolicy.baseRef -cne $headSha -or
    (@($aliasPolicy.requiredChecks.command) -join '|') -cne (@($shaPolicy.requiredChecks.command) -join '|')) {
    throw 'Equivalent HEAD and SHA refs produced different canonical policy definitions.'
}

$overlayFixtureName = "wiki-headref-overlay-$([guid]::NewGuid().ToString('N')).md"
$overlayFixturePath = Join-Path $repositoryRoot $overlayFixtureName
try {
    [IO.File]::WriteAllText($overlayFixturePath, "workspace overlay`n", [Text.UTF8Encoding]::new($false))
    $overlayDiff = & (Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1') `
        -BaseRef HEAD `
        -HeadRef HEAD `
        -Format Json | ConvertFrom-Json
    if ($overlayFixtureName -notin @($overlayDiff.changedPaths)) { throw '-HeadRef HEAD omitted an untracked working-tree path.' }
    $overlayPolicy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') `
        -BaseRef HEAD `
        -HeadRef HEAD `
        -Format Json | ConvertFrom-Json
    if ($overlayFixtureName -notin @($overlayPolicy.changedPaths)) { throw 'Change policy omitted the working tree for -HeadRef HEAD.' }
} finally {
    if (Test-Path -LiteralPath $overlayFixturePath) { Remove-Item -LiteralPath $overlayFixturePath -Force }
}

Write-Host 'LLM Wiki Git path regression passed: BOM, spaces, Unicode, NUL parsing, immutable refs, workspace HEAD overlays, and exit codes are safe.'
