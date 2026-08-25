[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ToolPath,
    [string]$ArgumentsBase64,
    [string]$ArgumentsPath,
    [string]$StageName = 'wiki-stage',
    [string]$LogPath,
    [string]$ResultPath,
    [string]$Fingerprint,
    [string]$PassedReceiptPath
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

function Write-StageResult([string]$Status, [int]$ExitCode, [string]$Detail) {
    if ([string]::IsNullOrWhiteSpace($ResultPath)) { return }
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $ResultPath) -Force
    $payload = [ordered]@{
        schemaVersion = 1
        stage = $StageName
        fingerprint = $Fingerprint
        status = $Status
        exitCode = $ExitCode
        detail = $Detail
        workerProcessId = $PID
        completedAtUtc = [DateTime]::UtcNow.ToString('o')
    }
    $temporary = "$ResultPath.$PID.tmp"
    [IO.File]::WriteAllText($temporary, (($payload | ConvertTo-Json -Depth 4) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    [IO.File]::Move($temporary, $ResultPath)
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
    if (-not [string]::IsNullOrWhiteSpace($PassedReceiptPath)) {
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $PassedReceiptPath) -Force
        [IO.File]::WriteAllText($PassedReceiptPath, ([DateTime]::UtcNow.ToString('o') + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    }
    Write-StageResult 'passed' 0 ''
} catch {
    Write-StageResult 'failed' 1 $_.Exception.Message
    throw
} finally {
    if ($LogPath) { Stop-Transcript | Out-Null }
}
