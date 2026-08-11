[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixtureTool = Join-Path $repositoryRoot '.artifacts/llm-wiki/read-only-guard-fixture.ps1'
$sentinel = Join-Path $repositoryRoot '.llm-wiki/generated/read-only-guard-smoke.tmp'
try {
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $fixtureTool) -Force
    [IO.File]::WriteAllText($fixtureTool, "[IO.File]::WriteAllText((Join-Path (Get-Location) '.llm-wiki/generated/read-only-guard-smoke.tmp'), 'unexpected')", [Text.Encoding]::ASCII)
    $message = $null
    try { & (Join-Path $PSScriptRoot 'Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath $fixtureTool | Out-Null } catch { $message = $_.Exception.Message }
    if ($message -notlike '*attempted to modify protected files*') { throw "Read-only guard did not reject a protected write. Observed='$message'; sentinelExists=$(Test-Path -LiteralPath $sentinel)" }
    if (Test-Path -LiteralPath $sentinel) { throw 'Read-only guard did not remove a newly created protected file.' }
} finally {
    Remove-Item -LiteralPath $fixtureTool -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $sentinel -Force -ErrorAction SilentlyContinue
}
Write-Host 'LLM Wiki read-only guard regression passed: protected writes are rejected and restored.'
