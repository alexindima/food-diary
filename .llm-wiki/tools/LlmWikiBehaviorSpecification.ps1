function Get-LlmWikiBehaviorSpecification {
    param([string]$Objective, [object]$InputSpec, [string[]]$Criterion, [string[]]$SourcePath, [bool]$Required)

    $spec = if ($null -eq $InputSpec) { [pscustomobject]@{} } else {
        $InputSpec | ConvertTo-Json -Depth 12 | ConvertFrom-Json
    }
    $issues = [Collections.Generic.List[string]]::new()
    $declaredFields = @($spec.PSObject.Properties | ForEach-Object Name)
    foreach ($name in @('currentBehavior', 'preservedBehavior', 'nonGoals', 'examples', 'failureNotApplicableReason', 'openQuestions')) {
        if (-not $spec.PSObject.Properties[$name]) { $spec | Add-Member -NotePropertyName $name -NotePropertyValue $null }
    }
    $examples = @($spec.examples | Where-Object { $null -ne $_ })
    $criteria = @($Criterion | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $applicable = $Required -or $null -ne $InputSpec
    if ($applicable) {
        if ([string]::IsNullOrWhiteSpace([string]$spec.currentBehavior)) { $issues.Add('Describe currentBehavior from the cited current sources.') }
        foreach ($name in @('preservedBehavior', 'nonGoals')) {
            if ($name -notin $declaredFields) { $issues.Add("Declare $name explicitly; an empty array is allowed when nothing applies.") }
        }
        if ($criteria.Count -eq 0) { $issues.Add('Supply observable acceptance outcomes through the existing -Criterion parameter.') }
        if ($examples.Count -eq 0) { $issues.Add('Supply at least one concrete given/when/then example.') }
        foreach ($example in $examples) {
            foreach ($name in @('given', 'when', 'then')) {
                if (-not $example.PSObject.Properties[$name] -or [string]::IsNullOrWhiteSpace([string]$example.$name)) { $issues.Add("Every behavior example requires $name.") }
            }
            if (-not $example.PSObject.Properties['kind'] -or [string]$example.kind -notin @('success', 'failure', 'boundary')) { $issues.Add('Every example kind must be success, failure, or boundary.') }
            foreach ($name in @('kind', 'given', 'when', 'then')) {
                if (-not $example.PSObject.Properties[$name]) { $example | Add-Member -NotePropertyName $name -NotePropertyValue '' }
            }
        }
        if (@($examples | Where-Object kind -eq 'success').Count -eq 0) { $issues.Add('Include a success example.') }
        if (@($examples | Where-Object kind -in @('failure', 'boundary')).Count -eq 0) { $issues.Add('Include a failure or boundary example, or record why it is inapplicable in failureNotApplicableReason.') }
        if (-not [string]::IsNullOrWhiteSpace([string]$spec.failureNotApplicableReason)) {
            $issues.Remove('Include a failure or boundary example, or record why it is inapplicable in failureNotApplicableReason.') | Out-Null
        }
        foreach ($question in @($spec.openQuestions | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })) {
            $issues.Add("Unresolved behavior decision: $question")
        }
    }
    [pscustomobject][ordered]@{
        required = $Required
        status = if (-not $applicable) { 'not-required' } elseif ($issues.Count -gt 0) { 'needs-input' } else { 'specified' }
        currentBehavior = [string]$spec.currentBehavior
        currentStateEvidence = @($SourcePath)
        targetBehavior = $Objective
        preservedBehavior = @($spec.preservedBehavior | Where-Object { $_ })
        nonGoals = @($spec.nonGoals | Where-Object { $_ })
        acceptanceCriteria = $criteria
        examples = @($examples | ForEach-Object { [pscustomobject][ordered]@{ kind = [string]$_.kind; given = [string]$_.given; when = [string]$_.when; then = [string]$_.then } })
        failureNotApplicableReason = [string]$spec.failureNotApplicableReason
        openQuestions = @($issues)
        authority = 'Desired behavior supplied by the task; current-state claims must be checked in the cited sources. Examples are expectations, not execution evidence.'
    }
}
