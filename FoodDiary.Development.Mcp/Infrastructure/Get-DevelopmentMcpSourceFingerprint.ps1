[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [ValidateSet('Text', 'Json')][string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($RepositoryRoot)
$inputs = @(
    Get-ChildItem -LiteralPath (Join-Path $root 'FoodDiary.Development.Mcp') -Recurse -File |
        Where-Object {
            $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]' -and
            $_.Extension -in @('.cs', '.csproj', '.props', '.targets')
        }
    @('Directory.Build.props', 'Directory.Packages.props', 'global.json') |
        ForEach-Object { Get-Item -LiteralPath (Join-Path $root $_) -ErrorAction SilentlyContinue }
) | Sort-Object { $_.FullName.Substring($root.Length + 1).Replace('\', '/') }

$lines = @($inputs | ForEach-Object {
    $relative = $_.FullName.Substring($root.Length + 1).Replace('\', '/')
    $stream = [IO.File]::OpenRead($_.FullName)
    $fileSha = [Security.Cryptography.SHA256]::Create()
    try { $hash = ([BitConverter]::ToString($fileSha.ComputeHash($stream)) -replace '-', '').ToLowerInvariant() }
    finally { $fileSha.Dispose(); $stream.Dispose() }
    "$relative`:$hash"
})
$payload = [Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
$sha = [Security.Cryptography.SHA256]::Create()
try { $fingerprint = ([BitConverter]::ToString($sha.ComputeHash($payload)) -replace '-', '').ToLowerInvariant() }
finally { $sha.Dispose() }
if ($Format -eq 'Json') {
    [pscustomobject]@{ schemaVersion = 1; fingerprint = $fingerprint; inputs = @($lines) } | ConvertTo-Json -Depth 4
} else {
    Write-Output $fingerprint
}
