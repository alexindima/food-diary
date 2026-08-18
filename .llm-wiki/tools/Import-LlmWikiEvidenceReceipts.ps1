[CmdletBinding()]
param(
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [switch]$DryRun,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiVerificationReceipts.ps1')

$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$evidencePath = Join-Path $repositoryRoot "$workspace/evidence.json"
if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) { throw "Evidence is absent: $workspace/evidence.json" }

$evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
$receipts = @(Get-LlmWikiVerificationReceipts $repositoryRoot | Where-Object validForCurrentState)
$byCommand = @{}
foreach ($receipt in $receipts) {
    $key = [string]$receipt.normalizedCommand
    if (-not $byCommand.ContainsKey($key)) { $byCommand[$key] = $receipt }
}
$candidates = [Collections.Generic.List[object]]::new()
foreach ($check in @($evidence.checks | Where-Object status -eq 'pending')) {
    $key = Normalize-LlmWikiVerificationCommand ([string]$check.command)
    if (-not $byCommand.ContainsKey($key)) { continue }
    $receipt = $byCommand[$key]
    $candidates.Add([pscustomobject][ordered]@{
        checkId = [string]$check.id
        command = [string]$check.command
        durationSeconds = [double]$receipt.durationSeconds
        recordedAtUtc = [string]$receipt.recordedAtUtc
        fingerprint = [string]$receipt.fingerprint
    })
}

if (-not $DryRun) {
    foreach ($candidate in $candidates) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvidence.ps1') check `
            -Path "$workspace/evidence.json" `
            -Id $candidate.checkId `
            -Status passed `
            -Command $candidate.command `
            -DurationSeconds $candidate.durationSeconds `
            -Reason "Imported from content-addressed verification receipt recorded at $($candidate.recordedAtUtc)." | Out-Null
    }
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    workspace = $workspace
    applied = -not $DryRun
    availableReceiptCount = $receipts.Count
    pendingCheckCount = @($evidence.checks | Where-Object status -eq 'pending').Count
    importedCount = $candidates.Count
    importedCheckIds = @($candidates | ForEach-Object checkId)
    imports = @($candidates)
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 8 } else {
    Write-Host "Evidence receipt import: applied=$($result.applied), imported=$($result.importedCount)/$($result.pendingCheckCount), current receipts=$($result.availableReceiptCount)"
    foreach ($candidate in $candidates) { Write-Host " - $($candidate.checkId): $($candidate.command) ($($candidate.durationSeconds)s)" }
}
