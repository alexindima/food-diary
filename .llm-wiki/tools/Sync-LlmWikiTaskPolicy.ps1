[CmdletBinding()]
param(
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [switch]$DryRun,
    [switch]$AcceptImpact,
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
$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
if (-not (Test-Path -LiteralPath $absoluteWorkspacePath -PathType Container)) {
    throw "Task workspace does not exist: $normalizedWorkspacePath"
}
if (Test-Path -LiteralPath (Join-Path $absoluteWorkspacePath 'completion.json') -PathType Leaf) {
    throw 'A sealed workspace cannot accept a new policy because that would invalidate its historical completion seal.'
}
$descriptorPath = Join-Path $absoluteWorkspacePath 'workspace.json'
$journalPath = Join-Path $absoluteWorkspacePath 'journal.json'
$descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json
if ([int]$descriptor.schemaVersion -ne [int]$policySnapshot.policy.workspace.latestSchemaVersion) {
    throw "Workspace schema v$($descriptor.schemaVersion) must be migrated before policy sync."
}
$doctor = & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$nonPolicyFailures = @($doctor.checks | Where-Object {
    $_.status -eq 'fail' -and $_.id -ne 'policy-fingerprint'
})
if ($nonPolicyFailures.Count -gt 0) {
    throw "Policy sync refused a structurally invalid workspace: $(@($nonPolicyFailures.message) -join ' ')"
}
$oldFingerprint = [string]$descriptor.policyFingerprint
$newFingerprint = [string]$policySnapshot.fingerprint
$impact = & (Join-Path $PSScriptRoot 'Compare-LlmWikiTaskPolicy.ps1') `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    workspace = $normalizedWorkspacePath
    dryRun = [bool]$DryRun
    changed = $oldFingerprint -cne $newFingerprint
    oldPolicyFingerprint = $oldFingerprint
    newPolicyFingerprint = $newFingerprint
    impact = $impact
    valid = $true
}
if (-not $result.changed -or $DryRun) {
    if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 6 } else {
        Write-Host "Task policy sync: changed=$($result.changed), dryRun=$($result.dryRun)"
        Write-Host "Policy: $oldFingerprint -> $newFingerprint"
        Write-Host "Impact: $($impact.changeCount) change(s), $($impact.affectingChangeCount) affect the current task."
        if (@($impact.requiredChecks).Count -gt 0) { Write-Host "Required checks: $(@($impact.requiredChecks) -join ', ')" }
    }
    return
}
if ($impact.affectingChangeCount -gt 0 -and -not $AcceptImpact) {
    throw "Policy sync requires -AcceptImpact because $($impact.affectingChangeCount) policy change(s) affect the current task. Review task-policy-impact and repeat the required checks: $(@($impact.requiredChecks) -join ', ')."
}

$originalDescriptor = Get-Content -LiteralPath $descriptorPath -Raw
$originalJournal = Get-Content -LiteralPath $journalPath -Raw
try {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') add `
        -WorkspacePath $normalizedWorkspacePath `
        -JournalType decision `
        -Text "Accepted workspace policy fingerprint $newFingerprint." `
        -Rationale "Previous fingerprint: $oldFingerprint. Impact explicitly accepted: $($impact.changeCount) change(s), $($impact.affectingChangeCount) affecting; required checks: $(@($impact.requiredChecks) -join ', '). Structural doctor checks passed before policy sync." | Out-Null
    $descriptor | Add-Member -NotePropertyName policyFingerprint -NotePropertyValue $newFingerprint -Force
    $descriptor | Add-Member -NotePropertyName policySnapshot -NotePropertyValue $policySnapshot.policy -Force
    $descriptor | Add-Member -NotePropertyName policyValidatedAtUtc -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force
    [System.IO.File]::WriteAllText(
        $descriptorPath,
        (($descriptor | ConvertTo-Json -Depth 15) + [Environment]::NewLine),
        [System.Text.UTF8Encoding]::new($false))
    $verifiedDoctor = & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
    if (-not $verifiedDoctor.valid) {
        throw "Workspace failed doctor after policy sync: $(@($verifiedDoctor.errors) -join ' ')"
    }
} catch {
    [System.IO.File]::WriteAllText($descriptorPath, $originalDescriptor, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText($journalPath, $originalJournal, [System.Text.UTF8Encoding]::new($false))
    throw
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 6 } else {
    Write-Host "Task policy sync completed: $oldFingerprint -> $newFingerprint"
    Write-Host "Accepted impact: $($impact.changeCount) change(s), $($impact.affectingChangeCount) affecting."
}
