[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repository -Name 'facade-scope' -IndependentRepository
$ownedParent = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture))
$shell = (Get-Process -Id $PID).Path
try {
    $tools = Join-Path $fixture '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $tools,(Join-Path $fixture 'owned') -Force
    Copy-Item (Join-Path $repository '.llm-wiki/wiki.ps1') (Join-Path $fixture '.llm-wiki/wiki.ps1')
    foreach ($name in @('LlmWikiGitPaths','LlmWikiJson','LlmWikiProcess','Manage-LlmWikiTaskBaseline','Resolve-LlmWikiSession','Get-LlmWikiExperience')) {
        Copy-Item (Join-Path $PSScriptRoot "$name.ps1") $tools
    }
    # Only routing is under test; graph and isolation execution have their own suites.
    [IO.File]::WriteAllText((Join-Path $tools 'Invoke-LlmWikiReadOnlyTool.ps1'), 'param($ToolPath,$ToolArguments,[switch]$PrepareCodeGraph,[switch]$BackendOnlyRefresh); & $ToolPath @ToolArguments')
    [IO.File]::WriteAllText((Join-Path $tools 'Get-LlmWikiAdaptiveWorkflow.ps1'), @'
param($Objective,$BaseRef,$ProposedPath,$Format,$Limit)
@{ profile='tiny'; ceremonyBudget=@{label='tiny'}; stages=@(@{id='implementation';command='implement the declared paths'});nextAction=@{command='implement the declared paths'};objective=$Objective;paths=@($ProposedPath) } | ConvertTo-Json -Depth 6
'@)
    [IO.File]::WriteAllText((Join-Path $tools 'Get-LlmWikiDiffContext.ps1'), 'param($BaseRef,$HeadRef,$ChangedPath,$BaselineExcludedPath,$Format); @{changedPaths=@($(if($PSBoundParameters.ContainsKey("ChangedPath")){@($ChangedPath)}else{@("explicit-comparison")}));excluded=@($BaselineExcludedPath)} | ConvertTo-Json')
    [IO.File]::WriteAllText((Join-Path $tools 'Invoke-LlmWikiDeliveryWorkflow.ps1'), 'throw "Foreign tasks/current was incorrectly selected."')
    [IO.File]::WriteAllText((Join-Path $tools 'Write-LlmWikiWorkflowMetric.ps1'), '# Telemetry is outside this routing fixture.')
    [IO.File]::WriteAllText((Join-Path $fixture '.gitignore'), '.artifacts/')
    [IO.File]::WriteAllText((Join-Path $fixture 'owned/value.txt'), 'original')
    & git -C $fixture add .
    & git -C $fixture -c user.name=WikiFixture -c user.email=wiki-fixture@example.invalid commit -qm initial
    if ($LASTEXITCODE -ne 0) { throw 'Unable to commit facade fixture.' }
    function Invoke-Facade([string[]]$ArgumentList) {
        $json = & $shell -NoLogo -NoProfile -File (Join-Path $fixture '.llm-wiki/wiki.ps1') @ArgumentList -TaskSessionId 'scope-fixture' -Format Json
        if ($LASTEXITCODE -ne 0) { throw "Fixture facade failed: $($ArgumentList -join ' ')" }
        return ($json -join "`n") | ConvertFrom-Json
    }
    $null = Invoke-Facade @('develop','-Intent','Fix owned feature','-PlannedPath','owned')
    [IO.File]::WriteAllText((Join-Path $fixture 'owned/value.txt'), 'task edit')
    [IO.File]::WriteAllText((Join-Path $fixture 'foreign.txt'), 'foreign edit')
    $diff = Invoke-Facade @('diff')
    if (@($diff.changedPaths).Count -ne 1 -or $diff.changedPaths[0] -ne 'owned/value.txt') { throw 'Facade diff lost the scope supplied to develop.' }
    if ('foreign.txt' -notin @($diff.excluded)) { throw 'Facade hid unrelated changes instead of exposing excluded context.' }
    $explicit = Invoke-Facade @('diff','-ChangedPath','foreign.txt')
    if (@($explicit.changedPaths).Count -ne 1 -or $explicit.changedPaths[0] -ne 'foreign.txt') { throw 'Saved scope overrode an explicit caller path.' }
    foreach ($refArguments in @(@('diff','-BaseRef','HEAD'), @('diff','-BaseRef','HEAD','-HeadRef','HEAD'))) {
        $comparison = Invoke-Facade $refArguments
        if (@($comparison.changedPaths).Count -ne 1 -or $comparison.changedPaths[0] -ne 'explicit-comparison') { throw 'Session scope overrode an explicitly requested Git comparison.' }
    }
    $current = Join-Path $fixture '.artifacts/llm-wiki/tasks/current'
    $null = New-Item -ItemType Directory -Path $current -Force
    [IO.File]::WriteAllText((Join-Path $current 'workspace.json'), '{"objective":"Another task"}')
    $next = Invoke-Facade @('next')
    if ($next.mode -ne 'adaptive-route' -or $next.objective -ne 'Fix owned feature') { throw 'Next did not resume the session-specific bounded task.' }
    $newIntent = Invoke-Facade @('next','-Intent','Fix a different task','-PlannedPath','foreign.txt')
    if ($newIntent.mode -ne 'adaptive-route' -or $newIntent.objective -ne 'Fix a different task') { throw 'Next ignored a new explicit intent.' }
    Write-Host 'Facade task scope passed: retained scope, explicit overrides, excluded context and session-specific next.'
} finally {
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture)) -cne $ownedParent) { throw 'Fixture escaped its owned parent.' }
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
