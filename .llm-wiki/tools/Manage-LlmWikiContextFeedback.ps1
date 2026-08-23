[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('record', 'list', 'verify', 'metrics', 'prune')]
    [string]$Action = 'list',
    [string]$DispatchId,
    [string]$Owner,
    [string[]]$HelpfulPath,
    [string[]]$NoisyPath,
    [string[]]$MissingPath,
    [string]$Reason,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$Apply,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$feedbackRoot = if ([string]::IsNullOrWhiteSpace($env:LLM_WIKI_TEST_CONTEXT_FEEDBACK_ROOT)) {
    Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler/context-feedback'
} else {
    $candidate = [IO.Path]::GetFullPath($env:LLM_WIKI_TEST_CONTEXT_FEEDBACK_ROOT)
    $artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki'))
    if (-not $candidate.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'LLM_WIKI_TEST_CONTEXT_FEEDBACK_ROOT must resolve under .artifacts/llm-wiki.' }
    $candidate
}
$dispatchRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler/dispatches'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$feedbackPolicy = $policy.scheduler.contextBundles.feedback
$now = $AsOfUtc.ToUniversalTime()

function Get-Hash([object]$Value) {
    if ($null -eq $Value) { $Value = @() }
    $json = ConvertTo-Json -InputObject $Value -Depth 30 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Normalize-PathList([string[]]$Values) {
    @($Values | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object {
        if ([IO.Path]::IsPathRooted($_)) { throw "Context feedback paths must be repository-relative: $_" }
        $value = $_.Replace('\', '/')
        while ($value.StartsWith('./', [StringComparison]::Ordinal)) { $value = $value.Substring(2) }
        if ($value -match '(^|/)\.\.(/|$)' -or $value.StartsWith('.artifacts/', [StringComparison]::Ordinal)) { throw "Unsafe context feedback path: $_" }
        $value
    } | Sort-Object -Unique)
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = $Receipt.schemaVersion
        feedbackId = $Receipt.feedbackId
        dispatchId = $Receipt.dispatchId
        workspace = $Receipt.workspace
        owner = $Receipt.owner
        dispatchOutcome = $Receipt.dispatchOutcome
        dispatchHeadEventHash = $Receipt.dispatchHeadEventHash
        recordedAtUtc = $Receipt.recordedAtUtc
        contextBundleHash = $Receipt.contextBundleHash
        bundleItemPaths = @($Receipt.bundleItemPaths)
        requiredCapabilities = @($Receipt.requiredCapabilities)
        helpfulPaths = @($Receipt.helpfulPaths)
        noisyPaths = @($Receipt.noisyPaths)
        missingPaths = @($Receipt.missingPaths)
        quality = $Receipt.quality
        reason = $Receipt.reason
    }
}
function Read-Dispatch([string]$Id) {
    if ($Id -notmatch '^[a-f0-9]{32}$') { throw 'DispatchId must be a 32-character lowercase hexadecimal identifier.' }
    $path = Join-Path $dispatchRoot "$Id.json"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Dispatch receipt does not exist: $Id" }
    Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.feedbackId -notmatch '^[a-f0-9]{32}$') { $issues.Add('feedbackId is invalid.') }
    if ([string]$Receipt.dispatchId -notmatch '^[a-f0-9]{32}$') { $issues.Add('dispatchId is invalid.') }
    if ([string]$Receipt.contextBundleHash -notmatch '^[a-f0-9]{64}$') { $issues.Add('contextBundleHash is invalid.') }
    if ([string]$Receipt.dispatchHeadEventHash -notmatch '^[a-f0-9]{64}$') { $issues.Add('dispatchHeadEventHash is invalid.') }
    if ([string]$Receipt.feedbackHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('feedbackHash is invalid.') }
    if ($null -eq $Receipt.quality -or [double]$Receipt.quality.score -lt 0 -or [double]$Receipt.quality.score -gt 100) { $issues.Add('quality score is invalid.') }
    $bundlePaths = @($Receipt.bundleItemPaths | Sort-Object -Unique)
    if (@($Receipt.helpfulPaths | Where-Object { $_ -and $_ -notin $bundlePaths }).Count -gt 0) { $issues.Add('helpfulPaths contains paths outside the dispatched bundle.') }
    if (@($Receipt.noisyPaths | Where-Object { $_ -and $_ -notin $bundlePaths }).Count -gt 0) { $issues.Add('noisyPaths contains paths outside the dispatched bundle.') }
    if (@($Receipt.missingPaths | Where-Object { $_ -and $_ -in $bundlePaths }).Count -gt 0) { $issues.Add('missingPaths contains paths already present in the bundle.') }
    if (@($Receipt.helpfulPaths | Where-Object { $_ -and $_ -in @($Receipt.noisyPaths) }).Count -gt 0) { $issues.Add('A path cannot be both helpful and noisy.') }
    $dispatchPath = Join-Path $dispatchRoot "$($Receipt.dispatchId).json"
    if (Test-Path -LiteralPath $dispatchPath -PathType Leaf) {
        try {
        $dispatch = Get-Content -LiteralPath $dispatchPath -Raw | ConvertFrom-Json
        $terminal = @($dispatch.events | Where-Object type -in @('completed', 'failed'))
        if ($terminal.Count -ne 1 -or [string]$dispatch.events[-1].type -notin @('completed', 'failed')) { $issues.Add('Linked dispatch is not terminal.') }
        if ([string]$dispatch.workspace -cne [string]$Receipt.workspace) { $issues.Add('Dispatch workspace does not match.') }
        if ([string]$dispatch.owner -cne [string]$Receipt.owner) { $issues.Add('Dispatch owner does not match.') }
        if ([string]$dispatch.contextBundleHash -cne [string]$Receipt.contextBundleHash) { $issues.Add('Dispatch context bundle hash does not match.') }
        if ((Get-Hash @($dispatch.requiredCapabilities)) -cne (Get-Hash @($Receipt.requiredCapabilities))) { $issues.Add('Dispatch capabilities do not match.') }
        if ($terminal.Count -eq 1 -and [string]$Receipt.dispatchOutcome -cne [string]$terminal[0].type) { $issues.Add('Dispatch outcome does not match.') }
        if ([string]$dispatch.events[-1].eventHash -cne [string]$Receipt.dispatchHeadEventHash) { $issues.Add('Dispatch terminal event hash does not match.') }
        } catch { $issues.Add($_.Exception.Message) }
    }
    [pscustomobject][ordered]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}
function Get-FeedbackFiles {
    if (-not (Test-Path -LiteralPath $feedbackRoot -PathType Container)) { return @() }
    @(Get-ChildItem -LiteralPath $feedbackRoot -File -Filter '*.json' | Sort-Object Name)
}
function Get-Percent([int]$Resolved, [int]$Total) {
    if ($Total -eq 0) { return 100 }
    [Math]::Round(100.0 * $Resolved / $Total, 2)
}
function Get-QualitySnapshot([object]$Dispatch, [string]$Outcome) {
    if ($Outcome -eq 'failed') {
        return [pscustomobject][ordered]@{ score = 0; verification = 0; acceptance = 0; reviews = 0; completion = 0; measured = $true }
    }
    $workspace = Join-Path $repositoryRoot ([string]$Dispatch.workspace)
    $evidence = Get-Content -LiteralPath (Join-Path $workspace 'evidence.json') -Raw | ConvertFrom-Json
    $acceptance = Get-Content -LiteralPath (Join-Path $workspace 'acceptance-matrix.json') -Raw | ConvertFrom-Json
    $checks = @($evidence.checks)
    $reviews = @($evidence.reviews)
    $criteria = @($acceptance.criteria)
    $verificationScore = Get-Percent (@($checks | Where-Object status -in @('passed', 'passed-with-known-baseline-failures', 'not-applicable')).Count) $checks.Count
    $reviewScore = Get-Percent (@($reviews | Where-Object status -in @('completed', 'not-applicable')).Count) $reviews.Count
    $acceptanceScore = Get-Percent (@($criteria | Where-Object status -in @('satisfied', 'not-applicable')).Count) $criteria.Count
    $completionScore = if (Test-Path -LiteralPath (Join-Path $workspace 'completion.json') -PathType Leaf) { 100 } else { 0 }
    [pscustomobject][ordered]@{
        score = [Math]::Round(($verificationScore * 0.45) + ($acceptanceScore * 0.25) + ($reviewScore * 0.15) + ($completionScore * 0.15), 2)
        verification = $verificationScore
        acceptance = $acceptanceScore
        reviews = $reviewScore
        completion = $completionScore
        measured = $true
    }
}
function Get-Views {
    @((Get-FeedbackFiles) | Where-Object { $_ -is [IO.FileInfo] } | ForEach-Object {
        $feedbackFilePath = $_.FullName
        try {
            $receipt = Get-Content -LiteralPath $feedbackFilePath -Raw | ConvertFrom-Json
            $validation = Test-Receipt $receipt
            [pscustomobject][ordered]@{ receipt = $receipt; valid = $validation.valid; issues = @($validation.issues); path = $feedbackFilePath }
        } catch {
            [pscustomobject][ordered]@{ receipt = $null; valid = $false; issues = @($_.Exception.Message); path = $feedbackFilePath }
        }
    })
}
function Get-Metrics([object[]]$Views) {
    $validReceipts = @($Views | Where-Object valid | ForEach-Object receipt)
    $adjustmentResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiQualityAdjustment.ps1') metrics -Format Json | ConvertFrom-Json
    $adjustmentByDispatch = @{}
    foreach ($profile in @($adjustmentResult.metrics.dispatchProfiles)) {
        $adjustmentByDispatch[[string]$profile.dispatchId] = [int]$profile.totalDelta
    }
    $qualitySamples = @($validReceipts | ForEach-Object {
        $baseScore = [double]$_.quality.score
        $delta = if ($adjustmentByDispatch.ContainsKey([string]$_.dispatchId)) { [int]$adjustmentByDispatch[[string]$_.dispatchId] } else { 0 }
        [pscustomobject]@{
            receipt = $_
            baseScore = $baseScore
            adjustment = $delta
            adjustedScore = [Math]::Max(0, [Math]::Min(100, $baseScore + $delta))
        }
    })
    $paths = @(
        $validReceipts | ForEach-Object {
            foreach ($propertyName in @('helpfulPaths', 'noisyPaths', 'missingPaths')) {
                if ($_.PSObject.Properties[$propertyName]) { @($_.$propertyName) }
            }
        } | Where-Object { $_ } | Sort-Object -Unique
    )
    $profiles = @($paths | ForEach-Object {
        $path = [string]$_
        $helpful = @($validReceipts | Where-Object { $path -in @($_.helpfulPaths) }).Count
        $noisy = @($validReceipts | Where-Object { $path -in @($_.noisyPaths) }).Count
        $missing = @($validReceipts | Where-Object { $path -in @($_.missingPaths) }).Count
        $samples = $helpful + $noisy + $missing
        $raw = $helpful * [int]$feedbackPolicy.helpfulWeight +
            $noisy * [int]$feedbackPolicy.noisyWeight +
            $missing * [int]$feedbackPolicy.missingWeight
        $adjustment = if ($samples -ge [int]$feedbackPolicy.minimumPathSamples) {
            [Math]::Max(-[int]$feedbackPolicy.maximumAbsoluteAdjustment, [Math]::Min([int]$feedbackPolicy.maximumAbsoluteAdjustment, $raw))
        } else { 0 }
        [pscustomobject][ordered]@{
            path = $path
            sampleCount = $samples
            helpfulCount = $helpful
            noisyCount = $noisy
            missingCount = $missing
            adjustment = $adjustment
            eligible = $samples -ge [int]$feedbackPolicy.minimumPathSamples
        }
    } | Sort-Object @{Expression='adjustment';Descending=$true}, path)
    $fingerprintInputs = @($validReceipts | Sort-Object feedbackId | ForEach-Object { "$($_.feedbackId)|$($_.feedbackHash)" })
    $ownerQualityProfiles = @($qualitySamples | Group-Object { $_.receipt.owner } | ForEach-Object {
        $receipts = @($_.Group)
        [pscustomobject][ordered]@{
            owner = $_.Name
            sampleCount = $receipts.Count
            baseAverageQualityScore = [Math]::Round((@($receipts.baseScore) | Measure-Object -Average).Average, 2)
            averageAdjustment = [Math]::Round((@($receipts.adjustment) | Measure-Object -Average).Average, 2)
            averageQualityScore = [Math]::Round((@($receipts.adjustedScore) | Measure-Object -Average).Average, 2)
        }
    } | Sort-Object owner)
    $capabilityQualityProfiles = @($qualitySamples | ForEach-Object {
        $sample = $_
        foreach ($capability in @($sample.receipt.requiredCapabilities)) {
            [pscustomobject]@{
                owner = $sample.receipt.owner
                capability = $capability
                baseScore = $sample.baseScore
                adjustment = $sample.adjustment
                adjustedScore = $sample.adjustedScore
            }
        }
    } | Group-Object { "$($_.owner)`n$($_.capability)" } | ForEach-Object {
        [pscustomobject][ordered]@{
            owner = [string]$_.Group[0].owner
            capability = [string]$_.Group[0].capability
            sampleCount = $_.Count
            baseAverageQualityScore = [Math]::Round((@($_.Group.baseScore) | Measure-Object -Average).Average, 2)
            averageAdjustment = [Math]::Round((@($_.Group.adjustment) | Measure-Object -Average).Average, 2)
            averageQualityScore = [Math]::Round((@($_.Group.adjustedScore) | Measure-Object -Average).Average, 2)
        }
    } | Sort-Object owner, capability)
    [pscustomobject][ordered]@{
        schemaVersion = 1
        validReceiptCount = $validReceipts.Count
        invalidReceiptCount = @($Views | Where-Object { $null -ne $_ -and -not $_.valid }).Count
        feedbackFingerprint = Get-Hash $fingerprintInputs
        qualityAdjustmentFingerprint = $adjustmentResult.metrics.fingerprint
        validQualityAdjustmentCount = [int]$adjustmentResult.metrics.validReceiptCount
        invalidQualityAdjustmentCount = [int]$adjustmentResult.metrics.invalidReceiptCount
        qualityAdjustmentProfiles = @($adjustmentResult.metrics.dispatchProfiles)
        profiles = $profiles
        ownerQualityProfiles = $ownerQualityProfiles
        capabilityQualityProfiles = $capabilityQualityProfiles
    }
}

if ($Action -eq 'record') {
    $dispatch = Read-Dispatch $DispatchId
    if ([string]::IsNullOrWhiteSpace($Owner) -or [string]$dispatch.owner -cne $Owner) { throw 'Owner must match the dispatch owner.' }
    $terminal = @($dispatch.events | Where-Object type -in @('completed', 'failed'))
    if ($terminal.Count -ne 1 -or [string]$dispatch.events[-1].type -notin @('completed', 'failed')) { throw 'Context feedback requires a terminal dispatch.' }
    if ([string]::IsNullOrWhiteSpace([string]$dispatch.contextBundleHash)) { throw 'Dispatch has no context bundle lineage.' }
    $feedbackPath = Join-Path $feedbackRoot "$DispatchId.json"
    if (Test-Path -LiteralPath $feedbackPath) { throw "Context feedback already exists for dispatch: $DispatchId" }
    $bundlePath = Join-Path $repositoryRoot ([string]$dispatch.contextBundlePath)
    if (-not (Test-Path -LiteralPath $bundlePath -PathType Leaf)) { throw 'Dispatched context bundle is absent.' }
    $bundle = Get-Content -LiteralPath $bundlePath -Raw | ConvertFrom-Json
    if ([string]$bundle.bundleHash -cne [string]$dispatch.contextBundleHash) { throw 'Current context bundle does not match the dispatched bundle.' }
    $helpful = @(Normalize-PathList $HelpfulPath)
    $noisy = @(Normalize-PathList $NoisyPath)
    $missing = @(Normalize-PathList $MissingPath)
    if ($helpful.Count + $noisy.Count + $missing.Count -eq 0) { throw 'Record at least one helpful, noisy, or missing path.' }
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        feedbackId = [guid]::NewGuid().ToString('N')
        dispatchId = [string]$dispatch.dispatchId
        workspace = [string]$dispatch.workspace
        owner = [string]$dispatch.owner
        dispatchOutcome = [string]$terminal[0].type
        dispatchHeadEventHash = [string]$dispatch.events[-1].eventHash
        recordedAtUtc = $now.ToString('o')
        contextBundleHash = [string]$dispatch.contextBundleHash
        bundleItemPaths = @($bundle.items.path | Sort-Object -Unique)
        requiredCapabilities = @($dispatch.requiredCapabilities)
        helpfulPaths = $helpful
        noisyPaths = $noisy
        missingPaths = $missing
        quality = Get-QualitySnapshot $dispatch ([string]$terminal[0].type)
        reason = [string]$Reason
        feedbackHash = ''
    }
    $receipt.feedbackHash = Get-Hash (Get-Payload $receipt)
    $validation = Test-Receipt $receipt
    if (-not $validation.valid) { throw "Context feedback is invalid: $(@($validation.issues) -join ' ')" }
    if (-not (Test-Path -LiteralPath $feedbackRoot)) { New-Item -ItemType Directory -Path $feedbackRoot | Out-Null }
    $temporary = Join-Path $feedbackRoot ('.context-feedback-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        [IO.File]::WriteAllText($temporary, (($receipt | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporary -Destination $feedbackPath
    } finally { if (Test-Path -LiteralPath $temporary) { [IO.File]::Delete($temporary) } }
    $result = [pscustomobject][ordered]@{ action = 'record'; valid = $true; feedback = $receipt; path = $feedbackPath.Substring($repositoryRoot.Length + 1).Replace('\', '/') }
} elseif ($Action -eq 'verify') {
    if ([string]::IsNullOrWhiteSpace($DispatchId)) { throw 'verify requires DispatchId.' }
    $file = Join-Path $feedbackRoot "$DispatchId.json"
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Context feedback does not exist: $DispatchId" }
    $receipt = Get-Content -LiteralPath $file -Raw | ConvertFrom-Json
    $validation = Test-Receipt $receipt
    $result = [pscustomobject][ordered]@{ action = 'verify'; valid = $validation.valid; issues = @($validation.issues); feedback = $receipt }
} elseif ($Action -eq 'prune') {
    $views = @(Get-Views)
    $protected = @($views | Where-Object { -not $_.valid })
    $candidates = @($views | Where-Object valid | Sort-Object { [DateTime]$_.receipt.recordedAtUtc } -Descending | Select-Object -Skip ([int]$feedbackPolicy.retentionCount))
    if ($Apply) { foreach ($candidate in $candidates) { [IO.File]::Delete([string]$candidate.path) } }
    $result = [pscustomobject][ordered]@{ action = 'prune'; valid = $protected.Count -eq 0; apply = [bool]$Apply; candidateCount = $candidates.Count; changedCount = $(if ($Apply) { $candidates.Count } else { 0 }); protectedInvalidCount = $protected.Count }
} else {
    $views = @(Get-Views)
    $metrics = Get-Metrics $views
    if ($Action -eq 'metrics') {
        $result = [pscustomobject][ordered]@{ action = 'metrics'; valid = $metrics.invalidReceiptCount -eq 0 -and $metrics.invalidQualityAdjustmentCount -eq 0; metrics = $metrics }
    } else {
        $result = [pscustomobject][ordered]@{
            action = 'list'
            valid = $metrics.invalidReceiptCount -eq 0 -and $metrics.invalidQualityAdjustmentCount -eq 0
            totalCount = $views.Count
            invalidCount = $metrics.invalidReceiptCount
            feedback = @($views | ForEach-Object {
                [pscustomobject][ordered]@{
                    feedbackId = [string]$_.receipt.feedbackId
                    dispatchId = [string]$_.receipt.dispatchId
                    workspace = [string]$_.receipt.workspace
                    outcome = [string]$_.receipt.dispatchOutcome
                    valid = $_.valid
                    issues = @($_.issues)
                }
            })
        }
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    Write-Host "Context feedback: action=$($result.action), valid=$($result.valid)"
    if ($result.PSObject.Properties['metrics']) { Write-Host "Receipts=$($result.metrics.validReceiptCount), profiles=$(@($result.metrics.profiles).Count), fingerprint=$($result.metrics.feedbackFingerprint)" }
    if ($result.PSObject.Properties['totalCount']) { Write-Host "Receipts=$($result.totalCount), invalid=$($result.invalidCount)" }
    if ($result.PSObject.Properties['issues']) { foreach ($issue in @($result.issues | Where-Object { $_ })) { Write-Host " - $issue" } }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
