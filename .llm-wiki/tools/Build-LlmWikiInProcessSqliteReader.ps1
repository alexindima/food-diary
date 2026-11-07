[CmdletBinding()]
param(
    [switch]$Force,
    [switch]$CheckOnly,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')

function Get-CurrentRuntimeIdentifier {
    $runtimeInformationType = [Runtime.InteropServices.RuntimeInformation]
    $runtimeIdentifierProperty = $runtimeInformationType.GetProperty('RuntimeIdentifier')
    if ($null -ne $runtimeIdentifierProperty) {
        return [string]$runtimeIdentifierProperty.GetValue($null, $null)
    }

    $osPrefix = if ($runtimeInformationType::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)) {
        'win'
    } elseif ($runtimeInformationType::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Linux)) {
        'linux'
    } elseif ($runtimeInformationType::IsOSPlatform([Runtime.InteropServices.OSPlatform]::OSX)) {
        'osx'
    } else {
        throw 'Unable to derive a runtime identifier for the current operating system.'
    }
    $architecture = $runtimeInformationType::ProcessArchitecture.ToString().ToLowerInvariant()
    return "$osPrefix-$architecture"
}

function Get-RelativePathPortable {
    param(
        [Parameter(Mandatory)][string]$BasePath,
        [Parameter(Mandatory)][string]$Path
    )

    $relativePathMethod = [IO.Path].GetMethods() |
        Where-Object { $_.Name -eq 'GetRelativePath' -and $_.GetParameters().Count -eq 2 } |
        Select-Object -First 1
    if ($null -ne $relativePathMethod) {
        return [string]$relativePathMethod.Invoke($null, @($BasePath, $Path))
    }

    $baseFullPath = [IO.Path]::GetFullPath($BasePath).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $pathFullPath = [IO.Path]::GetFullPath($Path)
    $baseUri = [Uri]$baseFullPath
    $pathUri = [Uri]$pathFullPath
    return [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()).Replace('/', [IO.Path]::DirectorySeparatorChar)
}

function Get-Sha256Hex {
    param([Parameter(Mandatory)][byte[]]$Bytes)

    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $algorithm.ComputeHash($Bytes)
    } finally {
        $algorithm.Dispose()
    }
    return -join @($hashBytes | ForEach-Object { $_.ToString('x2') })
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$projectPath = Join-Path $PSScriptRoot 'LlmWiki.SqliteReader/LlmWiki.SqliteReader.csproj'
$artifactRepositoryRoot = $repositoryRoot
if (-not [string]::IsNullOrWhiteSpace($env:LLM_WIKI_READ_ONLY_SOURCE_ROOT) -and
    (Test-Path -LiteralPath $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT -PathType Container)) {
    $artifactRepositoryRoot = [IO.Path]::GetFullPath($env:LLM_WIKI_READ_ONLY_SOURCE_ROOT)
}
$artifactRoot = Join-Path $artifactRepositoryRoot '.artifacts/llm-wiki/in-process-sqlite-reader'
$buildArtifactRoot = Join-Path $artifactRepositoryRoot '.artifacts/dotnet/llm-wiki-in-process-sqlite-reader'
$runtimeIdentifier = Get-CurrentRuntimeIdentifier
$runtimeFramework = [Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
$inputPaths = @(
    $PSCommandPath
    $projectPath
    (Join-Path $PSScriptRoot 'LlmWiki.SqliteReader/DomainDataReader.cs')
    (Join-Path $PSScriptRoot 'LlmWiki.SqliteReader/CompiledIndexReader.cs')
    (Join-Path $PSScriptRoot 'LlmWiki.SqliteReader/StandaloneIndexReader.cs')
    (Join-Path $PSScriptRoot 'LlmWiki.SqliteReader/ContextSearchReader.cs')
    (Join-Path $repositoryRoot 'FoodDiary.Development.Mcp/Wiki/SqliteContextSearchReader.cs')
    (Join-Path $repositoryRoot 'FoodDiary.Development.Mcp/Protocol/WikiContextSearchResult.cs')
    (Join-Path $repositoryRoot 'FoodDiary.Development.Mcp/Protocol/WikiContextSearchCandidate.cs')
    (Join-Path $repositoryRoot 'Directory.Build.props')
    (Join-Path $repositoryRoot 'Directory.Packages.props')
)
$fingerprintMaterial = [Text.StringBuilder]::new()
$null = $fingerprintMaterial.AppendLine("framework=$runtimeFramework")
$null = $fingerprintMaterial.AppendLine("rid=$runtimeIdentifier")
foreach ($path in $inputPaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "SQLite reader input is missing: $path" }
    $relativePath = (Get-RelativePathPortable -BasePath $repositoryRoot -Path $path).Replace('\', '/')
    $hash = if ([IO.Path]::GetExtension($path) -in @('.props', '.csproj')) {
        # XML normalizes line endings when MSBuild reads it. Git's detached
        # read-only checkout may use LF while the live workspace still has CRLF.
        $xmlText = [IO.File]::ReadAllText($path).Replace("`r`n", "`n").Replace("`r", "`n")
        Get-Sha256Hex ([Text.Encoding]::UTF8.GetBytes($xmlText))
    } else {
        # Keep exact C# bytes: verbatim/raw string literals can contain newlines.
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $null = $fingerprintMaterial.AppendLine("$relativePath=$hash")
}
$fingerprint = Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes($fingerprintMaterial.ToString()))
$outputPath = Join-Path $artifactRoot $fingerprint
$assemblyPath = Join-Path $outputPath 'LlmWiki.SqliteReader.dll'
$manifestPath = Join-Path $outputPath 'build-manifest.json'
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
function Test-ReaderBuild {
    try {
        $saved = [IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
        if ($saved.schemaVersion -ne 2 -or $saved.fingerprint -cne $fingerprint -or $saved.runtimeIdentifier -cne $runtimeIdentifier -or $saved.runtimeFramework -cne $runtimeFramework) { return $false }
        $files = @($saved.outputs)
        foreach ($required in @('LlmWiki.SqliteReader.dll','Microsoft.Data.Sqlite.dll','SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll')) {
            if (@($files | Where-Object path -CEQ $required).Count -ne 1) { return $false }
        }
        if (@($files | Where-Object { $_.path -in @('e_sqlite3.dll','libe_sqlite3.so','libe_sqlite3.dylib') }).Count -ne 1) { return $false }
        $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($file in $files) {
            if (-not $seen.Add([string]$file.path) -or [IO.Path]::IsPathRooted($file.path) -or $file.path -match '(?:^|[\\/])\.\.(?:[\\/]|$)' -or $file.sha256 -notmatch '^[a-f0-9]{64}$') { return $false }
            if ((Get-FileHash -LiteralPath (Join-Path $outputPath $file.path) -Algorithm SHA256 -ErrorAction Stop).Hash.ToLowerInvariant() -cne $file.sha256) { return $false }
        }
        $null = [Reflection.AssemblyName]::GetAssemblyName($assemblyPath)
        return $true
    } catch { return $false }
}
$reused = -not $Force -and (Test-ReaderBuild)
$buildLock = $null
$pendingOutput = $null
try {
if (-not $reused -and -not $CheckOnly) {
    $null = [IO.Directory]::CreateDirectory($artifactRoot)
    $lockTimer = [Diagnostics.Stopwatch]::StartNew()
    while ($null -eq $buildLock) {
        try { $buildLock = [IO.File]::Open((Join-Path $artifactRoot 'build.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
        catch [IO.IOException] {
            if ($lockTimer.Elapsed.TotalSeconds -ge 120) { throw 'Timed out waiting for the SQLite reader build.' }
            Start-Sleep -Milliseconds 50
        }
    }
    $reused = -not $Force -and (Test-ReaderBuild)
    if (-not $reused) {
    $pendingOutput = "$outputPath.pending-$([guid]::NewGuid().ToString('N'))"
    $null = [IO.Directory]::CreateDirectory($pendingOutput)
    $publishOutput = & dotnet publish $projectPath `
        --configuration Release `
        --artifacts-path $buildArtifactRoot `
        --output $pendingOutput `
        --nologo 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "In-process SQLite reader publish failed:`n$($publishOutput -join "`n")"
    }
    $nativeDirectory = Join-Path $pendingOutput "runtimes/$runtimeIdentifier/native"
    if (-not (Test-Path -LiteralPath $nativeDirectory -PathType Container)) {
        $nativeDirectory = Get-ChildItem -LiteralPath (Join-Path $pendingOutput 'runtimes') -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -eq $runtimeIdentifier -or $runtimeIdentifier.StartsWith("$($_.Name)-", [StringComparison]::OrdinalIgnoreCase) } |
            ForEach-Object { Join-Path $_.FullName 'native' } |
            Where-Object { Test-Path -LiteralPath $_ -PathType Container } |
            Select-Object -First 1
    }
    if ([string]::IsNullOrWhiteSpace([string]$nativeDirectory)) {
        throw "Published SQLite reader does not contain native assets for '$runtimeIdentifier'."
    }
    foreach ($nativeFile in Get-ChildItem -LiteralPath $nativeDirectory -File) {
        Copy-Item -LiteralPath $nativeFile.FullName -Destination (Join-Path $pendingOutput $nativeFile.Name) -Force
    }
    $manifest = [pscustomobject][ordered]@{
        schemaVersion = 2
        fingerprint = $fingerprint
        runtimeIdentifier = $runtimeIdentifier
        runtimeFramework = $runtimeFramework
        assemblyPath = $assemblyPath
        outputs = @(Get-ChildItem -LiteralPath $pendingOutput -Recurse -File | Sort-Object FullName | ForEach-Object {
            [ordered]@{ path = (Get-RelativePathPortable $pendingOutput $_.FullName).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
        })
    }
    Write-LlmWikiTextFileAtomically -Path (Join-Path $pendingOutput 'build-manifest.json') -Content ($manifest | ConvertTo-Json -Depth 6)
    if ([IO.Directory]::Exists($outputPath)) {
        # Preserve the invalid generation for diagnosis; never modify DLLs that
        # an existing reader may have loaded into its process.
        [IO.Directory]::Move($outputPath, "$outputPath.invalid-$([guid]::NewGuid().ToString('N'))")
    }
    [IO.Directory]::Move($pendingOutput, $outputPath)
    $pendingOutput = $null
    if (-not (Test-ReaderBuild)) { throw 'Published SQLite reader failed output validation.' }
    }
}
} finally {
    try {
    if ($null -ne $pendingOutput -and [IO.Directory]::Exists($pendingOutput)) {
        $ownedPrefix = $outputPath + '.pending-'
        if (-not $pendingOutput.StartsWith($ownedPrefix, [StringComparison]::Ordinal)) { throw 'Invalid owned SQLite reader staging path.' }
        Remove-Item -LiteralPath $pendingOutput -Recurse -Force
    }
    } finally { if ($null -ne $buildLock) { $buildLock.Dispose() } }
}
$stopwatch.Stop()
$result = [pscustomobject][ordered]@{
    ready = Test-ReaderBuild
    reused = $reused
    fingerprint = $fingerprint
    runtimeIdentifier = $runtimeIdentifier
    outputPath = $outputPath
    assemblyPath = $assemblyPath
    durationMs = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 4; exit 0 }
Write-Host "In-process SQLite reader: ready=$($result.ready), reused=$reused, RID=$runtimeIdentifier, duration=$($result.durationMs)ms."
