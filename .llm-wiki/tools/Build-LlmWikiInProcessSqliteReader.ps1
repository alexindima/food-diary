[CmdletBinding()]
param(
    [switch]$Force,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'

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
    $projectPath
    (Join-Path $PSScriptRoot 'LlmWiki.SqliteReader/DomainDataReader.cs')
    (Join-Path $PSScriptRoot 'LlmWiki.SqliteReader/CompiledIndexReader.cs')
    (Join-Path $repositoryRoot 'Directory.Build.props')
    (Join-Path $repositoryRoot 'Directory.Packages.props')
)
$fingerprintMaterial = [Text.StringBuilder]::new()
$null = $fingerprintMaterial.AppendLine("framework=$runtimeFramework")
$null = $fingerprintMaterial.AppendLine("rid=$runtimeIdentifier")
foreach ($path in $inputPaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "SQLite reader input is missing: $path" }
    $relativePath = (Get-RelativePathPortable -BasePath $repositoryRoot -Path $path).Replace('\', '/')
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    $null = $fingerprintMaterial.AppendLine("$relativePath=$hash")
}
$fingerprint = Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes($fingerprintMaterial.ToString()))
$outputPath = Join-Path $artifactRoot $fingerprint
$assemblyPath = Join-Path $outputPath 'LlmWiki.SqliteReader.dll'
$manifestPath = Join-Path $outputPath 'build-manifest.json'
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$reused = -not $Force -and (Test-Path -LiteralPath $assemblyPath -PathType Leaf) -and (Test-Path -LiteralPath $manifestPath -PathType Leaf)
if (-not $reused) {
    $null = New-Item -ItemType Directory -Path $outputPath -Force
    $publishOutput = & dotnet publish $projectPath `
        --configuration Release `
        --artifacts-path $buildArtifactRoot `
        --output $outputPath `
        --nologo 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "In-process SQLite reader publish failed:`n$($publishOutput -join "`n")"
    }
    $nativeDirectory = Join-Path $outputPath "runtimes/$runtimeIdentifier/native"
    if (-not (Test-Path -LiteralPath $nativeDirectory -PathType Container)) {
        $nativeDirectory = Get-ChildItem -LiteralPath (Join-Path $outputPath 'runtimes') -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -eq $runtimeIdentifier -or $runtimeIdentifier.StartsWith("$($_.Name)-", [StringComparison]::OrdinalIgnoreCase) } |
            ForEach-Object { Join-Path $_.FullName 'native' } |
            Where-Object { Test-Path -LiteralPath $_ -PathType Container } |
            Select-Object -First 1
    }
    if ([string]::IsNullOrWhiteSpace([string]$nativeDirectory)) {
        throw "Published SQLite reader does not contain native assets for '$runtimeIdentifier'."
    }
    foreach ($nativeFile in Get-ChildItem -LiteralPath $nativeDirectory -File) {
        Copy-Item -LiteralPath $nativeFile.FullName -Destination (Join-Path $outputPath $nativeFile.Name) -Force
    }
    $manifest = [pscustomobject][ordered]@{
        schemaVersion = 1
        fingerprint = $fingerprint
        runtimeIdentifier = $runtimeIdentifier
        runtimeFramework = $runtimeFramework
        assemblyPath = $assemblyPath
    }
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding utf8
}
$stopwatch.Stop()
$result = [pscustomobject][ordered]@{
    ready = $true
    reused = $reused
    fingerprint = $fingerprint
    runtimeIdentifier = $runtimeIdentifier
    outputPath = $outputPath
    assemblyPath = $assemblyPath
    durationMs = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 4; exit 0 }
Write-Host "In-process SQLite reader: ready=True, reused=$reused, RID=$runtimeIdentifier, duration=$($result.durationMs)ms."
