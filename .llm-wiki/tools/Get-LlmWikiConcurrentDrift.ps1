[CmdletBinding()]
param(
    [string]$SessionId,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$arguments = @{ Action = 'ChangedPaths'; Format = 'Object' }
if (-not [string]::IsNullOrWhiteSpace($SessionId)) { $arguments.SessionId = $SessionId }
$baseline = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskBaseline.ps1') @arguments
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    available = [bool]$baseline.available
    sessionKey = [string]$baseline.sessionKey
    baselineHead = [string]$baseline.head
    commitsAhead = [int]$baseline.commitsAhead
    ageHours = [double]$baseline.ageHours
    taskChangedPaths = @($baseline.changedPaths)
    concurrentOrPreExistingPaths = @($baseline.excludedChangedPaths)
    driftDetected = @($baseline.excludedChangedPaths).Count -gt 0 -or [int]$baseline.commitsAhead -gt 0
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 5; exit 0 }
Write-Host "Context drift: available=$($result.available), detected=$($result.driftDetected), commits-ahead=$($result.commitsAhead), task-paths=$(@($result.taskChangedPaths).Count), concurrent/pre-existing=$(@($result.concurrentOrPreExistingPaths).Count)."
foreach ($path in $result.concurrentOrPreExistingPaths) { Write-Host " - external: $path" }
