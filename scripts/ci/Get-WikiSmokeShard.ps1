[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Shard,
    [string]$PlanPath = (Join-Path $PSScriptRoot 'wiki-smoke-shards.json')
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$catalog = Import-PowerShellDataFile (Join-Path $repositoryRoot '.llm-wiki/policies/affected-smoke-catalog.psd1')
$expected = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($group in $catalog.Groups) {
    if ($group.IncludeInAll) { [void]$expected.Add([string]$group.Id) }
}
$plan = Get-Content -LiteralPath $PlanPath -Raw | ConvertFrom-Json
if ($plan.schemaVersion -ne 1) { throw 'Unsupported Wiki smoke shard schema.' }
$requiredShards = @('smoke-1', 'smoke-2', 'smoke-3', 'smoke-4')
$names = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($entry in $plan.shards) {
    if ($entry.name -cnotin $requiredShards -or -not $names.Add($entry.name)) {
        throw "Unknown or duplicate Wiki smoke shard: $($entry.name)"
    }
    if (@($entry.groups).Count -eq 0) { throw "Empty Wiki smoke shard: $($entry.name)" }
    foreach ($group in $entry.groups) {
        if (-not $expected.Contains($group)) { throw "Unknown Wiki smoke group: $group" }
        if (-not $seen.Add($group)) { throw "Duplicate Wiki smoke group: $group" }
    }
}
if ($names.Count -ne $requiredShards.Count) { throw 'A required Wiki smoke shard is missing.' }
if (-not $seen.SetEquals($expected)) {
    throw "Unassigned Wiki smoke groups: $(($expected | Where-Object { -not $seen.Contains($_) }) -join ', ')"
}
if (-not $names.Contains($Shard)) { throw "Unknown requested Wiki smoke shard: $Shard" }
($plan.shards | Where-Object name -CEQ $Shard).groups
