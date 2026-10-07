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
    # Compared with 63d3954eb: linkGoogle now requires generated SDK transport
    # and adds a POST-method assertion; its discovery and endpoint checks remain.
    Core = @{ count = 336; hash = 'c91ff39543ed6c226d74276bccf4e3b550132ff2aa90d53cc733a356d9436d8e' }
    # The lease assertion requires fresh-orphan recovery with unchanged stable
    # lock identity; its old unlink requirement is replaced. Counts remain frozen.
    Governed = @{ count = 376; hash = '992083313b939fab1e649bd4f61fe64e1d46bcfb017b0f580e3a41f6d2c2922f' }
    Workspace = @{ count = 279; hash = 'ba880a6e89141db9d594badfdd783734b187785ad3281d1c4cf755ae336fffd4' }
    Orchestration = @{ count = 97; hash = 'fdd52d1221307232c0a905826fd28e98c22428d303ffe571b91210a1a2c2aac4' }
    Common = @{ count = 1; hash = '43c752083d5bd294ccf4a8efb8bdf4cd9831a14bc33acdbaa06de6659838f6bd' }
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
if (-not $workflow.Contains('needs: [llm-wiki-focused, llm-wiki-audit]') -or
    -not $workflow.Contains('needs.llm-wiki-audit.result')) { throw 'Wiki gate must require the aggregate result of every audit shard.' }
& (Join-Path $repositoryRoot 'scripts/ci/Test-WikiCiResult.ps1')
$assertionCount = $groups.Core.Count + $groups.Governed.Count + $groups.Common.Count
Write-Host "Full audit shard contracts passed: $assertionCount assertions verified, isolated CI matrix, exhaustive default and failure gate."
