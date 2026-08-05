[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('promote', 'candidates', 'list', 'show', 'verify', 'relevant', 'supersede')]
    [string]$Action = 'list',
    [string]$WorkspacePath,
    [string]$JournalId,
    [string]$Id,
    [string[]]$ScopePath = @(),
    [string[]]$Tag = @(),
    [string[]]$Evidence = @(),
    [string]$Reason,
    [Nullable[int]]$ReviewAfterDays,
    [switch]$AllowDuplicate,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$canonicalKnowledgePath = [IO.Path]::GetFullPath((Join-Path $wikiRoot 'knowledge/memories.json'))
$knowledgePath = $canonicalKnowledgePath
if (-not [string]::IsNullOrWhiteSpace($env:LLM_WIKI_TEST_MEMORY_REGISTRY_PATH)) {
    $testRegistryRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki'))
    $candidateKnowledgePath = [IO.Path]::GetFullPath($env:LLM_WIKI_TEST_MEMORY_REGISTRY_PATH)
    $testRegistryPrefix = $testRegistryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $candidateKnowledgePath.StartsWith($testRegistryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'LLM_WIKI_TEST_MEMORY_REGISTRY_PATH must resolve under .artifacts/llm-wiki.'
    }
    $knowledgePath = $candidateKnowledgePath
}
$policy = Get-Content -LiteralPath (Join-Path $wikiRoot 'policies/workspace-policies.json') -Raw | ConvertFrom-Json
$memoryPolicy = $policy.scheduler.contextBundles.memory

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 30 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-FileSha([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Read-Registry {
    $registry = Get-Content -LiteralPath $knowledgePath -Raw | ConvertFrom-Json
    if ($registry.schemaVersion -ne 1 -or $null -eq $registry.events) { throw 'Unsupported memory registry schema.' }
    $registry
}
function Write-Registry([object]$Registry) {
    [IO.File]::WriteAllText($knowledgePath, (($Registry | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}
function Get-EventPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion = $Event.schemaVersion
        sequence = $Event.sequence
        kind = $Event.kind
        id = $Event.id
        createdAtUtc = $Event.createdAtUtc
        previousHash = $Event.previousHash
        memory = $Event.memory
        targetId = $Event.targetId
        reason = $Event.reason
    }
}
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    $previous = ''
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $sequence = 0
    foreach ($event in @($Registry.events)) {
        $sequence++
        if ([int]$event.sequence -ne $sequence) { $issues.Add("Event sequence is invalid at $sequence.") }
        if ([string]$event.previousHash -cne $previous) { $issues.Add("Event $sequence has invalid previousHash.") }
        if ([string]$event.eventHash -cne (Get-Hash (Get-EventPayload $event))) { $issues.Add("Event $sequence has invalid eventHash.") }
        if ($event.kind -eq 'promoted') {
            if (-not $ids.Add([string]$event.id)) { $issues.Add("Duplicate memory id: $($event.id)") }
            foreach ($field in @('statement', 'rationale')) {
                if ([string]::IsNullOrWhiteSpace([string]$event.memory.$field)) { $issues.Add("Memory '$($event.id)' has no $field.") }
            }
            if (@($event.memory.scopePaths).Count -eq 0) { $issues.Add("Memory '$($event.id)' has no scope paths.") }
            if (@($event.memory.evidence).Count -eq 0) { $issues.Add("Memory '$($event.id)' has no evidence.") }
            foreach ($pattern in @($event.memory.scopePaths)) {
                try { $null = [regex]::new([string]$pattern) } catch { $issues.Add("Memory '$($event.id)' has invalid scope regex '$pattern'.") }
            }
        } elseif ($event.kind -eq 'superseded') {
            if (-not $ids.Contains([string]$event.targetId)) { $issues.Add("Supersedence targets unknown memory '$($event.targetId)'.") }
            if ([string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Supersedence for '$($event.targetId)' has no reason.") }
        } else { $issues.Add("Unknown memory event kind '$($event.kind)'.") }
        $previous = [string]$event.eventHash
    }
    @($issues)
}
function Get-View([object]$Registry) {
    $superseded = @{}
    foreach ($event in @($Registry.events | Where-Object kind -eq 'superseded')) { $superseded[[string]$event.targetId] = $event }
    @($Registry.events | Where-Object kind -eq 'promoted' | ForEach-Object {
        $event = $_
        $changed = [Collections.Generic.List[string]]::new()
        foreach ($source in @($event.memory.sources)) {
            $current = Get-FileSha (Join-Path $repositoryRoot ([string]$source.path))
            if ([string]$source.sha256 -cne $current) { $changed.Add([string]$source.path) }
        }
        $age = [Math]::Max(0, ($AsOfUtc.ToUniversalTime() - ([DateTime]$event.createdAtUtc).ToUniversalTime()).TotalDays)
        $expired = $age -ge [int]$event.memory.reviewAfterDays
        [pscustomobject][ordered]@{
            id = $event.id
            statement = $event.memory.statement
            rationale = $event.memory.rationale
            scopePaths = @($event.memory.scopePaths)
            tags = @($event.memory.tags)
            evidence = @($event.memory.evidence)
            source = $event.memory.source
            sources = @($event.memory.sources)
            createdAtUtc = $event.createdAtUtc
            reviewAfterDays = [int]$event.memory.reviewAfterDays
            state = $(if ($superseded.ContainsKey([string]$event.id)) { 'superseded' } elseif ($changed.Count -gt 0 -or $expired) { 'stale' } else { 'active' })
            staleReasons = @(
                @($changed | ForEach-Object { "Source changed: $_" }) +
                $(if ($expired) { @("Review age exceeded: $([int][Math]::Floor($age)) days.") } else { @() })
            )
            eventHash = $event.eventHash
            supersededBy = $(if ($superseded.ContainsKey([string]$event.id)) { $superseded[[string]$event.id].eventHash } else { '' })
        }
    })
}
function Normalize-Workspace([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value) -or [IO.Path]::IsPathRooted($Value)) { throw 'WorkspacePath must be repository-relative.' }
    $normalized = $Value.Replace('\', '/').TrimEnd('/')
    if ($normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') { throw 'WorkspacePath must identify one task workspace.' }
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot "$normalized/workspace.json"))) { throw "Task workspace does not exist: $normalized" }
    $normalized
}
function Get-Terms([string]$Text) {
    @([regex]::Matches($Text.ToLowerInvariant(), '[\p{L}\p{N}]+') |
        ForEach-Object Value |
        Where-Object { $_.Length -ge 3 -and $_ -notin @('the', 'and', 'for', 'with', 'that', 'this', 'from', 'into') } |
        Sort-Object -Unique)
}
function Get-SimilarityPercent([string]$Left, [string]$Right) {
    $leftTerms = @(Get-Terms $Left)
    $rightTerms = @(Get-Terms $Right)
    $union = @($leftTerms + $rightTerms | Sort-Object -Unique)
    if ($union.Count -eq 0) { return 0 }
    $intersection = @($leftTerms | Where-Object { $_ -in $rightTerms })
    [Math]::Round(100 * $intersection.Count / $union.Count, 2)
}
function Get-DuplicateMatches([string]$Statement, [object[]]$Memories) {
    @($Memories | Where-Object state -eq 'active' | ForEach-Object {
        $similarity = Get-SimilarityPercent $Statement ([string]$_.statement)
        if ($similarity -ge [int]$memoryPolicy.duplicateSimilarityPercent) {
            [pscustomobject][ordered]@{ id = $_.id; similarityPercent = $similarity; statement = $_.statement }
        }
    } | Sort-Object @{Expression='similarityPercent';Descending=$true}, id)
}

$registry = Read-Registry
if ($Action -eq 'promote') {
    $workspace = Normalize-Workspace $WorkspacePath
    if ([string]::IsNullOrWhiteSpace($JournalId)) { throw 'promote requires -JournalId.' }
    $journal = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') show -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $entry = $journal.entries | Where-Object id -eq $JournalId | Select-Object -First 1
    if ($null -eq $entry -or $entry.type -notin @('decision', 'learning')) { throw 'Only an existing decision or learning journal entry can be promoted.' }
    if ([string]::IsNullOrWhiteSpace([string]$entry.rationale)) { throw 'The source journal entry must include rationale.' }
    $packet = Get-Content -LiteralPath (Join-Path $repositoryRoot "$workspace/change-packet.json") -Raw | ConvertFrom-Json
    $scope = if (@($ScopePath).Count -gt 0) { @($ScopePath) } else { @($packet.diff.changedPaths | ForEach-Object { '^' + [regex]::Escape([string]$_) + '$' }) }
    if ($scope.Count -eq 0) { throw 'promote requires scope paths or a task with changed paths.' }
    foreach ($pattern in $scope) { try { $null = [regex]::new($pattern) } catch { throw "Invalid scope regex '$pattern'." } }
    if (@($Evidence).Count -eq 0) { throw 'promote requires at least one -Evidence item.' }
    $memoryId = if ([string]::IsNullOrWhiteSpace($Id)) { "memory-$([Guid]::NewGuid().ToString('N').Substring(0,12))" } else { $Id.ToLowerInvariant() }
    if ($memoryId -notmatch '^[a-z0-9][a-z0-9-]{2,79}$') { throw 'Id must be a lowercase kebab-case identifier.' }
    if (@($registry.events | Where-Object { $_.kind -eq 'promoted' -and $_.id -eq $memoryId }).Count -gt 0) { throw "Memory already exists: $memoryId" }
    $duplicateMatches = @(Get-DuplicateMatches ([string]$entry.text) (Get-View $registry))
    if ($duplicateMatches.Count -gt 0 -and -not $AllowDuplicate) {
        throw "Memory duplicates '$($duplicateMatches[0].id)' at $($duplicateMatches[0].similarityPercent)% similarity. Reuse or supersede it; use -AllowDuplicate only with an explicit reason."
    }
    if ($AllowDuplicate -and [string]::IsNullOrWhiteSpace($Reason)) { throw 'AllowDuplicate requires -Reason.' }
    $sourcePaths = @($packet.diff.changedPaths | Where-Object { Test-Path -LiteralPath (Join-Path $repositoryRoot $_) -PathType Leaf } | Sort-Object -Unique)
    $days = if ($null -ne $ReviewAfterDays) { [int]$ReviewAfterDays } else { [int]$memoryPolicy.defaultReviewAfterDays }
    if ($days -lt 1 -or $days -gt [int]$memoryPolicy.maximumReviewAfterDays) { throw "ReviewAfterDays must be between 1 and $($memoryPolicy.maximumReviewAfterDays)." }
    $previous = if (@($registry.events).Count -gt 0) { [string]$registry.events[-1].eventHash } else { '' }
    $event = [pscustomobject][ordered]@{
        schemaVersion = 1; sequence = @($registry.events).Count + 1; kind = 'promoted'; id = $memoryId
        createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o'); previousHash = $previous
        memory = [pscustomobject][ordered]@{
            statement = [string]$entry.text; rationale = [string]$entry.rationale
            scopePaths = @($scope | Sort-Object -Unique); tags = @($Tag | Where-Object { $_ } | Sort-Object -Unique)
            evidence = @($Evidence); reviewAfterDays = $days
            duplicateOverride = $(if ($AllowDuplicate) { [pscustomobject][ordered]@{ reason = $Reason; matches = $duplicateMatches } } else { $null })
            source = [pscustomobject][ordered]@{ workspace = $workspace; journalId = $JournalId; journalGitHead = $entry.gitHead; packetFingerprint = [string]$packet.fingerprint }
            sources = @($sourcePaths | ForEach-Object { [pscustomobject][ordered]@{ path = $_; sha256 = Get-FileSha (Join-Path $repositoryRoot $_) } })
        }
        targetId = ''; reason = ''; eventHash = ''
    }
    $event.eventHash = Get-Hash (Get-EventPayload $event)
    $registry.events = @($registry.events) + $event
    Write-Registry $registry
    $result = [pscustomobject][ordered]@{ action = 'promote'; valid = $true; memory = (Get-View $registry | Where-Object id -eq $memoryId); eventHash = $event.eventHash }
} elseif ($Action -eq 'candidates') {
    $workspace = Normalize-Workspace $WorkspacePath
    $issues = Test-Registry $registry
    $journal = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') show -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $evidenceArtifact = Get-Content -LiteralPath (Join-Path $repositoryRoot "$workspace/evidence.json") -Raw | ConvertFrom-Json
    $availableEvidence = @(
        @($evidenceArtifact.checks | Where-Object status -eq 'passed' | ForEach-Object { "check:$($_.id):passed" }) +
        @($evidenceArtifact.reviews | Where-Object status -eq 'completed' | ForEach-Object { "review:$($_.id):completed" })
    )
    $view = Get-View $registry
    $candidates = @($journal.entries | Where-Object {
        $_.type -in @('decision', 'learning') -and $_.status -eq 'open'
    } | ForEach-Object {
        $score = $(if ($_.type -eq 'decision') { 60 } else { 50 })
        $reasons = [Collections.Generic.List[string]]::new()
        $reasons.Add("Journal type '$($_.type)' base score.")
        if (-not [string]::IsNullOrWhiteSpace([string]$_.rationale)) { $score += 20; $reasons.Add('Explicit rationale is present.') }
        if ($availableEvidence.Count -gt 0) { $score += [Math]::Min(20, 5 * $availableEvidence.Count); $reasons.Add("$($availableEvidence.Count) resolved evidence item(s) are available.") }
        $duplicates = @(Get-DuplicateMatches ([string]$_.text) $view)
        [pscustomobject][ordered]@{
            journalId = $_.id; type = $_.type; statement = $_.text; rationale = $_.rationale
            score = [Math]::Min(100, $score)
            eligible = $score -ge [int]$memoryPolicy.minimumCandidateScore -and -not [string]::IsNullOrWhiteSpace([string]$_.rationale) -and $availableEvidence.Count -gt 0
            reasons = @($reasons); suggestedEvidence = @($availableEvidence)
            duplicateMatches = $duplicates
            recommendation = $(if ($duplicates.Count -gt 0) { 'reuse-or-supersede' } elseif ($score -ge [int]$memoryPolicy.minimumCandidateScore -and $availableEvidence.Count -gt 0) { 'promote' } else { 'keep-task-local' })
        }
    } | Sort-Object @{Expression='eligible';Descending=$true}, @{Expression='score';Descending=$true}, journalId |
        Select-Object -First ([int]$memoryPolicy.maximumCandidates))
    $result = [pscustomobject][ordered]@{
        action = 'candidates'; valid = $issues.Count -eq 0; workspace = $workspace
        totalCount = $candidates.Count; eligibleCount = @($candidates | Where-Object eligible).Count
        duplicateCandidateCount = @($candidates | Where-Object { @($_.duplicateMatches).Count -gt 0 }).Count
        minimumCandidateScore = [int]$memoryPolicy.minimumCandidateScore
        registryFingerprint = Get-Hash @($registry.events.eventHash)
        issues = @($issues); candidates = $candidates
    }
} elseif ($Action -eq 'supersede') {
    if ([string]::IsNullOrWhiteSpace($Id) -or [string]::IsNullOrWhiteSpace($Reason)) { throw 'supersede requires -Id and -Reason.' }
    $view = Get-View $registry
    $target = $view | Where-Object id -eq $Id | Select-Object -First 1
    if ($null -eq $target -or $target.state -eq 'superseded') { throw "Active or stale memory not found: $Id" }
    $previous = if (@($registry.events).Count -gt 0) { [string]$registry.events[-1].eventHash } else { '' }
    $event = [pscustomobject][ordered]@{
        schemaVersion = 1; sequence = @($registry.events).Count + 1; kind = 'superseded'; id = ''
        createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o'); previousHash = $previous
        memory = $null; targetId = $Id; reason = $Reason; eventHash = ''
    }
    $event.eventHash = Get-Hash (Get-EventPayload $event)
    $registry.events = @($registry.events) + $event
    Write-Registry $registry
    $result = [pscustomobject][ordered]@{ action = 'supersede'; valid = $true; id = $Id; eventHash = $event.eventHash }
} else {
    $issues = Test-Registry $registry
    $view = Get-View $registry
    if ($Action -eq 'show') {
        if ([string]::IsNullOrWhiteSpace($Id)) { throw 'show requires -Id.' }
        $view = @($view | Where-Object id -eq $Id)
    } elseif ($Action -eq 'relevant') {
        $workspace = Normalize-Workspace $WorkspacePath
        $packet = Get-Content -LiteralPath (Join-Path $repositoryRoot "$workspace/change-packet.json") -Raw | ConvertFrom-Json
        $paths = @($packet.diff.changedPaths)
        $view = @($view | Where-Object {
            $_.state -eq 'active' -and @($_.scopePaths | Where-Object {
                $pattern = $_
                @($paths | Where-Object { $_ -match $pattern }).Count -gt 0
            }).Count -gt 0
        } | Select-Object -First ([int]$memoryPolicy.maximumRelevantItems))
    }
    $result = [pscustomobject][ordered]@{
        action = $Action; valid = $issues.Count -eq 0; totalCount = @($view).Count
        activeCount = @($view | Where-Object state -eq 'active').Count
        staleCount = @($view | Where-Object state -eq 'stale').Count
        supersededCount = @($view | Where-Object state -eq 'superseded').Count
        registryFingerprint = Get-Hash @($registry.events.eventHash)
        issues = @($issues); memories = @($view)
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    if ($result.action -eq 'promote') {
        Write-Host "Promoted durable memory: $($result.memory.id), state=$($result.memory.state), hash=$($result.eventHash)"
    } elseif ($result.action -eq 'supersede') {
        Write-Host "Superseded durable memory: $($result.id), hash=$($result.eventHash)"
    } else {
        Write-Host "Durable memory: action=$($result.action), valid=$($result.valid), total=$($result.totalCount)"
        foreach ($memory in @($result.memories | Where-Object { $null -ne $_ })) { Write-Host " - [$($memory.state)] $($memory.id): $($memory.statement)" }
        foreach ($candidate in @($result.candidates | Where-Object { $null -ne $_ })) { Write-Host " - [$($candidate.recommendation)/$($candidate.score)] $($candidate.journalId): $($candidate.statement)" }
    }
    foreach ($issue in @($result.issues | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
