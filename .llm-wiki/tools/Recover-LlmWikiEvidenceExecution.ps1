[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$EvidencePath,
    [Parameter(Mandatory)][string]$Id,
    [Parameter(Mandatory)][string]$ExecutionRecordPath,
    [ValidateSet('Text', 'Json')][string]$Format = 'Text'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LlmWikiEvidenceCommands.ps1')
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$comparison = if ([IO.Path]::DirectorySeparatorChar -eq '\') { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
function Resolve-InputPath([string]$Path) {
    [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($Path)) { $Path } else { Join-Path $repositoryRoot $Path }))
}
$evidenceFile = Resolve-InputPath $EvidencePath
$recordFile = Resolve-InputPath $ExecutionRecordPath
$repositoryPrefix = $repositoryRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
if (-not $evidenceFile.StartsWith($repositoryPrefix, $comparison)) { throw 'Evidence must be inside this repository.' }
$logPrefix = (Join-Path (Split-Path -Parent $evidenceFile) 'logs') + [IO.Path]::DirectorySeparatorChar
if (-not $recordFile.StartsWith($logPrefix, $comparison) -or -not $recordFile.EndsWith('.log.execution.json', [StringComparison]::Ordinal)) {
    throw 'Recovery requires an original execution record inside this evidence bundle logs directory.'
}
if (-not [IO.File]::Exists($evidenceFile)) { throw "Evidence bundle does not exist: $EvidencePath." }
try { $executionLease = [IO.File]::Open($evidenceFile + '.execution.lock', [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
catch { throw "Cannot acquire evidence execution lease: $($_.Exception.Message) A live execution or recovery may own it; inspect before retrying." }
try {
$original = [IO.File]::ReadAllText($evidenceFile)
$evidence = $original | ConvertFrom-Json
$record = [IO.File]::ReadAllText($recordFile) | ConvertFrom-Json
foreach ($field in @('command', 'checkout', 'startedAtUtc', 'completedAtUtc', 'exitCode', 'durationSeconds', 'fingerprintBefore', 'fingerprintAfter', 'logPath', 'logSha256')) {
    if (-not $record.PSObject.Properties[$field] -or $null -eq $record.$field) { throw "Incomplete execution record: $field." }
}
$logFile = Resolve-InputPath ([string]$record.logPath)
if (-not $recordFile.Equals($logFile + '.execution.json', $comparison) -or
    -not (Resolve-InputPath ([string]$record.checkout)).Equals($repositoryRoot, $comparison)) { throw 'Execution record belongs to another log or checkout.' }
if ((Get-FileHash -LiteralPath $logFile -Algorithm SHA256).Hash.ToLowerInvariant() -cne [string]$record.logSha256) { throw 'Execution log hash changed.' }
if ([DateTimeOffset]$record.completedAtUtc -lt [DateTimeOffset]$record.startedAtUtc -or [double]$record.durationSeconds -lt 0) { throw 'Execution record has invalid timing.' }
$fingerprint = [string](Get-LlmWikiVerificationFingerprint $repositoryRoot).fingerprint
if ([string]$record.fingerprintBefore -cne $fingerprint -or [string]$record.fingerprintAfter -cne $fingerprint) { throw 'Execution record does not match current repository inputs.' }
$policy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') -BaseRef ([string]$evidence.git.base) -ChangedPath @($evidence.change.changedPaths) -Format Json | ConvertFrom-Json
$requirement = @($policy.requiredChecks | Where-Object id -eq $Id)
$entry = @($evidence.checks | Where-Object id -eq $Id)
if ($requirement.Count -ne 1 -or $entry.Count -ne 1 -or [string]$record.command -cne [string]$requirement[0].command) { throw 'Execution record must match exactly one current canonical required check.' }
$status = if ([int]$record.exitCode -eq 0) { 'passed' } else { 'failed' }
$lineage = & (Join-Path $PSScriptRoot 'New-LlmWikiEvidenceLineage.ps1') -Kind executed-check -EvidencePath $EvidencePath -Id $Id -Command ([string]$record.command) -Status $status -ExitCode ([int]$record.exitCode) -DurationSeconds ([double]$record.durationSeconds) -ArtifactPath $logFile -Format Json | ConvertFrom-Json
$lineage.execution | Add-Member -NotePropertyName verification -NotePropertyValue ([pscustomobject][ordered]@{
    fingerprint = $fingerprint
    recordPath = $recordFile.Substring($repositoryPrefix.Length).Replace('\', '/')
    recordSha256 = (Get-FileHash -LiteralPath $recordFile -Algorithm SHA256).Hash.ToLowerInvariant()
})
$entry[0].status = $status
$entry[0].command = [string]$record.command
$entry[0].durationSeconds = [double]$record.durationSeconds
$entry[0].reason = "Recovered original execution; command was not rerun (exit=$($record.exitCode))."
$entry[0] | Add-Member -NotePropertyName executionCommand -NotePropertyValue ([string]$record.command) -Force
$entry[0] | Add-Member -NotePropertyName lineage -NotePropertyValue $lineage -Force
function Write-EvidenceAtomically([string]$Text) {
    $temporary = $evidenceFile + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
    try {
        [IO.File]::WriteAllText($temporary, $Text, [Text.UTF8Encoding]::new($false))
        [IO.File]::Move($temporary, $evidenceFile, $true)
    } finally { if (Test-Path -LiteralPath $temporary) { [IO.File]::Delete($temporary) } }
}
if ((Get-LlmWikiVerificationFingerprint $repositoryRoot).fingerprint -cne $fingerprint -or [IO.File]::ReadAllText($evidenceFile) -cne $original) { throw 'Repository or evidence changed during recovery.' }
try {
    Write-EvidenceAtomically (($evidence | ConvertTo-Json -Depth 30) + [Environment]::NewLine)
    if ($status -eq 'passed') {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationReceipts.ps1') -Action Record -RepositoryRoot $repositoryRoot -Command ([string]$record.command) -SourceEvidencePath $EvidencePath -CheckId $Id -ExpectedFingerprint $fingerprint -Format Json | Out-Null
    }
    if ((Get-LlmWikiVerificationFingerprint $repositoryRoot).fingerprint -cne $fingerprint) { throw 'Repository changed before recovery publication completed.' }
} catch {
    Write-EvidenceAtomically $original
    throw
}
$result = [pscustomobject][ordered]@{ id = $Id; status = $status; commandExitCode = [int]$record.exitCode; commandRerun = $false; executionRecordPath = $ExecutionRecordPath; fingerprint = $fingerprint }
if ($Format -eq 'Json') { $result | ConvertTo-Json } else { Write-Host "Recovered '$Id': $status, original command exit=$($record.exitCode), command was not rerun." }
} finally { $executionLease.Dispose() }
