[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('create', 'show', 'verify', 'compare')]
    [string]$Action = 'show',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$SourceWorkspacePath,
    [Nullable[int]]$Limit,
    [Nullable[int]]$CharacterBudget,
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
$bundlePolicy = $policy.scheduler.contextBundles
$effectiveLimit = if ($null -ne $Limit) { [int]$Limit } else { [int]$bundlePolicy.defaultItems }
$effectiveCharacterBudget = if ($null -ne $CharacterBudget) { [int]$CharacterBudget } else { [int]$bundlePolicy.defaultTotalCharacters }
if ($effectiveLimit -lt 1 -or $effectiveLimit -gt [int]$bundlePolicy.maximumItems) { throw "Limit must be between 1 and $($bundlePolicy.maximumItems)." }
if ($effectiveCharacterBudget -lt 1 -or $effectiveCharacterBudget -gt [int]$bundlePolicy.maximumTotalCharacters) { throw "CharacterBudget must be between 1 and $($bundlePolicy.maximumTotalCharacters)." }

function Normalize-Workspace([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value) -or [IO.Path]::IsPathRooted($Value)) { throw 'WorkspacePath must be repository-relative.' }
    $normalized = $Value.Replace('\', '/').TrimEnd('/')
    if ($normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') { throw 'WorkspacePath must identify one non-hidden task workspace.' }
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot "$normalized/workspace.json") -PathType Leaf)) { throw "Task workspace does not exist: $normalized" }
    $normalized
}
function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 30 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-FileSha([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-BundlePayload([object]$Bundle) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Bundle.schemaVersion
        workspace = [string]$Bundle.workspace
        createdAtUtc = ([DateTimeOffset]$Bundle.createdAtUtc).ToUniversalTime().ToString('o')
        packetFingerprint = [string]$Bundle.packetFingerprint
        policyFingerprint = [string]$Bundle.policyFingerprint
        generatorFingerprint = [string]$Bundle.generatorFingerprint
        budgets = $Bundle.budgets
        query = $Bundle.query
        learning = $Bundle.learning
        memories = @($Bundle.memories)
        redaction = $Bundle.redaction
        security = $Bundle.security
        items = @($Bundle.items)
        omitted = @($Bundle.omitted)
    }
}
function Get-RedactionPatterns { @($policy.export.redaction.patterns) }
function Protect-Text([string]$Text, [Collections.Generic.HashSet[string]]$Categories, [ref]$Count) {
    $value = $Text
    foreach ($definition in Get-RedactionPatterns) {
        $regex = [regex]::new([string]$definition.pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
        $value = $regex.Replace($value, {
            param($match)
            $Count.Value++
            [void]$Categories.Add([string]$definition.category)
            if ([string]$definition.replacementMode -eq 'preserve-group-1') { return $match.Groups[1].Value + '[REDACTED]@' }
            if ([string]$definition.replacementMode -eq 'preserve-groups-1-2') { return $match.Groups[1].Value + $match.Groups[2].Value + '[REDACTED]' }
            '[REDACTED]'
        })
    }
    $value
}
function Protect-PromptInjection([string]$Text, [bool]$InstructionAuthority, [Collections.Generic.HashSet[string]]$Categories, [ref]$Count) {
    if ($InstructionAuthority) { return $Text }
    $value = $Text
    foreach ($definition in @($bundlePolicy.security.promptInjectionPatterns)) {
        $regex = [regex]::new([string]$definition.pattern)
        $value = $regex.Replace($value, {
            param($match)
            $Count.Value++
            [void]$Categories.Add([string]$definition.id)
            [string]$definition.replacement
        })
    }
    $value
}
function Get-Excerpt([string]$AbsolutePath, [Nullable[int]]$Line, [int]$MaximumCharacters, [Collections.Generic.HashSet[string]]$Categories, [ref]$RedactionCount) {
    $extension = [IO.Path]::GetExtension($AbsolutePath).ToLowerInvariant()
    if ($extension -notin @('.cs', '.ts', '.html', '.scss', '.css', '.json', '.md', '.ps1', '.yml', '.yaml', '.xml', '.csproj', '.slnx')) { return $null }
    $lines = @(Get-Content -LiteralPath $AbsolutePath)
    if ($lines.Count -eq 0) { return [pscustomobject][ordered]@{ startLine = 1; endLine = 0; truncated = $false; text = '' } }
    $radius = [int]$bundlePolicy.symbolContextLines
    $start = if ($null -ne $Line -and [int]$Line -gt 0) { [Math]::Max(1, [int]$Line - $radius) } else { 1 }
    $end = if ($null -ne $Line -and [int]$Line -gt 0) { [Math]::Min($lines.Count, [int]$Line + $radius) } else { $lines.Count }
    $text = (@($lines[($start - 1)..($end - 1)]) -join [Environment]::NewLine)
    $truncated = $start -gt 1 -or $end -lt $lines.Count -or $text.Length -gt $MaximumCharacters
    if ($text.Length -gt $MaximumCharacters) { $text = $text.Substring(0, $MaximumCharacters) }
    $text = Protect-Text $text $Categories $RedactionCount
    [pscustomobject][ordered]@{ startLine = $start; endLine = $end; truncated = $truncated; text = $text }
}
function Add-Candidate([hashtable]$Map, [string]$Path, [string]$Kind, [int]$Score, [string]$Reason, [Nullable[int]]$Line, [bool]$Required) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return }
    $normalized = $Path.Replace('\', '/')
    while ($normalized.StartsWith('./', [StringComparison]::Ordinal)) { $normalized = $normalized.Substring(2) }
    $absolute = Join-Path $repositoryRoot $normalized
    $exists = Test-Path -LiteralPath $absolute -PathType Leaf
    if (-not $exists -and -not $Required) { return }
    if (-not $Map.ContainsKey($normalized)) {
        $Map[$normalized] = [pscustomobject][ordered]@{ path = $normalized; kind = $Kind; score = $Score; required = $Required; exists = $exists; line = $Line; reasons = [Collections.Generic.List[string]]::new() }
    }
    $candidate = $Map[$normalized]
    if ($Score -gt [int]$candidate.score) { $candidate.score = $Score; $candidate.kind = $Kind }
    if ($Required) { $candidate.required = $true }
    if ($null -eq $candidate.line -and $null -ne $Line) { $candidate.line = $Line }
    if ($Reason -notin @($candidate.reasons)) { $candidate.reasons.Add($Reason) }
}
function Read-Bundle([string]$NormalizedWorkspace) {
    $path = Join-Path $repositoryRoot "$NormalizedWorkspace/context-bundle.json"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Context bundle is absent: $NormalizedWorkspace/context-bundle.json" }
    Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}
function Test-Bundle([object]$Bundle, [string]$NormalizedWorkspace) {
    $issues = [Collections.Generic.List[string]]::new()
    $descriptor = Get-Content -LiteralPath (Join-Path $repositoryRoot "$NormalizedWorkspace/workspace.json") -Raw | ConvertFrom-Json
    if ([string]$Bundle.bundleHash -cne (Get-Hash (Get-BundlePayload $Bundle))) { $issues.Add('Bundle hash is invalid.') }
    if ([string]$Bundle.workspace -cne $NormalizedWorkspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Bundle.packetFingerprint -cne [string]$descriptor.currentPacketFingerprint) { $issues.Add('Task packet drifted.') }
    if ([string]$Bundle.policyFingerprint -cne (Get-FileSha $policyPath)) { $issues.Add('Workspace policy drifted.') }
    if ([string]$Bundle.generatorFingerprint -cne (Get-FileSha $PSCommandPath)) { $issues.Add('Context bundle generator changed.') }
    $memoryValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiMemory.ps1') verify -AsOfUtc ([DateTime]$Bundle.createdAtUtc) -Format Json | ConvertFrom-Json
    if (-not $memoryValidation.valid -or [string]$Bundle.learning.memoryRegistryFingerprint -cne [string]$memoryValidation.registryFingerprint) {
        $issues.Add('Durable memory registry drifted.')
    }
    $learningValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') verify -AsOfUtc ([DateTime]$Bundle.createdAtUtc) -Format Json | ConvertFrom-Json
    if (-not $learningValidation.valid -or [string]$Bundle.learning.promotionRegistryFingerprint -cne [string]$learningValidation.registryFingerprint) {
        $issues.Add('Applied learning registry drifted.')
    }
    $experimentValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningExperiment.ps1') verify -AsOfUtc ([DateTime]$Bundle.createdAtUtc) -Format Json | ConvertFrom-Json
    if (-not $experimentValidation.valid -or [string]$Bundle.learning.experimentRegistryFingerprint -cne [string]$experimentValidation.registryFingerprint) {
        $issues.Add('Learning experiment registry drifted.')
    }
    $securityValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextSecurity.ps1') verify -WorkspacePath $NormalizedWorkspace -Format Json | ConvertFrom-Json
    if (-not $securityValidation.valid) { $issues.Add("Context security assessment is invalid: $(@($securityValidation.issues) -join ' ')") }
    if ([string]$Bundle.security.assessmentHash -cne [string]$securityValidation.assessment.assessmentHash) { $issues.Add('Context security assessment drifted.') }
    if (@($Bundle.items).Count -gt [int]$Bundle.budgets.itemLimit) { $issues.Add('Item budget was exceeded.') }
    if ([int]$Bundle.budgets.usedCharacters -gt [int]$Bundle.budgets.characterLimit) { $issues.Add('Character budget was exceeded.') }
    foreach ($item in @($Bundle.items)) {
        $absolute = Join-Path $repositoryRoot ([string]$item.path)
        $exists = Test-Path -LiteralPath $absolute -PathType Leaf
        if ([bool]$item.exists -ne $exists) { $issues.Add("Context source existence changed: $($item.path)."); continue }
        if ($exists -and [string]$item.sha256 -cne (Get-FileSha $absolute)) { $issues.Add("Context source changed: $($item.path).") }
    }
    [pscustomobject][ordered]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}

$normalizedWorkspace = Normalize-Workspace $WorkspacePath
if ($Action -eq 'create') {
    $workspaceAbsolute = Join-Path $repositoryRoot $normalizedWorkspace
    $descriptor = Get-Content -LiteralPath (Join-Path $workspaceAbsolute 'workspace.json') -Raw | ConvertFrom-Json
    $packet = Get-Content -LiteralPath (Join-Path $workspaceAbsolute 'change-packet.json') -Raw | ConvertFrom-Json
    $moduleName = [string](@($packet.diff.modules | Select-Object -First 1).name)
    $query = [string]$descriptor.objective
    $stopWords = @('safely', 'safe', 'change', 'changes', 'changing', 'modify', 'update', 'create', 'implement', 'feature', 'task', 'preserve', 'support', 'add', 'the', 'and', 'for', 'with')
    $semanticQuery = @([regex]::Matches($query.ToLowerInvariant(), '[\p{L}\p{N}]+') | ForEach-Object Value | Where-Object { $_.Length -ge 3 -and $_ -notin $stopWords } | Sort-Object -Unique) -join ' '
    if ([string]::IsNullOrWhiteSpace($semanticQuery)) { $semanticQuery = $query }
    $changeType = if (@($packet.diff.scopes) -contains 'Frontend') { 'Frontend' } elseif (@($packet.diff.scopes) -contains 'Api') { 'Api' } elseif (@($packet.diff.scopes) -contains 'Database') { 'Database' } else { 'Backend' }
    $discovered = & (Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1') -Module $moduleName -Query $semanticQuery -ChangeType $changeType -Limit 20 -Format Json | ConvertFrom-Json
    $feedbackResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextFeedback.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $feedbackResult.valid) { throw 'Context feedback registry contains invalid receipts.' }
    $feedbackMetrics = $feedbackResult.metrics
    $memoryResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiMemory.ps1') relevant -WorkspacePath $normalizedWorkspace -AsOfUtc $AsOfUtc -Format Json | ConvertFrom-Json
    if (-not $memoryResult.valid) { throw 'Durable memory registry is invalid.' }
    $learningResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') list -AsOfUtc $AsOfUtc -Format Json | ConvertFrom-Json
    if (-not $learningResult.valid) { throw 'Learning-promotion registry is invalid.' }
    $experimentResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningExperiment.ps1') active -WorkspacePath $normalizedWorkspace -AsOfUtc $AsOfUtc -Format Json | ConvertFrom-Json
    if (-not $experimentResult.valid) { throw 'Learning-experiment registry is invalid.' }
    $candidates = @{}
    Add-Candidate $candidates 'AGENTS.md' 'guide' 120 'Repository-wide agent instructions.' $null $true
    foreach ($path in @($packet.brief.instructions)) { Add-Candidate $candidates $path 'guide' 115 'Ownership guide required by the task packet.' $null $true }
    foreach ($path in @($packet.diff.changedPaths)) { Add-Candidate $candidates $path 'changed-source' 110 'Directly changed path in the task scope.' $null $true }
    foreach ($path in @($packet.brief.contextPages)) { Add-Candidate $candidates $path 'wiki' 100 'Wiki page selected by diff impact analysis.' $null $true }
    foreach ($path in @($packet.brief.focusedTests)) { Add-Candidate $candidates $path 'test' 80 'Focused test selected by the task packet.' $null $false }
    foreach ($item in @($discovered.agentGuides)) { Add-Candidate $candidates $item.path 'guide' (70 + [Math]::Min(20, [int]$item.score)) 'Semantic context search matched an agent guide.' $null $false }
    foreach ($item in @($discovered.wikiPages)) { Add-Candidate $candidates $item.path 'wiki' (65 + [Math]::Min(20, [int]$item.score)) 'Semantic context search matched a Wiki page.' $null $false }
    foreach ($item in @($discovered.symbols)) { Add-Candidate $candidates $item.path 'symbol' (55 + [Math]::Min(20, [int]$item.score)) "Matched C# symbol '$($item.name)'." ([int]$item.line) $false }
    foreach ($item in @($discovered.frontendSymbols)) { Add-Candidate $candidates $item.path 'symbol' (55 + [Math]::Min(20, [int]$item.score)) "Matched frontend symbol '$($item.name)'." ([int]$item.line) $false }
    foreach ($item in @($discovered.tests)) { Add-Candidate $candidates $item.path 'test' (50 + [Math]::Min(20, [int]$item.score)) 'Semantic context search matched a test.' $null $false }
    foreach ($profile in @($feedbackMetrics.profiles | Where-Object { $_.eligible -and $_.missingCount -gt 0 })) {
        Add-Candidate $candidates ([string]$profile.path) 'feedback-recovery' 68 'Prior terminal tasks reported this context as missing.' $null $false
    }
    foreach ($candidate in @($candidates.Values)) {
        $candidate | Add-Member -NotePropertyName baseScore -NotePropertyValue ([int]$candidate.score)
        $profile = $feedbackMetrics.profiles | Where-Object path -eq $candidate.path | Select-Object -First 1
        $adjustment = if ($null -ne $profile -and $profile.eligible) { [int]$profile.adjustment } else { 0 }
        $candidate | Add-Member -NotePropertyName learningAdjustment -NotePropertyValue $adjustment
        $candidate.score = [Math]::Max(0, [int]$candidate.score + $adjustment)
        if ($adjustment -ne 0) { $candidate.reasons.Add("Validated context feedback adjusted relevance by $adjustment.") }
    }
    $requiredCandidates = @($candidates.Values | Where-Object required)
    if ($requiredCandidates.Count -gt $effectiveLimit) { throw "Required context has $($requiredCandidates.Count) items but the limit is $effectiveLimit. Decompose the task or increase -Limit." }
    $selectedCandidates = @($candidates.Values | Sort-Object @{Expression='required';Descending=$true}, @{Expression='score';Descending=$true}, path | Select-Object -First $effectiveLimit)
    $contextSecurity = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextSecurity.ps1') create `
        -WorkspacePath $normalizedWorkspace `
        -Path @($selectedCandidates.path) `
        -AsOfUtc $AsOfUtc `
        -Format Json | ConvertFrom-Json
    if (-not $contextSecurity.valid) { throw 'Unable to create a valid context security assessment.' }
    $items = [Collections.Generic.List[object]]::new()
    $omitted = [Collections.Generic.List[object]]::new()
    $usedCharacters = 0
    $redactionCount = 0
    $redactionCategories = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $quarantineCount = 0
    $quarantineCategories = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $memories = [Collections.Generic.List[object]]::new()
    foreach ($memory in @($memoryResult.memories)) {
        $statement = Protect-Text ([string]$memory.statement) $redactionCategories ([ref]$redactionCount)
        $rationale = Protect-Text ([string]$memory.rationale) $redactionCategories ([ref]$redactionCount)
        $memoryCharacters = $statement.Length + $rationale.Length
        if (($usedCharacters + $memoryCharacters) -gt $effectiveCharacterBudget) { continue }
        $usedCharacters += $memoryCharacters
        $memories.Add([pscustomobject][ordered]@{
            id = [string]$memory.id
            statement = $statement
            rationale = $rationale
            scopePaths = @($memory.scopePaths)
            tags = @($memory.tags)
            evidence = @($memory.evidence)
            source = $memory.source
            eventHash = [string]$memory.eventHash
        })
    }
    foreach ($learning in @($learningResult.candidates | Where-Object {
        $_.decision -eq 'approved' -and
        $_.materialization -eq 'applied' -and
        $_.application.target -eq 'durable-memory'
    })) {
        $relevant = @($packet.diff.changedPaths | Where-Object {
            $changedPath = [string]$_
            @($learning.application.scopePaths | Where-Object { $changedPath -match [string]$_ }).Count -gt 0
        }).Count -gt 0
        if (-not $relevant) { continue }
        $statement = Protect-Text ([string]$learning.application.statement) $redactionCategories ([ref]$redactionCount)
        $rationale = Protect-Text ([string]$learning.application.rationale) $redactionCategories ([ref]$redactionCount)
        $memoryCharacters = $statement.Length + $rationale.Length
        if (($usedCharacters + $memoryCharacters) -gt $effectiveCharacterBudget) { continue }
        $usedCharacters += $memoryCharacters
        $memories.Add([pscustomobject][ordered]@{
            id = [string]$learning.id
            statement = $statement
            rationale = $rationale
            scopePaths = @($learning.application.scopePaths)
            tags = @($learning.tags)
            evidence = @($learning.application.evidence)
            source = [pscustomobject][ordered]@{
                kind = 'approved-learning'
                candidateId = [string]$learning.id
                decidedAtUtc = [string]$learning.decidedAtUtc
            }
            eventHash = [string]$learning.application.evidenceHash
        })
    }
    foreach ($experiment in @($experimentResult.experiments | Where-Object { $_.canary.application.target -eq 'durable-memory' })) {
        $application = $experiment.canary.application
        $relevant = @($packet.diff.changedPaths | Where-Object {
            $changedPath = [string]$_
            @($application.scopePaths | Where-Object { $changedPath -match [string]$_ }).Count -gt 0
        }).Count -gt 0
        if (-not $relevant -or @($memories.id) -contains [string]$experiment.candidateId) { continue }
        $statement = Protect-Text ([string]$application.statement) $redactionCategories ([ref]$redactionCount)
        $rationale = Protect-Text ([string]$application.rationale) $redactionCategories ([ref]$redactionCount)
        $memoryCharacters = $statement.Length + $rationale.Length
        if (($usedCharacters + $memoryCharacters) -gt $effectiveCharacterBudget) { continue }
        $usedCharacters += $memoryCharacters
        $memories.Add([pscustomobject][ordered]@{
            id = [string]$experiment.candidateId
            statement = $statement
            rationale = $rationale
            scopePaths = @($application.scopePaths)
            tags = @('canary', 'learning-experiment')
            evidence = @($application.evidence)
            source = [pscustomobject][ordered]@{
                kind = 'learning-canary'
                candidateId = [string]$experiment.candidateId
                canaryEventHash = [string]$experiment.canaryEventHash
                percentage = [int]$experiment.canary.percentage
            }
            eventHash = [string]$experiment.canaryEventHash
        })
    }
    $perItemCharacterBudget = [Math]::Max(1, [Math]::Min([int]$bundlePolicy.maximumItemCharacters, [Math]::Floor($effectiveCharacterBudget / [Math]::Max(1, $selectedCandidates.Count))))
    foreach ($candidate in $selectedCandidates) {
        $remaining = $effectiveCharacterBudget - $usedCharacters
        $maximum = [Math]::Min($perItemCharacterBudget, [Math]::Max(0, $remaining))
        $absolute = Join-Path $repositoryRoot ([string]$candidate.path)
        $excerpt = if ($candidate.exists -and $maximum -gt 0) { Get-Excerpt $absolute $candidate.line $maximum $redactionCategories ([ref]$redactionCount) } else { $null }
        $securitySource = $contextSecurity.assessment.sources | Where-Object path -eq $candidate.path | Select-Object -First 1
        if ($null -ne $excerpt -and $null -ne $securitySource) {
            $excerpt.text = Protect-PromptInjection ([string]$excerpt.text) ([bool]$securitySource.instructionAuthority) $quarantineCategories ([ref]$quarantineCount)
        }
        $characters = if ($null -ne $excerpt) { ([string]$excerpt.text).Length } else { 0 }
        $usedCharacters += [int]$characters
        $items.Add([pscustomobject][ordered]@{
            path = [string]$candidate.path
            kind = [string]$candidate.kind
            score = [int]$candidate.score
            baseScore = [int]$candidate.baseScore
            learningAdjustment = [int]$candidate.learningAdjustment
            required = [bool]$candidate.required
            exists = [bool]$candidate.exists
            reasons = @($candidate.reasons)
            sha256 = $(if ($candidate.exists) { Get-FileSha $absolute } else { '' })
            trustZone = $(if ($null -eq $securitySource) { 'default' } else { [string]$securitySource.trustZone })
            trust = $(if ($null -eq $securitySource) { [string]$bundlePolicy.security.defaultTrust } else { [string]$securitySource.trust })
            instructionAuthority = $(if ($null -eq $securitySource) { $false } else { [bool]$securitySource.instructionAuthority })
            securityFindingCount = $(if ($null -eq $securitySource) { 0 } else { [int]$securitySource.findingCount })
            excerpt = $excerpt
        })
    }
    foreach ($candidate in @($candidates.Values | Where-Object path -notin @($selectedCandidates.path) | Sort-Object @{Expression='score';Descending=$true}, path)) {
        $omitted.Add([pscustomobject][ordered]@{ path = [string]$candidate.path; score = [int]$candidate.score; reason = 'item-budget' })
    }
    $bundle = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $normalizedWorkspace
        createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        packetFingerprint = [string]$descriptor.currentPacketFingerprint
        policyFingerprint = Get-FileSha $policyPath
        generatorFingerprint = Get-FileSha $PSCommandPath
        budgets = [pscustomobject][ordered]@{ itemLimit = $effectiveLimit; characterLimit = $effectiveCharacterBudget; usedItems = $items.Count; usedCharacters = $usedCharacters }
        query = [pscustomobject][ordered]@{ module = $moduleName; objective = $query; semanticQuery = $semanticQuery; changeType = $changeType }
        learning = [pscustomobject][ordered]@{
            feedbackFingerprint = [string]$feedbackMetrics.feedbackFingerprint
            validReceiptCount = [int]$feedbackMetrics.validReceiptCount
            minimumPathSamples = [int]$policy.scheduler.contextBundles.feedback.minimumPathSamples
            memoryRegistryFingerprint = [string]$memoryResult.registryFingerprint
            promotionRegistryFingerprint = [string]$learningResult.registryFingerprint
            experimentRegistryFingerprint = [string]$experimentResult.registryFingerprint
            relevantMemoryCount = $memories.Count
        }
        memories = @($memories)
        redaction = [pscustomobject][ordered]@{ policyId = [string]$policy.export.redaction.policyId; count = $redactionCount; categories = @($redactionCategories | Sort-Object) }
        security = [pscustomobject][ordered]@{
            assessmentHash = [string]$contextSecurity.assessment.assessmentHash
            scannerFingerprint = [string]$contextSecurity.assessment.scannerFingerprint
            findingCount = [int]$contextSecurity.assessment.summary.findingCount
            quarantineMatchCount = $quarantineCount
            quarantineCategories = @($quarantineCategories | Sort-Object)
            trustedInstructionCount = [int]$contextSecurity.assessment.summary.trustedInstructionCount
            untrustedSourceCount = [int]$contextSecurity.assessment.summary.untrustedSourceCount
        }
        items = @($items)
        omitted = @($omitted)
        bundleHash = ''
    }
    $bundle.bundleHash = Get-Hash (Get-BundlePayload $bundle)
    [IO.File]::WriteAllText((Join-Path $workspaceAbsolute 'context-bundle.json'), (($bundle | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $result = [pscustomobject][ordered]@{ action = 'create'; valid = $true; bundle = $bundle; path = "$normalizedWorkspace/context-bundle.json" }
} elseif ($Action -eq 'compare') {
    if ([string]::IsNullOrWhiteSpace($SourceWorkspacePath)) { throw 'compare requires SourceWorkspacePath.' }
    $sourceWorkspace = Normalize-Workspace $SourceWorkspacePath
    $left = Read-Bundle $sourceWorkspace
    $right = Read-Bundle $normalizedWorkspace
    $leftPaths = @($left.items.path | Sort-Object -Unique)
    $rightPaths = @($right.items.path | Sort-Object -Unique)
    $common = @($leftPaths | Where-Object { $_ -in $rightPaths })
    $union = @($leftPaths + $rightPaths | Sort-Object -Unique)
    $result = [pscustomobject][ordered]@{
        action = 'compare'
        valid = $true
        sourceWorkspace = $sourceWorkspace
        workspace = $normalizedWorkspace
        overlapPercent = $(if ($union.Count -eq 0) { 100 } else { [Math]::Round(100 * $common.Count / $union.Count, 2) })
        commonPaths = $common
        addedPaths = @($rightPaths | Where-Object { $_ -notin $leftPaths })
        removedPaths = @($leftPaths | Where-Object { $_ -notin $rightPaths })
        sourceHash = $left.bundleHash
        bundleHash = $right.bundleHash
    }
} else {
    $bundle = Read-Bundle $normalizedWorkspace
    $validation = Test-Bundle $bundle $normalizedWorkspace
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $validation.valid; issues = @($validation.issues); bundle = $bundle }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    Write-Host "Context bundle: action=$($result.action), valid=$($result.valid)"
    if ($null -ne $result.bundle) { Write-Host "Items=$(@($result.bundle.items).Count), characters=$($result.bundle.budgets.usedCharacters), redactions=$($result.bundle.redaction.count), hash=$($result.bundle.bundleHash)" }
    if ($null -ne $result.overlapPercent) { Write-Host "Overlap=$($result.overlapPercent)%" }
    foreach ($issue in @($result.issues | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
