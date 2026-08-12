[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiIndexTiming.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixtureRoot = Join-Path $repositoryRoot ".artifacts/llm-wiki/index-timing-test-$([guid]::NewGuid().ToString('N'))"
try {
    $null = New-Item -ItemType Directory -Path $fixtureRoot -Force
    & git -C $fixtureRoot init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize index timing fixture.' }
    foreach ($duration in @(10, 20, 30, 40, 50, 60)) {
        Add-LlmWikiIndexTimings -RepositoryRoot $fixtureRoot -Mode update -Timings @(
            [pscustomobject]@{ tool = 'Build-Example.ps1'; durationSeconds = $duration }
        )
    }
    $stats = @(Get-LlmWikiIndexTimingStats -RepositoryRoot $fixtureRoot -Mode update)[0]
    if ($stats.sampleCount -ne 5) { throw "Expected five retained timing samples, got $($stats.sampleCount)." }
    if ($stats.medianSeconds -ne 40) { throw "Expected rolling median 40, got $($stats.medianSeconds)." }
} finally {
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host 'LLM Wiki index timing tests passed.'
