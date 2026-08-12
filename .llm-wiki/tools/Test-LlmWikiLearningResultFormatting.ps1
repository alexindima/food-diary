[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Format-LlmWikiLearningResults.ps1')

$promotion = [pscustomobject]@{ id='promotion-one'; decision='approved'; materialization='applied'; distinctTaskCount=2; averageScore=90; eligible=$false; target='durable-memory' }
$eval = [pscustomobject]@{ id='eval-one'; decision='approved'; materialization='applied'; signals=@('one') }
$health = [pscustomobject]@{ id='health-one'; recommendation=[pscustomobject]@{ effectiveVerdict='healthy'; sampleCount=2; degradationPercent=0 } }

$cases = @(
    @{ Writer = 'Write-LlmWikiLearningPromotionResult'; Singular = 'candidate'; Plural = 'candidates'; Item = $promotion; Token = 'promotion-one' },
    @{ Writer = 'Write-LlmWikiEvalPromotionResult'; Singular = 'candidate'; Plural = 'candidates'; Item = $eval; Token = 'eval-one' },
    @{ Writer = 'Write-LlmWikiLearningHealthResult'; Singular = 'health'; Plural = 'health'; Item = $health; Token = 'health-one' }
)
foreach ($case in $cases) {
    foreach ($shape in @('singular', 'plural', 'empty', 'legacy')) {
        $result = [ordered]@{ action = $shape; valid = $true }
        if ($shape -eq 'singular') { $result[$case.Singular] = $case.Item }
        if ($shape -eq 'plural') { $result[$case.Plural] = @($case.Item) }
        if ($shape -eq 'legacy') { $result['legacyValue'] = 'ignored' }
        $text = (& $case.Writer ([pscustomobject]$result) 6>&1 | Out-String)
        if ($shape -in @('singular', 'plural') -and $text -notmatch $case.Token) { throw "$($case.Writer) omitted its $shape result." }
        if ($shape -in @('empty', 'legacy') -and $text -notmatch "action=$shape, valid=True") { throw "$($case.Writer) rejected its $shape result." }
    }
}

Write-Host 'LLM Wiki learning result formatter regression passed: promotion, eval, and health shapes are safe.'
