[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('compare', 'create', 'show', 'verify')]
    [string]$Action = 'compare',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$SourceWorkspacePath,
    [switch]$FailOnRegression,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$benchmarkPolicy = $policy.scheduler.contextBundles.benchmark

function Normalize-Workspace([string]$Value, [string]$ParameterName) {
    if ([string]::IsNullOrWhiteSpace($Value) -or [IO.Path]::IsPathRooted($Value)) {
        throw "$ParameterName must be repository-relative."
    }
    $normalized = $Value.Replace('\', '/').TrimEnd('/')
    if ($normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') {
        throw "$ParameterName must identify one non-hidden task workspace."
    }
    foreach ($artifact in @('workspace.json', 'change-packet.json', 'context-bundle.json', 'context-budget.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot "$normalized/$artifact") -PathType Leaf)) {
            throw "Benchmark input is absent: $normalized/$artifact"
        }
    }
    $normalized
}
function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
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
        sourceWorkspace = $Receipt.sourceWorkspace
        createdAtUtc = $Receipt.createdAtUtc
        policyFingerprint = $Receipt.policyFingerprint
        generatorFingerprint = $Receipt.generatorFingerprint
        inputs = $Receipt.inputs
        comparability = $Receipt.comparability
        baseline = $Receipt.baseline
        candidate = $Receipt.candidate
        deltas = $Receipt.deltas
        gates = @($Receipt.gates)
        verdict = $Receipt.verdict
    }
}
function Get-Jaccard([object[]]$Left, [object[]]$Right) {
    $leftValues = @($Left | ForEach-Object { ([string]$_).ToLowerInvariant() } | Sort-Object -Unique)
    $rightValues = @($Right | ForEach-Object { ([string]$_).ToLowerInvariant() } | Sort-Object -Unique)
    $union = @($leftValues + $rightValues | Sort-Object -Unique)
    if ($union.Count -eq 0) { return 100.0 }
    $common = @($leftValues | Where-Object { $_ -in $rightValues })
    [Math]::Round(100 * $common.Count / $union.Count, 2)
}
function Get-BudgetFit([double]$Utilization) {
    # Full score inside the useful 35-90% band; degrade linearly outside it.
    if ($Utilization -ge 35 -and $Utilization -le 90) { return 100.0 }
    if ($Utilization -lt 35) { return [Math]::Round([Math]::Max(0, 100 * $Utilization / 35), 2) }
    [Math]::Round([Math]::Max(0, 100 - (($Utilization - 90) * 10)), 2)
}
function Get-Quality([object]$Bundle, [object]$Budget) {
    $metrics = $Budget.receipt.metrics
    $contentYield = if ([int]$metrics.selectedItems -eq 0) { 100.0 } else {
        [Math]::Round(100 * [int]$metrics.contentItems / [int]$metrics.selectedItems, 2)
    }
    $diversity = [Math]::Min(100, 25 * [int]$metrics.kindDiversity)
    $components = [pscustomobject][ordered]@{
        requiredCoverage = [double]$metrics.requiredCoveragePercent
        scoreCoverage = [double]$metrics.scoreCoveragePercent
        lowTruncation = [Math]::Max(0, 100 - [double]$metrics.truncationPercent)
        contentYield = $contentYield
        kindDiversity = $diversity
        budgetFit = Get-BudgetFit ([double]$metrics.characterUtilizationPercent)
    }
    $score = 0.0
    foreach ($property in @($components.PSObject.Properties)) {
        $score += [double]$property.Value * [double]$benchmarkPolicy.weights.($property.Name) / 100
    }
    [pscustomobject][ordered]@{
        qualityScore = [Math]::Round($score, 2)
        components = $components
        selectedItems = [int]$metrics.selectedItems
        usedCharacters = [int]$Bundle.budgets.usedCharacters
        relevancePerThousandCharacters = [double]$metrics.relevancePerThousandCharacters
        securityFindingCount = [int]$Bundle.security.findingCount
        quarantineMatchCount = [int]$Bundle.security.quarantineMatchCount
        bundleHash = [string]$Bundle.bundleHash
        budgetReceiptHash = [string]$Budget.receipt.receiptHash
    }
}
function Read-VerifiedInput([string]$NormalizedWorkspace) {
    $bundleResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') verify `
        -WorkspacePath $NormalizedWorkspace -Format Json | ConvertFrom-Json
    if (-not $bundleResult.valid) { throw "Context bundle is invalid for ${NormalizedWorkspace}: $(@($bundleResult.issues) -join ' ')" }
    $budgetResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBudget.ps1') verify `
        -WorkspacePath $NormalizedWorkspace -Format Json | ConvertFrom-Json
    if (-not $budgetResult.valid) { throw "Context budget is invalid for ${NormalizedWorkspace}: $(@($budgetResult.issues) -join ' ')" }
    [pscustomobject]@{
        descriptor = Get-Content -LiteralPath (Join-Path $repositoryRoot "$NormalizedWorkspace/workspace.json") -Raw | ConvertFrom-Json
        packet = Get-Content -LiteralPath (Join-Path $repositoryRoot "$NormalizedWorkspace/change-packet.json") -Raw | ConvertFrom-Json
        bundle = $bundleResult.bundle
        budget = $budgetResult
    }
}
function New-Benchmark([string]$TargetWorkspace, [string]$BaselineWorkspace, [string]$CreatedAtUtc) {
    $target = Read-VerifiedInput $TargetWorkspace
    $source = Read-VerifiedInput $BaselineWorkspace
    $pathSimilarity = Get-Jaccard @($source.packet.diff.changedPaths) @($target.packet.diff.changedPaths)
    $moduleSimilarity = Get-Jaccard @($source.packet.diff.modules.name) @($target.packet.diff.modules.name)
    $scopeSimilarity = Get-Jaccard @($source.packet.diff.scopes) @($target.packet.diff.scopes)
    $comparabilityScore = [Math]::Round(($pathSimilarity * 0.5) + ($moduleSimilarity * 0.3) + ($scopeSimilarity * 0.2), 2)
    $baseline = Get-Quality $source.bundle $source.budget
    $candidate = Get-Quality $target.bundle $target.budget
    $deltas = [pscustomobject][ordered]@{
        qualityScore = [Math]::Round($candidate.qualityScore - $baseline.qualityScore, 2)
        requiredCoverage = [Math]::Round($candidate.components.requiredCoverage - $baseline.components.requiredCoverage, 2)
        scoreCoverage = [Math]::Round($candidate.components.scoreCoverage - $baseline.components.scoreCoverage, 2)
        truncation = [Math]::Round((100 - $candidate.components.lowTruncation) - (100 - $baseline.components.lowTruncation), 2)
        selectedItems = $candidate.selectedItems - $baseline.selectedItems
        usedCharacters = $candidate.usedCharacters - $baseline.usedCharacters
        relevancePerThousandCharacters = [Math]::Round($candidate.relevancePerThousandCharacters - $baseline.relevancePerThousandCharacters, 2)
        securityFindings = $candidate.securityFindingCount - $baseline.securityFindingCount
        quarantineMatches = $candidate.quarantineMatchCount - $baseline.quarantineMatchCount
    }
    $gates = @(
        [pscustomobject][ordered]@{ id = 'comparable-tasks'; passed = $comparabilityScore -ge [double]$benchmarkPolicy.minimumComparabilityPercent; actual = $comparabilityScore; threshold = [double]$benchmarkPolicy.minimumComparabilityPercent }
        [pscustomobject][ordered]@{ id = 'required-coverage-regression'; passed = $deltas.requiredCoverage -ge (-[double]$benchmarkPolicy.maximumRequiredCoverageRegressionPoints); actual = $deltas.requiredCoverage; threshold = -[double]$benchmarkPolicy.maximumRequiredCoverageRegressionPoints }
        [pscustomobject][ordered]@{ id = 'security-finding-regression'; passed = $deltas.securityFindings -le [int]$benchmarkPolicy.maximumSecurityFindingIncrease; actual = $deltas.securityFindings; threshold = [int]$benchmarkPolicy.maximumSecurityFindingIncrease }
        [pscustomobject][ordered]@{ id = 'quarantine-regression'; passed = $deltas.quarantineMatches -le 0; actual = $deltas.quarantineMatches; threshold = 0 }
    )
    $hardGatePassed = @($gates | Where-Object { -not $_.passed }).Count -eq 0
    $verdict = if (-not $hardGatePassed) {
        'regressed'
    } elseif ($deltas.qualityScore -ge [double]$benchmarkPolicy.minimumImprovementPoints) {
        'improved'
    } elseif ($deltas.qualityScore -le (-[double]$benchmarkPolicy.minimumImprovementPoints)) {
        'regressed'
    } else { 'equivalent' }
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $TargetWorkspace
        sourceWorkspace = $BaselineWorkspace
        createdAtUtc = $CreatedAtUtc
        policyFingerprint = Get-FileSha $policyPath
        generatorFingerprint = Get-FileSha $PSCommandPath
        inputs = [pscustomobject][ordered]@{
            sourcePacketFingerprint = [string]$source.descriptor.currentPacketFingerprint
            candidatePacketFingerprint = [string]$target.descriptor.currentPacketFingerprint
            sourceBundleHash = [string]$source.bundle.bundleHash
            candidateBundleHash = [string]$target.bundle.bundleHash
            sourceBudgetReceiptHash = [string]$source.budget.receipt.receiptHash
            candidateBudgetReceiptHash = [string]$target.budget.receipt.receiptHash
        }
        comparability = [pscustomobject][ordered]@{
            score = $comparabilityScore
            minimumScore = [double]$benchmarkPolicy.minimumComparabilityPercent
            pathSimilarity = $pathSimilarity
            moduleSimilarity = $moduleSimilarity
            scopeSimilarity = $scopeSimilarity
        }
        baseline = $baseline
        candidate = $candidate
        deltas = $deltas
        gates = $gates
        verdict = $verdict
        receiptHash = ''
    }
    $receipt.receiptHash = Get-Hash (Get-Payload $receipt)
    $receipt
}
function Test-Benchmark([object]$Receipt, [string]$TargetWorkspace) {
    $issues = [Collections.Generic.List[string]]::new()
    if ([int]$Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $TargetWorkspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Receipt.policyFingerprint -cne (Get-FileSha $policyPath)) { $issues.Add('Context benchmark policy drifted.') }
    if ([string]$Receipt.generatorFingerprint -cne (Get-FileSha $PSCommandPath)) { $issues.Add('Context benchmark generator changed.') }
    if ([string]$Receipt.receiptHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Context benchmark receipt hash is invalid.') }
    try {
        $expected = New-Benchmark $TargetWorkspace ([string]$Receipt.sourceWorkspace) ([string]$Receipt.createdAtUtc)
        foreach ($part in @('inputs', 'comparability', 'baseline', 'candidate', 'deltas', 'gates')) {
            if ((Get-Hash $Receipt.$part) -cne (Get-Hash $expected.$part)) { $issues.Add("Context benchmark $part drifted.") }
        }
        if ([string]$Receipt.verdict -cne [string]$expected.verdict) { $issues.Add('Context benchmark verdict drifted.') }
    } catch { $issues.Add($_.Exception.Message) }
    @($issues)
}

$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
$receiptPath = Join-Path $repositoryRoot "$workspace/context-benchmark.json"
$receipt = $null
$issues = @()
$savedPath = $null
if ($Action -in @('show', 'verify')) {
    $workspace = Normalize-Workspace $WorkspacePath 'WorkspacePath'
    $receiptPath = Join-Path $repositoryRoot "$workspace/context-benchmark.json"
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        $issues = @('context-benchmark.json is absent.')
    } else {
        try {
            $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
            $issues = @(Test-Benchmark $receipt $workspace)
        } catch { $issues = @($_.Exception.Message) }
    }
} else {
    $workspace = Normalize-Workspace $WorkspacePath 'WorkspacePath'
    $sourceWorkspace = Normalize-Workspace $SourceWorkspacePath 'SourceWorkspacePath'
    $receiptPath = Join-Path $repositoryRoot "$workspace/context-benchmark.json"
    $receipt = New-Benchmark $workspace $sourceWorkspace ([DateTime]::UtcNow.ToString('o'))
    $issues = @(Test-Benchmark $receipt $workspace)
    if ($Action -eq 'create' -and $issues.Count -eq 0) {
        [IO.File]::WriteAllText($receiptPath, (($receipt | ConvertTo-Json -Depth 40) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        $savedPath = "$workspace/context-benchmark.json"
    }
}
$valid = $issues.Count -eq 0 -and $null -ne $receipt
$result = [pscustomobject][ordered]@{
    action = $Action
    valid = $valid
    regression = $valid -and [string]$receipt.verdict -eq 'regressed'
    receipt = $receipt
    issues = @($issues)
    savedPath = $savedPath
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 40
} else {
    Write-Host "Context benchmark: action=$Action, valid=$valid"
    if ($null -ne $receipt) {
        Write-Host "Verdict=$($receipt.verdict), comparable=$($receipt.comparability.score)%, baseline=$($receipt.baseline.qualityScore), candidate=$($receipt.candidate.qualityScore), delta=$($receipt.deltas.qualityScore), hash=$($receipt.receiptHash)"
        foreach ($gate in @($receipt.gates | Where-Object { -not $_.passed })) { Write-Host " - failed gate: $($gate.id), actual=$($gate.actual), threshold=$($gate.threshold)" }
    }
    foreach ($issue in @($issues)) { Write-Host " - $issue" }
}
if (($FailOnInvalid -and -not $valid) -or ($FailOnRegression -and $result.regression)) { exit 1 }
