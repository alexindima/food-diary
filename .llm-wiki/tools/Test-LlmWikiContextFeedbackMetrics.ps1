[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'context-feedback'
$previousRoot = $env:LLM_WIKI_TEST_CONTEXT_FEEDBACK_ROOT

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 30 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function New-Receipt([string]$Id, [string[]]$Helpful, [string[]]$Noisy, [string[]]$Missing) {
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1; feedbackId = $Id; dispatchId = $Id; workspace = '.artifacts/llm-wiki/tasks/fixture'; owner = 'fixture'
        dispatchOutcome = 'completed'; dispatchHeadEventHash = ('a' * 64); recordedAtUtc = '2026-01-01T00:00:00.0000000Z'
        contextBundleHash = ('b' * 64); bundleItemPaths = @($Helpful + $Noisy); requiredCapabilities = @('backend')
        helpfulPaths = @($Helpful); noisyPaths = @($Noisy); missingPaths = @($Missing)
        quality = [pscustomobject]@{ score = 100; verification = 100; acceptance = 100; reviews = 100; completion = 100; measured = $true }
        reason = 'fixture'; feedbackHash = ''
    }
    # Hash the JSON-round-tripped shape because that is exactly what the production validator reads.
    # PowerShell editions can otherwise retain different in-memory collection metadata before serialization.
    $normalized = $receipt | ConvertTo-Json -Depth 30 | ConvertFrom-Json
    $payload = [pscustomobject][ordered]@{
        schemaVersion = $normalized.schemaVersion
        feedbackId = $normalized.feedbackId
        dispatchId = $normalized.dispatchId
        workspace = $normalized.workspace
        owner = $normalized.owner
        dispatchOutcome = $normalized.dispatchOutcome
        dispatchHeadEventHash = $normalized.dispatchHeadEventHash
        recordedAtUtc = $normalized.recordedAtUtc
        contextBundleHash = $normalized.contextBundleHash
        bundleItemPaths = @($normalized.bundleItemPaths)
        requiredCapabilities = @($normalized.requiredCapabilities)
        helpfulPaths = @($normalized.helpfulPaths)
        noisyPaths = @($normalized.noisyPaths)
        missingPaths = @($normalized.missingPaths)
        quality = $normalized.quality
        reason = $normalized.reason
    }
    $receipt.feedbackHash = Get-Hash $payload
    $receipt
}

try {
    $env:LLM_WIKI_TEST_CONTEXT_FEEDBACK_ROOT = $fixtureRoot
    $empty = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextFeedback.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $empty.valid -or $empty.metrics.validReceiptCount -ne 0 -or @($empty.metrics.profiles).Count -ne 0) { throw 'Empty feedback metrics are invalid.' }

    $null = New-Item -ItemType Directory -Path $fixtureRoot -Force
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'invalid.json'), '{}', [Text.UTF8Encoding]::new($false))
    $invalid = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextFeedback.ps1') metrics -Format Json | ConvertFrom-Json
    if ($invalid.valid -or $invalid.metrics.invalidReceiptCount -ne 1) { throw 'Invalid-only feedback metrics were not reported safely.' }
    Remove-Item -LiteralPath (Join-Path $fixtureRoot 'invalid.json') -Force

    $first = New-Receipt ('1' * 32) @('src/helpful.cs') @() @('src/missing.cs')
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'one.json'), (($first | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $one = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextFeedback.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $one.valid -or $one.metrics.validReceiptCount -ne 1 -or @($one.metrics.profiles).Count -ne 2) {
        $diagnostic = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextFeedback.ps1') list -Format Json | ConvertFrom-Json
        throw "Single feedback receipt metrics are invalid: $($one | ConvertTo-Json -Depth 10 -Compress); receipts=$($diagnostic | ConvertTo-Json -Depth 10 -Compress)"
    }

    $second = New-Receipt ('2' * 32) @('src/helpful.cs') @('src/noisy.cs') @()
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'two.json'), (($second | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $many = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextFeedback.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $many.valid -or $many.metrics.validReceiptCount -ne 2 -or @($many.metrics.profiles).Count -ne 3) { throw 'Multiple feedback receipt metrics are invalid.' }
} finally {
    $env:LLM_WIKI_TEST_CONTEXT_FEEDBACK_ROOT = $previousRoot
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki context-feedback metrics regression passed: empty, invalid, single, and multiple receipt sets are safe.'
