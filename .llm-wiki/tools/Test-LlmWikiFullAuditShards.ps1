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
    # Compared with ca6ae5b5e: only BillingController and architecture-test paths
    # changed inside assertions after their moves. Every assertion is preserved.
    Core = @{ count = 333; hash = '3c8768a5fd7976a6c8a62d5148dabb55b75cc17d3578fdae534163ce869293fb' }
    # Daily metrics assertion now verifies the exact UTC dates and total across midnight;
    # no assertions were removed or relaxed.
    Governed = @{ count = 376; hash = '9a0737e2c64b6e66529ce281c5e186137475102dbb50a6de9b537d372051b221' }
    Workspace = @{ count = 279; hash = 'ba880a6e89141db9d594badfdd783734b187785ad3281d1c4cf755ae336fffd4' }
    Orchestration = @{ count = 97; hash = 'd22a4de6aae3709be895115af15f25a04555712c818dca2c04e3a6b393b58105' }
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
Write-Host 'Full audit shard contracts passed: all 710 original assertions preserved, isolated CI matrix, exhaustive default and failure gate.'
