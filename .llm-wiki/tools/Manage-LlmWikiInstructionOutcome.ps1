[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('observe', 'list', 'verify', 'metrics', 'candidates')]
    [string]$Action = 'list',
    [string]$WorkspacePath,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$registryPath = Join-Path $wikiRoot 'knowledge/instruction-outcomes.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = ConvertFrom-LlmWikiJson (Get-Content -LiteralPath $policyPath -Raw)
$outcomePolicy = $policy.scheduler.verificationPlanner.instructionOutcomes

function Get-Hash([object]$Value) {
    Get-LlmWikiJsonFingerprint -Value $Value
}
function Get-FileSha([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Normalize-Workspace([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value) -or [IO.Path]::IsPathRooted($Value)) { throw 'WorkspacePath must be repository-relative.' }
    $normalized = $Value.Replace('\', '/').TrimEnd('/')
    if ($normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') { throw 'WorkspacePath must identify one non-hidden task workspace.' }
    $normalized
}
function Get-EventPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Event.schemaVersion
        eventId = [string]$Event.eventId
        workspace = [string]$Event.workspace
        recordedAtUtc = ([DateTimeOffset]$Event.recordedAtUtc).ToUniversalTime().ToString('o')
        completionFingerprint = [string]$Event.completionFingerprint
        retrospectiveHash = [string]$Event.retrospectiveHash
        instructionSetFingerprint = [string]$Event.instructionSetFingerprint
        sources = @($Event.sources)
        taskSignals = $Event.taskSignals
        outcome = $Event.outcome
        success = [bool]$Event.success
        policyFingerprint = [string]$Event.policyFingerprint
        previousEventHash = [string]$Event.previousEventHash
    }
}
function Read-Registry {
    if (-not (Test-Path -LiteralPath $registryPath -PathType Leaf)) {
        [pscustomobject][ordered]@{ schemaVersion = 2; events = @() }
    } else {
        ConvertFrom-LlmWikiJson (Get-Content -LiteralPath $registryPath -Raw)
    }
}
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    if ([int]$Registry.schemaVersion -ne 2) { $issues.Add('Registry schemaVersion must be 2.') }
    $previous = ''
    $completions = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($event in @($Registry.events)) {
        if ([int]$event.schemaVersion -ne 2) { $issues.Add("Event '$($event.eventId)' schemaVersion must be 2.") }
        if ([string]$event.eventId -notmatch '^[a-f0-9]{32}$') { $issues.Add('Instruction outcome eventId is invalid.') }
        if (-not $completions.Add([string]$event.completionFingerprint)) { $issues.Add("Duplicate instruction outcome for completion '$($event.completionFingerprint)'.") }
        if ([string]$event.previousEventHash -cne $previous) { $issues.Add("Instruction outcome chain is invalid at '$($event.eventId)'.") }
        if (@($event.sources).Count -gt [int]$outcomePolicy.maximumSourcesPerEvent) { $issues.Add("Instruction source limit exceeded at '$($event.eventId)'.") }
        if (@($event.sources).Count -ne @($event.sources.path | Sort-Object -Unique).Count) { $issues.Add("Instruction sources are duplicated at '$($event.eventId)'.") }
        foreach ($source in @($event.sources)) {
            if ([string]$source.path -notmatch '(^|/)AGENTS\.md$') { $issues.Add("Non-governed instruction source '$($source.path)' was recorded.") }
            if ([string]$source.fingerprint -notmatch '^[a-f0-9]{64}$') { $issues.Add("Instruction fingerprint for '$($source.path)' is invalid.") }
        }
        if ([string]$event.instructionSetFingerprint -cne (Get-Hash @($event.sources))) { $issues.Add("Instruction set fingerprint is invalid at '$($event.eventId)'.") }
        if ([int]$event.taskSignals.complexityScore -lt 0 -or [int]$event.taskSignals.complexityScore -gt 100) { $issues.Add("Instruction task complexity is invalid at '$($event.eventId)'.") }
        if ([string]$event.taskSignals.riskLevel -notin @('low', 'medium', 'high', 'critical', 'unknown')) { $issues.Add("Instruction task risk is invalid at '$($event.eventId)'.") }
        $expectedBand = [int]($outcomePolicy.complexityBandUpperBounds | Where-Object { [int]$_ -ge [int]$event.taskSignals.complexityScore } | Select-Object -First 1)
        if ([int]$event.taskSignals.complexityBandUpperBound -ne $expectedBand) { $issues.Add("Instruction task complexity band is invalid at '$($event.eventId)'.") }
        if ([double]$event.outcome.score -lt 0 -or [double]$event.outcome.score -gt 100) { $issues.Add("Instruction outcome score is invalid at '$($event.eventId)'.") }
        if ([bool]$event.success -ne ([double]$event.outcome.score -ge [double]$outcomePolicy.successScoreThreshold)) { $issues.Add("Instruction outcome success is invalid at '$($event.eventId)'.") }
        if ([string]$event.policyFingerprint -notmatch '^[a-f0-9]{64}$') { $issues.Add("Policy fingerprint is invalid at '$($event.eventId)'.") }
        if ([string]$event.eventHash -cne (Get-Hash (Get-EventPayload $event))) { $issues.Add("Instruction outcome event hash is invalid at '$($event.eventId)'.") }
        $previous = [string]$event.eventHash
    }
    [pscustomobject][ordered]@{
        valid = $issues.Count -eq 0
        issues = @($issues)
        headHash = $previous
        registryFingerprint = Get-Hash @($Registry.events | ForEach-Object { "$($_.eventId)|$($_.eventHash)" })
    }
}
function Get-Profiles([object]$Registry) {
    $observations = @($Registry.events | ForEach-Object {
        $event = $_
        foreach ($source in @($event.sources)) {
            [pscustomobject]@{
                path = [string]$source.path
                fingerprint = [string]$source.fingerprint
                recordedAtUtc = [string]$event.recordedAtUtc
                score = [double]$event.outcome.score
                success = [bool]$event.success
                repairAttempts = [int]$event.outcome.repairAttempts
                completionFingerprint = [string]$event.completionFingerprint
                riskLevel = [string]$event.taskSignals.riskLevel
                complexityScore = [int]$event.taskSignals.complexityScore
                complexityBandUpperBound = [int]$event.taskSignals.complexityBandUpperBound
                cohortKey = [string]$event.taskSignals.cohortKey
                modelRouteId = [string]$event.taskSignals.modelRouteId
                contextStrategyId = [string]$event.taskSignals.contextStrategyId
            }
        }
    })
    @($observations | Group-Object path, fingerprint | ForEach-Object {
        $items = @($_.Group | Sort-Object { [DateTime]$_.recordedAtUtc })
        $recent = @($items | Select-Object -Last ([Math]::Min($items.Count, [int]$outcomePolicy.recentWindowSamples)))
        $baseline = if ($items.Count -gt $recent.Count) { @($items | Select-Object -First ($items.Count - $recent.Count)) } else { @() }
        $average = [Math]::Round([double](($items.score | Measure-Object -Average).Average), 2)
        $recentAverage = [Math]::Round([double](($recent.score | Measure-Object -Average).Average), 2)
        $recentSuccess = [Math]::Round(100.0 * @($recent | Where-Object success).Count / $recent.Count, 2)
        $baselineAverage = if ($baseline.Count -eq 0) { $null } else { [Math]::Round([double](($baseline.score | Measure-Object -Average).Average), 2) }
        $drop = if ($null -eq $baselineAverage) { 0.0 } else { [Math]::Round([double]$baselineAverage - $recentAverage, 2) }
        $reasons = [Collections.Generic.List[string]]::new()
        if ($recent.Count -ge [int]$outcomePolicy.minimumDriftSamples -and $baseline.Count -ge [int]$outcomePolicy.minimumDriftSamples -and $drop -gt [double]$outcomePolicy.maximumRecentOutcomeDropPoints) { $reasons.Add('recent-outcome-drop') }
        if ($recent.Count -ge [int]$outcomePolicy.minimumDriftSamples -and $recentSuccess -lt [double]$outcomePolicy.minimumRecentSuccessRatePercent) { $reasons.Add('recent-success-rate') }
        $eligible = $items.Count -ge [int]$outcomePolicy.minimumSamples
        $cohorts = @($items | Group-Object cohortKey | ForEach-Object {
            $cohortItems = @($_.Group)
            $cohortMean = [double](($cohortItems.score | Measure-Object -Average).Average)
            $cohortVariance = if ($cohortItems.Count -le 1) {
                0.0
            } else {
                [double](($cohortItems | ForEach-Object { [Math]::Pow([double]$_.score - $cohortMean, 2) } | Measure-Object -Sum).Sum) / ($cohortItems.Count - 1)
            }
            [pscustomobject][ordered]@{
                key = [string]$_.Name
                riskLevel = [string]$cohortItems[0].riskLevel
                complexityBandUpperBound = [int]$cohortItems[0].complexityBandUpperBound
                sampleCount = $cohortItems.Count
                successCount = @($cohortItems | Where-Object success).Count
                averageOutcomeScore = [Math]::Round($cohortMean, 2)
                outcomeStandardDeviation = [Math]::Round([Math]::Sqrt($cohortVariance), 4)
                successRatePercent = [Math]::Round(100.0 * @($cohortItems | Where-Object success).Count / $cohortItems.Count, 2)
                averageRepairAttempts = [Math]::Round([double](($cohortItems.repairAttempts | Measure-Object -Average).Average), 2)
            }
        } | Sort-Object key)
        [pscustomobject][ordered]@{
            path = [string]$items[0].path
            fingerprint = [string]$items[0].fingerprint
            sampleCount = $items.Count
            successRatePercent = [Math]::Round(100.0 * @($items | Where-Object success).Count / $items.Count, 2)
            averageOutcomeScore = $average
            recentSampleCount = $recent.Count
            recentAverageOutcomeScore = $recentAverage
            recentSuccessRatePercent = $recentSuccess
            baselineSampleCount = $baseline.Count
            baselineAverageOutcomeScore = $baselineAverage
            recentOutcomeDropPoints = $drop
            averageRepairAttempts = [Math]::Round([double](($items.repairAttempts | Measure-Object -Average).Average), 2)
            health = $(if ($eligible -and $reasons.Count -gt 0) { 'degraded' } elseif ($eligible) { 'healthy' } else { 'insufficient-data' })
            degradationReasons = @($reasons)
            cohorts = $cohorts
        }
    } | Sort-Object path, fingerprint)
}
function Write-Registry([object]$Registry) {
    [IO.File]::WriteAllText($registryPath, (($Registry | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}

$registry = Read-Registry
$validation = Test-Registry $registry
if ($Action -eq 'observe') {
    if (-not $validation.valid) { throw "Instruction outcome registry is invalid: $(@($validation.issues) -join ' ')" }
    $workspace = Normalize-Workspace $WorkspacePath
    $absoluteWorkspace = Join-Path $repositoryRoot $workspace
    foreach ($name in @('change-packet.json', 'completion.json', 'retrospective.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $absoluteWorkspace $name) -PathType Leaf)) { throw "Instruction outcome input is absent: $workspace/$name" }
    }
    $completion = ConvertFrom-LlmWikiJson (Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'completion.json') -Raw)
    if ([string]$completion.completionFingerprint -in @($registry.events.completionFingerprint)) {
        $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $true; addedCount = 0; eventHash = ''; reason = 'Completion outcome was already observed.' }
    } else {
        $packet = ConvertFrom-LlmWikiJson (Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-packet.json') -Raw)
        $retrospectiveValidation = ConvertFrom-LlmWikiJson (& (Join-Path $PSScriptRoot 'Manage-LlmWikiRetrospective.ps1') verify -WorkspacePath $workspace -Format Json)
        if (-not $retrospectiveValidation.valid) { throw "Task retrospective is invalid: $(@($retrospectiveValidation.issues) -join ' ')" }
        $sources = @(@('AGENTS.md') + @($packet.brief.instructions) |
            ForEach-Object { ([string]$_).Replace('\', '/') } |
            Where-Object { $_ -match '(^|/)AGENTS\.md$' } |
            Sort-Object -Unique |
            ForEach-Object {
                $absoluteSource = Join-Path $repositoryRoot $_
                if (Test-Path -LiteralPath $absoluteSource -PathType Leaf) {
                    [pscustomobject][ordered]@{ path = $_; fingerprint = Get-FileSha $absoluteSource }
                }
            })
        if ($sources.Count -gt [int]$outcomePolicy.maximumSourcesPerEvent) { throw 'Instruction source count exceeds policy.' }
        $retrospective = $retrospectiveValidation.retrospective
        $resolved = [int]$retrospective.outcome.prediction.resolvedCount
        $predictionErrors = [int]$retrospective.outcome.prediction.falseNegativeCount + [int]$retrospective.outcome.prediction.falsePositiveCount
        $verificationScore = if ($resolved -eq 0) { 100.0 } else { [Math]::Round([Math]::Max(0, 100.0 * ($resolved - $predictionErrors) / $resolved), 2) }
        $score = [Math]::Round(([double]$retrospective.outcome.readinessScore + [double]$retrospective.outcome.confidenceScore + [double]$retrospective.outcome.critiqueScore + $verificationScore) / 4.0, 2)
        $modelRoutePath = Join-Path $absoluteWorkspace 'model-routing.json'
        $modelRoute = if (Test-Path -LiteralPath $modelRoutePath -PathType Leaf) { ConvertFrom-LlmWikiJson (Get-Content -LiteralPath $modelRoutePath -Raw) } else { $null }
        $complexityScore = if ($null -eq $modelRoute) { [int]$retrospective.outcome.risk.score } else { [int]$modelRoute.signals.complexityScore }
        $riskLevel = if ($null -eq $modelRoute) { [string]$retrospective.outcome.risk.level } else { [string]$modelRoute.signals.riskLevel }
        if ([string]::IsNullOrWhiteSpace($riskLevel)) { $riskLevel = 'unknown' }
        $complexityBand = [int]($outcomePolicy.complexityBandUpperBounds | Where-Object { [int]$_ -ge $complexityScore } | Select-Object -First 1)
        $contextApplicationPath = Join-Path $absoluteWorkspace 'context-strategy-application.json'
        $contextApplication = if (Test-Path -LiteralPath $contextApplicationPath -PathType Leaf) { ConvertFrom-LlmWikiJson (Get-Content -LiteralPath $contextApplicationPath -Raw) } else { $null }
        $taskSignals = [pscustomobject][ordered]@{
            riskLevel = $riskLevel
            complexityScore = $complexityScore
            complexityBandUpperBound = $complexityBand
            cohortKey = "$riskLevel|complexity<=$complexityBand"
            modelRouteId = $(if ($null -eq $modelRoute) { 'unrouted' } else { [string]$modelRoute.recommendation.routeId })
            contextStrategyId = $(if ($null -eq $contextApplication) { 'default' } else { [string]$contextApplication.applied.variantId })
        }
        $event = [pscustomobject][ordered]@{
            schemaVersion = 2
            eventId = [guid]::NewGuid().ToString('N')
            workspace = $workspace
            recordedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
            completionFingerprint = [string]$completion.completionFingerprint
            retrospectiveHash = [string]$retrospective.retrospectiveHash
            instructionSetFingerprint = Get-Hash $sources
            sources = $sources
            taskSignals = $taskSignals
            outcome = [pscustomobject][ordered]@{
                score = $score
                quality = [string]$retrospective.outcome.quality
                readinessScore = [double]$retrospective.outcome.readinessScore
                confidenceScore = [double]$retrospective.outcome.confidenceScore
                critiqueScore = [double]$retrospective.outcome.critiqueScore
                verificationScore = $verificationScore
                repairAttempts = [int]$retrospective.outcome.repair.totalAttempts
                falseNegativeCount = [int]$retrospective.outcome.prediction.falseNegativeCount
            }
            success = [bool]($score -ge [double]$outcomePolicy.successScoreThreshold)
            policyFingerprint = Get-FileSha $policyPath
            previousEventHash = [string]$validation.headHash
            eventHash = ''
        }
        $event.eventHash = Get-Hash (Get-EventPayload $event)
        $registry.events = @($registry.events) + @($event)
        if (@($registry.events).Count -gt [int]$outcomePolicy.maximumEvents) { throw 'Instruction outcome registry reached maximumEvents.' }
        $post = Test-Registry $registry
        if (-not $post.valid) { throw "New instruction outcome is invalid: $(@($post.issues) -join ' ')" }
        Write-Registry $registry
        [IO.File]::WriteAllText(
            (Join-Path $absoluteWorkspace 'instruction-outcome.json'),
            (([pscustomobject][ordered]@{ schemaVersion = 1; registryEvent = $event; registryFingerprint = $post.registryFingerprint } | ConvertTo-Json -Depth 30) + [Environment]::NewLine),
            [Text.UTF8Encoding]::new($false)
        )
        $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $true; addedCount = 1; eventHash = $event.eventHash; outcome = $event }
    }
} elseif ($Action -eq 'verify') {
    $issues = [Collections.Generic.List[string]]::new()
    foreach ($issue in @($validation.issues)) { $issues.Add($issue) }
    $workspaceReceipt = $null
    if (-not [string]::IsNullOrWhiteSpace($WorkspacePath)) {
        $workspace = Normalize-Workspace $WorkspacePath
        $receiptPath = Join-Path (Join-Path $repositoryRoot $workspace) 'instruction-outcome.json'
        if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
            $issues.Add('instruction-outcome.json is absent.')
        } else {
            $workspaceReceipt = ConvertFrom-LlmWikiJson (Get-Content -LiteralPath $receiptPath -Raw)
            $matching = $registry.events | Where-Object eventHash -eq $workspaceReceipt.registryEvent.eventHash | Select-Object -First 1
            if ($null -eq $matching -or (Get-Hash (Get-EventPayload $matching)) -cne (Get-Hash (Get-EventPayload $workspaceReceipt.registryEvent))) { $issues.Add('Workspace instruction outcome does not match the registry.') }
        }
    }
    $result = [pscustomobject][ordered]@{ action = 'verify'; valid = $issues.Count -eq 0; issues = @($issues); registryFingerprint = $validation.registryFingerprint; headHash = $validation.headHash; outcome = $workspaceReceipt }
} elseif ($Action -eq 'metrics') {
    $profiles = Get-Profiles $registry
    $result = [pscustomobject][ordered]@{
        action = 'metrics'; valid = $validation.valid; issues = @($validation.issues)
        metrics = [pscustomobject][ordered]@{
            schemaVersion = 1
            validEventCount = @($registry.events).Count
            profileCount = $profiles.Count
            degradedProfileCount = @($profiles | Where-Object health -eq 'degraded').Count
            registryFingerprint = $validation.registryFingerprint
            profiles = $profiles
        }
    }
} elseif ($Action -eq 'candidates') {
    $profiles = Get-Profiles $registry
    $candidates = @($profiles | Where-Object health -eq 'degraded' | ForEach-Object {
        $profile = $_
        $absolute = Join-Path $repositoryRoot $profile.path
        $current = if (Test-Path -LiteralPath $absolute -PathType Leaf) { Get-FileSha $absolute } else { '' }
        [pscustomobject][ordered]@{
            id = "instruction-$((Get-Hash "$($profile.path)|$($profile.fingerprint)").Substring(0, 16))"
            type = 'instruction-effectiveness'
            path = $profile.path
            observedFingerprint = $profile.fingerprint
            currentFingerprint = $current
            current = $current -ceq $profile.fingerprint
            score = [int]$outcomePolicy.candidateScore
            statement = "Review '$($profile.path)' because tasks using this instruction version show degraded outcomes."
            evidence = @("samples=$($profile.sampleCount)", "recent-score=$($profile.recentAverageOutcomeScore)", "recent-success=$($profile.recentSuccessRatePercent)%", @($profile.degradationReasons))
            recommendedWorkflow = 'learning-shadow'
        }
    })
    $result = [pscustomobject][ordered]@{ action = 'candidates'; valid = $validation.valid; issues = @($validation.issues); registryFingerprint = $validation.registryFingerprint; eligibleCount = @($candidates | Where-Object current).Count; candidates = $candidates }
} else {
    $result = [pscustomobject][ordered]@{ action = 'list'; valid = $validation.valid; issues = @($validation.issues); registryFingerprint = $validation.registryFingerprint; events = @($registry.events) }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    Write-Host "Instruction outcomes: action=$Action, valid=$($result.valid), registry=$($validation.registryFingerprint)"
    if ($null -ne $result.metrics) {
        Write-Host "Events=$($result.metrics.validEventCount), profiles=$($result.metrics.profileCount), degraded=$($result.metrics.degradedProfileCount)"
    }
    foreach ($candidate in @($result.candidates)) { Write-Host " - [$($candidate.score)] $($candidate.path): current=$($candidate.current)" }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
