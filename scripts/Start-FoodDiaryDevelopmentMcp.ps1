[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$BuildMode = '--build-if-stale',
    [Parameter(Position = 1)]
    [string]$RepositoryRoot,
    [switch]$PrepareOnly,
    [switch]$CleanupSession,
    [string]$SessionDirectory
)

$ErrorActionPreference = 'Stop'
$systemTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
$sessionRoot = [IO.Path]::GetFullPath((Join-Path $systemTemp 'fooddiary-development-mcp'))
if ($CleanupSession) {
    $cleanupTarget = [IO.Path]::GetFullPath($SessionDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ([IO.Path]::GetDirectoryName($cleanupTarget) -ne $sessionRoot -or
        [IO.Path]::GetFileName($cleanupTarget) -notmatch '^(?:[0-9a-fA-F]{32}|[0-9a-f]{64})$') {
        throw "Unsafe Development MCP cleanup target: $cleanupTarget"
    }
    if (Test-Path -LiteralPath $cleanupTarget -PathType Container) {
        $cleanupLock = [IO.File]::Open(
            (Join-Path $cleanupTarget '.session.lock'),
            [IO.FileMode]::OpenOrCreate,
            [IO.FileAccess]::ReadWrite,
            [IO.FileShare]::None)
        $cleanupLock.Dispose()
        Remove-Item -LiteralPath $cleanupTarget -Recurse -Force
    }
    exit 0
}
if ([string]::IsNullOrWhiteSpace($BuildMode)) { $BuildMode = '--build-if-stale' }
if ($BuildMode -notin @('--no-build', '--build-if-missing', '--build-if-stale')) {
    [Console]::Error.WriteLine("Unsupported build mode '$BuildMode'.")
    exit 2
}
$resolvedRoot = if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
} else {
    [IO.Path]::GetFullPath($RepositoryRoot)
}
$wikiPath = Join-Path $resolvedRoot '.llm-wiki/wiki.ps1'
if (-not (Test-Path -LiteralPath $wikiPath -PathType Leaf)) {
    [Console]::Error.WriteLine("FoodDiary repository root is invalid: $resolvedRoot")
    exit 2
}

$projectRoot = Join-Path $resolvedRoot 'FoodDiary.Development.Mcp'
$projectPath = Join-Path $projectRoot 'FoodDiary.Development.Mcp.csproj'
$sourceDirectory = Join-Path $projectRoot 'bin/Debug/net10.0'
$sourceAssembly = Join-Path $sourceDirectory 'FoodDiary.Development.Mcp.dll'
$buildIdentityPath = Join-Path $sourceDirectory 'fooddiary-development-mcp-build.json'
$fingerprintTool = Join-Path $projectRoot 'Infrastructure/Get-DevelopmentMcpSourceFingerprint.ps1'

function Get-SourceFingerprint {
    & $fingerprintTool -RepositoryRoot $resolvedRoot -Format Text
}
function Get-BuildIdentity {
    if (-not (Test-Path -LiteralPath $buildIdentityPath -PathType Leaf)) { return $null }
    try { return (Get-Content -LiteralPath $buildIdentityPath -Raw | ConvertFrom-Json) } catch { return $null }
}
function Get-Sha256Hash {
    param([string]$Path)

    $stream = [IO.File]::OpenRead($Path)
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($hasher.ComputeHash($stream)) -replace '-', '').ToLowerInvariant()
    } finally {
        $hasher.Dispose()
        $stream.Dispose()
    }
}
function Remove-StaleSessionDirectories {
    param([string]$SessionRoot)

    if (-not (Test-Path -LiteralPath $SessionRoot -PathType Container)) { return }

    $staleCutoff = [DateTime]::UtcNow.AddMinutes(-10)
    foreach ($directory in @(Get-ChildItem -LiteralPath $SessionRoot -Directory -Force)) {
        $knownSessionName = $directory.Name -match '^(?:[0-9a-fA-F]{32}|[0-9a-f]{64})$' -or
            $directory.Name -match '^\d+-\d+-\d+$'
        if (-not $knownSessionName -or $directory.LastWriteTimeUtc -ge $staleCutoff) {
            continue
        }

        $sessionLockPath = Join-Path $directory.FullName '.session.lock'
        $staleSessionLock = $null
        try {
            $staleSessionLock = [IO.File]::Open(
                $sessionLockPath,
                [IO.FileMode]::OpenOrCreate,
                [IO.FileAccess]::ReadWrite,
                [IO.FileShare]::None)
            $staleSessionLock.Dispose()
            $staleSessionLock = $null
            [IO.Directory]::Delete($directory.FullName, $true)
        } catch [IO.IOException] {
            continue
        } catch [UnauthorizedAccessException] {
            continue
        } finally {
            if ($null -ne $staleSessionLock) { $staleSessionLock.Dispose() }
        }
    }
}

$rootSha = [Security.Cryptography.SHA256]::Create()
try { $mutexSuffix = ([BitConverter]::ToString($rootSha.ComputeHash([Text.Encoding]::UTF8.GetBytes($resolvedRoot))) -replace '-', '').Substring(0, 16) }
finally { $rootSha.Dispose() }
$currentFingerprint = Get-SourceFingerprint
$buildIdentity = Get-BuildIdentity
$outputMissing = -not (Test-Path -LiteralPath $sourceAssembly -PathType Leaf)
$outputStale = $outputMissing -or $null -eq $buildIdentity -or
    [string]$buildIdentity.sourceFingerprint -cne $currentFingerprint
$shouldBuild = (($BuildMode -eq '--build-if-stale') -and $outputStale) -or
    (($BuildMode -eq '--build-if-missing') -and $outputMissing)

if ($BuildMode -eq '--no-build' -and $outputMissing) {
    [Console]::Error.WriteLine("FoodDiary Development MCP is not built at '$sourceAssembly'.")
    exit 3
}
if ($shouldBuild) {
    $buildMutex = [Threading.Mutex]::new($false, "Local\FoodDiaryDevelopmentMcpBuild-$mutexSuffix")
    $mutexAcquired = $false
    try {
        try { $mutexAcquired = $buildMutex.WaitOne([TimeSpan]::FromMinutes(2)) }
        catch [Threading.AbandonedMutexException] { $mutexAcquired = $true }
        if (-not $mutexAcquired) { throw 'Timed out waiting for another Development MCP build to finish.' }
        $currentFingerprint = Get-SourceFingerprint
        $buildIdentity = Get-BuildIdentity
        $outputMissing = -not (Test-Path -LiteralPath $sourceAssembly -PathType Leaf)
        $outputStale = $outputMissing -or $null -eq $buildIdentity -or
            [string]$buildIdentity.sourceFingerprint -cne $currentFingerprint
        $shouldBuild = (($BuildMode -eq '--build-if-stale') -and $outputStale) -or
            (($BuildMode -eq '--build-if-missing') -and $outputMissing)
        if ($shouldBuild) {
            [Console]::Error.WriteLine("FoodDiary Development MCP output is $(if ($outputMissing) { 'absent' } else { 'stale' }); building before startup.")
            $buildOutput = @(& dotnet build $projectPath --nologo --verbosity quiet 2>&1)
            $buildExitCode = $LASTEXITCODE
            foreach ($line in $buildOutput) { [Console]::Error.WriteLine([string]$line) }
            if ($buildExitCode -ne 0 -or -not (Test-Path -LiteralPath $sourceAssembly -PathType Leaf)) { exit 3 }
            $postBuildFingerprint = Get-SourceFingerprint
            if ($postBuildFingerprint -cne $currentFingerprint) {
                throw 'Development MCP sources changed during the build. Restart the server so it can build a coherent snapshot.'
            }
            $head = (& git -C $resolvedRoot rev-parse HEAD).Trim()
            if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve the Development MCP build revision.' }
            $buildIdentity = [ordered]@{
                schemaVersion = 1
                builtAtUtc = [DateTime]::UtcNow.ToString('o')
                gitHead = $head
                sourceFingerprint = $currentFingerprint
            }
            [IO.File]::WriteAllText(
                $buildIdentityPath,
                (($buildIdentity | ConvertTo-Json) + [Environment]::NewLine),
                [Text.UTF8Encoding]::new($false))
        }
    } finally {
        if ($mutexAcquired) { $buildMutex.ReleaseMutex() }
        $buildMutex.Dispose()
    }
} elseif ($null -eq $buildIdentity) {
    $buildIdentity = [pscustomobject]@{ gitHead = $null; sourceFingerprint = $null }
}

if ([IO.Path]::GetDirectoryName($sessionRoot) -ne $systemTemp -or
    [IO.Path]::GetFileName($sessionRoot) -ne 'fooddiary-development-mcp') {
    throw "Unsafe Development MCP session root: $sessionRoot"
}
$null = New-Item -ItemType Directory -Path $sessionRoot -Force
Remove-StaleSessionDirectories -SessionRoot $sessionRoot

$publicationMutex = [Threading.Mutex]::new($false, "Local\FoodDiaryDevelopmentMcpBuild-$mutexSuffix")
$publicationMutexAcquired = $false
try {
    try { $publicationMutexAcquired = $publicationMutex.WaitOne([TimeSpan]::FromMinutes(2)) }
    catch [Threading.AbandonedMutexException] { $publicationMutexAcquired = $true }
    if (-not $publicationMutexAcquired) { throw 'Timed out waiting to publish the Development MCP runtime.' }
    $buildIdentity = Get-BuildIdentity
    $runtimeFingerprint = if ($null -ne $buildIdentity -and [string]$buildIdentity.sourceFingerprint -match '^[0-9a-f]{64}$') {
        [string]$buildIdentity.sourceFingerprint
    } else {
        Get-Sha256Hash -Path $sourceAssembly
    }
    $sessionDirectory = Join-Path $sessionRoot $runtimeFingerprint
    if (-not (Test-Path -LiteralPath (Join-Path $sessionDirectory 'FoodDiary.Development.Mcp.dll') -PathType Leaf)) {
        $stagingDirectory = Join-Path $sessionRoot ([guid]::NewGuid().ToString('N'))
        try {
            $null = New-Item -ItemType Directory -Path $stagingDirectory -Force
            Copy-Item -Path (Join-Path $sourceDirectory '*') -Destination $stagingDirectory -Recurse -Force
            Move-Item -LiteralPath $stagingDirectory -Destination $sessionDirectory
        } finally {
            Remove-Item -LiteralPath $stagingDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    [IO.Directory]::SetLastWriteTimeUtc($sessionDirectory, [DateTime]::UtcNow)
} finally {
    if ($publicationMutexAcquired) { $publicationMutex.ReleaseMutex() }
    $publicationMutex.Dispose()
}
if ($PrepareOnly) {
    Write-Output (Join-Path $sessionDirectory 'FoodDiary.Development.Mcp.dll')
    exit 0
}
$env:FOODDIARY_REPOSITORY_ROOT = $resolvedRoot
$env:FOODDIARY_MCP_BUILD_GIT_HEAD = [string]$buildIdentity.gitHead
$env:FOODDIARY_MCP_BUILD_SOURCE_FINGERPRINT = [string]$buildIdentity.sourceFingerprint
$env:FOODDIARY_MCP_SESSION_LOCK = Join-Path $sessionDirectory '.session.lock'
& dotnet (Join-Path $sessionDirectory 'FoodDiary.Development.Mcp.dll')
exit $LASTEXITCODE
