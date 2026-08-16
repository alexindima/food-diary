[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$changedPaths = @(
    'FoodDiary.Application.Cycles/Commands/UpdateMenstrualEpisode/UpdateMenstrualEpisodeCommand.cs'
    'FoodDiary.Domain/Entities/Tracking/MenstrualEpisode.cs'
    'FoodDiary.Presentation.Api/Features/Cycles/Requests/UpdateMenstrualEpisodeHttpRequest.cs'
    'FoodDiary.Web.Client/src/app/features/cycle-tracking/api/cycles.service.ts'
)
$plan = & (Join-Path $PSScriptRoot 'Get-LlmWikiTestPlan.ps1') `
    -Intent 'Edit menstrual episode history' `
    -ChangedPath $changedPaths `
    -Format Json | ConvertFrom-Json

$ids = @($plan.commands | ForEach-Object { [string]$_.id })
if ($ids.Count -ne @($ids | Sort-Object -Unique).Count) {
    throw "Test-plan command IDs are not unique: $($ids -join ', ')."
}
$unrelated = @($plan.focusedTestFiles | Where-Object {
    $_ -match '/features/(?:dashboard|dietologist)/'
})
if ($unrelated.Count -gt 0) {
    throw "Unrelated cross-feature frontend tests leaked into the cycle plan: $($unrelated -join ', ')."
}
$cycleTests = @($plan.focusedTestFiles | Where-Object { $_ -match '(?i)cycle|menstrual' })
if ($cycleTests.Count -eq 0) {
    throw 'Cycle test-plan precision regression found no cycle-focused tests.'
}

Write-Host "LLM Wiki test-plan precision passed: $($ids.Count) unique command IDs, $($cycleTests.Count) cycle-focused tests."
