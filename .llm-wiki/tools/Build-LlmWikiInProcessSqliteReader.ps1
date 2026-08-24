[CmdletBinding()]
param(
    [switch]$Force,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$projectPath = Join-Path $PSScriptRoot 'LlmWiki.SqliteReader/LlmWiki.SqliteReader.csproj'
$artifactRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/in-process-sqlite-reader'
$buildArtifactRoot = Join-Path $repositoryRoot '.artifacts/dotnet/llm-wiki-in-process-sqlite-reader'
$runtimeIdentifier = [Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
$runtimeFramework = [Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
$inputPaths = @(
    $projectPath
    (Join-Path $PSScriptRoot 'LlmWiki.SqliteReader/DomainDataReader.cs')
    (Join-Path $repositoryRoot 'Directory.Build.props')
    (Join-Path $repositoryRoot 'Directory.Packages.props')
)
$fingerprintMaterial = [Text.StringBuilder]::new()
$null = $fingerprintMaterial.AppendLine("framework=$runtimeFramework")
$null = $fingerprintMaterial.AppendLine("rid=$runtimeIdentifier")
foreach ($path in $inputPaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "SQLite reader input is missing: $path" }
    $relativePath = [IO.Path]::GetRelativePath($repositoryRoot, $path).Replace('\', '/')
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    $null = $fingerprintMaterial.AppendLine("$relativePath=$hash")
}
$fingerprint = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($fingerprintMaterial.ToString()))).ToLowerInvariant()
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
