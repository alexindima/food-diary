[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('init', 'add', 'resolve', 'show', 'validate')]
    [string]$Action = 'show',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [ValidateSet('decision', 'assumption', 'blocker', 'learning', 'note')]
    [string]$JournalType = 'note',
    [string]$Text,
    [string]$Rationale,
    [string]$NoteId,
    [string]$Resolution,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path

if ([System.IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+(?:/.*)?$') {
    throw 'WorkspacePath must be inside .artifacts/llm-wiki/tasks/<task-name>.'
}
$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
$journalPath = Join-Path $absoluteWorkspacePath 'journal.json'

function Write-Journal([object]$Journal) {
    if (-not (Test-Path -LiteralPath $absoluteWorkspacePath -PathType Container)) {
        New-Item -ItemType Directory -Path $absoluteWorkspacePath -Force | Out-Null
    }
    [System.IO.File]::WriteAllText(
        $journalPath,
        (($Journal | ConvertTo-Json -Depth 10) + [Environment]::NewLine),
        [System.Text.UTF8Encoding]::new($false))
}
function Read-Journal {
    if (-not (Test-Path -LiteralPath $journalPath -PathType Leaf)) {
        throw "Task journal does not exist: $normalizedWorkspacePath/journal.json"
    }
    return Get-Content -LiteralPath $journalPath -Raw | ConvertFrom-Json
}
function Get-Head {
    $head = git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD.' }
    return [string]$head
}
function Get-JournalView([object]$Journal) {
    $resolutions = @{}
    foreach ($event in @($Journal.events | Where-Object kind -eq 'resolution')) {
        $resolutions[[string]$event.targetId] = $event
    }
    $entries = @($Journal.events | Where-Object kind -eq 'entry' | ForEach-Object {
        $resolutionEvent = $resolutions[[string]$_.id]
        [pscustomobject][ordered]@{
            id = $_.id
            type = $_.type
            text = $_.text
            rationale = $_.rationale
            status = $(if ($null -ne $resolutionEvent) { 'resolved' } else { 'open' })
            createdAtUtc = $_.createdAtUtc
            gitHead = $_.gitHead
            resolution = $(if ($null -ne $resolutionEvent) { $resolutionEvent.text } else { '' })
            resolvedAtUtc = $(if ($null -ne $resolutionEvent) { $resolutionEvent.createdAtUtc } else { $null })
        }
    })
    return [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $normalizedWorkspacePath
        entryCount = $entries.Count
        openCount = @($entries | Where-Object status -eq 'open').Count
        openBlockerCount = @($entries | Where-Object { $_.type -eq 'blocker' -and $_.status -eq 'open' }).Count
        entries = $entries
    }
}
function Test-Journal([object]$Journal) {
    $issues = [System.Collections.Generic.List[string]]::new()
    if ($Journal.schemaVersion -ne 1) { $issues.Add('Unsupported journal schemaVersion.') }
    $knownEntries = @{}
    foreach ($event in @($Journal.events)) {
        if ($event.kind -eq 'entry') {
            if ([string]::IsNullOrWhiteSpace([string]$event.id)) {
                $issues.Add('Journal entry has no id.')
            } elseif ($knownEntries.ContainsKey([string]$event.id)) {
                $issues.Add("Duplicate journal entry id: $($event.id)")
            } else {
                $knownEntries[[string]$event.id] = $true
            }
            if ($event.type -notin @('decision', 'assumption', 'blocker', 'learning', 'note')) {
                $issues.Add("Invalid journal entry type: $($event.type)")
            }
            if ([string]::IsNullOrWhiteSpace([string]$event.text)) {
                $issues.Add("Journal entry '$($event.id)' has no text.")
            }
        } elseif ($event.kind -eq 'resolution') {
            if (-not $knownEntries.ContainsKey([string]$event.targetId)) {
                $issues.Add("Resolution targets an unknown or later entry: $($event.targetId)")
            }
            if ([string]::IsNullOrWhiteSpace([string]$event.text)) {
                $issues.Add("Resolution for '$($event.targetId)' has no text.")
            }
        } else {
            $issues.Add("Unknown journal event kind: $($event.kind)")
        }
    }
    return @($issues)
}

switch ($Action) {
    'init' {
        if (Test-Path -LiteralPath $journalPath) { throw "Task journal already exists: $normalizedWorkspacePath/journal.json" }
        Write-Journal ([ordered]@{
            schemaVersion = 1
            createdAtUtc = [DateTime]::UtcNow.ToString('o')
            events = @()
        })
        Write-Host "Initialized task journal: $normalizedWorkspacePath/journal.json"
    }
    'add' {
        if ([string]::IsNullOrWhiteSpace($Text)) { throw 'add requires -Text.' }
        $journal = Read-Journal
        $entryCount = @($journal.events | Where-Object kind -eq 'entry').Count
        $id = 'J-{0:d4}' -f ($entryCount + 1)
        $event = [pscustomobject][ordered]@{
            kind = 'entry'
            id = $id
            type = $JournalType
            text = $Text
            rationale = $Rationale
            createdAtUtc = [DateTime]::UtcNow.ToString('o')
            gitHead = Get-Head
        }
        $journal.events = @($journal.events) + $event
        Write-Journal $journal
        Write-Host "Added task journal entry $id ($JournalType)."
    }
    'resolve' {
        if ([string]::IsNullOrWhiteSpace($NoteId) -or [string]::IsNullOrWhiteSpace($Resolution)) {
            throw 'resolve requires -NoteId and -Resolution.'
        }
        $journal = Read-Journal
        $entry = @($journal.events | Where-Object { $_.kind -eq 'entry' -and $_.id -eq $NoteId })
        if ($entry.Count -eq 0) { throw "Unknown task journal entry: $NoteId" }
        if (@($journal.events | Where-Object { $_.kind -eq 'resolution' -and $_.targetId -eq $NoteId }).Count -gt 0) {
            throw "Task journal entry is already resolved: $NoteId"
        }
        $journal.events = @($journal.events) + [pscustomobject][ordered]@{
            kind = 'resolution'
            targetId = $NoteId
            text = $Resolution
            createdAtUtc = [DateTime]::UtcNow.ToString('o')
            gitHead = Get-Head
        }
        Write-Journal $journal
        Write-Host "Resolved task journal entry $NoteId."
    }
    'validate' {
        $journal = Read-Journal
        $issues = @(Test-Journal $journal)
        $result = [pscustomobject][ordered]@{
            valid = $issues.Count -eq 0
            eventCount = @($journal.events).Count
            issues = @($issues)
        }
        if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 6 } else {
            Write-Host "Task journal: valid=$($result.valid), events=$($result.eventCount)"
            foreach ($issue in $issues) { Write-Host " - $issue" }
        }
        if ($FailOnInvalid -and -not $result.valid) { exit 1 }
    }
    default {
        $view = Get-JournalView (Read-Journal)
        if ($Format -eq 'Json') { $view | ConvertTo-Json -Depth 8 } else {
            Write-Host "Task journal: $($view.entryCount) entries, $($view.openCount) open, $($view.openBlockerCount) open blocker(s)."
            foreach ($entry in $view.entries) {
                Write-Host " - [$($entry.status)/$($entry.type)] $($entry.id): $($entry.text)"
                if (-not [string]::IsNullOrWhiteSpace([string]$entry.resolution)) {
                    Write-Host "   Resolution: $($entry.resolution)"
                }
            }
        }
    }
}
