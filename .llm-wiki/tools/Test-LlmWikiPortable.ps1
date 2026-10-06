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

$portableValue = [pscustomobject][ordered]@{
    text = "apostrophe ' and <>&"
    date = '2026-10-06T00:00:00Z'
    unicode = -join @([char]0x0442, [char]0x0435, [char]0x0441, [char]0x0442)
}
# Retain the existing PowerShell 7 fingerprint while accepting the identical
# value serialized by Windows PowerShell. The constant comes from the baseline.
if ((Get-LlmWikiJsonFingerprint $portableValue) -cne '56cd2450fd16dc1da430fc36d693f260cf5b0a0f2b2e4e03855155589b7dc8ba') {
    throw 'JSON fingerprint changed across runtimes for an identical value.'
}
foreach ($case in @(
    @{ value = "'"; canonical = '{"text":"''"}' },
    @{ value = '\u0027'; canonical = '{"text":"\\u0027"}' },
    @{ value = "\'"; canonical = '{"text":"\\''"}' },
    @{ value = '\uABCD'; canonical = '{"text":"\\uABCD"}' }
)) {
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $expected = [BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($case.canonical))).Replace('-', '').ToLowerInvariant() }
    finally { $hasher.Dispose() }
    if ((Get-LlmWikiJsonFingerprint ([pscustomobject][ordered]@{text=$case.value})) -cne $expected) {
        throw 'JSON fingerprint confused an apostrophe escape with a literal backslash-u sequence.'
    }
}
$upperEscape = Get-LlmWikiJsonFingerprint ([pscustomobject][ordered]@{text='\uABCD'})
$lowerEscape = Get-LlmWikiJsonFingerprint ([pscustomobject][ordered]@{text='\uabcd'})
if ($upperEscape -ceq $lowerEscape) { throw 'Distinct literal Unicode escape strings produced the same fingerprint.' }

& (Join-Path $toolsRoot 'Test-LlmWiki.ps1') | Out-Host
if (-not $?) {
    exit 1
}
& (Join-Path $toolsRoot 'Test-LlmWikiLint.ps1') | Out-Host
if (-not $?) {
    exit 1
}

Write-Host "LLM Wiki portable smoke passed on PowerShell $($PSVersionTable.PSVersion) ($([System.Runtime.InteropServices.RuntimeInformation]::OSDescription))."
