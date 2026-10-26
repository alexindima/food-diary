[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
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
$standardOutputDrainCount = ($gitPathHelperText | Select-String -Pattern 'StandardOutput\.ReadToEndAsync\(\)' -AllMatches).Matches.Count
$standardErrorDrainCount = ($gitPathHelperText | Select-String -Pattern 'StandardError\.ReadToEndAsync\(\)' -AllMatches).Matches.Count
if ($standardOutputDrainCount -eq 0 -or $standardOutputDrainCount -ne $standardErrorDrainCount) {
    throw 'Git path enumeration must drain stdout and stderr concurrently.'
}
if ($gitPathHelperText -notmatch 'function Invoke-LlmWikiGitCommand\b') {
    throw 'Git path helpers no longer expose a general-purpose safe git invocation for non-path-list output (diff --name-status, log, rev-parse, ...); native `&' + " git`` calls elsewhere silently regain the Set-StrictMode NativeCommandError risk on any stderr warning (e.g. core.autocrlf CRLF notices)."
}
$warningRepository = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'git-stderr-pressure' -IndependentRepository
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

$overlayRepository = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'headref-overlay' -IndependentRepository
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
    $null = New-Item -ItemType Directory -Path (Join-Path $overlayRepository 'folder with spaces') -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $overlayRepository 'node_modules') -Force
    [IO.File]::WriteAllText((Join-Path $overlayRepository $unicodePath), 'public class NewSource {}')
    [IO.File]::WriteAllText((Join-Path $overlayRepository '.gitignore'), "node_modules/`n")
    [IO.File]::WriteAllText((Join-Path $overlayRepository 'node_modules/ignored.cs'), 'ignored')
    Remove-Item -LiteralPath (Join-Path $overlayRepository 'tracked.md')
    $inventory = @(Get-LlmWikiSourceInventory -RepositoryRoot $overlayRepository)
    if ($unicodePath -notin $inventory -or $overlayFixtureName -notin $inventory -or
        'tracked.md' -in $inventory -or 'node_modules/ignored.cs' -in $inventory) {
        throw 'Source inventory lost an untracked Unicode source or included ignored/deleted files.'
    }
    $linkedRoot = Join-Path $overlayRepository 'linked-checkout'
    & git -C $overlayRepository worktree add --quiet --detach $linkedRoot HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Unable to prepare the linked-worktree regression.' }
    [IO.File]::WriteAllText((Join-Path $linkedRoot 'linked-only.txt'), 'linked')
    $inherited = @{}
    try {
        foreach ($name in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_COMMON_DIR', 'GIT_INDEX_FILE')) {
            $inherited[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
            $value = switch ($name) {
                'GIT_WORK_TREE' { $overlayRepository }
                'GIT_INDEX_FILE' { Join-Path $overlayRepository '.git/index' }
                default { Join-Path $overlayRepository '.git' }
            }
            [Environment]::SetEnvironmentVariable($name, $value, 'Process')
        }
        $actualRoot = Invoke-LlmWikiGitCommand -RepositoryRoot $linkedRoot -Arguments @('rev-parse', '--show-toplevel')
        if ([IO.Path]::GetFullPath($actualRoot.StandardOutput.Trim()) -ne [IO.Path]::GetFullPath($linkedRoot)) {
            throw 'Inherited Git environment redirected the linked checkout.'
        }
        $linkedInventory = @(Get-LlmWikiSourceInventory -RepositoryRoot $linkedRoot)
        if ('linked-only.txt' -notin $linkedInventory -or $overlayFixtureName -in $linkedInventory) {
            throw 'Linked-worktree inventory included another checkout or lost its own untracked source.'
        }
        if ([Environment]::GetEnvironmentVariable('GIT_WORK_TREE', 'Process') -ne $overlayRepository) {
            throw 'Git helper modified the calling process environment.'
        }
    } finally {
        foreach ($name in $inherited.Keys) {
            if ($null -eq $inherited[$name]) { Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue }
            else { [Environment]::SetEnvironmentVariable($name, $inherited[$name], 'Process') }
        }
    }
    & git -C $overlayRepository worktree remove --force $linkedRoot
    if ($LASTEXITCODE -ne 0) { throw 'Unable to remove the owned linked-worktree fixture.' }
} finally {
    Remove-Item -LiteralPath $overlayRepository -Recurse -Force -ErrorAction SilentlyContinue
}

$hookPath = Join-Path $repositoryRoot 'FoodDiary.Web.Client/.husky/pre-push'
$hookText = [IO.File]::ReadAllText($hookPath)
$shell = Get-Command sh -ErrorAction SilentlyContinue
$shellPath = if ($null -ne $shell) { $shell.Source } elseif ($env:ProgramFiles -and (Test-Path -LiteralPath (Join-Path $env:ProgramFiles 'Git/bin/sh.exe'))) { Join-Path $env:ProgramFiles 'Git/bin/sh.exe' } else { throw 'Git hook scope regression requires sh.' }
$hookFixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'pre-push-scope' -IndependentRepository
try {
    $probe = Join-Path $hookFixture 'pre-push.sh'
    [IO.File]::WriteAllText($probe, $hookText.Replace("`r`n", "`n"), [Text.UTF8Encoding]::new($false))
    $null = Invoke-LlmWikiGitCommand $hookFixture @('init', '--quiet')
    foreach ($path in @('deleted.cs', 'moved.cs', $unicodePath)) {
        $absolute = Join-Path $hookFixture $path
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $absolute) -Force
        [IO.File]::WriteAllText($absolute, "$path`n")
    }
    # Existing remote history may contain whitespace errors. A new branch must
    # check its own outgoing commits rather than block on that old baseline.
    [IO.File]::WriteAllText((Join-Path $hookFixture 'legacy.txt'), "legacy `n")
    $null = Invoke-LlmWikiGitCommand $hookFixture @('add', '--', 'deleted.cs', 'moved.cs', $unicodePath, 'legacy.txt')
    $null = Invoke-LlmWikiGitCommand $hookFixture @('-c', 'user.name=Wiki fixture', '-c', 'user.email=wiki@example.invalid', 'commit', '--quiet', '-m', 'base')
    $base = (Invoke-LlmWikiGitCommand $hookFixture @('rev-parse', 'HEAD')).Lines[0].Trim()
    $null = Invoke-LlmWikiGitCommand $hookFixture @('update-ref', 'refs/remotes/origin/master', $base)
    Remove-Item -LiteralPath (Join-Path $hookFixture 'deleted.cs')
    $null = Invoke-LlmWikiGitCommand $hookFixture @('add', '-u')
    $null = Invoke-LlmWikiGitCommand $hookFixture @('-c', 'user.name=Wiki fixture', '-c', 'user.email=wiki@example.invalid', 'commit', '--quiet', '-m', 'delete')
    $deleted = (Invoke-LlmWikiGitCommand $hookFixture @('rev-parse', 'HEAD')).Lines[0].Trim()
    $null = Invoke-LlmWikiGitCommand $hookFixture @('mv', '--', 'moved.cs', 'renamed.cs')
    [IO.File]::AppendAllText((Join-Path $hookFixture $unicodePath), "changed`n")
    $null = Invoke-LlmWikiGitCommand $hookFixture @('add', '-u')
    $null = Invoke-LlmWikiGitCommand $hookFixture @('-c', 'user.name=Wiki fixture', '-c', 'user.email=wiki@example.invalid', 'commit', '--quiet', '-m', 'move')
    $moved = (Invoke-LlmWikiGitCommand $hookFixture @('rev-parse', 'HEAD')).Lines[0].Trim()
    [IO.File]::AppendAllText((Join-Path $hookFixture $unicodePath), "bad whitespace `n")
    $null = Invoke-LlmWikiGitCommand $hookFixture @('add', '-u')
    $null = Invoke-LlmWikiGitCommand $hookFixture @('-c', 'user.name=Wiki fixture', '-c', 'user.email=wiki@example.invalid', 'commit', '--quiet', '-m', 'bad whitespace')
    $bad = (Invoke-LlmWikiGitCommand $hookFixture @('rev-parse', 'HEAD')).Lines[0].Trim()
    $badTree = (Invoke-LlmWikiGitCommand $hookFixture @('rev-parse', "$bad`^{tree}")).Lines[0].Trim()
    $merge = (Invoke-LlmWikiGitCommand $hookFixture @('-c', 'user.name=Wiki fixture', '-c', 'user.email=wiki@example.invalid', 'commit-tree', $badTree, '-p', $moved, '-p', $bad, '-m', 'merge')).Lines[0].Trim()
    [IO.File]::WriteAllText((Join-Path $hookFixture $unicodePath), "local whitespace error `n")
    $zero = '0' * 40
    $missing = '1' * 40
    foreach ($case in @(
        @{ name = 'delete file'; input = "refs/heads/a $deleted refs/heads/a $base`n"; pass = $true }
        @{ name = 'rename and Unicode, ignoring dirty worktree'; input = "refs/heads/a $moved refs/heads/a $deleted`n"; pass = $true }
        @{ name = 'multiple clean refs'; input = "refs/heads/a $deleted refs/heads/a $base`nrefs/heads/b $moved refs/heads/b $deleted`n"; pass = $true }
        @{ name = 'new branch excludes known history'; input = "refs/heads/a $moved refs/heads/a $zero`n"; pass = $true }
        @{ name = 'missing remote object'; input = "refs/heads/a $moved refs/heads/a $missing`n"; pass = $true }
        @{ name = 'pure deletion'; input = "(delete) $zero refs/heads/a $bad`n"; pass = $true }
        @{ name = 'forced rollback'; input = "refs/heads/a $moved refs/heads/a $bad`n"; pass = $true }
        @{ name = 'outgoing whitespace'; input = "refs/heads/a $bad refs/heads/a $moved`n"; pass = $false }
        @{ name = 'new branch with whitespace'; input = "refs/heads/a $bad refs/heads/a $zero`n"; pass = $false }
        @{ name = 'second ref fails'; input = "refs/heads/a $moved refs/heads/a $deleted`nrefs/heads/b $bad refs/heads/b $moved`n"; pass = $false }
        @{ name = 'deletion does not hide updated ref'; input = "(delete) $zero refs/heads/a $base`nrefs/heads/b $bad refs/heads/b $moved`n"; pass = $false }
        @{ name = 'merge whitespace'; input = "refs/heads/a $merge refs/heads/a $bad`n"; pass = $false }
        @{ name = 'root commit whitespace'; input = "refs/heads/a $base refs/heads/a $zero`n"; pass = $false; remote = 'untracked-remote' }
        @{ name = 'invalid outgoing object'; input = "refs/heads/a $missing refs/heads/a $base`n"; pass = $false }
        @{ name = 'manual invocation checks tracked changes'; input = ''; pass = $false }
    )) {
        $stdinPath = Join-Path $hookFixture 'input.txt'
        $stdoutPath = Join-Path $hookFixture 'stdout.txt'
        $stderrPath = Join-Path $hookFixture 'stderr.txt'
        [IO.File]::WriteAllText($stdinPath, $case.input, [Text.UTF8Encoding]::new($false))
        $window = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) { @{ WindowStyle = 'Hidden' } } else { @{} }
        $remote = if ($case.ContainsKey('remote')) { $case.remote } else { 'origin' }
        $child = Start-Process @window -FilePath $shellPath -ArgumentList @('-e', ('"' + $probe + '"'), $remote) -WorkingDirectory $hookFixture -RedirectStandardInput $stdinPath -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath -PassThru
        try {
            # Windows PowerShell 5 needs the process handle cached before exit
            # to retain the native exit code on this Start-Process object.
            $null = $child.Handle
            if (-not $child.WaitForExit(30000)) { Stop-LlmWikiProcessTree $child; throw 'Pre-push scope fixture timed out.' }
            # Unix Start-Process forwards output through asynchronous handlers.
            # The timed wait does not drain them before the files are inspected.
            $child.WaitForExit()
            if (($child.ExitCode -eq 0) -ne $case.pass) { throw "Pre-push case '$($case.name)' failed: exit=$($child.ExitCode); expected pass=$($case.pass); $([IO.File]::ReadAllText($stdoutPath)); $([IO.File]::ReadAllText($stderrPath))" }
        } finally { $child.Dispose() }
    }
} finally {
    $fixturePrefix = [IO.Path]::GetFullPath((Split-Path -Parent $hookFixture)).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not [IO.Path]::GetFullPath($hookFixture).StartsWith($fixturePrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe pre-push fixture cleanup path.' }
    Remove-Item -LiteralPath $hookFixture -Recurse -Force
}
Write-Host 'LLM Wiki Git path regression passed: BOM, spaces, Unicode, NUL parsing, batching, refs, worktrees, exit codes and 15 real pre-push whitespace scenarios.'
