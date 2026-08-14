[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ToolPath,
    [hashtable]$ToolArguments = @{}
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$wikiRoot = Join-Path $repositoryRoot '.llm-wiki'
$protectedRoots = @('generated', 'knowledge', 'reviews') | ForEach-Object { Join-Path $wikiRoot $_ }
$gitDirectory = (& git -C $repositoryRoot rev-parse --absolute-git-dir).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve Git directory for the read-only Wiki guard.' }
$lockDirectory = Join-Path $gitDirectory 'llm-wiki/index-transactions'
$null = New-Item -ItemType Directory -Path $lockDirectory -Force
$lockPath = Join-Path $lockDirectory 'update.lock'
$readLock = $null

function Get-ProtectedFiles {
    @($protectedRoots | ForEach-Object {
        if (Test-Path -LiteralPath $_ -PathType Container) { Get-ChildItem -LiteralPath $_ -File -Recurse }
    })
}

function Get-ExistingWorktreePaths {
    $paths = @(& git -C $repositoryRoot status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to snapshot the existing worktree for the read-only Wiki guard.' }
    @($paths | ForEach-Object {
        $path = ([string]$_).Substring(3).Trim()
        if ($path -match ' -> ') { $path = ($path -split ' -> ', 2)[1] }
        $path.Trim('"').Replace('/', '\')
    } | Where-Object { $_ })
}

function Get-BytesHash([byte[]]$Bytes) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Bytes)) -replace '-', '') }
    finally { $sha.Dispose() }
}

try {
    try {
        # Read-only Wiki calls can be nested (for example full-tools -> task contract
        # validation -> diff context). Share both read and write access between readers;
        # an updater still requests FileShare.None and therefore remains mutually exclusive.
        $readLock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::ReadWrite)
    } catch {
        throw 'A Wiki index update is running. Wait for it to finish before starting read-only research.'
    }
    $before = @{}
    foreach ($file in @(Get-ProtectedFiles)) {
        $relative = $file.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
        $bytes = [IO.File]::ReadAllBytes($file.FullName)
        $before[$relative] = [pscustomobject]@{ bytes = $bytes; hash = Get-BytesHash $bytes }
    }
    foreach ($relativePath in @(Get-ExistingWorktreePaths)) {
        $absolute = Join-Path $repositoryRoot $relativePath
        if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { continue }
        $relative = $absolute.Substring($repositoryRoot.Length + 1).Replace('\', '/')
        if ($before.ContainsKey($relative)) { continue }
        $bytes = [IO.File]::ReadAllBytes($absolute)
        $before[$relative] = [pscustomobject]@{ bytes = $bytes; hash = Get-BytesHash $bytes }
    }

    $toolFailure = $null
    try {
        & $ToolPath @ToolArguments
        if (-not $? -or ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0)) { throw "Read-only Wiki tool failed with exit code $LASTEXITCODE." }
    } catch { $toolFailure = $_ }

    $afterPaths = @{}
    foreach ($file in @(Get-ProtectedFiles)) { $afterPaths[$file.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')] = $file.FullName }
    $mutated = [Collections.Generic.List[string]]::new()
    foreach ($entry in $before.GetEnumerator()) {
        $absolute = Join-Path $repositoryRoot $entry.Key
        $same = (Test-Path -LiteralPath $absolute -PathType Leaf) -and (Get-BytesHash ([IO.File]::ReadAllBytes($absolute))) -ceq [string]$entry.Value.hash
        if ($same) { continue }
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $absolute) -Force
        [IO.File]::WriteAllBytes($absolute, [byte[]]$entry.Value.bytes)
        $mutated.Add($entry.Key)
    }
    foreach ($entry in $afterPaths.GetEnumerator()) {
        if ($before.ContainsKey($entry.Key)) { continue }
        Remove-Item -LiteralPath $entry.Value -Force
        $mutated.Add($entry.Key)
    }
    if ($null -ne $toolFailure) { throw $toolFailure }
    if ($mutated.Count -gt 0) {
        throw "Read-only Wiki command attempted to modify protected files; the original bytes were restored: $($mutated -join ', ')"
    }
} finally {
    if ($readLock) { $readLock.Dispose() }
}
