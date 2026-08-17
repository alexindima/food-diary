[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('assess', 'create', 'show', 'verify')]
    [string]$Action = 'assess',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string[]]$Path = @(),
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$securityPolicy = $policy.scheduler.contextBundles.security
$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
$packetPath = Join-Path $absoluteWorkspace 'change-packet.json'
$receiptPath = Join-Path $absoluteWorkspace 'context-security.json'
if (-not (Test-Path -LiteralPath $packetPath -PathType Leaf)) { throw "Change packet is absent: $workspace/change-packet.json" }

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 30 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-FileSha([string]$Value) {
    (Get-FileHash -LiteralPath $Value -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-Trust([string]$RelativePath) {
    $trustZones = if ($null -ne $securityPolicy -and $securityPolicy.PSObject.Properties['trustZones']) { @($securityPolicy.trustZones) } else { @() }
    foreach ($zone in @($trustZones | Where-Object { $null -ne $_ })) {
        if ($RelativePath -match [string]$zone.pattern) {
            return [pscustomobject]@{ zone = [string]$zone.id; trust = [string]$zone.trust; instructionAuthority = [bool]$zone.instructionAuthority }
        }
    }
    [pscustomobject]@{ zone = 'default'; trust = [string]$securityPolicy.defaultTrust; instructionAuthority = $false }
}
function Get-LineNumber([string]$Text, [int]$Index) {
    if ($Index -le 0) { return 1 }
    ([regex]::Matches($Text.Substring(0, $Index), "`n")).Count + 1
}
function Get-ScanEntry([string]$RelativePath) {
    if ([string]::IsNullOrWhiteSpace($RelativePath)) { return $null }
    $normalized = $RelativePath.Replace('\', '/')
    while ($normalized.StartsWith('./', [StringComparison]::Ordinal)) { $normalized = $normalized.Substring(2) }
    if ([IO.Path]::IsPathRooted($normalized) -or $normalized -match '(^|/)\.\.(/|$)') { throw "Context security path escapes the repository: $RelativePath" }
    $absolute = Join-Path $repositoryRoot $normalized
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) {
        return [pscustomobject][ordered]@{
            path = $normalized; exists = $false; sha256 = ''; scannedCharacters = 0; truncated = $false
            trustZone = 'default'; trust = [string]$securityPolicy.defaultTrust; instructionAuthority = $false
            findingCount = 0; quarantineCount = 0; highestSeverity = 'none'; findings = @()
        }
    }
    $trust = Get-Trust $normalized
    $text = Get-Content -LiteralPath $absolute -Raw
    $limit = [int]$securityPolicy.maximumScanCharactersPerFile
    $truncated = $text.Length -gt $limit
    if ($truncated) { $text = $text.Substring(0, $limit) }
    $findings = @($securityPolicy.promptInjectionPatterns | ForEach-Object {
        $definition = $_
        $matches = @([regex]::Matches($text, [string]$definition.pattern))
        if ($matches.Count -eq 0) { return }
        [pscustomobject][ordered]@{
            id = [string]$definition.id; severity = [string]$definition.severity; count = $matches.Count
            firstLine = Get-LineNumber $text $matches[0].Index
            quarantined = -not [bool]$trust.instructionAuthority
        }
    } | Sort-Object id)
    $severityRank = @{ none = 0; low = 1; medium = 2; high = 3; critical = 4 }
    $highest = 'none'
    foreach ($finding in $findings) { if ($severityRank[$finding.severity] -gt $severityRank[$highest]) { $highest = $finding.severity } }
    [pscustomobject][ordered]@{
        path = $normalized; exists = $true; sha256 = Get-FileSha $absolute; scannedCharacters = $text.Length; truncated = $truncated
        trustZone = $trust.zone; trust = $trust.trust; instructionAuthority = $trust.instructionAuthority
        findingCount = [int](($findings.count | Measure-Object -Sum).Sum)
        quarantineCount = [int](($findings | Where-Object quarantined | Select-Object -ExpandProperty count | Measure-Object -Sum).Sum)
        highestSeverity = $highest; findings = $findings
    }
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Receipt.schemaVersion
        workspace = [string]$Receipt.workspace
        createdAtUtc = ([DateTimeOffset]$Receipt.createdAtUtc).ToUniversalTime().ToString('o')
        packetFingerprint = [string]$Receipt.packetFingerprint
        policyFingerprint = [string]$Receipt.policyFingerprint
        scannerFingerprint = [string]$Receipt.scannerFingerprint
        sources = @($Receipt.sources)
        summary = $Receipt.summary
    }
}
function Get-Summary([object[]]$Sources) {
    $validSources = @($Sources | Where-Object { $null -ne $_ })
    $findingMeasure = $validSources | ForEach-Object {
        if ($_.PSObject.Properties['findingCount']) { [int]$_.findingCount }
    } | Measure-Object -Sum
    $quarantineMeasure = $validSources | ForEach-Object {
        if ($_.PSObject.Properties['quarantineCount']) { [int]$_.quarantineCount }
    } | Measure-Object -Sum
    [pscustomobject][ordered]@{
        sourceCount = $validSources.Count
        findingCount = [int]$findingMeasure.Sum
        quarantineCount = [int]$quarantineMeasure.Sum
        truncatedSourceCount = @($validSources | Where-Object { $_.PSObject.Properties['truncated'] -and [bool]$_.truncated }).Count
        trustedInstructionCount = @($validSources | Where-Object { $_.PSObject.Properties['instructionAuthority'] -and [bool]$_.instructionAuthority }).Count
        untrustedSourceCount = @($validSources | Where-Object { $_.PSObject.Properties['trust'] -and [string]$_.trust -eq 'untrusted-data' }).Count
    }
}
function New-Assessment([string[]]$RequestedPaths) {
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    $paths = @($RequestedPaths | ForEach-Object { $_.Replace('\', '/').TrimStart('./') } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    if ($paths.Count -eq 0) {
        $paths = @(@($packet.brief.instructions) + @($packet.brief.contextPages) + @($packet.diff.changedPaths) | Sort-Object -Unique)
    }
    $sources = @($paths | ForEach-Object { Get-ScanEntry $_ } | Where-Object { $null -ne $_ })
    $summary = Get-Summary $sources
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1; workspace = $workspace; createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        packetFingerprint = [string]$packet.fingerprint
        policyFingerprint = Get-FileSha $policyPath; scannerFingerprint = Get-FileSha $PSCommandPath
        sources = $sources; summary = $summary; assessmentHash = ''
    }
    $receipt.assessmentHash = Get-Hash (Get-Payload $receipt)
    $receipt
}
function Test-Assessment([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $workspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Receipt.packetFingerprint -cne [string]$packet.fingerprint) { $issues.Add('Task packet drifted.') }
    if ([string]$Receipt.policyFingerprint -cne (Get-FileSha $policyPath)) { $issues.Add('Context security policy drifted.') }
    if ([string]$Receipt.scannerFingerprint -cne (Get-FileSha $PSCommandPath)) { $issues.Add('Context security scanner changed.') }
    $receiptSources = if ($null -ne $Receipt -and $Receipt.PSObject.Properties['sources']) { @($Receipt.sources | Where-Object { $null -ne $_ }) } else { @() }
    $currentSources = @($receiptSources | ForEach-Object { Get-ScanEntry ([string]$_.path) } | Where-Object { $null -ne $_ })
    foreach ($source in $receiptSources) {
        $current = Get-ScanEntry ([string]$source.path)
        if ((Get-Hash $source) -cne (Get-Hash $current)) { $issues.Add("Context security source assessment drifted: $($source.path).") }
    }
    if ((Get-Hash $Receipt.summary) -cne (Get-Hash (Get-Summary $currentSources))) { $issues.Add('Context security summary is invalid.') }
    if ([string]$Receipt.assessmentHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Context security assessment hash is invalid.') }
    [pscustomobject]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}

if ($Action -in @('create', 'assess')) {
    $assessment = New-Assessment $Path
    if ($Action -eq 'create') {
        [IO.File]::WriteAllText($receiptPath, (($assessment | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    }
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $true; assessment = $assessment; issues = @(); savedPath = $(if ($Action -eq 'create') { "$workspace/context-security.json" } else { $null }) }
} else {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw "Context security assessment is absent: $workspace/context-security.json" }
    $assessment = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $validation = Test-Assessment $assessment
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $validation.valid; assessment = $assessment; issues = @($validation.issues) }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    Write-Host "Context security: action=$Action, valid=$($result.valid), findings=$($result.assessment.summary.findingCount), quarantined=$($result.assessment.summary.quarantineCount)"
    foreach ($source in @($result.assessment.sources | Where-Object findingCount -gt 0)) { Write-Host " - $($source.path): trust=$($source.trust), findings=$($source.findingCount), quarantined=$($source.quarantineCount)" }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
