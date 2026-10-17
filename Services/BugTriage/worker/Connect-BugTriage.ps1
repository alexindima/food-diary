# Check the same authenticated stdio route used by the bridge, without an API key.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
& python (Join-Path $PSScriptRoot 'ssh_transport.py')
if ($LASTEXITCODE -ne 0) { throw 'BugTriage authenticated SSH transport check failed.' }
