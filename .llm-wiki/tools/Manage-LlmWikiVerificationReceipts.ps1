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
    [string]$SourceEvidencePath,
    [string]$CheckId,
    [string]$ExpectedFingerprint,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiVerificationReceipts.ps1')

if ($Action -eq 'Record') {
    if ([string]::IsNullOrWhiteSpace($Command)) { throw 'Command is required when recording verification.' }
    $state = Get-LlmWikiVerificationFingerprint $RepositoryRoot
    if ($PSBoundParameters.ContainsKey('ExpectedFingerprint') -and ($ExpectedFingerprint -cne $state.fingerprint -or -not $PSBoundParameters.ContainsKey('SourceEvidencePath'))) { throw 'The measured execution state no longer matches current repository inputs.' }
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
    if ($PSBoundParameters.ContainsKey('SourceEvidencePath')) {
        if ([string]::IsNullOrWhiteSpace($CheckId)) { throw 'SourceEvidencePath requires the actual executed CheckId.' }
        $sourceAbsolute = [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($SourceEvidencePath)) { $SourceEvidencePath } else { Join-Path $RepositoryRoot $SourceEvidencePath }))
        $prefix = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        if (-not $sourceAbsolute.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Source execution evidence must remain inside the repository.' }
        $sourceRelative = $sourceAbsolute.Substring($prefix.Length).Replace('\', '/')
        $validation = & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') -EvidencePath $sourceRelative -Format Json | ConvertFrom-Json
        $validExecution = @($validation.items | Where-Object { $_.kind -eq 'check' -and $_.id -eq $CheckId -and $_.cacheReusable })
        if ($validExecution.Count -ne 1) { throw 'Only a current passing executed check with a hashed log can publish execution proof.' }
        $source = Get-Content -LiteralPath $sourceAbsolute -Raw | ConvertFrom-Json
        $entry = $source.checks | Where-Object id -eq $CheckId | Select-Object -First 1
        if ($Result -ne 'passed' -or $null -eq $entry.lineage.execution.exitCode -or [int]$entry.lineage.execution.exitCode -ne 0 -or (Normalize-LlmWikiVerificationCommand $entry.command) -cne $normalizedCommand) { throw 'Receipt command/result must match its actual passing source check.' }
        if (-not (Test-LlmWikiExecutionSeal $entry ([string]$state.fingerprint) $RepositoryRoot)) { throw 'Schema2 publication requires the original actual whole-repository execution seal; scoped or legacy lineage cannot be promoted.' }
        $receipt.fingerprint = [string]$entry.lineage.execution.verification.fingerprint
        if (($PSBoundParameters.ContainsKey('ExpectedFingerprint') -and $receipt.fingerprint -cne $ExpectedFingerprint) -or (Get-LlmWikiVerificationFingerprint $RepositoryRoot).fingerprint -cne $receipt.fingerprint) { throw 'Repository inputs changed before execution receipt publication.' }
        $receipt.schemaVersion = 2
        $receipt.durationSeconds = $entry.lineage.execution.durationSeconds
        $receipt | Add-Member -NotePropertyName executionProof -NotePropertyValue ([pscustomobject][ordered]@{
            sourceEvidencePath = $sourceRelative; checkId = $CheckId
            lineageSha256 = Get-LlmWikiSha256 ($entry.lineage | ConvertTo-Json -Depth 15 -Compress)
        })
        if (-not (Test-LlmWikiVerificationExecutionProof $receipt $RepositoryRoot)) { throw 'Source execution proof changed before receipt publication.' }
    }
    $root = Get-LlmWikiVerificationReceiptRoot $RepositoryRoot
    $null = New-Item -ItemType Directory -Path $root -Force
    $path = Join-Path $root "$(Get-LlmWikiSha256 $normalizedCommand).json"
    [IO.File]::WriteAllText($path, (($receipt | ConvertTo-Json -Depth 10) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}

$receipts = @(Get-LlmWikiVerificationReceipts $RepositoryRoot)
if ($Format -eq 'Json') { $receipts | ConvertTo-Json -Depth 6; exit 0 }
if ($receipts.Count -eq 0) { Write-Host 'No verification receipts recorded.'; exit 0 }
foreach ($receipt in $receipts) {
    $stateLabel = if ($receipt.validForCurrentState) { 'satisfied' } else { 'stale' }
    Write-Host "[$stateLabel; $($receipt.durationSeconds)s] $($receipt.command)"
    if (@($receipt.coverageScope).Count -gt 0) { Write-Host "  coverage: $(@($receipt.coverageScope) -join ', ')" }
}
