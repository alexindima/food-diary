[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'read-only-guard'
$mutationTool = Join-Path $fixtureRoot 'read-only-guard-mutation.ps1'
$safeTool = Join-Path $fixtureRoot 'read-only-guard-safe.ps1'
$signalPath = Join-Path $fixtureRoot 'concurrent-write.signal'
$protectedSentinel = Join-Path $repositoryRoot '.llm-wiki/generated/read-only-guard-smoke.tmp'
$dirtySentinel = Join-Path $repositoryRoot 'read-only-guard-worktree-smoke.tmp'
$cleanSourceRelative = 'Shared/FoodDiary.Results/Result.cs'
$cleanSource = Join-Path $repositoryRoot $cleanSourceRelative
$writerJob = $null

try {
    $null = New-Item -ItemType Directory -Path $fixtureRoot -Force
    [IO.File]::WriteAllText($dirtySentinel, 'original-dirty-content', [Text.Encoding]::ASCII)
    $cleanSourceHash = (Get-FileHash -LiteralPath $cleanSource -Algorithm SHA256).Hash
    $mutationScript = @'
param([Parameter(Mandatory)][string]$SignalPath)
$root = (Get-Location).Path
[IO.File]::WriteAllText($SignalPath, 'ready', [Text.Encoding]::ASCII)
Start-Sleep -Milliseconds 750
[IO.File]::WriteAllText((Join-Path $root '.llm-wiki/generated/read-only-guard-smoke.tmp'), 'unexpected', [Text.Encoding]::ASCII)
[IO.File]::WriteAllText((Join-Path $root 'read-only-guard-worktree-smoke.tmp'), 'snapshot-mutation', [Text.Encoding]::ASCII)
[IO.File]::AppendAllText((Join-Path $root 'Shared/FoodDiary.Results/Result.cs'), "`n// unexpected snapshot mutation`n", [Text.Encoding]::UTF8)
'@
    [IO.File]::WriteAllText($mutationTool, $mutationScript, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($safeTool, "Write-Output 'read-only-safe-control'", [Text.UTF8Encoding]::new($false))

    $writerJob = Start-Job -ScriptBlock {
        param([string]$Signal, [string]$Target)
        $deadline = [DateTime]::UtcNow.AddSeconds(120)
        while (-not (Test-Path -LiteralPath $Signal -PathType Leaf)) {
            if ([DateTime]::UtcNow -ge $deadline) { throw 'Timed out waiting for the isolated mutation tool.' }
            Start-Sleep -Milliseconds 50
        }
        [IO.File]::WriteAllText($Target, 'concurrent-writer-content', [Text.Encoding]::ASCII)
    } -ArgumentList $signalPath, $dirtySentinel

    $guardPath = Join-Path $PSScriptRoot 'Invoke-LlmWikiReadOnlyTool.ps1'
    $message = $null
    try {
        & $guardPath -ToolPath $mutationTool -ToolArguments @{ SignalPath = $signalPath } | Out-Null
    } catch {
        $message = $_.Exception.Message
    }
    Wait-Job -Job $writerJob -Timeout 125 | Out-Null
    Receive-Job -Job $writerJob -ErrorAction Stop | Out-Null
    if ($message -notlike '*modified its isolated snapshot*No source files were restored or overwritten*') {
        throw "Read-only guard did not reject isolated source mutation safely. Observed='$message'"
    }
    if ((Get-Content -LiteralPath $dirtySentinel -Raw) -cne 'concurrent-writer-content') {
        throw 'Read-only guard overwrote a concurrent change in the original dirty worktree file.'
    }
    if ((Get-FileHash -LiteralPath $cleanSource -Algorithm SHA256).Hash -cne $cleanSourceHash) {
        throw 'Read-only guard allowed an isolated tool to change an initially clean source file in the original worktree.'
    }
    if (Test-Path -LiteralPath $protectedSentinel) {
        throw 'Read-only guard allowed an isolated tool to create a protected file in the original worktree.'
    }

    $safeOutput = @(& $guardPath -ToolPath $safeTool)
    if ('read-only-safe-control' -notin $safeOutput) {
        throw 'Read-only guard did not preserve output from a legitimate read-only tool.'
    }

    $productOnlyPlan = & (Join-Path $PSScriptRoot 'Invoke-LlmWikiAffectedSmoke.ps1') `
        -ChangedPath 'FoodDiary.Application/Users/Example.cs' `
        -Plan `
        -Format Json | ConvertFrom-Json
    if ($productOnlyPlan.changedPathCount -ne 1 -or @($productOnlyPlan.groups).Count -ne 0) {
        throw 'Affected smoke plan did not return a stable empty-groups contract for a product-only delta.'
    }
    $emptyPlan = & (Join-Path $PSScriptRoot 'Invoke-LlmWikiAffectedSmoke.ps1') `
        -ChangedPath @() `
        -Plan `
        -Format Json | ConvertFrom-Json
    if ($emptyPlan.changedPathCount -ne 0 -or @($emptyPlan.groups).Count -ne 0) {
        throw 'Affected smoke plan did not return a stable empty-groups contract for an empty delta.'
    }
} finally {
    if ($writerJob) {
        Stop-Job -Job $writerJob -ErrorAction SilentlyContinue
        Remove-Job -Job $writerJob -Force -ErrorAction SilentlyContinue
    }
    Remove-Item -LiteralPath $protectedSentinel -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $dirtySentinel -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki read-only guard regression passed: isolated mutations are rejected, concurrent dirty-file writes survive, clean sources remain untouched, and safe output is preserved.'
