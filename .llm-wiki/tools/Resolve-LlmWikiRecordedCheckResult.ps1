[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [object]$Evidence,
    [Parameter(Mandatory = $true)]
    [string]$Id
)

$entry = @($Evidence.checks | Where-Object id -eq $Id | Select-Object -First 1)
if ($entry.Count -eq 0) { throw "Executed check is absent from refreshed evidence: $Id" }
$status = [string]$entry[0].status
if ($status -notin @('passed', 'failed')) {
    throw "Executed check '$Id' has non-terminal recorded status '$status'."
}
[pscustomobject][ordered]@{
    status = $status
    exitCode = $(if ($status -eq 'passed') { 0 } else { 1 })
    entry = $entry[0]
}

