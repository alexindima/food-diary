[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('export', 'verify')]
    [string]$Action = 'export',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$Path,
    [Nullable[int]]$Limit,
    [switch]$FailOnSensitive,
    [switch]$Overwrite,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$workspacePolicySnapshot = ConvertFrom-LlmWikiJson `
    (& (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -WithFingerprint -Format Json)
$workspacePolicy = $workspacePolicySnapshot.policy
$effectiveLimit = if ($null -ne $Limit) { [int]$Limit } else { [int]$workspacePolicy.export.defaultContextItems }
if ($effectiveLimit -lt 1 -or $effectiveLimit -gt [int]$workspacePolicy.export.maximumContextItems) {
    throw "Limit must be between 1 and $($workspacePolicy.export.maximumContextItems)."
}
$redactionCount = 0
$redactionCategories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

function Get-Fingerprint([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant()
    } finally {
        $sha.Dispose()
    }
}
function Get-Patterns {
    return @($workspacePolicy.export.redaction.patterns)
}
function Protect-Text([string]$Text) {
    $value = $Text
    foreach ($definition in Get-Patterns) {
        $category = [string]$definition.category
        $replacementMode = [string]$definition.replacementMode
        $regex = [regex]::new(
            [string]$definition.pattern,
            [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        $value = $regex.Replace($value, {
            param($match)
            $script:redactionCount++
            $null = $script:redactionCategories.Add($category)
            if ($replacementMode -eq 'preserve-group-1') {
                return $match.Groups[1].Value + '[REDACTED]@'
            }
            if ($replacementMode -eq 'preserve-groups-1-2') {
                return $match.Groups[1].Value + $match.Groups[2].Value + '[REDACTED]'
            }
            return '[REDACTED]'
        })
    }
    return $value
}
function ConvertTo-SafeValue([object]$Value) {
    if ($null -eq $Value) { return $null }
    if ($Value -is [string]) { return Protect-Text ([string]$Value) }
    if ($Value -is [bool] -or $Value -is [ValueType]) { return $Value }
    if ($Value -is [System.Collections.IDictionary]) {
        $dictionary = [ordered]@{}
        foreach ($key in $Value.Keys) { $dictionary[[string]$key] = ConvertTo-SafeValue $Value[$key] }
        return [pscustomobject]$dictionary
    }
    if ($Value -is [System.Collections.IEnumerable]) {
        return @($Value | ForEach-Object { ConvertTo-SafeValue $_ })
    }
    $object = [ordered]@{}
    foreach ($property in @($Value.PSObject.Properties)) {
        if ($property.MemberType -in @('NoteProperty', 'Property')) {
            $object[$property.Name] = ConvertTo-SafeValue $property.Value
        }
    }
    return [pscustomobject]$object
}
function Find-SensitiveCategories([string]$Text) {
    $found = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($definition in Get-Patterns) {
        if ([regex]::IsMatch(
            $Text,
            [string]$definition.pattern,
            [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            $null = $found.Add([string]$definition.category)
        }
    }
    return @($found | Sort-Object)
}
function Resolve-ExportPath([string]$RequestedPath, [string]$DefaultName) {
    $resolved = if ([string]::IsNullOrWhiteSpace($RequestedPath)) {
        ".artifacts/llm-wiki/exports/$DefaultName.task-export.json"
    } else {
        $RequestedPath.Replace('\', '/')
    }
    if ([System.IO.Path]::IsPathRooted($resolved) -or
        $resolved -notmatch [string]$workspacePolicy.export.pathPattern) {
        throw 'Export path must be a JSON file directly inside .artifacts/llm-wiki/exports.'
    }
    return $resolved
}
function Write-Result([object]$Result) {
    if ($Format -eq 'Json') {
        $Result | ConvertTo-Json -Depth 10
    } else {
        Write-Host "Task export: valid=$($Result.valid), path=$($Result.path)"
        if ($null -ne $Result.redactionCount) {
            Write-Host "Redactions: $($Result.redactionCount) ($(@($Result.redactionCategories) -join ', '))"
        }
        foreach ($issue in @($Result.issues)) { Write-Host " - $issue" }
    }
}

if ($Action -eq 'verify') {
    if ([string]::IsNullOrWhiteSpace($Path)) { throw 'verify requires -Path.' }
    $normalizedPath = Resolve-ExportPath $Path 'unused'
    $absolutePath = Join-Path $repositoryRoot $normalizedPath
    $issues = [System.Collections.Generic.List[string]]::new()
    $package = $null
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        $issues.Add('Export file is absent.')
    } else {
        try {
            $package = ConvertFrom-LlmWikiJson (Get-Content -LiteralPath $absolutePath -Raw)
            if ($package.schemaVersion -ne 1) { $issues.Add('Unsupported export schemaVersion.') }
            if ([string]$package.kind -cne 'llm-wiki-task-export') { $issues.Add('Unexpected export kind.') }
            if ([string]$package.redaction.policy -cne [string]$workspacePolicy.export.redaction.policyId) {
                $issues.Add('Export redaction policy does not match the current workspace policy.')
            }
            if ([string]$package.source.policyFingerprint -cne [string]$workspacePolicySnapshot.fingerprint) {
                $issues.Add('Export was produced under a different workspace policy fingerprint.')
            }
            $sealedContent = [ordered]@{
                schemaVersion = $package.schemaVersion
                kind = $package.kind
                exportedAtUtc = $package.exportedAtUtc
                source = $package.source
                handoff = $package.handoff
                redaction = $package.redaction
            }
            if ((Get-Fingerprint $sealedContent) -cne [string]$package.seal.sha256) {
                $issues.Add('Export SHA-256 seal is invalid.')
            }
            $sensitiveCategories = Find-SensitiveCategories ($sealedContent | ConvertTo-Json -Depth 20 -Compress)
            if ($sensitiveCategories.Count -gt 0) {
                $issues.Add("Export still contains sensitive patterns: $($sensitiveCategories -join ', ')")
            }
        } catch {
            $issues.Add("Unable to read export: $($_.Exception.Message)")
        }
    }
    $result = [pscustomobject][ordered]@{
        schemaVersion = 1
        path = $normalizedPath
        valid = $issues.Count -eq 0
        sourceWorkspace = $(if ($null -ne $package) { [string]$package.source.workspace } else { '' })
        sha256 = $(if ($null -ne $package) { [string]$package.seal.sha256 } else { '' })
        issues = @($issues)
    }
    Write-Result $result
    if ($FailOnInvalid -and -not $result.valid) { exit 1 }
    return
}

if ([System.IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$workspaceName = Split-Path $normalizedWorkspacePath -Leaf
$normalizedExportPath = Resolve-ExportPath $Path $workspaceName
$absoluteExportPath = Join-Path $repositoryRoot $normalizedExportPath
if ((Test-Path -LiteralPath $absoluteExportPath) -and -not $Overwrite) {
    throw "Export already exists: $normalizedExportPath. Use -Overwrite to replace it."
}

$doctor = ConvertFrom-LlmWikiJson (& (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json)
if (-not $doctor.valid) {
    throw "Refusing to export an invalid workspace: $(@($doctor.errors) -join ' ')"
}
$handoff = ConvertFrom-LlmWikiJson (& (Join-Path $PSScriptRoot 'Get-LlmWikiTaskHandoff.ps1') `
    -WorkspacePath $normalizedWorkspacePath `
    -Limit $effectiveLimit `
    -Format Json)
$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
$taskContract = ConvertFrom-LlmWikiJson (Get-Content -LiteralPath (Join-Path $absoluteWorkspacePath 'task-contract.json') -Raw)
$handoff.scope | Add-Member -NotePropertyName allowedPathPatterns `
    -NotePropertyValue @($taskContract.scope.allowedPathPatterns) -Force
$handoff.scope | Add-Member -NotePropertyName excludedPathPatterns `
    -NotePropertyValue @($taskContract.scope.excludedPathPatterns) -Force

# Rebuild the few fields that may point to local runtime artifacts. Logs and
# arbitrary workspace files are deliberately never part of this contract.
$handoff.checks = @($handoff.checks | ForEach-Object {
    [pscustomobject][ordered]@{
        id = $_.id
        status = $_.status
        command = $_.command
        reason = $_.reason
    }
})
$safeHandoff = ConvertTo-SafeValue $handoff
$redaction = [pscustomobject][ordered]@{
    applied = $redactionCount -gt 0
    count = $redactionCount
    categories = @($redactionCategories | Sort-Object)
    policy = [string]$workspacePolicy.export.redaction.policyId
}
if ($FailOnSensitive -and $redactionCount -gt 0) {
    throw "Sensitive patterns were found and redacted ($redactionCount): $(@($redactionCategories | Sort-Object) -join ', '). Export was not written."
}
$sealedContent = [ordered]@{
    schemaVersion = 1
    kind = 'llm-wiki-task-export'
    exportedAtUtc = [DateTime]::UtcNow.ToString('o')
    source = [pscustomobject][ordered]@{
        workspace = $normalizedWorkspacePath
        workspaceSchemaVersion = $doctor.workspaceSchemaVersion
        state = $safeHandoff.state
        continuityFingerprint = $safeHandoff.continuity.currentPacketFingerprint
        policyFingerprint = [string]$workspacePolicySnapshot.fingerprint
    }
    handoff = $safeHandoff
    redaction = $redaction
}
$package = [ordered]@{}
foreach ($item in $sealedContent.GetEnumerator()) { $package[$item.Key] = $item.Value }
$package.seal = [pscustomobject][ordered]@{
    algorithm = 'SHA-256'
    sha256 = Get-Fingerprint $sealedContent
}
$outputDirectory = Split-Path -Parent $absoluteExportPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}
$temporaryPath = Join-Path $outputDirectory ('.task-export-' + [guid]::NewGuid().ToString('N') + '.json')
try {
    [System.IO.File]::WriteAllText(
        $temporaryPath,
        (($package | ConvertTo-Json -Depth 20) + [Environment]::NewLine),
        [System.Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $temporaryPath -Destination $absoluteExportPath -Force:$Overwrite
} finally {
    if (Test-Path -LiteralPath $temporaryPath) { [System.IO.File]::Delete($temporaryPath) }
}
$verify = ConvertFrom-LlmWikiJson (& $PSCommandPath verify -Path $normalizedExportPath -Format Json)
if (-not $verify.valid) {
    [System.IO.File]::Delete($absoluteExportPath)
    throw "Generated export failed self-verification: $(@($verify.issues) -join ' ')"
}
Write-Result ([pscustomobject][ordered]@{
    schemaVersion = 1
    path = $normalizedExportPath
    valid = $true
    sourceWorkspace = $normalizedWorkspacePath
    sha256 = $package.seal.sha256
    redactionCount = $redactionCount
    redactionCategories = @($redactionCategories | Sort-Object)
    issues = @()
})
