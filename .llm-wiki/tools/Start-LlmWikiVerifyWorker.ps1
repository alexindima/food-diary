[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WikiPath,
    [Parameter(Mandatory)][string]$ArgumentsPath,
    [Parameter(Mandatory)][string]$LogPath
)

$ErrorActionPreference = 'Stop'
$argumentsObject = Get-Content -LiteralPath $ArgumentsPath -Raw | ConvertFrom-Json
$arguments = @{}
foreach ($property in $argumentsObject.PSObject.Properties) { $arguments[$property.Name] = $property.Value }
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $LogPath) -Force
Start-Transcript -LiteralPath $LogPath -Force | Out-Null
try {
    & $WikiPath verify @arguments
    if (-not $?) { exit 1 }
} finally {
    Stop-Transcript | Out-Null
}
