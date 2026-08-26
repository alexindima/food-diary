[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$facade = Join-Path $repositoryRoot '.llm-wiki/wiki.ps1'

function Assert-Retrieval([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$generatedBefore = @(& git -C $repositoryRoot diff --name-only -- .llm-wiki/generated)
$catalog = & $facade catalog -Format Json -Limit 2 | ConvertFrom-Json
$generatedAfter = @(& git -C $repositoryRoot diff --name-only -- .llm-wiki/generated)
Assert-Retrieval ([bool]$catalog.readOnly) 'Catalog query is not explicitly read-only.'
Assert-Retrieval ($catalog.index -eq 'catalog') 'Catalog reader returned the wrong index identity.'
Assert-Retrieval (($generatedBefore -join "`n") -ceq ($generatedAfter -join "`n")) 'Catalog read modified a compiled projection.'

$context = & $facade context -Query 'billing renewal service' -Format Json -Limit 5 | ConvertFrom-Json
Assert-Retrieval ([bool]$context.conclusive -and -not [bool]$context.abstained) 'Grounded context query did not report a conclusive result.'
Assert-Retrieval ($context.candidates[0].path -eq 'FoodDiary.Application.Billing/Services/BillingRenewalService.cs') 'Grounded context query lost its expected top candidate.'
Assert-Retrieval (-not [string]::IsNullOrWhiteSpace([string]$context.confidence)) 'Context query omitted calibrated confidence.'

$ownership = & $facade ownership -Query 'subscription checkout payment webhook renewal and financial state' -Format Json -Limit 5 | ConvertFrom-Json
Assert-Retrieval ([bool]$ownership.conclusive -and @($ownership.ownershipGuides).Count -gt 0) 'Intent ownership returned an empty successful result.'
Assert-Retrieval ($ownership.ownershipGuides[0].guide -eq 'FoodDiary.Application.Billing/AGENTS.md') 'Intent ownership resolved the wrong scoped guide.'

$trace = & $facade trace -Query 'Trace the primary user scenario end to end from endpoint or event through command/query to persistence/provider for gamification achievements points streaks rewards and concurrent updates.' -Format Json -Limit 5 | ConvertFrom-Json
Assert-Retrieval ([bool]$trace.abstained -and -not [bool]$trace.traceConclusive) 'Broad trace invented a conclusive execution chain.'
Assert-Retrieval (@($trace.entryCandidates).Count -gt 0) 'Broad trace did not return bounded entry candidates.'
Assert-Retrieval ($trace.entryCandidates[0].path -notmatch 'FoodDiary\.Application\.Admin/Queries/GetAdminUserLoginEvents') 'Broad trace regressed to the unrelated admin-login fallback.'

foreach ($variant in @(
    'Trace primary user scenario end to end for gamification achievements points streaks rewards and concurrent updates.',
    'Trace the user journey from endpoint through persistence for gamification achievements points streaks and rewards.',
    'Trace user flow end-to-end for gamification achievements and rewards.'
)) {
    $variantTrace = & $facade trace -Query $variant -Format Json -Limit 5 | ConvertFrom-Json
    Assert-Retrieval ([bool]$variantTrace.abstained -and -not [bool]$variantTrace.traceConclusive) "Broad trace paraphrase fabricated a chain: $variant"
    Assert-Retrieval ($variantTrace.entryCandidates[0].path -notmatch 'FoodDiary\.Application\.Admin/Queries/GetAdminUserLoginEvents') "Broad trace paraphrase selected the unrelated admin-login flow: $variant"
}

$risk = & $facade hotspots -Query 'abcxyz987-no-static-risk-match' -Format Json -Limit 3 | ConvertFrom-Json
Assert-Retrieval (-not [bool]$risk.conclusive -and [bool]$risk.abstained) 'Empty risk query still looks conclusive.'
Assert-Retrieval (-not [string]::IsNullOrWhiteSpace([string]$risk.abstentionReason)) 'Empty risk query omitted its evidence caveat.'

Write-Host 'LLM Wiki read-only retrieval contracts passed.'
