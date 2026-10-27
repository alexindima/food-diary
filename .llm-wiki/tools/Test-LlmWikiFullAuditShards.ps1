[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$tool = Join-Path $PSScriptRoot 'Test-LlmWikiTools.ps1'
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($tool, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count) { throw 'Full audit has syntax errors.' }
# Frozen from the unsharded audit at 45e8de30e. Updating assertions requires an
# intentional inventory refresh, never silently dropping coverage during moves.
$expected = @{
    # Compared with 01ccc7bf3: the exact cache allowlist assertion also includes
    # documented module-page reuse. No assertions were removed; count is unchanged.
    # Include the symbol-index reuse approved in e19f1cf4d in the exact cache
    # allowlist assertion. All 336 Core assertions and their shard remain.
    # The browser storage assertion now names the migrated non-secret session
    # metadata owner. Its top-three requirement and all 336 assertions remain.
    Core = @{ count = 336; hash = 'cf61c41740d3e71bbbd460da127b8bb5719a7487d3c31e10cf31765c67868854' }
    # The lease assertion requires fresh-orphan recovery with unchanged stable
    # lock identity; its old unlink requirement is replaced. Counts remain frozen.
    # CI repair retains tamper rejection and adds separate stale-metadata and
    # genuine unexpected-module cases, increasing Workspace coverage by one.
    # Add the real empty-check retrospective regression; all prior assertions
    # remain unchanged. This assertion belongs to both Governed and Workspace.
    # Compared with 74dbb61f3^, retain every prior assertion and add two context
    # role checks: preserve mandatory reviewer context and reject assignments
    # from invalid bundles. Both checks belong to Governed and Workspace.
    Governed = @{ count = 380; hash = 'b659181d181b2837694371b674b962fef5c25f32ba96f93ac1abe917523185bf' }
    Workspace = @{ count = 283; hash = '761d8aef297f6f1794486ef0e3cf6e3f07f839214f19718b32fd9054921558af' }
    Orchestration = @{ count = 97; hash = 'fdd52d1221307232c0a905826fd28e98c22428d303ffe571b91210a1a2c2aac4' }
    # Retain durable-memory preservation and add canonical model-outcome
    # registry preservation for every shard; all earlier assertions remain.
    Common = @{ count = 2; hash = 'd103f99207c36ec561f389a4c84d8c6250e43bf658e305f442acdb22ea0f9779' }
}
$groups = @{ Core = @(); Governed = @(); Workspace = @(); Orchestration = @(); Common = @() }
foreach ($command in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.CommandAst] -and $node.GetCommandName() -eq 'Assert-Wiki'
}, $true)) {
    $group = 'Common'
    $partition = ''
    for ($parent = $command.Parent; $null -ne $parent; $parent = $parent.Parent) {
        if ($parent -isnot [Management.Automation.Language.IfStatementAst]) { continue }
        $condition = [string]$parent.Clauses[0].Item1.Extent.Text
        if ($condition -ceq '$AuditShard -ne ''Orchestration''') { $partition = 'Workspace' }
        if ($condition -ceq '$Profile -eq ''Full'' -and $AuditShard -ne ''Workspace''') { $partition = 'Orchestration' }
        if ($condition -ceq '$AuditShard -in @(''All'', ''Core'')') { $group = 'Core'; break }
        if ($condition -ceq '$Profile -eq ''Full'' -and $AuditShard -ne ''Core''') { $group = 'Governed'; break }
    }
    $groups[$group] += $command.Extent.Text.Replace("`r`n", "`n")
    if ($partition) { $groups[$partition] += $command.Extent.Text.Replace("`r`n", "`n") }
}
foreach ($group in @('Core', 'Governed', 'Workspace', 'Orchestration', 'Common')) {
    [string[]]$items = $groups[$group]
    [Array]::Sort($items, [StringComparer]::Ordinal)
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($items -join "`n---`n")))).ToLowerInvariant()
    if ($items.Count -ne $expected[$group].count -or $hash -cne $expected[$group].hash) {
        throw "Full audit $group assertion inventory changed: count=$($items.Count), hash=$hash. Review coverage before refreshing the inventory."
    }
}
$source = [IO.File]::ReadAllText($tool)
if (-not $source.Contains('AuditShard = $AuditShard') -or -not $source.Contains('[string]$AuditShard = ''All''')) {
    throw 'Full audit lost shard propagation into the isolated snapshot or the exhaustive default.'
}
if (-not $source.Contains('-SnapshotPartition "tools-audit-${Profile}:$AuditShard"')) {
    throw 'Full audit shards must retain distinct private checkout partitions.'
}
foreach ($profile in @('Focused', 'Core')) {
    $rejected = $false
    try { & $tool -Profile $profile -AuditShard Governed | Out-Null }
    catch { $rejected = $_.Exception.Message -eq 'AuditShard requires the Full profile.' }
    if (-not $rejected) { throw "Shard selection unexpectedly accepted $profile profile." }
}
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$workflow = [IO.File]::ReadAllText((Join-Path $repositoryRoot '.github/workflows/ci-tests.yml'))
$audit = [regex]::Match($workflow, '(?s)\n  llm-wiki-audit:.*?(?=\n  llm-wiki:)').Value
foreach ($required in @('shard: [Core, Workspace, Orchestration]', 'fail-fast: false', '-Profile Full -AuditShard $env:WIKI_AUDIT_SHARD', 'llm-wiki-audit-${{ matrix.shard }}-failure-logs', 'actions/checkout@')) {
    if (-not $audit.Contains($required)) { throw "CI audit shard contract missing: $required" }
}
if (-not $workflow.Contains('needs: [llm-wiki-focused, llm-wiki-smoke, llm-wiki-audit]') -or
    -not $workflow.Contains('needs.llm-wiki-audit.result')) { throw 'Wiki gate must require the aggregate result of every audit shard.' }
& (Join-Path $repositoryRoot 'scripts/ci/Test-WikiCiResult.ps1')
$assertionCount = $groups.Core.Count + $groups.Governed.Count + $groups.Common.Count
Write-Host "Full audit shard contracts passed: $assertionCount assertions verified, isolated CI matrix, exhaustive default and failure gate."
