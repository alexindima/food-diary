[CmdletBinding()]
param(
    [string]$TasksPath = '.artifacts/llm-wiki/tasks',
    [switch]$FailOnGap,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$policy = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | ConvertFrom-Json
$registry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAgentRegistry.ps1') list -Format Json | ConvertFrom-Json
$schedule = & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskSchedule.ps1') -TasksPath $TasksPath -Format Json | ConvertFrom-Json
$capabilities = [System.Collections.Generic.List[object]]::new()

foreach ($capability in @($policy.scheduler.agentRegistry.allowedCapabilities | Where-Object { $_ -ne 'generalist' })) {
    $supportingAgents = @($registry.agents | Where-Object { 'generalist' -in @($_.capabilities) -or $capability -in @($_.capabilities) })
    $demandTasks = @($schedule.tasks | Where-Object { $capability -in @($_.requiredCapabilities) })
    $capabilities.Add([pscustomobject][ordered]@{
        capability = [string]$capability
        demandTaskCount = $demandTasks.Count
        demandTasks = @($demandTasks | ForEach-Object { $_.name })
        agentCount = $supportingAgents.Count
        totalCapacity = [int](($supportingAgents | ForEach-Object { $_.capacity } | Measure-Object -Sum).Sum)
        availableCapacity = [int](($supportingAgents | ForEach-Object { $_.availableCapacity } | Measure-Object -Sum).Sum)
        owners = @($supportingAgents | ForEach-Object { $_.owner })
        gap = $demandTasks.Count -gt 0 -and $supportingAgents.Count -eq 0
        constrained = $demandTasks.Count -gt 0 -and $supportingAgents.Count -eq 1
    })
}

$taskGaps = @($schedule.tasks | ForEach-Object {
    $task = $_
    $missing = @($task.requiredCapabilities | Where-Object {
        $required = $_
        $null -eq ($registry.agents | Where-Object { 'generalist' -in @($_.capabilities) -or $required -in @($_.capabilities) } | Select-Object -First 1)
    })
    if ($missing.Count -gt 0) {
        [pscustomobject][ordered]@{
            name = $task.name
            path = $task.path
            state = $task.state
            requiredCapabilities = @($task.requiredCapabilities)
            missingCapabilities = $missing
        }
    }
})
$gapCapabilities = @($capabilities | Where-Object gap)
$constrainedCapabilities = @($capabilities | Where-Object constrained)
$response = [pscustomobject][ordered]@{
    schemaVersion = 1
    tasksPath = $TasksPath
    routingMode = $schedule.routingMode
    activeAgentCount = [int]$registry.activeCount
    totalCapacity = [int]$registry.totalCapacity
    availableCapacity = [int]$registry.availableCapacity
    taskCount = @($schedule.tasks).Count
    gapCapabilityCount = $gapCapabilities.Count
    constrainedCapabilityCount = $constrainedCapabilities.Count
    taskGapCount = $taskGaps.Count
    valid = $taskGaps.Count -eq 0
    gapCapabilities = @($gapCapabilities | ForEach-Object { $_.capability })
    constrainedCapabilities = @($constrainedCapabilities | ForEach-Object { $_.capability })
    capabilities = @($capabilities)
    taskGaps = $taskGaps
    agents = @($registry.agents)
}

if ($Format -eq 'Json') {
    $response | ConvertTo-Json -Depth 12
} else {
    Write-Host "AI fleet coverage: agents=$($response.activeAgentCount), capacity=$($response.availableCapacity)/$($response.totalCapacity), task gaps=$($response.taskGapCount)"
    foreach ($item in $capabilities | Where-Object { $_.demandTaskCount -gt 0 }) {
        Write-Host " - $($item.capability): demand=$($item.demandTaskCount), agents=$($item.agentCount), available=$($item.availableCapacity), gap=$($item.gap)"
    }
    foreach ($taskGap in $taskGaps) { Write-Host " ! $($taskGap.name): missing $(@($taskGap.missingCapabilities) -join ', ')" }
}
if ($FailOnGap -and -not $response.valid) { exit 1 }
