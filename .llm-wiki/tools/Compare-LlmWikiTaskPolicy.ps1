[CmdletBinding()]
param(
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$policySnapshot = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -WithFingerprint -Format Json | ConvertFrom-Json

if ([System.IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$descriptorPath = Join-Path $repositoryRoot "$normalizedWorkspacePath/workspace.json"
if (-not (Test-Path -LiteralPath $descriptorPath -PathType Leaf)) {
    throw "Task workspace descriptor does not exist: $normalizedWorkspacePath/workspace.json"
}
$descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json

function Add-PolicyLeaves([object]$Value, [string]$Path, [hashtable]$Leaves) {
    if ($null -eq $Value) {
        $Leaves[$Path] = 'null'
        return
    }
    if ($Value -is [System.Management.Automation.PSCustomObject]) {
        foreach ($property in $Value.PSObject.Properties | Sort-Object Name) {
            $childPath = if ($Path) { "$Path.$($property.Name)" } else { $property.Name }
            Add-PolicyLeaves $property.Value $childPath $Leaves
        }
        return
    }
    if ($Value -is [System.Collections.IEnumerable] -and $Value -isnot [string]) {
        $items = @($Value)
        for ($index = 0; $index -lt $items.Count; $index++) {
            $key = if ($items[$index] -is [System.Management.Automation.PSCustomObject] -and
                $items[$index].PSObject.Properties['id'] -and
                -not [string]::IsNullOrWhiteSpace([string]$items[$index].id)) {
                "id=$($items[$index].id)"
            } else {
                [string]$index
            }
            Add-PolicyLeaves $items[$index] "$Path[$key]" $Leaves
        }
        return
    }
    $Leaves[$Path] = ($Value | ConvertTo-Json -Compress)
}

function Get-Impact([string]$Path) {
    if ($Path -like 'export.redaction.*') {
        return [pscustomobject]@{ area = 'redaction-security'; severity = 'critical'; affectsCurrentTask = $true; requiredCheck = 'task-export-verify' }
    }
    if ($Path -like 'workspace.*') {
        return [pscustomobject]@{ area = 'workspace-integrity'; severity = 'high'; affectsCurrentTask = $true; requiredCheck = 'task-doctor' }
    }
    if ($Path -like 'audit.*') {
        return [pscustomobject]@{ area = 'audit-governance'; severity = 'high'; affectsCurrentTask = $true; requiredCheck = 'task-audit' }
    }
    if ($Path -like 'export.*') {
        return [pscustomobject]@{ area = 'export-safety'; severity = 'high'; affectsCurrentTask = $true; requiredCheck = 'task-export-verify' }
    }
    if ($Path -like 'import.*') {
        return [pscustomobject]@{ area = 'import-safety'; severity = 'medium'; affectsCurrentTask = $false; requiredCheck = 'task-import-smoke' }
    }
    return [pscustomobject]@{ area = 'policy-metadata'; severity = 'low'; affectsCurrentTask = $false; requiredCheck = 'workspace-policy' }
}

$oldLeaves = @{}
$newLeaves = @{}
$snapshotAvailable = $null -ne $descriptor.policySnapshot
if ($snapshotAvailable) { Add-PolicyLeaves $descriptor.policySnapshot '' $oldLeaves }
Add-PolicyLeaves $policySnapshot.policy '' $newLeaves
$changes = [System.Collections.Generic.List[object]]::new()
if ($snapshotAvailable) {
    $paths = @($oldLeaves.Keys + $newLeaves.Keys | Sort-Object -Unique)
    foreach ($path in $paths) {
        $oldPresent = $oldLeaves.ContainsKey($path)
        $newPresent = $newLeaves.ContainsKey($path)
        if ($oldPresent -and $newPresent -and $oldLeaves[$path] -ceq $newLeaves[$path]) { continue }
        $impact = Get-Impact $path
        $changes.Add([pscustomobject][ordered]@{
            path = $path
            kind = $(if (-not $oldPresent) { 'added' } elseif (-not $newPresent) { 'removed' } else { 'changed' })
            oldValue = $(if ($oldPresent) { $oldLeaves[$path] } else { $null })
            newValue = $(if ($newPresent) { $newLeaves[$path] } else { $null })
            area = $impact.area
            severity = $impact.severity
            affectsCurrentTask = $impact.affectsCurrentTask
            requiredCheck = $impact.requiredCheck
        })
    }
}
$requiredChecks = @($changes | Where-Object affectsCurrentTask | Select-Object -ExpandProperty requiredCheck -Unique)
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    workspace = $normalizedWorkspacePath
    snapshotAvailable = $snapshotAvailable
    changed = [string]$descriptor.policyFingerprint -cne [string]$policySnapshot.fingerprint
    oldPolicyFingerprint = [string]$descriptor.policyFingerprint
    newPolicyFingerprint = [string]$policySnapshot.fingerprint
    changeCount = $changes.Count
    affectingChangeCount = @($changes | Where-Object affectsCurrentTask).Count
    highestSeverity = $(foreach ($level in @('critical', 'high', 'medium', 'low')) {
        if (@($changes | Where-Object severity -eq $level).Count -gt 0) { $level; break }
    })
    requiredChecks = $requiredChecks
    changes = @($changes)
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
} else {
    Write-Host "Task policy impact: changed=$($result.changed), changes=$($result.changeCount), affecting=$($result.affectingChangeCount)"
    if (-not $result.snapshotAvailable) { Write-Host ' - Semantic comparison unavailable: migrate the workspace to schema v4.' }
    foreach ($change in $changes) {
        Write-Host " - [$($change.severity)] $($change.kind) $($change.path) ($($change.area))"
    }
    if ($requiredChecks.Count -gt 0) { Write-Host "Required checks: $($requiredChecks -join ', ')" }
}
