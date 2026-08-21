[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ToolPath,
    [hashtable]$ToolArguments = @{}
)

$ErrorActionPreference = 'Stop'
$sourceRepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$activeSnapshotRoot = [string]$env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')

foreach ($variableName in @(
    'GIT_ALTERNATE_OBJECT_DIRECTORIES'
    'GIT_COMMON_DIR'
    'GIT_CONFIG'
    'GIT_CONFIG_COUNT'
    'GIT_CONFIG_PARAMETERS'
    'GIT_DIR'
    'GIT_GRAFT_FILE'
    'GIT_IMPLICIT_WORK_TREE'
    'GIT_INDEX_FILE'
    'GIT_INTERNAL_SUPER_PREFIX'
    'GIT_NO_REPLACE_OBJECTS'
    'GIT_OBJECT_DIRECTORY'
    'GIT_PREFIX'
    'GIT_REPLACE_REF_BASE'
    'GIT_SHALLOW_FILE'
    'GIT_WORK_TREE'
)) {
    Remove-Item -LiteralPath "Env:$variableName" -ErrorAction SilentlyContinue
}

function Get-RepositoryRelativePath {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$Path
    )

    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/')
    $absolute = [IO.Path]::GetFullPath($Path)
    $prefix = $root + [IO.Path]::DirectorySeparatorChar
    if (-not $absolute.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { return $null }
    return $absolute.Substring($prefix.Length).Replace('\', '/')
}

function Get-WorkspaceOverlayPaths {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [string[]]$RelevantPath
    )

    $normalizedRelevantPaths = @($RelevantPath | Where-Object { $_ } | ForEach-Object { ([string]$_).Replace('\', '/').TrimEnd('/') } | Sort-Object -Unique)
    $pathspecs = if ($normalizedRelevantPaths.Count -gt 0) {
        @($normalizedRelevantPaths + @('.llm-wiki') | Sort-Object -Unique)
    } else {
        @()
    }
    $trackedArguments = @('diff', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--') + @($pathspecs)
    $untrackedArguments = @('ls-files', '--others', '--exclude-standard', '--') + @($pathspecs)
    $tracked = @(Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments $trackedArguments -FailureMessage 'Unable to enumerate tracked workspace changes for the read-only snapshot.')
    $untracked = @(Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments $untrackedArguments -FailureMessage 'Unable to enumerate untracked workspace changes for the read-only snapshot.')
    return @($tracked + $untracked | Where-Object { $_ } | Sort-Object -Unique)
}

function Copy-WorkspaceOverlay {
    param(
        [Parameter(Mandatory)][string]$SourceRoot,
        [Parameter(Mandatory)][string]$SnapshotRoot,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$Path
    )

    foreach ($relativePath in @($Path | Where-Object { $_ })) {
        $sourcePath = Join-Path $SourceRoot $relativePath
        $snapshotPath = Join-Path $SnapshotRoot $relativePath
        if (Test-Path -LiteralPath $sourcePath -PathType Leaf) {
            $null = New-Item -ItemType Directory -Path (Split-Path -Parent $snapshotPath) -Force
            Copy-Item -LiteralPath $sourcePath -Destination $snapshotPath -Force
        } elseif (Test-Path -LiteralPath $snapshotPath -PathType Leaf) {
            Remove-Item -LiteralPath $snapshotPath -Force
        }
    }
}

function Select-RelevantOverlayPath {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$WorkspacePath,
        [Parameter(Mandatory)][hashtable]$Arguments
    )

    $scopePaths = if ($Arguments.ContainsKey('ProposedPath') -and @($Arguments['ProposedPath']).Count -gt 0) {
        @($Arguments['ProposedPath'])
    } elseif ($Arguments.ContainsKey('ChangedPath')) {
        @($Arguments['ChangedPath'])
    } else {
        @()
    }
    $normalizedScopes = @($scopePaths | Where-Object { $_ } | ForEach-Object { ([string]$_).Replace('\', '/').TrimEnd('/') } | Sort-Object -Unique)
    if ($normalizedScopes.Count -eq 0) { return @($WorkspacePath) }
    return @($WorkspacePath | Where-Object {
        $candidate = ([string]$_).Replace('\', '/').TrimEnd('/')
        if ($candidate.StartsWith('.llm-wiki/', [StringComparison]::OrdinalIgnoreCase)) { return $true }
        foreach ($scope in $normalizedScopes) {
            if ($candidate -eq $scope -or
                $candidate.StartsWith("$scope/", [StringComparison]::OrdinalIgnoreCase) -or
                $scope.StartsWith("$candidate/", [StringComparison]::OrdinalIgnoreCase)) { return $true }
        }
        return $false
    })
}

function Get-ReadOnlySnapshotFingerprint {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$OverlayPath
    )

    $head = (& git -C $RepositoryRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD for the isolated read-only snapshot.' }
    $material = [Collections.Generic.List[string]]::new()
    $material.Add('schema=4')
    $material.Add("head=$head")
    foreach ($relativePath in @($OverlayPath | Sort-Object -Unique)) {
        $material.Add("$relativePath=$(Get-FileHashOrMissing (Join-Path $RepositoryRoot $relativePath))")
    }
    foreach ($dependencyPath in @(
        '.artifacts/llm-wiki/code-graph/code-graph.fingerprint'
    )) {
        $material.Add("dependency:$dependencyPath=$(Get-FileHashOrMissing (Join-Path $RepositoryRoot $dependencyPath))")
    }
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        return (([BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($material -join "`n"))) -replace '-', '').ToLowerInvariant())
    } finally {
        $hasher.Dispose()
    }
}

function Get-FileHashOrMissing {
    param([Parameter(Mandatory)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '<missing>' }
    $stream = [IO.File]::OpenRead($Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose(); $stream.Dispose() }
}

function Remove-StaleReadOnlySnapshots {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$SnapshotParent,
        [Parameter(Mandatory)][string]$CurrentFingerprint,
        [ValidateRange(2, 50)][int]$Retain = 8
    )

    $parentPrefix = [IO.Path]::GetFullPath($SnapshotParent).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $readyFiles = @(Get-ChildItem -LiteralPath $SnapshotParent -Filter '*.ready' -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending)
    $retained = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $null = $retained.Add($CurrentFingerprint)
    foreach ($readyFile in $readyFiles | Select-Object -First $Retain) {
        $null = $retained.Add([IO.Path]::GetFileNameWithoutExtension($readyFile.Name))
    }
    foreach ($readyFile in $readyFiles) {
        $fingerprint = [IO.Path]::GetFileNameWithoutExtension($readyFile.Name)
        if ($fingerprint -notmatch '^[a-f0-9]{64}$' -or $retained.Contains($fingerprint)) { continue }
        $snapshotRoot = [IO.Path]::GetFullPath((Join-Path $SnapshotParent $fingerprint))
        if (-not $snapshotRoot.StartsWith($parentPrefix, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $lockPath = Join-Path $SnapshotParent "$fingerprint.lock"
        $pruneLock = $null
        try {
            try {
                $pruneLock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            } catch [IO.IOException] {
                continue
            }
            $snapshotGitPath = Join-Path $snapshotRoot '.git'
            if (Test-Path -LiteralPath $snapshotGitPath -PathType Container) {
                Remove-Item -LiteralPath $snapshotRoot -Recurse -Force
                $removeExitCode = 0
            } else {
                $previousErrorActionPreference = $ErrorActionPreference
                try {
                    $ErrorActionPreference = 'Continue'
                    $null = & git -C $RepositoryRoot worktree remove --force $snapshotRoot 2>&1
                    $removeExitCode = $LASTEXITCODE
                } finally {
                    $ErrorActionPreference = $previousErrorActionPreference
                }
            }
            if ($removeExitCode -eq 0) {
                Remove-Item -LiteralPath $readyFile.FullName -Force -ErrorAction SilentlyContinue
                break
            }
        } finally {
            if ($pruneLock) { $pruneLock.Dispose() }
            Remove-Item -LiteralPath $lockPath -Force -ErrorAction SilentlyContinue
        }
    }
}

function Get-GuardState {
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    $status = @(& git -C $RepositoryRoot status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to capture read-only snapshot state.' }
    $status = @($status | Sort-Object)
    $hashes = [ordered]@{}
    foreach ($relativePath in @(Get-WorkspaceOverlayPaths -RepositoryRoot $RepositoryRoot)) {
        $hashes[$relativePath] = Get-FileHashOrMissing (Join-Path $RepositoryRoot $relativePath)
    }
    $wikiRoot = Join-Path $RepositoryRoot '.llm-wiki'
    foreach ($protectedRootName in @('generated', 'knowledge', 'reviews')) {
        $protectedRoot = Join-Path $wikiRoot $protectedRootName
        foreach ($file in @(Get-ChildItem -LiteralPath $protectedRoot -File -Recurse -ErrorAction SilentlyContinue)) {
            $relativePath = Get-RepositoryRelativePath -RepositoryRoot $RepositoryRoot -Path $file.FullName
            if ($relativePath) { $hashes[$relativePath] = Get-FileHashOrMissing $file.FullName }
        }
    }
    return [pscustomobject]@{ status = $status; hashes = $hashes }
}

function Compare-GuardState {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][object]$Before,
        [Parameter(Mandatory)][object]$After
    )

    $mutated = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    if (($Before.status -join "`n") -cne ($After.status -join "`n")) {
        foreach ($relativePath in @(Get-WorkspaceOverlayPaths -RepositoryRoot $RepositoryRoot)) { $null = $mutated.Add($relativePath) }
    }
    $allHashPaths = @(@($Before.hashes.Keys) + @($After.hashes.Keys) | Sort-Object -Unique)
    foreach ($relativePath in $allHashPaths) {
        $beforeHash = if ($Before.hashes.Contains($relativePath)) { [string]$Before.hashes[$relativePath] } else { '<missing>' }
        $afterHash = if ($After.hashes.Contains($relativePath)) { [string]$After.hashes[$relativePath] } else { '<missing>' }
        if ($beforeHash -cne $afterHash) { $null = $mutated.Add([string]$relativePath) }
    }
    return @($mutated | Sort-Object)
}

function Invoke-ToolInsideSnapshot {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$SnapshotToolPath,
        [Parameter(Mandatory)][hashtable]$Arguments
    )

    $gitDirectory = (& git -C $RepositoryRoot rev-parse --absolute-git-dir).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve Git directory for the read-only Wiki guard.' }
    $lockDirectory = Join-Path $gitDirectory 'llm-wiki/index-transactions'
    $null = New-Item -ItemType Directory -Path $lockDirectory -Force
    $lockPath = Join-Path $lockDirectory 'update.lock'
    $readLock = $null
    try {
        try {
            $readLock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::ReadWrite)
        } catch {
            throw 'A Wiki index update is running. Wait for it to finish before starting read-only research.'
        }
        $before = Get-GuardState -RepositoryRoot $RepositoryRoot
        $toolFailure = $null
        Push-Location $RepositoryRoot
        try {
            & $SnapshotToolPath @Arguments
            if (-not $? -or ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0)) { throw "Read-only Wiki tool failed with exit code $LASTEXITCODE." }
        } catch {
            $toolFailure = $_
        } finally {
            Pop-Location
        }
        $after = Get-GuardState -RepositoryRoot $RepositoryRoot
        $mutated = @(Compare-GuardState -RepositoryRoot $RepositoryRoot -Before $before -After $after)
        if ($mutated.Count -gt 0) {
            $failureSuffix = if ($null -ne $toolFailure) { " Tool failure: $($toolFailure.Exception.Message)" } else { '' }
            throw "Read-only Wiki command modified its isolated snapshot: $($mutated -join ', '). No source files were restored or overwritten.$failureSuffix"
        }
        if ($null -ne $toolFailure) { throw $toolFailure }
    } finally {
        if ($readLock) { $readLock.Dispose() }
    }
}

if (-not [string]::IsNullOrWhiteSpace($activeSnapshotRoot)) {
    $repositoryRoot = [IO.Path]::GetFullPath($activeSnapshotRoot)
    $originalRoot = if (-not [string]::IsNullOrWhiteSpace($env:LLM_WIKI_READ_ONLY_SOURCE_ROOT)) {
        [IO.Path]::GetFullPath($env:LLM_WIKI_READ_ONLY_SOURCE_ROOT)
    } else {
        $sourceRepositoryRoot
    }
    $alreadyInsideSnapshot = Get-RepositoryRelativePath -RepositoryRoot $repositoryRoot -Path $ToolPath
    $relativeToolPath = Get-RepositoryRelativePath -RepositoryRoot $originalRoot -Path $ToolPath
    $snapshotToolPath = if ($alreadyInsideSnapshot) {
        $ToolPath
    } elseif ($relativeToolPath) {
        Join-Path $repositoryRoot $relativeToolPath
    } else {
        $ToolPath
    }
    Invoke-ToolInsideSnapshot -RepositoryRoot $repositoryRoot -SnapshotToolPath $snapshotToolPath -Arguments $ToolArguments
    return
}

$repositoryPathHasher = [Security.Cryptography.SHA256]::Create()
try {
    $repositoryPathHash = $repositoryPathHasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($sourceRepositoryRoot.ToLowerInvariant()))
} finally {
    $repositoryPathHasher.Dispose()
}
$repositorySnapshotKey = (([BitConverter]::ToString($repositoryPathHash) -replace '-', '').ToLowerInvariant()).Substring(0, 16)
$snapshotTempRoot = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
    Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'Temp'
} else {
    [IO.Path]::GetTempPath()
}
$snapshotParent = Join-Path $snapshotTempRoot "fooddiary-llm-wiki-read-only/$repositorySnapshotKey"
$relativeToolPath = Get-RepositoryRelativePath -RepositoryRoot $sourceRepositoryRoot -Path $ToolPath
if (-not $relativeToolPath) { throw "Read-only Wiki tool must be inside the repository: $ToolPath" }
$requestedScopePaths = if ($ToolArguments.ContainsKey('ProposedPath') -and @($ToolArguments['ProposedPath']).Count -gt 0) {
    @($ToolArguments['ProposedPath'])
} elseif ($ToolArguments.ContainsKey('ChangedPath')) {
    @($ToolArguments['ChangedPath'])
} else {
    @()
}
$workspaceOverlayPaths = @(Get-WorkspaceOverlayPaths -RepositoryRoot $sourceRepositoryRoot -RelevantPath $requestedScopePaths)
$overlayPaths = @(Select-RelevantOverlayPath -WorkspacePath $workspaceOverlayPaths -Arguments $ToolArguments)
$snapshotFingerprint = Get-ReadOnlySnapshotFingerprint -RepositoryRoot $sourceRepositoryRoot -OverlayPath $overlayPaths
$snapshotRoot = Join-Path $snapshotParent $snapshotFingerprint
$readyPath = Join-Path $snapshotParent "$snapshotFingerprint.ready"
$snapshotLockPath = Join-Path $snapshotParent "$snapshotFingerprint.lock"
$snapshotLock = $null
$removeSnapshot = $false
$snapshotCreated = $false
$requiredSnapshotFiles = @(
    '.llm-wiki/tools/Invoke-LlmWikiReadOnlyTool.ps1'
    '.llm-wiki/tools/LlmWikiGitPaths.ps1'
)
$previousSnapshotRoot = $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT
$previousSourceRoot = $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT
try {
    $null = New-Item -ItemType Directory -Path $snapshotParent -Force
    $lockDeadline = [DateTime]::UtcNow.AddMinutes(2)
    while ($null -eq $snapshotLock) {
        try {
            $snapshotLock = [IO.File]::Open($snapshotLockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        } catch [IO.IOException] {
            if ([DateTime]::UtcNow -ge $lockDeadline) { throw 'Timed out waiting for an identical read-only snapshot.' }
            Start-Sleep -Milliseconds 100
        }
    }
    $snapshotIsReady = (Test-Path -LiteralPath $readyPath -PathType Leaf) -and
        (Test-Path -LiteralPath (Join-Path $snapshotRoot '.git'))
    if ($snapshotIsReady) {
        foreach ($requiredPath in $requiredSnapshotFiles) {
            $sourceRequiredPath = Join-Path $sourceRepositoryRoot $requiredPath
            $snapshotRequiredPath = Join-Path $snapshotRoot $requiredPath
            if (-not (Test-Path -LiteralPath $snapshotRequiredPath -PathType Leaf) -or
                (Get-FileHashOrMissing $snapshotRequiredPath) -cne (Get-FileHashOrMissing $sourceRequiredPath)) {
                $snapshotIsReady = $false
                break
            }
        }
    }
    if (-not $snapshotIsReady) {
        $snapshotPrefix = [IO.Path]::GetFullPath($snapshotParent).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        $resolvedSnapshotRoot = [IO.Path]::GetFullPath($snapshotRoot)
        if (-not $resolvedSnapshotRoot.StartsWith($snapshotPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to prepare a read-only snapshot outside its cache root: $resolvedSnapshotRoot"
        }
        Remove-Item -LiteralPath $readyPath -Force -ErrorAction SilentlyContinue
        if (Test-Path -LiteralPath $snapshotRoot) { Remove-Item -LiteralPath $snapshotRoot -Recurse -Force }
        $head = (& git -C $sourceRepositoryRoot rev-parse HEAD).Trim()
        if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD for the isolated read-only snapshot clone.' }
        $previousErrorActionPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $cloneOutput = & git clone --shared --no-checkout --quiet $sourceRepositoryRoot $snapshotRoot 2>&1 | Out-String
            $cloneExitCode = $LASTEXITCODE
            if ($cloneExitCode -eq 0) {
                $checkoutOutput = & git -C $snapshotRoot checkout --detach --quiet $head 2>&1 | Out-String
                $checkoutExitCode = $LASTEXITCODE
            } else {
                $checkoutOutput = ''
                $checkoutExitCode = -1
            }
        } finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
        if ($cloneExitCode -ne 0 -or $checkoutExitCode -ne 0) {
            throw "Unable to create isolated read-only snapshot clone.`n$($cloneOutput.Trim())`n$($checkoutOutput.Trim())"
        }
        $snapshotCreated = $true
        Copy-WorkspaceOverlay -SourceRoot $sourceRepositoryRoot -SnapshotRoot $snapshotRoot -Path $overlayPaths
        Copy-WorkspaceOverlay -SourceRoot $sourceRepositoryRoot -SnapshotRoot $snapshotRoot -Path @(
            '.artifacts/llm-wiki/code-graph/code-graph.sqlite'
            '.artifacts/llm-wiki/code-graph/code-graph.sqlite-wal'
            '.artifacts/llm-wiki/code-graph/code-graph.fingerprint'
        )
        [IO.File]::WriteAllText($readyPath, $snapshotFingerprint + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    }
    $snapshotToolPath = Join-Path $snapshotRoot $relativeToolPath
    if (-not (Test-Path -LiteralPath $snapshotToolPath -PathType Leaf) -or
        (Get-FileHashOrMissing $snapshotToolPath) -cne (Get-FileHashOrMissing $ToolPath)) {
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $snapshotToolPath) -Force
        Copy-Item -LiteralPath $ToolPath -Destination $snapshotToolPath -Force
    }
    $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT = $snapshotRoot
    $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT = $sourceRepositoryRoot
    try {
        & (Join-Path $snapshotRoot '.llm-wiki/tools/Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath $snapshotToolPath -ToolArguments $ToolArguments
    } catch {
        if ($_.Exception.Message -like '*modified its isolated snapshot*') { $removeSnapshot = $true }
        throw
    }
} finally {
    if ($null -eq $previousSnapshotRoot) { Remove-Item Env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT -ErrorAction SilentlyContinue }
    else { $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT = $previousSnapshotRoot }
    if ($null -eq $previousSourceRoot) { Remove-Item Env:LLM_WIKI_READ_ONLY_SOURCE_ROOT -ErrorAction SilentlyContinue }
    else { $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT = $previousSourceRoot }
    if ($snapshotLock) { $snapshotLock.Dispose() }
    if ($removeSnapshot) {
        Remove-Item -LiteralPath $readyPath -Force -ErrorAction SilentlyContinue
        if (Test-Path -LiteralPath (Join-Path $snapshotRoot '.git') -PathType Container) {
            Remove-Item -LiteralPath $snapshotRoot -Recurse -Force -ErrorAction SilentlyContinue
        } else {
            & git -C $sourceRepositoryRoot worktree remove --force $snapshotRoot 2>$null
            if ($LASTEXITCODE -ne 0) { Write-Warning "Unable to remove isolated read-only snapshot: $snapshotRoot" }
        }
    }
    Remove-Item -LiteralPath $snapshotLockPath -Force -ErrorAction SilentlyContinue
    if ($snapshotCreated) {
        Remove-StaleReadOnlySnapshots -RepositoryRoot $sourceRepositoryRoot -SnapshotParent $snapshotParent -CurrentFingerprint $snapshotFingerprint
    }
}
