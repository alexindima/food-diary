[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [Alias('Intent')]
    [string]$Objective,
    [string[]]$ProposedPath,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$journeys = & (Join-Path $PSScriptRoot 'Find-LlmWikiProductJourney.ps1') -Query $Objective -ChangedPath $ProposedPath -Format Json | ConvertFrom-Json
$cases = [Collections.Generic.List[object]]::new()
foreach ($journey in @($journeys.journeys)) {
    foreach ($scenario in @($journey.scenarios)) {
        $scenarioId = if ($null -ne $scenario -and $scenario.PSObject.Properties['id'] -and $scenario.id) { [string]$scenario.id } else { [string]$scenario }
        $cases.Add([pscustomobject][ordered]@{ id = "QA-$scenarioId"; journey = [string]$journey.id; kind = 'journey'; scenario = $scenario; expected = 'The documented journey outcome completes without an unexpected side effect.' })
    }
}
foreach ($generic in @(
    @{ id = 'QA-ERROR'; kind = 'negative'; scenario = 'Trigger validation, authorization, provider, or network failure applicable to the changed flow.'; expected = 'A safe actionable error is shown and partial side effects are not committed.' },
    @{ id = 'QA-RETRY'; kind = 'resilience'; scenario = 'Retry or repeat the user action.'; expected = 'The result is idempotent or duplicate behavior is explicitly prevented.' }
)) { $cases.Add([pscustomobject]$generic) }
if (@($ProposedPath | Where-Object { $_ -match '(?i)FoodDiary\.Web\.Client|\.(html|scss|css|ts)$' }).Count -gt 0) {
    foreach ($generic in @(
        @{ id = 'QA-A11Y'; kind = 'accessibility'; scenario = 'Complete the changed UI flow using keyboard and screen-reader labels.'; expected = 'Focus, labels, contrast, and reduced-motion behavior remain usable.' },
        @{ id = 'QA-LOCALE'; kind = 'localization'; scenario = 'Exercise changed user-visible behavior in English and Russian.'; expected = 'Both locales are complete and Russian text contains no mojibake.' },
        @{ id = 'QA-MOBILE'; kind = 'responsive'; scenario = 'Exercise the changed UI flow at the narrow mobile breakpoint.'; expected = 'Content and actions remain visible, operable, and stable.' }
    )) { $cases.Add([pscustomobject]$generic) }
}
$journeyIds = @($journeys.journeys | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id } } | Where-Object { $_ })
$result = [pscustomobject][ordered]@{ schemaVersion = 1; objective = $Objective; journeys = $journeyIds; cases = @($cases); note = 'Derived QA plan. Promote only stable product journeys to the durable journey catalog.' }
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; exit 0 }
Write-Host "Manual QA plan: $($cases.Count) case(s), $(@($journeys.journeys).Count) matched journey(s)"
foreach ($case in $cases) { Write-Host " - $($case.id) [$($case.kind)]: $($case.scenario) => $($case.expected)" }
Write-Host $result.note
