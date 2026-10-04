[CmdletBinding()]
param([switch]$Isolated)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
# Exercise the exact guard helpers without paying for a whole repository clone.
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$guardAst = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Invoke-LlmWikiReadOnlyTool.ps1'), [ref]$null, [ref]$null)
foreach ($name in @('Get-FileHashOrMissing', 'Get-GuardState', 'Compare-GuardState')) {
    $definition = $guardAst.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $true) | Select-Object -First 1
    if ($null -eq $definition) { throw "Missing guard helper: $name" }
    Invoke-Expression $definition.Extent.Text
}
$statusFixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'guard-status-paths'
try {
    & git -C $statusFixture init --quiet
    $oldName = Join-Path $statusFixture 'старое имя.txt'
    $newName = Join-Path $statusFixture 'новое имя.txt'
    [IO.File]::WriteAllText($oldName, 'baseline')
    & git -C $statusFixture add .
    & git -C $statusFixture -c user.name='Wiki Tests' -c user.email='wiki@example.invalid' commit --quiet -m baseline
    Move-Item -LiteralPath $oldName -Destination $newName
    & git -C $statusFixture add -A
    $before = Get-GuardState -RepositoryRoot $statusFixture
    if (-not $before.hashes.Contains('старое имя.txt') -or -not $before.hashes.Contains('новое имя.txt')) { throw 'Guard lost a Unicode rename path.' }
    [IO.File]::WriteAllText($newName, 'modified')
    $dirtyBefore = Get-GuardState -RepositoryRoot $statusFixture
    [IO.File]::WriteAllText($newName, 'mutated!')
    $dirtyAfter = Get-GuardState -RepositoryRoot $statusFixture
    if (($dirtyBefore.status -join [char]0) -cne ($dirtyAfter.status -join [char]0)) { throw 'Guard regression did not preserve dirty status.' }
    if ('новое имя.txt' -notin @(Compare-GuardState -RepositoryRoot $statusFixture -Before $dirtyBefore -After $dirtyAfter)) { throw 'Guard missed dirty content behind stable Unicode status.' }
} finally {
    $statusFixturePrefix = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot)).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not [IO.Path]::GetFullPath($statusFixture).StartsWith($statusFixturePrefix, [StringComparison]::Ordinal)) { throw 'Unsafe guard status fixture cleanup path.' }
    Remove-Item -LiteralPath $statusFixture -Recurse -Force
}
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

    # Changing an existing overlay leaves its porcelain status unchanged.
    $dirtyOnlyTool = Join-Path $fixtureRoot 'read-only-dirty-overlay-mutation.ps1'
    [IO.File]::WriteAllText($dirtyOnlyTool, @'
[IO.File]::WriteAllText((Join-Path (Get-Location) 'read-only-guard-worktree-smoke.tmp'), 'corrupted-overlay')
'@, [Text.UTF8Encoding]::new($false))
    $dirtyOnlyRejected = $false
    try { & $guardPath -ToolPath $dirtyOnlyTool | Out-Null }
    catch { $dirtyOnlyRejected = $_.Exception.Message -like '*modified its isolated snapshot*' }
    if (-not $dirtyOnlyRejected) { throw 'Read-only guard accepted a mutation with unchanged Git status.' }
    [IO.File]::WriteAllText($safeTool, @'
Get-Content (Join-Path (Get-Location) 'read-only-guard-worktree-smoke.tmp') -Raw
'@, [Text.UTF8Encoding]::new($false))
    $recoveredOverlay = & $guardPath -ToolPath $safeTool
    if ($recoveredOverlay -cne 'concurrent-writer-content') { throw 'A rejected dirty mutation poisoned the next snapshot reader.' }
    if ([IO.File]::ReadAllText($dirtySentinel) -cne 'concurrent-writer-content') { throw 'Dirty overlay validation changed the source worktree.' }

    $cleanToolsRoot = Join-Path $cleanRepositoryRoot '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $cleanToolsRoot -Force
    Copy-Item -LiteralPath $guardPath -Destination (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1') -Destination (Join-Path $cleanToolsRoot 'LlmWikiGitPaths.ps1') -Force
    $fakeGraphManagerPath = Join-Path $cleanToolsRoot 'Manage-LlmWikiCodeGraph.ps1'
    $fakeGraphManagerSource = @'
param([string]$Action,[string]$Format,[switch]$SkipRefresh,[switch]$BackendOnlyRefresh)
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$directory = Join-Path $root '.artifacts/llm-wiki/code-graph'
$marker = Join-Path $directory 'prepared.marker'
$stale = Join-Path $directory 'force-stale.marker'
$unavailable = Join-Path $directory 'force-status-failure.marker'
if ($Action -eq 'status') {
    if (Test-Path $unavailable) { throw 'Fixture database status is unavailable.' }
    @{changeSetFresh=(-not (Test-Path $stale));searchDocuments=1;typescriptProjectionComplete=$true} | ConvertTo-Json -Compress
    return
}
if ($Action -ne 'build') { throw 'Unexpected fixture graph action.' }
$null = New-Item -ItemType Directory -Path $directory -Force
$count = if (Test-Path $marker) { [int][IO.File]::ReadAllText($marker) } else { 0 }
[IO.File]::WriteAllText($marker,[string]($count+1),[Text.Encoding]::ASCII)
[IO.File]::WriteAllText((Join-Path $directory 'code-graph.sqlite'),'fixture',[Text.Encoding]::ASCII)
Remove-Item -LiteralPath $stale -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $unavailable -ErrorAction SilentlyContinue

'@
    [IO.File]::WriteAllText($fakeGraphManagerPath,$fakeGraphManagerSource,[Text.UTF8Encoding]::new($false))
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

    $reuseIdentity = Join-Path $cleanSnapshotRoot '.git/checkout-reuse.marker'
    [IO.File]::WriteAllText($reuseIdentity, 'same-clone', [Text.Encoding]::ASCII)
    $scopeDirectory = Join-Path $cleanRepositoryRoot 'CleanScope'
    $null = New-Item -ItemType Directory -Path $scopeDirectory -Force
    $overlayFile = Join-Path $scopeDirectory 'overlay.txt'
    foreach ($content in @('first edit', 'second edit')) {
        [IO.File]::WriteAllText($overlayFile, $content, [Text.Encoding]::ASCII)
        $null = & (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath $cleanSafeTool -ToolArguments @{ ProposedPath = @('CleanScope') }
        if (-not (Test-Path -LiteralPath $reuseIdentity) -or
            [IO.File]::ReadAllText((Join-Path $cleanSnapshotRoot 'CleanScope/overlay.txt')) -cne $content) {
            throw 'Changed overlay was not applied to the existing locked checkout.'
        }
    }
    Remove-Item -LiteralPath $overlayFile -Force
    $null = & (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath $cleanSafeTool -ToolArguments @{ ProposedPath = @('CleanScope') }
    if (Test-Path -LiteralPath (Join-Path $cleanSnapshotRoot 'CleanScope/overlay.txt')) { throw 'Snapshot retained an obsolete untracked overlay file.' }

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

    $privateMarker = Join-Path $cleanSnapshotRoot '.artifacts/llm-wiki/code-graph/prepared.marker'
    $null = & (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath $preparedSafeTool -ToolArguments @{ ProposedPath = @('CleanScope') } -PrepareCodeGraph
    if ([int][IO.File]::ReadAllText($privateMarker) -ne 1) { throw 'Fresh read-only projection was needlessly rebuilt.' }
    [IO.File]::WriteAllText((Join-Path $cleanSnapshotRoot '.artifacts/llm-wiki/code-graph/force-stale.marker'),'stale',[Text.Encoding]::ASCII)
    $null = & (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath $preparedSafeTool -ToolArguments @{ ProposedPath = @('CleanScope') } -PrepareCodeGraph
    if ([int][IO.File]::ReadAllText($privateMarker) -ne 2) { throw 'Stale read-only projection was not refreshed once.' }
    [IO.File]::WriteAllText((Join-Path $cleanSnapshotRoot '.artifacts/llm-wiki/code-graph/force-status-failure.marker'),'unavailable',[Text.Encoding]::ASCII)
    $null = & (Join-Path $cleanToolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath $preparedSafeTool -ToolArguments @{ ProposedPath = @('CleanScope') } -PrepareCodeGraph
    if ([int][IO.File]::ReadAllText($privateMarker) -ne 3) { throw 'An unavailable readiness probe prevented private projection recovery.' }

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
