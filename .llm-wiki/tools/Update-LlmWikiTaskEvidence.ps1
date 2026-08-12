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
        ForEach-Object { [string]$_ } |
        Where-Object { -not (Test-GovernanceArtifactPath $_) } |
        Sort-Object -Unique
}
function Test-GovernanceArtifactPath([string]$Path) {
    $normalized = $Path.Replace('\', '/')
    return $normalized -match '^\.llm-wiki/(generated|reviews)/' -or
        $normalized -match '^\.artifacts/llm-wiki/'
}
function Get-PathAffinity([string]$Left, [string]$Right) {
    $leftParts = $Left.Replace('\', '/').Split('/')
    $rightParts = $Right.Replace('\', '/').Split('/')
    $limit = [Math]::Min($leftParts.Count, $rightParts.Count)
    $score = 0
    for ($index = 0; $index -lt $limit; $index++) {
        if ($leftParts[$index] -cne $rightParts[$index]) { break }
        $score++
    }
    return $score
}
function Test-SameSet([object[]]$Left, [object[]]$Right) {
    $leftValues = @($Left | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $rightValues = @($Right | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    return ($leftValues.Count -eq $rightValues.Count -and @(Compare-Object $leftValues $rightValues).Count -eq 0)
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
    if (-not $Entry.PSObject.Properties['lineage'] -or $null -eq $Entry.lineage) { return 'lineage-missing' }
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
function Get-Ids([object[]]$Items) {
    @($Items | ForEach-Object {
        if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id }
    } | Where-Object { $_ } | Sort-Object -Unique)
}
function Get-AvailableScenarios([object]$Packet, [string]$Intent) {
    $items = [System.Collections.Generic.List[object]]::new()
    foreach ($scenario in @($Packet.testPlan.scenarios)) {
        if ($null -eq $scenario -or -not $scenario.PSObject.Properties['id']) { continue }
        $items.Add([pscustomobject][ordered]@{
            id = [string]$scenario.id
            description = [string]$scenario.description
            evidence = [string]$scenario.evidence
        })
    }
    $journeys = & (Join-Path $PSScriptRoot 'Find-LlmWikiProductJourney.ps1') `
        -Query $Intent -ChangedPath @($Packet.diff.changedPaths) -Format Json | ConvertFrom-Json
    foreach ($journey in @($journeys.journeys)) {
        if ($null -eq $journey -or -not $journey.PSObject.Properties['id']) { continue }
        $items.Add([pscustomobject][ordered]@{
            id = [string]$journey.id
            description = [string]$journey.title
            evidence = "Product journey: $(@($journey.scenarios) -join ', ')"
        })
    }
    @($items | Group-Object id | ForEach-Object { $_.Group | Select-Object -First 1 } | Sort-Object id)
}

$history = [System.Collections.Generic.List[object]]::new()
$newCheckIds = @(Get-Ids @($newPacket.policy.requiredChecks))
$newReviewIds = @(Get-Ids @($newPacket.policy.reviewObligations))
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
        $priorLineage = if ($null -ne $oldEntry -and $oldEntry.PSObject.Properties['lineage']) { $oldEntry.lineage } else { $null }
        $history.Add((New-HistoryEntry 'check' ([string]$requirement.id) $priorStatus $reason $priorLineage))
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
foreach ($oldEntry in @($evidence.checks | Where-Object { $_.id -notin $newCheckIds })) {
    $priorLineage = if ($oldEntry.PSObject.Properties['lineage']) { $oldEntry.lineage } else { $null }
    $history.Add((New-HistoryEntry 'check' ([string]$oldEntry.id) ([string]$oldEntry.status) 'requirement-removed' $priorLineage))
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
        $priorLineage = if ($null -ne $oldEntry -and $oldEntry.PSObject.Properties['lineage']) { $oldEntry.lineage } else { $null }
        $history.Add((New-HistoryEntry 'review' ([string]$requirement.id) $priorStatus $reason $priorLineage))
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
foreach ($oldEntry in @($evidence.reviews | Where-Object { $_.id -notin $newReviewIds })) {
    $priorLineage = if ($oldEntry.PSObject.Properties['lineage']) { $oldEntry.lineage } else { $null }
    $history.Add((New-HistoryEntry 'review' ([string]$oldEntry.id) ([string]$oldEntry.status) 'requirement-removed' $priorLineage))
    $invalidatedReviewIds.Add([string]$oldEntry.id)
}

$oldAvailableScenarios = @(Get-AvailableScenarios $oldPacket ([string]$acceptance.objective))
$newAvailableScenarios = @(Get-AvailableScenarios $newPacket ([string]$acceptance.objective))
$oldScenarios = @{}
foreach ($item in $oldAvailableScenarios) { $oldScenarios[[string]$item.id] = "$($item.description)|$($item.evidence)" }
$newScenarios = @{}
foreach ($item in $newAvailableScenarios) { $newScenarios[[string]$item.id] = "$($item.description)|$($item.evidence)" }
$changedScenarioIds = @($oldScenarios.Keys + $newScenarios.Keys | Sort-Object -Unique | Where-Object {
    -not $oldScenarios.ContainsKey($_) -or -not $newScenarios.ContainsKey($_) -or $oldScenarios[$_] -cne $newScenarios[$_]
})
$newTestPaths = @($newPacket.testPlan.focusedTestFiles)
$invalidatedCriterionIds = [System.Collections.Generic.List[string]]::new()
$retainedCriterionIds = [System.Collections.Generic.List[string]]::new()
$autoLinkedPaths = @{}
$newProductPaths = @($newPacket.diff.changedPaths | Where-Object {
    $_ -notin @($oldPacket.diff.changedPaths) -and -not (Test-GovernanceArtifactPath $_)
})
foreach ($newPath in $newProductPaths) {
    $ranked = foreach ($candidate in @($acceptance.criteria)) {
        $scores = @($candidate.mapping.changedPaths | ForEach-Object { Get-PathAffinity $_ $newPath })
        $best = if ($scores.Count -gt 0) { [int](($scores | Measure-Object -Maximum).Maximum) } else { 0 }
        if ($best -ge 2) { [pscustomobject]@{ id = [string]$candidate.id; score = $best } }
    }
    $ranked = @($ranked)
    $highest = if ($ranked.Count -gt 0) { [int](($ranked | Measure-Object -Property score -Maximum).Maximum) } else { 0 }
    $winners = @($ranked | Where-Object score -eq $highest)
    if ($highest -ge 2 -and $winners.Count -eq 1) {
        $autoLinkedPaths[$newPath] = [string]$winners[0].id
    }
}
foreach ($criterion in @($acceptance.criteria)) {
    $mappedChangedPaths = if ($null -ne $criterion.mapping.PSObject.Properties['changedPaths']) { @($criterion.mapping.changedPaths) } else { @() }
    $mappedChangedPaths += @($autoLinkedPaths.GetEnumerator() | Where-Object Value -eq $criterion.id | Select-Object -ExpandProperty Key)
    $mappedChangedPaths = @($mappedChangedPaths | Sort-Object -Unique)
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
    $retainedChangedPaths = @($mappedChangedPaths | Where-Object { $_ -in @($newPacket.diff.changedPaths) })
    $retainedChecks = @($mappedChecks | Where-Object { $_ -in $newCheckIds -and $_ -notin $invalidatedCheckIds })
    $retainedReviews = @($mappedReviews | Where-Object { $_ -in $newReviewIds -and $_ -notin $invalidatedReviewIds })
    $retainedScenarios = @($mappedScenarios | Where-Object { $newScenarios.ContainsKey([string]$_) -and $_ -notin $changedScenarioIds })
    $retainedTests = @($mappedTests | Where-Object { $_ -in $newTestPaths })
    $hasDirectEvidence = $retainedChangedPaths.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace([string]$criterion.resolution.evidenceNote)
    $hasRetainedEvidence = $hasDirectEvidence -or
        $retainedChecks.Count + $retainedReviews.Count + $retainedScenarios.Count + $retainedTests.Count -gt 0
    $evidenceInvalidated = $mappingChanged -and -not $hasRetainedEvidence
    if ($evidenceInvalidated -or $unanchoredResolution) {
        $history.Add((New-HistoryEntry 'acceptance' ([string]$criterion.id) ([string]$criterion.status) $(if ($mappingChanged) { 'mapped-evidence-invalidated' } else { 'unanchored-packet-changed' }) $null))
        $invalidatedCriterionIds.Add([string]$criterion.id)
        $criterion.status = 'pending'
        $criterion.resolution.reason = $null
        $criterion.resolution.evidenceNote = $null
    } elseif ([string]$criterion.status -in @('satisfied', 'not-applicable')) {
        $retainedCriterionIds.Add([string]$criterion.id)
    }
    $criterion.mapping.checkIds = $retainedChecks
    $criterion.mapping.reviewIds = $retainedReviews
    $criterion.mapping.scenarioIds = $retainedScenarios
    $criterion.mapping.testPaths = $retainedTests
    $criterion.mapping | Add-Member -NotePropertyName changedPaths -NotePropertyValue $retainedChangedPaths -Force
}

$evidence.checks = @($newChecks)
$evidence.reviews = @($newReviews)
$evidence.structuralViolations = @($newPacket.policy.violations)
$evidence.git.head = [string]$newPacket.inputs.gitHead
$evidence.git.comparedHead = $newPacket.inputs.headRef
$evidence.change.changedPaths = @($newPacket.diff.changedPaths)
$evidence.change.scopes = @($newPacket.diff.scopes)
$evidence.change.modules = @($newPacket.diff.modules | ForEach-Object {
    if ($_ -is [string]) { [string]$_ } elseif ($null -ne $_ -and $_.PSObject.Properties['name']) { [string]$_.name }
} | Where-Object { $_ } | Sort-Object -Unique)
$priorHistory = if ($evidence.PSObject.Properties['invalidationHistory']) { @($evidence.invalidationHistory | Where-Object { $null -ne $_ }) } else { @() }
$combinedHistory = @($priorHistory) + @($history)
$evidence | Add-Member -NotePropertyName invalidationHistory -NotePropertyValue $combinedHistory -Force
$acceptance.availableEvidence.scenarios = $newAvailableScenarios
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
    retainedCriteria = @($retainedCriterionIds | Sort-Object -Unique)
    retainedChecks = @($newChecks | Where-Object { $_.id -notin $invalidatedCheckIds } | Select-Object -ExpandProperty id)
    retainedReviews = @($newReviews | Where-Object { $_.id -notin $invalidatedReviewIds } | Select-Object -ExpandProperty id)
    autoLinkedPaths = @($autoLinkedPaths.GetEnumerator() | Sort-Object Key | ForEach-Object {
        [pscustomobject][ordered]@{ path = [string]$_.Key; criterionId = [string]$_.Value; reason = 'unique-path-affinity' }
    })
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
    if (@($result.autoLinkedPaths).Count -gt 0) {
        Write-Host "Auto-linked $(@($result.autoLinkedPaths).Count) new production path(s) to unambiguous acceptance criteria."
    }
}
