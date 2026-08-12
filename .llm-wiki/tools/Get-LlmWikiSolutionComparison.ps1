[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [Alias('Intent')]
    [string]$Objective,
    [string[]]$Option,
    [string[]]$ProposedPath,
    [string[]]$BoundaryEvidence,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$options = @($Option | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$scopePaths = @($ProposedPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
$boundaryEvidence = @($BoundaryEvidence | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)

function Get-OptionalStringProperty([object]$InputObject, [string[]]$Name) {
    if ($null -eq $InputObject) { return '' }
    foreach ($candidate in $Name) {
        $property = $InputObject.PSObject.Properties[$candidate]
        if ($null -ne $property -and -not [string]::IsNullOrWhiteSpace([string]$property.Value)) {
            return [string]$property.Value
        }
    }
    ''
}
$existingScopePaths = @($scopePaths | Where-Object { Test-Path -LiteralPath (Join-Path (Split-Path $PSScriptRoot -Parent | Split-Path -Parent) $_) })
$precedents = @()
if ($scopePaths.Count -gt 0) {
    try {
        $precedentResult = & (Join-Path $PSScriptRoot 'Get-LlmWikiGitPrecedents.ps1') `
            -Objective $Objective `
            -ScopePath $scopePaths `
            -Limit 8 `
            -Format Json | ConvertFrom-Json
        $precedents = @($precedentResult.precedents)
    } catch {
        $precedents = @()
    }
}
if ($options.Count -eq 0) {
    $options = @(
        'Bounded change: extend the existing flow and reuse its source of truth.'
        'Structural change: introduce a new abstraction only where existing boundaries cannot express the behavior.'
        'Defer: preserve current behavior until the unresolved product or compatibility decision is answered.'
    )
}
if ($options.Count -lt 2) { throw 'solutions requires at least two -Option values, or no options to generate the standard bounded alternatives.' }

$rows = for ($index = 0; $index -lt $options.Count; $index++) {
    $text = [string]$options[$index]
    $structural = $text -match '(?i)structur|new abstraction|replace|rewrite|migration|(?:separate|new)\s+\w*\s*(?:system|subsystem|workflow|boundary)'
    $defer = $text -match '(?i)defer|wait|preserve current'
    $bounded = -not $structural -and -not $defer
    $matchingPrecedents = @($precedents | Where-Object {
        $subject = Get-OptionalStringProperty $_ @('subject')
        $bounded -or ($structural -and $subject -match '(?i)architect|refactor|restructur|migrat')
    })
    $evidence = @(
        $existingScopePaths | ForEach-Object {
            [pscustomobject][ordered]@{ kind = 'current-source'; reference = $_; description = 'Declared scope path exists in the current checkout.' }
        }
        $matchingPrecedents | Select-Object -First 4 | ForEach-Object {
            [pscustomobject][ordered]@{
                kind = 'git-precedent'
                reference = Get-OptionalStringProperty $_ @('hash', 'commit')
                description = Get-OptionalStringProperty $_ @('subject')
            }
        }
        if ($structural) {
            $boundaryEvidence | ForEach-Object {
                [pscustomobject][ordered]@{ kind = 'boundary-evidence'; reference = $_; description = 'Caller-supplied evidence that the current boundary is insufficient; verify it in current sources before selection.' }
            }
        }
    )
    $missingEvidence = @()
    if ($existingScopePaths.Count -eq 0) { $missingEvidence += 'At least one grounded current-source path.' }
    if ($matchingPrecedents.Count -eq 0 -and (-not $structural -or $boundaryEvidence.Count -eq 0)) { $missingEvidence += 'A verified similar implementation or an explicit statement that no precedent exists.' }
    if ($structural -and $boundaryEvidence.Count -eq 0) { $missingEvidence += 'Evidence that the existing boundary cannot satisfy an explicit invariant.' }
    [pscustomobject][ordered]@{
        id = "OPT-$('{0:d2}' -f ($index + 1))"
        proposal = $text
        changeCost = $(if ($structural) { 'high' } elseif ($defer) { 'low' } else { 'medium' })
        compatibilityRisk = $(if ($structural) { 'high' } else { 'low' })
        evidence = $evidence
        evidenceCoverage = [pscustomobject][ordered]@{
            groundedPathCount = $existingScopePaths.Count
            precedentCount = $matchingPrecedents.Count
            status = if ($missingEvidence.Count -eq 0) { 'grounded' } elseif ($evidence.Count -gt 0) { 'partial' } else { 'unsubstantiated' }
            missing = $missingEvidence
        }
        tradeoffs = if ($structural) {
            @('May improve a durable boundary.', 'Adds migration, compatibility, and maintenance cost.')
        } elseif ($defer) {
            @('Avoids immediate change risk.', 'Leaves the requested outcome unresolved.')
        } else {
            @('Minimizes change and reuses current ownership.', 'May be insufficient if a proven invariant requires a new boundary.')
        }
        rejectionConditions = if ($structural) {
            @('Reject when the current boundary can satisfy all acceptance criteria.', 'Reject without assessed consumers and migration evidence.')
        } elseif ($defer) {
            @('Reject when acceptance criteria can be satisfied safely now.')
        } else {
            @('Reject when current-source evidence proves the existing boundary cannot satisfy an explicit invariant.')
        }
        decisionChangesWhen = if ($structural) {
            'Prefer this option only after current-source evidence proves the bounded option cannot satisfy a durable invariant.'
        } elseif ($defer) {
            'Prefer this option only while a blocking product, compatibility, or authority decision remains unresolved.'
        } else {
            'Reconsider when consumer, compatibility, or invariant evidence proves a bounded extension is insufficient.'
        }
        evidenceRequired = @('Current source and tests for the affected flow', 'Consumer and compatibility impact', 'Focused verification command')
        recommendation = $(if (-not $structural -and -not $defer) { 'preferred-when-sufficient' } elseif ($defer) { 'only-if-blocked' } else { 'requires-boundary-evidence' })
    }
}
$preferred = @($rows | Where-Object recommendation -eq 'preferred-when-sufficient' | Select-Object -First 1)
$result = [pscustomobject][ordered]@{
    schemaVersion = 2
    objective = $Objective
    scope = @($ProposedPath)
    alternatives = @($rows)
    recommendedOptionId = $(if ($preferred.Count -gt 0) { $preferred[0].id } else { $rows[0].id })
    recommendationRationale = 'The recommended starting point is the lowest-cost option that can be validated against current sources; evidence gaps remain explicit and can change the decision.'
    decisionRule = 'Choose the smallest option that satisfies acceptance criteria and current-source evidence; structural change requires explicit boundary evidence.'
    nextAction = "Run design with -Decision '<selected option and evidence>'."
    provenance = 'Derived comparison; decisions remain recorded in the existing design/task journal artifacts.'
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 10; exit 0 }
Write-Host "Solution comparison: $Objective"
foreach ($row in $rows) { Write-Host " - $($row.id) [$($row.changeCost) cost, $($row.compatibilityRisk) compatibility risk]: $($row.proposal)" }
Write-Host "Recommended starting point: $($result.recommendedOptionId)"
Write-Host $result.recommendationRationale
Write-Host $result.decisionRule
Write-Host "Next: $($result.nextAction)"
