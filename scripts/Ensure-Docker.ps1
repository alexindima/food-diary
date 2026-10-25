#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateRange(1, 600)][int]$TimeoutSeconds = 180,
    [switch]$NoStart
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'DockerReadiness.psm1') -Force
Wait-FoodDiaryDocker -TimeoutSeconds $TimeoutSeconds -NoStart:$NoStart
