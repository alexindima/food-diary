[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string[]]$ChangedPath = @(),
    [string]$Intent,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$normalizedPaths = @($ChangedPath | Where-Object { $_ } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique)
$nonUiPaths = @($normalizedPaths | Where-Object {
    $_ -notmatch '^FoodDiary\.Web\.Client/.+\.(?:ts|html|scss|css|json)$' -and
    $_ -notmatch '^\.llm-wiki/(?:generated|reviews)/'
})
$contractPaths = @($normalizedPaths | Where-Object {
    $_ -match '(?:package(?:-lock)?\.json|angular\.json)$' -or $_ -match '^FoodDiary\.Web\.Client/projects/.+/public-api\.ts$'
})
$eligible = $normalizedPaths.Count -gt 0 -and $nonUiPaths.Count -eq 0 -and $contractPaths.Count -eq 0

$testPlanArguments = @{ BaseRef = $BaseRef; ChangedPath = $normalizedPaths; Compact = $true; Format = 'Json'; Limit = 8 }
if (-not [string]::IsNullOrWhiteSpace($Intent)) { $testPlanArguments.Intent = $Intent }
$testPlan = & (Join-Path $PSScriptRoot 'Get-LlmWikiTestPlan.ps1') @testPlanArguments | ConvertFrom-Json
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    eligible = $eligible
    intent = $Intent
    changedPaths = $normalizedPaths
    rejectedPaths = @($nonUiPaths + $contractPaths | Sort-Object -Unique)
    focusedTests = @($testPlan.focusedTests)
    requiredCommands = @($testPlan.commands | Where-Object priority -eq 'required')
    recommendedCommands = @($testPlan.commands | Where-Object priority -eq 'recommended')
    completionCommand = './.llm-wiki/wiki.ps1 verify-fast -VisualUiCompletion'
    finalizationCommand = './.llm-wiki/wiki.ps1 ui-finalize'
    steps = @('review current UI delta', 'run focused component tests', 'run style/build checks', 'record browser evidence', 'run visual UI completion gate')
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 8; exit 0 }
Write-Host "UI continuation: $(if ($eligible) { 'eligible' } else { 'not eligible' }); $($normalizedPaths.Count) task-delta path(s)."
if (-not $eligible) {
    foreach ($path in $result.rejectedPaths) { Write-Host " - boundary expansion: $path" }
    Write-Host 'Return to develop/research because the established frontend-only boundary changed.'
    exit 2
}
Write-Host 'Focused tests:'
foreach ($test in $result.focusedTests) { Write-Host " - [$($test.priority); $($test.reason)] $($test.path)" }
Write-Host 'Completion flow:'
foreach ($step in $result.steps) { Write-Host " - $step" }
Write-Host "Final gate: $($result.completionCommand)"
Write-Host "Finalize once before commit: $($result.finalizationCommand)"
