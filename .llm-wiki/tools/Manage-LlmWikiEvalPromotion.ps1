[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('observe', 'candidates', 'list', 'show', 'approve', 'reject', 'apply', 'rollback', 'verify')]
    [string]$Action = 'list',
    [string]$WorkspacePath,
    [string]$Id,
    [string]$Reason,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Format-LlmWikiLearningResults.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$knowledgeRoot = if ([string]::IsNullOrWhiteSpace($env:LLM_WIKI_TEST_KNOWLEDGE_ROOT)) {
    Join-Path $wikiRoot 'knowledge'
} else {
    $candidate = [IO.Path]::GetFullPath($env:LLM_WIKI_TEST_KNOWLEDGE_ROOT)
    $artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki'))
    if (-not $candidate.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'LLM_WIKI_TEST_KNOWLEDGE_ROOT must resolve under .artifacts/llm-wiki.' }
    $candidate
}
$registryPath = Join-Path $knowledgeRoot 'eval-promotions.json'
$policy = Get-Content -LiteralPath (Join-Path $wikiRoot 'policies/workspace-policies.json') -Raw | ConvertFrom-Json
$evalPolicy = $policy.scheduler.evalPromotion

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 50 -Compress
    if ($null -eq $json) { $json = 'null' }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Read-Registry {
    $value = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    if ($value.schemaVersion -ne 1 -or $null -eq $value.events) { throw 'Unsupported eval-promotion registry schema.' }
    $value
}
function Write-Registry([object]$Value) {
    [IO.File]::WriteAllText($registryPath, (($Value | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}
function Get-EventPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Event.schemaVersion
        sequence = [int]$Event.sequence
        kind = [string]$Event.kind
        candidateId = [string]$Event.candidateId
        createdAtUtc = ([DateTimeOffset]$Event.createdAtUtc).ToUniversalTime().ToString('o')
        previousHash = [string]$Event.previousHash
        observation = $Event.observation
        reason = $(if ($null -eq $Event.reason) { $null } else { [string]$Event.reason })
    }
}
function Add-Event([object]$Registry, [string]$Kind, [string]$CandidateId, [object]$Observation, [string]$EventReason, [string]$CreatedAtUtc) {
    $event = [pscustomobject][ordered]@{
        schemaVersion = 1
        sequence = @($Registry.events).Count + 1
        kind = $Kind
        candidateId = $CandidateId
        createdAtUtc = $CreatedAtUtc
        previousHash = $(if (@($Registry.events).Count -eq 0) { '' } else { [string]$Registry.events[-1].eventHash })
        observation = $Observation
        reason = $EventReason
        eventHash = ''
    }
    $event.eventHash = Get-Hash (Get-EventPayload $event)
    $Registry.events = @($Registry.events) + $event
    $event
}
function Get-View([object]$Registry) {
    $states = [ordered]@{}
    foreach ($event in @($Registry.events)) {
        $candidateId = [string]$event.candidateId
        if (-not $states.Contains($candidateId)) {
            $states[$candidateId] = [pscustomobject][ordered]@{
                id = $candidateId; observation = $null; decision = 'pending'; materialization = 'not-applied'
                decisionReason = ''; materializationReason = ''; headEventHash = ''
            }
        }
        $state = $states[$candidateId]
        switch ($event.kind) {
            'observed' { $state.observation = $event.observation }
            'approved' { $state.decision = 'approved'; $state.decisionReason = [string]$event.reason }
            'rejected' { $state.decision = 'rejected'; $state.decisionReason = [string]$event.reason }
            'applied' { $state.materialization = 'applied'; $state.materializationReason = [string]$event.reason }
            'rolled-back' { $state.materialization = 'rolled-back'; $state.materializationReason = [string]$event.reason }
        }
        $state.headEventHash = [string]$event.eventHash
    }
    @($states.Values | ForEach-Object {
        $state = $_
        [pscustomobject][ordered]@{
            id = $state.id
            workspace = $state.observation.workspace
            retrospectiveHash = $state.observation.retrospectiveHash
            signals = @($state.observation.signals)
            case = $state.observation.case
            caseHash = $state.observation.caseHash
            decision = $state.decision
            decisionReason = $state.decisionReason
            materialization = $state.materialization
            materializationReason = $state.materializationReason
            headEventHash = $state.headEventHash
        }
    } | Sort-Object id)
}
function Test-Case([object]$Case, [Collections.Generic.List[string]]$Issues, [string]$Prefix) {
    if ([string]::IsNullOrWhiteSpace([string]$Case.id)) { $Issues.Add("$Prefix has no id.") }
    if (@($Case.changedPaths).Count -eq 0 -or @($Case.changedPaths).Count -gt [int]$evalPolicy.maximumChangedPaths) { $Issues.Add("$Prefix changedPaths count is invalid.") }
    foreach ($field in @('changedPaths', 'expectedModules', 'expectedScopes', 'expectedRules', 'expectedChecks', 'expectedViolationRules')) {
        $values = @($Case.$field)
        if (@($values | Sort-Object -Unique).Count -ne $values.Count) { $Issues.Add("$Prefix $field contains duplicates.") }
    }
}
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    $states = @{}
    $previous = ''
    $sequence = 0
    foreach ($event in @($Registry.events)) {
        $sequence++
        if ([int]$event.sequence -ne $sequence) { $issues.Add("Event sequence is invalid at $sequence.") }
        if ([string]$event.previousHash -cne $previous) { $issues.Add("Event $sequence has invalid previousHash.") }
        if ([string]$event.eventHash -cne (Get-Hash (Get-EventPayload $event))) { $issues.Add("Event $sequence has invalid eventHash.") }
        $candidateId = [string]$event.candidateId
        $prior = if ($states.ContainsKey($candidateId)) { $states[$candidateId] } else { $null }
        if ($event.kind -eq 'observed') {
            if ($null -ne $prior) { $issues.Add("Candidate '$candidateId' was observed more than once.") }
            if ($null -eq $event.observation) {
                $issues.Add("Observation $sequence is absent.")
            } else {
                Test-Case $event.observation.case $issues "Observation $sequence"
                if ([string]$event.observation.case.id -cne $candidateId) { $issues.Add("Observation $sequence candidate id drifted.") }
                if ([string]$event.observation.caseHash -cne (Get-Hash $event.observation.case)) { $issues.Add("Observation $sequence caseHash is invalid.") }
                if (@($event.observation.signals).Count -eq 0 -or @($event.observation.signals).Count -gt [int]$evalPolicy.maximumSignals) { $issues.Add("Observation $sequence signal count is invalid.") }
                if (@($event.observation.signals | Where-Object { [int]$_.score -lt [int]$evalPolicy.minimumSignalScore }).Count -gt 0) { $issues.Add("Observation $sequence contains a weak signal.") }
            }
            $states[$candidateId] = [pscustomobject]@{ decision = 'pending'; materialization = 'not-applied' }
        } elseif ($event.kind -in @('approved', 'rejected')) {
            if ($null -eq $prior -or $prior.decision -ne 'pending') { $issues.Add("Decision $sequence has invalid prior state.") }
            if ([bool]$evalPolicy.approvalRequiresHumanReason -and [string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Decision $sequence has no reason.") }
            if ($null -ne $prior) { $prior.decision = $(if ($event.kind -eq 'approved') { 'approved' } else { 'rejected' }) }
        } elseif ($event.kind -eq 'applied') {
            if ($null -eq $prior -or $prior.decision -ne 'approved' -or $prior.materialization -eq 'applied') { $issues.Add("Application $sequence has invalid prior state.") }
            if ([string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Application $sequence has no reason.") }
            if ($null -ne $prior) { $prior.materialization = 'applied' }
        } elseif ($event.kind -eq 'rolled-back') {
            if ($null -eq $prior -or $prior.materialization -ne 'applied') { $issues.Add("Rollback $sequence has invalid prior state.") }
            if ([string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Rollback $sequence has no reason.") }
            if ($null -ne $prior) { $prior.materialization = 'rolled-back' }
        } else {
            $issues.Add("Unknown eval-promotion event kind '$($event.kind)'.")
        }
        $previous = [string]$event.eventHash
    }
    if (@($states.Keys).Count -gt [int]$evalPolicy.maximumCandidates) { $issues.Add('Eval-promotion registry exceeds maximumCandidates.') }
    @($issues)
}
function New-Observation([string]$Workspace) {
    $normalized = $Workspace.Replace('\', '/').TrimEnd('/')
    if ([IO.Path]::IsPathRooted($Workspace) -or $normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') { throw 'WorkspacePath must identify one task workspace.' }
    $retrospectiveResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRetrospective.ps1') verify -WorkspacePath $normalized -Format Json | ConvertFrom-Json
    if (-not $retrospectiveResult.valid) { throw "A valid sealed retrospective is required: $(@($retrospectiveResult.issues) -join ' ')" }
    $retrospective = $retrospectiveResult.retrospective
    $signals = @($retrospective.learningCandidates | Where-Object { [int]$_.score -ge [int]$evalPolicy.minimumSignalScore } | Select-Object -First ([int]$evalPolicy.maximumSignals))
    if ($signals.Count -eq 0) { return $null }
    $absolute = Join-Path $repositoryRoot $normalized
    $packet = Get-Content -LiteralPath (Join-Path $absolute 'change-packet.json') -Raw | ConvertFrom-Json
    $changedPaths = @($packet.diff.changedPaths | Sort-Object -Unique)
    if ($changedPaths.Count -eq 0) { throw 'Cannot create an eval candidate without changed paths.' }
    if ($changedPaths.Count -gt [int]$evalPolicy.maximumChangedPaths) { throw "Eval candidate exceeds maximumChangedPaths ($($evalPolicy.maximumChangedPaths))." }
    $diff = & (Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1') -ChangedPath $changedPaths -Format Json | ConvertFrom-Json
    $changePolicy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') -ChangedPath $changedPaths -Format Json | ConvertFrom-Json
    $identity = Get-Hash ([pscustomobject][ordered]@{ changedPaths = $changedPaths; signalIds = @($signals.id | Sort-Object -Unique) })
    $candidateId = "learned-$($identity.Substring(0, 16))"
    $case = [pscustomobject][ordered]@{
        id = $candidateId
        changedPaths = $changedPaths
        expectedModules = @($diff.modules.name | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        expectedScopes = @($diff.scopes | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        expectedRules = @($changePolicy.matchedRules.id | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        expectedChecks = @($changePolicy.requiredChecks.id | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        expectedViolationRules = @(
            @($changePolicy.violations) |
                ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['rule']) { [string]$_.rule } } |
                Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
                Sort-Object -Unique
        )
    }
    [pscustomobject][ordered]@{
        workspace = $normalized
        retrospectiveHash = [string]$retrospective.retrospectiveHash
        packetFingerprint = [string]$retrospective.packetFingerprint
        signals = @($signals | ForEach-Object {
            [pscustomobject][ordered]@{ id = $_.id; type = $_.type; score = [int]$_.score; statement = $_.statement; evidence = @($_.evidence) }
        })
        case = $case
        caseHash = Get-Hash $case
    }
}
function Test-CaseExecution([object]$Case) {
    $diff = & (Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1') -ChangedPath @($Case.changedPaths) -Format Json | ConvertFrom-Json
    $changePolicy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') -ChangedPath @($Case.changedPaths) -Format Json | ConvertFrom-Json
    $actual = @{
        expectedModules = @($diff.modules.name); expectedScopes = @($diff.scopes)
        expectedRules = @($changePolicy.matchedRules.id); expectedChecks = @($changePolicy.requiredChecks.id)
        expectedViolationRules = @(
            @($changePolicy.violations) |
                ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['rule']) { [string]$_.rule } }
        )
    }
    $missing = [Collections.Generic.List[string]]::new()
    foreach ($field in $actual.Keys) {
        foreach ($value in @($Case.$field)) {
            if (@($actual[$field]) -notcontains $value) { $missing.Add("${field}:$value") }
        }
    }
    $unexpectedViolations = @($actual.expectedViolationRules | Where-Object {
        -not [string]::IsNullOrWhiteSpace([string]$_) -and @($Case.expectedViolationRules) -notcontains $_
    })
    foreach ($value in $unexpectedViolations) { $missing.Add("unexpectedViolation:$value") }
    [pscustomobject][ordered]@{ valid = $missing.Count -eq 0; missing = @($missing) }
}

$registry = Read-Registry
$now = $AsOfUtc.ToUniversalTime().ToString('o')
$result = $null
if ($Action -eq 'observe') {
    if ([string]::IsNullOrWhiteSpace($WorkspacePath)) { throw 'WorkspacePath is required.' }
    $observation = New-Observation $WorkspacePath
    if ($null -eq $observation) {
        $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $true; addedCount = 0; candidate = $null; eventHash = ''; issues = @() }
    } else {
        $existing = Get-View $registry | Where-Object id -eq $observation.case.id
        if ($null -ne $existing) {
            $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $true; addedCount = 0; candidate = $existing; eventHash = ''; issues = @() }
        } else {
            $event = Add-Event $registry 'observed' $observation.case.id $observation '' $now
            $issues = @(Test-Registry $registry)
            if ($issues.Count -eq 0) { Write-Registry $registry }
            $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $issues.Count -eq 0; addedCount = 1; candidate = (Get-View $registry | Where-Object id -eq $observation.case.id); eventHash = $event.eventHash; issues = $issues }
        }
    }
} elseif ($Action -in @('approve', 'reject', 'apply', 'rollback')) {
    if ([string]::IsNullOrWhiteSpace($Id) -or [string]::IsNullOrWhiteSpace($Reason)) { throw 'Id and Reason are required.' }
    $candidate = Get-View $registry | Where-Object id -eq $Id
    if ($null -eq $candidate) { throw "Eval candidate not found: $Id" }
    if ($Action -in @('approve', 'reject') -and $candidate.decision -ne 'pending') { throw 'Only a pending eval candidate can receive a decision.' }
    if ($Action -eq 'apply') {
        if ($candidate.decision -ne 'approved' -or $candidate.materialization -eq 'applied') { throw 'Only an approved, unapplied eval candidate can be applied.' }
        if ([bool]$evalPolicy.requirePassingBeforeApply) {
            $execution = Test-CaseExecution $candidate.case
            if (-not $execution.valid) { throw "Eval candidate does not currently pass: $(@($execution.missing) -join ', ')" }
        }
    }
    if ($Action -eq 'rollback' -and $candidate.materialization -ne 'applied') { throw 'Only an applied eval candidate can be rolled back.' }
    $kind = @{ approve = 'approved'; reject = 'rejected'; apply = 'applied'; rollback = 'rolled-back' }[$Action]
    $event = Add-Event $registry $kind $Id $null $Reason $now
    $issues = @(Test-Registry $registry)
    if ($issues.Count -eq 0) { Write-Registry $registry }
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $issues.Count -eq 0; candidate = (Get-View $registry | Where-Object id -eq $Id); eventHash = $event.eventHash; issues = $issues }
} else {
    $issues = @(Test-Registry $registry)
    $all = @(Get-View $registry)
    $candidates = $all
    if ($Action -eq 'candidates') { $candidates = @($all | Where-Object decision -eq 'pending') }
    if ($Action -eq 'show') {
        if ([string]::IsNullOrWhiteSpace($Id)) { throw 'Id is required.' }
        $candidates = @($all | Where-Object id -eq $Id)
        if ($candidates.Count -eq 0) { throw "Eval candidate not found: $Id" }
    }
    $result = [pscustomobject][ordered]@{
        action = $Action
        valid = $issues.Count -eq 0
        totalCount = $all.Count
        pendingCount = @($all | Where-Object decision -eq 'pending').Count
        approvedCount = @($all | Where-Object decision -eq 'approved').Count
        appliedCount = @($all | Where-Object materialization -eq 'applied').Count
        rolledBackCount = @($all | Where-Object materialization -eq 'rolled-back').Count
        candidates = $candidates
        registryFingerprint = Get-Hash @($registry.events)
        issues = $issues
    }
}
if ($FailOnInvalid -and -not $result.valid) { throw "Eval-promotion registry is invalid: $(@($result.issues) -join ' ')" }
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 50 } else {
    Write-LlmWikiEvalPromotionResult $result
}
