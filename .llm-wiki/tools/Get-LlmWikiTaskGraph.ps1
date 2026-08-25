[CmdletBinding()]
param(
    [string]$TasksPath = '.artifacts/llm-wiki/tasks',
    [switch]$IncludeSealed,
    [switch]$FailOnConflict,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
if ([System.IO.Path]::IsPathRooted($TasksPath)) { throw 'TasksPath must be repository-relative.' }
$normalizedTasksPath = $TasksPath.Replace('\', '/').TrimEnd('/')
if ($normalizedTasksPath -cne '.artifacts/llm-wiki/tasks') { throw 'TasksPath must be .artifacts/llm-wiki/tasks.' }
$absoluteTasksPath = Join-Path $repositoryRoot $normalizedTasksPath
$nodes = [System.Collections.Generic.List[object]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

foreach ($directory in @(Get-ChildItem -LiteralPath $absoluteTasksPath -Directory -Force -ErrorAction SilentlyContinue | Sort-Object Name)) {
    if ($directory.Name.StartsWith('.', [StringComparison]::Ordinal)) { continue }
    $completionPath = Join-Path $directory.FullName 'completion.json'
    $sealed = Test-Path -LiteralPath $completionPath -PathType Leaf
    if ($sealed -and -not $IncludeSealed) { continue }
    $packetPath = Join-Path $directory.FullName 'change-packet.json'
    $descriptorPath = Join-Path $directory.FullName 'workspace.json'
    if (-not (Test-Path -LiteralPath $packetPath -PathType Leaf) -or -not (Test-Path -LiteralPath $descriptorPath -PathType Leaf)) {
        $warnings.Add("Skipped incomplete workspace: $normalizedTasksPath/$($directory.Name)")
        continue
    }
    try {
        $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
        $descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json
        $decompositionProperty = $descriptor.PSObject.Properties['decomposition']
        $decomposition = if ($null -ne $decompositionProperty) { $decompositionProperty.Value } else { $null }
        $decompositionStateProperty = if ($null -ne $decomposition) { $decomposition.PSObject.Properties['state'] } else { $null }
        if ($null -ne $decompositionStateProperty -and [string]$decompositionStateProperty.Value -eq 'applied') { continue }
        $contractRuleIds = @($packet.policy.matchedRules | ForEach-Object {
            $idProperty = $_.PSObject.Properties['id']
            if ($null -ne $idProperty) { [string]$idProperty.Value }
        } | Where-Object {
            $_ -in @('api-contract', 'backend-public-contract', 'shared-ui-consumer-contract', 'frontend-component-contract', 'configuration-contract', 'persistence-model-contract')
        })
        $projectNames = @($packet.diff.projects | ForEach-Object {
            $nameProperty = $_.PSObject.Properties['name']
            if ($null -ne $nameProperty) { [string]$nameProperty.Value }
        } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
        $nodes.Add([pscustomobject][ordered]@{
            name = $directory.Name
            path = "$normalizedTasksPath/$($directory.Name)"
            objective = [string]$descriptor.objective
            state = $(if ($sealed) { 'sealed' } else { 'active' })
            packetFingerprint = [string]$packet.fingerprint
            changedPaths = @($packet.diff.changedPaths | Sort-Object -Unique)
            modules = @($packet.ownership.directModules | Sort-Object -Unique)
            impactedModules = @($packet.ownership.transitivelyImpactedModules | Sort-Object -Unique)
            downstreamModules = @($packet.ownership.downstreamModules | Sort-Object -Unique)
            projects = $projectNames
            scopes = @($packet.diff.scopes | Sort-Object -Unique)
            generatedActions = @($packet.diff.generatedActions | Sort-Object -Unique)
            contractRules = $contractRuleIds
            decomposition = $decomposition
        })
    } catch {
        $warnings.Add("Skipped unreadable workspace '$($directory.Name)': $($_.Exception.Message)")
    }
}

$edges = [System.Collections.Generic.List[object]]::new()
$edgeKeys = @{}
function Add-Edge(
    [string]$Type,
    [string]$Left,
    [string]$Right,
    [string]$Severity,
    [bool]$Blocking,
    [string]$Direction,
    [string]$From,
    [string]$To,
    [object[]]$Evidence,
    [string]$Recommendation
) {
    if ($Direction -eq 'undirected') {
        $pair = @($Left, $Right) | Sort-Object
        $Left = $pair[0]
        $Right = $pair[1]
        $From = ''
        $To = ''
    }
    $key = "$Type|$Left|$Right|$From|$To"
    if ($edgeKeys.ContainsKey($key)) { return }
    $edgeKeys[$key] = $true
    $edges.Add([pscustomobject][ordered]@{
        type = $Type
        left = $Left
        right = $Right
        severity = $Severity
        blocking = $Blocking
        direction = $Direction
        from = $From
        to = $To
        evidence = @($Evidence | Sort-Object -Unique)
        recommendation = $Recommendation
    })
}
function Get-Intersection([object[]]$Left, [object[]]$Right) {
    @($Left | Where-Object { $_ -in $Right } | Sort-Object -Unique)
}

for ($leftIndex = 0; $leftIndex -lt $nodes.Count; $leftIndex++) {
    for ($rightIndex = $leftIndex + 1; $rightIndex -lt $nodes.Count; $rightIndex++) {
        $left = $nodes[$leftIndex]
        $right = $nodes[$rightIndex]
        $pathOverlap = @(Get-Intersection $left.changedPaths $right.changedPaths)
        if (@($pathOverlap).Count -gt 0) {
            Add-Edge 'write-conflict' $left.name $right.name 'critical' $true 'undirected' '' '' $pathOverlap 'Do not merge in parallel; assign ownership or re-scope one task, then refresh both packets.'
        }
        $moduleOverlap = @(Get-Intersection $left.modules $right.modules)
        $projectOverlap = @(Get-Intersection $left.projects $right.projects)
        if (@($pathOverlap).Count -eq 0 -and @($moduleOverlap).Count -gt 0 -and @($projectOverlap).Count -gt 0) {
            Add-Edge 'boundary-coordination' $left.name $right.name 'high' $false 'undirected' '' '' @($moduleOverlap + $projectOverlap) 'Coordinate invariants and rebase the later task before merge.'
        }
        $generatedOverlap = @(Get-Intersection $left.generatedActions $right.generatedActions)
        if (@($pathOverlap).Count -eq 0 -and @($generatedOverlap).Count -gt 0) {
            Add-Edge 'generated-artifact-coordination' $left.name $right.name 'medium' $false 'undirected' '' '' $generatedOverlap 'Regenerate shared derived artifacts only after both source changes are integrated.'
        }
        $leftDependsOnRight = @(Get-Intersection $left.modules $right.downstreamModules)
        if (@($leftDependsOnRight).Count -gt 0) {
            Add-Edge 'module-dependency' $left.name $right.name 'high' $false 'directed' $right.name $left.name $leftDependsOnRight "Merge '$($right.name)' first; refresh and rebase '$($left.name)' against it."
        }
        $rightDependsOnLeft = @(Get-Intersection $right.modules $left.downstreamModules)
        if (@($rightDependsOnLeft).Count -gt 0) {
            Add-Edge 'module-dependency' $left.name $right.name 'high' $false 'directed' $left.name $right.name $rightDependsOnLeft "Merge '$($left.name)' first; refresh and rebase '$($right.name)' against it."
        }
        if (@($left.contractRules).Count -gt 0) {
            $contractImpact = @(Get-Intersection $left.impactedModules @($right.modules + $right.impactedModules))
            if (@($contractImpact).Count -gt 0) {
                Add-Edge 'contract-before-consumer' $left.name $right.name 'high' $false 'directed' $left.name $right.name @($left.contractRules + $contractImpact) "Merge contract producer '$($left.name)' before '$($right.name)', then rerun consumer compatibility checks."
            }
        }
        if (@($right.contractRules).Count -gt 0) {
            $contractImpact = @(Get-Intersection $right.impactedModules @($left.modules + $left.impactedModules))
            if (@($contractImpact).Count -gt 0) {
                Add-Edge 'contract-before-consumer' $left.name $right.name 'high' $false 'directed' $right.name $left.name @($right.contractRules + $contractImpact) "Merge contract producer '$($right.name)' before '$($left.name)', then rerun consumer compatibility checks."
            }
        }
    }
}

foreach ($node in @($nodes | Where-Object { $null -ne $_.decomposition })) {
    foreach ($prerequisiteWorkspace in @($node.decomposition.prerequisiteWorkspaces)) {
        $prerequisiteName = Split-Path -Leaf ([string]$prerequisiteWorkspace)
        if ($prerequisiteName -in @($nodes | ForEach-Object { $_.name })) {
            Add-Edge 'decomposition-prerequisite' $node.name $prerequisiteName 'high' $false 'directed' $prerequisiteName $node.name @([string]$node.decomposition.decompositionId) "Complete decomposition shard '$prerequisiteName' before '$($node.name)'."
        }
    }
}

$indegree = @{}
$outgoing = @{}
foreach ($node in $nodes) { $indegree[$node.name] = 0; $outgoing[$node.name] = [System.Collections.Generic.List[string]]::new() }
foreach ($edge in @($edges | Where-Object direction -eq 'directed')) {
    if ($edge.from -ceq $edge.to) { continue }
    if (-not $outgoing[$edge.from].Contains([string]$edge.to)) {
        $outgoing[$edge.from].Add([string]$edge.to)
        $indegree[$edge.to] = [int]$indegree[$edge.to] + 1
    }
}
$waves = [System.Collections.Generic.List[object]]::new()
$remaining = @($nodes | ForEach-Object { $_.name } | Sort-Object)
$waveNumber = 1
while ($remaining.Count -gt 0) {
    $ready = @($remaining | Where-Object { $indegree[$_] -eq 0 } | Sort-Object)
    if ($ready.Count -eq 0) { break }
    $waves.Add([pscustomobject][ordered]@{ wave = $waveNumber; tasks = $ready })
    foreach ($name in $ready) {
        $remaining = @($remaining | Where-Object { $_ -cne $name })
        foreach ($target in $outgoing[$name]) { $indegree[$target] = [int]$indegree[$target] - 1 }
    }
    $waveNumber++
}
$cycleNodes = @($remaining | Sort-Object)
$blockingEdges = @($edges | Where-Object blocking)
$conflictedNodes = @($blockingEdges | ForEach-Object { $_.left; $_.right } | Sort-Object -Unique)
foreach ($node in $nodes) {
    $node | Add-Member -NotePropertyName edgeCount -NotePropertyValue @($edges | Where-Object { $_.left -eq $node.name -or $_.right -eq $node.name }).Count
    $node | Add-Member -NotePropertyName blockingConflictCount -NotePropertyValue @($blockingEdges | Where-Object { $_.left -eq $node.name -or $_.right -eq $node.name }).Count
    $node | Add-Member -NotePropertyName prerequisiteTasks -NotePropertyValue @($edges | Where-Object { $_.direction -eq 'directed' -and $_.to -eq $node.name } | Select-Object -ExpandProperty from -Unique)
    $node | Add-Member -NotePropertyName dependentTasks -NotePropertyValue @($edges | Where-Object { $_.direction -eq 'directed' -and $_.from -eq $node.name } | Select-Object -ExpandProperty to -Unique)
}
$valid = $blockingEdges.Count -eq 0 -and $cycleNodes.Count -eq 0
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    tasksPath = $normalizedTasksPath
    includeSealed = [bool]$IncludeSealed
    valid = $valid
    nodeCount = $nodes.Count
    edgeCount = $edges.Count
    blockingConflictCount = $blockingEdges.Count
    directedEdgeCount = @($edges | Where-Object direction -eq 'directed').Count
    cycleCount = $(if ($cycleNodes.Count -gt 0) { 1 } else { 0 })
    cycleNodes = $cycleNodes
    conflictedNodes = $conflictedNodes
    nodes = @($nodes)
    edges = @($edges)
    mergeWaves = @($waves)
    warnings = @($warnings)
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 12
} else {
    Write-Host "Task graph: nodes=$($result.nodeCount), edges=$($result.edgeCount), blocking=$($result.blockingConflictCount), cycles=$($result.cycleCount)"
    foreach ($edge in $edges) {
        $arrow = if ($edge.direction -eq 'directed') { "$($edge.from) -> $($edge.to)" } else { "$($edge.left) <-> $($edge.right)" }
        Write-Host " - [$($edge.severity)] $($edge.type): $arrow"
    }
    foreach ($wave in $waves) { Write-Host "Merge wave $($wave.wave): $(@($wave.tasks) -join ', ')" }
}
if ($FailOnConflict -and -not $valid) { exit 1 }
