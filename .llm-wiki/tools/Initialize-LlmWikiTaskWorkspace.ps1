[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Objective,
    [Parameter(Mandatory = $true)]
    [string[]]$Criterion,
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [Alias('ProposedPath')]
    [string[]]$PlannedPath = @(),
    [string[]]$AllowedPath = @(),
    [string[]]$ExcludedPath = @()
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$workspacePolicySnapshot = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -WithFingerprint -Format Json | ConvertFrom-Json
$workspacePolicy = $workspacePolicySnapshot.policy

if ([System.IO.Path]::IsPathRooted($WorkspacePath)) {
    throw 'WorkspacePath must be repository-relative.'
}
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+(?:/.*)?$') {
    throw 'WorkspacePath must be inside .artifacts/llm-wiki/tasks/<task-name>.'
}
if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'Objective must not be empty.' }
$criteria = @($Criterion | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($criteria.Count -eq 0) { throw 'At least one acceptance criterion is required.' }
$ChangedPath = @($ChangedPath | Where-Object { $_ } | ForEach-Object { ([string]$_).Replace('\', '/') } | Sort-Object -Unique)
$PlannedPath = @($PlannedPath | Where-Object { $_ } | ForEach-Object { ([string]$_).Replace('\', '/').TrimEnd('/') } | Sort-Object -Unique)

$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
if (Test-Path -LiteralPath $absoluteWorkspacePath) {
    throw "Task workspace already exists: $normalizedWorkspacePath"
}

$tasksRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/tasks'
if (-not (Test-Path -LiteralPath $tasksRoot)) {
    New-Item -ItemType Directory -Path $tasksRoot -Force | Out-Null
}
$temporaryName = '.task-start-' + [guid]::NewGuid().ToString('N')
$temporaryAbsolutePath = Join-Path $tasksRoot $temporaryName
$temporaryRelativePath = ".artifacts/llm-wiki/tasks/$temporaryName"
New-Item -ItemType Directory -Path $temporaryAbsolutePath | Out-Null

function Get-TemporaryArtifactPath([string]$Name) {
    return "$temporaryRelativePath/$Name"
}

try {
    $packetArguments = @{
        BaseRef = $BaseRef
        Objective = $Objective
        Format = 'Json'
        OutputPath = (Get-TemporaryArtifactPath 'change-packet.json')
    }
    if ($PSBoundParameters.ContainsKey('HeadRef')) { $packetArguments.HeadRef = $HeadRef }
    if ($PSBoundParameters.ContainsKey('ChangedPath')) { $packetArguments.ChangedPath = $ChangedPath }
    & (Join-Path $PSScriptRoot 'Get-LlmWikiChangePacket.ps1') @packetArguments | Out-Null
    $packet = Get-Content -LiteralPath (Join-Path $temporaryAbsolutePath 'change-packet.json') -Raw | ConvertFrom-Json

    $scopeRoots = if ($PlannedPath.Count -gt 0) { @($PlannedPath) } elseif ($ChangedPath.Count -gt 0) { @($ChangedPath) } else { @($packet.diff.changedPaths) }
    $allowedPatterns = if ($AllowedPath.Count -gt 0) {
        @($AllowedPath)
    } else {
        @($scopeRoots | ForEach-Object {
            $normalized = ([string]$_).Replace('\', '/').TrimEnd('/')
            if ([IO.Path]::GetExtension($normalized)) { '^' + [regex]::Escape($normalized) + '$' }
            else { '^' + [regex]::Escape($normalized) + '(?:/.*)?$' }
        })
    }
    if ($allowedPatterns.Count -eq 0) {
        throw 'No changed paths were detected; provide at least one -AllowedPath regex.'
    }

    & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskContract.ps1') init `
        -Path (Get-TemporaryArtifactPath 'task-contract.json') `
        -Objective $Objective `
        -BaseRef $BaseRef `
        -AllowedPath $allowedPatterns `
        -ExcludedPath $ExcludedPath | Out-Null

    $changeArguments = @{ BaseRef = $BaseRef; Objective = $Objective }
    if ($PSBoundParameters.ContainsKey('HeadRef')) { $changeArguments.HeadRef = $HeadRef }
    if ($PSBoundParameters.ContainsKey('ChangedPath')) { $changeArguments.ChangedPath = $ChangedPath }

    & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeManifest.ps1') init `
        @changeArguments `
        -Path (Get-TemporaryArtifactPath 'change-manifest.json') `
        -PlannedPath $scopeRoots `
        -AllowedPath $allowedPatterns `
        -ExcludedPath $ExcludedPath `
        -EvidencePath "$normalizedWorkspacePath/evidence.json" | Out-Null

    & (Join-Path $PSScriptRoot 'Manage-LlmWikiAcceptanceMatrix.ps1') init `
        @changeArguments `
        -Path (Get-TemporaryArtifactPath 'acceptance-matrix.json') `
        -Criterion $criteria `
        -EvidencePath "$normalizedWorkspacePath/evidence.json" | Out-Null

    $evidenceArguments = @{ BaseRef = $BaseRef; Path = (Get-TemporaryArtifactPath 'evidence.json') }
    if ($PSBoundParameters.ContainsKey('HeadRef')) { $evidenceArguments.HeadRef = $HeadRef }
    if ($PSBoundParameters.ContainsKey('ChangedPath')) { $evidenceArguments.ChangedPath = $ChangedPath }
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvidence.ps1') init @evidenceArguments | Out-Null
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') init `
        -WorkspacePath $temporaryRelativePath | Out-Null

    $reportArguments = @{
        PacketInput = $packet
        ManifestPath = (Get-TemporaryArtifactPath 'change-manifest.json')
        AcceptancePath = (Get-TemporaryArtifactPath 'acceptance-matrix.json')
        EvidencePath = (Get-TemporaryArtifactPath 'evidence.json')
        OutputPath = (Get-TemporaryArtifactPath 'review-report.md')
    }
    & (Join-Path $PSScriptRoot 'Get-LlmWikiReviewReport.ps1') @reportArguments | Out-Null

    $head = git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD.' }
    $workspace = [ordered]@{
        schemaVersion = [int]$workspacePolicy.workspace.latestSchemaVersion
        format = [string]$workspacePolicy.workspace.format
        policyFingerprint = [string]$workspacePolicySnapshot.fingerprint
        policySnapshot = $workspacePolicy
        policyValidatedAtUtc = [DateTime]::UtcNow.ToString('o')
        objective = $Objective
        createdAtUtc = [DateTime]::UtcNow.ToString('o')
        git = [ordered]@{
            base = $BaseRef
            headAtStart = [string]$head
        }
        packetFingerprint = $packet.fingerprint
        initialPacketFingerprint = $packet.fingerprint
        currentPacketFingerprint = $packet.fingerprint
        artifacts = [ordered]@{
            packet = "$normalizedWorkspacePath/change-packet.json"
            taskContract = "$normalizedWorkspacePath/task-contract.json"
            manifest = "$normalizedWorkspacePath/change-manifest.json"
            acceptance = "$normalizedWorkspacePath/acceptance-matrix.json"
            evidence = "$normalizedWorkspacePath/evidence.json"
            journal = "$normalizedWorkspacePath/journal.json"
            report = "$normalizedWorkspacePath/review-report.md"
        }
        artifactSchemaVersions = [ordered]@{
            packet = [int]$workspacePolicy.workspace.artifactSchemaVersions.packet
            taskContract = [int]$workspacePolicy.workspace.artifactSchemaVersions.taskContract
            manifest = [int]$workspacePolicy.workspace.artifactSchemaVersions.manifest
            acceptance = [int]$workspacePolicy.workspace.artifactSchemaVersions.acceptance
            evidence = [int]$workspacePolicy.workspace.artifactSchemaVersions.evidence
            journal = [int]$workspacePolicy.workspace.artifactSchemaVersions.journal
            report = [int]$workspacePolicy.workspace.artifactSchemaVersions.report
        }
        migrations = @()
    }
    $workspaceJson = $workspace | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText(
        (Join-Path $temporaryAbsolutePath 'workspace.json'),
        $workspaceJson + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false))

    $finalParent = Split-Path -Parent $absoluteWorkspacePath
    if (-not (Test-Path -LiteralPath $finalParent)) {
        New-Item -ItemType Directory -Path $finalParent -Force | Out-Null
    }
    Move-Item -LiteralPath $temporaryAbsolutePath -Destination $absoluteWorkspacePath
    Write-Host "Initialized LLM Wiki task workspace: $normalizedWorkspacePath"
    Write-Host "Artifacts: packet, task contract, manifest, acceptance matrix, evidence, and review report."
    Write-Host "Planned paths: $($scopeRoots.Count)."
    if ($scopeRoots.Count -eq 0) { Write-Warning 'Governed task workspace has no planned paths; provide -PlannedPath or -ChangedPath.' }
} catch {
    if (Test-Path -LiteralPath $temporaryAbsolutePath) {
        Remove-Item -LiteralPath $temporaryAbsolutePath -Recurse -Force
    }
    throw
}
