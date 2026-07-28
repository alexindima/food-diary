[CmdletBinding()]
param(
    [string[]]$Path,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$normalizedPaths = @($Path | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object {
    if ([System.IO.Path]::IsPathRooted($_)) { throw 'Content fingerprint paths must be repository-relative.' }
    $normalized = $_.Replace('\', '/')
    if ($normalized -match '(^|/)\.\.(/|$)') { throw "Content fingerprint path escapes the repository: $normalized" }
    $normalized
} | Sort-Object -Unique)
$entries = foreach ($normalizedPath in $normalizedPaths) {
    $absolutePath = Join-Path $repositoryRoot $normalizedPath
    if (Test-Path -LiteralPath $absolutePath -PathType Leaf) {
        [pscustomobject][ordered]@{
            path = $normalizedPath
            state = 'file'
            sha256 = (Get-FileHash -LiteralPath $absolutePath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    } elseif (Test-Path -LiteralPath $absolutePath -PathType Container) {
        [pscustomobject][ordered]@{ path = $normalizedPath; state = 'directory'; sha256 = '' }
    } else {
        [pscustomobject][ordered]@{ path = $normalizedPath; state = 'absent'; sha256 = '' }
    }
}
$json = @($entries) | ConvertTo-Json -Depth 5 -Compress
$bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
$sha = [System.Security.Cryptography.SHA256]::Create()
try {
    $fingerprint = ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant()
} finally { $sha.Dispose() }
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    fingerprint = $fingerprint
    pathCount = $normalizedPaths.Count
    entries = @($entries)
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 6 } else { $result }
