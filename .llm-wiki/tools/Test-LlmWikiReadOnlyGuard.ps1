[CmdletBinding()]
param([switch]$Isolated)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (-not $Isolated) {
    $cloneParent = Join-Path $repositoryRoot '.artifacts/llm-wiki/read-only-guard-fixtures'
    $cloneRoot = Join-Path $cloneParent ([guid]::NewGuid().ToString('N'))
    $previousSnapshot = $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT
    $previousSource = $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT
    $previousSandbox = $env:LLM_WIKI_SMOKE_SANDBOX
    $hashAlgorithm = [Security.Cryptography.SHA256]::Create()
    try { $rootHash = ([BitConverter]::ToString($hashAlgorithm.ComputeHash([Text.Encoding]::UTF8.GetBytes(([IO.Path]::GetFullPath($cloneRoot)).ToLowerInvariant()))) -replace '-', '').ToLowerInvariant().Substring(0, 16) }
    finally { $hashAlgorithm.Dispose() }
    $snapshotTemp = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) { Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'Temp' } else { [IO.Path]::GetTempPath() }
    $snapshotParent = Join-Path $snapshotTemp 'fooddiary-llm-wiki-read-only'
    $cloneSnapshots = Join-Path $snapshotParent $rootHash
    try {
        $null = New-Item -ItemType Directory -Path $cloneParent -Force
        & git clone --shared --quiet $repositoryRoot $cloneRoot
        if ($LASTEXITCODE -ne 0) { throw 'Unable to clone the read-only mutation regression fixture.' }
        Get-ChildItem -LiteralPath (Join-Path $repositoryRoot '.llm-wiki') -Force |
            Copy-Item -Destination (Join-Path $cloneRoot '.llm-wiki') -Recurse -Force
        $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT = $null
        $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT = $null
        $env:LLM_WIKI_SMOKE_SANDBOX = Join-Path $cloneRoot '.artifacts/llm-wiki/smoke-private'
        & (Get-Process -Id $PID).Path -NoProfile -File (Join-Path $cloneRoot '.llm-wiki/tools/Test-LlmWikiReadOnlyGuard.ps1') -Isolated
        if ($LASTEXITCODE -ne 0) { throw 'Private read-only mutation regression failed.' }
    } finally {
        $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT = $previousSnapshot
        $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT = $previousSource
        $env:LLM_WIKI_SMOKE_SANDBOX = $previousSandbox
        foreach ($ownedPath in @(@{ Path = $cloneSnapshots; Parent = $snapshotParent }, @{ Path = $cloneRoot; Parent = $cloneParent })) {
            $resolved = [IO.Path]::GetFullPath($ownedPath.Path)
            $prefix = [IO.Path]::GetFullPath($ownedPath.Parent).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
            if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe read-only regression cleanup path.' }
            if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
        }
    }
    return
}
& (Join-Path $PSScriptRoot 'Test-LlmWikiReadOnlyOverlayPaths.ps1')
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
    $fakeGraphManagerPath = Join-Path $cleanToolsRoot 'Manage-LlmWikiCodeGraph.ps1'
    [IO.File]::WriteAllText(
        $fakeGraphManagerPath,
        "param([string]`$Action,[string]`$Format)`n`$marker=Join-Path (Resolve-Path (Join-Path `$PSScriptRoot '../..')).Path '.artifacts/llm-wiki/code-graph/prepared.marker'`n`$null=New-Item -ItemType Directory -Path (Split-Path -Parent `$marker) -Force`n[IO.File]::WriteAllText(`$marker,'prepared',[Text.Encoding]::ASCII)`n",
        [Text.UTF8Encoding]::new($false))
    $cleanSafeTool = Join-Path $cleanToolsRoot 'clean-safe.ps1'
    [IO.File]::WriteAllText($cleanSafeTool, "param([switch]`$Fail)`nif (`$Fail) { exit 7 }`nWrite-Output 'read-only-clean-control'", [Text.UTF8Encoding]::new($false))
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

    $freshRunner = Join-Path $fixtureRoot 'fresh-guard.ps1'
    [IO.File]::WriteAllText($freshRunner, @'
param([string]$Guard, [string]$Tool, [switch]$StaleExitCode)
if ($StaleExitCode) { $global:LASTEXITCODE = 17 }
& $Guard -ToolPath $Tool -ToolArguments @{ ProposedPath = @('CleanScope') }
'@, [Text.UTF8Encoding]::new($false))
    $freshShell = (Get-Process -Id $PID).Path
    foreach ($staleExitCode in @($false, $true)) {
        $freshArguments = @('-NoProfile', '-File', $freshRunner, '-Guard', (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1'), '-Tool', $cleanSafeTool)
        if ($staleExitCode) { $freshArguments += '-StaleExitCode' }
        $freshOutput = @(& $freshShell @freshArguments)
        if ($LASTEXITCODE -ne 0 -or 'read-only-clean-control' -notin $freshOutput) {
            throw 'Warm read-only snapshot inherited an absent or stale native exit code in a fresh process.'
        }
    }
    $exitFailure = $null
    try {
        & (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath $cleanSafeTool -ToolArguments @{ ProposedPath = @('CleanScope'); Fail = $true }
    } catch { $exitFailure = $_.Exception.Message }
    if ($exitFailure -notlike '*failed with exit code 7*') {
        throw "Read-only guard lost the actual tool exit code: '$exitFailure'."
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
    $cachedManagerPath = Join-Path $cleanSnapshotRoot '.llm-wiki/tools/Manage-LlmWikiCodeGraph.ps1'
    $cachedManagerLf = [IO.File]::ReadAllText($cachedManagerPath).Replace("`r`n", "`n").Replace("`r", "`n")
    [IO.File]::WriteAllText($cachedManagerPath, $cachedManagerLf, [Text.UTF8Encoding]::new($false))
    $reuseMarkerPath = Join-Path $cleanSnapshotRoot 'line-ending-cache-reuse.marker'
    [IO.File]::WriteAllText($reuseMarkerPath, 'preserve-on-reuse', [Text.Encoding]::ASCII)
    $lineEndingReuseOutput = @(& (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') `
        -ToolPath $cleanSafeTool `
        -ToolArguments @{ ProposedPath = @('CleanScope') })
    if ('read-only-clean-control' -notin $lineEndingReuseOutput -or
        -not (Test-Path -LiteralPath $reuseMarkerPath -PathType Leaf)) {
        throw 'Read-only guard rebuilt a valid cached snapshot solely because Git normalized text-file line endings.'
    }

    Remove-Item -LiteralPath $cachedGuardPath -Force

    $recoveredOutput = @(& (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') `
        -ToolPath $cleanSafeTool `
        -ToolArguments @{ ProposedPath = @('CleanScope') })
    if ('read-only-clean-control' -notin $recoveredOutput -or
        -not (Test-Path -LiteralPath $cachedGuardPath -PathType Leaf)) {
        throw 'Read-only guard did not rebuild a cached snapshot whose required tooling was missing.'
    }

    $preparedSafeTool = Join-Path $cleanToolsRoot 'prepared-safe.ps1'
    [IO.File]::WriteAllText(
        $preparedSafeTool,
        "`$marker=Join-Path (Get-Location).Path '.artifacts/llm-wiki/code-graph/prepared.marker'`nif(-not (Test-Path -LiteralPath `$marker -PathType Leaf)){throw 'Code graph was not prepared inside the snapshot.'}`nWrite-Output 'read-only-prepared-control'`n",
        [Text.UTF8Encoding]::new($false))
    $preparedOutput = @(& (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') `
        -ToolPath $preparedSafeTool `
        -ToolArguments @{ ProposedPath = @('CleanScope') } `
        -PrepareCodeGraph)
    if ('read-only-prepared-control' -notin $preparedOutput -or (Test-Path -LiteralPath (Join-Path $cleanRepositoryRoot '.artifacts/llm-wiki/code-graph/prepared.marker'))) {
        throw 'Read-only guard did not prepare the code graph exclusively inside its stable snapshot.'
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
