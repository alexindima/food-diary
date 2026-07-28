[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('profile', 'find', 'clusters', 'reuse', 'show', 'verify')]
    [string]$Action = 'find',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$SourceWorkspacePath,
    [switch]$DryRun,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$similarityPolicy = $policy.scheduler.taskSimilarity

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 50 -Compress
    if ($null -eq $json) { $json = 'null' }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Normalize-Workspace([string]$Value, [string]$Label) {
    if ([IO.Path]::IsPathRooted($Value)) { throw "$Label must be repository-relative." }
    $normalized = $Value.Replace('\', '/').TrimEnd('/')
    if ($normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') { throw "$Label must identify one non-hidden task workspace." }
    $normalized
}
function Get-WorkspaceAbsolute([string]$Workspace) {
    $absolute = Join-Path $repositoryRoot $Workspace
    if (-not (Test-Path -LiteralPath $absolute -PathType Container)) { throw "Workspace does not exist: $Workspace" }
    $absolute
}
function Get-PathArea([string]$Path) {
    $parts = $Path.Replace('\', '/').Split('/')
    if ($parts.Count -le 1) { return $parts[0] }
    if ($parts[0] -in @('FoodDiary.Application', 'FoodDiary.Domain', 'FoodDiary.Infrastructure', 'FoodDiary.Integrations', 'FoodDiary.Presentation.Api')) {
        return "$($parts[0])/$($parts[1])"
    }
    $parts[0]
}
function New-Profile([string]$Workspace) {
    $absolute = Get-WorkspaceAbsolute $Workspace
    $packet = Get-Content -LiteralPath (Join-Path $absolute 'change-packet.json') -Raw | ConvertFrom-Json
    $descriptor = Get-Content -LiteralPath (Join-Path $absolute 'workspace.json') -Raw | ConvertFrom-Json
    $features = [pscustomobject][ordered]@{
        modules = @(@($packet.diff.modules.name) + @($packet.brief.change.directModules) + @($packet.brief.change.downstreamModules) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        scopes = @($packet.diff.scopes | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        rules = @($packet.policy.matchedRules.id | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        checks = @($packet.policy.requiredChecks.id | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        pathAreas = @($packet.diff.changedPaths | ForEach-Object { Get-PathArea ([string]$_) } | Sort-Object -Unique)
    }
    $clusterInput = [pscustomobject][ordered]@{ modules = $features.modules; scopes = $features.scopes; rules = $features.rules }
    $payload = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $Workspace
        packetFingerprint = [string]$packet.fingerprint
        policyFingerprint = [string]$descriptor.policyFingerprint
        riskLevel = [string]$packet.brief.risk.level
        riskScore = [int]$packet.brief.risk.score
        features = $features
        clusterKey = "cluster-$((Get-Hash $clusterInput).Substring(0, 16))"
    }
    $payload | Add-Member -NotePropertyName profileHash -NotePropertyValue (Get-Hash $payload)
    $payload
}
function Get-Jaccard([object[]]$Left, [object[]]$Right) {
    $leftSet = @($Left | Sort-Object -Unique)
    $rightSet = @($Right | Sort-Object -Unique)
    $union = @($leftSet + $rightSet | Sort-Object -Unique)
    if ($union.Count -eq 0) { return 100.0 }
    $intersection = @($leftSet | Where-Object { $rightSet -contains $_ })
    [Math]::Round(100 * $intersection.Count / $union.Count, 2)
}
function Compare-Profiles([object]$Target, [object]$Source) {
    $components = [ordered]@{}
    $score = 0.0
    foreach ($name in @('modules', 'scopes', 'rules', 'checks', 'pathAreas')) {
        $componentScore = Get-Jaccard @($Target.features.$name) @($Source.features.$name)
        $weight = [int]$similarityPolicy.weights.$name
        $components[$name] = [pscustomobject][ordered]@{ score = $componentScore; weight = $weight; contribution = [Math]::Round($componentScore * $weight / 100, 2) }
        $score += [double]$components[$name].contribution
    }
    [pscustomobject][ordered]@{
        score = [Math]::Round($score, 2)
        components = [pscustomobject]$components
        riskScoreDelta = [Math]::Abs([int]$Target.riskScore - [int]$Source.riskScore)
        sameCluster = [string]$Target.clusterKey -ceq [string]$Source.clusterKey
    }
}
function Get-SealedWorkspaces {
    $tasksRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/tasks'
    @(
        foreach ($directory in @(Get-ChildItem -LiteralPath $tasksRoot -Directory -Force -ErrorAction SilentlyContinue | Sort-Object Name)) {
            if ($directory.Name.StartsWith('.', [StringComparison]::Ordinal)) { continue }
            $workspace = ".artifacts/llm-wiki/tasks/$($directory.Name)"
            if (-not (Test-Path -LiteralPath (Join-Path $directory.FullName 'completion.json') -PathType Leaf)) { continue }
            try {
                $completion = & (Join-Path $PSScriptRoot 'Complete-LlmWikiTaskWorkspace.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
                $doctor = & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
                if ($completion.valid -and $doctor.valid) {
                    $completionReceipt = Get-Content -LiteralPath (Join-Path $directory.FullName 'completion.json') -Raw | ConvertFrom-Json
                    [pscustomobject][ordered]@{ workspace = $workspace; completion = $completionReceipt; profile = New-Profile $workspace }
                }
            } catch { continue }
        }
    )
}
function Find-Candidates([string]$TargetWorkspace, [string]$SourceFilter) {
    $targetProfile = New-Profile $TargetWorkspace
    $items = [Collections.Generic.List[object]]::new()
    foreach ($source in @(Get-SealedWorkspaces)) {
        if ($source.workspace -ceq $TargetWorkspace) { continue }
        if (-not [string]::IsNullOrWhiteSpace($SourceFilter) -and $source.workspace -cne $SourceFilter) { continue }
        $comparison = Compare-Profiles $targetProfile $source.profile
        if ([double]$comparison.score -lt [double]$similarityPolicy.minimumCandidateScore) { continue }
        $items.Add([pscustomobject][ordered]@{
            sourceWorkspace = $source.workspace
            completionFingerprint = [string]$source.completion.completionFingerprint
            finishedAtUtc = [string]$source.completion.finishedAtUtc
            profile = $source.profile
            similarity = $comparison
            reusable = [double]$comparison.score -ge [double]$similarityPolicy.minimumPlanReuseScore -and
                [int]$comparison.riskScoreDelta -le [int]$similarityPolicy.maximumRiskScoreDelta -and
                [string]$targetProfile.policyFingerprint -ceq [string]$source.profile.policyFingerprint
        })
    }
    [pscustomobject][ordered]@{
        targetProfile = $targetProfile
        candidates = @($items | Sort-Object @{ Expression = { $_.similarity.score }; Descending = $true }, @{ Expression = 'finishedAtUtc'; Descending = $true }, sourceWorkspace | Select-Object -First ([int]$similarityPolicy.maximumCandidates))
    }
}
function Get-ReusePayload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = $Receipt.schemaVersion
        createdAtUtc = $Receipt.createdAtUtc
        workspace = $Receipt.workspace
        sourceWorkspace = $Receipt.sourceWorkspace
        sourceCompletionFingerprint = $Receipt.sourceCompletionFingerprint
        policyFingerprint = $Receipt.policyFingerprint
        targetProfile = $Receipt.targetProfile
        sourceProfile = $Receipt.sourceProfile
        similarity = $Receipt.similarity
        drift = $Receipt.drift
        implementation = $Receipt.implementation
        verification = $Receipt.verification
    }
}
function Test-Reuse([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    if ([string]$Receipt.receiptHash -cne (Get-Hash (Get-ReusePayload $Receipt))) { $issues.Add('Plan-reuse receipt hash is invalid.') }
    if ([string]$Receipt.workspace -cne $normalizedWorkspace) { $issues.Add('Plan-reuse target workspace drifted.') }
    try {
        $target = New-Profile $normalizedWorkspace
        $source = New-Profile ([string]$Receipt.sourceWorkspace)
        if ([string]$Receipt.targetProfile.profileHash -cne [string]$target.profileHash) { $issues.Add('Target similarity profile drifted.') }
        if ([string]$Receipt.sourceProfile.profileHash -cne [string]$source.profileHash) { $issues.Add('Source similarity profile drifted.') }
        $comparison = Compare-Profiles $target $source
        if ((Get-Hash $Receipt.similarity) -cne (Get-Hash $comparison)) { $issues.Add('Similarity calculation drifted.') }
        $sourceSeal = & (Join-Path $PSScriptRoot 'Complete-LlmWikiTaskWorkspace.ps1') verify -WorkspacePath $Receipt.sourceWorkspace -Format Json | ConvertFrom-Json
        $sourceCompletion = Get-Content -LiteralPath (Join-Path (Get-WorkspaceAbsolute $Receipt.sourceWorkspace) 'completion.json') -Raw | ConvertFrom-Json
        if (-not $sourceSeal.valid -or
            [string]::IsNullOrWhiteSpace([string]$Receipt.sourceCompletionFingerprint) -or
            [string]$Receipt.sourceCompletionFingerprint -cne [string]$sourceCompletion.completionFingerprint) {
            $issues.Add('Source completion lineage drifted.')
        }
        $verification = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationPlan.ps1') verify -WorkspacePath $normalizedWorkspace -Format Json | ConvertFrom-Json
        if (-not $verification.valid -or [string]$Receipt.verification.targetPlanHash -cne [string]$verification.plan.planHash) { $issues.Add('Target verification plan drifted.') }
        $targetPacket = Get-Content -LiteralPath (Join-Path (Get-WorkspaceAbsolute $normalizedWorkspace) 'change-packet.json') -Raw | ConvertFrom-Json
        $sourcePacket = Get-Content -LiteralPath (Join-Path (Get-WorkspaceAbsolute $Receipt.sourceWorkspace) 'change-packet.json') -Raw | ConvertFrom-Json
        if ([string]$Receipt.implementation.targetPlanHash -cne (Get-Hash $targetPacket.implementationPlan)) { $issues.Add('Target implementation plan drifted.') }
        if ([string]$Receipt.implementation.sourcePlanHash -cne (Get-Hash $sourcePacket.implementationPlan)) { $issues.Add('Source implementation plan drifted.') }
    } catch { $issues.Add($_.Exception.Message) }
    [pscustomobject][ordered]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}

$normalizedWorkspace = Normalize-Workspace $WorkspacePath 'WorkspacePath'
$absoluteWorkspace = if ($Action -eq 'clusters') { $null } else { Get-WorkspaceAbsolute $normalizedWorkspace }
$receiptPath = if ($null -eq $absoluteWorkspace) { $null } else { Join-Path $absoluteWorkspace 'plan-reuse.json' }
$result = $null
if ($Action -eq 'profile') {
    $result = [pscustomobject][ordered]@{ action = 'profile'; valid = $true; profile = New-Profile $normalizedWorkspace }
} elseif ($Action -eq 'clusters') {
    $sealed = @(Get-SealedWorkspaces)
    $clusters = @($sealed | Group-Object { $_.profile.clusterKey } | ForEach-Object {
        [pscustomobject][ordered]@{
            clusterKey = [string]$_.Name
            taskCount = $_.Count
            workspaces = @($_.Group.workspace | Sort-Object)
            modules = @($_.Group.profile.features.modules | Sort-Object -Unique)
            scopes = @($_.Group.profile.features.scopes | Sort-Object -Unique)
            rules = @($_.Group.profile.features.rules | Sort-Object -Unique)
        }
    } | Sort-Object @{ Expression = 'taskCount'; Descending = $true }, clusterKey)
    $result = [pscustomobject][ordered]@{ action = 'clusters'; valid = $true; taskCount = $sealed.Count; clusterCount = $clusters.Count; clusters = $clusters; policyFingerprint = (Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant() }
} elseif ($Action -eq 'find') {
    $sourceFilter = if ([string]::IsNullOrWhiteSpace($SourceWorkspacePath)) { '' } else { Normalize-Workspace $SourceWorkspacePath 'SourceWorkspacePath' }
    $found = Find-Candidates $normalizedWorkspace $sourceFilter
    $result = [pscustomobject][ordered]@{ action = 'find'; valid = $true; workspace = $normalizedWorkspace; candidateCount = @($found.candidates).Count; targetProfile = $found.targetProfile; candidates = @($found.candidates) }
} elseif ($Action -eq 'reuse') {
    if (Test-Path -LiteralPath (Join-Path $absoluteWorkspace 'completion.json') -PathType Leaf) { throw 'A sealed target workspace cannot accept plan reuse.' }
    $doctor = & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') -WorkspacePath $normalizedWorkspace -Format Json | ConvertFrom-Json
    if (-not $doctor.valid) { throw "Target workspace is invalid: $(@($doctor.errors) -join ' ')" }
    $sourceFilter = if ([string]::IsNullOrWhiteSpace($SourceWorkspacePath)) { '' } else { Normalize-Workspace $SourceWorkspacePath 'SourceWorkspacePath' }
    $found = Find-Candidates $normalizedWorkspace $sourceFilter
    $candidate = @($found.candidates | Where-Object reusable | Select-Object -First 1)[0]
    if ($null -eq $candidate) { throw 'No similar sealed source satisfies plan-reuse gates.' }
    $sourceAbsolute = Get-WorkspaceAbsolute $candidate.sourceWorkspace
    $targetPacket = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-packet.json') -Raw | ConvertFrom-Json
    $sourcePacket = Get-Content -LiteralPath (Join-Path $sourceAbsolute 'change-packet.json') -Raw | ConvertFrom-Json
    $sourceEvidence = Get-Content -LiteralPath (Join-Path $sourceAbsolute 'evidence.json') -Raw | ConvertFrom-Json
    $targetChecks = @($targetPacket.policy.requiredChecks.id | Sort-Object -Unique)
    $sourceChecks = @($sourcePacket.policy.requiredChecks.id | Sort-Object -Unique)
    $commonChecks = @($targetChecks | Where-Object { $sourceChecks -contains $_ })
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        workspace = $normalizedWorkspace
        sourceWorkspace = [string]$candidate.sourceWorkspace
        sourceCompletionFingerprint = [string]$candidate.completionFingerprint
        policyFingerprint = [string]$found.targetProfile.policyFingerprint
        targetProfile = $found.targetProfile
        sourceProfile = $candidate.profile
        similarity = $candidate.similarity
        drift = [pscustomobject][ordered]@{
            riskScoreDelta = [int]$candidate.similarity.riskScoreDelta
            addedChecks = @($targetChecks | Where-Object { $sourceChecks -notcontains $_ })
            removedChecks = @($sourceChecks | Where-Object { $targetChecks -notcontains $_ })
            commonChecks = $commonChecks
        }
        implementation = [pscustomobject][ordered]@{
            targetPlanHash = Get-Hash $targetPacket.implementationPlan
            sourcePlanHash = Get-Hash $sourcePacket.implementationPlan
            targetPlan = $targetPacket.implementationPlan
            sourceExperience = $sourcePacket.implementationPlan
        }
        verification = [pscustomobject][ordered]@{
            targetPlanHash = ''
            sourceResolvedChecks = @($sourceEvidence.checks | Where-Object status -in @('passed', 'not-applicable') | Select-Object id, status, durationSeconds, reason)
        }
        receiptHash = ''
    }
    if (-not $DryRun) {
        if (Test-Path -LiteralPath $receiptPath -PathType Leaf) { throw 'plan-reuse.json already exists.' }
        $verification = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationPlan.ps1') create -WorkspacePath $normalizedWorkspace -AsOfUtc $AsOfUtc -Format Json | ConvertFrom-Json
        if (-not $verification.valid) { throw 'Canonical target verification plan could not be created.' }
        $receipt.verification.targetPlanHash = [string]$verification.plan.planHash
        $receipt = $receipt | ConvertTo-Json -Depth 50 | ConvertFrom-Json
        $receipt.receiptHash = Get-Hash (Get-ReusePayload $receipt)
        $validation = Test-Reuse $receipt
        if (-not $validation.valid) { throw "Plan reuse receipt is invalid: $(@($validation.issues) -join ' ')" }
        [IO.File]::WriteAllText($receiptPath, (($receipt | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') add -WorkspacePath $normalizedWorkspace -JournalType decision `
            -Text "Reused implementation experience from $($candidate.sourceWorkspace)." `
            -Rationale "Similarity $($candidate.similarity.score)/100; source completion $($candidate.completionFingerprint); target verification remained canonical." | Out-Null
    }
    $result = [pscustomobject][ordered]@{ action = 'reuse'; valid = $true; dryRun = [bool]$DryRun; reused = -not [bool]$DryRun; receipt = $receipt; candidate = $candidate }
} else {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw "Plan reuse receipt is absent: $normalizedWorkspace/plan-reuse.json" }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $validation = Test-Reuse $receipt
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $validation.valid; receipt = $receipt; issues = @($validation.issues) }
}
if ($FailOnInvalid -and -not $result.valid) { throw "Task similarity operation is invalid: $(@($result.issues) -join ' ')" }
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 50 } else {
    Write-Host "Task similarity: action=$Action, valid=$($result.valid)"
    foreach ($candidate in @($result.candidates)) { Write-Host " - $($candidate.sourceWorkspace): score=$($candidate.similarity.score), reusable=$($candidate.reusable)" }
    foreach ($cluster in @($result.clusters)) { Write-Host " - $($cluster.clusterKey): tasks=$($cluster.taskCount)" }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
