[CmdletBinding()]
param(
    [string]$WorkspacePath,
    [string]$EvidencePath,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($WorkspacePath) -eq [string]::IsNullOrWhiteSpace($EvidencePath)) {
    throw 'Specify exactly one of WorkspacePath or EvidencePath.'
}
$normalizedWorkspacePath = ''
$normalizedEvidencePath = ''
if (-not [string]::IsNullOrWhiteSpace($WorkspacePath)) {
    if ([System.IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
    $normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
    if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
        throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
    }
    $normalizedEvidencePath = "$normalizedWorkspacePath/evidence.json"
} else {
    if ([System.IO.Path]::IsPathRooted($EvidencePath)) {
        $resolvedEvidencePath = [System.IO.Path]::GetFullPath($EvidencePath)
        $repositoryPrefix = $repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
        if (-not $resolvedEvidencePath.StartsWith($repositoryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Absolute EvidencePath must remain inside the repository.'
        }
        $normalizedEvidencePath = $resolvedEvidencePath.Substring($repositoryPrefix.Length).Replace('\', '/')
    } else {
        $normalizedEvidencePath = $EvidencePath.Replace('\', '/')
    }
    if ($normalizedEvidencePath -notmatch '^\.artifacts/llm-wiki/(?:tasks/[^/]+/)?[^/]+\.json$') {
        throw 'EvidencePath must be a direct JSON artifact or task evidence under .artifacts/llm-wiki.'
    }
}
$absoluteEvidencePath = Join-Path $repositoryRoot $normalizedEvidencePath
if (-not (Test-Path -LiteralPath $absoluteEvidencePath -PathType Leaf)) { throw "Evidence is absent: $normalizedEvidencePath" }
$evidence = Get-Content -LiteralPath $absoluteEvidencePath -Raw | ConvertFrom-Json
$policy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') `
    -ChangedPath @($evidence.change.changedPaths) `
    -Format Json | ConvertFrom-Json
$policyHash = (Get-FileHash -LiteralPath (Join-Path $wikiRoot 'policies/change-policies.json') -Algorithm SHA256).Hash.ToLowerInvariant()
$issues = [System.Collections.Generic.List[string]]::new()
$items = [System.Collections.Generic.List[object]]::new()

function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 12 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Test-SameSet([object[]]$Left, [object[]]$Right) {
    $leftValues = @($Left | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $rightValues = @($Right | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    return ($leftValues.Count -eq $rightValues.Count -and (Compare-Object $leftValues $rightValues).Count -eq 0)
}
function Test-Lineage([string]$Kind, [object]$Entry, [object]$Requirement) {
    $entryIssues = [System.Collections.Generic.List[string]]::new()
    $lineage = $Entry.lineage
    if ($null -eq $lineage) {
        $entryIssues.Add('lineage is missing')
    } else {
        if ([string]$lineage.subject.id -cne [string]$Entry.id) { $entryIssues.Add('subject id does not match') }
        if ([string]$lineage.status -cne [string]$Entry.status) { $entryIssues.Add('recorded status does not match') }
        $expectedDefinition = if ($Kind -eq 'review') { [string]$Requirement.description } else { [string]$Requirement.command }
        if ([string]$lineage.subject.definition -cne $expectedDefinition) { $entryIssues.Add('definition changed') }
        if ([string]$lineage.subject.definitionFingerprint -cne (Get-Hash $expectedDefinition)) { $entryIssues.Add('definition fingerprint is invalid') }
        if ([string]$lineage.subject.sourceRule -cne [string]$Requirement.sourceRule) { $entryIssues.Add('source rule changed') }
        $expectedPaths = @($policy.matchedRules | Where-Object id -eq $Requirement.sourceRule | Select-Object -First 1).matchedPaths
        if (-not (Test-SameSet @($lineage.dependencies.paths) $expectedPaths)) { $entryIssues.Add('dependency paths changed') }
        $content = & (Join-Path $PSScriptRoot 'Get-LlmWikiContentFingerprint.ps1') -Path $expectedPaths -Format Json | ConvertFrom-Json
        if ([string]$lineage.dependencies.contentFingerprint -cne [string]$content.fingerprint) { $entryIssues.Add('dependency content changed') }
        if ([string]$lineage.policyFingerprint -cne $policyHash) { $entryIssues.Add('change policy changed') }
        if ($Kind -eq 'check') {
            if ([string]$lineage.execution.command -cne [string]$Entry.command) { $entryIssues.Add('command does not match evidence') }
            if ([string]$lineage.execution.commandFingerprint -cne (Get-Hash ([string]$Entry.command))) { $entryIssues.Add('command fingerprint is invalid') }
            if ([string]$lineage.kind -eq 'executed-check' -and $null -ne $lineage.artifact) {
                $artifactPath = [string]$lineage.artifact.path
                $expectedPrefix = if (-not [string]::IsNullOrWhiteSpace($normalizedWorkspacePath)) { "$normalizedWorkspacePath/logs/" } else { '.artifacts/llm-wiki/' }
                if (-not $artifactPath.StartsWith($expectedPrefix, [StringComparison]::Ordinal)) {
                    $entryIssues.Add('execution artifact path is outside the evidence boundary')
                } else {
                    $absoluteArtifactPath = Join-Path $repositoryRoot $artifactPath
                    if (-not (Test-Path -LiteralPath $absoluteArtifactPath -PathType Leaf)) {
                        $entryIssues.Add('execution artifact is missing')
                    } elseif ((Get-FileHash -LiteralPath $absoluteArtifactPath -Algorithm SHA256).Hash.ToLowerInvariant() -cne [string]$lineage.artifact.sha256) {
                        $entryIssues.Add('execution artifact hash is invalid')
                    }
                }
            }
        }
        $currentRuntimeVersion = switch ([string]$lineage.execution.runtime) {
            'dotnet' { [string](& dotnet --version) }
            'npm' { [string](& npm --version) }
            default { [string]$PSVersionTable.PSVersion }
        }
        if ([string]$lineage.execution.runtimeVersion -cne $currentRuntimeVersion) { $entryIssues.Add('runtime version changed') }
        $currentOs = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription.Trim()
        $currentArchitecture = [string][System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
        if ([string]$lineage.environment.os -cne $currentOs -or [string]$lineage.environment.architecture -cne $currentArchitecture) {
            $entryIssues.Add('execution platform changed')
        }
        $compatibilityPayload = [pscustomobject][ordered]@{
            kind = [string]$lineage.kind
            id = [string]$Entry.id
            sourceRule = [string]$Requirement.sourceRule
            definition = $expectedDefinition
            command = [string]$lineage.execution.command
            dependencyContentFingerprint = [string]$lineage.dependencies.contentFingerprint
            policyFingerprint = [string]$lineage.policyFingerprint
            runtime = "$($lineage.execution.runtime)/$($lineage.execution.runtimeVersion)"
            platform = "$($lineage.environment.os)/$($lineage.environment.architecture)"
        }
        if ([string]$lineage.compatibilityFingerprint -cne (Get-Hash $compatibilityPayload)) { $entryIssues.Add('compatibility fingerprint is invalid') }
    }
    foreach ($message in $entryIssues) { $issues.Add("$Kind '$($Entry.id)': $message.") }
    $items.Add([pscustomobject][ordered]@{
        kind = $Kind
        id = [string]$Entry.id
        status = [string]$Entry.status
        valid = $entryIssues.Count -eq 0
        reusable = $entryIssues.Count -eq 0 -and [string]$Entry.status -in @('passed', 'not-applicable', 'completed')
        cacheReusable = $entryIssues.Count -eq 0 -and [string]$Entry.status -eq 'passed' -and
            [string]$lineage.kind -eq 'executed-check' -and [int]$lineage.execution.exitCode -eq 0 -and $null -ne $lineage.artifact
        issues = @($entryIssues)
        compatibilityFingerprint = $(if ($null -ne $lineage) { [string]$lineage.compatibilityFingerprint } else { '' })
    })
}

foreach ($entry in @($evidence.checks | Where-Object status -in @('passed', 'failed', 'not-applicable'))) {
    $requirement = $policy.requiredChecks | Where-Object id -eq $entry.id | Select-Object -First 1
    if ($null -eq $requirement) { $issues.Add("check '$($entry.id)': requirement is no longer active.") } else { Test-Lineage 'check' $entry $requirement }
}
foreach ($entry in @($evidence.reviews | Where-Object status -in @('completed', 'not-applicable'))) {
    $requirement = $policy.reviewObligations | Where-Object id -eq $entry.id | Select-Object -First 1
    if ($null -eq $requirement) { $issues.Add("review '$($entry.id)': requirement is no longer active.") } else { Test-Lineage 'review' $entry $requirement }
}
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    workspace = $normalizedWorkspacePath
    evidencePath = $normalizedEvidencePath
    valid = $issues.Count -eq 0
    resolvedCount = $items.Count
    reusableCount = @($items | Where-Object reusable).Count
    invalidCount = @($items | Where-Object { -not $_.valid }).Count
    issues = @($issues)
    items = @($items)
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
} else {
    Write-Host "Evidence lineage: valid=$($result.valid), reusable=$($result.reusableCount)/$($result.resolvedCount)"
    foreach ($issue in $issues) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
