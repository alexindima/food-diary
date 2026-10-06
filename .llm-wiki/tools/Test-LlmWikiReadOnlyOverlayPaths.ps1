[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiChangeSetSnapshot.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$guardAst = [Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $PSScriptRoot 'Invoke-LlmWikiReadOnlyTool.ps1'), [ref]$null, [ref]$null)
$overlayFunction = $guardAst.Find({
    param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-WorkspaceOverlayPaths'
}, $false)
. ([scriptblock]::Create($overlayFunction.Extent.Text))
foreach ($name in @('Test-CommonReadOnlyOverlayPath', 'Get-ReadOnlySnapshotSlotScope', 'Get-ReadOnlySnapshotFingerprint', 'Get-FileHashOrMissing', 'Remove-StaleReadOnlySnapshots')) {
    $function = $guardAst.Find({
        param($node)
        $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $false)
    . ([scriptblock]::Create($function.Extent.Text))
}
$defaultSlot = Get-ReadOnlySnapshotFingerprint -RepositoryRoot $repositoryRoot -OverlayPath @() -SlotKey
$coreSlot = Get-ReadOnlySnapshotFingerprint -RepositoryRoot $repositoryRoot -OverlayPath @() -SlotKey -Partition 'tools-audit-Full:Core'
$workspaceSlot = Get-ReadOnlySnapshotFingerprint -RepositoryRoot $repositoryRoot -OverlayPath @() -SlotKey -Partition 'tools-audit-Full:Workspace'
if (@(@($defaultSlot, $coreSlot, $workspaceSlot) | Sort-Object -Unique).Count -ne 3 -or
    $coreSlot -cne (Get-ReadOnlySnapshotFingerprint -RepositoryRoot $repositoryRoot -OverlayPath @() -SlotKey -Partition 'tools-audit-Full:Core')) {
    throw 'Audit partitions collided with short queries or lost stable checkout reuse.'
}
$sourceIdentity = Get-ReadOnlySnapshotFingerprint -RepositoryRoot $repositoryRoot -OverlayPath @()
if ($sourceIdentity -cne (Get-ReadOnlySnapshotFingerprint -RepositoryRoot $repositoryRoot -OverlayPath @() -Partition 'tools-audit-Full:Core')) {
    throw 'Audit partition changed the source content identity.'
}
$selectorFunction = $guardAst.Find({
    param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Select-RelevantOverlayPath'
}, $false)
. ([scriptblock]::Create($selectorFunction.Extent.Text))
$scopeAlias = @(Select-RelevantOverlayPath -WorkspacePath @('alpha/file.cs', 'other/file.cs', '.llm-wiki/note.md') -Arguments @{ ScopePath = @('alpha') })
if (($scopeAlias -join ',') -ne 'alpha/file.cs,.llm-wiki/note.md') { throw 'Context ScopePath did not constrain the snapshot overlay.' }
$readerPath = 'FoodDiary.Development.Mcp/Wiki/SqliteContextSearchReader.cs'
$readerOverlay = @(Select-RelevantOverlayPath -WorkspacePath @($readerPath, 'other/file.cs') -Arguments @{ ScopePath = @('alpha') })
if ($readerOverlay.Count -ne 1 -or $readerOverlay[0] -ne $readerPath) { throw 'Scoped snapshots omitted shared CLI reader inputs.' }
foreach ($overlay in @(@(), @('.llm-wiki/note.md', 'Directory.Build.props', $readerPath))) {
    if (@(Get-ReadOnlySnapshotSlotScope -OverlayPath $overlay -RequestedScope @('alpha')).Count -ne 0) {
        throw 'Identical HEAD/common overlays unnecessarily retained a separate scope slot.'
    }
}
foreach ($path in @('alpha/file.cs', 'alpha/deleted.cs', 'alpha/new.cs', 'other/file.cs', '.llm-wiki-other/file.cs')) {
    $slot = @(Get-ReadOnlySnapshotSlotScope -OverlayPath @('.llm-wiki/note.md', $path) -RequestedScope @('alpha'))
    if (($slot -join ',') -cne 'alpha') { throw "Product overlay '$path' lost scope isolation." }
}
if (@(Get-ReadOnlySnapshotSlotScope -OverlayPath @('alpha/file.cs') -RequestedScope @()).Count -ne 0) {
    throw 'An unscoped product overlay introduced a null/empty slot path.'
}
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'read-only-overlay-paths'
try {
    $unicodeName = -join @([char]0x0444, [char]0x0430, [char]0x0439, [char]0x043B)
    $unicodePath = "folder with spaces/$unicodeName.txt"
    foreach ($directory in @('alpha', 'folder with spaces', '.llm-wiki')) {
        $null = New-Item -ItemType Directory -Path (Join-Path $fixture $directory) -Force
    }
    foreach ($path in @('alpha/changed.txt', 'alpha/deleted.txt', $unicodePath, 'outside.txt', '.llm-wiki/note.txt')) {
        [IO.File]::WriteAllText((Join-Path $fixture $path), 'baseline', [Text.UTF8Encoding]::new($false))
    }
    [IO.File]::WriteAllText((Join-Path $fixture 'alpha/old-location.cs'), 'public class UniqueRelocatedHandler {}', [Text.UTF8Encoding]::new($false))
    $null = Invoke-LlmWikiGitCommand -RepositoryRoot $fixture -Arguments @('init', '--quiet')
    $null = Invoke-LlmWikiGitCommand -RepositoryRoot $fixture -Arguments @('add', '--all')
    $null = Invoke-LlmWikiGitCommand -RepositoryRoot $fixture -Arguments @('-c', 'user.name=Wiki Smoke', '-c', 'user.email=wiki-smoke@example.invalid', 'commit', '--quiet', '-m', 'fixture')
    $null = Invoke-LlmWikiGitCommand -RepositoryRoot $fixture -Arguments @('mv', 'alpha/old-location.cs', 'alpha/new-location.cs')
    foreach ($path in @('alpha/changed.txt', $unicodePath, 'outside.txt', '.llm-wiki/note.txt', 'alpha/untracked.txt', 'outside-untracked.txt')) {
        [IO.File]::WriteAllText((Join-Path $fixture $path), 'changed', [Text.UTF8Encoding]::new($false))
    }
    Remove-Item -LiteralPath (Join-Path $fixture 'alpha/deleted.txt') -Force
    $smallScopes = @('alpha', 'folder with spaces', '.llm-wiki')
    $expectedScoped = @(
        @(Invoke-LlmWikiGitPathList -RepositoryRoot $fixture -Arguments (@('diff', '--no-renames', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--') + $smallScopes)) +
        @(Invoke-LlmWikiGitPathList -RepositoryRoot $fixture -Arguments (@('ls-files', '--others', '--exclude-standard', '--') + $smallScopes)) |
            Sort-Object -Unique
    )
    $largeScopes = @('alpha', 'alpha/changed.txt', 'alpha', 'folder with spaces') + @(1..1000 | ForEach-Object {
        "absent-scope-$($_.ToString('0000'))/" + ('x' * 70)
    })
    if (($largeScopes -join ' ').Length -le 32767) { throw 'Large-scope regression must exceed the Windows argv limit.' }
    $actualScoped = @(Get-WorkspaceOverlayPaths -RepositoryRoot $fixture -RelevantPath $largeScopes)
    if (($actualScoped -join "`n") -cne ($expectedScoped -join "`n")) { throw 'Batched overlay differs from the complete unbatched scoped result.' }
    [array]::Reverse($largeScopes)
    $reversedScoped = @(Get-WorkspaceOverlayPaths -RepositoryRoot $fixture -RelevantPath $largeScopes)
    if (($actualScoped -join "`n") -cne ($reversedScoped -join "`n")) { throw 'Batched overlay is not stable across duplicate/reordered scopes.' }
    if ($unicodePath -notin $actualScoped -or 'alpha/deleted.txt' -notin $actualScoped -or 'alpha/untracked.txt' -notin $actualScoped) {
        throw 'Batched overlay lost Unicode, deleted, or untracked paths.'
    }
    if ('alpha/old-location.cs' -notin $actualScoped -or 'alpha/new-location.cs' -notin $actualScoped) {
        throw 'A staged rename must delete the old snapshot source and copy the new source.'
    }
    $smallSnapshot = Get-LlmWikiChangeSetSnapshot -RepositoryRoot $fixture -RelevantPath $smallScopes
    $largeSnapshot = Get-LlmWikiChangeSetSnapshot -RepositoryRoot $fixture -RelevantPath (@($largeScopes) + '.llm-wiki')
    if ($smallSnapshot.fingerprint -cne $largeSnapshot.fingerprint -or
        ($largeSnapshot.changedPaths -join "`n") -cne ($expectedScoped -join "`n")) {
        throw 'Batched change-set snapshot lost paths or changed its content fingerprint.'
    }
    $magicRejected = $false
    try { $null = Split-LlmWikiPositiveGitPathspecBatch -Pathspec @('alpha', ':(exclude)alpha/private') } catch { $magicRejected = $true }
    if (-not $magicRejected) { throw 'Exclusion pathspecs must not silently change meaning across batches.' }
    $expectedAll = @(
        @(Invoke-LlmWikiGitPathList -RepositoryRoot $fixture -Arguments @('diff', '--no-renames', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--')) +
        @(Invoke-LlmWikiGitPathList -RepositoryRoot $fixture -Arguments @('ls-files', '--others', '--exclude-standard', '--')) |
            Sort-Object -Unique
    )
    $actualAll = @(Get-WorkspaceOverlayPaths -RepositoryRoot $fixture -RelevantPath @())
    if (($actualAll -join "`n") -cne ($expectedAll -join "`n")) { throw 'Empty overlay scope must enumerate the complete workspace.' }
    $cacheFixture = Join-Path $fixture '.artifacts/lock-lifetime'
    $staleKey = 'a' * 64
    $currentKey = 'f' * 64
    $staleRoot = Join-Path $cacheFixture $staleKey
    $staleLockPath = Join-Path $cacheFixture "$staleKey.lock"
    $staleReadyPath = Join-Path $cacheFixture "$staleKey.ready"
    $null = New-Item -ItemType Directory -Path (Join-Path $staleRoot '.git') -Force
    [IO.File]::WriteAllText($staleReadyPath, 'ready')
    (Get-Item -LiteralPath $staleReadyPath).LastWriteTimeUtc = [datetime]::UtcNow.AddMinutes(-10)
    foreach ($recentKey in @(('b' * 64), ('c' * 64))) {
        [IO.File]::WriteAllText((Join-Path $cacheFixture "$recentKey.ready"), 'recent')
    }
    $heldLock = [IO.File]::Open($staleLockPath, 'OpenOrCreate', 'ReadWrite', 'None')
    try {
        Remove-StaleReadOnlySnapshots -RepositoryRoot $fixture -SnapshotParent $cacheFixture -CurrentFingerprint $currentKey -Retain 2
        if (-not (Test-Path -LiteralPath $staleRoot) -or -not (Test-Path -LiteralPath $staleLockPath)) {
            throw 'Pruning removed a busy snapshot or its live lock identity.'
        }
    } finally { $heldLock.Dispose() }
    Remove-StaleReadOnlySnapshots -RepositoryRoot $fixture -SnapshotParent $cacheFixture -CurrentFingerprint $currentKey -Retain 2
    if ((Test-Path -LiteralPath $staleRoot) -or (Test-Path -LiteralPath $staleReadyPath) -or
        -not (Test-Path -LiteralPath $staleLockPath)) {
        throw 'Pruning must remove the stale clone while retaining its reusable lock identity.'
    }
    $clearTools = Join-Path $fixture '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $clearTools -Force
    foreach ($tool in @('Clear-LlmWikiReadOnlySnapshotCache.ps1','LlmWikiGitPaths.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $tool) -Destination (Join-Path $clearTools $tool) -Force
    }
    $cacheHasher = [Security.Cryptography.SHA256]::Create()
    try { $cacheHash = $cacheHasher.ComputeHash([Text.Encoding]::UTF8.GetBytes(([IO.Path]::GetFullPath($fixture)).ToLowerInvariant())) }
    finally { $cacheHasher.Dispose() }
    $cacheKey = (([BitConverter]::ToString($cacheHash) -replace '-', '').ToLowerInvariant()).Substring(0,16)
    $tempRoot = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
        Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'Temp'
    } else { [IO.Path]::GetTempPath() }
    $cacheBase = [IO.Path]::GetFullPath((Join-Path $tempRoot 'fooddiary-llm-wiki-read-only'))
    $ownedCache = [IO.Path]::GetFullPath((Join-Path $cacheBase $cacheKey))
    $ownedRoot = Join-Path $ownedCache $staleKey
    $ownedLockPath = Join-Path $ownedCache "$staleKey.lock"
    try {
        $null = New-Item -ItemType Directory -Path (Join-Path $ownedRoot '.git') -Force
        [IO.File]::WriteAllText((Join-Path $ownedCache "$staleKey.ready"), 'ready')
        $heldLock = [IO.File]::Open($ownedLockPath, 'OpenOrCreate', 'ReadWrite', 'None')
        $shell = (Get-Process -Id $PID).Path
        try {
            $output = & $shell -NoLogo -NoProfile -File (Join-Path $clearTools 'Clear-LlmWikiReadOnlySnapshotCache.ps1') -Retain 0
            if ($LASTEXITCODE -ne 0 -or ($output -join ' ') -notmatch 'busy=1' -or
                -not (Test-Path -LiteralPath $ownedRoot) -or -not (Test-Path -LiteralPath $ownedLockPath)) {
                throw 'Explicit cache cleanup removed a busy clone or its live lock identity.'
            }
        } finally { $heldLock.Dispose() }
        $output = & $shell -NoLogo -NoProfile -File (Join-Path $clearTools 'Clear-LlmWikiReadOnlySnapshotCache.ps1') -Retain 0
        if ($LASTEXITCODE -ne 0 -or ($output -join ' ') -notmatch 'removed=1' -or
            (Test-Path -LiteralPath $ownedRoot) -or -not (Test-Path -LiteralPath $ownedLockPath)) {
            throw 'Explicit cleanup must delete the unlocked clone but preserve its lock identity.'
        }
    } finally {
        $cachePrefix = $cacheBase.TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
        if (-not $ownedCache.StartsWith($cachePrefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe owned cache fixture cleanup path.' }
        if (Test-Path -LiteralPath $ownedCache) { Remove-Item -LiteralPath $ownedCache -Recurse -Force }
    }
    $invalidRepository = Join-Path $fixture 'not-a-repository'
    $null = New-Item -ItemType Directory -Path $invalidRepository -Force
    [IO.File]::WriteAllText((Join-Path $invalidRepository '.git'), 'gitdir: missing', [Text.Encoding]::ASCII)
    $gitFailure = $false
    try { $null = Get-WorkspaceOverlayPaths -RepositoryRoot $invalidRepository -RelevantPath $largeScopes } catch {
        $gitFailure = $_.Exception.Message -like '*Unable to enumerate tracked workspace changes*'
    }
    if (-not $gitFailure) { throw 'Batched overlay swallowed a Git enumeration failure.' }
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if (-not $resolvedFixture.StartsWith([IO.Path]::GetFullPath($repositoryRoot) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the regression repository.'
    }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host 'LLM Wiki overlay batching and lock lifetime regressions passed: scoped paths, Unicode, renames, Git failures, busy-cache protection, and persistent lock identity after prune/clear.'
