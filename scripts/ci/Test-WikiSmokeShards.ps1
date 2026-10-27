[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$selector = Join-Path $PSScriptRoot 'Get-WikiSmokeShard.ps1'
$planPath = Join-Path $PSScriptRoot 'wiki-smoke-shards.json'
$plan = Get-Content -LiteralPath $planPath -Raw | ConvertFrom-Json
$runner = Join-Path $PSScriptRoot '../../.llm-wiki/tools/Invoke-LlmWikiParallelSmoke.ps1'
foreach ($shard in $plan.shards) {
    $groups = @(& $selector -Shard $shard.name)
    $resolved = & $runner -RequestedGroup $groups -Plan -Format Json | ConvertFrom-Json
    if (@(Compare-Object $groups $resolved.groups).Count -ne 0) {
        throw "Wiki planner changed the exact shard group selection: $($shard.name)"
    }
}
$tempRoot = Join-Path $PSScriptRoot "../../.artifacts/wiki-ci-plan-tests/$([guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path $tempRoot -Force
function Assert-Rejected([string]$Name, [scriptblock]$Mutate, [string]$ExpectedError) {
    $candidate = Get-Content -LiteralPath $planPath -Raw | ConvertFrom-Json
    & $Mutate $candidate
    $candidatePath = Join-Path $tempRoot "$Name.json"
    $candidate | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $candidatePath
    try { & $selector -Shard smoke-1 -PlanPath $candidatePath | Out-Null }
    catch {
        if ($_.Exception.Message -notlike "*$ExpectedError*") { throw }
        Write-Host "Rejected $Name as expected."
        return
    }
    throw "Invalid Wiki smoke plan was accepted: $Name"
}
Assert-Rejected duplicate-group { param($p) $p.shards[1].groups += $p.shards[0].groups[0] } 'Duplicate Wiki smoke group'
Assert-Rejected missing-group { param($p) $p.shards[0].groups = @($p.shards[0].groups | Select-Object -Skip 1) } 'Unassigned Wiki smoke groups'
Assert-Rejected unknown-group { param($p) $p.shards[0].groups += 'unknown-group' } 'Unknown Wiki smoke group'
Assert-Rejected duplicate-shard { param($p) $p.shards[1].name = $p.shards[0].name } 'Unknown or duplicate Wiki smoke shard'
Assert-Rejected missing-shard { param($p) $p.shards = @($p.shards | Select-Object -SkipLast 1) } 'required Wiki smoke shard is missing'
Assert-Rejected empty-shard { param($p) $p.shards[0].groups = @() } 'Empty Wiki smoke shard'
Assert-Rejected unknown-schema { param($p) $p.schemaVersion = 2 } 'Unsupported Wiki smoke shard schema'
Write-Host 'Wiki smoke shard completeness, exact selection and rejection checks passed.'
