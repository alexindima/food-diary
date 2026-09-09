[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$EventName,
    [Parameter(Mandatory)][ValidateSet('success', 'failure', 'cancelled', 'skipped')][string]$FocusedResult,
    [Parameter(Mandatory)][ValidateSet('success', 'failure', 'cancelled', 'skipped')][string]$AuditResult
)

$ErrorActionPreference = 'Stop'
if ($FocusedResult -ne 'success') {
    throw "Wiki focused regressions did not succeed: $FocusedResult."
}
$expectedAuditResult = if ($EventName -eq 'pull_request') { 'skipped' } else { 'success' }
if ($AuditResult -ne $expectedAuditResult) {
    throw "Wiki Full audit returned '$AuditResult'; '$expectedAuditResult' is required for '$EventName'."
}
Write-Host 'All applicable Wiki CI checks passed.'
