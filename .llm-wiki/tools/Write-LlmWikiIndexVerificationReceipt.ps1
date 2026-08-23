[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$manifestPath = Join-Path $repositoryRoot '.llm-wiki/policies/query-indexes.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Wiki query-index manifest is missing: $manifestPath"
}
try { $indexManifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json }
catch { throw "Wiki query-index manifest is invalid: $($_.Exception.Message)" }
$indexPaths = @($indexManifest.paths | ForEach-Object { ([string]$_).Replace('\', '/') } | Where-Object { $_ } | Sort-Object -Unique)
if ([int]$indexManifest.schemaVersion -ne 1 -or $indexPaths.Count -eq 0 -or
    @($indexPaths | Where-Object { [IO.Path]::IsPathRooted($_) -or $_ -match '(^|/)\.\.(/|$)' }).Count -gt 0) {
    throw 'Wiki query-index manifest must use schemaVersion 1 and contain only repository-relative paths.'
}

function Add-Text([Security.Cryptography.IncrementalHash]$Hash, [string]$Value) {
    $Hash.AppendData([Text.Encoding]::UTF8.GetBytes($Value))
}

$head = (Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', 'HEAD') -FailureMessage 'Unable to resolve HEAD for the Wiki index verification receipt.').Lines[0].Trim()
function Get-GitBlobHashes([string[]]$Paths) {
    # A live redirected pipe needs an explicit BOM-free StandardInputEncoding to stay
    # stable, but that ProcessStartInfo property exists only on .NET 5+ (PowerShell 7+);
    # on Windows PowerShell 5.1 the default StreamWriter encoding can also inject a stray
    # UTF-8 preamble into the stream once its internal buffer first flushes, corrupting the
    # path git reads at that boundary. Redirecting through temp files instead of a live pipe
    # sidesteps both problems and behaves identically on every PowerShell/.NET runtime.
    $stdinPath = [IO.Path]::GetTempFileName()
    $stdoutPath = [IO.Path]::GetTempFileName()
    $stderrPath = [IO.Path]::GetTempFileName()
    try {
        [IO.File]::WriteAllText($stdinPath, (($Paths -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
        $process = Start-Process -FilePath 'git' -ArgumentList 'hash-object', '--stdin-paths' `
            -WorkingDirectory $repositoryRoot -RedirectStandardInput $stdinPath `
            -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath `
            -NoNewWindow -PassThru -Wait
        if ($process.ExitCode -ne 0) {
            # Get-Content -Raw on a genuinely empty file emits no pipeline output at all on
            # Windows PowerShell 5.1 (captured as PowerShell's internal AutomationNull, which
            # still fools `-eq $null` but throws on any method call, unlike PowerShell 7+
            # which returns ''); `-join ''` reliably collapses either case to a real string.
            # -Encoding UTF8 matches the BOM-free UTF-8 git wrote, instead of Get-Content's
            # system-codepage default for a BOM-less file on Windows PowerShell 5.1.
            $errorOutput = (Get-Content -LiteralPath $stderrPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue) -join ''
            throw "Git source hashing failed: $($errorOutput.Trim())"
        }
        return @(Get-Content -LiteralPath $stdoutPath -Encoding UTF8 | Where-Object { $_ })
    } finally {
        Remove-Item -LiteralPath $stdinPath, $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    }
}

$deletedPaths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--deleted') -FailureMessage 'Unable to enumerate deleted paths for the Wiki index verification receipt.')
$sourcePaths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '-co', '--exclude-standard') -FailureMessage 'Unable to enumerate source paths for the Wiki index verification receipt.') | Where-Object {
    $_ -and $_ -notmatch '^\.llm-wiki/(?:generated|reviews)/' -and
    $_ -notmatch '^\.artifacts/' -and $_ -notmatch '(?i)(review-receipt|source-impact-review)' -and
    $_ -notin $deletedPaths
} | Sort-Object -Unique
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

$gitDirectory = (Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', '--absolute-git-dir') -FailureMessage 'Unable to resolve the Git directory for the Wiki index verification receipt.').Lines[0].Trim()
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
