[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ImportPath,
    [Parameter(Mandatory = $true)]
    [string]$WorkspacePath,
    [string]$BaseRef = 'HEAD',
    [switch]$AllowPartialScope,
    [switch]$SkipJournal,
    [switch]$DryRun,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$workspacePolicy = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | ConvertFrom-Json

function Write-Result([object]$Result) {
    if ($Format -eq 'Json') {
        $Result | ConvertTo-Json -Depth 10
    } else {
        Write-Host "Task import: valid=$($Result.valid), dryRun=$($Result.dryRun), workspace=$($Result.workspace)"
        Write-Host "Source: $($Result.importPath) ($($Result.sourceSha256))"
        Write-Host "Scope: $($Result.changedPathCount) changed path(s), $($Result.allowedPatternCount) allowed pattern(s)"
        Write-Host "Acceptance reset to pending: $($Result.acceptanceCriterionCount)"
        Write-Host "Journal entries imported: $($Result.importedJournalEntryCount)"
    }
}
function Assert-SafeChangedPath([string]$Value) {
    $normalized = $Value.Replace('\', '/')
    if ([string]::IsNullOrWhiteSpace($normalized) -or
        [System.IO.Path]::IsPathRooted($normalized) -or
        $normalized -match '(^|/)\.\.(/|$)' -or
        $normalized.Contains(':')) {
        throw "Export contains an unsafe changed path: '$Value'."
    }
    return $normalized
}

if ([System.IO.Path]::IsPathRooted($ImportPath)) { throw 'ImportPath must be repository-relative.' }
$normalizedImportPath = $ImportPath.Replace('\', '/')
if ($normalizedImportPath -notmatch [string]$workspacePolicy.import.pathPattern) {
    throw 'ImportPath must be a JSON file directly inside .artifacts/llm-wiki/exports.'
}
if ([System.IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspacePath -notmatch [string]$workspacePolicy.import.workspacePattern) {
    throw 'WorkspacePath must identify a non-staging workspace directly inside .artifacts/llm-wiki/tasks.'
}
$absoluteImportPath = Join-Path $repositoryRoot $normalizedImportPath
$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
if (Test-Path -LiteralPath $absoluteWorkspacePath) {
    throw "Task workspace already exists: $normalizedWorkspacePath"
}

$verification = & (Join-Path $PSScriptRoot 'Export-LlmWikiTaskWorkspace.ps1') verify `
    -Path $normalizedImportPath `
    -Format Json | ConvertFrom-Json
if (-not $verification.valid) {
    throw "Import package is invalid: $(@($verification.issues) -join ' ')"
}
$package = Get-Content -LiteralPath $absoluteImportPath -Raw | ConvertFrom-Json
$handoff = $package.handoff
$objective = [string]$handoff.objective
if ([string]::IsNullOrWhiteSpace($objective)) { throw 'Import package has no objective.' }
$criteria = @($handoff.acceptanceCriteria | ForEach-Object { [string]$_.text } | Where-Object {
    -not [string]::IsNullOrWhiteSpace($_)
})
if ($criteria.Count -eq 0) { throw 'Import package has no acceptance criteria.' }
if ([int]$handoff.scope.omittedChangedPathCount -gt 0 -and -not $AllowPartialScope) {
    throw "Import package omitted $($handoff.scope.omittedChangedPathCount) changed path(s). Re-export with a larger -Limit or use -AllowPartialScope explicitly."
}
$changedPaths = @($handoff.scope.changedPaths | ForEach-Object { Assert-SafeChangedPath ([string]$_) } | Select-Object -Unique)
$allowedPatterns = if ($changedPaths.Count -gt 0) {
    @($changedPaths | ForEach-Object { '^' + [regex]::Escape($_) + '$' })
} else {
    @($handoff.scope.allowedPathPatterns | ForEach-Object { [string]$_ } | Where-Object {
        -not [string]::IsNullOrWhiteSpace($_)
    })
}
if ($allowedPatterns.Count -eq 0) {
    throw 'Import package has neither changed paths nor an allowed-path scope contract.'
}
$excludedPatterns = @($handoff.scope.excludedPathPatterns | ForEach-Object { [string]$_ } | Where-Object {
    -not [string]::IsNullOrWhiteSpace($_)
})

$sourceEntries = @($handoff.journal.entries)
$journalPlan = if ($SkipJournal) {
    @()
} else {
    @($sourceEntries | ForEach-Object {
        $sourceType = [string]$_.type
        $sourceStatus = [string]$_.status
        [pscustomobject][ordered]@{
            sourceId = [string]$_.id
            type = $(if ($sourceType -eq 'blocker' -and $sourceStatus -eq 'open') { 'blocker' } elseif ($sourceType -in @('decision', 'assumption', 'learning', 'note')) { $sourceType } else { 'learning' })
            text = "[Imported $([string]$_.id)/$sourceStatus] $([string]$_.text)"
            rationale = $(if (-not [string]::IsNullOrWhiteSpace([string]$_.resolution)) { "Source resolution: $([string]$_.resolution)" } else { [string]$_.rationale })
        }
    })
}
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    valid = $true
    dryRun = [bool]$DryRun
    importPath = $normalizedImportPath
    sourceSha256 = [string]$package.seal.sha256
    sourceWorkspace = [string]$package.source.workspace
    sourceContinuityFingerprint = [string]$package.source.continuityFingerprint
    workspace = $normalizedWorkspacePath
    baseRef = $BaseRef
    changedPathCount = $changedPaths.Count
    allowedPatternCount = $allowedPatterns.Count
    acceptanceCriterionCount = $criteria.Count
    importedJournalEntryCount = $journalPlan.Count
    acceptanceResetToPending = $true
}
if ($DryRun) {
    Write-Result $result
    return
}

$tasksRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/tasks'
if (-not (Test-Path -LiteralPath $tasksRoot)) {
    New-Item -ItemType Directory -Path $tasksRoot -Force | Out-Null
}
$stagingPrefix = [string]$workspacePolicy.import.stagingPrefix
$stagingName = $stagingPrefix + [guid]::NewGuid().ToString('N')
$stagingPath = ".artifacts/llm-wiki/tasks/$stagingName"
$absoluteStagingPath = Join-Path $tasksRoot $stagingName
$movedToFinal = $false
try {
    $initializeArguments = @{
        Objective = $objective
        Criterion = $criteria
        WorkspacePath = $stagingPath
        BaseRef = $BaseRef
        AllowedPath = $allowedPatterns
        ExcludedPath = $excludedPatterns
    }
    if ($changedPaths.Count -gt 0) { $initializeArguments.ChangedPath = $changedPaths }
    & (Join-Path $PSScriptRoot 'Initialize-LlmWikiTaskWorkspace.ps1') @initializeArguments | Out-Null

    & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') add `
        -WorkspacePath $stagingPath `
        -JournalType learning `
        -Text "Imported portable task context from $normalizedImportPath." `
        -Rationale "Verified source SHA-256: $($package.seal.sha256)" | Out-Null
    foreach ($entry in $journalPlan) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') add `
            -WorkspacePath $stagingPath `
            -JournalType $entry.type `
            -Text $entry.text `
            -Rationale $entry.rationale | Out-Null
    }

    $descriptorPath = Join-Path $absoluteStagingPath 'workspace.json'
    $descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json
    foreach ($artifact in @($descriptor.artifacts.PSObject.Properties)) {
        $artifact.Value = ([string]$artifact.Value).Replace($stagingPath, $normalizedWorkspacePath)
    }
    $descriptor | Add-Member -NotePropertyName importedFrom -NotePropertyValue ([pscustomobject][ordered]@{
        exportPath = $normalizedImportPath
        exportSha256 = [string]$package.seal.sha256
        exportedAtUtc = [string]$package.exportedAtUtc
        sourceWorkspace = [string]$package.source.workspace
        sourceContinuityFingerprint = [string]$package.source.continuityFingerprint
        sourcePolicyFingerprint = [string]$package.source.policyFingerprint
        importedAtUtc = [DateTime]::UtcNow.ToString('o')
        acceptanceResetToPending = $true
    }) -Force
    [System.IO.File]::WriteAllText(
        $descriptorPath,
        (($descriptor | ConvertTo-Json -Depth 15) + [Environment]::NewLine),
        [System.Text.UTF8Encoding]::new($false))

    Move-Item -LiteralPath $absoluteStagingPath -Destination $absoluteWorkspacePath
    $movedToFinal = $true
    $doctor = & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
    if (-not $doctor.valid) {
        throw "Imported workspace failed validation: $(@($doctor.errors) -join ' ')"
    }
} catch {
    if ($movedToFinal -and (Test-Path -LiteralPath $absoluteWorkspacePath) -and
        -not (Test-Path -LiteralPath $absoluteStagingPath)) {
        Move-Item -LiteralPath $absoluteWorkspacePath -Destination $absoluteStagingPath
        $movedToFinal = $false
    }
    throw
} finally {
    if (Test-Path -LiteralPath $absoluteStagingPath) {
        $resolvedStagingPath = (Resolve-Path -LiteralPath $absoluteStagingPath).Path
        $resolvedTasksRoot = (Resolve-Path -LiteralPath $tasksRoot).Path
        if (-not $resolvedStagingPath.StartsWith($resolvedTasksRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -or
            (Split-Path $resolvedStagingPath -Leaf) -notlike "$stagingPrefix*") {
            throw "Refusing to clean unexpected import staging path: $resolvedStagingPath"
        }
        Remove-Item -LiteralPath $resolvedStagingPath -Recurse -Force
    }
}
Write-Result $result
