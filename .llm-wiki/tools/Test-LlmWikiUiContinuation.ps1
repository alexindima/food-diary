[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
function Assert-UiContinuation([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }

$tool = Join-Path $PSScriptRoot 'Get-LlmWikiUiContinuation.ps1'
$ui = & $tool -ChangedPath @(
    'FoodDiary.Web.Client/src/app/features/dashboard/pages/dashboard.ts',
    'FoodDiary.Web.Client/src/app/features/dashboard/pages/dashboard.spec.ts'
) -Format Json | ConvertFrom-Json
Assert-UiContinuation ([bool]$ui.eligible) 'A bounded frontend iteration was rejected.'
Assert-UiContinuation (@($ui.focusedTests).Count -gt 0) 'UI continuation omitted focused tests.'
Assert-UiContinuation ([string]$ui.completionCommand -match 'verify-fast') 'UI continuation omitted the fast completion gate.'
Assert-UiContinuation ([string]$ui.finalizationCommand -match 'ui-finalize') 'UI continuation omitted the one-time final index synchronization.'

$expanded = & $tool -ChangedPath @(
    'FoodDiary.Web.Client/src/app/features/dashboard/pages/dashboard.ts',
    'FoodDiary.Application/Users/UpdateUser.cs'
) -Format Json | ConvertFrom-Json
Assert-UiContinuation (-not [bool]$expanded.eligible) 'A frontend/backend boundary expansion was accepted as UI continuation.'
Assert-UiContinuation (@($expanded.rejectedPaths) -contains 'FoodDiary.Application/Users/UpdateUser.cs') 'UI continuation did not explain the rejected path.'

Write-Host 'LLM Wiki UI-continuation regression passed.'
