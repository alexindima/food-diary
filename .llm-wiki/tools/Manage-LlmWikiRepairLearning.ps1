[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('candidates', 'promote', 'list', 'verify', 'relevant')]
    [string]$Action = 'list',
    [string]$WorkspacePath,
    [string]$CandidateId,
    [string]$CheckId,
    [string]$Category,
    [string[]]$Path = @(),
    [string]$Owner,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$registryPath = Join-Path $wikiRoot 'knowledge/repair-learnings.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = (Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json).repairLoop

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-EventPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion = $Event.schemaVersion; sequence = $Event.sequence; id = $Event.id
        candidateFingerprint = $Event.candidateFingerprint; promotedAtUtc = $Event.promotedAtUtc
        owner = $Event.owner; source = $Event.source; learning = $Event.learning
        policyFingerprint = $Event.policyFingerprint; previousHash = $Event.previousHash
    }
}
function Get-RegistryPayload([object]$Registry) {
    [pscustomobject][ordered]@{ schemaVersion = $Registry.schemaVersion; events = @($Registry.events) }
}
function Read-Registry {
    if (-not (Test-Path -LiteralPath $registryPath -PathType Leaf)) { throw 'Repair learning registry is absent.' }
    Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
}
function Write-Registry([object]$Registry) {
    $Registry.registryHash = Get-Hash (Get-RegistryPayload $Registry)
    [IO.File]::WriteAllText($registryPath, (($Registry | ConvertTo-Json -Depth 40) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    if ($Registry.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    $previousHash = ''
    $sequence = 1
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $fingerprints = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($event in @($Registry.events)) {
        if ([int]$event.sequence -ne $sequence) { $issues.Add("Event sequence is invalid at $($event.id).") }
        if (-not $ids.Add([string]$event.id)) { $issues.Add("Duplicate repair learning id: $($event.id)") }
        if (-not $fingerprints.Add([string]$event.candidateFingerprint)) { $issues.Add("Duplicate repair learning fingerprint at $($event.id).") }
        if ([string]$event.previousHash -cne $previousHash) { $issues.Add("Repair learning hash chain is invalid at $($event.id).") }
        if ([string]$event.eventHash -cne (Get-Hash (Get-EventPayload $event))) { $issues.Add("Repair learning event hash is invalid at $($event.id).") }
        if ([string]$event.learning.category -notin @($policy.allowedCategories)) { $issues.Add("Repair learning category is invalid at $($event.id).") }
        if ([int]$event.learning.confidence -lt [int]$policy.learningMinimumConfidence -or [int]$event.learning.confidence -gt 100) {
            $issues.Add("Repair learning confidence is invalid at $($event.id).")
        }
        if (@($event.learning.repairPaths).Count -eq 0 -or @($event.learning.repairPaths).Count -gt [int]$policy.maximumRepairPaths) {
            $issues.Add("Repair learning scope is invalid at $($event.id).")
        }
        $previousHash = [string]$event.eventHash
        $sequence++
    }
    if ([string]$Registry.registryHash -cne (Get-Hash (Get-RegistryPayload $Registry))) { $issues.Add('Repair learning registry hash is invalid.') }
    [pscustomobject]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}
function Get-Workspace([string]$RequestedWorkspace) {
    if ([string]::IsNullOrWhiteSpace($RequestedWorkspace)) { throw "$Action requires WorkspacePath." }
    $normalized = $RequestedWorkspace.Replace('\', '/').TrimEnd('/')
    if ([IO.Path]::IsPathRooted($RequestedWorkspace) -or $normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
        throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
    }
    $absolute = Join-Path $repositoryRoot $normalized
    if (-not (Test-Path -LiteralPath $absolute -PathType Container)) { throw "Task workspace does not exist: $normalized" }
    [pscustomobject]@{ relative = $normalized; absolute = $absolute }
}
function Get-Candidates([object]$Workspace) {
    $repair = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLoop.ps1') verify -WorkspacePath $Workspace.relative -Format Json | ConvertFrom-Json
    if (-not $repair.valid) { throw "Repair registry is invalid: $(@($repair.issues) -join ' ')" }
    @($repair.registry.attempts | Where-Object state -eq 'completed' | ForEach-Object {
        $attempt = $_
        $priorFailures = @($repair.registry.attempts | Where-Object {
            $_.checkId -eq $attempt.checkId -and $_.sequence -lt $attempt.sequence -and $_.state -eq 'failed'
        }).Count
        $confidence = [Math]::Min(100,
            45 +
            $(if ($attempt.proof.checkStatus -eq 'passed' -and -not [string]::IsNullOrWhiteSpace([string]$attempt.proof.lineageHash)) { 25 } else { 0 }) +
            [Math]::Min(15, $priorFailures * 5) +
            $(if ([string]$attempt.category -ne 'unknown') { 5 } else { 0 }) +
            $(if ([string]$attempt.resolution.Length -ge 20) { 5 } else { 0 }) +
            $(if ([string]$attempt.hypothesis.Length -ge 20) { 5 } else { 0 }))
        $fingerprint = Get-Hash ([pscustomobject][ordered]@{
            category = $attempt.category; symptom = $attempt.symptom; hypothesis = $attempt.hypothesis
            resolution = $attempt.resolution; repairPaths = @($attempt.repairPaths); checkId = $attempt.checkId
        })
        [pscustomobject][ordered]@{
            id = "repair-$($fingerprint.Substring(0, 12))"; fingerprint = $fingerprint
            workspace = $Workspace.relative; attemptId = $attempt.id; attemptHash = $attempt.attemptHash
            checkId = $attempt.checkId; category = $attempt.category; symptom = $attempt.symptom
            hypothesis = $attempt.hypothesis; resolution = $attempt.resolution
            repairPaths = @($attempt.repairPaths); proof = $attempt.proof
            priorFailedAttempts = $priorFailures; confidence = $confidence
            eligible = $confidence -ge [int]$policy.learningMinimumConfidence
        }
    })
}

$registry = Read-Registry
$validation = Test-Registry $registry
if ($Action -eq 'verify') {
    $result = [pscustomobject][ordered]@{ action = 'verify'; valid = $validation.valid; totalCount = @($registry.events).Count; issues = @($validation.issues) }
} elseif (-not $validation.valid) {
    throw "Repair learning registry is invalid: $(@($validation.issues) -join ' ')"
} elseif ($Action -eq 'candidates') {
    $workspace = Get-Workspace $WorkspacePath
    $candidates = @(Get-Candidates $workspace)
    $result = [pscustomobject][ordered]@{
        action = 'candidates'; valid = $true; workspace = $workspace.relative
        minimumConfidence = [int]$policy.learningMinimumConfidence
        eligibleCount = @($candidates | Where-Object eligible).Count; candidates = $candidates
    }
} elseif ($Action -eq 'promote') {
    if ([string]::IsNullOrWhiteSpace($CandidateId) -or [string]::IsNullOrWhiteSpace($Owner)) { throw 'promote requires CandidateId and Owner.' }
    $workspace = Get-Workspace $WorkspacePath
    $candidate = Get-Candidates $workspace | Where-Object id -eq $CandidateId | Select-Object -First 1
    if ($null -eq $candidate) { throw "Unknown repair learning candidate: $CandidateId" }
    if (-not $candidate.eligible) { throw "Repair learning candidate confidence is below $($policy.learningMinimumConfidence)." }
    if (@($registry.events | Where-Object candidateFingerprint -eq $candidate.fingerprint).Count -gt 0) { throw 'Equivalent repair learning is already promoted.' }
    $previousHash = if (@($registry.events).Count -eq 0) { '' } else { [string]$registry.events[-1].eventHash }
    $event = [pscustomobject][ordered]@{
        schemaVersion = 1; sequence = @($registry.events).Count + 1; id = $candidate.id
        candidateFingerprint = $candidate.fingerprint; promotedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o'); owner = $Owner
        source = [pscustomobject][ordered]@{
            workspace = $workspace.relative; attemptId = $candidate.attemptId; attemptHash = $candidate.attemptHash
        }
        learning = [pscustomobject][ordered]@{
            checkId = $candidate.checkId; category = $candidate.category; symptom = $candidate.symptom
            hypothesis = $candidate.hypothesis; resolution = $candidate.resolution
            repairPaths = @($candidate.repairPaths); confidence = $candidate.confidence
            priorFailedAttempts = $candidate.priorFailedAttempts; proof = $candidate.proof
        }
        policyFingerprint = (Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant()
        previousHash = $previousHash; eventHash = ''
    }
    $event.eventHash = Get-Hash (Get-EventPayload $event)
    $registry.events = @($registry.events) + $event
    Write-Registry $registry
    $result = [pscustomobject][ordered]@{ action = 'promote'; valid = $true; learning = $event; registryHash = $registry.registryHash }
} else {
    $events = @($registry.events)
    if ($Action -eq 'relevant') {
        $paths = @($Path | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
        $events = @($events | Where-Object {
            $event = $_
            ([string]::IsNullOrWhiteSpace($CheckId) -or $event.learning.checkId -eq $CheckId) -and
            ([string]::IsNullOrWhiteSpace($Category) -or $event.learning.category -eq $Category) -and
            ($paths.Count -eq 0 -or @($event.learning.repairPaths | Where-Object { $_ -in $paths }).Count -gt 0)
        } | Sort-Object { [int]$_.learning.confidence } -Descending | Select-Object -First ([int]$policy.learningMaximumMatches))
    }
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $true; totalCount = $events.Count; learnings = $events; issues = @() }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 40 } else {
    Write-Host "Repair learning: action=$Action, valid=$($result.valid), total=$(@($result.learnings).Count + @($result.candidates).Count)"
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
