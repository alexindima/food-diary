[CmdletBinding()]
param([Parameter(Mandatory)][ValidatePattern('^[a-z][a-z0-9-]{0,39}$')][string]$TaskName)
$ErrorActionPreference = 'Stop'
& node (Join-Path $PSScriptRoot 'ai/task-runtime.mjs') start --name $TaskName
if ($LASTEXITCODE -ne 0) { throw "Task runtime preparation failed. Inspect .artifacts/task-runtime/$TaskName." }
