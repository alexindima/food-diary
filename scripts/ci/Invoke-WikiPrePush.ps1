[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$LocalObjectName,
    [Parameter(Mandatory)][string]$RemoteObjectName,
    [string]$RemoteName = 'origin',
    [string]$RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')),
    [ValidateRange(5, 180)][int]$TimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
. (Join-Path $RepositoryRoot '.llm-wiki/tools/LlmWikiGitPaths.ps1')
. (Join-Path $RepositoryRoot '.llm-wiki/tools/LlmWikiProcess.ps1')

function Invoke-PrePushGit([string[]]$GitArguments, [int[]]$AllowedExitCode = @(0)) {
    Invoke-LlmWikiGitCommand -RepositoryRoot $RepositoryRoot -Arguments $GitArguments -AllowedExitCode $AllowedExitCode
}

$head = (Invoke-PrePushGit @('rev-parse', '--verify', "$LocalObjectName`^{commit}")).Lines[0].Trim()
$knownRemote = $RemoteObjectName -notmatch '^0+$' -and
    (Invoke-PrePushGit @('cat-file', '-e', "$RemoteObjectName`^{commit}") @(0, 128)).ExitCode -eq 0
$revisionArguments = if ($knownRemote) { @("$RemoteObjectName..$head") } else { @($head, '--not', "--remotes=$RemoteName") }
$commits = @((Invoke-PrePushGit (@('rev-list', '--reverse', '--topo-order') + $revisionArguments)).Lines | Where-Object { $_ })
if ($commits.Count -eq 0) {
    Write-Host 'Wiki pre-push: no new commits to check.'
    return
}

$base = if ($knownRemote) { $RemoteObjectName } else {
    $ancestry = ((Invoke-PrePushGit @('rev-list', '--parents', '-n', '1', $commits[0])).Lines[0] -split ' ')
    if ($ancestry.Count -gt 1) { $ancestry[1] } else { $commits[0] }
}
[string[]]$paths = @(foreach ($commit in $commits) {
    Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments @(
        'diff-tree', '--root', '--no-commit-id', '--no-renames', '--name-only', '-r', '-m', $commit, '--'
    )
}) | Sort-Object -Unique
if ($paths.Count -eq 0) { return }

$ownedRoot = Join-Path $RepositoryRoot ('.artifacts/pre-push-wiki/' + [guid]::NewGuid().ToString('N'))
$snapshotRoot = Join-Path ([IO.Path]::GetTempPath()) ('fooddiary-wiki-pre-push/' + [guid]::NewGuid().ToString('N'))
$snapshot = Join-Path $snapshotRoot 'checkout'
$null = New-Item -ItemType Directory -Path $ownedRoot -Force
$null = New-Item -ItemType Directory -Path $snapshotRoot -Force
# Generators exclude absolute paths containing .artifacts, bin or obj. Keep
# the source checkout outside those output trees as well as the working tree.
$snapshotPrefix = [IO.Path]::GetFullPath($snapshotRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
if (-not [IO.Path]::GetFullPath($snapshot).StartsWith($snapshotPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Invalid owned Wiki pre-push checkout path.'
}
$created = $false
$timer = [Diagnostics.Stopwatch]::StartNew()
try {
    $null = Invoke-PrePushGit @('worktree', 'add', '--quiet', '--detach', $snapshot, $head)
    $created = $true
    $request = Join-Path $ownedRoot 'request.json'
    @{ snapshot = $snapshot; base = $base; changedPaths = @($paths) } | ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath $request -Encoding utf8
    $worker = Join-Path $ownedRoot 'verify.ps1'
    @'
param([string]$RequestPath)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$request = Get-Content -LiteralPath $RequestPath -Raw | ConvertFrom-Json
# Git hooks inherit repository environment; the verification belongs to the snapshot.
foreach ($name in @('GIT_DIR', 'GIT_COMMON_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_OBJECT_DIRECTORY',
    'GIT_ALTERNATE_OBJECT_DIRECTORIES', 'GIT_PREFIX', 'GIT_INTERNAL_SUPER_PREFIX', 'GIT_IMPLICIT_WORK_TREE',
    'GIT_SHALLOW_FILE', 'GIT_GRAFT_FILE', 'GIT_REPLACE_REF_BASE', 'GIT_NAMESPACE')) {
    Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue
}
Set-Location -LiteralPath $request.snapshot
$wikiPath = Join-Path $request.snapshot '.llm-wiki/wiki.ps1'
$global:LASTEXITCODE = 0
& $wikiPath lint
if (-not $? -or $LASTEXITCODE -ne 0) { exit 1 }
& $wikiPath impact -BaseRef $request.base -ChangedPath @($request.changedPaths) -FailOnUnreviewed
if (-not $? -or $LASTEXITCODE -ne 0) { exit 1 }
'@ | Set-Content -LiteralPath $worker -Encoding utf8
    Write-Host "Wiki pre-push: $($head.Substring(0, 10)), $($paths.Count) outgoing paths; static contracts and source reviews."
    Invoke-LlmWikiProcessBatch -WorkingDirectory $RepositoryRoot -MaxConcurrency 1 -TimeoutSeconds $TimeoutSeconds -Checks @(
        @{ name = 'committed Wiki pre-push'; script = $worker; arguments = ('-RequestPath "' + $request + '"') }
    )
    Write-Host "Wiki pre-push passed in $([Math]::Round($timer.Elapsed.TotalSeconds, 1))s. Index generation, retrieval and behavior tests remain in CI."
} finally {
    if ($created) {
        $null = Invoke-PrePushGit @('worktree', 'remove', '--force', $snapshot)
    }
    # Only remove the now-empty owned directory; preserve unexpected contents.
    if (@(Get-ChildItem -LiteralPath $snapshotRoot -Force).Count -eq 0) {
        Remove-Item -LiteralPath $snapshotRoot -Force
    }
}
