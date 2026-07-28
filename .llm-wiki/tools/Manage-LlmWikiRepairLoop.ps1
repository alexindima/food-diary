[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('suggest', 'start', 'complete', 'fail', 'show', 'verify')]
    [string]$Action = 'show',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$CheckId,
    [string]$AttemptId,
    [string]$Symptom,
    [string]$Hypothesis,
    [string[]]$RepairPath,
    [string]$Owner,
    [string]$Resolution,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$normalizedWorkspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $normalizedWorkspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$workspaceAbsolute = Join-Path $repositoryRoot $normalizedWorkspace
$packetPath = Join-Path $workspaceAbsolute 'change-packet.json'
$manifestPath = Join-Path $workspaceAbsolute 'change-manifest.json'
$acceptancePath = Join-Path $workspaceAbsolute 'acceptance-matrix.json'
$evidencePath = Join-Path $workspaceAbsolute 'evidence.json'
$registryPath = Join-Path $workspaceAbsolute 'repair-loop.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$knowledgePath = Join-Path $wikiRoot 'knowledge/failures.json'
foreach ($requiredPath in @($packetPath, $manifestPath, $acceptancePath, $evidencePath, $policyPath, $knowledgePath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) { throw "Required repair-loop input is absent: $requiredPath" }
}
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$repairPolicy = $policy.repairLoop

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    if ($null -eq $json) { $json = 'null' }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-FileHashValue([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-Category([string]$Id, [string]$Text) {
    $value = "$Id $Text"
    if ($value -match '(?i)compile|build|CS\d{4}') { return 'compile' }
    if ($value -match '(?i)format|whitespace') { return 'format' }
    if ($value -match '(?i)lint|eslint') { return 'lint' }
    if ($value -match '(?i)contract|compatib|snapshot') { return 'contract' }
    if ($value -match '(?i)architecture|dependency') { return 'architecture' }
    if ($value -match '(?i)docker|database|migration|network|timeout|infrastructure') { return 'infrastructure' }
    if ($value -match '(?i)test|spec|assert|expected|failed') { return 'test' }
    'unknown'
}
function Get-AttemptPayload([object]$Attempt) {
    [pscustomobject][ordered]@{
        id = $Attempt.id
        sequence = $Attempt.sequence
        checkId = $Attempt.checkId
        category = $Attempt.category
        symptom = $Attempt.symptom
        hypothesis = $Attempt.hypothesis
        repairPaths = @($Attempt.repairPaths)
        owner = $Attempt.owner
        state = $Attempt.state
        startedAtUtc = $Attempt.startedAtUtc
        finishedAtUtc = $Attempt.finishedAtUtc
        resolution = $Attempt.resolution
        packetFingerprint = $Attempt.packetFingerprint
        policyFingerprint = $Attempt.policyFingerprint
        evidenceHashAtStart = $Attempt.evidenceHashAtStart
        evidenceHashAtFinish = $Attempt.evidenceHashAtFinish
        proof = $Attempt.proof
        attemptFingerprint = $Attempt.attemptFingerprint
        previousHash = $Attempt.previousHash
    }
}
function Get-RegistryPayload([object]$Registry) {
    [pscustomobject][ordered]@{
        schemaVersion = $Registry.schemaVersion
        workspace = $Registry.workspace
        attempts = @($Registry.attempts)
    }
}
function New-Registry {
    [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $normalizedWorkspace
        attempts = @()
        registryHash = ''
    }
}
function Read-Registry {
    if (Test-Path -LiteralPath $registryPath -PathType Leaf) {
        Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    } else {
        $registry = New-Registry
        Set-RegistryHash $registry
        $registry
    }
}
function Set-RegistryHash([object]$Registry) {
    $Registry.registryHash = Get-Hash (Get-RegistryPayload $Registry)
}
function Write-Registry([object]$Registry) {
    Set-RegistryHash $Registry
    $temporaryPath = "$registryPath.$([guid]::NewGuid().ToString('N')).tmp"
    try {
        [IO.File]::WriteAllText($temporaryPath, (($Registry | ConvertTo-Json -Depth 40) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        if (Test-Path -LiteralPath $registryPath) { [IO.File]::Delete($registryPath) }
        [IO.File]::Move($temporaryPath, $registryPath)
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [IO.File]::Delete($temporaryPath) }
    }
}
function Get-Inputs {
    [pscustomobject]@{
        packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
        manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        acceptance = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
        evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
        knowledge = Get-Content -LiteralPath $knowledgePath -Raw | ConvertFrom-Json
    }
}
function Get-PermittedPaths([object]$Inputs) {
    @(
        @($Inputs.packet.diff.changedPaths) +
        @($Inputs.manifest.scope.plannedPaths) +
        @($Inputs.acceptance.availableEvidence.testPaths)
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique
}
function Get-Suggestion([object]$Inputs, [string]$RequestedCheckId, [string]$RequestedSymptom) {
    $check = $Inputs.evidence.checks | Where-Object id -eq $RequestedCheckId | Select-Object -First 1
    if ($null -eq $check) { throw "Unknown evidence check: $RequestedCheckId" }
    $effectiveSymptom = if ([string]::IsNullOrWhiteSpace($RequestedSymptom)) {
        if ([string]::IsNullOrWhiteSpace([string]$check.reason)) { "Check '$RequestedCheckId' is $($check.status)." } else { [string]$check.reason }
    } else { $RequestedSymptom }
    $category = Get-Category $RequestedCheckId $effectiveSymptom
    $permitted = @(Get-PermittedPaths $Inputs)
    $knowledgeMatches = @($Inputs.knowledge.entries | Where-Object {
        $entry = $_
        $effectiveSymptom -match [regex]::Escape([string]$entry.id) -or
        $effectiveSymptom -match [regex]::Escape([string]$entry.symptom) -or
        @($permitted | Where-Object {
            $path = $_
            @($entry.pathPatterns | Where-Object { $path -match $_ }).Count -gt 0
        }).Count -gt 0
    })
    $recommendedPaths = @($permitted | Select-Object -First ([int]$repairPolicy.maximumRepairPaths))
    $promotedLearnings = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLearning.ps1') relevant `
        -CheckId $RequestedCheckId `
        -Category $category `
        -Path $permitted `
        -Format Json | ConvertFrom-Json
    [pscustomobject][ordered]@{
        checkId = $RequestedCheckId
        currentStatus = [string]$check.status
        symptom = $effectiveSymptom
        category = $category
        permittedPaths = $permitted
        recommendedPaths = $recommendedPaths
        knowledgeMatches = @($knowledgeMatches | ForEach-Object {
            [pscustomobject][ordered]@{ id = $_.id; cause = $_.cause; fix = $_.fix; verification = @($_.verification) }
        })
        promotedRepairLearnings = @($promotedLearnings.learnings | ForEach-Object {
            [pscustomobject][ordered]@{
                id = $_.id
                confidence = $_.learning.confidence
                hypothesis = $_.learning.hypothesis
                resolution = $_.learning.resolution
                repairPaths = @($_.learning.repairPaths)
                sourceAttemptHash = $_.source.attemptHash
            }
        })
    }
}
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    if ($Registry.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Registry.workspace -cne $normalizedWorkspace) { $issues.Add('Workspace does not match.') }
    $previousHash = ''
    $expectedSequence = 1
    foreach ($attempt in @($Registry.attempts)) {
        if ([int]$attempt.sequence -ne $expectedSequence) { $issues.Add("Attempt sequence is invalid at $($attempt.id).") }
        if ([string]$attempt.previousHash -cne $previousHash) { $issues.Add("Attempt hash chain is invalid at $($attempt.id).") }
        $expectedHash = Get-Hash (Get-AttemptPayload $attempt)
        if ([string]$attempt.attemptHash -cne $expectedHash) { $issues.Add("Attempt hash is invalid at $($attempt.id).") }
        if ([string]$attempt.category -notin @($repairPolicy.allowedCategories)) { $issues.Add("Attempt category is not allowed at $($attempt.id).") }
        if (@($attempt.repairPaths).Count -gt [int]$repairPolicy.maximumRepairPaths) { $issues.Add("Attempt repair scope is too broad at $($attempt.id).") }
        if ([string]$attempt.state -notin @('active', 'completed', 'failed')) { $issues.Add("Attempt state is invalid at $($attempt.id).") }
        $previousHash = [string]$attempt.attemptHash
        $expectedSequence++
    }
    if (@($Registry.attempts).Count -gt [int]$repairPolicy.maximumTotalAttempts) { $issues.Add('Repair registry exceeds the total attempt limit.') }
    foreach ($checkGroup in @($Registry.attempts | Group-Object checkId)) {
        if ($checkGroup.Count -gt [int]$repairPolicy.maximumAttemptsPerFailure) {
            $issues.Add("Repair registry exceeds the attempt limit for '$($checkGroup.Name)'.")
        }
    }
    if (@($Registry.attempts | Where-Object state -eq 'active').Count -gt 1) { $issues.Add('Repair registry has more than one active attempt.') }
    if ([string]$Registry.registryHash -cne (Get-Hash (Get-RegistryPayload $Registry))) { $issues.Add('Repair registry hash is invalid.') }
    $active = @($Registry.attempts | Where-Object state -eq 'active')
    $latestByCheck = @($Registry.attempts | Group-Object checkId | ForEach-Object { $_.Group | Sort-Object sequence -Descending | Select-Object -First 1 })
    $unresolved = @($latestByCheck | Where-Object state -ne 'completed')
    [pscustomobject]@{
        valid = $issues.Count -eq 0
        issues = @($issues)
        activeAttempts = $active
        unresolvedAttempts = $unresolved
    }
}

$inputs = Get-Inputs
if ($Action -eq 'suggest') {
    if ([string]::IsNullOrWhiteSpace($CheckId)) { throw 'suggest requires CheckId.' }
    $result = [pscustomobject][ordered]@{ action = 'suggest'; valid = $true; suggestion = Get-Suggestion $inputs $CheckId $Symptom }
} elseif ($Action -eq 'start') {
    foreach ($required in @('CheckId', 'Hypothesis', 'Owner')) {
        if ([string]::IsNullOrWhiteSpace([string](Get-Variable $required -ValueOnly))) { throw "start requires $required." }
    }
    $paths = @($RepairPath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
    if ($paths.Count -eq 0) { throw 'start requires RepairPath.' }
    if ($paths.Count -gt [int]$repairPolicy.maximumRepairPaths) { throw "Repair scope exceeds $($repairPolicy.maximumRepairPaths) paths." }
    $suggestion = Get-Suggestion $inputs $CheckId $Symptom
    if ($suggestion.currentStatus -ne 'failed') { throw "Repair attempts require a failed check; '$CheckId' is $($suggestion.currentStatus)." }
    $outside = @($paths | Where-Object { $_ -notin @($suggestion.permittedPaths) })
    if ($outside.Count -gt 0) { throw "Repair paths are outside the task plan: $($outside -join ', ')" }
    $registry = Read-Registry
    $validation = Test-Registry $registry
    if (-not $validation.valid) { throw "Repair registry is invalid: $(@($validation.issues) -join ' ')" }
    if (@($registry.attempts).Count -ge [int]$repairPolicy.maximumTotalAttempts) { throw 'Repair attempt limit is exhausted.' }
    if (@($registry.attempts | Where-Object checkId -eq $CheckId).Count -ge [int]$repairPolicy.maximumAttemptsPerFailure) { throw "Repair attempt limit is exhausted for '$CheckId'." }
    if (@($registry.attempts | Where-Object state -eq 'active').Count -gt 0) { throw 'Only one repair attempt may be active per workspace.' }
    $attemptFingerprint = Get-Hash ([pscustomobject][ordered]@{
        checkId = $CheckId
        category = $suggestion.category
        hypothesis = $Hypothesis.Trim().ToLowerInvariant()
        repairPaths = $paths
    })
    if (@($registry.attempts | Where-Object attemptFingerprint -eq $attemptFingerprint).Count -ge [int]$repairPolicy.maximumRepeatedAttemptFingerprint) {
        throw 'An equivalent repair attempt has already been tried; change the hypothesis or repair scope.'
    }
    $previousHash = if (@($registry.attempts).Count -eq 0) { '' } else { [string]$registry.attempts[-1].attemptHash }
    $attempt = [pscustomobject][ordered]@{
        id = [guid]::NewGuid().ToString('N')
        sequence = @($registry.attempts).Count + 1
        checkId = $CheckId
        category = [string]$suggestion.category
        symptom = [string]$suggestion.symptom
        hypothesis = $Hypothesis
        repairPaths = $paths
        owner = $Owner
        state = 'active'
        startedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        finishedAtUtc = $null
        resolution = $null
        packetFingerprint = [string]$inputs.packet.fingerprint
        policyFingerprint = Get-FileHashValue $policyPath
        evidenceHashAtStart = Get-FileHashValue $evidencePath
        evidenceHashAtFinish = $null
        proof = $null
        attemptFingerprint = $attemptFingerprint
        previousHash = $previousHash
        attemptHash = ''
    }
    $attempt.attemptHash = Get-Hash (Get-AttemptPayload $attempt)
    $registry.attempts = @($registry.attempts) + $attempt
    Write-Registry $registry
    $result = [pscustomobject][ordered]@{ action = 'start'; valid = $true; attempt = $attempt; registryHash = $registry.registryHash }
} elseif ($Action -in @('complete', 'fail')) {
    if ([string]::IsNullOrWhiteSpace($AttemptId) -or [string]::IsNullOrWhiteSpace($Resolution)) { throw "$Action requires AttemptId and Resolution." }
    $registry = Read-Registry
    $validation = Test-Registry $registry
    if (-not $validation.valid) { throw "Repair registry is invalid: $(@($validation.issues) -join ' ')" }
    $attempt = $registry.attempts | Where-Object id -eq $AttemptId | Select-Object -First 1
    if ($null -eq $attempt) { throw "Unknown repair attempt: $AttemptId" }
    if ($attempt.state -ne 'active') { throw "Repair attempt is already terminal: $($attempt.state)" }
    $evidenceHash = Get-FileHashValue $evidencePath
    $check = $inputs.evidence.checks | Where-Object id -eq $attempt.checkId | Select-Object -First 1
    if ($Action -eq 'complete') {
        if ($null -eq $check -or $check.status -ne 'passed') { throw "Repair completion requires check '$($attempt.checkId)' to be passed." }
        if ($evidenceHash -ceq [string]$attempt.evidenceHashAtStart) { throw 'Repair completion requires fresh evidence after the attempt started.' }
    }
    $attempt.state = if ($Action -eq 'complete') { 'completed' } else { 'failed' }
    $attempt.finishedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
    $attempt.resolution = $Resolution
    $attempt.evidenceHashAtFinish = $evidenceHash
    $attempt.proof = [pscustomobject][ordered]@{
        checkStatus = $(if ($null -eq $check) { $null } else { [string]$check.status })
        lineageHash = $(if ($null -eq $check -or $null -eq $check.lineage) { $null } else { Get-Hash $check.lineage })
    }
    $attempt.attemptHash = Get-Hash (Get-AttemptPayload $attempt)
    $attemptIndex = [int]$attempt.sequence - 1
    for ($index = $attemptIndex + 1; $index -lt @($registry.attempts).Count; $index++) {
        $registry.attempts[$index].previousHash = [string]$registry.attempts[$index - 1].attemptHash
        $registry.attempts[$index].attemptHash = Get-Hash (Get-AttemptPayload $registry.attempts[$index])
    }
    Write-Registry $registry
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $true; attempt = $attempt; registryHash = $registry.registryHash }
} else {
    $registryExists = Test-Path -LiteralPath $registryPath -PathType Leaf
    $registry = Read-Registry
    if (-not $registryExists) { Set-RegistryHash $registry }
    $validation = Test-Registry $registry
    if ($Action -eq 'verify' -and -not $registryExists) {
        $validation.valid = $false
        $validation.issues = @($validation.issues) + 'repair-loop.json is absent.'
    }
    $result = [pscustomobject][ordered]@{
        action = $Action
        valid = $validation.valid
        issues = @($validation.issues)
        registry = $registry
        activeAttempts = @($validation.activeAttempts)
        unresolvedAttempts = @($validation.unresolvedAttempts)
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 40 } else {
    if ($Action -eq 'suggest') {
        Write-Host "Repair suggestion: check=$($result.suggestion.checkId), category=$($result.suggestion.category), paths=$(@($result.suggestion.recommendedPaths).Count)"
    } elseif ($Action -in @('start', 'complete', 'fail')) {
        Write-Host "Repair attempt: action=$Action, id=$($result.attempt.id), state=$($result.attempt.state), check=$($result.attempt.checkId)"
    } else {
        Write-Host "Repair loop: valid=$($result.valid), attempts=$(@($result.registry.attempts).Count), active=$(@($result.activeAttempts).Count), unresolved=$(@($result.unresolvedAttempts).Count)"
        foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
    }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
