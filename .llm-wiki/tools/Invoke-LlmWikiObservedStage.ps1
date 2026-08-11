[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ToolPath,
    [string]$ArgumentsBase64,
    [string]$ArgumentsPath,
    [string]$StageName = 'wiki-stage',
    [string]$LogPath
)

$ErrorActionPreference = 'Stop'
$json = if ($ArgumentsPath) {
    [IO.File]::ReadAllText($ArgumentsPath)
} elseif ($ArgumentsBase64) {
    [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($ArgumentsBase64))
} else {
    throw 'ArgumentsPath or ArgumentsBase64 is required.'
}
$argumentObject = $json | ConvertFrom-Json
$arguments = @{}
foreach ($property in $argumentObject.PSObject.Properties) {
    $arguments[$property.Name] = $property.Value
}

if ($LogPath) {
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $LogPath) -Force
    if (Test-Path -LiteralPath $LogPath) { Remove-Item -LiteralPath $LogPath -Force }
    Start-Transcript -LiteralPath $LogPath -Force | Out-Null
}
try {
    Write-Host "Observed stage '$StageName' started. Live log: $LogPath"
    $global:LASTEXITCODE = 0
    & $ToolPath @arguments
    if (-not $? -or ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0)) {
        throw "Observed Wiki tool failed with exit code $LASTEXITCODE."
    }
} finally {
    if ($LogPath) { Stop-Transcript | Out-Null }
}
