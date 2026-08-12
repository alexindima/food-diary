[CmdletBinding()]
param(
    [ValidateSet('Record', 'List')]
    [string]$Action = 'List',
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$Command,
    [ValidateSet('passed', 'failed')]
    [string]$Result = 'passed',
    [ValidateRange(0, 86400)]
    [double]$DurationSeconds = 0,
    [string[]]$CoverageScope = @(),
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiVerificationReceipts.ps1')

if ($Action -eq 'Record') {
    if ([string]::IsNullOrWhiteSpace($Command)) { throw 'Command is required when recording verification.' }
    $state = Get-LlmWikiVerificationFingerprint $RepositoryRoot
    $normalizedCommand = Normalize-LlmWikiVerificationCommand $Command
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        command = $Command.Trim()
        normalizedCommand = $normalizedCommand
        result = $Result
        durationSeconds = [Math]::Round($DurationSeconds, 2)
        coverageScope = @($CoverageScope | Where-Object { $_ } | Sort-Object -Unique)
        recordedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        head = $state.head
        fingerprint = $state.fingerprint
        changedPaths = @($state.paths)
    }
    $root = Get-LlmWikiVerificationReceiptRoot $RepositoryRoot
    $null = New-Item -ItemType Directory -Path $root -Force
    $path = Join-Path $root "$(Get-LlmWikiSha256 $normalizedCommand).json"
    [IO.File]::WriteAllText($path, (($receipt | ConvertTo-Json -Depth 6) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}

$receipts = @(Get-LlmWikiVerificationReceipts $RepositoryRoot)
if ($Format -eq 'Json') { $receipts | ConvertTo-Json -Depth 6; exit 0 }
if ($receipts.Count -eq 0) { Write-Host 'No verification receipts recorded.'; exit 0 }
foreach ($receipt in $receipts) {
    $stateLabel = if ($receipt.validForCurrentState) { 'satisfied' } else { 'stale' }
    Write-Host "[$stateLabel; $($receipt.durationSeconds)s] $($receipt.command)"
    if (@($receipt.coverageScope).Count -gt 0) { Write-Host "  coverage: $(@($receipt.coverageScope) -join ', ')" }
}
