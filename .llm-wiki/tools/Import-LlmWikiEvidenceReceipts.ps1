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
if (Test-Path -LiteralPath (Join-Path $repositoryRoot "$workspace/completion.json")) { throw 'A sealed workspace cannot import evidence.' }

$evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
$receipts = @(Get-LlmWikiVerificationReceipts $repositoryRoot | Where-Object validForCurrentState)
$byCommand = @{}
foreach ($receipt in $receipts) {
    $key = [string]$receipt.normalizedCommand
    if (-not $byCommand.ContainsKey($key)) { $byCommand[$key] = $receipt }
}
$candidates = [Collections.Generic.List[object]]::new()
foreach ($check in @($evidence.checks | Where-Object status -in @('pending', 'passed', 'failed'))) {
    $key = Normalize-LlmWikiVerificationCommand ([string]$check.command)
    if (-not $byCommand.ContainsKey($key)) { continue }
    $receipt = $byCommand[$key]
    $executed = [int]$receipt.schemaVersion -eq 2
    if (-not $executed -and $check.status -ne 'pending') { continue }
    if ($executed) {
        if ([string]$receipt.executionProof.sourceEvidencePath -ceq "$workspace/evidence.json") { continue }
        $validation = & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') -EvidencePath $receipt.executionProof.sourceEvidencePath -Format Json | ConvertFrom-Json
        if (@($validation.items | Where-Object { $_.kind -eq 'check' -and $_.id -eq $receipt.executionProof.checkId -and $_.cacheReusable }).Count -ne 1) { continue }
        if ($check.status -eq 'passed' -and $check.PSObject.Properties['lineage'] -and $null -ne $check.lineage -and $check.lineage.kind -eq 'executed-check' -and $check.lineage.PSObject.Properties['artifact']) {
            $targetValidation = & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
            if (@($targetValidation.items | Where-Object { $_.kind -eq 'check' -and $_.id -eq $check.id -and $_.cacheReusable }).Count -eq 1) { continue }
        }
    }
    $candidates.Add([pscustomobject][ordered]@{
        checkId = [string]$check.id
        command = [string]$check.command
        durationSeconds = [double]$receipt.durationSeconds
        recordedAtUtc = [string]$receipt.recordedAtUtc
        fingerprint = [string]$receipt.fingerprint
        executionProof = $(if ($executed) { $receipt.executionProof } else { $null })
    })
}

if (-not $DryRun) {
    foreach ($candidate in $candidates) {
        if ($null -ne $candidate.executionProof) {
            $receipt = $byCommand[(Normalize-LlmWikiVerificationCommand $candidate.command)]
            if (-not (Test-LlmWikiVerificationExecutionProof $receipt $repositoryRoot)) { throw 'Source execution proof changed before import.' }
            $source = Get-Content -LiteralPath (Join-Path $repositoryRoot $candidate.executionProof.sourceEvidencePath) -Raw | ConvertFrom-Json
            $sourceEntry = $source.checks | Where-Object id -eq $candidate.executionProof.checkId | Select-Object -First 1
            $logs = Join-Path $repositoryRoot "$workspace/logs"
            $null = New-Item -ItemType Directory -Path $logs -Force
            $safeId = $candidate.checkId -replace '[^A-Za-z0-9_.-]', '_'
            $logRelative = "$workspace/logs/imported-$safeId-$([guid]::NewGuid().ToString('N')).log"
            $logAbsolute = Join-Path $repositoryRoot $logRelative
            try {
                [IO.File]::Copy((Join-Path $repositoryRoot $sourceEntry.lineage.artifact.path), $logAbsolute, $false)
                if ((Get-LlmWikiFileSha256 $logAbsolute) -cne [string]$sourceEntry.lineage.artifact.sha256 -or -not (Test-LlmWikiVerificationExecutionProof $receipt $repositoryRoot)) { throw 'Actual execution log changed during import.' }
                $lineage = & (Join-Path $PSScriptRoot 'New-LlmWikiEvidenceLineage.ps1') -Kind executed-check `
                    -EvidencePath "$workspace/evidence.json" -Id $candidate.checkId -Command $sourceEntry.lineage.execution.command `
                    -Definition $candidate.command -Status passed -ExitCode $sourceEntry.lineage.execution.exitCode `
                    -DurationSeconds $sourceEntry.lineage.execution.durationSeconds -ArtifactPath $logRelative -Format Json | ConvertFrom-Json
                if ((Get-LlmWikiVerificationFingerprint $repositoryRoot).fingerprint -cne $candidate.fingerprint) { throw 'Repository inputs changed while execution evidence was imported.' }
                $lineage.execution | Add-Member -NotePropertyName verification -NotePropertyValue $sourceEntry.lineage.execution.verification
                $lineage | Add-Member -NotePropertyName reuse -NotePropertyValue ([pscustomobject]@{
                    sourceEvidencePath = $candidate.executionProof.sourceEvidencePath
                    sourceCheckId = $candidate.executionProof.checkId
                    sourceLineageSha256 = $candidate.executionProof.lineageSha256
                    sourceLogPath = $sourceEntry.lineage.artifact.path
                    sourceLogSha256 = $sourceEntry.lineage.artifact.sha256
                })
                $target = $evidence.checks | Where-Object id -eq $candidate.checkId | Select-Object -First 1
                $target.status = 'passed'; $target.durationSeconds = $sourceEntry.lineage.execution.durationSeconds
                $target.reason = 'Imported actual current execution proof; command was not rerun.'
                $target | Add-Member -NotePropertyName executionCommand -NotePropertyValue $sourceEntry.lineage.execution.command -Force
                $target | Add-Member -NotePropertyName lineage -NotePropertyValue $lineage -Force
                [IO.File]::WriteAllText($evidencePath, (($evidence | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
            } catch { if (Test-Path -LiteralPath $logAbsolute) { [IO.File]::Delete($logAbsolute) }; throw }
            continue
        }
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvidence.ps1') check `
            -Path "$workspace/evidence.json" `
            -Id $candidate.checkId `
            -Status passed `
            -Command $candidate.command `
            -DurationSeconds $candidate.durationSeconds `
            -Reason "Imported from content-addressed verification receipt recorded at $($candidate.recordedAtUtc)." | Out-Null
        $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
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
