[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('find', 'reuse')]
    [string]$Action = 'find',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [Parameter(Mandatory = $true)]
    [string]$CheckId,
    [string]$SourceWorkspacePath,
    [switch]$DryRun,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
function Normalize-WorkspacePath([string]$Value, [string]$Label) {
    if ([System.IO.Path]::IsPathRooted($Value)) { throw "$Label must be repository-relative." }
    $normalized = $Value.Replace('\', '/').TrimEnd('/')
    if ($normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') {
        throw "$Label must identify one non-hidden workspace directly inside .artifacts/llm-wiki/tasks."
    }
    return $normalized
}
$normalizedWorkspacePath = Normalize-WorkspacePath $WorkspacePath 'WorkspacePath'
$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
if (-not (Test-Path -LiteralPath $absoluteWorkspacePath -PathType Container)) { throw "Target workspace does not exist: $normalizedWorkspacePath" }
if (Test-Path -LiteralPath (Join-Path $absoluteWorkspacePath 'completion.json') -PathType Leaf) { throw 'A sealed target workspace cannot accept cached evidence.' }
$targetEvidencePath = Join-Path $absoluteWorkspacePath 'evidence.json'
$targetEvidence = Get-Content -LiteralPath $targetEvidencePath -Raw | ConvertFrom-Json
$targetEntry = $targetEvidence.checks | Where-Object id -eq $CheckId | Select-Object -First 1
if ($null -eq $targetEntry) { throw "Target workspace does not require check '$CheckId'." }
if ([string]$targetEntry.status -in @('passed', 'not-applicable')) { throw "Target check '$CheckId' is already resolved." }
$expectedLineage = & (Join-Path $PSScriptRoot 'New-LlmWikiEvidenceLineage.ps1') `
    -Kind executed-check `
    -EvidencePath "$normalizedWorkspacePath/evidence.json" `
    -Id $CheckId `
    -Command ([string]$targetEntry.command) `
    -Definition ([string]$targetEntry.command) `
    -Status passed `
    -ExitCode 0 `
    -Format Json | ConvertFrom-Json
$expectedFingerprint = [string]$expectedLineage.compatibilityFingerprint
$normalizedSourceFilter = if (-not [string]::IsNullOrWhiteSpace($SourceWorkspacePath)) {
    Normalize-WorkspacePath $SourceWorkspacePath 'SourceWorkspacePath'
} else { '' }

$tasksRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/tasks'
$candidates = [System.Collections.Generic.List[object]]::new()
foreach ($directory in @(Get-ChildItem -LiteralPath $tasksRoot -Directory -Force -ErrorAction SilentlyContinue | Sort-Object Name)) {
    if ($directory.Name.StartsWith('.', [StringComparison]::Ordinal) -or $directory.FullName -ceq $absoluteWorkspacePath) { continue }
    $sourcePath = ".artifacts/llm-wiki/tasks/$($directory.Name)"
    if ($normalizedSourceFilter -and $sourcePath -cne $normalizedSourceFilter) { continue }
    $completionPath = Join-Path $directory.FullName 'completion.json'
    $sourceEvidencePath = Join-Path $directory.FullName 'evidence.json'
    if (-not (Test-Path -LiteralPath $completionPath -PathType Leaf) -or -not (Test-Path -LiteralPath $sourceEvidencePath -PathType Leaf)) { continue }
    try {
        $seal = & (Join-Path $PSScriptRoot 'Complete-LlmWikiTaskWorkspace.ps1') verify -WorkspacePath $sourcePath -Format Json | ConvertFrom-Json
        if (-not $seal.valid) { continue }
        $doctor = & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') -WorkspacePath $sourcePath -Format Json | ConvertFrom-Json
        if (-not $doctor.valid) { continue }
        $lineageValidation = & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $sourcePath -Format Json | ConvertFrom-Json
        $lineageItem = $lineageValidation.items | Where-Object {
            $_.kind -eq 'check' -and $_.id -eq $CheckId -and $_.cacheReusable -and $_.compatibilityFingerprint -ceq $expectedFingerprint
        } | Select-Object -First 1
        if ($null -eq $lineageItem) { continue }
        $sourceEvidence = Get-Content -LiteralPath $sourceEvidencePath -Raw | ConvertFrom-Json
        $sourceEntry = $sourceEvidence.checks | Where-Object id -eq $CheckId | Select-Object -First 1
        $completion = Get-Content -LiteralPath $completionPath -Raw | ConvertFrom-Json
        $artifactAbsolutePath = Join-Path $repositoryRoot ([string]$sourceEntry.lineage.artifact.path)
        if (-not (Test-Path -LiteralPath $artifactAbsolutePath -PathType Leaf)) { continue }
        $candidates.Add([pscustomobject][ordered]@{
            sourceWorkspace = $sourcePath
            finishedAtUtc = [string]$completion.finishedAtUtc
            completionFingerprint = [string]$completion.completionFingerprint
            compatibilityFingerprint = [string]$sourceEntry.lineage.compatibilityFingerprint
            recordedAtUtc = [string]$sourceEntry.lineage.recordedAtUtc
            durationSeconds = $sourceEntry.durationSeconds
            sourceLogPath = [string]$sourceEntry.lineage.artifact.path
            sourceLogSha256 = [string]$sourceEntry.lineage.artifact.sha256
        })
    } catch {
        continue
    }
}
$orderedCandidates = @($candidates | Sort-Object @{ Expression = 'finishedAtUtc'; Descending = $true }, sourceWorkspace)
$selected = $orderedCandidates | Select-Object -First 1
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    action = $Action
    workspace = $normalizedWorkspacePath
    checkId = $CheckId
    expectedCompatibilityFingerprint = $expectedFingerprint
    candidateCount = $orderedCandidates.Count
    selectedSourceWorkspace = $(if ($null -ne $selected) { $selected.sourceWorkspace } else { '' })
    dryRun = [bool]$DryRun
    reused = $false
    candidates = $orderedCandidates
}
if ($Action -eq 'reuse' -and -not $DryRun) {
    if ($null -eq $selected) { throw "No sealed compatible evidence exists for check '$CheckId'." }
    $sourceAbsolutePath = Join-Path $repositoryRoot ([string]$selected.sourceWorkspace)
    $sourceEvidence = Get-Content -LiteralPath (Join-Path $sourceAbsolutePath 'evidence.json') -Raw | ConvertFrom-Json
    $sourceEntry = $sourceEvidence.checks | Where-Object id -eq $CheckId | Select-Object -First 1
    $sourceLogAbsolutePath = Join-Path $repositoryRoot ([string]$selected.sourceLogPath)
    $logsPath = Join-Path $absoluteWorkspacePath 'logs'
    if (-not (Test-Path -LiteralPath $logsPath)) { New-Item -ItemType Directory -Path $logsPath | Out-Null }
    $safeId = $CheckId -replace '[^A-Za-z0-9_.-]', '_'
    $targetLogRelativePath = "$normalizedWorkspacePath/logs/reused-$safeId-$($expectedFingerprint.Substring(0, 12)).log"
    $targetLogAbsolutePath = Join-Path $repositoryRoot $targetLogRelativePath
    if (Test-Path -LiteralPath $targetLogAbsolutePath) { throw "Target cache log already exists: $targetLogRelativePath" }
    $evidenceRaw = Get-Content -LiteralPath $targetEvidencePath -Raw
    $journalPath = Join-Path $absoluteWorkspacePath 'journal.json'
    $journalRaw = Get-Content -LiteralPath $journalPath -Raw
    $logCopied = $false
    try {
        [System.IO.File]::Copy($sourceLogAbsolutePath, $targetLogAbsolutePath, $false)
        $logCopied = $true
        $lineageCopy = $sourceEntry.lineage | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $lineageCopy.artifact.path = $targetLogRelativePath
        $lineageCopy | Add-Member -NotePropertyName reuse -NotePropertyValue ([pscustomobject][ordered]@{
            reusedAtUtc = [DateTime]::UtcNow.ToString('o')
            sourceWorkspace = [string]$selected.sourceWorkspace
            sourceCompletionFingerprint = [string]$selected.completionFingerprint
            sourceCompatibilityFingerprint = [string]$selected.compatibilityFingerprint
            sourceRecordedAtUtc = [string]$selected.recordedAtUtc
            sourceLogPath = [string]$selected.sourceLogPath
            sourceLogSha256 = [string]$selected.sourceLogSha256
        }) -Force
        $targetEntry.status = 'passed'
        $targetEntry.durationSeconds = $sourceEntry.durationSeconds
        $targetEntry.reason = "Reused sealed compatible evidence from $($selected.sourceWorkspace)."
        $targetEntry | Add-Member -NotePropertyName lineage -NotePropertyValue $lineageCopy -Force
        [System.IO.File]::WriteAllText($targetEvidencePath, (($targetEvidence | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') add `
            -WorkspacePath $normalizedWorkspacePath `
            -JournalType decision `
            -Text "Reused sealed evidence for check '$CheckId' from $($selected.sourceWorkspace)." `
            -Rationale "Compatibility fingerprint: $expectedFingerprint. Source completion: $($selected.completionFingerprint)." | Out-Null
        $validation = & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $normalizedWorkspacePath -Format Json | ConvertFrom-Json
        if (-not $validation.valid) { throw "Reused evidence failed lineage validation: $(@($validation.issues) -join ' ')" }
        $result.reused = $true
    } catch {
        [System.IO.File]::WriteAllText($targetEvidencePath, $evidenceRaw, [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText($journalPath, $journalRaw, [System.Text.UTF8Encoding]::new($false))
        if ($logCopied -and (Test-Path -LiteralPath $targetLogAbsolutePath)) { [System.IO.File]::Delete($targetLogAbsolutePath) }
        throw
    }
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
} else {
    Write-Host "Evidence cache: action=$Action, candidates=$($result.candidateCount), reused=$($result.reused), dryRun=$($result.dryRun)"
    foreach ($candidate in $orderedCandidates) { Write-Host " - $($candidate.sourceWorkspace): $($candidate.compatibilityFingerprint)" }
}
