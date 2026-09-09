[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$gate = Join-Path $PSScriptRoot 'Assert-WikiCiResult.ps1'
$count = 0
foreach ($eventName in @('pull_request', 'push', 'schedule', 'workflow_dispatch')) {
    foreach ($focused in @('success', 'failure', 'cancelled', 'skipped')) {
        foreach ($audit in @('success', 'failure', 'cancelled', 'skipped')) {
            $expected = $focused -eq 'success' -and (
                ($eventName -eq 'pull_request' -and $audit -eq 'skipped') -or
                ($eventName -ne 'pull_request' -and $audit -eq 'success'))
            $passed = $false
            try {
                & $gate -EventName $eventName -FocusedResult $focused -AuditResult $audit *> $null
                $passed = $true
            } catch { }
            if ($passed -ne $expected) {
                throw "Incorrect Wiki gate result: event=$eventName, focused=$focused, audit=$audit."
            }
            $count++
        }
    }
}
Write-Host "Wiki CI gate contracts passed: $count combinations."
