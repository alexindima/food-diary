[CmdletBinding()]
param(
    [string]$TasksPath = '.artifacts/llm-wiki/tasks',
    [Nullable[int]]$MaxConcurrency,
    [string]$AgentId,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnBlocked,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$now = $AsOfUtc.ToUniversalTime()
$policy = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | ConvertFrom-Json
$effectiveConcurrency = if ($null -ne $MaxConcurrency) { [int]$MaxConcurrency } else { [int]$policy.scheduler.defaultConcurrency }
if ($effectiveConcurrency -lt 1 -or $effectiveConcurrency -gt [int]$policy.scheduler.maximumConcurrency) {
    throw "MaxConcurrency must be between 1 and $($policy.scheduler.maximumConcurrency)."
}
$graph = & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskGraph.ps1') -TasksPath $TasksPath -Format Json | ConvertFrom-Json
$leaseRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
$dispatchRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
$dispatchMetrics = & (Join-Path $PSScriptRoot 'Get-LlmWikiDispatchMetrics.ps1') -AsOfUtc $now -Format Json | ConvertFrom-Json
$contextFeedback = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextFeedback.ps1') metrics -AsOfUtc $now -Format Json | ConvertFrom-Json
$agentRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAgentRegistry.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
$circuitRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiWorkspaceCircuit.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
$openCircuitByWorkspace = @{}
foreach ($circuit in @($circuitRegistry.circuits | Where-Object open)) { $openCircuitByWorkspace[[string]$circuit.workspace] = $circuit }
$availableAgents = @($agentRegistry.agents | Where-Object active)
if (-not [string]::IsNullOrWhiteSpace($AgentId)) {
    $availableAgents = @($availableAgents | Where-Object agentId -eq $AgentId)
    if ($availableAgents.Count -eq 0) { throw "Active agent registration was not found: $AgentId" }
}
$capabilityRoutingEnabled = $agentRegistry.activeCount -gt 0
$activeLeaseByWorkspace = @{}
foreach ($lease in @($leaseRegistry.leases | Where-Object active)) { $activeLeaseByWorkspace[[string]$lease.workspace] = $lease }
$items = [System.Collections.Generic.List[object]]::new()

foreach ($node in @($graph.nodes | Sort-Object name)) {
    $reasons = [System.Collections.Generic.List[string]]::new()
    $workspacePath = [string]$node.path
    $doctor = $null
    try {
        $doctor = & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
    } catch {
        $reasons.Add("Doctor failed: $($_.Exception.Message)")
    }
    if ($null -ne $doctor -and -not $doctor.valid) { $reasons.Add("Workspace doctor reports $($doctor.errorCount) error(s).") }
    $journal = $null
    try {
        $journal = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') show -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
        if ($journal.openBlockerCount -gt 0) { $reasons.Add("Task journal has $($journal.openBlockerCount) open blocker(s).") }
    } catch {
        $reasons.Add("Journal is unavailable: $($_.Exception.Message)")
    }
    if ($node.blockingConflictCount -gt 0) { $reasons.Add("Task graph has $($node.blockingConflictCount) blocking conflict(s).") }
    if ($node.name -in @($graph.cycleNodes)) { $reasons.Add('Task participates in a dependency cycle.') }
    $circuit = if ($openCircuitByWorkspace.ContainsKey($workspacePath)) { $openCircuitByWorkspace[$workspacePath] } else { $null }
    if ($null -ne $circuit) { $reasons.Add("Workspace circuit is open until $($circuit.openUntilUtc): $($circuit.reason)") }
    $prerequisites = @($node.prerequisiteTasks)
    if ($prerequisites.Count -gt 0) { $reasons.Add("Waiting for prerequisite task(s): $($prerequisites -join ', ').") }
    $lease = if ($activeLeaseByWorkspace.ContainsKey($workspacePath)) { $activeLeaseByWorkspace[$workspacePath] } else { $null }
    $dispatch = $dispatchRegistry.dispatches | Where-Object { $_.workspace -eq $workspacePath -and $_.state -in @('running', 'orphaned', 'packet-drift', 'context-drift', 'invalid') } | Sort-Object startedAtUtc -Descending | Select-Object -First 1
    if ($null -ne $dispatch -and $dispatch.state -in @('orphaned', 'packet-drift', 'context-drift', 'invalid')) {
        $reasons.Add("Dispatch $($dispatch.dispatchId) requires attention: $($dispatch.state).")
    }
    $state = if ($null -ne $lease -and ($null -eq $dispatch -or $dispatch.state -eq 'running')) { 'running' } elseif ($reasons.Count -gt 0) { 'blocked' } else { 'ready' }
    $priorityScore = 100 +
        (@($node.dependentTasks).Count * 25) +
        (@($node.contractRules).Count * 15) +
        ($(if (@($node.scopes) -contains 'Api' -or @($node.scopes) -contains 'Contracts') { 10 } else { 0 })) -
        [Math]::Min(40, [Math]::Floor(@($node.changedPaths).Count / 10))
    $requiredCapabilities = [System.Collections.Generic.List[string]]::new()
    foreach ($scope in @($node.scopes)) {
        switch -Regex ([string]$scope) {
            '^(Api|Contracts)$' { $requiredCapabilities.Add('api'); $requiredCapabilities.Add('backend') }
            '^Backend$' { $requiredCapabilities.Add('backend') }
            '^Frontend$' { $requiredCapabilities.Add('frontend') }
            '^Database$' { $requiredCapabilities.Add('database') }
            '^Tests$' { $requiredCapabilities.Add('tests') }
        }
    }
    foreach ($changedPath in @($node.changedPaths)) {
        $pathValue = [string]$changedPath
        if ($pathValue -match '(^|/)(tests?|.*Tests?)(/|$)') { $requiredCapabilities.Add('tests') }
        if ($pathValue -match 'Infrastructure|Initializer|JobManager|\\.github/workflows') { $requiredCapabilities.Add('infrastructure') }
        if ($pathValue -match '(?i)security|auth|identity|credential|privacy') { $requiredCapabilities.Add('security') }
        if ($pathValue -match 'assets/i18n|FoodDiary\.Resources') { $requiredCapabilities.Add('localization') }
        if ($pathValue -match '^(docs|\.llm-wiki)/') { $requiredCapabilities.Add('docs') }
    }
    $requiredCapabilities = @($requiredCapabilities | Select-Object -Unique | Sort-Object)
    $packetPath = Join-Path (Join-Path $repositoryRoot $workspacePath) 'change-packet.json'
    $packet = if (Test-Path -LiteralPath $packetPath -PathType Leaf) { Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json } else { $null }
    $riskCalibrationPath = Join-Path (Join-Path $repositoryRoot $workspacePath) 'risk-calibration.json'
    $riskLevel = if (Test-Path -LiteralPath $riskCalibrationPath -PathType Leaf) {
        [string](Get-Content -LiteralPath $riskCalibrationPath -Raw | ConvertFrom-Json).level
    } elseif ($null -ne $packet) {
        [string]$packet.brief.risk.level
    } else { 'low' }
    $modelRoutingPath = Join-Path (Join-Path $repositoryRoot $workspacePath) 'model-routing.json'
    $modelRouting = $null
    if (Test-Path -LiteralPath $modelRoutingPath -PathType Leaf) {
        try {
            $modelRouteCheck = & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRouting.ps1') verify `
                -WorkspacePath $workspacePath `
                -Format Json | ConvertFrom-Json
            if (-not $modelRouteCheck.valid) {
                $reasons.Add("Model routing receipt is invalid: $(@($modelRouteCheck.issues) -join ' ')")
            } else {
                $modelRouting = $modelRouteCheck.route.recommendation
            }
        } catch {
            $reasons.Add("Model routing verification failed: $($_.Exception.Message)")
        }
    }
    if ($state -eq 'ready' -and $reasons.Count -gt 0) { $state = 'blocked' }
    $items.Add([pscustomobject][ordered]@{
        name = [string]$node.name
        path = $workspacePath
        objective = [string]$node.objective
        state = $state
        priorityScore = [int]$priorityScore
        changedPathCount = @($node.changedPaths).Count
        prerequisites = $prerequisites
        dependents = @($node.dependentTasks)
        blockingConflictCount = [int]$node.blockingConflictCount
        reasons = @($reasons)
        lease = $lease
        dispatch = $dispatch
        circuit = $circuit
        requiredCapabilities = $requiredCapabilities
        riskLevel = $riskLevel
        modelRouting = $modelRouting
        assignedAgent = $null
        assignmentRationale = $null
        dispatchCommand = ''
        selected = $false
        lane = $null
        acquireCommand = ''
    })
}

$running = @($items | Where-Object state -eq 'running')
$capacity = [Math]::Max(0, $effectiveConcurrency - $running.Count)
$selected = [System.Collections.Generic.List[object]]::new()
$assignedByAgent = @{}
$coordinationTypes = @('boundary-coordination', 'generated-artifact-coordination')
$runningNames = @($running.name)
foreach ($candidate in @($items | Where-Object state -eq 'ready' | Sort-Object @{ Expression = 'priorityScore'; Descending = $true }, name)) {
    if ($selected.Count -ge $capacity) { break }
    $parallelNames = @($runningNames + @($selected.name))
    $coordinationEdge = $graph.edges | Where-Object {
        $_.type -in $coordinationTypes -and
        (($_.left -eq $candidate.name -and $_.right -in $parallelNames) -or ($_.right -eq $candidate.name -and $_.left -in $parallelNames))
    } | Select-Object -First 1
    if ($null -ne $coordinationEdge) {
        $candidate.state = 'waiting-coordination'
        $candidate.reasons = @($candidate.reasons) + "Cannot run beside '$($(if ($coordinationEdge.left -eq $candidate.name) { $coordinationEdge.right } else { $coordinationEdge.left }))' because of $($coordinationEdge.type)."
        continue
    }
    $assignedAgent = $null
    if ($capabilityRoutingEnabled) {
        $eligibleAgents = @($availableAgents | Where-Object {
            $agent = $_
            $alreadyAssigned = if ($assignedByAgent.ContainsKey([string]$agent.agentId)) { [int]$assignedByAgent[[string]$agent.agentId] } else { 0 }
            $hasCapacity = [int]$agent.availableCapacity - $alreadyAssigned -gt 0
            $capabilities = @($agent.capabilities)
            $missing = @($candidate.requiredCapabilities | Where-Object { $_ -notin $capabilities -and 'generalist' -notin $capabilities })
            $hasCapacity -and $missing.Count -eq 0
        })
        $rankedAgents = @($eligibleAgents | ForEach-Object {
            $agent = $_
            $alreadyAssigned = if ($assignedByAgent.ContainsKey([string]$agent.agentId)) { [int]$assignedByAgent[[string]$agent.agentId] } else { 0 }
            $ownerMetrics = $dispatchMetrics.owners | Where-Object owner -eq $agent.owner | Select-Object -First 1
            $reliabilitySamples = if ($null -ne $ownerMetrics) { [int]$ownerMetrics.terminalCount } else { 0 }
            $coldStart = [double]$policy.scheduler.agentRegistry.routing.coldStartScore
            $routing = $policy.scheduler.agentRegistry.routing
            $ownerSuccessScore = if ($reliabilitySamples -ge [int]$routing.minimumReliabilitySamples) { [double]$ownerMetrics.successRatePercent } else { $coldStart }
            $ownerHeartbeatScore = if ($null -ne $ownerMetrics -and [int]$ownerMetrics.dispatchCount -ge [int]$routing.minimumReliabilitySamples) { [double]$ownerMetrics.heartbeatCoveragePercent } else { $coldStart }
            $relevantCapabilityProfiles = @($dispatchMetrics.capabilityProfiles | Where-Object {
                $_.owner -eq $agent.owner -and $_.capability -in @($candidate.requiredCapabilities)
            })
            $qualifiedCapabilityProfiles = @($relevantCapabilityProfiles | Where-Object { [int]$_.terminalCount -ge [int]$routing.minimumCapabilitySamples })
            $capabilityCoverageComplete = @($candidate.requiredCapabilities).Count -gt 0 -and $qualifiedCapabilityProfiles.Count -eq @($candidate.requiredCapabilities).Count
            $capabilityTerminalSamples = @($qualifiedCapabilityProfiles | Measure-Object terminalCount -Sum).Sum
            $capabilityCompletedSamples = @($qualifiedCapabilityProfiles | Measure-Object completedCount -Sum).Sum
            $capabilityDispatchSamples = @($qualifiedCapabilityProfiles | Measure-Object dispatchCount -Sum).Sum
            $capabilityHeartbeatSamples = @($qualifiedCapabilityProfiles | ForEach-Object {
                [Math]::Round(([double]$_.heartbeatCoveragePercent * [int]$_.dispatchCount) / 100.0, 4)
            } | Measure-Object -Sum).Sum
            $capabilitySuccessScore = if ($capabilityCoverageComplete -and $capabilityTerminalSamples -gt 0) { [Math]::Round(($capabilityCompletedSamples * 100.0) / $capabilityTerminalSamples, 2) } else { $null }
            $capabilityHeartbeatScore = if ($capabilityCoverageComplete -and $capabilityDispatchSamples -gt 0) { [Math]::Round(($capabilityHeartbeatSamples * 100.0) / $capabilityDispatchSamples, 2) } else { $null }
            $blend = [double]$routing.capabilityReliabilityBlendPercent / 100.0
            $successScore = if ($null -ne $capabilitySuccessScore) { [Math]::Round(($capabilitySuccessScore * $blend) + ($ownerSuccessScore * (1 - $blend)), 2) } else { $ownerSuccessScore }
            $heartbeatScore = if ($null -ne $capabilityHeartbeatScore) { [Math]::Round(($capabilityHeartbeatScore * $blend) + ($ownerHeartbeatScore * (1 - $blend)), 2) } else { $ownerHeartbeatScore }
            $capabilityDurationWeighted = @($qualifiedCapabilityProfiles | ForEach-Object {
                if ($null -ne $_.averageDurationMinutes) { [double]$_.averageDurationMinutes * [int]$_.terminalCount }
            } | Measure-Object -Sum).Sum
            $averageDurationMinutes = if ($capabilityCoverageComplete -and $capabilityTerminalSamples -gt 0) {
                [Math]::Round($capabilityDurationWeighted / $capabilityTerminalSamples, 2)
            } elseif ($reliabilitySamples -ge [int]$routing.minimumReliabilitySamples -and $null -ne $ownerMetrics.averageDurationMinutes) {
                [double]$ownerMetrics.averageDurationMinutes
            } else { $null }
            $durationScore = if ($null -eq $averageDurationMinutes) { $coldStart } else {
                [Math]::Round([Math]::Max(0, 100 - (($averageDurationMinutes * 100.0) / [double]$policy.scheduler.slo.maximumP95DurationMinutes)), 2)
            }
            $remainingCapacity = [Math]::Max(0, [int]$agent.availableCapacity - $alreadyAssigned)
            $capacityScore = [Math]::Round(($remainingCapacity * 100.0) / [Math]::Max(1, [int]$agent.capacity), 2)
            $agentCapabilities = @($agent.capabilities)
            $extraCapabilities = @($agentCapabilities | Where-Object { $_ -notin @($candidate.requiredCapabilities) }).Count
            $specializationScore = if ('generalist' -in $agentCapabilities) { 50 } else { [Math]::Max(0, 100 - ($extraCapabilities * 10)) }
            $historicalDispatches = if ($null -ne $ownerMetrics) { [int]$ownerMetrics.dispatchCount } else { 0 }
            $fairnessScore = [Math]::Round(100.0 / (1 + $historicalDispatches + $alreadyAssigned), 2)
            $ownerQuality = $contextFeedback.metrics.ownerQualityProfiles | Where-Object owner -eq $agent.owner | Select-Object -First 1
            $qualityProfiles = @($contextFeedback.metrics.capabilityQualityProfiles | Where-Object {
                $_.owner -eq $agent.owner -and $_.capability -in @($candidate.requiredCapabilities) -and
                [int]$_.sampleCount -ge [int]$routing.minimumCapabilitySamples
            })
            $qualityCoverageComplete = @($candidate.requiredCapabilities).Count -gt 0 -and $qualityProfiles.Count -eq @($candidate.requiredCapabilities).Count
            $qualityScore = if ($qualityCoverageComplete) {
                [Math]::Round((@($qualityProfiles.averageQualityScore) | Measure-Object -Average).Average, 2)
            } elseif ($null -ne $ownerQuality -and [int]$ownerQuality.sampleCount -ge [int]$routing.minimumReliabilitySamples) {
                [double]$ownerQuality.averageQualityScore
            } else { $coldStart }
            $score = [Math]::Round((
                ($successScore * [double]$routing.successWeight) +
                ($heartbeatScore * [double]$routing.heartbeatWeight) +
                ($durationScore * [double]$routing.durationWeight) +
                ($qualityScore * [double]$routing.qualityWeight) +
                ($capacityScore * [double]$routing.availableCapacityWeight) +
                ($specializationScore * [double]$routing.specializationWeight) +
                ($fairnessScore * [double]$routing.fairnessWeight)
            ) / 100.0, 2)
            $riskReliabilityWeight = if ($candidate.riskLevel -eq 'critical') {
                [double]$routing.criticalRiskReliabilityWeight
            } elseif ($candidate.riskLevel -eq 'high') {
                [double]$routing.highRiskReliabilityWeight
            } else { 0.0 }
            $riskReliabilityScore = [Math]::Round(($successScore + $qualityScore) / 2.0, 2)
            if ($riskReliabilityWeight -gt 0) {
                $score = [Math]::Round((($score * (100 - $riskReliabilityWeight)) + ($riskReliabilityScore * $riskReliabilityWeight)) / 100.0, 2)
            }
            [pscustomobject][ordered]@{
                agent = $agent
                score = $score
                components = [pscustomobject][ordered]@{
                    success = $successScore
                    heartbeat = $heartbeatScore
                    duration = $durationScore
                    quality = $qualityScore
                    availableCapacity = $capacityScore
                    specialization = $specializationScore
                    fairness = $fairnessScore
                    reliabilitySamples = $reliabilitySamples
                    historicalDispatches = $historicalDispatches
                    averageDurationMinutes = $averageDurationMinutes
                    capabilityProfileUsed = $capabilityCoverageComplete
                    capabilityProfileCount = $qualifiedCapabilityProfiles.Count
                    capabilityTerminalSamples = $capabilityTerminalSamples
                    capabilitySuccess = $capabilitySuccessScore
                    capabilityHeartbeat = $capabilityHeartbeatScore
                    qualityProfileUsed = $qualityCoverageComplete
                    qualityProfileCount = $qualityProfiles.Count
                    riskLevel = $candidate.riskLevel
                    riskAware = $riskReliabilityWeight -gt 0
                    riskReliabilityWeight = $riskReliabilityWeight
                    riskReliabilityScore = $riskReliabilityScore
                }
            }
        } | Sort-Object @{ Expression = 'score'; Descending = $true }, @{ Expression = { $_.agent.owner }; Descending = $false })
        $selectedRanking = $rankedAgents | Select-Object -First 1
        $assignedAgent = if ($null -ne $selectedRanking) { $selectedRanking.agent } else { $null }
        if ($null -eq $assignedAgent) {
            $candidate.state = 'waiting-capability'
            $candidate.reasons = @($candidate.reasons) + "No active agent has capacity and required capabilities: $(@($candidate.requiredCapabilities) -join ', ')."
            continue
        }
        $candidate.assignmentRationale = [pscustomobject][ordered]@{
            selectedScore = $selectedRanking.score
            selectedComponents = $selectedRanking.components
            evaluatedAgentCount = $rankedAgents.Count
            rankings = @($rankedAgents | ForEach-Object {
                [pscustomobject][ordered]@{
                    agentId = $_.agent.agentId
                    owner = $_.agent.owner
                    score = $_.score
                    components = $_.components
                }
            })
            weights = $policy.scheduler.agentRegistry.routing
            modelRouting = $candidate.modelRouting
        }
    }
    $candidate.selected = $true
    $candidate.lane = $running.Count + $selected.Count + 1
    $candidate.assignedAgent = $assignedAgent
    $selectedOwner = if ($null -ne $assignedAgent) { [string]$assignedAgent.owner } else { '<agent>' }
    if ($null -ne $assignedAgent) {
        $assignedByAgent[[string]$assignedAgent.agentId] = $(if ($assignedByAgent.ContainsKey([string]$assignedAgent.agentId)) { [int]$assignedByAgent[[string]$assignedAgent.agentId] + 1 } else { 1 })
    }
    $candidate.acquireCommand = "./.llm-wiki/wiki.ps1 task-lease-acquire -WorkspacePath $($candidate.path) -Owner $selectedOwner"
    $agentArgument = if ($null -ne $assignedAgent) { " -AgentId $($assignedAgent.agentId)" } else { '' }
    $scoreArgument = if ($null -ne $candidate.assignmentRationale) { " -RoutingScore $($candidate.assignmentRationale.selectedScore)" } else { '' }
    $candidate.dispatchCommand = "./.llm-wiki/wiki.ps1 task-dispatch-start -WorkspacePath $($candidate.path) -Owner $selectedOwner -Lane $($candidate.lane)$agentArgument$scoreArgument"
    $selected.Add($candidate)
}
$readyNotSelected = @($items | Where-Object { $_.state -eq 'ready' -and -not $_.selected })
foreach ($candidate in $readyNotSelected) {
    $candidate.reasons = @($candidate.reasons) + 'Concurrency capacity is exhausted for this scheduling cycle.'
}
$blocked = @($items | Where-Object state -in @('blocked', 'waiting-coordination', 'waiting-capability'))
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    asOfUtc = $now.ToString('o')
    tasksPath = $TasksPath
    maxConcurrency = $effectiveConcurrency
    runningCount = $running.Count
    availableCapacity = $capacity
    selectedCount = $selected.Count
    readyButUnscheduledCount = $readyNotSelected.Count
    blockedCount = $blocked.Count
    graphValid = [bool]$graph.valid
    routingMode = $(if ($capabilityRoutingEnabled) { 'capability-aware' } else { 'unregistered-fallback' })
    agentRegistry = $agentRegistry
    circuitRegistry = [pscustomobject][ordered]@{
        openCount = [int]$circuitRegistry.openCount
        invalidReceiptCount = [int]$circuitRegistry.invalidReceiptCount
    }
    selectedTasks = @($selected)
    runningTasks = $running
    blockedTasks = $blocked
    tasks = @($items)
    mergeWaves = @($graph.mergeWaves)
    dispatchMetrics = [pscustomobject][ordered]@{
        windowDays = $dispatchMetrics.windowDays
        terminalCount = $dispatchMetrics.terminalCount
        successRatePercent = $dispatchMetrics.successRatePercent
        reconciliationRatePercent = $dispatchMetrics.reconciliationRatePercent
        heartbeatCoveragePercent = $dispatchMetrics.heartbeatCoveragePercent
        sloVerdict = $dispatchMetrics.slo.verdict
        sloViolationCount = $dispatchMetrics.slo.violationCount
        attentionCount = $dispatchMetrics.attentionCount
    }
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 12
} else {
    Write-Host "Task schedule: running=$($result.runningCount), selected=$($result.selectedCount), blocked=$($result.blockedCount), concurrency=$effectiveConcurrency"
    foreach ($item in $selected) { Write-Host " - lane $($item.lane): $($item.name) (priority $($item.priorityScore))" }
    foreach ($item in $blocked) { Write-Host " - blocked $($item.name): $(@($item.reasons) -join ' ')" }
}
if ($FailOnBlocked -and ($blocked.Count -gt 0 -or -not $graph.valid)) { exit 1 }
