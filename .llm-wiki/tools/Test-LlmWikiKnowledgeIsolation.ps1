[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$wikiRoot = Join-Path $repositoryRoot '.llm-wiki'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$sandbox = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'knowledge-isolation'
$previous = $env:LLM_WIKI_TEST_KNOWLEDGE_ROOT
$registries = @('learning-promotions.json', 'learning-experiments.json', 'eval-promotions.json', 'learning-health.json')
try {
    New-Item -ItemType Directory -Path $sandbox -Force | Out-Null
    $canonicalHashes = @{}
    foreach ($name in $registries) {
        $canonical = Join-Path $wikiRoot "knowledge/$name"
        $canonicalHashes[$name] = (Get-FileHash -LiteralPath $canonical -Algorithm SHA256).Hash
        Copy-Item -LiteralPath $canonical -Destination (Join-Path $sandbox $name)
    }
    $env:LLM_WIKI_TEST_KNOWLEDGE_ROOT = $sandbox
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') list | Out-Null
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningExperiment.ps1') list | Out-Null
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvalPromotion.ps1') list | Out-Null
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningHealth.ps1') list | Out-Null
    foreach ($name in $registries) {
        $canonical = Join-Path $wikiRoot "knowledge/$name"
        if ((Get-FileHash -LiteralPath $canonical -Algorithm SHA256).Hash -cne $canonicalHashes[$name]) { throw "Canonical knowledge registry changed during isolated smoke: $name" }
    }
} finally {
    if ([string]::IsNullOrWhiteSpace($previous)) { Remove-Item Env:LLM_WIKI_TEST_KNOWLEDGE_ROOT -ErrorAction SilentlyContinue } else { $env:LLM_WIKI_TEST_KNOWLEDGE_ROOT = $previous }
    if (Test-Path -LiteralPath $sandbox) { Remove-Item -LiteralPath $sandbox -Recurse -Force -ErrorAction SilentlyContinue }
}
Write-Host 'LLM Wiki knowledge-isolation regression passed: mutable smoke registries stay under .artifacts.'
