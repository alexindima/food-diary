[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$canonicalPath = Join-Path $wikiRoot 'knowledge/memories.json'
$canonicalHash = (Get-FileHash -LiteralPath $canonicalPath -Algorithm SHA256).Hash
$canonicalRegistry = Get-Content -LiteralPath $canonicalPath -Raw | ConvertFrom-Json
$smokeEntries = @($canonicalRegistry.events | Where-Object { [string]$_.id -like 'smoke-*' })
if ($smokeEntries.Count -gt 0) {
    throw "Canonical durable-memory registry contains $($smokeEntries.Count) smoke test artifact(s)."
}

$sandboxDirectory = Join-Path $repositoryRoot '.artifacts/llm-wiki/memory-isolation-smoke'
$sandboxPath = Join-Path $sandboxDirectory 'memories.json'
$previousOverride = $env:LLM_WIKI_TEST_MEMORY_REGISTRY_PATH
try {
    New-Item -ItemType Directory -Path $sandboxDirectory -Force | Out-Null
    [IO.File]::WriteAllText(
        $sandboxPath,
        "{`n  `"schemaVersion`": 1,`n  `"events`": []`n}`n",
        [Text.UTF8Encoding]::new($false))
    $env:LLM_WIKI_TEST_MEMORY_REGISTRY_PATH = $sandboxPath
    $verification = & (Join-Path $PSScriptRoot 'Manage-LlmWikiMemory.ps1') verify -Format Json | ConvertFrom-Json
    if (-not $verification.valid -or $verification.totalCount -ne 0) {
        throw 'Isolated durable-memory registry did not validate as empty.'
    }

    $unsafeOverrideRejected = $false
    try {
        $env:LLM_WIKI_TEST_MEMORY_REGISTRY_PATH = $canonicalPath
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiMemory.ps1') verify | Out-Null
    } catch {
        $unsafeOverrideRejected = $_.Exception.Message -match 'must resolve under \.artifacts/llm-wiki'
    }
    if (-not $unsafeOverrideRejected) {
        throw 'Durable-memory registry accepted a test override outside the artifact sandbox.'
    }
} finally {
    if ([string]::IsNullOrWhiteSpace($previousOverride)) {
        Remove-Item Env:LLM_WIKI_TEST_MEMORY_REGISTRY_PATH -ErrorAction SilentlyContinue
    } else {
        $env:LLM_WIKI_TEST_MEMORY_REGISTRY_PATH = $previousOverride
    }
    if (Test-Path -LiteralPath $sandboxPath) { Remove-Item -LiteralPath $sandboxPath -Force }
    if (Test-Path -LiteralPath $sandboxDirectory) { Remove-Item -LiteralPath $sandboxDirectory -Force }
}

$canonicalHashAfter = (Get-FileHash -LiteralPath $canonicalPath -Algorithm SHA256).Hash
if ($canonicalHashAfter -cne $canonicalHash) {
    throw 'Durable-memory isolation smoke modified the canonical registry.'
}

Write-Host 'LLM Wiki durable-memory isolation smoke passed.'
