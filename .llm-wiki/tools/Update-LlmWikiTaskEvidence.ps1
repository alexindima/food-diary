[CmdletBinding()]
param(
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [Parameter(Mandatory = $true)]
    [string]$PacketPath,
    [switch]$Apply,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
if ([System.IO.Path]::IsPathRooted($WorkspacePath) -or [System.IO.Path]::IsPathRooted($PacketPath)) {
    throw 'WorkspacePath and PacketPath must be repository-relative.'
}
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
$normalizedPacketPath = $PacketPath.Replace('\', '/')
if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
if ($normalizedPacketPath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+/\.refresh-[^/]+-packet\.json$') {
    throw 'PacketPath must identify a refresh packet inside the same task workspace.'
}
if (-not $normalizedPacketPath.StartsWith("$normalizedWorkspacePath/", [StringComparison]::Ordinal)) {
    throw 'PacketPath must belong to WorkspacePath.'
}
$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
$oldPacketPath = Join-Path $absoluteWorkspacePath 'change-packet.json'
$evidencePath = Join-Path $absoluteWorkspacePath 'evidence.json'
$acceptancePath = Join-Path $absoluteWorkspacePath 'acceptance-matrix.json'
$absolutePacketPath = Join-Path $repositoryRoot $normalizedPacketPath
foreach ($path in @($oldPacketPath, $evidencePath, $acceptancePath, $absolutePacketPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required invalidation input is absent: $path" }
}
$oldPacket = Get-Content -LiteralPath $oldPacketPath -Raw | ConvertFrom-Json
$newPacket = Get-Content -LiteralPath $absolutePacketPath -Raw | ConvertFrom-Json
$evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
$acceptance = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
$now = [DateTime]::UtcNow.ToString('o')

function Get-RulePaths([object]$Packet, [string]$RuleId) {
    @($Packet.policy.matchedRules | Where-Object id -eq $RuleId | Select-Object -First 1).matchedPaths |
        ForEach-Object { [string]$_ } | Sort-Object -Unique
}
function Test-SameSet([object[]]$Left, [object[]]$Right) {
    $leftValues = @($Left | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $rightValues = @($Right | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    return ($leftValues.Count -eq $rightValues.Count -and (Compare-Object $leftValues $rightValues).Count -eq 0)
}
function Get-ChangeReason([object]$OldRequirement, [object]$NewRequirement, [string]$ValueProperty) {
    if ($null -eq $OldRequirement) { return 'new-requirement' }
    if ([string]$OldRequirement.($ValueProperty) -cne [string]$NewRequirement.($ValueProperty)) { return 'definition-changed' }
    if ([string]$OldRequirement.sourceRule -cne [string]$NewRequirement.sourceRule) { return 'source-rule-changed' }
    $oldPaths = Get-RulePaths $oldPacket ([string]$OldRequirement.sourceRule)
    $newPaths = Get-RulePaths $newPacket ([string]$NewRequirement.sourceRule)
    if (-not (Test-SameSet $oldPaths $newPaths)) { return 'matched-paths-changed' }
    return $null
}
function Get-LineageChangeReason([object]$Entry, [object]$Requirement) {
    if ($null -eq $Entry -or [string]$Entry.status -notin @('passed', 'failed', 'completed', 'not-applicable')) { return $null }
    if ($null -eq $Entry.lineage) { return 'lineage-missing' }
    $paths = Get-RulePaths $newPacket ([string]$Requirement.sourceRule)
    $content = & (Join-Path $PSScriptRoot 'Get-LlmWikiContentFingerprint.ps1') -Path $paths -Format Json | ConvertFrom-Json
    if ([string]$Entry.lineage.dependencies.contentFingerprint -cne [string]$content.fingerprint) { return 'dependency-content-changed' }
    $policyHash = (Get-FileHash -LiteralPath (Join-Path $wikiRoot 'policies/change-policies.json') -Algorithm SHA256).Hash.ToLowerInvariant()
    if ([string]$Entry.lineage.policyFingerprint -cne $policyHash) { return 'change-policy-changed' }
    $currentRuntimeVersion = switch ([string]$Entry.lineage.execution.runtime) {
        'dotnet' { [string](& dotnet --version) }
        'npm' { [string](& npm --version) }
        default { [string]$PSVersionTable.PSVersion }
    }
    if ([string]$Entry.lineage.execution.runtimeVersion -cne $currentRuntimeVersion) { return 'runtime-version-changed' }
    $currentOs = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription.Trim()
    $currentArchitecture = [string][System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    if ([string]$Entry.lineage.environment.os -cne $currentOs -or
        [string]$Entry.lineage.environment.architecture -cne $currentArchitecture) {
        return 'execution-platform-changed'
    }
    return $null
}
function New-HistoryEntry([string]$Kind, [string]$Id, [string]$PriorStatus, [string]$Reason, [object]$PriorLineage) {
    [pscustomobject][ordered]@{
        invalidatedAtUtc = $now
        oldPacketFingerprint = [string]$oldPacket.fingerprint
        newPacketFingerprint = [string]$newPacket.fingerprint
        kind = $Kind
        id = $Id
        priorStatus = $PriorStatus
        priorCompatibilityFingerprint = $(if ($null -ne $PriorLineage) { [string]$PriorLineage.compatibilityFingerprint } else { '' })
        priorRecordedAtUtc = $(if ($null -ne $PriorLineage) { [string]$PriorLineage.recordedAtUtc } else { '' })
        reason = $Reason
    }
}

$history = [System.Collections.Generic.List[object]]::new()
$invalidatedCheckIds = [System.Collections.Generic.List[string]]::new()
$invalidatedReviewIds = [System.Collections.Generic.List[string]]::new()
$newChecks = [System.Collections.Generic.List[object]]::new()
foreach ($requirement in @($newPacket.policy.requiredChecks)) {
    $oldRequirement = $oldPacket.policy.requiredChecks | Where-Object id -eq $requirement.id | Select-Object -First 1
    $oldEntry = $evidence.checks | Where-Object id -eq $requirement.id | Select-Object -First 1
    $reason = Get-ChangeReason $oldRequirement $requirement 'command'
    if ($null -eq $oldEntry) { $reason = 'new-requirement' }
    if ($null -eq $reason) { $reason = Get-LineageChangeReason $oldEntry $requirement }
    if ($null -ne $reason) {
        $priorStatus = if ($null -ne $oldEntry) { [string]$oldEntry.status } else { 'absent' }
        $history.Add((New-HistoryEntry 'check' ([string]$requirement.id) $priorStatus $reason $oldEntry.lineage))
        $invalidatedCheckIds.Add([string]$requirement.id)
        $newChecks.Add([pscustomobject][ordered]@{
            id = [string]$requirement.id
            status = 'pending'
            command = [string]$requirement.command
            durationSeconds = $null
            reason = "Invalidated by task refresh: $reason."
        })
    } else {
        $oldEntry.command = [string]$requirement.command
        $newChecks.Add($oldEntry)
    }
}
foreach ($oldEntry in @($evidence.checks | Where-Object { $_.id -notin @($newPacket.policy.requiredChecks.id) })) {
    $history.Add((New-HistoryEntry 'check' ([string]$oldEntry.id) ([string]$oldEntry.status) 'requirement-removed' $oldEntry.lineage))
    $invalidatedCheckIds.Add([string]$oldEntry.id)
}

$newReviews = [System.Collections.Generic.List[object]]::new()
foreach ($requirement in @($newPacket.policy.reviewObligations)) {
    $oldRequirement = $oldPacket.policy.reviewObligations | Where-Object id -eq $requirement.id | Select-Object -First 1
    $oldEntry = $evidence.reviews | Where-Object id -eq $requirement.id | Select-Object -First 1
    $reason = Get-ChangeReason $oldRequirement $requirement 'description'
    if ($null -eq $oldEntry) { $reason = 'new-requirement' }
    if ($null -eq $reason) { $reason = Get-LineageChangeReason $oldEntry $requirement }
    if ($null -ne $reason) {
        $priorStatus = if ($null -ne $oldEntry) { [string]$oldEntry.status } else { 'absent' }
        $history.Add((New-HistoryEntry 'review' ([string]$requirement.id) $priorStatus $reason $oldEntry.lineage))
        $invalidatedReviewIds.Add([string]$requirement.id)
        $newReviews.Add([pscustomobject][ordered]@{
            id = [string]$requirement.id
            status = 'pending'
            description = [string]$requirement.description
            reason = "Invalidated by task refresh: $reason."
        })
    } else {
        $oldEntry.description = [string]$requirement.description
        $newReviews.Add($oldEntry)
    }
}
foreach ($oldEntry in @($evidence.reviews | Where-Object { $_.id -notin @($newPacket.policy.reviewObligations.id) })) {
    $history.Add((New-HistoryEntry 'review' ([string]$oldEntry.id) ([string]$oldEntry.status) 'requirement-removed' $oldEntry.lineage))
    $invalidatedReviewIds.Add([string]$oldEntry.id)
}

$oldScenarios = @{}
foreach ($item in @($oldPacket.testPlan.scenarios)) { $oldScenarios[[string]$item.id] = "$($item.description)|$($item.evidence)" }
$newScenarios = @{}
foreach ($item in @($newPacket.testPlan.scenarios)) { $newScenarios[[string]$item.id] = "$($item.description)|$($item.evidence)" }
$changedScenarioIds = @($oldScenarios.Keys + $newScenarios.Keys | Sort-Object -Unique | Where-Object {
    -not $oldScenarios.ContainsKey($_) -or -not $newScenarios.ContainsKey($_) -or $oldScenarios[$_] -cne $newScenarios[$_]
})
$newTestPaths = @($newPacket.testPlan.focusedTestFiles)
$invalidatedCriterionIds = [System.Collections.Generic.List[string]]::new()
foreach ($criterion in @($acceptance.criteria)) {
    $mappedChangedPaths = if ($null -ne $criterion.mapping.PSObject.Properties['changedPaths']) { @($criterion.mapping.changedPaths) } else { @() }
    $mappedChecks = @($criterion.mapping.checkIds)
    $mappedReviews = @($criterion.mapping.reviewIds)
    $mappedScenarios = @($criterion.mapping.scenarioIds)
    $mappedTests = @($criterion.mapping.testPaths)
    $mappingChanged = @($mappedChecks | Where-Object { $_ -in $invalidatedCheckIds }).Count -gt 0 -or
        @($mappedReviews | Where-Object { $_ -in $invalidatedReviewIds }).Count -gt 0 -or
        @($mappedScenarios | Where-Object { $_ -in $changedScenarioIds }).Count -gt 0 -or
        @($mappedTests | Where-Object { $_ -notin $newTestPaths }).Count -gt 0 -or
        @($mappedChangedPaths | Where-Object { $_ -notin @($newPacket.diff.changedPaths) }).Count -gt 0
    $hasMapping = $mappedChangedPaths.Count + $mappedChecks.Count + $mappedReviews.Count + $mappedScenarios.Count + $mappedTests.Count -gt 0
    $unanchoredResolution = -not $hasMapping -and [string]$criterion.status -ne 'pending' -and
        [string]$oldPacket.fingerprint -cne [string]$newPacket.fingerprint
    if ($mappingChanged -or $unanchoredResolution) {
        $history.Add((New-HistoryEntry 'acceptance' ([string]$criterion.id) ([string]$criterion.status) $(if ($mappingChanged) { 'mapped-evidence-invalidated' } else { 'unanchored-packet-changed' }) $null))
        $invalidatedCriterionIds.Add([string]$criterion.id)
        $criterion.status = 'pending'
        $criterion.resolution.reason = $null
        $criterion.resolution.evidenceNote = $null
    }
    $criterion.mapping.checkIds = @($mappedChecks | Where-Object { $_ -in @($newPacket.policy.requiredChecks.id) })
    $criterion.mapping.reviewIds = @($mappedReviews | Where-Object { $_ -in @($newPacket.policy.reviewObligations.id) })
    $criterion.mapping.scenarioIds = @($mappedScenarios | Where-Object { $newScenarios.ContainsKey([string]$_) })
    $criterion.mapping.testPaths = @($mappedTests | Where-Object { $_ -in $newTestPaths })
    $criterion.mapping | Add-Member -NotePropertyName changedPaths -NotePropertyValue @($mappedChangedPaths | Where-Object { $_ -in @($newPacket.diff.changedPaths) }) -Force
}

$evidence.checks = @($newChecks)
$evidence.reviews = @($newReviews)
$evidence.structuralViolations = @($newPacket.policy.violations)
$evidence.git.head = [string]$newPacket.inputs.gitHead
$evidence.git.comparedHead = $newPacket.inputs.headRef
$evidence.change.changedPaths = @($newPacket.diff.changedPaths)
$evidence.change.scopes = @($newPacket.diff.scopes)
$evidence.change.modules = @($newPacket.diff.modules.name)
$priorHistory = @($evidence.invalidationHistory | Where-Object { $null -ne $_ })
$combinedHistory = @($priorHistory) + @($history)
$evidence | Add-Member -NotePropertyName invalidationHistory -NotePropertyValue $combinedHistory -Force
$acceptance.availableEvidence.scenarios = @($newPacket.testPlan.scenarios | ForEach-Object {
    [pscustomobject][ordered]@{ id = $_.id; description = $_.description; evidence = $_.evidence }
})
$acceptance.availableEvidence | Add-Member -NotePropertyName changedPaths -NotePropertyValue @($newPacket.diff.changedPaths) -Force
$acceptance.availableEvidence.checks = @($newPacket.policy.requiredChecks | ForEach-Object {
    [pscustomobject][ordered]@{ id = $_.id; command = $_.command }
})
$acceptance.availableEvidence.reviews = @($newPacket.policy.reviewObligations | ForEach-Object {
    [pscustomobject][ordered]@{ id = $_.id; description = $_.description }
})
$acceptance.availableEvidence.testPaths = @($newPacket.testPlan.focusedTestFiles)

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    workspace = $normalizedWorkspacePath
    applied = [bool]$Apply
    packetChanged = [string]$oldPacket.fingerprint -cne [string]$newPacket.fingerprint
    oldPacketFingerprint = [string]$oldPacket.fingerprint
    newPacketFingerprint = [string]$newPacket.fingerprint
    invalidatedChecks = @($invalidatedCheckIds | Sort-Object -Unique)
    invalidatedReviews = @($invalidatedReviewIds | Sort-Object -Unique)
    invalidatedCriteria = @($invalidatedCriterionIds | Sort-Object -Unique)
    retainedChecks = @($newChecks | Where-Object { $_.id -notin $invalidatedCheckIds } | Select-Object -ExpandProperty id)
    retainedReviews = @($newReviews | Where-Object { $_.id -notin $invalidatedReviewIds } | Select-Object -ExpandProperty id)
    historyEntriesAdded = $history.Count
}
if ($Apply) {
    $evidenceRaw = Get-Content -LiteralPath $evidencePath -Raw
    $acceptanceRaw = Get-Content -LiteralPath $acceptancePath -Raw
    try {
        [System.IO.File]::WriteAllText($evidencePath, (($evidence | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText($acceptancePath, (($acceptance | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
    } catch {
        [System.IO.File]::WriteAllText($evidencePath, $evidenceRaw, [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText($acceptancePath, $acceptanceRaw, [System.Text.UTF8Encoding]::new($false))
        throw
    }
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 8
} else {
    Write-Host "Evidence invalidation: applied=$($result.applied), checks=$(@($result.invalidatedChecks).Count), reviews=$(@($result.invalidatedReviews).Count), criteria=$(@($result.invalidatedCriteria).Count)"
    Write-Host "Retained: $(@($result.retainedChecks).Count) checks, $(@($result.retainedReviews).Count) reviews."
}
