[CmdletBinding()]
param(
    [ValidateRange(0, 50)]
    [int]$Retain = 8
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$repositoryPathHasher = [Security.Cryptography.SHA256]::Create()
try {
    $repositoryPathHash = $repositoryPathHasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($repositoryRoot.ToLowerInvariant()))
} finally {
    $repositoryPathHasher.Dispose()
}
$repositorySnapshotKey = (([BitConverter]::ToString($repositoryPathHash) -replace '-', '').ToLowerInvariant()).Substring(0, 16)
$snapshotTempRoot = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
    Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'Temp'
} else {
    [IO.Path]::GetTempPath()
}
$cacheBase = [IO.Path]::GetFullPath((Join-Path $snapshotTempRoot 'fooddiary-llm-wiki-read-only'))
$snapshotParent = [IO.Path]::GetFullPath((Join-Path $cacheBase $repositorySnapshotKey))
$cachePrefix = $cacheBase.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
if (-not $snapshotParent.StartsWith($cachePrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean a read-only snapshot cache outside the expected temp root: $snapshotParent"
}
if (-not (Test-Path -LiteralPath $snapshotParent -PathType Container)) {
    Write-Host 'LLM Wiki read-only snapshot cache is empty.'
    exit 0
}

$readyFiles = @(Get-ChildItem -LiteralPath $snapshotParent -Filter '*.ready' -File |
    Sort-Object LastWriteTimeUtc -Descending)
$removed = 0
$busy = 0
$failed = [Collections.Generic.List[string]]::new()
foreach ($readyFile in @($readyFiles | Select-Object -Skip $Retain)) {
    $fingerprint = [IO.Path]::GetFileNameWithoutExtension($readyFile.Name)
    if ($fingerprint -notmatch '^[a-f0-9]{64}$') { continue }
    $snapshotRoot = [IO.Path]::GetFullPath((Join-Path $snapshotParent $fingerprint))
    $snapshotPrefix = $snapshotParent.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $snapshotRoot.StartsWith($snapshotPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean a read-only snapshot outside its repository cache: $snapshotRoot"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $snapshotRoot '.git') -PathType Leaf)) {
        if (Test-Path -LiteralPath $snapshotRoot -PathType Container) {
            Remove-Item -LiteralPath $snapshotRoot -Recurse -Force
        }
        Remove-Item -LiteralPath $readyFile.FullName -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath (Join-Path $snapshotParent "$fingerprint.lock") -Force -ErrorAction SilentlyContinue
        $removed++
        continue
    }
    $lockPath = Join-Path $snapshotParent "$fingerprint.lock"
    $lock = $null
    try {
        try {
            $lock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        } catch [IO.IOException] {
            $busy++
            continue
        }
        $previousErrorActionPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $output = & git -C $repositoryRoot worktree remove --force $snapshotRoot 2>&1 | Out-String
            $exitCode = $LASTEXITCODE
        } finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
        if ($exitCode -eq 0) {
            Remove-Item -LiteralPath $readyFile.FullName -Force -ErrorAction SilentlyContinue
            $removed++
        } else {
            $failed.Add("$fingerprint ($($output.Trim()))")
        }
    } finally {
        if ($lock) { $lock.Dispose() }
        Remove-Item -LiteralPath $lockPath -Force -ErrorAction SilentlyContinue
    }
}

& git -C $repositoryRoot worktree prune
if ($LASTEXITCODE -ne 0) { throw 'Git failed to prune stale read-only worktree registrations.' }
Write-Host "LLM Wiki read-only snapshot cache cleanup: removed=$removed, retained=$([Math]::Min($Retain, $readyFiles.Count)), busy=$busy, failed=$($failed.Count)."
if ($failed.Count -gt 0) { Write-Warning ($failed -join [Environment]::NewLine) }
