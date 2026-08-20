[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string[]]$Path,
    [string[]]$Name,
    [object[]]$Contract,
    [switch]$BuildBackendIndex
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')
$projectPath = Join-Path $PSScriptRoot 'contract-reference-extractor/LlmWiki.ContractReferenceExtractor.csproj'
if ($BuildBackendIndex -and @($Contract).Count -eq 0) { throw 'BuildBackendIndex requires contract metadata.' }
if (-not $BuildBackendIndex -and @($Name).Count -eq 0) { throw 'Reference extraction requires at least one name.' }
$fingerprint = Get-LlmWikiIndexInputFingerprint $RepositoryRoot @(
    '.llm-wiki/tools/contract-reference-extractor/LlmWiki.ContractReferenceExtractor.csproj'
    '.llm-wiki/tools/contract-reference-extractor/Program.cs'
)
$artifactRoot = Join-Path $RepositoryRoot ".artifacts/llm-wiki/contract-reference-extractor/$fingerprint"
$readyPath = Join-Path $artifactRoot 'ready.txt'
$extractorOutputDirectory = Join-Path $artifactRoot 'bin/LlmWiki.ContractReferenceExtractor/release'
$extractorDllPath = Join-Path $extractorOutputDirectory 'LlmWiki.ContractReferenceExtractor.dll'
$extractorRuntimeConfigPath = Join-Path $extractorOutputDirectory 'LlmWiki.ContractReferenceExtractor.runtimeconfig.json'
$lockRoot = Join-Path $RepositoryRoot '.artifacts/llm-wiki/contract-reference-extractor'
$lockPath = Join-Path $lockRoot 'build.lock'
$null = New-Item -ItemType Directory -Path $lockRoot -Force

$lockStream = $null
$deadline = [DateTime]::UtcNow.AddMinutes(2)
while ($null -eq $lockStream) {
    try {
        $lockStream = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    } catch [IO.IOException] {
        if ([DateTime]::UtcNow -ge $deadline) { throw 'Timed out waiting for the contract-reference extractor build lock.' }
        Start-Sleep -Milliseconds 100
    }
}
try {
    if (-not (Test-Path -LiteralPath $readyPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $extractorDllPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $extractorRuntimeConfigPath -PathType Leaf)) {
        $buildOutput = & dotnet build $projectPath -c Release --artifacts-path $artifactRoot --nologo --verbosity quiet 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { throw "Contract-reference extractor build failed.`n$($buildOutput.Trim())" }
        if (-not (Test-Path -LiteralPath $extractorDllPath -PathType Leaf) -or
            -not (Test-Path -LiteralPath $extractorRuntimeConfigPath -PathType Leaf)) {
            throw 'Contract-reference extractor build produced no runnable framework-dependent application.'
        }
        [IO.File]::WriteAllText($readyPath, $fingerprint + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    }
} finally {
    $lockStream.Dispose()
}

$payload = if ($BuildBackendIndex) {
    [ordered]@{ paths = @($Path); contracts = @($Contract) } | ConvertTo-Json -Depth 8 -Compress
} else {
    [ordered]@{ paths = @($Path); names = @($Name) } | ConvertTo-Json -Compress
}
$startInfo = [Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = 'dotnet'
$startInfo.WorkingDirectory = $RepositoryRoot
$startInfo.UseShellExecute = $false
$startInfo.RedirectStandardInput = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.StandardInputEncoding = [Text.UTF8Encoding]::new($false)
$startInfo.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
$startInfo.StandardErrorEncoding = [Text.UTF8Encoding]::new($false)
$startInfo.ArgumentList.Add($extractorDllPath)
$startInfo.ArgumentList.Add($(if ($BuildBackendIndex) { '--backend-index' } else { '--stdin' }))
$process = [Diagnostics.Process]::new()
$process.StartInfo = $startInfo
try {
    if (-not $process.Start()) { throw 'Unable to start the contract-reference extractor.' }
    $outputTask = $process.StandardOutput.ReadToEndAsync()
    $errorTask = $process.StandardError.ReadToEndAsync()
    $process.StandardInput.Write($payload)
    $process.StandardInput.Close()
    if (-not $process.WaitForExit(120000)) {
        $process.Kill($true)
        throw 'Contract-reference extractor timed out after 120 seconds.'
    }
    $output = $outputTask.GetAwaiter().GetResult()
    $errorText = $errorTask.GetAwaiter().GetResult().Trim()
    if ($process.ExitCode -ne 0) {
        throw "Contract-reference extractor failed with exit code $($process.ExitCode).$(if ($errorText) { " $errorText" })"
    }
} finally {
    $process.Dispose()
}

if ($BuildBackendIndex) {
    if ($errorText -notmatch 'LLM_WIKI_METRICS contracts=(?<contracts>\d+);consumerEdges=(?<edges>\d+)') {
        throw 'Contract-reference extractor omitted backend-index metrics.'
    }
    [pscustomobject]@{ indexJson = $output; contracts = [int]$Matches['contracts']; consumerEdges = [int]$Matches['edges'] }
} else {
    try { @($output | ConvertFrom-Json) }
    catch { throw "Contract-reference extractor returned invalid JSON: $($_.Exception.Message)" }
}
