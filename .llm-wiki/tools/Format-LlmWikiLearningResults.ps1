function Get-LlmWikiResultItems([object]$Result, [string]$PluralProperty, [string]$SingularProperty) {
    if ($Result.PSObject.Properties[$PluralProperty]) { return @($Result.$PluralProperty) }
    if ($Result.PSObject.Properties[$SingularProperty]) { return @($Result.$SingularProperty) }
    @()
}

function Get-LlmWikiResultIssues([object]$Result) {
    if ($Result.PSObject.Properties['issues']) { return @($Result.issues) }
    @()
}

function Write-LlmWikiLearningPromotionResult([object]$Result) {
    Write-Host "Learning promotion: action=$($Result.action), valid=$($Result.valid)"
    if ($Result.PSObject.Properties['addedCount']) { Write-Host "Observed=$($Result.addedCount)" }
    foreach ($candidate in @(Get-LlmWikiResultItems $Result 'candidates' 'candidate' | Where-Object { $null -ne $_ })) {
        Write-Host " - [$($candidate.decision)/$($candidate.materialization)] $($candidate.id): tasks=$($candidate.distinctTaskCount), score=$($candidate.averageScore), eligible=$($candidate.eligible), target=$($candidate.target)"
    }
    foreach ($issue in @(Get-LlmWikiResultIssues $Result)) { Write-Host " - $issue" }
}

function Write-LlmWikiEvalPromotionResult([object]$Result) {
    Write-Host "Eval promotion: action=$($Result.action), valid=$($Result.valid)"
    foreach ($candidate in @(Get-LlmWikiResultItems $Result 'candidates' 'candidate' | Where-Object { $null -ne $_ })) {
        Write-Host " - $($candidate.id): decision=$($candidate.decision), materialization=$($candidate.materialization), signals=$(@($candidate.signals).Count)"
    }
    foreach ($issue in @(Get-LlmWikiResultIssues $Result)) { Write-Host " - $issue" }
}

function Write-LlmWikiLearningHealthResult([object]$Result) {
    Write-Host "Learning health: action=$($Result.action), valid=$($Result.valid)"
    foreach ($item in @(Get-LlmWikiResultItems $Result 'health' 'health' | Where-Object { $null -ne $_ })) {
        Write-Host " - $($item.id): verdict=$($item.recommendation.effectiveVerdict), samples=$($item.recommendation.sampleCount), degraded=$($item.recommendation.degradationPercent)%"
    }
    foreach ($issue in @(Get-LlmWikiResultIssues $Result)) { Write-Host " - $issue" }
}
