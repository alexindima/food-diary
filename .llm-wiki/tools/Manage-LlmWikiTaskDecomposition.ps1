[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('list', 'create', 'verify', 'apply', 'prune')]
    [string]$Action = 'list',
    [string]$WorkspacePath,
    [string]$DecompositionId,
    [Nullable[int]]$MaxShards,
    [Nullable[int]]$SimulateFailureAfter,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$schedulerRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler'
$planRoot = Join-Path $schedulerRoot 'decompositions'
$applicationRoot = Join-Path $schedulerRoot 'decomposition-applications'
$lockPath = Join-Path $schedulerRoot '.decomposition-lock'
$policySnapshot = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -WithFingerprint -Format Json | ConvertFrom-Json
$policy = $policySnapshot.policy
$policyFingerprint = [string]$policySnapshot.fingerprint
$decompositionPolicy = $policy.scheduler.decomposition
$now = $AsOfUtc.ToUniversalTime()
$effectiveMaxShards = if ($null -ne $MaxShards) { [int]$MaxShards } else { [int]$decompositionPolicy.defaultMaximumShards }
if ($effectiveMaxShards -lt 2 -or $effectiveMaxShards -gt [int]$decompositionPolicy.maximumShards) {
    throw "MaxShards must be between 2 and $($decompositionPolicy.maximumShards)."
}

function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 30 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-PlanPayload([object]$Plan) {
    [ordered]@{
        schemaVersion = $Plan.schemaVersion
        decompositionId = $Plan.decompositionId
        createdAtUtc = $Plan.createdAtUtc
        policyFingerprint = $Plan.policyFingerprint
        parentWorkspace = $Plan.parentWorkspace
        parentPacketFingerprint = $Plan.parentPacketFingerprint
        parentObjective = $Plan.parentObjective
        strategy = $Plan.strategy
        maxShards = $Plan.maxShards
        sourcePathCount = $Plan.sourcePathCount
        shards = $Plan.shards
    }
}
function Get-ApplicationPayload([object]$Application) {
    [ordered]@{
        schemaVersion = $Application.schemaVersion
        applicationId = $Application.applicationId
        decompositionId = $Application.decompositionId
        decompositionHash = $Application.decompositionHash
        appliedAtUtc = $Application.appliedAtUtc
        parentWorkspace = $Application.parentWorkspace
        childWorkspaces = $Application.childWorkspaces
    }
}
function Normalize-Workspace([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value) -or [IO.Path]::IsPathRooted($Value)) { throw 'WorkspacePath must be repository-relative.' }
    $normalized = $Value.Replace('\', '/').TrimEnd('/')
    if ($normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') { throw 'WorkspacePath must identify one non-hidden task workspace.' }
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot $normalized) -PathType Container)) { throw "Task workspace does not exist: $normalized" }
    $normalized
}
function Convert-ToSlug([string]$Value) {
    $slug = ($Value.ToLowerInvariant() -replace '[^a-z0-9]+', '-').Trim('-')
    if ([string]::IsNullOrWhiteSpace($slug)) { return 'shard' }
    if ($slug.Length -gt 24) { $slug = $slug.Substring(0, 24).Trim('-') }
    $slug
}
function Get-Capabilities([string[]]$Paths) {
    $values = [Collections.Generic.List[string]]::new()
    foreach ($path in $Paths) {
        if ($path -match '^(FoodDiary\.Web\.Client|.*\.(?:ts|tsx|js|jsx|html|scss|css)$)') { $values.Add('frontend') }
        if ($path -match '(^|/)(tests?|.*Tests?)(/|$)') { $values.Add('tests') }
        if ($path -match '(?i)security|auth|identity|credential|privacy') { $values.Add('security') }
        if ($path -match 'Infrastructure|Initializer|JobManager|\.github/workflows') { $values.Add('infrastructure') }
        if ($path -match '(?i)migration|DbContext|EntityTypeConfiguration|\\.sql$') { $values.Add('database') }
        if ($path -match 'Presentation|Web\.Api|Controller|Endpoint|Contracts?') { $values.Add('api'); $values.Add('backend') }
        if ($path -match '\.cs$' -and $path -notmatch '(^|/)(tests?|.*Tests?)(/|$)') { $values.Add('backend') }
        if ($path -match 'assets/i18n|FoodDiary\.Resources') { $values.Add('localization') }
        if ($path -match '^(docs|\.llm-wiki)/|(?:^|/)AGENTS\.md$') { $values.Add('docs') }
    }
    if ($values.Count -eq 0) { $values.Add('generalist') }
    @($values | Select-Object -Unique | Sort-Object)
}
function Get-BucketKey([string]$Path) {
    $segments = @($Path.Replace('\', '/') -split '/')
    $top = $segments[0]
    if ($top -eq 'FoodDiary.Web.Client') {
        if ($Path -match '^FoodDiary\.Web\.Client/projects/([^/]+)') { return "frontend-$($Matches[1])" }
        return 'frontend-app'
    }
    if ($top -eq 'tests' -and $segments.Count -gt 1) { return "tests-$($segments[1])" }
    if ($top -in @('MailInbox', 'MailRelay', 'Shared') -and $segments.Count -gt 1) { return "$top-$($segments[1])" }
    if ($top -in @('docs', '.llm-wiki', '.github') -or $Path -match '(^|/)AGENTS\.md$') { return 'governance-docs' }
    return $top
}
function Get-PlanFiles {
    if (-not (Test-Path -LiteralPath $planRoot -PathType Container)) { return @() }
    @(Get-ChildItem -LiteralPath $planRoot -File -Filter '*.json' | Sort-Object Name)
}
function Get-ApplicationFiles {
    if (-not (Test-Path -LiteralPath $applicationRoot -PathType Container)) { return @() }
    @(Get-ChildItem -LiteralPath $applicationRoot -File -Filter '*.json' | Sort-Object Name)
}
function Test-Application([object]$Application, [object]$Plan) {
    $issues = [Collections.Generic.List[string]]::new()
    if ($Application.schemaVersion -ne 1) { $issues.Add('Application schemaVersion must be 1.') }
    if ([string]$Application.applicationId -notmatch '^[a-f0-9]{32}$') { $issues.Add('applicationId is invalid.') }
    if ([string]$Application.applicationHash -cne (Get-Hash (Get-ApplicationPayload $Application))) { $issues.Add('applicationHash is invalid.') }
    if ($null -eq $Plan) { $issues.Add("Referenced decomposition plan is missing: $($Application.decompositionId)") }
    elseif ([string]$Application.decompositionHash -cne [string]$Plan.decompositionHash) { $issues.Add('Application references a different decomposition hash.') }
    [pscustomobject][ordered]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}
function Read-Plan([string]$Id) {
    if ($Id -notmatch '^[a-f0-9]{32}$') { throw 'DecompositionId must be a 32-character lowercase hexadecimal identifier.' }
    $matches = @(Get-PlanFiles | Where-Object BaseName -like "*-$Id")
    if ($matches.Count -ne 1) { throw "Decomposition plan does not exist or is ambiguous: $Id" }
    Get-Content -LiteralPath $matches[0].FullName -Raw | ConvertFrom-Json
}
function Write-Atomic([string]$Root, [string]$FileName, [object]$Value, [string]$Prefix) {
    if (-not (Test-Path -LiteralPath $Root)) { New-Item -ItemType Directory -Path $Root | Out-Null }
    $temporary = Join-Path $Root (".$Prefix-" + [guid]::NewGuid().ToString('N') + '.json')
    $target = Join-Path $Root $FileName
    try {
        [IO.File]::WriteAllText($temporary, (($Value | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporary -Destination $target
    } finally {
        if (Test-Path -LiteralPath $temporary) { [IO.File]::Delete($temporary) }
    }
    $target
}
function Test-Plan([object]$Plan, [switch]$Current) {
    $issues = [Collections.Generic.List[string]]::new()
    if ($Plan.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Plan.decompositionId -notmatch '^[a-f0-9]{32}$') { $issues.Add('decompositionId is invalid.') }
    if ([string]$Plan.decompositionHash -cne (Get-Hash (Get-PlanPayload $Plan))) { $issues.Add('decompositionHash is invalid.') }
    if (@($Plan.shards).Count -lt 2) { $issues.Add('A decomposition plan requires at least two shards.') }
    $allPaths = @($Plan.shards.changedPaths | ForEach-Object { @($_) })
    if ($allPaths.Count -ne @($allPaths | Select-Object -Unique).Count) { $issues.Add('A changed path is assigned to more than one shard.') }
    if ($allPaths.Count -ne [int]$Plan.sourcePathCount) { $issues.Add('Shard path coverage does not match sourcePathCount.') }
    if ($Current) {
        try {
            $workspace = Normalize-Workspace ([string]$Plan.parentWorkspace)
            $descriptor = Get-Content -LiteralPath (Join-Path $repositoryRoot "$workspace/workspace.json") -Raw | ConvertFrom-Json
            if ([string]$descriptor.currentPacketFingerprint -cne [string]$Plan.parentPacketFingerprint) { $issues.Add('Parent packet fingerprint changed.') }
            if ($policyFingerprint -cne [string]$Plan.policyFingerprint) { $issues.Add('Workspace policy fingerprint changed.') }
            $descriptorDecompositionProperty = $descriptor.PSObject.Properties['decomposition']
            $descriptorDecomposition = if ($null -ne $descriptorDecompositionProperty) { $descriptorDecompositionProperty.Value } else { $null }
            $applied = $null -ne $descriptorDecomposition -and [string]$descriptorDecomposition.state -eq 'applied' -and [string]$descriptorDecomposition.decompositionId -eq [string]$Plan.decompositionId
            foreach ($shard in @($Plan.shards)) {
                $childPath = Join-Path $repositoryRoot ([string]$shard.workspace)
                if ($applied) {
                    $childDescriptorPath = Join-Path $childPath 'workspace.json'
                    if (-not (Test-Path -LiteralPath $childDescriptorPath -PathType Leaf)) { $issues.Add("Applied child workspace is missing: $($shard.workspace)"); continue }
                    $childDescriptor = Get-Content -LiteralPath $childDescriptorPath -Raw | ConvertFrom-Json
                    if ([string]$childDescriptor.decomposition.decompositionId -cne [string]$Plan.decompositionId -or [string]$childDescriptor.decomposition.shardId -cne [string]$shard.shardId) {
                        $issues.Add("Child workspace decomposition backlink is invalid: $($shard.workspace)")
                    }
                } elseif (Test-Path -LiteralPath $childPath) { $issues.Add("Child workspace already exists: $($shard.workspace)") }
            }
        } catch { $issues.Add($_.Exception.Message) }
    }
    [pscustomobject][ordered]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}

$mutating = $Action -in @('create', 'apply', 'prune')
$lockStream = $null
if ($mutating) {
    if (-not (Test-Path -LiteralPath $schedulerRoot)) { New-Item -ItemType Directory -Path $schedulerRoot | Out-Null }
    if ((Test-Path -LiteralPath $lockPath) -and ($now - [IO.File]::GetLastWriteTimeUtc($lockPath)).TotalMinutes -gt 10) { [IO.File]::Delete($lockPath) }
    try { $lockStream = [IO.File]::Open($lockPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None) }
    catch { throw 'Task decomposition registry is busy; retry after the current mutation completes.' }
}

try {
    if ($Action -eq 'create') {
        $workspace = Normalize-Workspace $WorkspacePath
        $absoluteWorkspace = Join-Path $repositoryRoot $workspace
        $descriptor = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'workspace.json') -Raw | ConvertFrom-Json
        if ($null -ne $descriptor.PSObject.Properties['decomposition'] -and [string]$descriptor.decomposition.state -eq 'applied') { throw 'Workspace is already decomposed.' }
        $packet = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-packet.json') -Raw | ConvertFrom-Json
        $paths = @($packet.diff.changedPaths | ForEach-Object { [string]$_ } | Sort-Object -Unique)
        if ($paths.Count -lt [int]$decompositionPolicy.minimumChangedPaths) { throw "Workspace has $($paths.Count) changed path(s); decomposition requires at least $($decompositionPolicy.minimumChangedPaths)." }
        $buckets = @($paths | Group-Object { Get-BucketKey ([string]$_) } | ForEach-Object {
            [pscustomobject]@{ key = [string]$_.Name; paths = @($_.Group | Sort-Object) }
        })
        while ($buckets.Count -gt $effectiveMaxShards) {
            $smallest = @($buckets | Sort-Object { @($_.paths).Count }, key | Select-Object -First 1)[0]
            $others = @($buckets | Where-Object key -ne $smallest.key)
            $target = @($others | Sort-Object {
                $leftCaps = @(Get-Capabilities $_.paths)
                $rightCaps = @(Get-Capabilities $smallest.paths)
                -@($leftCaps | Where-Object { $_ -in $rightCaps }).Count
            }, { @($_.paths).Count }, key | Select-Object -First 1)[0]
            $target.paths = @($target.paths + $smallest.paths | Sort-Object -Unique)
            $target.key = "$($target.key)-mixed"
            $buckets = @($others)
        }
        if ($buckets.Count -lt 2) { throw 'The workspace does not contain at least two independent decomposition buckets.' }
        $parentName = Split-Path -Leaf $workspace
        $orderedBuckets = @($buckets | Sort-Object key)
        $shards = [Collections.Generic.List[object]]::new()
        for ($index = 0; $index -lt $orderedBuckets.Count; $index++) {
            $bucket = $orderedBuckets[$index]
            $capabilities = @(Get-Capabilities $bucket.paths)
            $shardId = 'S-{0:d3}' -f ($index + 1)
            $childName = "$parentName--{0:d2}-{1}" -f ($index + 1), (Convert-ToSlug $bucket.key)
            $shards.Add([pscustomobject][ordered]@{
                shardId = $shardId
                name = $childName
                workspace = ".artifacts/llm-wiki/tasks/$childName"
                objective = "$($descriptor.objective) [$($bucket.key)]"
                bucket = [string]$bucket.key
                changedPaths = @($bucket.paths)
                changedPathCount = @($bucket.paths).Count
                requiredCapabilities = $capabilities
                prerequisites = @()
            })
        }
        $contractShardIds = @($shards | Where-Object { 'api' -in @($_.requiredCapabilities) -or 'database' -in @($_.requiredCapabilities) } | ForEach-Object shardId)
        foreach ($shard in $shards) {
            if ('tests' -in @($shard.requiredCapabilities)) {
                $shard.prerequisites = @($shards | Where-Object { $_.shardId -ne $shard.shardId -and 'tests' -notin @($_.requiredCapabilities) } | ForEach-Object shardId | Sort-Object)
            } elseif ('frontend' -in @($shard.requiredCapabilities)) {
                $shard.prerequisites = @($contractShardIds | Where-Object { $_ -ne $shard.shardId } | Sort-Object)
            } elseif ('backend' -in @($shard.requiredCapabilities) -and 'api' -notin @($shard.requiredCapabilities)) {
                $shard.prerequisites = @($contractShardIds | Where-Object { $_ -ne $shard.shardId } | Sort-Object)
            }
        }
        $plan = [pscustomobject][ordered]@{
            schemaVersion = 1
            decompositionId = [guid]::NewGuid().ToString('N')
            createdAtUtc = $now.ToString('o')
            policyFingerprint = $policyFingerprint
            parentWorkspace = $workspace
            parentPacketFingerprint = [string]$descriptor.currentPacketFingerprint
            parentObjective = [string]$descriptor.objective
            strategy = 'path-boundary-capability-v1'
            maxShards = $effectiveMaxShards
            sourcePathCount = $paths.Count
            shards = @($shards)
            decompositionHash = ''
        }
        $plan.decompositionHash = Get-Hash (Get-PlanPayload $plan)
        $fileName = "$($now.ToString('yyyyMMddTHHmmssfffZ'))-$($plan.decompositionId).json"
        $path = Write-Atomic $planRoot $fileName $plan 'decomposition'
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'create'; valid = $true; plan = $plan; path = $path.Substring($repositoryRoot.Length + 1).Replace('\', '/') }
    } elseif ($Action -eq 'verify') {
        if ([string]::IsNullOrWhiteSpace($DecompositionId)) { throw 'verify requires DecompositionId.' }
        $plan = Read-Plan $DecompositionId
        $validation = Test-Plan $plan -Current
        $applicationValidations = @((Get-ApplicationFiles) | ForEach-Object {
            try {
                $application = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
                if ([string]$application.decompositionId -ne [string]$plan.decompositionId) { return }
                $applicationValidation = Test-Application $application $plan
                [pscustomobject][ordered]@{ application = $application; valid = $applicationValidation.valid; issues = @($applicationValidation.issues) }
            } catch { [pscustomobject][ordered]@{ application = $null; valid = $false; issues = @($_.Exception.Message) } }
        })
        $applicationIssues = @($applicationValidations | Where-Object { -not $_.valid } | ForEach-Object { @($_.issues) })
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'verify'; valid = $validation.valid -and $applicationIssues.Count -eq 0; issues = @($validation.issues) + $applicationIssues; plan = $plan; applications = $applicationValidations }
    } elseif ($Action -eq 'apply') {
        if ([string]::IsNullOrWhiteSpace($DecompositionId)) { throw 'apply requires DecompositionId.' }
        $plan = Read-Plan $DecompositionId
        $validation = Test-Plan $plan -Current
        if (-not $validation.valid) { throw "Decomposition plan is not current: $(@($validation.issues) -join ' ')" }
        $parentBeforeApply = Get-Content -LiteralPath (Join-Path $repositoryRoot "$($plan.parentWorkspace)/workspace.json") -Raw | ConvertFrom-Json
        $parentDecompositionProperty = $parentBeforeApply.PSObject.Properties['decomposition']
        if ($null -ne $parentDecompositionProperty -and [string]$parentDecompositionProperty.Value.state -eq 'applied') { throw 'Decomposition plan is already applied.' }
        $parentAbsolute = Join-Path $repositoryRoot ([string]$plan.parentWorkspace)
        $parentDescriptorPath = Join-Path $parentAbsolute 'workspace.json'
        $parentDescriptorRaw = Get-Content -LiteralPath $parentDescriptorPath -Raw
        $parentContract = Get-Content -LiteralPath (Join-Path $parentAbsolute 'task-contract.json') -Raw | ConvertFrom-Json
        $created = [Collections.Generic.List[string]]::new()
        try {
            foreach ($shard in @($plan.shards)) {
                & (Join-Path $PSScriptRoot 'Initialize-LlmWikiTaskWorkspace.ps1') `
                    -Objective ([string]$shard.objective) `
                    -Criterion 'Complete and verify this decomposition shard within its assigned path boundary.' `
                    -WorkspacePath ([string]$shard.workspace) `
                    -BaseRef ([string]$parentContract.git.base) `
                    -ChangedPath @($shard.changedPaths) | Out-Null
                $created.Add([string]$shard.workspace)
                if ($null -ne $SimulateFailureAfter -and $created.Count -ge [int]$SimulateFailureAfter) {
                    throw "Injected decomposition failure after $($created.Count) child workspace(s)."
                }
                $childDescriptorPath = Join-Path $repositoryRoot "$($shard.workspace)/workspace.json"
                $childDescriptor = Get-Content -LiteralPath $childDescriptorPath -Raw | ConvertFrom-Json
                $prerequisiteWorkspaces = @($shard.prerequisites | ForEach-Object {
                    $requiredId = [string]$_
                    [string]($plan.shards | Where-Object shardId -eq $requiredId | Select-Object -First 1).workspace
                })
                $childDescriptor | Add-Member -Force -NotePropertyName decomposition -NotePropertyValue ([pscustomobject][ordered]@{
                    parentWorkspace = [string]$plan.parentWorkspace
                    decompositionId = [string]$plan.decompositionId
                    decompositionHash = [string]$plan.decompositionHash
                    shardId = [string]$shard.shardId
                    requiredCapabilities = @($shard.requiredCapabilities)
                    prerequisiteWorkspaces = $prerequisiteWorkspaces
                })
                [IO.File]::WriteAllText($childDescriptorPath, (($childDescriptor | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') create `
                    -WorkspacePath ([string]$shard.workspace) `
                    -AsOfUtc $now | Out-Null
            }
            $parentDescriptor = $parentDescriptorRaw | ConvertFrom-Json
            $parentDescriptor | Add-Member -Force -NotePropertyName decomposition -NotePropertyValue ([pscustomobject][ordered]@{
                state = 'applied'
                decompositionId = [string]$plan.decompositionId
                decompositionHash = [string]$plan.decompositionHash
                appliedAtUtc = $now.ToString('o')
                childWorkspaces = @($created)
            })
            [IO.File]::WriteAllText($parentDescriptorPath, (($parentDescriptor | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
            $application = [pscustomobject][ordered]@{
                schemaVersion = 1
                applicationId = [guid]::NewGuid().ToString('N')
                decompositionId = [string]$plan.decompositionId
                decompositionHash = [string]$plan.decompositionHash
                appliedAtUtc = $now.ToString('o')
                parentWorkspace = [string]$plan.parentWorkspace
                childWorkspaces = @($created)
                applicationHash = ''
            }
            $application.applicationHash = Get-Hash (Get-ApplicationPayload $application)
            $applicationFile = "$($now.ToString('yyyyMMddTHHmmssfffZ'))-$($application.applicationId).json"
            $applicationPath = Write-Atomic $applicationRoot $applicationFile $application 'decomposition-application'
        } catch {
            foreach ($child in @($created | Sort-Object -Descending)) {
                $target = (Join-Path $repositoryRoot $child)
                $resolvedParent = (Resolve-Path (Split-Path -Parent $target)).Path
                if ($resolvedParent -cne (Join-Path $repositoryRoot '.artifacts/llm-wiki/tasks')) { throw "Unsafe decomposition rollback target: $target" }
                if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
            }
            [IO.File]::WriteAllText($parentDescriptorPath, $parentDescriptorRaw, [Text.UTF8Encoding]::new($false))
            throw
        }
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'apply'; valid = $true; plan = $plan; application = $application; path = $applicationPath.Substring($repositoryRoot.Length + 1).Replace('\', '/') }
    } elseif ($Action -eq 'prune') {
        $protected = [Collections.Generic.HashSet[string]]::new()
        foreach ($descriptorFile in @(Get-ChildItem -LiteralPath (Join-Path $repositoryRoot '.artifacts/llm-wiki/tasks') -Filter workspace.json -File -Recurse -ErrorAction SilentlyContinue)) {
            try {
                $descriptor = Get-Content -LiteralPath $descriptorFile.FullName -Raw | ConvertFrom-Json
                $decompositionProperty = $descriptor.PSObject.Properties['decomposition']
                if ($null -ne $decompositionProperty -and -not [string]::IsNullOrWhiteSpace([string]$decompositionProperty.Value.decompositionId)) {
                    [void]$protected.Add([string]$decompositionProperty.Value.decompositionId)
                }
            } catch {}
        }
        $candidates = @(Get-PlanFiles | Sort-Object Name -Descending | Select-Object -Skip ([int]$decompositionPolicy.retentionCount) | Where-Object {
            -not $protected.Contains(($_.BaseName -split '-')[-1])
        })
        foreach ($file in $candidates) { [IO.File]::Delete($file.FullName) }
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'prune'; removedCount = $candidates.Count; protectedCount = $protected.Count }
    } else {
        $planById = @{}
        $plans = @((Get-PlanFiles) | ForEach-Object {
            try {
                $plan = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
                $validation = Test-Plan $plan
                $planById[[string]$plan.decompositionId] = $plan
                [pscustomobject][ordered]@{ decompositionId = [string]$plan.decompositionId; parentWorkspace = [string]$plan.parentWorkspace; createdAtUtc = [string]$plan.createdAtUtc; shardCount = @($plan.shards).Count; valid = $validation.valid; issues = @($validation.issues) }
            } catch { [pscustomobject][ordered]@{ decompositionId = $_.BaseName; valid = $false; issues = @($_.Exception.Message) } }
        })
        $applications = @((Get-ApplicationFiles) | ForEach-Object {
            try {
                $application = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
                $applicationPlan = if ($planById.ContainsKey([string]$application.decompositionId)) { $planById[[string]$application.decompositionId] } else { $null }
                $validation = Test-Application $application $applicationPlan
                [pscustomobject][ordered]@{ applicationId = [string]$application.applicationId; decompositionId = [string]$application.decompositionId; appliedAtUtc = [string]$application.appliedAtUtc; valid = $validation.valid; issues = @($validation.issues) }
            } catch { [pscustomobject][ordered]@{ applicationId = $_.BaseName; valid = $false; issues = @($_.Exception.Message) } }
        })
        $invalidCount = @($plans | Where-Object { -not $_.valid }).Count + @($applications | Where-Object { -not $_.valid }).Count
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'list'; totalCount = $plans.Count; applicationCount = $applications.Count; invalidCount = $invalidCount; plans = $plans; applications = $applications }
    }
} finally {
    if ($null -ne $lockStream) { $lockStream.Dispose() }
    if ($mutating -and (Test-Path -LiteralPath $lockPath)) { [IO.File]::Delete($lockPath) }
}

if ($Format -eq 'Json') { $response | ConvertTo-Json -Depth 30 }
else {
    if ($Action -eq 'list') { Write-Host "Task decompositions: total=$($response.totalCount), invalid=$($response.invalidCount)" }
    elseif ($Action -eq 'create') { Write-Host "Decomposition plan: $($response.plan.decompositionId), shards=$(@($response.plan.shards).Count)" }
    elseif ($Action -eq 'apply') { Write-Host "Applied decomposition: $($response.plan.decompositionId), children=$(@($response.application.childWorkspaces).Count)" }
    else { Write-Host "Task decomposition: action=$Action, valid=$($response.valid)" }
}
if ($FailOnInvalid -and (($Action -eq 'list' -and $response.invalidCount -gt 0) -or ($Action -eq 'verify' -and -not $response.valid))) { exit 1 }
