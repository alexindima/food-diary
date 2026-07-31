[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [Alias('Intent')]
    [string]$Objective,
    [string[]]$Option,
    [string[]]$ProposedPath,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$options = @($Option | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
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
    $structural = $text -match '(?i)structur|new abstraction|replace|rewrite|migration'
    $defer = $text -match '(?i)defer|wait|preserve current'
    [pscustomobject][ordered]@{
        id = "OPT-$('{0:d2}' -f ($index + 1))"
        proposal = $text
        changeCost = $(if ($structural) { 'high' } elseif ($defer) { 'low' } else { 'medium' })
        compatibilityRisk = $(if ($structural) { 'high' } else { 'low' })
        evidenceRequired = @('Current source and tests for the affected flow', 'Consumer and compatibility impact', 'Focused verification command')
        recommendation = $(if (-not $structural -and -not $defer) { 'preferred-when-sufficient' } elseif ($defer) { 'only-if-blocked' } else { 'requires-boundary-evidence' })
    }
}
$preferred = @($rows | Where-Object recommendation -eq 'preferred-when-sufficient' | Select-Object -First 1)
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    objective = $Objective
    scope = @($ProposedPath)
    alternatives = @($rows)
    recommendedOptionId = $(if ($preferred.Count -gt 0) { $preferred[0].id } else { $rows[0].id })
    decisionRule = 'Choose the smallest option that satisfies acceptance criteria and current-source evidence; structural change requires explicit boundary evidence.'
    nextAction = "Run design with -Decision '<selected option and evidence>'."
    provenance = 'Derived comparison; decisions remain recorded in the existing design/task journal artifacts.'
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 10; exit 0 }
Write-Host "Solution comparison: $Objective"
foreach ($row in $rows) { Write-Host " - $($row.id) [$($row.changeCost) cost, $($row.compatibilityRisk) compatibility risk]: $($row.proposal)" }
Write-Host "Recommended starting point: $($result.recommendedOptionId)"
Write-Host $result.decisionRule
Write-Host "Next: $($result.nextAction)"

