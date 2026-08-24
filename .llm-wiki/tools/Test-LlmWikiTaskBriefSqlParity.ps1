[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$briefTool = Join-Path $PSScriptRoot 'Get-LlmWikiTaskBrief.ps1'
$null = & $manager -Action build -Format Json

$cases = @(
    [pscustomobject]@{ Intent = 'Update the password reset command handler'; ProposedPath = @() }
    [pscustomobject]@{ Intent = 'Improve the fasting controller endpoint'; ProposedPath = @() }
    [pscustomobject]@{ Intent = 'Improve the responsive dashboard component visual layout'; ProposedPath = @() }
    [pscustomobject]@{ Intent = 'Review repository architecture documentation'; ProposedPath = @() }
    [pscustomobject]@{
        Intent = 'Update user application behavior'
        ProposedPath = @('FoodDiary.Application.Users/Commands/UpdateUser/UpdateUserCommandHandler.cs')
    }
)
$sqlDurations = [Collections.Generic.List[double]]::new()
$jsonDurations = [Collections.Generic.List[double]]::new()
$caseIndex = 0

foreach ($case in $cases) {
    $arguments = @{
        Intent = $case.Intent
        ProposedPath = [string[]]@($case.ProposedPath)
        Format = 'Json'
        Limit = 6
        Compact = $true
        SkipTestPlan = $true
        SkipQueryCache = $true
    }
    if (($caseIndex % 2) -eq 0) {
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $briefTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $briefTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
    } else {
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $briefTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $briefTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
    }

    if (($sqlite | ConvertTo-Json -Depth 14 -Compress) -cne ($json | ConvertTo-Json -Depth 14 -Compress)) {
        throw "$($case.Intent): SQLite/JSON task-brief parity failed."
    }
    $sqlDurations.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
    $jsonDurations.Add($jsonStopwatch.Elapsed.TotalMilliseconds)
    $caseIndex++
}

$sqlAverage = [Math]::Round(($sqlDurations | Measure-Object -Average).Average, 2)
$jsonAverage = [Math]::Round(($jsonDurations | Measure-Object -Average).Average, 2)
if ($sqlAverage -gt ($jsonAverage + 500)) {
    throw "SQLite task-brief route regressed beyond the 500ms end-to-end safety envelope: SQL=${sqlAverage}ms, JSON=${jsonAverage}ms."
}
Write-Host "LLM Wiki task-brief SQL parity passed: $($cases.Count)/$($cases.Count) cases; SQL=${sqlAverage}ms, JSON=${jsonAverage}ms average end-to-end."
