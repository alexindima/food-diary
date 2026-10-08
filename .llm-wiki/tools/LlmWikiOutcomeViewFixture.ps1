function New-LlmWikiOutcomeViewRegistry {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ToolsRoot,
        [Parameter(Mandatory)][string]$FixtureRoot,
        [Parameter(Mandatory)][ValidateSet('Model', 'Context', 'Instruction')][string]$Kind,
        [ValidateRange(1, 1000)][int]$Count = 60
    )
    $name = switch ($Kind) {
        'Model' { 'Manage-LlmWikiModelRoutingOutcome.ps1' }
        'Context' { 'Manage-LlmWikiContextOutcome.ps1' }
        'Instruction' { 'Manage-LlmWikiInstructionOutcome.ps1' }
    }
    . (Join-Path $ToolsRoot 'LlmWikiJson.ps1')
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $ToolsRoot $name), [ref]$null, [ref]$null)
    foreach ($functionName in @('Get-Hash', 'Get-EventPayload', 'Get-ActualOutcomePayload')) {
        $definition = $ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $functionName}, $true)
        if ($null -ne $definition) { . ([scriptblock]::Create($definition.Extent.Text)) }
    }
    $policyPath = Join-Path $FixtureRoot '.llm-wiki/policies/workspace-policies.json'
    $policy = [IO.File]::ReadAllText($policyPath) | ConvertFrom-LlmWikiJson
    $sourcePath = Join-Path $FixtureRoot 'AGENTS.md'
    if (-not (Test-Path -LiteralPath $sourcePath)) { [IO.File]::WriteAllText($sourcePath, 'Owned fixture instructions.') }
    $sourceFingerprint = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
    $policyFingerprint = (Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $events = [Collections.Generic.List[object]]::new()
    $previous = ''
    foreach ($index in 1..$Count) {
        $score = if ($index -gt ($Count - 10)) { 20.0 } else { 90.0 }
        $event = [pscustomobject][ordered]@{
            schemaVersion=$(if ($Kind -eq 'Instruction') {2} else {1});eventId=$index.ToString('x32')
            workspace='.artifacts/llm-wiki/tasks/fixture';recordedAtUtc=([DateTime]::SpecifyKind([DateTime]'2026-10-01', [DateTimeKind]::Utc)).AddMinutes($index).ToString('o')
            completionFingerprint=$index.ToString('x64');retrospectiveHash=('a' * 64)
            policyFingerprint=$policyFingerprint;previousEventHash=$previous;eventHash='';success=($score -ge 75)
        }
        if ($Kind -eq 'Instruction') {
            $sources = @([pscustomobject][ordered]@{path='AGENTS.md';fingerprint=$sourceFingerprint})
            $event | Add-Member -NotePropertyName sources -NotePropertyValue $sources
            $event | Add-Member -NotePropertyName instructionSetFingerprint -NotePropertyValue (Get-Hash $sources)
            $event | Add-Member -NotePropertyName taskSignals -NotePropertyValue ([pscustomobject][ordered]@{
                complexityScore=35;riskLevel='medium';complexityBandUpperBound=[int]($policy.scheduler.verificationPlanner.instructionOutcomes.complexityBandUpperBounds | Where-Object { $_ -ge 35 } | Select-Object -First 1)
                cohortKey='backend|medium';modelRouteId='route';contextStrategyId='variant'
            })
            $event | Add-Member -NotePropertyName outcome -NotePropertyValue ([pscustomobject][ordered]@{score=$score;repairAttempts=1})
        } else {
            $event | Add-Member -NotePropertyName actualOutcome -NotePropertyValue ([pscustomobject][ordered]@{
                score=$score;baseScore=$score;components=[pscustomobject][ordered]@{readiness=$score;confidence=$score;critique=$score;verification=$score}
                penalty=0;penaltyBreakdown=[pscustomobject][ordered]@{failedRepair=0;falseNegative=0;impactDrift=0;flakyCheck=0;quarantinedContextSource=0;rolledBack=0}
                quality='fixture';completionVerdict='ready';repairAttempts=1
            })
            if ($Kind -eq 'Model') {
                foreach ($pair in @(@('routeReceiptHash',('b' * 64)),@('routeId',"route-$($index % 3)"),@('routeRank',1),@('model','fixture-model'),@('reasoningEffort','high'),@('relativeCostUnits',1),@('complexityScore',35),@('riskLevel','medium'))) {
                    $event | Add-Member -NotePropertyName $pair[0] -NotePropertyValue $pair[1]
                }
            } else {
                foreach ($pair in @(@('strategyApplicationHash',('b' * 64)),@('strategyState','applied'),@('variantId',"variant-$($index % 3)"),@('itemLimit',20),@('characterBudget',12000),@('syntheticQualityScore',90.0))) {
                    $event | Add-Member -NotePropertyName $pair[0] -NotePropertyValue $pair[1]
                }
                $event | Add-Member -NotePropertyName taskProfile -NotePropertyValue ([pscustomobject][ordered]@{cohortKey='backend|medium';changeClass='backend';riskLevel='medium';scopes=@('Backend');modules=@('Fixture')})
            }
        }
        $event.eventHash = Get-Hash (Get-EventPayload $event)
        $previous = $event.eventHash
        $events.Add($event)
    }
    [pscustomobject][ordered]@{schemaVersion=$(if ($Kind -eq 'Instruction') {2} else {1});events=@($events.ToArray())}
}
