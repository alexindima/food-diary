[CmdletBinding()]
param(
    [ValidateRange(0, 1000000)]
    [double]$DurationMs,
    [ValidateRange(0, 1000)]
    [int]$QueryTermCount,
    [ValidateRange(0, 1000)]
    [int]$CandidateCount,
    [string]$TopLayer,
    [string]$TopRole,
    [bool]$Ready = $true
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$path = Join-Path $repositoryRoot '.artifacts/llm-wiki/context-query-observations.jsonl'
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force
$entry = [pscustomobject][ordered]@{
    schemaVersion = 1
    observedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    durationMs = [Math]::Round($DurationMs, 2)
    queryTermCount = $QueryTermCount
    candidateCount = $CandidateCount
    topLayer = $(if ([string]::IsNullOrWhiteSpace($TopLayer)) { 'unknown' } else { $TopLayer })
    topRole = $(if ([string]::IsNullOrWhiteSpace($TopRole)) { 'unknown' } else { $TopRole })
    ready = $Ready
}
$line = ($entry | ConvertTo-Json -Compress) + [Environment]::NewLine
$bytes = [Text.UTF8Encoding]::new($false).GetBytes($line)
$pathHashBytes = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($path.ToLowerInvariant()))
$mutexName = 'Local\LlmWikiContextObservation-' + [Convert]::ToHexString($pathHashBytes)
$mutex = [Threading.Mutex]::new($false, $mutexName)
$lockTaken = $false
try {
    $lockTaken = $mutex.WaitOne([TimeSpan]::FromSeconds(10))
    if (-not $lockTaken) {
        throw "Timed out waiting to append context query observation to '$path'."
    }
    $stream = [IO.File]::Open($path, [IO.FileMode]::Append, [IO.FileAccess]::Write, [IO.FileShare]::Read)
    try { $stream.Write($bytes, 0, $bytes.Length) } finally { $stream.Dispose() }
} finally {
    if ($lockTaken) { $mutex.ReleaseMutex() }
    $mutex.Dispose()
}
