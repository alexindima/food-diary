[CmdletBinding()]
param([string]$ToolsRoot = '')
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ToolsRoot)) { $ToolsRoot = $PSScriptRoot }
$root = Join-Path ([IO.Path]::GetTempPath()) "wiki-stage-$([guid]::NewGuid().ToString('N'))"
$fixtureTools = Join-Path $root '.llm-wiki/tools'
$null = New-Item -ItemType Directory -Path $fixtureTools -Force
try {
    foreach ($name in @('Get-LlmWikiVerificationStageFingerprint.ps1', 'LlmWikiGitPaths.ps1', 'Invoke-LlmWikiAffectedSmoke.ps1')) {
        Copy-Item -LiteralPath (Join-Path $ToolsRoot $name) -Destination $fixtureTools
    }
    $policyRoot = Join-Path $root '.llm-wiki/policies'
    $null = New-Item -ItemType Directory -Path $policyRoot -Force
    Copy-Item -LiteralPath (Join-Path $ToolsRoot '../policies/affected-smoke-catalog.psd1') -Destination $policyRoot
    & git -C $root init --quiet
    & git -C $root config core.autocrlf false
    [IO.File]::WriteAllText((Join-Path $root 'source.cs'), 'baseline')
    & git -C $root add .
    & git -C $root -c user.name=Wiki -c user.email=wiki@example.invalid commit --quiet -m baseline
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize stage fingerprint fixture.' }
    $tool = Join-Path $fixtureTools 'Get-LlmWikiVerificationStageFingerprint.ps1'
    $changeDefinitions = Join-Path $policyRoot 'change-policies.json'
    [IO.File]::WriteAllText($changeDefinitions, 'first')
    $policyBefore = & $tool -Stage 'workspace policy'
    $definitionTime = [IO.File]::GetLastWriteTimeUtc($changeDefinitions)
    [IO.File]::WriteAllText($changeDefinitions, 'other')
    [IO.File]::SetLastWriteTimeUtc($changeDefinitions, $definitionTime)
    if ($policyBefore -ceq (& $tool -Stage 'workspace policy')) { throw 'Workspace-policy stage ignored changed referenced check definitions.' }
    foreach ($name in @('AgentRegistry', 'TaskLease', 'SchedulePlan', 'OrchestrationCycle', 'DispatchWatchdog', 'WorkspaceCircuit', 'TaskDecomposition')) {
        $managerPath = Join-Path $fixtureTools "Manage-LlmWiki$name.ps1"
        [IO.File]::WriteAllText($managerPath, 'first')
        $beforeManager = & $tool -Stage 'affected smoke:facade-contract'
        [IO.File]::WriteAllText($managerPath, 'other')
        if ($beforeManager -ceq (& $tool -Stage 'affected smoke:facade-contract')) { throw "Scheduler manager '$name' did not invalidate its regression receipt." }
    }
    $facadeTests = @('SchedulerLocks', 'DispatchMetricsReuse', 'FullAuditShards', 'FacadeCommandCatalog', 'CriticalToolContracts', 'StrictAffected', 'AffectedSmokePlanning', 'ObservedStageReceipt', 'ConcurrentVerify')
    foreach ($name in $facadeTests) {
        [IO.File]::WriteAllText((Join-Path $fixtureTools "Test-LlmWiki$name.ps1"), '# stable facade fixture')
    }
    $runner = Join-Path $fixtureTools 'Invoke-LlmWikiAffectedSmoke.ps1'
    & $runner -ChangedPath @() -RequestedGroup 'facade-contract' -NoCache
    $facadeReceipt = Join-Path $root '.git/llm-wiki/affected-smoke-groups/facade-contract.json'
    if (-not (Test-Path -LiteralPath $facadeReceipt)) { throw 'Facade fixture did not publish a stable success receipt.' }
    $facadeReuse = @(& $runner -ChangedPath @() -RequestedGroup 'facade-contract' 6>&1 | ForEach-Object { $_.ToString() }) -join "`n"
    if ($facadeReuse -notmatch 'group cached: facade-contract') { throw 'Unchanged facade inputs did not reuse their successful receipt.' }
    $shardGuard = Join-Path $fixtureTools 'Test-LlmWikiFullAuditShards.ps1'
    $guardTime = [IO.File]::GetLastWriteTimeUtc($shardGuard)
    [IO.File]::WriteAllText($shardGuard, "throw 'Changed facade guard executed'")
    [IO.File]::SetLastWriteTimeUtc($shardGuard, $guardTime)
    $facadeRejected = $false
    try { & $runner -ChangedPath @() -RequestedGroup 'facade-contract' }
    catch {
        if ($_.Exception.Message -notlike '*Changed facade guard executed*') { throw }
        $facadeRejected = $true
    }
    if (-not $facadeRejected) { throw 'Cached facade receipt skipped its changed, failing shard guard.' }
    [IO.File]::WriteAllText($shardGuard, '# stable facade fixture')
    foreach ($name in $facadeTests) {
        $testPath = Join-Path $fixtureTools "Test-LlmWiki$name.ps1"
        $beforeTest = & $tool -Stage 'affected smoke:facade-contract'
        [IO.File]::WriteAllText($testPath, '# edited facade fixture')
        if ($beforeTest -ceq (& $tool -Stage 'affected smoke:facade-contract')) { throw "Direct facade test '$name' did not invalidate its receipt." }
        if ((& $tool -Stage 'affected smoke:facade-contract') -cne (& $tool -Stage 'affected smoke:facade-contract')) { throw 'Unchanged facade inputs produced unstable fingerprints.' }
        Remove-Item -LiteralPath $testPath
        if ($beforeTest -ceq (& $tool -Stage 'affected smoke:facade-contract')) { throw "Deleted facade test '$name' did not invalidate its receipt." }
        [IO.File]::WriteAllText($testPath, '# stable facade fixture')
    }
    foreach ($stage in @('affected smoke', 'affected smoke:code-graph', 'affected smoke:context-bundle')) {
        [IO.File]::WriteAllText((Join-Path $root 'source.cs'), 'first edit')
        $before = & $tool -Stage $stage
        [IO.File]::WriteAllText((Join-Path $root 'source.cs'), 'other edit')
        $after = & $tool -Stage $stage
        if ($before -ceq $after) { throw "$stage ignored changed product content with identical Git status." }
        if ($after -cne (& $tool -Stage $stage)) { throw "$stage is unstable for unchanged inputs." }
    }
    # Keep the fixture source readable by PS5 without a UTF-8 BOM; the path is still Cyrillic.
    $unicodeStem = -join @([char]0x043f, [char]0x0443, [char]0x0442, [char]0x044c)
    $unicodeTail = -join @([char]0x0444, [char]0x0430, [char]0x0439, [char]0x043b)
    $unicodeName = if ([IO.Path]::DirectorySeparatorChar -eq '\') { "$unicodeStem $unicodeTail.cs" } else { "$unicodeStem -> $unicodeTail.cs" }
    $unicodePath = Join-Path $root $unicodeName
    [IO.File]::WriteAllText($unicodePath, 'first')
    $before = & $tool -Stage 'affected smoke'
    [IO.File]::WriteAllText($unicodePath, 'other')
    if ($before -ceq (& $tool -Stage 'affected smoke')) { throw 'Unicode/arrow path content was not hashed.' }
    & git -C $root add .
    & git -C $root -c user.name=Wiki -c user.email=wiki@example.invalid commit --quiet -m unicode
    & git -C $root mv $unicodeName 'renamed.cs'
    $before = & $tool -Stage 'affected smoke'
    [IO.File]::WriteAllText((Join-Path $root 'renamed.cs'), 'again')
    $after = & $tool -Stage 'affected smoke'
    if ($before -ceq $after) { throw 'Rename destination content was not hashed.' }
    Remove-Item -LiteralPath (Join-Path $root 'renamed.cs')
    if ($after -ceq (& $tool -Stage 'affected smoke')) { throw 'Deleted input was not invalidated.' }
    $graphBeforeReview = & $tool -Stage 'affected smoke'
    $null = New-Item -ItemType Directory -Path (Join-Path $root '.llm-wiki/reviews') -Force
    $reviewPath = Join-Path $root '.llm-wiki/reviews/receipt.json'
    [IO.File]::WriteAllText($reviewPath, '{}')
    if ($graphBeforeReview -cne (& $tool -Stage 'affected smoke')) { throw 'Review metadata invalidated graph smoke.' }
    $reviewBefore = & $tool -Stage 'source impact'
    [IO.File]::WriteAllText($reviewPath, '{"reviewed":true}')
    if ($reviewBefore -ceq (& $tool -Stage 'source impact')) { throw 'Source-impact gate ignored changed review metadata.' }
    $stub = Join-Path $fixtureTools 'Test-LlmWikiCollections.ps1'
    [IO.File]::WriteAllText($stub, '[IO.File]::WriteAllText((Join-Path $PSScriptRoot "LlmWikiCollections.ps1"), "changed during check")')
    $runner = Join-Path $fixtureTools 'Invoke-LlmWikiAffectedSmoke.ps1'
    $rejected = $false
    try { & $runner -ChangedPath @() -RequestedGroup 'strict-shapes' -NoCache }
    catch {
        if ($_.Exception.Message -notlike '*Inputs changed during smoke group*') { throw }
        $rejected = $true
    }
    $receipt = Join-Path $root '.git/llm-wiki/affected-smoke-groups/strict-shapes.json'
    if (-not $rejected -or (Test-Path -LiteralPath $receipt)) { throw 'Changed inputs published a success receipt.' }
    [IO.File]::WriteAllText($stub, '# unchanged-input fixture')
    & $runner -ChangedPath @() -RequestedGroup 'strict-shapes' -NoCache
    if (-not (Test-Path -LiteralPath $receipt)) { throw 'Stable successful check did not publish its receipt.' }
    Write-Host 'Stage fingerprints passed: product edits, stable reuse, Unicode/arrow paths, renames and deletion.'
} finally {
    $resolved = [IO.Path]::GetFullPath($root)
    if (-not $resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture cleanup.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
