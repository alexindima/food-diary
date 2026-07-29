[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('assess', 'create', 'show', 'verify')]
    [string]$Action = 'assess',
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
$optimizerPolicy = $bundlePolicy.optimizer
$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') {
    throw 'WorkspacePath must identify one non-hidden task workspace.'
}
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
$bundlePath = Join-Path $absoluteWorkspace 'context-bundle.json'
$receiptPath = Join-Path $absoluteWorkspace 'context-budget.json'
if (-not (Test-Path -LiteralPath $bundlePath -PathType Leaf)) {
    throw "Context bundle is absent: $workspace/context-bundle.json"
}

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 30 -Compress
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
        schemaVersion = [int]$Receipt.schemaVersion
        workspace = [string]$Receipt.workspace
        createdAtUtc = ([DateTimeOffset]$Receipt.createdAtUtc).ToUniversalTime().ToString('o')
        packetFingerprint = [string]$Receipt.packetFingerprint
        policyFingerprint = [string]$Receipt.policyFingerprint
        generatorFingerprint = [string]$Receipt.generatorFingerprint
        contextBundleHash = [string]$Receipt.contextBundleHash
        metrics = $Receipt.metrics
        findings = @($Receipt.findings)
        recommendations = @($Receipt.recommendations)
        verdict = [string]$Receipt.verdict
    }
}
function Get-Percent([double]$Part, [double]$Whole) {
    if ($Whole -le 0) { return 100.0 }
    [Math]::Round(100.0 * $Part / $Whole, 2)
}
function New-BudgetReceipt([string]$CreatedAtUtc) {
    $bundleValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') verify `
        -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    if (-not $bundleValidation.valid) {
        throw "Context bundle is invalid: $(@($bundleValidation.issues) -join ' ')"
    }
    $bundle = $bundleValidation.bundle
    $selected = @($bundle.items)
    $omitted = @($bundle.omitted)
    $required = @($selected | Where-Object required)
    $requiredWithContent = @($required | Where-Object { $null -ne $_.excerpt -and ([string]$_.excerpt.text).Length -gt 0 })
    $contentItems = @($selected | Where-Object { $null -ne $_.excerpt -and ([string]$_.excerpt.text).Length -gt 0 })
    $truncatedItems = @($contentItems | Where-Object { [bool]$_.excerpt.truncated })
    $selectedScore = [double](($selected | Measure-Object -Property score -Sum).Sum)
    $omittedScore = [double](($omitted | Measure-Object -Property score -Sum).Sum)
    $kindCount = @($contentItems.kind | Sort-Object -Unique).Count
    $characterLimit = [int]$bundle.budgets.characterLimit
    $usedCharacters = [int]$bundle.budgets.usedCharacters
    $itemLimit = [int]$bundle.budgets.itemLimit
    $scoreCoverage = Get-Percent $selectedScore ($selectedScore + $omittedScore)
    $utilization = Get-Percent $usedCharacters $characterLimit
    $truncation = Get-Percent $truncatedItems.Count $contentItems.Count
    $requiredCoverage = Get-Percent $requiredWithContent.Count $required.Count
    $findings = [Collections.Generic.List[object]]::new()
    $recommendations = [Collections.Generic.List[object]]::new()
    function Add-Finding([string]$Id, [string]$Severity, [string]$Summary, [object[]]$Evidence) {
        $findings.Add([pscustomobject][ordered]@{
            id = $Id
            severity = $Severity
            summary = $Summary
            evidence = @($Evidence)
        })
    }
    function Add-Recommendation([string]$Id, [string]$Action, [string]$Reason, [int]$Items, [int]$Characters) {
        if ($recommendations.Count -ge [int]$optimizerPolicy.maximumRecommendations) { return }
        $recommendations.Add([pscustomobject][ordered]@{
            id = $Id
            action = $Action
            reason = $Reason
            suggestedItemLimit = [Math]::Min([int]$bundlePolicy.maximumItems, [Math]::Max(1, $Items))
            suggestedCharacterBudget = [Math]::Min([int]$bundlePolicy.maximumTotalCharacters, [Math]::Max(1000, $Characters))
        })
    }
    if ($requiredCoverage -lt 100) {
        Add-Finding 'required-context-without-content' 'critical' 'Mandatory context is selected but has no usable excerpt.' @($required | Where-Object { $null -eq $_.excerpt -or ([string]$_.excerpt.text).Length -eq 0 } | Select-Object -ExpandProperty path)
    }
    if ($scoreCoverage -lt [double]$optimizerPolicy.minimumScoreCoveragePercent) {
        Add-Finding 'low-score-coverage' 'warning' 'The item limit omits too much discovered relevance.' @($omitted | Select-Object -First 10 -ExpandProperty path)
        $neededItems = [Math]::Min([int]$bundlePolicy.maximumItems, $selected.Count + [Math]::Max(1, [Math]::Ceiling($omitted.Count / 2)))
        Add-Recommendation 'increase-item-limit' 'regenerate' 'Include more high-scoring discovered sources.' $neededItems $characterLimit
    }
    if ($utilization -lt [double]$optimizerPolicy.minimumCharacterUtilizationPercent -and $characterLimit -gt 1000) {
        Add-Finding 'low-character-utilization' 'info' 'The character budget is substantially larger than the useful selected context.' @()
        $headroom = 1 + ([double]$optimizerPolicy.recommendedHeadroomPercent / 100)
        Add-Recommendation 'reduce-character-budget' 'regenerate' 'Keep modest headroom while reducing prompt size.' $itemLimit ([Math]::Ceiling($usedCharacters * $headroom))
    }
    if ($truncation -gt [double]$optimizerPolicy.maximumTruncationPercent) {
        Add-Finding 'high-truncation' 'warning' 'Most usable context excerpts are truncated.' @($truncatedItems.path)
        $headroom = 1 + ([double]$optimizerPolicy.recommendedHeadroomPercent / 100)
        Add-Recommendation 'increase-character-budget' 'regenerate' 'Give selected sources more room or decompose the task.' $itemLimit ([Math]::Ceiling($characterLimit * $headroom))
    }
    if ($kindCount -lt [int]$optimizerPolicy.minimumKindDiversity -and $contentItems.Count -gt 1) {
        Add-Finding 'low-kind-diversity' 'warning' 'The context is dominated by one source kind.' @($contentItems.kind | Sort-Object -Unique)
        Add-Recommendation 'broaden-context-kinds' 'review-query' 'Refine the task objective or context search so guidance, implementation, and tests are represented.' $itemLimit $characterLimit
    }
    $zeroContent = @($selected | Where-Object { $null -eq $_.excerpt -or ([string]$_.excerpt.text).Length -eq 0 })
    if ($zeroContent.Count -gt 0) {
        Add-Finding 'empty-selected-items' $(if (@($zeroContent | Where-Object required).Count -gt 0) { 'critical' } else { 'warning' }) 'Selected context items consume item capacity without usable content.' @($zeroContent.path)
        Add-Recommendation 'replace-empty-items' 'regenerate' 'Repair missing or unsupported sources, or replace optional empty items.' $itemLimit $characterLimit
    }
    if ($recommendations.Count -eq 0) {
        Add-Recommendation 'keep-current-budget' 'none' 'Current item and character budgets are balanced for this bundle.' $itemLimit $characterLimit
    }
    $criticalCount = @($findings | Where-Object severity -eq 'critical').Count
    $warningCount = @($findings | Where-Object severity -eq 'warning').Count
    $verdict = if ($criticalCount -gt 0) { 'invalid' } elseif ($warningCount -gt 0) { 'tune' } else { 'balanced' }
    $descriptor = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'workspace.json') -Raw | ConvertFrom-Json
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $workspace
        createdAtUtc = $CreatedAtUtc
        packetFingerprint = [string]$descriptor.currentPacketFingerprint
        policyFingerprint = Get-FileSha $policyPath
        generatorFingerprint = Get-FileSha $PSCommandPath
        contextBundleHash = [string]$bundle.bundleHash
        metrics = [pscustomobject][ordered]@{
            selectedItems = $selected.Count
            contentItems = $contentItems.Count
            omittedItems = $omitted.Count
            requiredCoveragePercent = $requiredCoverage
            scoreCoveragePercent = $scoreCoverage
            characterUtilizationPercent = $utilization
            truncationPercent = $truncation
            kindDiversity = $kindCount
            relevancePerThousandCharacters = $(if ($usedCharacters -eq 0) { 0 } else { [Math]::Round(1000 * $selectedScore / $usedCharacters, 2) })
        }
        findings = @($findings)
        recommendations = @($recommendations)
        verdict = $verdict
        receiptHash = ''
    }
    $receipt.receiptHash = Get-Hash (Get-Payload $receipt)
    $receipt
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    if ([int]$Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $workspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Receipt.policyFingerprint -cne (Get-FileSha $policyPath)) { $issues.Add('Context budget policy drifted.') }
    if ([string]$Receipt.generatorFingerprint -cne (Get-FileSha $PSCommandPath)) { $issues.Add('Context budget generator changed.') }
    $bundle = Get-Content -LiteralPath $bundlePath -Raw | ConvertFrom-Json
    if ([string]$Receipt.contextBundleHash -cne [string]$bundle.bundleHash) { $issues.Add('Context bundle drifted.') }
    if ([string]$Receipt.receiptHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Context budget receipt hash is invalid.') }
    try {
        $expected = New-BudgetReceipt ([string]$Receipt.createdAtUtc)
        if ((Get-Hash $Receipt.metrics) -cne (Get-Hash $expected.metrics)) { $issues.Add('Context budget metrics drifted.') }
        if ((Get-Hash @($Receipt.findings)) -cne (Get-Hash @($expected.findings))) { $issues.Add('Context budget findings drifted.') }
        if ((Get-Hash @($Receipt.recommendations)) -cne (Get-Hash @($expected.recommendations))) { $issues.Add('Context budget recommendations drifted.') }
        if ([string]$Receipt.verdict -cne [string]$expected.verdict) { $issues.Add('Context budget verdict drifted.') }
    } catch { $issues.Add($_.Exception.Message) }
    @($issues)
}

$receipt = $null
$issues = @()
$savedPath = $null
if ($Action -in @('show', 'verify')) {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        $issues = @('context-budget.json is absent.')
    } else {
        try {
            $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
            $issues = @(Test-Receipt $receipt)
        } catch { $issues = @($_.Exception.Message) }
    }
} else {
    $receipt = New-BudgetReceipt ([DateTime]::UtcNow.ToString('o'))
    $issues = @(Test-Receipt $receipt)
    if ($Action -eq 'create' -and $issues.Count -eq 0) {
        [IO.File]::WriteAllText($receiptPath, (($receipt | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        $savedPath = "$workspace/context-budget.json"
    }
}
$result = [pscustomobject][ordered]@{
    action = $Action
    valid = $issues.Count -eq 0 -and $null -ne $receipt -and [string]$receipt.verdict -ne 'invalid'
    receipt = $receipt
    issues = @($issues)
    savedPath = $savedPath
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 30
} else {
    Write-Host "Context budget: action=$Action, valid=$($result.valid)"
    if ($null -ne $receipt) {
        Write-Host "Verdict=$($receipt.verdict), coverage=$($receipt.metrics.scoreCoveragePercent)%, utilization=$($receipt.metrics.characterUtilizationPercent)%, truncation=$($receipt.metrics.truncationPercent)%, hash=$($receipt.receiptHash)"
        foreach ($recommendation in @($receipt.recommendations)) {
            Write-Host " - $($recommendation.id): $($recommendation.reason)"
        }
    }
    foreach ($issue in @($issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
