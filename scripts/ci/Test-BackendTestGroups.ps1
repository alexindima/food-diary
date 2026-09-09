[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$runner = Join-Path $PSScriptRoot 'Invoke-BackendTests.ps1'
$planPath = Join-Path $PSScriptRoot 'backend-test-groups.json'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$plan = Get-Content -LiteralPath $planPath -Raw | ConvertFrom-Json
foreach ($entry in $plan.groups) {
    $result = & $runner -Group $entry.name -PlanOnly
    if ($result.Projects.Count -ne $entry.projects.Count) { throw 'Selected project count differs from the plan.' }
    Write-Host "$($entry.name): $($result.Projects.Count) projects; complete partition: $($result.TotalProjects)"
}

# Keep process/database tests out of the early lane, including mixed projects
# whose .csproj names do not advertise their integration tests.
foreach ($project in ($plan.groups | Where-Object name -EQ 'fast').projects) {
    $directory = Split-Path (Join-Path $repositoryRoot $project)
    $sources = Get-ChildItem -LiteralPath $directory -Filter '*.cs' -Recurse -File |
        Where-Object { $_.FullName -notmatch '[/\\](bin|obj)[/\\]' }
    foreach ($source in $sources) {
        $content = Get-Content -LiteralPath $source.FullName -Raw
        if ($content -match 'RequiresDocker|Testcontainers|\[PowerShellFact|Category["'']\s*,\s*["''](?:Integration|Slow)') {
            throw "Slow dependency in fast project $project`: $($source.Name). Move the whole project to a slow group."
        }
    }
}

$tempRoot = Join-Path $repositoryRoot ".artifacts/backend-ci-plan-tests/$([Guid]::NewGuid().ToString('N'))"
[void](New-Item -ItemType Directory -Force -Path $tempRoot)
function Assert-Rejected([string]$Name, [scriptblock]$Mutate, [string]$ExpectedError) {
    $candidate = Get-Content -LiteralPath $planPath -Raw | ConvertFrom-Json
    & $Mutate $candidate
    $candidatePath = Join-Path $tempRoot "$Name.json"
    $candidate | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $candidatePath
    try {
        & $runner -Group fast -PlanPath $candidatePath -PlanOnly | Out-Null
    } catch {
        if ($_.Exception.Message -notlike "*$ExpectedError*") { throw }
        Write-Host "Rejected $Name as expected."
        return
    }
    throw "Invalid plan was accepted: $Name"
}

Assert-Rejected 'duplicate-project' { param($p) $p.groups[1].projects += $p.groups[0].projects[0] } 'Duplicate test project'
Assert-Rejected 'missing-project' { param($p) $p.groups[0].projects = @($p.groups[0].projects | Select-Object -Skip 1) } 'Unassigned test projects'
Assert-Rejected 'unknown-project' { param($p) $p.groups[0].projects += '../Unknown.Tests.csproj' } 'Unknown test project'
Assert-Rejected 'duplicate-group' { param($p) $p.groups[1].name = $p.groups[0].name } 'Unknown or duplicate test group'
Assert-Rejected 'missing-group' { param($p) $p.groups = @($p.groups | Select-Object -SkipLast 1) } 'required backend test group is missing'
Assert-Rejected 'empty-group' { param($p) $p.groups[0].projects = @() } 'Empty test group'
Assert-Rejected 'unknown-schema' { param($p) $p.schemaVersion = 2 } 'Unsupported backend test plan schema'
Write-Host 'Backend test partition checks passed.'
