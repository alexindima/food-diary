[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('executed-check', 'manual-check', 'review-attestation')]
    [string]$Kind,
    [Parameter(Mandatory = $true)]
    [string]$EvidencePath,
    [Parameter(Mandatory = $true)]
    [string]$Id,
    [string]$Command,
    [string]$Definition,
    [string]$Reason,
    [string]$Status,
    [Nullable[int]]$ExitCode,
    [Nullable[double]]$DurationSeconds,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$absoluteEvidencePath = if ([System.IO.Path]::IsPathRooted($EvidencePath)) { $EvidencePath } else { Join-Path $repositoryRoot $EvidencePath }
$evidence = Get-Content -LiteralPath $absoluteEvidencePath -Raw | ConvertFrom-Json
$policy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') `
    -ChangedPath @($evidence.change.changedPaths) `
    -Format Json | ConvertFrom-Json
$requirement = if ($Kind -eq 'review-attestation') {
    $policy.reviewObligations | Where-Object id -eq $Id | Select-Object -First 1
} else {
    $policy.requiredChecks | Where-Object id -eq $Id | Select-Object -First 1
}
$sourceRule = if ($null -ne $requirement) { [string]$requirement.sourceRule } else { 'manual' }
$isProductDependency = {
    param([string]$Path)
    $normalized = $Path.Replace('\', '/')
    return $normalized -notmatch '^\.llm-wiki/(generated|reviews)/' -and
        $normalized -notmatch '^\.artifacts/llm-wiki/'
}
$dependencyPaths = if ($sourceRule -eq 'manual') {
    @($evidence.change.changedPaths | Where-Object $isProductDependency)
} else {
    @($policy.matchedRules | Where-Object id -eq $sourceRule | Select-Object -First 1).matchedPaths |
        Where-Object $isProductDependency
}
$content = & (Join-Path $PSScriptRoot 'Get-LlmWikiContentFingerprint.ps1') -Path @($dependencyPaths) -Format Json | ConvertFrom-Json
$head = & git rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD for evidence lineage.' }
$policyHash = (Get-FileHash -LiteralPath (Join-Path $wikiRoot 'policies/change-policies.json') -Algorithm SHA256).Hash.ToLowerInvariant()
$definitionValue = if (-not [string]::IsNullOrWhiteSpace($Definition)) { $Definition } elseif ($null -ne $requirement) {
    if ($Kind -eq 'review-attestation') { [string]$requirement.description } else { [string]$requirement.command }
} else { '' }
$runtimeName = if ($Command -match '^(?:dotnet )') { 'dotnet' } elseif ($Command -match '(?:^|&& )npm ') { 'npm' } else { 'powershell' }
$runtimeVersion = switch ($runtimeName) {
    'dotnet' { [string](& dotnet --version) }
    'npm' { [string](& npm --version) }
    default { [string]$PSVersionTable.PSVersion }
}
$platform = "$([System.Runtime.InteropServices.RuntimeInformation]::OSDescription.Trim())/$([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)"
function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 12 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
$compatibilityPayload = [pscustomobject][ordered]@{
    kind = $Kind
    id = $Id
    sourceRule = $sourceRule
    definition = $definitionValue
    command = $Command
    dependencyContentFingerprint = [string]$content.fingerprint
    policyFingerprint = $policyHash
    runtime = "$runtimeName/$runtimeVersion"
    platform = $platform
}
$lineage = [pscustomobject][ordered]@{
    schemaVersion = 1
    recordedAtUtc = [DateTime]::UtcNow.ToString('o')
    kind = $Kind
    status = $Status
    subject = [pscustomobject][ordered]@{
        id = $Id
        sourceRule = $sourceRule
        definition = $definitionValue
        definitionFingerprint = Get-Hash $definitionValue
    }
    git = [pscustomobject][ordered]@{
        head = [string]$head
        base = [string]$evidence.git.base
    }
    dependencies = [pscustomobject][ordered]@{
        paths = @($dependencyPaths | Sort-Object -Unique)
        contentFingerprint = [string]$content.fingerprint
    }
    policyFingerprint = $policyHash
    environment = [pscustomobject][ordered]@{
        os = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription.Trim()
        architecture = [string][System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    }
    execution = [pscustomobject][ordered]@{
        command = $Command
        commandFingerprint = Get-Hash ([string]$Command)
        exitCode = $ExitCode
        durationSeconds = $DurationSeconds
        runtime = $runtimeName
        runtimeVersion = $runtimeVersion
    }
    attestation = [pscustomobject][ordered]@{ reason = $Reason }
    compatibilityFingerprint = Get-Hash $compatibilityPayload
}
if ($Format -eq 'Json') { $lineage | ConvertTo-Json -Depth 10 } else { $lineage }
