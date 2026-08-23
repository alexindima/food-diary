function Write-LlmWikiLearningExperimentResult([object]$Result) {
    $action = [string]$Result.action
    Write-Host "Learning experiment: action=$action, valid=$($Result.valid)"

    $experiments = if ($Result.PSObject.Properties['experiments']) {
        @($Result.experiments)
    } elseif ($Result.PSObject.Properties['experiment']) {
        @($Result.experiment)
    } else {
        @()
    }
    foreach ($experiment in @($experiments | Where-Object { $null -ne $_ })) {
        Write-Host " - $($experiment.candidateId): shadow=$($experiment.shadow.verdict), canary=$($experiment.canaryState), current=$($experiment.currentEvaluation.verdict), successful=$($experiment.successful)"
    }

    $issues = @(if ($Result.PSObject.Properties['issues']) { @($Result.issues) } else { @() })
    foreach ($issue in $issues) { Write-Host " - $issue" }
}
