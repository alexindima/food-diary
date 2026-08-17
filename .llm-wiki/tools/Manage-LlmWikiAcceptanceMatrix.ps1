[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('init', 'show', 'map', 'resolve', 'validate')]
    [string]$Action = 'show',
    [string]$Path = '.artifacts/llm-wiki/acceptance-matrix.json',
    [string]$Objective,
    [string[]]$Criterion,
    [string]$CriterionId,
    [string[]]$ScenarioId,
    [string[]]$CheckId,
    [string[]]$ReviewId,
    [string[]]$TestPath,
    [ValidateSet('pending', 'satisfied', 'not-applicable', 'rejected')]
    [string]$AcceptanceStatus = 'pending',
    [string]$Reason,
    [string]$EvidenceNote,
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string]$EvidencePath = '.artifacts/llm-wiki/evidence.json',
    [switch]$RequireEvidence,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiRequirementCriteria.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitRenames.ps1')
$requirementPolicy = (Get-Content -LiteralPath (Join-Path $wikiRoot 'policies/workspace-policies.json') -Raw | ConvertFrom-Json).requirementModel
$absolutePath = if ([System.IO.Path]::IsPathRooted($Path)) { $Path } else { Join-Path $repositoryRoot $Path }
$absoluteEvidencePath = if ([System.IO.Path]::IsPathRooted($EvidencePath)) { $EvidencePath } else { Join-Path $repositoryRoot $EvidencePath }

function Write-Matrix([object]$Matrix) {
    $directory = Split-Path -Parent $absolutePath
    if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($absolutePath, (($Matrix | ConvertTo-Json -Depth 15) + [Environment]::NewLine), $utf8WithoutBom)
}

function Read-Matrix {
    if (-not (Test-Path -LiteralPath $absolutePath)) { throw "Acceptance matrix does not exist: $Path" }
    Get-Content -LiteralPath $absolutePath -Raw | ConvertFrom-Json
}

function Get-Criterion([object]$Matrix, [string]$Id) {
    $item = $Matrix.criteria | Where-Object id -eq $Id | Select-Object -First 1
    if ($null -eq $item) { throw "Unknown acceptance criterion: $Id" }
    return $item
}

function Merge-Unique([object[]]$Existing, [object[]]$Additional) {
    @($Existing + $Additional | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
}

function Test-TestOnlyPacket([object]$Packet) {
    $productPaths = @($Packet.diff.changedPaths | Where-Object {
        $_ -notmatch '^\.llm-wiki/(?:generated|reviews)/' -and
        $_ -notmatch '^\.artifacts/llm-wiki/'
    })
    if ($productPaths.Count -eq 0) { return $false }
    $testPaths = @($productPaths | Where-Object {
        $_ -match '(?i)(^|/)(tests?|__tests__)/' -or
        $_ -match '(?i)\.Tests?/' -or
        $_ -match '(?i)(?:^|/)[^/]*(?:Tests?|Specs?)\.cs$' -or
        $_ -match '(?i)\.(?:spec|test)\.ts$'
    })
    return $testPaths.Count -eq $productPaths.Count
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

switch ($Action) {
    'init' {
        if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'acceptance init requires -Objective.' }
        $criteriaText = @($Criterion | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        if ($criteriaText.Count -eq 0) { throw 'acceptance init requires at least one -Criterion.' }
        if (@($criteriaText | Sort-Object -Unique).Count -ne $criteriaText.Count) { throw 'Acceptance criteria must be unique.' }
        $packetArguments = @{ BaseRef = $BaseRef; Objective = $Objective; Format = 'Json' }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $packetArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $packetArguments.ChangedPath = $ChangedPath }
        $packet = & (Join-Path $PSScriptRoot 'Get-LlmWikiChangePacket.ps1') @packetArguments | ConvertFrom-Json
        $criteria = [System.Collections.Generic.List[object]]::new()
        $testOnlyPacket = Test-TestOnlyPacket $packet
        $automaticChangedPaths = if ($testOnlyPacket) {
            @($packet.diff.changedPaths | Where-Object { $_ -notmatch '^\.llm-wiki/(?:generated|reviews)/' })
        } else { @() }
        $automaticCheckIds = if ($testOnlyPacket) {
            @($packet.policy.requiredChecks | ForEach-Object {
                if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id }
            } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        } else { @() }
        $automaticTestPaths = if ($testOnlyPacket) { @($packet.testPlan.focusedTestFiles) } else { @() }
        for ($index = 0; $index -lt $criteriaText.Count; $index++) {
            $criterionTokens = @([regex]::Matches($criteriaText[$index].ToLowerInvariant(), '[\p{L}\p{Nd}]+') | ForEach-Object Value | Where-Object { $_.Length -ge 4 } | Sort-Object -Unique)
            $suggestedChangedPaths = @($packet.diff.changedPaths | Where-Object {
                $candidate = ([string]$_).ToLowerInvariant()
                @($criterionTokens | Where-Object { $candidate.Contains($_) }).Count -gt 0
            } | Select-Object -First 6)
            $suggestedTestPaths = @($packet.testPlan.focusedTestFiles | Where-Object {
                $candidate = ([string]$_).ToLowerInvariant()
                @($criterionTokens | Where-Object { $candidate.Contains($_) }).Count -gt 0
            } | Select-Object -First 4)
            $suggestedScenarioIds = @($packet.testPlan.scenarios | Where-Object {
                $candidate = "$($_.id) $($_.description)".ToLowerInvariant()
                @($criterionTokens | Where-Object { $candidate.Contains($_) }).Count -gt 0
            } | ForEach-Object id | Select-Object -First 4)
            $criteria.Add([pscustomobject][ordered]@{
                id = 'AC-{0:d3}' -f ($index + 1)
                text = $criteriaText[$index]
                status = 'pending'
                mapping = [pscustomobject][ordered]@{
                    changedPaths = @($automaticChangedPaths)
                    scenarioIds = @()
                    checkIds = @($automaticCheckIds)
                    reviewIds = @()
                    testPaths = @($automaticTestPaths)
                }
                mappingSuggestions = [pscustomobject][ordered]@{
                    changedPaths = $suggestedChangedPaths
                    scenarioIds = $suggestedScenarioIds
                    testPaths = $suggestedTestPaths
                }
                resolution = [pscustomobject][ordered]@{
                    reason = $null
                    evidenceNote = $null
                }
            })
        }
        $matrix = [ordered]@{
            schemaVersion = 1
            objective = $Objective
            createdAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
            packetFingerprint = $packet.fingerprint
            git = [ordered]@{ base = $BaseRef; headAtInit = $packet.inputs.gitHead }
            availableEvidence = [ordered]@{
                changedPaths = @($packet.diff.changedPaths)
                renames = @($(if ($packet.diff.PSObject.Properties['renames']) { @($packet.diff.renames) } else { @() }))
                scenarios = @(Get-AvailableScenarios $packet $Objective)
                checks = @($packet.policy.requiredChecks | ForEach-Object { [ordered]@{ id = $_.id; command = $_.command } })
                reviews = @($packet.policy.reviewObligations | ForEach-Object { [ordered]@{ id = $_.id; description = $_.description } })
                testPaths = @($packet.testPlan.focusedTestFiles)
            }
            criteria = @($criteria)
            evidencePath = $EvidencePath
            automaticMapping = [ordered]@{
                applied = $testOnlyPacket
                mode = $(if ($testOnlyPacket) { 'test-only-bundle' } else { 'suggestions-only' })
            }
        }
        Write-Matrix $matrix
        Write-Host "Initialized acceptance matrix: $Path"
        Write-Host "Criteria: $($criteria.Count); scenarios: $(@($matrix.availableEvidence.scenarios).Count); checks: $(@($matrix.availableEvidence.checks).Count)."
    }
    'map' {
        if ([string]::IsNullOrWhiteSpace($CriterionId)) { throw 'acceptance map requires -CriterionId.' }
        $matrix = Read-Matrix
        $item = Get-Criterion $matrix $CriterionId
        $availableScenarioIds = @($matrix.availableEvidence.scenarios | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id } } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        $availableCheckIds = @($matrix.availableEvidence.checks | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id } } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        $availableReviewIds = @($matrix.availableEvidence.reviews | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id } } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        $availableChangedPaths = if ($matrix.availableEvidence.PSObject.Properties['changedPaths']) {
            @($matrix.availableEvidence.changedPaths | ForEach-Object { ([string]$_).Replace('\', '/') })
        } else { @() }
        $knownRenames = if ($matrix.availableEvidence.PSObject.Properties['renames']) { @($matrix.availableEvidence.renames) } else { @() }
        $baseRef = if ($matrix.git.PSObject.Properties['base']) { [string]$matrix.git.base } else { '' }
        if (-not [string]::IsNullOrWhiteSpace($baseRef)) {
            $knownRenames = @($knownRenames) + @(Get-LlmWikiGitRenames -RepositoryRoot $repositoryRoot -BaseRef $baseRef)
        }
        $normalizedChangedPaths = @($ChangedPath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { ([string]$_).Replace('\', '/') })
        $normalizedTestPaths = @($TestPath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { ([string]$_).Replace('\', '/') })
        foreach ($path in $normalizedChangedPaths) {
            if ($path -notin $availableChangedPaths) {
                if (Test-LlmWikiRenameDestination -Path $path -Renames $knownRenames -KnownPaths $availableChangedPaths) {
                    $availableChangedPaths = Merge-Unique $availableChangedPaths @($path)
                } else {
                    throw "Changed path is not present in the task packet: $path. Run task-refresh; rename destinations detected by Git are accepted automatically."
                }
            }
        }
        foreach ($id in @($ScenarioId | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })) {
            if ($id -notin $availableScenarioIds) { throw "Unknown scenario id: $id" }
        }
        foreach ($id in @($CheckId | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })) {
            if ($id -notin $availableCheckIds) { throw "Unknown check id: $id" }
        }
        foreach ($id in @($ReviewId | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })) {
            if ($id -notin $availableReviewIds) { throw "Unknown review id: $id" }
        }
        if ($null -eq $item.mapping.PSObject.Properties['changedPaths']) {
            $item.mapping | Add-Member -NotePropertyName changedPaths -NotePropertyValue @()
        }
        $item.mapping.changedPaths = Merge-Unique @($item.mapping.changedPaths) $normalizedChangedPaths
        $item.mapping.scenarioIds = Merge-Unique @($item.mapping.scenarioIds) @($ScenarioId)
        $item.mapping.checkIds = Merge-Unique @($item.mapping.checkIds) @($CheckId)
        $item.mapping.reviewIds = Merge-Unique @($item.mapping.reviewIds) @($ReviewId)
        $item.mapping.testPaths = Merge-Unique @($item.mapping.testPaths) $normalizedTestPaths
        $matrix.availableEvidence | Add-Member -NotePropertyName changedPaths -NotePropertyValue @($availableChangedPaths) -Force
        $matrix.availableEvidence | Add-Member -NotePropertyName renames -NotePropertyValue @($knownRenames | Sort-Object from, to -Unique) -Force
        Write-Matrix $matrix
        Write-Host "Mapped acceptance criterion: $CriterionId"
    }
    'resolve' {
        if ([string]::IsNullOrWhiteSpace($CriterionId)) { throw 'acceptance resolve requires -CriterionId.' }
        if ($AcceptanceStatus -eq 'pending') { throw 'resolve requires satisfied, not-applicable, or rejected status.' }
        if ($AcceptanceStatus -in @('not-applicable', 'rejected') -and [string]::IsNullOrWhiteSpace($Reason)) {
            throw "$AcceptanceStatus requires -Reason."
        }
        $matrix = Read-Matrix
        $item = Get-Criterion $matrix $CriterionId
        if ($AcceptanceStatus -eq 'satisfied' -and -not (Test-LlmWikiCriterionAtomic ([string]$item.text) $requirementPolicy)) {
            $connectorCount = Get-LlmWikiCriterionCompoundConnectorCount ([string]$item.text)
            throw "Criterion '$CriterionId' cannot be satisfied because it is compound ($connectorCount connector(s)). Run: ./.llm-wiki/wiki.ps1 task-requirements-expand -WorkspacePath <task-workspace> -Reason '<atomic decomposition rationale>', then map and resolve the generated atomic criteria."
        }
        $item.status = $AcceptanceStatus
        $item.resolution.reason = if ([string]::IsNullOrWhiteSpace($Reason)) { $null } else { $Reason }
        $item.resolution.evidenceNote = if ([string]::IsNullOrWhiteSpace($EvidenceNote)) { $null } else { $EvidenceNote }
        Write-Matrix $matrix
        Write-Host "Resolved acceptance criterion $CriterionId as $AcceptanceStatus."
    }
    'validate' {
        $matrix = Read-Matrix
        $evidence = if (Test-Path -LiteralPath $absoluteEvidencePath) {
            Get-Content -LiteralPath $absoluteEvidencePath -Raw | ConvertFrom-Json
        } else { $null }
        $unmapped = [System.Collections.Generic.List[string]]::new()
        $unresolved = [System.Collections.Generic.List[string]]::new()
        $unverified = [System.Collections.Generic.List[string]]::new()
        $nonAtomic = [System.Collections.Generic.List[string]]::new()
        foreach ($item in @($matrix.criteria)) {
            if (-not (Test-LlmWikiCriterionAtomic ([string]$item.text) $requirementPolicy)) { $nonAtomic.Add([string]$item.id) }
            $mappingCount = @(
                @($item.mapping.scenarioIds) +
                @($item.mapping.changedPaths) +
                @($item.mapping.checkIds) +
                @($item.mapping.reviewIds) +
                @($item.mapping.testPaths)
            ).Count
            if ($mappingCount -eq 0) { $unmapped.Add($item.id) }
            if ($item.status -in @('pending', 'rejected')) { $unresolved.Add($item.id) }
            if ($item.status -eq 'satisfied') {
                $verified = -not [string]::IsNullOrWhiteSpace([string]$item.resolution.evidenceNote)
                if ($null -ne $evidence) {
                    foreach ($id in @($item.mapping.checkIds)) {
                        $entry = $evidence.checks | Where-Object id -eq $id | Select-Object -First 1
                        if ($null -ne $entry -and $entry.status -in @('passed', 'not-applicable')) { $verified = $true }
                    }
                    foreach ($id in @($item.mapping.reviewIds)) {
                        $entry = $evidence.reviews | Where-Object id -eq $id | Select-Object -First 1
                        if ($null -ne $entry -and $entry.status -in @('completed', 'not-applicable')) { $verified = $true }
                    }
                }
                if (-not $verified) { $unverified.Add($item.id) }
            }
        }
        $evidenceMissing = [bool]$RequireEvidence -and $null -eq $evidence
        $valid = $unmapped.Count -eq 0 -and $unresolved.Count -eq 0 -and $unverified.Count -eq 0 -and $nonAtomic.Count -eq 0 -and -not $evidenceMissing
        $result = [pscustomobject][ordered]@{
            valid = $valid
            objective = $matrix.objective
            criteriaCount = @($matrix.criteria).Count
            satisfiedCount = @($matrix.criteria | Where-Object status -eq 'satisfied').Count
            notApplicableCount = @($matrix.criteria | Where-Object status -eq 'not-applicable').Count
            unmapped = @($unmapped)
            unresolved = @($unresolved)
            unverified = @($unverified)
            nonAtomic = @($nonAtomic)
            evidenceRequired = [bool]$RequireEvidence
            evidenceMissing = $evidenceMissing
        }
        if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 8 } else {
            Write-Host "Acceptance matrix: $(if ($valid) { 'valid' } else { 'invalid' }); $($result.satisfiedCount)/$($result.criteriaCount) satisfied, $($result.notApplicableCount) not applicable."
            foreach ($id in $unmapped) { Write-Host " - UNMAPPED: $id" }
            foreach ($id in $unresolved) { Write-Host " - UNRESOLVED: $id" }
            foreach ($id in $unverified) { Write-Host " - UNVERIFIED: $id" }
            foreach ($id in $nonAtomic) { Write-Host " - NON-ATOMIC: $id; run task-requirements-expand before resolving it." }
            if ($evidenceMissing) { Write-Host " - EVIDENCE MISSING: $EvidencePath" }
        }
        if ($FailOnInvalid -and -not $valid) { exit 1 }
    }
    default {
        $matrix = Read-Matrix
        if ($Format -eq 'Json') { $matrix | ConvertTo-Json -Depth 15; exit 0 }
        Write-Host "Objective: $($matrix.objective)"
        Write-Host "Packet fingerprint: $($matrix.packetFingerprint)"
        foreach ($item in @($matrix.criteria)) {
            $mapped = @($item.mapping.changedPaths + $item.mapping.scenarioIds + $item.mapping.checkIds + $item.mapping.reviewIds + $item.mapping.testPaths)
            Write-Host " - $($item.id) [$($item.status)]: $($item.text)"
            if ($mapped.Count -gt 0) { Write-Host "   Evidence mapping: $($mapped -join ', ')" }
            $suggested = @($item.mappingSuggestions.changedPaths + $item.mappingSuggestions.scenarioIds + $item.mappingSuggestions.testPaths | Where-Object { $_ })
            if ($mapped.Count -eq 0 -and $suggested.Count -gt 0) { Write-Host "   Suggested mapping (review before applying): $($suggested -join ', ')" }
        }
    }
}
