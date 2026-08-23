if (-not (Get-Command Invoke-LlmWikiGitPathList -ErrorAction SilentlyContinue)) {
    . (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
}
if (-not (Get-Command Get-LlmWikiChangeSetSnapshot -ErrorAction SilentlyContinue)) {
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
        [string[]]$DependencyPath
    )

    $snapshot = Get-LlmWikiChangeSetSnapshot -RepositoryRoot $RepositoryRoot -RelevantPath $RelevantPath
    $head = [string]$snapshot.head
    $workspacePaths = [string[]]@($snapshot.changedPaths)
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
        workspacePathCount = $workspacePaths.Count
        relevantPaths = @($snapshot.relevantPaths)
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
        Remove-Item -LiteralPath $staleEntry.FullName -Force -ErrorAction SilentlyContinue
    }
}
