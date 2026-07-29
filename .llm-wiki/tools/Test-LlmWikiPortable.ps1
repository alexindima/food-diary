[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot

. (Join-Path $toolsRoot 'LlmWikiJson.ps1')
Enable-LlmWikiStringDateJsonParsing

$isoTimestamp = '2026-07-29T13:34:35.1234567Z'
$parsed = "{`"at`":`"$isoTimestamp`"}" | ConvertFrom-Json
if ($parsed.at -isnot [string] -or $parsed.at -cne $isoTimestamp) {
    throw "ISO timestamps must remain JSON strings; actual type is '$($parsed.at.GetType().FullName)'."
}

$roundTrip = ConvertFrom-LlmWikiJson "{`"at`":`"$isoTimestamp`"}"
if ($roundTrip.at -isnot [string] -or $roundTrip.at -cne $isoTimestamp) {
    throw 'Canonical JSON parsing changed an ISO timestamp.'
}

& (Join-Path $toolsRoot 'Test-LlmWiki.ps1') | Out-Host
if (-not $?) {
    exit 1
}
& (Join-Path $toolsRoot 'Test-LlmWikiLint.ps1') | Out-Host
if (-not $?) {
    exit 1
}

Write-Host "LLM Wiki portable smoke passed on PowerShell $($PSVersionTable.PSVersion) ($([System.Runtime.InteropServices.RuntimeInformation]::OSDescription))."
