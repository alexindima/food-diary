[CmdletBinding()]
param(
    [ValidateSet('status', 'next', 'complete')]
    [string]$Action = 'status',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$PhaseId,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [switch]$FailOnInvalid
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$workspace = Join-Path $repositoryRoot $WorkspacePath
$manifestPath = Join-Path $workspace 'change-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "Workspace manifest is absent: $manifestPath" }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$changed = @(git -C $repositoryRoot diff --name-only --relative $manifest.git.base HEAD) + @(git -C $repositoryRoot diff --name-only --relative) + @(git -C $repositoryRoot ls-files --others --exclude-standard)
$changed = @($changed | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique)
$phases = @($manifest.plan.phases | Sort-Object order | ForEach-Object {
    $phaseKey = [string]$_.id
    $files = @($_.files | ForEach-Object { ([string]$_).Replace('\', '/') })
    $present = @($files | Where-Object { $_ -in $changed })
    $existing = @($files | Where-Object { Test-Path -LiteralPath (Join-Path $repositoryRoot $_) })
    $phaseState = if ($phaseKey -eq 'context') {
        if ($files.Count -eq 0 -or $existing.Count -eq $files.Count) { 'completed' } else { 'blocked' }
    } elseif ($phaseKey -eq 'implementation') {
        if ($files.Count -eq 0) { 'review' } elseif ($present.Count -eq $files.Count) { 'implemented' } elseif ($present.Count -gt 0) { 'in-progress' } else { 'pending' }
    } else {
        'ready-for-check'
    }
    [pscustomobject][ordered]@{
        id = $phaseKey; order = [int]$_.order; title = [string]$_.title
        files = $files; changedFiles = $present
        state = $phaseState
        missingFiles = @(if ($phaseKey -eq 'context') { @($files | Where-Object { -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $_)) }) } elseif ($phaseKey -eq 'implementation') { @($files | Where-Object { $_ -notin $changed }) } else { @() })
    }
})
$selected = if (-not [string]::IsNullOrWhiteSpace($PhaseId)) { $phases | Where-Object id -eq $PhaseId | Select-Object -First 1 } else { $phases | Where-Object state -in @('blocked', 'pending', 'in-progress', 'ready-for-check') | Select-Object -First 1 }
if ($null -eq $selected -and $phases.Count -gt 0) { $selected = $phases[-1] }
$valid = $null -ne $selected -and ($Action -ne 'complete' -or $selected.state -in @('completed', 'implemented', 'ready-for-check'))
$result = [pscustomobject][ordered]@{
    schemaVersion = 1; action = $Action; valid = $valid; phases = $phases
    currentPhase = $selected
    nextAction = $(if ($null -eq $selected) { 'No implementation phase remains; run focused checks and delivery validation.' } elseif ($selected.state -in @('completed', 'implemented', 'ready-for-check')) { 'Run focused checks for this phase, then continue to delivery validation.' } elseif ($selected.state -eq 'blocked') { "Restore missing context sources for $($selected.id): $($selected.missingFiles -join ', ')" } else { "Implement missing files for $($selected.id): $($selected.missingFiles -join ', ')" })
    sourceOfTruth = "$WorkspacePath/change-manifest.json plus the current Git diff"
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 10 } else {
    Write-Host "Implementation phases: $(@($phases | Where-Object state -in @('completed', 'implemented')).Count)/$($phases.Count) completed or implemented"
    foreach ($phase in $phases) { Write-Host " - $($phase.id) [$($phase.state)]: $($phase.title)" }
    Write-Host "NEXT: $($result.nextAction)"
}
if ($FailOnInvalid -and -not $valid) { exit 1 }
