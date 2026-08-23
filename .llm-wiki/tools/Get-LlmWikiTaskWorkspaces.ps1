[CmdletBinding()]
param(
    [string]$TasksPath = '.artifacts/llm-wiki/tasks',
    [switch]$Detailed,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$workspacePolicy = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | ConvertFrom-Json

if ([System.IO.Path]::IsPathRooted($TasksPath)) { throw 'TasksPath must be repository-relative.' }
$normalizedTasksPath = $TasksPath.Replace('\', '/').TrimEnd('/')
if ($normalizedTasksPath -notmatch '^\.artifacts/llm-wiki/tasks(?:/.*)?$') {
    throw 'TasksPath must be inside .artifacts/llm-wiki/tasks.'
}
$absoluteTasksPath = Join-Path $repositoryRoot $normalizedTasksPath

$items = [System.Collections.Generic.List[object]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
if (Test-Path -LiteralPath $absoluteTasksPath -PathType Container) {
    foreach ($directory in @(Get-ChildItem -LiteralPath $absoluteTasksPath -Directory -Force | Sort-Object Name)) {
        if ($directory.Name -like '.task-start-*' -or $directory.Name -like "$($workspacePolicy.import.stagingPrefix)*") {
            $warnings.Add("Abandoned staging workspace: $normalizedTasksPath/$($directory.Name)")
            continue
        }
        $relativeWorkspacePath = "$normalizedTasksPath/$($directory.Name)"
        $descriptorPath = Join-Path $directory.FullName 'workspace.json'
        $requiredFiles = @(
            'workspace.json'
            'change-packet.json'
            'task-contract.json'
            'change-manifest.json'
            'acceptance-matrix.json'
            'evidence.json'
            'review-report.md'
        )
        $missingFiles = @($requiredFiles | Where-Object {
            -not (Test-Path -LiteralPath (Join-Path $directory.FullName $_) -PathType Leaf)
        })
        if ($missingFiles.Count -gt 0) {
            $items.Add([pscustomobject][ordered]@{
                name = $directory.Name
                path = $relativeWorkspacePath
                state = 'incomplete'
                objective = ''
                readiness = $null
                score = $null
                changedPathCount = $null
                pendingCriteria = $null
                unresolvedChecks = $null
                unresolvedReviews = $null
                openJournalBlockers = $null
                lastActivityUtc = $directory.LastWriteTimeUtc.ToString('o')
                issues = @("Missing: $($missingFiles -join ', ')")
            })
            continue
        }

        try {
            $descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json
            $packet = Get-Content -LiteralPath (Join-Path $directory.FullName 'change-packet.json') -Raw | ConvertFrom-Json
            $acceptance = Get-Content -LiteralPath (Join-Path $directory.FullName 'acceptance-matrix.json') -Raw | ConvertFrom-Json
            $evidence = Get-Content -LiteralPath (Join-Path $directory.FullName 'evidence.json') -Raw | ConvertFrom-Json
            $completionPath = Join-Path $directory.FullName 'completion.json'
            $doctor = & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') `
                -WorkspacePath $relativeWorkspacePath `
                -Format Json | ConvertFrom-Json
            $journal = if (Test-Path -LiteralPath (Join-Path $directory.FullName 'journal.json') -PathType Leaf) {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') show `
                    -WorkspacePath $relativeWorkspacePath `
                    -Format Json | ConvertFrom-Json
            } else {
                [pscustomobject]@{ openBlockerCount = $null }
            }
            $state = if ($doctor.valid) {
                'in-progress'
            } elseif ($doctor.migrationRequired) {
                'migration-required'
            } elseif ($doctor.policyDrift) {
                'policy-drift'
            } else {
                'incomplete'
            }
            if ($null -ne $descriptor.decomposition -and [string]$descriptor.decomposition.state -eq 'applied') { $state = 'decomposed' }
            $issues = @($doctor.errors)
            if (Test-Path -LiteralPath $completionPath -PathType Leaf) {
                $state = if ($doctor.valid) {
                    'sealed'
                } elseif ($doctor.policyDrift -and @($doctor.checks | Where-Object { $_.status -eq 'fail' -and $_.id -ne 'policy-fingerprint' }).Count -eq 0) {
                    'policy-drift'
                } else {
                    'invalid-seal'
                }
            }
            $readiness = $null
            $score = $null
            if ($Detailed -and $state -eq 'in-progress') {
                $status = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') status `
                    -WorkspacePath $relativeWorkspacePath `
                    -Format Json | ConvertFrom-Json
                $readiness = $status.verdict
                $score = $status.score
                $issues = @($status.nextActions)
            } elseif ($state -eq 'sealed') {
                $completion = Get-Content -LiteralPath $completionPath -Raw | ConvertFrom-Json
                $readiness = $completion.readiness.verdict
                $score = $completion.readiness.score
            }
            $latestWrite = @(
                Get-ChildItem -LiteralPath $directory.FullName -File -Recurse |
                    Sort-Object LastWriteTimeUtc -Descending |
                    Select-Object -First 1
            )
            $items.Add([pscustomobject][ordered]@{
                name = $directory.Name
                path = $relativeWorkspacePath
                state = $state
                objective = [string]$descriptor.objective
                readiness = $readiness
                score = $score
                changedPathCount = @($packet.diff.changedPaths).Count
                pendingCriteria = @($acceptance.criteria | Where-Object status -eq 'pending').Count
                unresolvedChecks = @($evidence.checks | Where-Object status -notin @('passed', 'passed-with-known-baseline-failures', 'not-applicable')).Count
                unresolvedReviews = @($evidence.reviews | Where-Object status -notin @('completed', 'not-applicable')).Count
                openJournalBlockers = $journal.openBlockerCount
                policyChangeCount = $(if ($null -ne $doctor.policyImpact) { $doctor.policyImpact.changeCount } else { $null })
                policyAffectingChangeCount = $(if ($null -ne $doctor.policyImpact) { $doctor.policyImpact.affectingChangeCount } else { $null })
                policyHighestSeverity = $(if ($null -ne $doctor.policyImpact) { $doctor.policyImpact.highestSeverity } else { $null })
                lastActivityUtc = $(if ($latestWrite.Count -gt 0) { $latestWrite[0].LastWriteTimeUtc.ToString('o') } else { $directory.LastWriteTimeUtc.ToString('o') })
                issues = @($issues)
            })
        } catch {
            $items.Add([pscustomobject][ordered]@{
                name = $directory.Name
                path = $relativeWorkspacePath
                state = 'incomplete'
                objective = ''
                readiness = $null
                score = $null
                changedPathCount = $null
                pendingCriteria = $null
                unresolvedChecks = $null
                unresolvedReviews = $null
                openJournalBlockers = $null
                lastActivityUtc = $directory.LastWriteTimeUtc.ToString('o')
                issues = @($_.Exception.Message)
            })
        }
    }
}

$taskGraph = & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskGraph.ps1') -TasksPath $normalizedTasksPath -Format Json | ConvertFrom-Json
$taskLeases = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -Format Json | ConvertFrom-Json
$taskDispatches = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') list -Format Json | ConvertFrom-Json
$agentRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAgentRegistry.ps1') list -Format Json | ConvertFrom-Json
$circuitRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiWorkspaceCircuit.ps1') list -Format Json | ConvertFrom-Json
foreach ($item in $items) {
    $graphNode = $taskGraph.nodes | Where-Object name -eq $item.name | Select-Object -First 1
    $item | Add-Member -NotePropertyName graphEdgeCount -NotePropertyValue $(if ($null -ne $graphNode) { [int]$graphNode.edgeCount } else { 0 })
    $item | Add-Member -NotePropertyName blockingConflictCount -NotePropertyValue $(if ($null -ne $graphNode) { [int]$graphNode.blockingConflictCount } else { 0 })
    $item | Add-Member -NotePropertyName prerequisiteTasks -NotePropertyValue @(if ($null -ne $graphNode) { @($graphNode.prerequisiteTasks) } else { @() })
    $item | Add-Member -NotePropertyName dependentTasks -NotePropertyValue @(if ($null -ne $graphNode) { @($graphNode.dependentTasks) } else { @() })
    $activeLease = $taskLeases.leases | Where-Object { $_.active -and $_.workspace -eq $item.path } | Select-Object -First 1
    $item | Add-Member -NotePropertyName lease -NotePropertyValue $activeLease
    $item | Add-Member -NotePropertyName leased -NotePropertyValue ($null -ne $activeLease)
    $workspaceDispatches = @($taskDispatches.dispatches | Where-Object workspace -eq $item.path | Sort-Object startedAtUtc -Descending)
    $activeDispatch = $workspaceDispatches | Where-Object state -in @('running', 'orphaned', 'packet-drift', 'context-drift', 'invalid') | Select-Object -First 1
    $item | Add-Member -NotePropertyName dispatch -NotePropertyValue $activeDispatch
    $item | Add-Member -NotePropertyName dispatchState -NotePropertyValue $(if ($null -ne $activeDispatch) { [string]$activeDispatch.state } else { 'none' })
    $item | Add-Member -NotePropertyName dispatchHistoryCount -NotePropertyValue $workspaceDispatches.Count
    $activeOwner = if ($null -ne $activeDispatch) { [string]$activeDispatch.owner } elseif ($null -ne $activeLease) { [string]$activeLease.owner } else { '' }
    $registeredAgent = $agentRegistry.agents | Where-Object { $_.active -and $_.owner -eq $activeOwner } | Select-Object -First 1
    $item | Add-Member -NotePropertyName registeredAgent -NotePropertyValue $registeredAgent
    $workspaceCircuit = $circuitRegistry.circuits | Where-Object workspace -eq $item.path | Select-Object -First 1
    $item | Add-Member -NotePropertyName circuit -NotePropertyValue $workspaceCircuit
    $item | Add-Member -NotePropertyName circuitOpen -NotePropertyValue ($null -ne $workspaceCircuit -and [bool]$workspaceCircuit.open)
}
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    tasksPath = $normalizedTasksPath
    detailed = [bool]$Detailed
    totalCount = $items.Count
    inProgressCount = @($items | Where-Object state -eq 'in-progress').Count
    decomposedCount = @($items | Where-Object state -eq 'decomposed').Count
    sealedCount = @($items | Where-Object state -eq 'sealed').Count
    migrationRequiredCount = @($items | Where-Object state -eq 'migration-required').Count
    policyDriftCount = @($items | Where-Object state -eq 'policy-drift').Count
    invalidCount = @($items | Where-Object state -in @('invalid-seal', 'incomplete')).Count
    graph = [pscustomobject][ordered]@{
        valid = [bool]$taskGraph.valid
        edgeCount = [int]$taskGraph.edgeCount
        blockingConflictCount = [int]$taskGraph.blockingConflictCount
        cycleCount = [int]$taskGraph.cycleCount
        mergeWaves = @($taskGraph.mergeWaves)
    }
    activeLeaseCount = [int]$taskLeases.activeCount
    runningDispatchCount = [int]$taskDispatches.runningCount
    dispatchAttentionCount = [int]($taskDispatches.orphanedCount + $taskDispatches.driftedCount + $taskDispatches.invalidCount)
    activeAgentCount = [int]$agentRegistry.activeCount
    agentAvailableCapacity = [int]$agentRegistry.availableCapacity
    openCircuitCount = [int]$circuitRegistry.openCount
    warnings = @($warnings)
    workspaces = @($items)
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
} else {
    Write-Host "Task workspaces: $($result.totalCount) total, $($result.inProgressCount) in progress, $($result.sealedCount) sealed, $($result.migrationRequiredCount) migration required, $($result.policyDriftCount) policy drift, $($result.invalidCount) invalid."
    foreach ($item in $items) {
        $readinessText = if ($null -ne $item.readiness) { ", readiness=$($item.readiness) ($($item.score)/100)" } else { '' }
        $dispatchText = if ($item.dispatchState -ne 'none') { ", dispatch=$($item.dispatchState)" } else { '' }
        Write-Host " - [$($item.state)] $($item.name)$readinessText$dispatchText"
        Write-Host "   $($item.objective)"
        Write-Host "   paths=$($item.changedPathCount), acceptance=$($item.pendingCriteria), checks=$($item.unresolvedChecks), reviews=$($item.unresolvedReviews), blockers=$($item.openJournalBlockers)"
        foreach ($issue in @($item.issues | Select-Object -First 5)) { Write-Host "   - $issue" }
    }
    foreach ($warning in $warnings) { Write-Host "Warning: $warning" }
}
