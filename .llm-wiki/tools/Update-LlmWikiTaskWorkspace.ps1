[CmdletBinding()]
param(
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [switch]$DryRun,
    [switch]$NoBackup,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$workspacePolicySnapshot = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -WithFingerprint -Format Json | ConvertFrom-Json
$workspacePolicy = $workspacePolicySnapshot.policy
$latestVersion = [int]$workspacePolicy.workspace.latestSchemaVersion

if ([System.IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
if (-not (Test-Path -LiteralPath $absoluteWorkspacePath -PathType Container)) {
    throw "Task workspace does not exist: $normalizedWorkspacePath"
}

$descriptorPath = Join-Path $absoluteWorkspacePath 'workspace.json'
if (-not (Test-Path -LiteralPath $descriptorPath -PathType Leaf)) {
    throw 'Task workspace is incomplete; missing workspace.json.'
}
try {
    $descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json
} catch {
    throw "Invalid workspace.json: $($_.Exception.Message)"
}
$sourceVersion = [int]$descriptor.schemaVersion
if ($sourceVersion -lt 1) { throw "Unsupported workspace schemaVersion: $sourceVersion" }
if ($sourceVersion -gt $latestVersion) {
    throw "Workspace schemaVersion $sourceVersion is newer than supported version $latestVersion."
}

$completionPath = Join-Path $absoluteWorkspacePath 'completion.json'
if ($sourceVersion -lt $latestVersion -and (Test-Path -LiteralPath $completionPath -PathType Leaf)) {
    throw 'A sealed workspace cannot be migrated because migration would invalidate its completion seal.'
}

$artifactNames = [ordered]@{
    packet = 'change-packet.json'
    taskContract = 'task-contract.json'
    manifest = 'change-manifest.json'
    acceptance = 'acceptance-matrix.json'
    evidence = 'evidence.json'
    journal = 'journal.json'
    report = 'review-report.md'
}
$unsafeIssues = [System.Collections.Generic.List[string]]::new()
foreach ($artifact in $artifactNames.GetEnumerator()) {
    $actual = [string]$descriptor.artifacts.($artifact.Key)
    $expected = "$normalizedWorkspacePath/$($artifact.Value)"
    if (-not [string]::IsNullOrWhiteSpace($actual) -and $actual -cne $expected) {
        $unsafeIssues.Add("Descriptor path for '$($artifact.Key)' is non-canonical: '$actual'.")
    }
}
if ($unsafeIssues.Count -gt 0) {
    throw "Migration refused unsafe descriptor data: $($unsafeIssues -join ' ')"
}

$steps = [System.Collections.Generic.List[object]]::new()
$journalPath = Join-Path $absoluteWorkspacePath 'journal.json'
if ($sourceVersion -eq 1) {
    $steps.Add([pscustomobject][ordered]@{
        fromVersion = 1
        toVersion = 2
        description = 'Declare the workspace format and artifact schema versions; add the task journal contract.'
    })
}
if ($sourceVersion -lt 3) {
    $steps.Add([pscustomobject][ordered]@{
        fromVersion = 2
        toVersion = 3
        description = 'Bind the workspace to the validated workspace-policy fingerprint.'
    })
}
if ($sourceVersion -lt 4) {
    $steps.Add([pscustomobject][ordered]@{
        fromVersion = 3
        toVersion = 4
        description = 'Retain the accepted policy snapshot so future policy drift can be explained semantically.'
    })
}

$result = [ordered]@{
    schemaVersion = 1
    workspace = $normalizedWorkspacePath
    sourceVersion = $sourceVersion
    targetVersion = $latestVersion
    migrationRequired = $sourceVersion -lt $latestVersion
    dryRun = [bool]$DryRun
    changed = $false
    backupPath = $null
    steps = @($steps)
}

if ($sourceVersion -eq $latestVersion) {
    $doctor = & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
    if (-not $doctor.valid) {
        throw "Workspace already uses schemaVersion $latestVersion but is invalid: $(@($doctor.errors) -join ' ')"
    }
} elseif (-not $DryRun) {
    $backupPath = $null
    if (-not $NoBackup) {
        $backupRoot = Join-Path $absoluteWorkspacePath '.migration-backups'
        if (-not (Test-Path -LiteralPath $backupRoot)) {
            New-Item -ItemType Directory -Path $backupRoot | Out-Null
        }
        $backupName = ([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')) + "-v$sourceVersion"
        $backupPath = Join-Path $backupRoot $backupName
        New-Item -ItemType Directory -Path $backupPath | Out-Null
        foreach ($name in @('workspace.json', 'journal.json')) {
            $source = Join-Path $absoluteWorkspacePath $name
            if (Test-Path -LiteralPath $source -PathType Leaf) {
                Copy-Item -LiteralPath $source -Destination (Join-Path $backupPath $name)
            }
        }
        $result.backupPath = "$normalizedWorkspacePath/.migration-backups/$backupName"
    }

    $originalDescriptor = Get-Content -LiteralPath $descriptorPath -Raw
    $journalWasCreated = $false
    try {
        if (-not (Test-Path -LiteralPath $journalPath -PathType Leaf)) {
            & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') init `
                -WorkspacePath $normalizedWorkspacePath | Out-Null
            $journalWasCreated = $true
        } else {
            $journalValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') validate `
                -WorkspacePath $normalizedWorkspacePath `
                -Format Json | ConvertFrom-Json
            if (-not $journalValidation.valid) {
                throw "Existing journal is invalid: $(@($journalValidation.issues) -join ' ')"
            }
        }

        $migrations = @($descriptor.migrations)
        $currentVersion = $sourceVersion
        if ($currentVersion -lt 2) {
            $migrations += [pscustomobject][ordered]@{
                fromVersion = 1
                toVersion = 2
                migratedAtUtc = [DateTime]::UtcNow.ToString('o')
                tool = '.llm-wiki/tools/Update-LlmWikiTaskWorkspace.ps1'
            }
            $descriptor | Add-Member -NotePropertyName schemaVersion -NotePropertyValue 2 -Force
            $descriptor | Add-Member -NotePropertyName format -NotePropertyValue ([string]$workspacePolicy.workspace.format) -Force
            if ($null -eq $descriptor.artifacts) {
                $descriptor | Add-Member -NotePropertyName artifacts -NotePropertyValue ([pscustomobject]@{}) -Force
            }
            foreach ($artifact in $artifactNames.GetEnumerator()) {
                $descriptor.artifacts | Add-Member -NotePropertyName $artifact.Key `
                    -NotePropertyValue "$normalizedWorkspacePath/$($artifact.Value)" -Force
            }
            $descriptor | Add-Member -NotePropertyName artifactSchemaVersions -NotePropertyValue ([pscustomobject][ordered]@{
                packet = [int]$workspacePolicy.workspace.artifactSchemaVersions.packet
                taskContract = [int]$workspacePolicy.workspace.artifactSchemaVersions.taskContract
                manifest = [int]$workspacePolicy.workspace.artifactSchemaVersions.manifest
                acceptance = [int]$workspacePolicy.workspace.artifactSchemaVersions.acceptance
                evidence = [int]$workspacePolicy.workspace.artifactSchemaVersions.evidence
                journal = [int]$workspacePolicy.workspace.artifactSchemaVersions.journal
                report = [int]$workspacePolicy.workspace.artifactSchemaVersions.report
            }) -Force
            $currentVersion = 2
        }
        if ($currentVersion -lt 3) {
            $migrations += [pscustomobject][ordered]@{
                fromVersion = 2
                toVersion = 3
                migratedAtUtc = [DateTime]::UtcNow.ToString('o')
                tool = '.llm-wiki/tools/Update-LlmWikiTaskWorkspace.ps1'
            }
            $descriptor | Add-Member -NotePropertyName schemaVersion -NotePropertyValue 3 -Force
            $descriptor | Add-Member -NotePropertyName policyFingerprint -NotePropertyValue ([string]$workspacePolicySnapshot.fingerprint) -Force
            $descriptor | Add-Member -NotePropertyName policyValidatedAtUtc -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force
            $currentVersion = 3
        }
        if ($currentVersion -lt 4) {
            $migrations += [pscustomobject][ordered]@{
                fromVersion = 3
                toVersion = 4
                migratedAtUtc = [DateTime]::UtcNow.ToString('o')
                tool = '.llm-wiki/tools/Update-LlmWikiTaskWorkspace.ps1'
            }
            $descriptor | Add-Member -NotePropertyName schemaVersion -NotePropertyValue 4 -Force
            $descriptor | Add-Member -NotePropertyName policyFingerprint -NotePropertyValue ([string]$workspacePolicySnapshot.fingerprint) -Force
            $descriptor | Add-Member -NotePropertyName policySnapshot -NotePropertyValue $workspacePolicy -Force
            $descriptor | Add-Member -NotePropertyName policyValidatedAtUtc -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force
            $currentVersion = 4
        }
        $descriptor | Add-Member -NotePropertyName migrations -NotePropertyValue @($migrations) -Force
        [System.IO.File]::WriteAllText(
            $descriptorPath,
            (($descriptor | ConvertTo-Json -Depth 15) + [Environment]::NewLine),
            [System.Text.UTF8Encoding]::new($false))

        $doctor = & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') `
            -WorkspacePath $normalizedWorkspacePath `
            -Format Json | ConvertFrom-Json
        if (-not $doctor.valid) {
            throw "Migrated workspace failed validation: $(@($doctor.errors) -join ' ')"
        }
        $result.changed = $true
    } catch {
        [System.IO.File]::WriteAllText($descriptorPath, $originalDescriptor, [System.Text.UTF8Encoding]::new($false))
        if ($journalWasCreated -and (Test-Path -LiteralPath $journalPath)) {
            Remove-Item -LiteralPath $journalPath -Force
        }
        throw
    }
}

$output = [pscustomobject]$result
if ($Format -eq 'Json') {
    $output | ConvertTo-Json -Depth 8
} else {
    $verb = if ($output.dryRun) { 'planned' } elseif ($output.changed) { 'completed' } else { 'not required' }
    Write-Host "Task migration ${verb}: schema v$($output.sourceVersion) -> v$($output.targetVersion)."
    if ($output.backupPath) { Write-Host "Backup: $($output.backupPath)" }
    foreach ($step in $output.steps) {
        Write-Host " - v$($step.fromVersion) -> v$($step.toVersion): $($step.description)"
    }
}
