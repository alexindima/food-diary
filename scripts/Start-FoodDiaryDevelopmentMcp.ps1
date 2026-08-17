[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$BuildMode = '--build-if-stale',
    [Parameter(Position = 1)]
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
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
    $rootSha = [Security.Cryptography.SHA256]::Create()
    try { $mutexSuffix = ([BitConverter]::ToString($rootSha.ComputeHash([Text.Encoding]::UTF8.GetBytes($resolvedRoot))) -replace '-', '').Substring(0, 16) }
    finally { $rootSha.Dispose() }
    $buildMutex = [Threading.Mutex]::new($false, "Local\FoodDiaryDevelopmentMcpBuild-$mutexSuffix")
    $mutexAcquired = $false
    try {
        try { $mutexAcquired = $buildMutex.WaitOne([TimeSpan]::FromMinutes(2)) }
        catch [Threading.AbandonedMutexException] { $mutexAcquired = $true }
        if (-not $mutexAcquired) { throw 'Timed out waiting for another Development MCP build to finish.' }
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

$sessionRoot = Join-Path ([IO.Path]::GetTempPath()) 'fooddiary-development-mcp'
$sessionDirectory = Join-Path $sessionRoot ([guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $sessionDirectory -Force
try {
    Copy-Item -Path (Join-Path $sourceDirectory '*') -Destination $sessionDirectory -Recurse -Force
    $env:FOODDIARY_REPOSITORY_ROOT = $resolvedRoot
    $env:FOODDIARY_MCP_BUILD_GIT_HEAD = [string]$buildIdentity.gitHead
    $env:FOODDIARY_MCP_BUILD_SOURCE_FINGERPRINT = [string]$buildIdentity.sourceFingerprint
    & dotnet (Join-Path $sessionDirectory 'FoodDiary.Development.Mcp.dll')
    exit $LASTEXITCODE
} finally {
    Remove-Item -LiteralPath $sessionDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
