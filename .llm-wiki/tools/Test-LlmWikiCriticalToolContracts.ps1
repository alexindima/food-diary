[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Assert-CriticalTool([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

# Directly inventory the remaining lifecycle/helper tools that quality indexing
# cannot prove through dynamic facade dispatch. Each entry asserts its stable
# contract marker, while behavior-heavy tools receive executable checks below.
foreach ($contract in @(
    @{ file = 'Add-LlmWikiSourceReview.ps1'; marker = 'source-impact-reviews.json' }
    @{ file = 'Clear-LlmWikiReadOnlySnapshotCache.ps1'; marker = 'fooddiary-llm-wiki-read-only' }
    @{ file = 'Get-LlmWikiPhaseStatus.ps1'; marker = "ValidateSet('status', 'next', 'complete')" }
    @{ file = 'LlmWikiChangeSetSnapshot.ps1'; marker = 'Get-LlmWikiChangeSetSnapshot' }
    @{ file = 'LlmWikiGitRenames.ps1'; marker = 'ConvertFrom-LlmWikiGitNameStatus' }
    @{ file = 'LlmWikiImplementationBrief.ps1'; marker = 'Normalize-LlmWikiImplementationBrief' }
    @{ file = 'Measure-LlmWikiCodeGraph.ps1'; marker = 'incremental-build' }
    @{ file = 'Update-LlmWikiTaskEvidence.ps1'; marker = 'PacketPath must belong to WorkspacePath' }
)) {
    $contractSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot $contract.file) -Raw
    Assert-CriticalTool ($contractSource.Contains($contract.marker)) "Critical tool contract marker is missing: $($contract.file)"
}

$helpOutput = @(& (Join-Path $PSScriptRoot 'Show-LlmWikiHelp.ps1') -Tier core 6>&1 | ForEach-Object { [string]$_ })
Assert-CriticalTool ($helpOutput -contains 'Command stability tiers: core, governed, experimental.') 'Registry-backed compact help omitted stability tiers.'

# Get-LlmWikiOwnershipImpact: synthetic input must stay bounded to the supplied
# path and resolve its nearest scoped guide without consulting the worktree diff.
$ownershipDiff = [pscustomobject]@{
    changedPaths = @('.llm-wiki/wiki.ps1')
    modules = @()
}
$ownership = & (Join-Path $PSScriptRoot 'Get-LlmWikiOwnershipImpact.ps1') `
    -DiffInput $ownershipDiff -Format Json | ConvertFrom-Json
Assert-CriticalTool (@($ownership.changedPaths).Count -eq 1) 'Ownership impact did not preserve the injected scope.'
Assert-CriticalTool ($ownership.ownershipGuides[0].guide -eq 'AGENTS.md') 'Wiki tooling ownership did not resolve the repository guide.'

# Get-LlmWikiVerificationStageFingerprint: identical material is stable and
# command arguments participate in the receipt identity.
$fingerprintA = & (Join-Path $PSScriptRoot 'Get-LlmWikiVerificationStageFingerprint.ps1') `
    -Stage 'page contracts' -Arguments @{ mode = 'a' } -Format Json | ConvertFrom-Json
$fingerprintB = & (Join-Path $PSScriptRoot 'Get-LlmWikiVerificationStageFingerprint.ps1') `
    -Stage 'page contracts' -Arguments @{ mode = 'a' } -Format Json | ConvertFrom-Json
$fingerprintC = & (Join-Path $PSScriptRoot 'Get-LlmWikiVerificationStageFingerprint.ps1') `
    -Stage 'page contracts' -Arguments @{ mode = 'b' } -Format Json | ConvertFrom-Json
Assert-CriticalTool ($fingerprintA.fingerprint -eq $fingerprintB.fingerprint) 'Verification-stage fingerprint is not deterministic.'
Assert-CriticalTool ($fingerprintA.fingerprint -ne $fingerprintC.fingerprint) 'Verification-stage fingerprint ignored command arguments.'

# Invoke-LlmWikiDeliveryFinalization: reject paths outside the durable task root
# before any lifecycle component can mutate state.
$invalidWorkspaceRejected = $false
try {
    & (Join-Path $PSScriptRoot 'Invoke-LlmWikiDeliveryFinalization.ps1') `
        -WorkspacePath '../outside' -Format Json | Out-Null
} catch {
    $invalidWorkspaceRejected = $_.Exception.Message -match 'directly inside'
}
Assert-CriticalTool $invalidWorkspaceRejected 'Delivery finalization accepted an out-of-root workspace.'

# Start-LlmWikiVerifyWorker: execute it out-of-process because the worker owns
# exit-code propagation and transcript lifetime.
$fixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'critical-tool-contracts'
try {
    $fakeWikiPath = Join-Path $fixtureRoot 'fake-wiki.ps1'
    $argumentsPath = Join-Path $fixtureRoot 'arguments.json'
    $logPath = Join-Path $fixtureRoot 'worker.log'
    [IO.File]::WriteAllText($fakeWikiPath, "param([string]`$Command, [string]`$Probe)`nif (`$Command -ne 'verify' -or `$Probe -ne 'ok') { throw 'worker arguments were not forwarded' }`nWrite-Host 'worker-probe-ok'`n", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($argumentsPath, '{"Probe":"ok"}', [Text.UTF8Encoding]::new($false))
    $shellPath = (Get-Process -Id $PID).Path
    $process = Start-Process -FilePath $shellPath -ArgumentList @(
        '-NoLogo', '-NoProfile', '-File', (Join-Path $PSScriptRoot 'Start-LlmWikiVerifyWorker.ps1'),
        '-WikiPath', $fakeWikiPath, '-ArgumentsPath', $argumentsPath, '-LogPath', $logPath
    ) -Wait -PassThru -WindowStyle Hidden
    Assert-CriticalTool ($process.ExitCode -eq 0) 'Verify worker did not propagate a successful invocation.'
    Assert-CriticalTool ((Get-Content -LiteralPath $logPath -Raw) -match 'worker-probe-ok') 'Verify worker transcript omitted child output.'
} finally {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}

# LlmWikiInProcessSqlite is referenced explicitly here; its full build/load and
# SQL parity contract remains exercised by Test-LlmWikiDomainDataSqlParity.ps1.
$sqliteSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'LlmWikiInProcessSqlite.ps1') -Raw
Assert-CriticalTool ($sqliteSource -match 'Initialize-LlmWikiInProcessSqlite' -and $sqliteSource -match 'AppDomain.*SetData') 'In-process SQLite cache contract is missing.'

Write-Host 'LLM Wiki critical tool contracts passed: ownership, fingerprints, finalization boundaries, worker propagation, and SQLite cache declaration.'
