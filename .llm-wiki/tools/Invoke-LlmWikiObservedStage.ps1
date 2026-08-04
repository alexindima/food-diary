[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ToolPath,
    [Parameter(Mandatory = $true)]
    [string]$ArgumentsBase64
)

$ErrorActionPreference = 'Stop'
$json = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($ArgumentsBase64))
$argumentObject = $json | ConvertFrom-Json
$arguments = @{}
foreach ($property in $argumentObject.PSObject.Properties) {
    $arguments[$property.Name] = $property.Value
}

$global:LASTEXITCODE = 0
& $ToolPath @arguments
if (-not $? -or ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0)) {
    throw "Observed Wiki tool failed with exit code $LASTEXITCODE."
}
