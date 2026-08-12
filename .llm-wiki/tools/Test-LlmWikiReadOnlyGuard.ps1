[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixtureTool = Join-Path $repositoryRoot '.artifacts/llm-wiki/read-only-guard-fixture.ps1'
$nestedFixtureTool = Join-Path $repositoryRoot '.artifacts/llm-wiki/read-only-guard-nested-fixture.ps1'
$sentinel = Join-Path $repositoryRoot '.llm-wiki/generated/read-only-guard-smoke.tmp'
$worktreeSentinel = Join-Path $repositoryRoot 'read-only-guard-worktree-smoke.tmp'
try {
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $fixtureTool) -Force
    [IO.File]::WriteAllText($worktreeSentinel, 'original', [Text.Encoding]::ASCII)
    [IO.File]::WriteAllText($fixtureTool, "[IO.File]::WriteAllText((Join-Path (Get-Location) '.llm-wiki/generated/read-only-guard-smoke.tmp'), 'unexpected'); [IO.File]::WriteAllText((Join-Path (Get-Location) 'read-only-guard-worktree-smoke.tmp'), 'mutated')", [Text.Encoding]::ASCII)
    $guardPath = Join-Path $PSScriptRoot 'Invoke-LlmWikiReadOnlyTool.ps1'
    [IO.File]::WriteAllText($nestedFixtureTool, "& '$($guardPath.Replace("'", "''"))' -ToolPath '$($fixtureTool.Replace("'", "''"))'", [Text.Encoding]::ASCII)
    $message = $null
    try { & $guardPath -ToolPath $nestedFixtureTool | Out-Null } catch { $message = $_.Exception.Message }
    if ($message -notlike '*attempted to modify protected files*') { throw "Read-only guard did not reject a protected write. Observed='$message'; sentinelExists=$(Test-Path -LiteralPath $sentinel)" }
    if (Test-Path -LiteralPath $sentinel) { throw 'Read-only guard did not remove a newly created protected file.' }
    if ((Get-Content -LiteralPath $worktreeSentinel -Raw) -cne 'original') { throw 'Read-only guard did not restore a pre-existing dirty worktree file.' }
} finally {
    Remove-Item -LiteralPath $fixtureTool -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $nestedFixtureTool -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $sentinel -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $worktreeSentinel -Force -ErrorAction SilentlyContinue
}
Write-Host 'LLM Wiki read-only guard regression passed: protected writes and pre-existing worktree changes are rejected and restored.'
