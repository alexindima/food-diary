[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$indexPaths = @(
    '.llm-wiki/generated/repository-catalog.json',
    '.llm-wiki/generated/csharp-symbol-index.json',
    '.llm-wiki/generated/backend-contract-index.json',
    '.llm-wiki/generated/quality-index.json',
    '.llm-wiki/generated/architecture-health-index.json'
)

function Add-Text([Security.Cryptography.IncrementalHash]$Hash, [string]$Value) {
    $Hash.AppendData([Text.Encoding]::UTF8.GetBytes($Value))
}

$head = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD for the Wiki index verification receipt.' }
function Get-GitBlobHashes([string[]]$Paths) {
    $startInfo = New-Object Diagnostics.ProcessStartInfo
    $startInfo.FileName = 'git'
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.Arguments = 'hash-object --stdin-paths'
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.StandardInputEncoding = [Text.UTF8Encoding]::new($false)
    $startInfo.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) { throw 'Unable to start Git source hashing.' }
        $outputTask = $process.StandardOutput.ReadToEndAsync()
        $errorTask = $process.StandardError.ReadToEndAsync()
        foreach ($path in $Paths) { $process.StandardInput.WriteLine($path) }
        $process.StandardInput.Close()
        $process.WaitForExit()
        $output = $outputTask.Result
        $errorOutput = $errorTask.Result
        if ($process.ExitCode -ne 0) { throw "Git source hashing failed: $($errorOutput.Trim())" }
        return @($output -split '\r?\n' | Where-Object { $_ })
    } finally { $process.Dispose() }
}

$deletedPaths = @(& git -C $repositoryRoot ls-files --deleted) | ForEach-Object { $_.Replace('\', '/') }
$sourcePaths = @(& git -C $repositoryRoot ls-files -co --exclude-standard) | Where-Object {
    $_ -and $_ -notmatch '^\.llm-wiki/(?:generated|reviews)/' -and
    $_ -notmatch '^\.artifacts/' -and $_ -notmatch '(?i)(review-receipt|source-impact-review)' -and
    $_.Replace('\', '/') -notin $deletedPaths
} | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique
$sourceHashes = @(Get-GitBlobHashes $sourcePaths)
if ($sourceHashes.Count -ne $sourcePaths.Count) {
    throw "Git hashed $($sourceHashes.Count) source files for $($sourcePaths.Count) repository paths."
}

$sourceHash = [Security.Cryptography.IncrementalHash]::CreateHash([Security.Cryptography.HashAlgorithmName]::SHA256)
try {
    for ($index = 0; $index -lt $sourcePaths.Count; $index++) {
        Add-Text $sourceHash "$($sourcePaths[$index]):$($sourceHashes[$index])`n"
    }
    $sourceFingerprint = ([BitConverter]::ToString($sourceHash.GetHashAndReset()) -replace '-', '').ToLowerInvariant()
} finally { $sourceHash.Dispose() }

$indexHash = [Security.Cryptography.IncrementalHash]::CreateHash([Security.Cryptography.HashAlgorithmName]::SHA256)
try {
    foreach ($path in $indexPaths | Sort-Object) {
        $absolutePath = Join-Path $repositoryRoot $path
        if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) { throw "Required Wiki index is missing: $path" }
        Add-Text $indexHash $path
        $indexHash.AppendData([IO.File]::ReadAllBytes($absolutePath))
    }
    $indexFingerprint = ([BitConverter]::ToString($indexHash.GetHashAndReset()) -replace '-', '').ToLowerInvariant()
} finally { $indexHash.Dispose() }

$gitDirectory = (& git -C $repositoryRoot rev-parse --absolute-git-dir).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve the Git directory for the Wiki index verification receipt.' }
$receiptPath = Join-Path $gitDirectory 'llm-wiki/index-verification.json'
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $receiptPath) -Force
$receipt = [ordered]@{
    schemaVersion = 1
    gitHead = $head
    sourceFingerprint = $sourceFingerprint
    indexFingerprint = $indexFingerprint
    verifiedAtUtc = [DateTime]::UtcNow.ToString('o')
}
$temporaryPath = "$receiptPath.$PID.tmp"
[IO.File]::WriteAllText($temporaryPath, (($receipt | ConvertTo-Json) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
Move-Item -LiteralPath $temporaryPath -Destination $receiptPath -Force
Write-Host "Wiki index verification receipt recorded for $($head.Substring(0, 10))."
