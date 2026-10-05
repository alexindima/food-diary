if (-not (Test-Path Function:\Invoke-LlmWikiGitPathList)) {
    . (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
}
if (-not (Test-Path Function:\Get-LlmWikiChangeSetSnapshot)) {
    . (Join-Path $PSScriptRoot 'LlmWikiChangeSetSnapshot.ps1')
}

function Get-LlmWikiSha256 {
    param([Parameter(Mandatory)][string]$Value)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Value))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Get-LlmWikiFileSha256 {
    param([Parameter(Mandatory)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '<missing>' }
    $stream = [IO.File]::OpenRead($Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose(); $stream.Dispose() }
}

function Get-LlmWikiQueryCacheEntry {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$Namespace,
        [Parameter(Mandatory)][hashtable]$Arguments,
        [string[]]$RelevantPath,
        [string[]]$DependencyPath,
        [pscustomobject]$VerifiedWorkspace
    )

    # Only reuse a status probe captured during this request. This is not a
    # cross-request freshness cache, and a global probe cannot stand in for a scope.
    if ($null -ne $VerifiedWorkspace) {
        if (@($RelevantPath | Where-Object { $_ }).Count -gt 0) {
            throw 'A verified global workspace cannot replace a scoped query-cache snapshot.'
        }
        foreach ($property in @('repositoryRoot', 'head', 'fingerprint', 'changedPathCount', 'fresh')) {
            if ($null -eq $VerifiedWorkspace.PSObject.Properties[$property]) {
                throw "Verified query-cache workspace is missing '$property'."
            }
        }
        $pathComparison = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
        $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        if (-not [IO.Path]::IsPathRooted([string]$VerifiedWorkspace.repositoryRoot) -or
            -not [string]::Equals($root, [IO.Path]::GetFullPath([string]$VerifiedWorkspace.repositoryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar), $pathComparison)) {
            throw 'Verified query-cache workspace belongs to a different repository.'
        }
        if ($VerifiedWorkspace.fresh -isnot [bool] -or -not $VerifiedWorkspace.fresh -or
            [string]$VerifiedWorkspace.head -notmatch '^[a-fA-F0-9]{40}$' -or
            [string]$VerifiedWorkspace.fingerprint -notmatch '^[a-fA-F0-9]{64}$' -or
            [string]$VerifiedWorkspace.changedPathCount -notmatch '^\d+$') {
            throw 'Verified query-cache workspace is stale or invalid.'
        }
        $snapshot = $VerifiedWorkspace
        $workspacePathCount = [int]$VerifiedWorkspace.changedPathCount
        $relevantPaths = @()
    } else {
        $snapshot = Get-LlmWikiChangeSetSnapshot -RepositoryRoot $RepositoryRoot -RelevantPath $RelevantPath
        $workspacePathCount = @($snapshot.changedPaths).Count
        $relevantPaths = @($snapshot.relevantPaths)
    }
    $head = [string]$snapshot.head
    $argumentJson = [ordered]@{}
    foreach ($key in @($Arguments.Keys | Sort-Object)) {
        $value = $Arguments[$key]
        $argumentJson[$key] = if ($value -is [Management.Automation.SwitchParameter]) { [bool]$value } else { $value }
    }
    $argumentMaterial = $argumentJson | ConvertTo-Json -Depth 8 -Compress
    $argumentFingerprint = Get-LlmWikiSha256 $argumentMaterial
    $dependencyMaterial = [Collections.Generic.List[string]]::new()
    foreach ($dependency in @($DependencyPath | Where-Object { $_ } | Sort-Object -Unique)) {
        $normalizedDependency = ([string]$dependency).Replace('\', '/')
        $dependencyMaterial.Add("$normalizedDependency=$(Get-LlmWikiFileSha256 (Join-Path $RepositoryRoot $normalizedDependency))")
    }
    $dependencyFingerprint = Get-LlmWikiSha256 $(if ($dependencyMaterial.Count -gt 0) { $dependencyMaterial -join "`n" } else { '<none>' })
    $material = [Collections.Generic.List[string]]::new()
    $material.Add('schema=2')
    $material.Add("namespace=$Namespace")
    $material.Add("head=$head")
    $material.Add("changeSet=$($snapshot.fingerprint)")
    $material.Add("dependencies=$dependencyFingerprint")
    $material.Add("pwsh=$($PSVersionTable.PSVersion)")
    $material.Add($argumentMaterial)
    $fingerprint = Get-LlmWikiSha256 ($material -join "`n")
    $gitDirectory = (Invoke-LlmWikiGitCommand -RepositoryRoot $RepositoryRoot -Arguments @('rev-parse', '--absolute-git-dir') -FailureMessage 'Unable to resolve Git directory for the Wiki query cache.').Lines[0].Trim()
    $cacheDirectory = Join-Path $gitDirectory "llm-wiki/query-cache/$Namespace"
    $metadataPath = Join-Path $cacheDirectory "latest-$argumentFingerprint.meta"
    $missReason = 'cold cache; no prior entry for these arguments'
    if (Test-Path -LiteralPath $metadataPath -PathType Leaf) {
        try {
            $previous = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
            $missReason = if ([string]$previous.head -cne $head) {
                'Git HEAD changed'
            } elseif ([string]$previous.changeSetFingerprint -cne [string]$snapshot.fingerprint) {
                'relevant workspace paths changed'
            } elseif ([string]$previous.dependencyFingerprint -cne $dependencyFingerprint) {
                'dependent Wiki indexes changed'
            } else {
                'matching cache result is missing or expired'
            }
        } catch {
            $missReason = 'cache diagnostic metadata is unreadable'
        }
    }
    return [pscustomobject]@{
        fingerprint = $fingerprint
        path = Join-Path $cacheDirectory "$fingerprint.json"
        metadataPath = $metadataPath
        head = $head
        changeSetFingerprint = [string]$snapshot.fingerprint
        dependencyFingerprint = $dependencyFingerprint
        argumentFingerprint = $argumentFingerprint
        missReason = $missReason
        workspacePathCount = $workspacePathCount
        relevantPaths = $relevantPaths
    }
}

function Read-LlmWikiQueryCache {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Entry)
    if (-not (Test-Path -LiteralPath $Entry.path -PathType Leaf)) { return $null }
    try {
        $content = [IO.File]::ReadAllText($Entry.path, [Text.Encoding]::UTF8)
        $null = $content | ConvertFrom-Json -ErrorAction Stop
        return $content
    } catch {
        Remove-Item -LiteralPath $Entry.path -Force -ErrorAction SilentlyContinue
        return $null
    }
}

function Move-LlmWikiQueryCacheFile {
    param(
        [Parameter(Mandatory)][string]$TemporaryPath,
        [Parameter(Mandatory)][string]$DestinationPath
    )
    $backupPath = "$DestinationPath.$([guid]::NewGuid().ToString('N')).bak"
    if ([IO.File]::Exists($DestinationPath)) {
        try { [IO.File]::Replace($TemporaryPath, $DestinationPath, $backupPath) }
        finally { Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue }
        return
    }
    try {
        [IO.File]::Move($TemporaryPath, $DestinationPath)
    } catch [IO.IOException] {
        if (-not [IO.File]::Exists($TemporaryPath) -or -not [IO.File]::Exists($DestinationPath)) { throw }
        try { [IO.File]::Replace($TemporaryPath, $DestinationPath, $backupPath) }
        finally { Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue }
    }
}

function Remove-LlmWikiQueryCacheFileIfPresent {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    try {
        [IO.File]::Delete($Path)
    } catch [IO.FileNotFoundException] {
        return
    } catch [IO.DirectoryNotFoundException] {
        return
    }
}

function Write-LlmWikiQueryCache {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Entry,
        [Parameter(Mandatory)][string]$Content,
        [ValidateRange(5, 500)][int]$Retain = 100
    )
    $directory = Split-Path -Parent $Entry.path
    $null = [IO.Directory]::CreateDirectory($directory)
    $temporaryPath = "$($Entry.path).$([guid]::NewGuid().ToString('N')).tmp"
    $metadataTemporaryPath = "$($Entry.metadataPath).$([guid]::NewGuid().ToString('N')).tmp"
    try {
        [IO.File]::WriteAllText($temporaryPath, $Content, [Text.UTF8Encoding]::new($false))
        Move-LlmWikiQueryCacheFile -TemporaryPath $temporaryPath -DestinationPath $Entry.path
        $metadata = [ordered]@{
            schemaVersion = 1
            head = [string]$Entry.head
            changeSetFingerprint = [string]$Entry.changeSetFingerprint
            dependencyFingerprint = [string]$Entry.dependencyFingerprint
            argumentFingerprint = [string]$Entry.argumentFingerprint
            recordedAtUtc = [DateTime]::UtcNow.ToString('o')
        } | ConvertTo-Json -Compress
        [IO.File]::WriteAllText($metadataTemporaryPath, $metadata + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
        Move-LlmWikiQueryCacheFile -TemporaryPath $metadataTemporaryPath -DestinationPath $Entry.metadataPath
    } finally {
        Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $metadataTemporaryPath -Force -ErrorAction SilentlyContinue
    }
    $staleEntries = @(Get-ChildItem -LiteralPath $directory -Filter '*.json' -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -Skip $Retain)
    foreach ($staleEntry in $staleEntries) {
        Remove-LlmWikiQueryCacheFileIfPresent -Path $staleEntry.FullName
    }
}
