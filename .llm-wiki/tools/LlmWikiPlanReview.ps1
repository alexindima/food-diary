function Get-LlmWikiPlanReview {
    param([object]$Plan, [object]$BehaviorSpecification, [string[]]$SourcePath, [string[]]$Decision, [string]$RepositoryRoot, [object]$ReviewContent, [bool]$Required)
    $sources = @(foreach ($path in @($SourcePath + @($Plan.phases | ForEach-Object files) | Where-Object { $_ } | Sort-Object -Unique)) {
        $absolute = Join-Path $RepositoryRoot $path
        [ordered]@{ path = $path; sha256 = if (Test-Path -LiteralPath $absolute -PathType Leaf) { (Get-FileHash -LiteralPath $absolute -Algorithm SHA256).Hash.ToLowerInvariant() } elseif (Test-Path -LiteralPath $absolute -PathType Container) { '<directory>' } else { '<missing>' } }
    })
    $payload = [ordered]@{ objective = $Plan.objective; behavior = $BehaviorSpecification; decisions = @($Decision); phases = @($Plan.phases); sources = $sources }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($payload | ConvertTo-Json -Depth 30 -Compress))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $fingerprint = ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant() } finally { $sha.Dispose() }
    $questions = @(
        [ordered]@{ id = 'outcomes'; question = 'Does each observable acceptance outcome have sufficient implementation work and a concrete verification scenario?' }
        [ordered]@{ id = 'consumers'; question = 'Are runtime wiring, downstream consumers and compatibility covered? Cite the actual paths, or explain why none apply.' }
        [ordered]@{ id = 'failure-behavior'; question = 'Are negative cases, boundaries and preserved behavior covered by the proposed checks?' }
        [ordered]@{ id = 'sequence'; question = 'Can the work execute in this order without missing prerequisites or circular dependencies?' }
        [ordered]@{ id = 'scope'; question = 'Does the plan respect user decisions, non-goals and a bounded context budget?' }
    )
    $criteria = @($BehaviorSpecification.acceptanceCriteria)
    $issues = [Collections.Generic.List[string]]::new()
    $review = if ($null -eq $ReviewContent) { $null } else { $ReviewContent | ConvertTo-Json -Depth 20 | ConvertFrom-Json }
    if ($null -ne $review) {
        foreach ($name in @('planFingerprint', 'outcomes', 'perspectives')) {
            if (-not $review.PSObject.Properties[$name]) { $review | Add-Member -NotePropertyName $name -NotePropertyValue $null }
        }
        foreach ($row in @($review.outcomes | Where-Object { $null -ne $_ })) {
            foreach ($name in @('criterion', 'phaseIds', 'verification', 'reasoning')) {
                if (-not $row.PSObject.Properties[$name]) { $row | Add-Member -NotePropertyName $name -NotePropertyValue $null }
            }
        }
        foreach ($row in @($review.perspectives | Where-Object { $null -ne $_ })) {
            foreach ($name in @('id', 'status', 'reasoning')) {
                if (-not $row.PSObject.Properties[$name]) { $row | Add-Member -NotePropertyName $name -NotePropertyValue $null }
            }
        }
        if ([string]$review.planFingerprint -cne $fingerprint) { $issues.Add('Plan review is stale or belongs to another design; use the current fingerprint.') }
        foreach ($criterion in $criteria) {
            $rows = @($review.outcomes | Where-Object { $null -ne $_ -and [string]$_.criterion -ceq [string]$criterion })
            if ($rows.Count -ne 1) { $issues.Add("Review exactly once: $criterion"); continue }
            $row = $rows[0]
            if (@($row.phaseIds | Where-Object { $_ }).Count -eq 0 -or @($row.phaseIds | Where-Object { $_ -notin @($Plan.phases.id) }).Count -gt 0) { $issues.Add("Outcome requires existing plan phase IDs: $criterion") }
            if ([string]::IsNullOrWhiteSpace([string]$row.verification) -or [string]::IsNullOrWhiteSpace([string]$row.reasoning)) { $issues.Add("Outcome needs verification and sufficiency reasoning: $criterion") }
        }
        if (@($review.outcomes | Where-Object { $null -ne $_ -and $_.criterion -cnotin $criteria }).Count -gt 0) { $issues.Add('Review includes an unknown acceptance outcome.') }
        foreach ($question in $questions) {
            $rows = @($review.perspectives | Where-Object { $null -ne $_ -and $_.id -eq $question.id })
            if ($rows.Count -ne 1) { $issues.Add("Review perspective exactly once: $($question.id)"); continue }
            if ($rows[0].status -notin @('covered', 'not-applicable') -or [string]::IsNullOrWhiteSpace([string]$rows[0].reasoning)) { $issues.Add("Unresolved plan perspective: $($question.id)") }
        }
        if (-not $review.PSObject.Properties['findings']) { $issues.Add('Declare findings explicitly, including an empty array when none remain.') }
        foreach ($finding in @(@(if ($review.PSObject.Properties['findings']) { $review.findings }) | Where-Object { $null -ne $_ })) {
            if (-not $finding.PSObject.Properties['status'] -or $finding.status -ne 'resolved') { $issues.Add('Unresolved plan finding; resolve it before implementation.') }
        }
    } elseif ($Required) { $issues.Add('Review the generated plan backwards from every acceptance outcome before implementation.') }
    [pscustomobject][ordered]@{
        required = $Required
        status = if ($null -eq $review -and -not $Required) { 'not-required' } elseif ($null -eq $review) { 'needs-review' } elseif ($issues.Count) { 'needs-revision' } else { 'reviewed' }
        valid = $issues.Count -eq 0
        planFingerprint = $fingerprint
        acceptanceCriteria = $criteria
        perspectives = $questions
        issues = @($issues)
        review = $review
        authority = 'Validates completeness and source binding of a reasoned review; it does not independently judge semantic sufficiency or prove execution.'
    }
}
