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
$cleanRepositoryRoot = Join-Path $fixtureRoot 'clean-repository'
$cleanSnapshotParent = $null
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

    $cleanToolsRoot = Join-Path $cleanRepositoryRoot '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $cleanToolsRoot -Force
    Copy-Item -LiteralPath $guardPath -Destination (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1') -Destination (Join-Path $cleanToolsRoot 'LlmWikiGitPaths.ps1') -Force
    $cleanSafeTool = Join-Path $cleanToolsRoot 'clean-safe.ps1'
    [IO.File]::WriteAllText($cleanSafeTool, "Write-Output 'read-only-clean-control'", [Text.UTF8Encoding]::new($false))
    & git -C $cleanRepositoryRoot init --quiet
    & git -C $cleanRepositoryRoot config user.email 'wiki-smoke@example.invalid'
    & git -C $cleanRepositoryRoot config user.name 'Wiki Smoke'
    & git -C $cleanRepositoryRoot add --all
    & git -C $cleanRepositoryRoot commit --quiet -m 'fixture'
    if ($LASTEXITCODE -ne 0) { throw 'Unable to prepare the clean read-only guard regression repository.' }

    $cleanOutput = @(& (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') `
        -ToolPath $cleanSafeTool `
        -ToolArguments @{ ProposedPath = @('CleanScope') })
    if ('read-only-clean-control' -notin $cleanOutput) {
        throw 'Read-only guard did not preserve output when the scoped workspace overlay was empty.'
    }

    $cleanRepositoryPathHasher = [Security.Cryptography.SHA256]::Create()
    try {
        $cleanRepositoryPathHash = $cleanRepositoryPathHasher.ComputeHash(
            [Text.Encoding]::UTF8.GetBytes(([IO.Path]::GetFullPath($cleanRepositoryRoot)).ToLowerInvariant()))
    } finally {
        $cleanRepositoryPathHasher.Dispose()
    }
    $cleanRepositorySnapshotKey = (
        ([BitConverter]::ToString($cleanRepositoryPathHash) -replace '-', '').ToLowerInvariant()
    ).Substring(0, 16)
    $snapshotTempRoot = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
        Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'Temp'
    } else {
        [IO.Path]::GetTempPath()
    }
    $cleanSnapshotParent = Join-Path $snapshotTempRoot "fooddiary-llm-wiki-read-only/$cleanRepositorySnapshotKey"
    $cleanReadyFile = Get-ChildItem -LiteralPath $cleanSnapshotParent -Filter '*.ready' -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if (-not $cleanReadyFile) { throw 'Read-only guard did not publish the clean snapshot readiness marker.' }
    $cleanSnapshotRoot = Join-Path $cleanSnapshotParent $cleanReadyFile.BaseName
    $cachedGuardPath = Join-Path $cleanSnapshotRoot '.llm-wiki/tools/Invoke-LlmWikiReadOnlyTool.ps1'
    Remove-Item -LiteralPath $cachedGuardPath -Force

    $recoveredOutput = @(& (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') `
        -ToolPath $cleanSafeTool `
        -ToolArguments @{ ProposedPath = @('CleanScope') })
    if ('read-only-clean-control' -notin $recoveredOutput -or
        -not (Test-Path -LiteralPath $cachedGuardPath -PathType Leaf)) {
        throw 'Read-only guard did not rebuild a cached snapshot whose required tooling was missing.'
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
    if ($cleanSnapshotParent) {
        Remove-Item -LiteralPath $cleanSnapshotParent -Recurse -Force -ErrorAction SilentlyContinue
    }
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki read-only guard regression passed: isolated mutations are rejected, concurrent dirty-file writes survive, clean sources remain untouched, corrupt caches recover, and safe output is preserved.'
