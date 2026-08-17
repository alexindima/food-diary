[CmdletBinding()]
param(
    [ValidateSet('Check', 'Record')]
    [string]$Action,
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$BaseRef = 'HEAD',
    [string[]]$ChangedPath = @(),
    [string]$Mode = 'default'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')

function Invoke-Git([string[]]$Arguments) {
    $output = @(& git -C $RepositoryRoot @Arguments)
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments -join ' ') failed." }
    return $output
}

function Get-Sha256([string]$Value) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Value))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Get-FileSha256([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '<missing>' }
    $stream = [IO.File]::OpenRead($Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose(); $stream.Dispose() }
}

function ConvertTo-RepositoryPath([string]$Path) { return $Path.Trim().Replace('\', '/') }

$head = (Invoke-Git @('rev-parse', 'HEAD') | Select-Object -First 1)
$requestedBaseRef = $BaseRef
$resolvedBase = Resolve-LlmWikiCommitRef -RepositoryRoot $RepositoryRoot -Ref $BaseRef
$workspacePaths = @(
    Invoke-Git @('diff', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--')
    Invoke-Git @('ls-files', '--others', '--exclude-standard')
) | Where-Object { $_ } | ForEach-Object { ConvertTo-RepositoryPath $_ } | Sort-Object -Unique
$workspaceMetadata = @(Invoke-Git @('diff', '--raw', 'HEAD', '--'))
$workspaceEntries = @($workspacePaths | ForEach-Object {
    [ordered]@{ path = $_; hash = Get-FileSha256 (Join-Path $RepositoryRoot $_) }
})
$scope = @($ChangedPath | Where-Object { $_ } | ForEach-Object { ConvertTo-RepositoryPath $_ } | Sort-Object -Unique)
$environment = "pwsh=$($PSVersionTable.PSVersion);os=$([Runtime.InteropServices.RuntimeInformation]::OSDescription);arch=$([Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)"
$fingerprint = Get-Sha256 (@(
    'schema=3'
    "head=$head"
    "resolvedBase=$resolvedBase"
    "mode=$Mode"
    "environment=$environment"
    "scope=$($scope -join '|')"
    "gitRaw=$($workspaceMetadata -join '|')"
    @($workspaceEntries | ForEach-Object { "$($_.path):$($_.hash)" })
) -join "`n")

$gitDirectory = (Invoke-Git @('rev-parse', '--absolute-git-dir') | Select-Object -First 1)
$receiptPath = Join-Path $gitDirectory 'llm-wiki/verification-cache/verify-fast.json'
$hit = $false
$incrementalStyleOnly = $false
$incrementalPaths = @()
if (Test-Path -LiteralPath $receiptPath -PathType Leaf) {
    try {
        $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
        $hit = [int]$receipt.schemaVersion -eq 3 -and [string]$receipt.fingerprint -ceq $fingerprint
        $compatibleReceipt = [int]$receipt.schemaVersion -eq 3 -and
            [string]$receipt.head -ceq $head -and
            [string]$receipt.resolvedBase -ceq $resolvedBase -and
            [string]$receipt.mode -ceq $Mode -and
            [string]$receipt.environment -ceq $environment -and
            (@($receipt.scope) -join '|') -ceq ($scope -join '|')
        if (-not $hit -and $compatibleReceipt -and $Mode -eq 'visual-ui') {
            $previousEntries = @{}
            foreach ($entry in @($receipt.workspaceEntries)) { $previousEntries[[string]$entry.path] = [string]$entry.hash }
            $currentEntries = @{}
            foreach ($entry in $workspaceEntries) { $currentEntries[[string]$entry.path] = [string]$entry.hash }
            $allPaths = @($previousEntries.Keys + $currentEntries.Keys | Sort-Object -Unique)
            $incrementalPaths = @($allPaths | Where-Object {
                -not $previousEntries.ContainsKey($_) -or -not $currentEntries.ContainsKey($_) -or
                $previousEntries[$_] -cne $currentEntries[$_]
            })
            $incrementalStyleOnly = $incrementalPaths.Count -gt 0 -and
                @($incrementalPaths | Where-Object { $_ -notmatch '\.(?:scss|css)$' }).Count -eq 0
        }
    } catch { $hit = $false }
}

if ($Action -eq 'Record') {
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $receiptPath) -Force
    $receipt = [ordered]@{
        schemaVersion = 3
        fingerprint = $fingerprint
        recordedAtUtc = [DateTime]::UtcNow.ToString('o')
        head = $head
        requestedBaseRef = $requestedBaseRef
        resolvedBase = $resolvedBase
        mode = $Mode
        environment = $environment
        scope = $scope
        workspacePaths = @($workspacePaths)
        workspaceEntries = @($workspaceEntries)
    }
    [IO.File]::WriteAllText($receiptPath, (($receipt | ConvertTo-Json -Depth 5) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $hit = $true
}

return [pscustomobject]@{
    hit = $hit
    fingerprint = $fingerprint
    receiptPath = $receiptPath
    workspacePathCount = @($workspacePaths).Count
    scopePathCount = $scope.Count
    incrementalStyleOnly = $incrementalStyleOnly
    incrementalPaths = @($incrementalPaths)
}
