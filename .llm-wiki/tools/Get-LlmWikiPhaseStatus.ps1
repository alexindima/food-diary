[CmdletBinding()]
param(
    [ValidateSet('status', 'next', 'complete')]
    [string]$Action = 'status',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$PhaseId,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [switch]$FailOnInvalid
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiRequirementCriteria.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$workspace = Join-Path $repositoryRoot $WorkspacePath
function Read-PhaseArtifact([string]$Name) {
    $path = Join-Path $workspace $Name
    if (Test-Path -LiteralPath $path -PathType Leaf) { Get-Content -LiteralPath $path -Raw | ConvertFrom-Json }
}
function Get-PhaseValues([object]$Value, [string]$Name) {
    if ($null -ne $Value -and $Value.PSObject.Properties[$Name]) { @($Value.$Name | Where-Object { $null -ne $_ }) }
}
$manifest = Read-PhaseArtifact 'change-manifest.json'
if ($null -eq $manifest) { throw "Workspace manifest is absent: $WorkspacePath/change-manifest.json" }
$acceptance = Read-PhaseArtifact 'acceptance-matrix.json'
$evidence = Read-PhaseArtifact 'evidence.json'
$lineage = if ($null -ne $evidence) {
    & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') -EvidencePath "$($WorkspacePath.TrimEnd('/', '\'))/evidence.json" -Format Json | ConvertFrom-Json
} else { [pscustomobject]@{ items = @() } }
$changed = @(
    @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('diff', '--name-only', $manifest.git.base, 'HEAD', '--')) +
    @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('diff', '--name-only', 'HEAD', '--')) +
    @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard'))
) | Sort-Object -Unique
# Phase candidates can include unchanged downstream files owned by another task.
# Require acceptance links only for observed changes in this task's current scope.
$packet = Read-PhaseArtifact 'change-packet.json'
$taskChange = if ($null -ne $packet -and $packet.PSObject.Properties['diff']) { $packet.diff } elseif ($null -ne $evidence -and $evidence.PSObject.Properties['change']) { $evidence.change } else { $null }
if ($null -ne $taskChange -and $taskChange.PSObject.Properties['changedPaths']) {
    $taskChangedPaths = @(Get-PhaseValues $taskChange 'changedPaths' | ForEach-Object { ([string]$_).Replace('\', '/') })
    $changed = @($changed | Where-Object { $_ -in $taskChangedPaths })
}
$checks = @(@(Get-PhaseValues $evidence 'checks') + @(Get-PhaseValues $evidence 'supplementalChecks'))
$reviews = @(Get-PhaseValues $evidence 'reviews')
$executedIds = @($checks | Where-Object {
    $entry = $_
    $receipt = @($lineage.items | Where-Object { $_.kind -in @('check', 'supplemental') -and $_.id -eq $entry.id -and $_.valid })
    $receipt.Count -gt 0 -and $entry.status -eq 'passed' -and $entry.lineage.kind -eq 'executed-check' -and
        $null -ne $entry.lineage.execution.exitCode -and [int]$entry.lineage.execution.exitCode -eq 0 -and
        $entry.lineage.PSObject.Properties['artifact'] -and $null -ne $entry.lineage.artifact
} | ForEach-Object id)
$phases = @($manifest.plan.phases | Sort-Object order | ForEach-Object {
    $phase = $_
    $phaseKey = [string]$phase.id
    $files = @(Get-PhaseValues $phase 'files' | ForEach-Object { ([string]$_).Replace('\', '/') })
    $present = @($files | Where-Object { $_ -in $changed })
    $missing = @(if ($phaseKey -notin @('generated-artifacts', 'release-readiness')) { $files | Where-Object { -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $_)) } })
    $issues = [Collections.Generic.List[string]]::new()
    $allAcceptance = $phaseKey -in @('focused-verification', 'generated-artifacts', 'release-readiness')
    $explicitIds = @(Get-PhaseValues $phase 'criterionIds')
    $criteria = @(Get-PhaseValues $acceptance 'criteria' | Where-Object {
        $mappedPaths = @(@(Get-PhaseValues $_.mapping 'changedPaths') + @(Get-PhaseValues $_.mapping 'testPaths'))
        $allAcceptance -or $_.id -in $explicitIds -or @($mappedPaths | Where-Object { $_ -in $files }).Count -gt 0
    })
    foreach ($id in $explicitIds) {
        if ($id -notin @($criteria | ForEach-Object id)) { $issues.Add("Unknown phase criterion '$id'.") }
    }
    $checkIds = @(@(Get-PhaseValues $phase 'checkIds') + @($criteria | ForEach-Object { Get-PhaseValues $_.mapping 'checkIds' }) +
        @(if ($allAcceptance) { Get-PhaseValues $manifest.plan 'requiredChecks' | ForEach-Object id }) | Sort-Object -Unique)
    $reviewIds = @(@(Get-PhaseValues $phase 'reviewIds') + @($criteria | ForEach-Object { Get-PhaseValues $_.mapping 'reviewIds' }) +
        @(if ($phaseKey -eq 'release-readiness') { Get-PhaseValues $manifest.plan 'reviewObligations' | ForEach-Object id }) | Sort-Object -Unique)
    if ($phaseKey -eq 'context') {
        if ($missing.Count -gt 0) { $issues.Add("Restore missing context sources: $($missing -join ', ').") }
        $state = if ($issues.Count -eq 0) { 'completed' } else { 'blocked' }
    } else {
        if ($criteria.Count -eq 0) { $issues.Add('Map this phase to acceptance criteria through existing changedPaths/testPaths or explicit manifest criterionIds.') }
        $mappedFiles = @($criteria | ForEach-Object { @(Get-PhaseValues $_.mapping 'changedPaths'); @(Get-PhaseValues $_.mapping 'testPaths') })
        foreach ($file in $present) {
            if ($file -notin $mappedFiles -and $explicitIds.Count -eq 0) { $issues.Add("Map changed phase source '$file' to an acceptance outcome.") }
        }
        foreach ($criterion in $criteria) {
            if ($criterion.status -notin @('satisfied', 'not-applicable')) { $issues.Add("Resolve acceptance criterion '$($criterion.id)'."); continue }
            if (-not $criterion.resolution.PSObject.Properties['criterionFingerprint'] -or [string]$criterion.resolution.criterionFingerprint -cne (Get-LlmWikiCriterionFingerprint $criterion)) {
                $issues.Add("Resolve criterion '$($criterion.id)' against its current text using acceptance-resolve; legacy or changed requirements need fresh acceptance.")
            }
            if ($criterion.status -eq 'not-applicable') {
                if ([string]::IsNullOrWhiteSpace([string]$criterion.resolution.reason)) { $issues.Add("Explain why '$($criterion.id)' is not applicable.") }
            } elseif (@($criterion.mapping.checkIds | Where-Object { $_ -in $executedIds }).Count -eq 0) {
                $issues.Add("Criterion '$($criterion.id)' needs a mapped, current executed check with a passing log.")
            }
        }
        if (@($criteria | Where-Object status -eq 'satisfied').Count -eq 0) { $issues.Add('At least one observable acceptance outcome must be satisfied.') }
        foreach ($id in $checkIds) {
            if ($id -notin $executedIds) {
                $entry = @($checks | Where-Object id -eq $id | Select-Object -First 1)
                $waived = $entry.Count -gt 0 -and $entry[0].status -eq 'not-applicable' -and
                    @($lineage.items | Where-Object { $_.id -eq $id -and $_.kind -in @('check', 'supplemental') -and $_.valid }).Count -gt 0
                if (-not $waived) { $issues.Add("Run or refresh phase check '$id'; manual pass labels are insufficient.") }
            }
        }
        foreach ($id in $reviewIds) {
            if (@($reviews | Where-Object { $_.id -eq $id -and $_.status -in @('completed', 'not-applicable') }).Count -eq 0 -or
                @($lineage.items | Where-Object { $_.kind -eq 'review' -and $_.id -eq $id -and $_.valid }).Count -eq 0) { $issues.Add("Complete current review '$id'.") }
        }
        if ($phaseKey -eq 'release-readiness' -and $lineage.PSObject.Properties['valid'] -and -not $lineage.valid) {
            foreach ($issue in $lineage.issues) { $issues.Add([string]$issue) }
        }
        $state = if ($issues.Count -eq 0) { 'verified' } elseif ($present.Count -gt 0) { 'awaiting-evidence' } else { 'pending' }
    }
    [pscustomobject][ordered]@{
        id = $phaseKey; order = [int]$phase.order; title = [string]$phase.title
        files = $files; changedFiles = $present; state = $state; missingFiles = $missing
        criterionIds = @($criteria | ForEach-Object id); checkIds = $checkIds; reviewIds = $reviewIds; issues = @($issues)
    }
})
$selected = if (-not [string]::IsNullOrWhiteSpace($PhaseId)) { $phases | Where-Object id -eq $PhaseId | Select-Object -First 1 } else { $phases | Where-Object state -notin @('completed', 'verified') | Select-Object -First 1 }
$allComplete = $phases.Count -gt 0 -and @($phases | Where-Object state -notin @('completed', 'verified')).Count -eq 0
$valid = if ($Action -eq 'complete') { ($allComplete -and [string]::IsNullOrWhiteSpace($PhaseId)) -or ($null -ne $selected -and $selected.state -in @('completed', 'verified')) } else { [string]::IsNullOrWhiteSpace($PhaseId) -or $null -ne $selected }
$result = [pscustomobject][ordered]@{
    schemaVersion = 2; action = $Action; valid = $valid; phases = $phases; currentPhase = $selected; allComplete = $allComplete
    nextAction = if ($null -ne $selected -and $selected.issues.Count -gt 0) { [string]$selected.issues[0] } elseif ($allComplete) { 'All phases are verified; continue with final delivery validation.' } elseif ($null -eq $selected) { 'Unknown phase; select an ID from phases.' } else { 'Selected phase is verified; phase-next derives the next unfinished phase.' }
    sourceOfTruth = "$WorkspacePath/change-manifest.json, acceptance-matrix.json, evidence.json and current source/execution lineage"
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12 } else {
    Write-Host "Implementation phases: $(@($phases | Where-Object state -in @('completed', 'verified')).Count)/$($phases.Count) verified or context-complete"
    foreach ($phase in $phases) { Write-Host " - $($phase.id) [$($phase.state)]: $($phase.title)" }
    Write-Host "NEXT: $($result.nextAction)"
}
if ($FailOnInvalid -and -not $valid) { exit 1 }
