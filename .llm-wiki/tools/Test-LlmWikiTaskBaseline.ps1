[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $toolsRoot '../..')).Path
$fixtureRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/task-baseline-fixture'

function Assert-Baseline([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

try {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
    & git -C $fixtureRoot init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize task-baseline fixture.' }
    [System.IO.File]::WriteAllText((Join-Path $fixtureRoot 'existing.txt'), 'committed')
    & git -C $fixtureRoot add existing.txt
    & git -C $fixtureRoot -c user.name='LLM Wiki' -c user.email='llm-wiki@example.invalid' commit --quiet -m baseline
    if ($LASTEXITCODE -ne 0) { throw 'Unable to commit task-baseline fixture.' }

    [System.IO.File]::WriteAllText((Join-Path $fixtureRoot 'pre-existing.txt'), 'before develop')
    & (Join-Path $toolsRoot 'Manage-LlmWikiTaskBaseline.ps1') -Action Capture -RepositoryRoot $fixtureRoot -SessionId 'fixture-a' | Out-Null
    & (Join-Path $toolsRoot 'Manage-LlmWikiTaskBaseline.ps1') -Action Capture -RepositoryRoot $fixtureRoot -SessionId 'fixture-b' | Out-Null

    [System.IO.File]::WriteAllText((Join-Path $fixtureRoot 'task.txt'), 'new task file')
    $initialDelta = & (Join-Path $toolsRoot 'Manage-LlmWikiTaskBaseline.ps1') -Action ChangedPaths -RepositoryRoot $fixtureRoot -SessionId 'fixture-a'
    Assert-Baseline (@($initialDelta.changedPaths) -contains 'task.txt') 'Task delta omitted a new task file.'
    Assert-Baseline (@($initialDelta.changedPaths) -notcontains 'pre-existing.txt') 'Task delta included an unchanged pre-existing dirty file.'

    [System.IO.File]::WriteAllText((Join-Path $fixtureRoot 'pre-existing.txt'), 'changed during task')
    $updatedDelta = & (Join-Path $toolsRoot 'Manage-LlmWikiTaskBaseline.ps1') -Action ChangedPaths -RepositoryRoot $fixtureRoot -SessionId 'fixture-a'
    Assert-Baseline (@($updatedDelta.changedPaths) -contains 'task.txt') 'Updated task delta omitted the new task file.'
    Assert-Baseline (@($updatedDelta.changedPaths) -contains 'pre-existing.txt') 'Task delta omitted a task edit to a pre-existing dirty file.'

    & git -C $fixtureRoot add task.txt
    & git -C $fixtureRoot -c user.name='LLM Wiki' -c user.email='llm-wiki@example.invalid' commit --quiet -m task
    if ($LASTEXITCODE -ne 0) { throw 'Unable to commit task delta fixture.' }
    $committedDelta = & (Join-Path $toolsRoot 'Manage-LlmWikiTaskBaseline.ps1') -Action ChangedPaths -RepositoryRoot $fixtureRoot -SessionId 'fixture-a'
    Assert-Baseline (@($committedDelta.changedPaths) -contains 'task.txt') 'Task delta lost a task path after it was committed.'
    Assert-Baseline (@($committedDelta.changedPaths) -contains 'pre-existing.txt') 'Task delta lost the post-baseline dirty edit after a task commit.'
    $isolatedBaseline = & (Join-Path $toolsRoot 'Manage-LlmWikiTaskBaseline.ps1') -Action Status -RepositoryRoot $fixtureRoot -SessionId 'fixture-b'
    Assert-Baseline ($isolatedBaseline.baselinePath -cne $committedDelta.baselinePath) 'Independent sessions shared a task baseline path.'
} finally {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}

Write-Host 'LLM Wiki task-baseline smoke passed: pre-existing dirt is isolated, subsequent edits and commits remain visible.'
