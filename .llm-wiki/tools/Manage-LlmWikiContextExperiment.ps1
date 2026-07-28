[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('plan', 'run', 'show', 'verify')]
    [string]$Action = 'plan',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
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
$experimentPolicy = $bundlePolicy.experiments
$strategyOutcomePolicy = $bundlePolicy.strategyOutcomes
$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') {
    throw 'WorkspacePath must identify one non-hidden task workspace.'
}
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
foreach ($artifact in @('workspace.json', 'change-packet.json', 'context-bundle.json', 'context-budget.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $absoluteWorkspace $artifact) -PathType Leaf)) {
        throw "Experiment input is absent: $workspace/$artifact"
    }
}
$receiptPath = Join-Path $absoluteWorkspace 'context-experiment.json'

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 50 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
}
function Get-FileSha([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = $Receipt.schemaVersion
        workspace = $Receipt.workspace
        createdAtUtc = $Receipt.createdAtUtc
        policyFingerprint = $Receipt.policyFingerprint
        generatorFingerprint = $Receipt.generatorFingerprint
        inputs = $Receipt.inputs
        plan = @($Receipt.plan)
        results = @($Receipt.results)
        recommendation = $Receipt.recommendation
    }
}
function Limit-Integer([double]$Value, [int]$Minimum, [int]$Maximum) {
    [Math]::Min($Maximum, [Math]::Max($Minimum, [Math]::Round($Value)))
}
function Get-Plan([object]$Bundle, [object]$Budget) {
    $itemLimit = [int]$Bundle.budgets.itemLimit
    $characterBudget = [int]$Bundle.budgets.characterLimit
    $recommendation = @($Budget.receipt.recommendations | Select-Object -First 1)
    $balancedItems = if ($recommendation.Count -gt 0) { [int]$recommendation[0].suggestedItemLimit } else { $itemLimit }
    $balancedCharacters = if ($recommendation.Count -gt 0) { [int]$recommendation[0].suggestedCharacterBudget } else { $characterBudget }
    $variants = @(
        [pscustomobject][ordered]@{ id = 'baseline'; itemLimit = $itemLimit; characterBudget = $characterBudget; rationale = 'Rebuild the current limits as the control.' }
        [pscustomobject][ordered]@{
            id = 'compact'
            itemLimit = $itemLimit
            characterBudget = Limit-Integer ($characterBudget * [double]$experimentPolicy.compactCharacterPercent / 100) ([int]$experimentPolicy.minimumCharacterBudget) ([int]$bundlePolicy.maximumTotalCharacters)
            rationale = 'Reduce prompt size while preserving the selected-item capacity.'
        }
        [pscustomobject][ordered]@{
            id = 'coverage'
            itemLimit = Limit-Integer ($itemLimit * [double]$experimentPolicy.coverageItemPercent / 100) ([int]$experimentPolicy.minimumItemLimit) ([int]$bundlePolicy.maximumItems)
            characterBudget = Limit-Integer ($characterBudget * [double]$experimentPolicy.coverageCharacterPercent / 100) ([int]$experimentPolicy.minimumCharacterBudget) ([int]$bundlePolicy.maximumTotalCharacters)
            rationale = 'Trade modest prompt growth for more discovered-relevance coverage.'
        }
        [pscustomobject][ordered]@{
            id = 'depth'
            itemLimit = $itemLimit
            characterBudget = Limit-Integer ($characterBudget * [double]$experimentPolicy.depthCharacterPercent / 100) ([int]$experimentPolicy.minimumCharacterBudget) ([int]$bundlePolicy.maximumTotalCharacters)
            rationale = 'Give the same source set deeper excerpts.'
        }
        [pscustomobject][ordered]@{
            id = 'balanced'
            itemLimit = Limit-Integer $balancedItems ([int]$experimentPolicy.minimumItemLimit) ([int]$bundlePolicy.maximumItems)
            characterBudget = Limit-Integer $balancedCharacters ([int]$experimentPolicy.minimumCharacterBudget) ([int]$bundlePolicy.maximumTotalCharacters)
            rationale = 'Apply the deterministic recommendation from the current budget receipt.'
        }
    )
    @($variants |
        Group-Object { "$($_.itemLimit):$($_.characterBudget)" } |
        ForEach-Object { $_.Group | Select-Object -First 1 } |
        Select-Object -First ([int]$experimentPolicy.maximumVariants))
}
function Get-CurrentInputs {
    $bundleCheck = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    if (-not $bundleCheck.valid) { throw "Baseline context bundle is invalid: $(@($bundleCheck.issues) -join ' ')" }
    $budgetCheck = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBudget.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    if (-not $budgetCheck.valid) { throw "Baseline context budget is invalid: $(@($budgetCheck.issues) -join ' ')" }
    [pscustomobject]@{ bundle = $bundleCheck.bundle; budget = $budgetCheck }
}
function Remove-DerivedContext([string]$Directory) {
    foreach ($name in @('completion.json', 'completion.md', 'context-security.json', 'context-bundle.json', 'context-budget.json', 'context-benchmark.json', 'context-experiment.json', 'confidence-ledger.json', 'change-critique.json')) {
        $path = Join-Path $Directory $name
        if (Test-Path -LiteralPath $path -PathType Leaf) { [IO.File]::Delete($path) }
    }
}
function New-Receipt([string]$CreatedAtUtc) {
    $baseline = Get-CurrentInputs
    $outcomeMetrics = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextOutcome.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $outcomeMetrics.valid) { throw "Context strategy outcome history is invalid: $(@($outcomeMetrics.issues) -join ' ')" }
    $taskOutcomeProfile = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextOutcome.ps1') profile -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    if (-not $taskOutcomeProfile.valid) { throw 'Unable to derive a context outcome cohort for the task.' }
    $plan = @(Get-Plan $baseline.bundle $baseline.budget)
    $results = [Collections.Generic.List[object]]::new()
    $tasksRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki/tasks'))
    foreach ($variant in $plan) {
        $temporaryName = "context-experiment-$([Guid]::NewGuid().ToString('N'))"
        $temporaryWorkspace = ".artifacts/llm-wiki/tasks/$temporaryName"
        $temporaryAbsolute = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $temporaryWorkspace))
        if (-not $temporaryAbsolute.StartsWith($tasksRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Unsafe experiment workspace path: $temporaryAbsolute"
        }
        try {
            Copy-Item -LiteralPath $absoluteWorkspace -Destination $temporaryAbsolute -Recurse
            Remove-DerivedContext $temporaryAbsolute
            $bundleResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') create `
                -WorkspacePath $temporaryWorkspace `
                -Limit ([int]$variant.itemLimit) `
                -CharacterBudget ([int]$variant.characterBudget) `
                -Format Json | ConvertFrom-Json
            $budgetResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBudget.ps1') create `
                -WorkspacePath $temporaryWorkspace `
                -Format Json | ConvertFrom-Json
            $benchmarkResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBenchmark.ps1') compare `
                -SourceWorkspacePath $workspace `
                -WorkspacePath $temporaryWorkspace `
                -Format Json | ConvertFrom-Json
            $cohortOutcomeProfile = $outcomeMetrics.metrics.cohortProfiles | Where-Object {
                $_.variantId -eq $variant.id -and
                $_.cohortKey -eq $taskOutcomeProfile.profile.cohortKey -and
                $_.eligible
            } | Select-Object -First 1
            $globalOutcomeProfile = $outcomeMetrics.metrics.profiles | Where-Object {
                $_.variantId -eq $variant.id -and $_.eligible
            } | Select-Object -First 1
            $outcomeProfile = if ($null -ne $cohortOutcomeProfile) { $cohortOutcomeProfile } else { $globalOutcomeProfile }
            $outcomeSource = if ($null -ne $cohortOutcomeProfile) { 'cohort' } elseif ($null -ne $globalOutcomeProfile) { 'global' } else { 'none' }
            $empiricalAdjustment = if ($null -eq $outcomeProfile) { 0.0 } else { [double]$outcomeProfile.experimentAdjustmentPoints }
            $empiricalHealth = if ($null -eq $outcomeProfile) { 'insufficient-data' } else { [string]$outcomeProfile.health }
            $healthGatePassed = -not ([bool]$strategyOutcomePolicy.blockDegradedAdoption -and $empiricalHealth -eq 'degraded')
            $valid = [bool]($bundleResult.valid -and $budgetResult.valid -and $benchmarkResult.valid)
            $verdict = [string]$benchmarkResult.receipt.verdict
            $adoptionBlocks = @()
            if (-not $valid) { $adoptionBlocks += 'invalid-artifacts' }
            if ($verdict -eq 'regressed') { $adoptionBlocks += 'synthetic-regression' }
            if (-not $healthGatePassed) { $adoptionBlocks += 'degraded-outcome-history' }
            $qualityScore = [double]$benchmarkResult.receipt.candidate.qualityScore
            $results.Add([pscustomobject][ordered]@{
                id = [string]$variant.id
                itemLimit = [int]$variant.itemLimit
                characterBudget = [int]$variant.characterBudget
                valid = $valid
                verdict = $verdict
                comparabilityScore = [double]$benchmarkResult.receipt.comparability.score
                qualityScore = $qualityScore
                empiricalSampleCount = $(if ($null -eq $outcomeProfile) { 0 } else { [int]$outcomeProfile.sampleCount })
                empiricalOutcomeScore = $(if ($null -eq $outcomeProfile) { $null } else { [double]$outcomeProfile.posteriorOutcomeScore })
                empiricalRawOutcomeScore = $(if ($null -eq $outcomeProfile) { $null } else { [double]$outcomeProfile.averageOutcomeScore })
                empiricalConfidencePercent = $(if ($null -eq $outcomeProfile) { 0 } else { [double]$outcomeProfile.confidencePercent })
                empiricalAdjustmentPoints = $empiricalAdjustment
                empiricalSource = $outcomeSource
                empiricalHealth = $empiricalHealth
                empiricalCohortKey = [string]$taskOutcomeProfile.profile.cohortKey
                healthGatePassed = $healthGatePassed
                adoptionEligible = [bool]($valid -and $verdict -ne 'regressed' -and $healthGatePassed)
                adoptionBlocks = @($adoptionBlocks)
                effectiveQualityScore = [Math]::Round($qualityScore + $empiricalAdjustment, 2)
                qualityDelta = [double]$benchmarkResult.receipt.deltas.qualityScore
                usedCharacters = [int]$benchmarkResult.receipt.candidate.usedCharacters
                scoreCoveragePercent = [double]$budgetResult.receipt.metrics.scoreCoveragePercent
                truncationPercent = [double]$budgetResult.receipt.metrics.truncationPercent
                securityFindingCount = [int]$bundleResult.bundle.security.findingCount
                quarantineMatchCount = [int]$bundleResult.bundle.security.quarantineMatchCount
                bundleHash = [string]$bundleResult.bundle.bundleHash
                budgetReceiptHash = [string]$budgetResult.receipt.receiptHash
            })
        } finally {
            if (Test-Path -LiteralPath $temporaryAbsolute -PathType Container) {
                [IO.Directory]::Delete($temporaryAbsolute, $true)
            }
        }
    }
    $eligible = @($results | Where-Object adoptionEligible)
    $winner = $eligible | Sort-Object @{ Expression = 'effectiveQualityScore'; Descending = $true }, @{ Expression = 'qualityScore'; Descending = $true }, usedCharacters, id | Select-Object -First 1
    $recommendation = if ($null -eq $winner) {
        [pscustomobject][ordered]@{ verdict = 'no-safe-variant'; variantId = ''; itemLimit = 0; characterBudget = 0; reason = 'Every generated variant was blocked by validation, synthetic regression, or governed outcome-health gates.' }
    } else {
        [pscustomobject][ordered]@{
            verdict = $(if ($winner.qualityDelta -gt 0) { 'adopt' } else { 'keep-or-adopt-for-efficiency' })
            variantId = [string]$winner.id
            itemLimit = [int]$winner.itemLimit
            characterBudget = [int]$winner.characterBudget
            reason = "Highest outcome-adjusted quality score among variants that passed validation, synthetic-regression, and governed outcome-health gates; ties prefer synthetic quality, fewer used characters, and a stable variant id."
        }
    }
    $descriptor = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'workspace.json') -Raw | ConvertFrom-Json
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $workspace
        createdAtUtc = $CreatedAtUtc
        policyFingerprint = Get-FileSha $policyPath
        generatorFingerprint = Get-FileSha $PSCommandPath
        inputs = [pscustomobject][ordered]@{
            packetFingerprint = [string]$descriptor.currentPacketFingerprint
            baselineBundleHash = [string]$baseline.bundle.bundleHash
            baselineBudgetReceiptHash = [string]$baseline.budget.receipt.receiptHash
            strategyOutcomeRegistryFingerprint = [string]$outcomeMetrics.metrics.registryFingerprint
            strategyOutcomeCohortKey = [string]$taskOutcomeProfile.profile.cohortKey
        }
        plan = $plan
        results = @($results)
        recommendation = $recommendation
        receiptHash = ''
    }
    $receipt.receiptHash = Get-Hash (Get-Payload $receipt)
    $receipt
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    if ([int]$Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $workspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Receipt.policyFingerprint -cne (Get-FileSha $policyPath)) { $issues.Add('Context experiment policy drifted.') }
    if ([string]$Receipt.generatorFingerprint -cne (Get-FileSha $PSCommandPath)) { $issues.Add('Context experiment generator changed.') }
    $descriptor = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'workspace.json') -Raw | ConvertFrom-Json
    $bundle = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'context-bundle.json') -Raw | ConvertFrom-Json
    $budget = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'context-budget.json') -Raw | ConvertFrom-Json
    if ([string]$Receipt.inputs.packetFingerprint -cne [string]$descriptor.currentPacketFingerprint) { $issues.Add('Task packet drifted.') }
    if ([string]$Receipt.inputs.baselineBundleHash -cne [string]$bundle.bundleHash) { $issues.Add('Experiment baseline bundle drifted.') }
    if ([string]$Receipt.inputs.baselineBudgetReceiptHash -cne [string]$budget.receiptHash) { $issues.Add('Experiment baseline budget drifted.') }
    $outcomeMetrics = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextOutcome.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $outcomeMetrics.valid) { $issues.Add("Context strategy outcome history is invalid: $(@($outcomeMetrics.issues) -join ' ')") }
    if ([string]$Receipt.inputs.strategyOutcomeRegistryFingerprint -cne [string]$outcomeMetrics.metrics.registryFingerprint) { $issues.Add('Context strategy outcome history drifted.') }
    $taskOutcomeProfile = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextOutcome.ps1') profile -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    if ([string]$Receipt.inputs.strategyOutcomeCohortKey -cne [string]$taskOutcomeProfile.profile.cohortKey) { $issues.Add('Context strategy outcome cohort drifted.') }
    foreach ($result in @($Receipt.results)) {
        if ([string]$result.empiricalCohortKey -cne [string]$Receipt.inputs.strategyOutcomeCohortKey) { $issues.Add("Experiment result cohort drifted for '$($result.id)'.") }
        if ([string]$result.empiricalSource -notin @('none', 'global', 'cohort')) { $issues.Add("Experiment outcome source is invalid for '$($result.id)'.") }
        if ([string]$result.empiricalHealth -notin @('insufficient-data', 'healthy', 'degraded')) { $issues.Add("Experiment outcome health is invalid for '$($result.id)'.") }
        if ([string]$result.empiricalHealth -eq 'degraded' -and [double]$result.empiricalAdjustmentPoints -gt 0) { $issues.Add("Degraded outcome history boosted '$($result.id)'.") }
        $expectedHealthGate = -not ([bool]$strategyOutcomePolicy.blockDegradedAdoption -and [string]$result.empiricalHealth -eq 'degraded')
        if ([bool]$result.healthGatePassed -ne $expectedHealthGate) { $issues.Add("Experiment outcome health gate is invalid for '$($result.id)'.") }
        $expectedBlocks = @()
        if (-not [bool]$result.valid) { $expectedBlocks += 'invalid-artifacts' }
        if ([string]$result.verdict -eq 'regressed') { $expectedBlocks += 'synthetic-regression' }
        if (-not $expectedHealthGate) { $expectedBlocks += 'degraded-outcome-history' }
        $actualBlocks = @($result.adoptionBlocks)
        if ((@($actualBlocks | Sort-Object) -join '|') -cne (@($expectedBlocks | Sort-Object) -join '|')) { $issues.Add("Experiment adoption blocks are invalid for '$($result.id)'.") }
        $expectedEligibility = [bool]([bool]$result.valid -and [string]$result.verdict -ne 'regressed' -and $expectedHealthGate)
        if ([bool]$result.adoptionEligible -ne $expectedEligibility) { $issues.Add("Experiment adoption eligibility is invalid for '$($result.id)'.") }
        if ([double]$result.empiricalConfidencePercent -lt 0 -or [double]$result.empiricalConfidencePercent -gt 100) { $issues.Add("Experiment outcome confidence is invalid for '$($result.id)'.") }
        if ([double]$result.effectiveQualityScore -ne [Math]::Round([double]$result.qualityScore + [double]$result.empiricalAdjustmentPoints, 2)) { $issues.Add("Experiment effective quality is invalid for '$($result.id)'.") }
    }
    if ([string]$Receipt.receiptHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Context experiment receipt hash is invalid.') }
    $resultIds = @($Receipt.results.id)
    if ($resultIds.Count -eq 0 -or @($resultIds | Sort-Object -Unique).Count -ne $resultIds.Count) { $issues.Add('Experiment results must be non-empty and unique.') }
    if ($resultIds.Count -gt [int]$experimentPolicy.maximumVariants) { $issues.Add('Experiment exceeded maximumVariants.') }
    if ([string]$Receipt.recommendation.verdict -ne 'no-safe-variant' -and [string]$Receipt.recommendation.variantId -notin $resultIds) {
        $issues.Add('Recommended variant is absent from results.')
    } elseif ([string]$Receipt.recommendation.verdict -ne 'no-safe-variant') {
        $recommendedResult = $Receipt.results | Where-Object id -eq $Receipt.recommendation.variantId | Select-Object -First 1
        if (-not [bool]$recommendedResult.adoptionEligible) { $issues.Add('Recommended context variant is not adoption eligible.') }
    } elseif (@($Receipt.results | Where-Object adoptionEligible).Count -gt 0) {
        $issues.Add('Experiment reports no safe variant while adoption-eligible results exist.')
    }
    @($issues)
}

$receipt = $null
$issues = @()
$savedPath = $null
if ($Action -in @('show', 'verify')) {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        $issues = @('context-experiment.json is absent.')
    } else {
        try {
            $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
            $issues = @(Test-Receipt $receipt)
        } catch { $issues = @($_.Exception.Message) }
    }
} elseif ($Action -eq 'plan') {
    $baseline = Get-CurrentInputs
    $receipt = [pscustomobject][ordered]@{
        workspace = $workspace
        variants = @(Get-Plan $baseline.bundle $baseline.budget)
        baselineBundleHash = [string]$baseline.bundle.bundleHash
        baselineBudgetReceiptHash = [string]$baseline.budget.receipt.receiptHash
    }
} else {
    $receipt = New-Receipt ([DateTime]::UtcNow.ToString('o'))
    $issues = @(Test-Receipt $receipt)
    if ($issues.Count -eq 0) {
        [IO.File]::WriteAllText($receiptPath, (($receipt | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        $savedPath = "$workspace/context-experiment.json"
    }
}
$valid = $issues.Count -eq 0 -and $null -ne $receipt
$result = [pscustomobject][ordered]@{ action = $Action; valid = $valid; receipt = $receipt; issues = @($issues); savedPath = $savedPath }
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 50
} else {
    Write-Host "Context experiment: action=$Action, valid=$valid"
    if ($Action -eq 'plan') {
        foreach ($variant in @($receipt.variants)) { Write-Host " - $($variant.id): items=$($variant.itemLimit), characters=$($variant.characterBudget)" }
    } elseif ($null -ne $receipt) {
        foreach ($variant in @($receipt.results)) { Write-Host " - $($variant.id): quality=$($variant.qualityScore), empirical=$($variant.empiricalAdjustmentPoints), effective=$($variant.effectiveQualityScore), health=$($variant.empiricalHealth), eligible=$($variant.adoptionEligible), blocks=$(@($variant.adoptionBlocks) -join ','), delta=$($variant.qualityDelta), verdict=$($variant.verdict), characters=$($variant.usedCharacters)" }
        Write-Host "Recommendation=$($receipt.recommendation.variantId), items=$($receipt.recommendation.itemLimit), characters=$($receipt.recommendation.characterBudget), hash=$($receipt.receiptHash)"
    }
    foreach ($issue in @($issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $valid) { exit 1 }
