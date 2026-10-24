[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
$pipelinePath = Join-Path $PSScriptRoot 'Invoke-LlmWikiIndexPipeline.ps1'
$pipelineText = [IO.File]::ReadAllText($pipelinePath)
$ast = [Management.Automation.Language.Parser]::ParseFile($pipelinePath, [ref]$null, [ref]$null)
foreach ($definition in $ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] }, $true)) {
    . ([scriptblock]::Create($definition.Extent.Text))
}
$fixtureParent = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../.artifacts/llm-wiki/index-recovery-fixtures'))
$fixture = Join-Path $fixtureParent ([guid]::NewGuid().ToString('N'))
$repositoryRoot = $fixture
$generatedRoot = Join-Path $fixture '.llm-wiki/generated'
$transactionStateRoot = Join-Path $fixture '.git/llm-wiki/index-transactions'
$null = New-Item -ItemType Directory -Path $generatedRoot, $transactionStateRoot -Force
$script:copyCounter = 0
$script:failCopyAt = 0
function Copy-Item {
    [CmdletBinding()]
    param([string]$LiteralPath, [string]$Destination, [switch]$Recurse, [switch]$Force)
    $script:copyCounter++
    if ($script:failCopyAt -gt 0 -and $script:copyCounter -eq $script:failCopyAt) { throw 'Injected index copy failure.' }
    Microsoft.PowerShell.Management\Copy-Item -LiteralPath $LiteralPath -Destination $Destination -Recurse:$Recurse -Force:$Force
}
function Assert-Recovery([bool]$Value, [string]$Message) { if (-not $Value) { throw $Message } }
function Write-Fixture([string]$A = 'A', [string]$B = 'Б') {
    $null = New-Item -ItemType Directory -Path $generatedRoot -Force
    [IO.File]::WriteAllText((Join-Path $generatedRoot 'a.json'), $A, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $generatedRoot 'данные.json'), $B, [Text.UTF8Encoding]::new($false))
}
function Assert-Contents([string]$A = 'A', [string]$B = 'Б') {
    Assert-Recovery (@(Get-ChildItem -LiteralPath $generatedRoot -File).Count -eq 2) 'Index recovery lost or added files.'
    Assert-Recovery ([IO.File]::ReadAllText((Join-Path $generatedRoot 'a.json')) -ceq $A) 'Index recovery changed the first file.'
    Assert-Recovery ([IO.File]::ReadAllText((Join-Path $generatedRoot 'данные.json')) -ceq $B) 'Index recovery changed Unicode file bytes.'
}
function Invoke-LlmWikiGitCommand { [pscustomobject]@{ Lines = @(Join-Path $fixture '.git') } }

try {
    # Execute the actual pipeline try/catch/finally with no generators, failing
    # its second initial backup copy. No production repository is used.
    Write-Fixture
    $outer = $ast.FindAll({ param($node) $node -is [Management.Automation.Language.TryStatementAst] -and $node.CatchClauses.Count -gt 0 -and $node.CatchClauses[0].Body.Extent.Text.Contains('if ($transactionNeedsRecovery)') }, $true)[0]
    Assert-Recovery ($null -ne $outer) 'Pipeline transaction boundary was not found.'
    $Check = $false; $ReuseUnchangedChecks = $false; $selectedToolNames = @(); $stages = @()
    $updateLock = $null; $transactionRoot = $null; $backupSnapshot = $null; $transactionNeedsRecovery = $false
    $script:failCopyAt = 2
    $failed = $false
    try { . ([scriptblock]::Create($outer.Extent.Text)) } catch { $failed = $_.Exception.Message -like '*Injected index copy failure*' }
    Assert-Recovery $failed 'Initial backup failure was not preserved.'
    Assert-Contents
    Assert-Recovery (@(Get-ChildItem -LiteralPath $transactionStateRoot -Directory).Count -eq 0) 'Incomplete initial transaction was not cleaned safely.'
    $script:failCopyAt = 0

    $publicationRoot = Join-Path $transactionStateRoot ([guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $publicationRoot -Force
    Write-IndexTransactionState $publicationRoot ([ordered]@{ status = 'initializing' })
    Write-IndexTransactionState $publicationRoot ([ordered]@{ status = 'in-progress' })
    $publicationPath = Join-Path $publicationRoot 'state.json'
    $published = [IO.File]::ReadAllText($publicationPath)
    Assert-Recovery (($published | ConvertFrom-Json).status -ceq 'in-progress') 'State replacement did not publish the complete new state.'
    $guard = [IO.File]::Open($publicationPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $failed = $false
    try { Write-IndexTransactionState $publicationRoot ([ordered]@{ status = 'committed' }) } catch { $failed = $true } finally { $guard.Dispose() }
    if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
        Assert-Recovery ($failed -and [IO.File]::ReadAllText($publicationPath) -ceq $published) 'Failed atomic state publication lost the previously complete state.'
    }
    Remove-IndexTransactionDirectory $publicationRoot $transactionStateRoot

    $transactionRoot = Join-Path $transactionStateRoot ([guid]::NewGuid().ToString('N'))
    $backup = New-IndexSnapshot $generatedRoot (Join-Path $transactionRoot 'backup')
    Write-Fixture 'stage-A' 'стадия-Б'
    $checkpointName = 'checkpoints/' + [guid]::NewGuid().ToString('N')
    $checkpoint = New-IndexSnapshot $generatedRoot (Join-Path $transactionRoot $checkpointName)
    $state = [ordered]@{
        schemaVersion = 3; status = 'in-progress'; ownerPid = $PID; ownerStartTicks = 0
        backupManifestHash = $backup.manifestHash; checkpointSnapshot = $checkpointName; checkpointManifestHash = $checkpoint.manifestHash
        completedStage = 'verified-first-stage'
    }
    Write-IndexTransactionState $transactionRoot $state

    # A later partial copy is not referenced by the published state. Recovery
    # must select the prior complete immutable checkpoint.
    $pending = Join-Path $transactionRoot ('checkpoints/' + [guid]::NewGuid().ToString('N') + '.pending-abandoned')
    $null = New-Item -ItemType Directory -Path (Join-Path $pending 'content') -Force
    [IO.File]::WriteAllText((Join-Path $pending 'content/a.json'), 'partial')
    Write-Fixture 'broken-A' 'неполно'
    Restore-OrphanedIndexTransaction $transactionStateRoot $generatedRoot
    Assert-Contents 'stage-A' 'стадия-Б'

    # A damaged completed checkpoint must fall back to the original verified
    # backup; corrupted fallback must leave the current tree untouched.
    $transactionRoot = Join-Path $transactionStateRoot ([guid]::NewGuid().ToString('N'))
    Write-Fixture
    $backup = New-IndexSnapshot $generatedRoot (Join-Path $transactionRoot 'backup')
    $checkpointName = 'checkpoints/' + [guid]::NewGuid().ToString('N')
    $checkpoint = New-IndexSnapshot $generatedRoot (Join-Path $transactionRoot $checkpointName)
    $state.backupManifestHash = $backup.manifestHash; $state.checkpointSnapshot = $checkpointName; $state.checkpointManifestHash = $checkpoint.manifestHash
    Write-IndexTransactionState $transactionRoot $state
    [IO.File]::WriteAllText((Join-Path $checkpoint.path 'content/a.json'), 'corrupt')
    $chosen = Get-IndexRecoverySnapshot ([pscustomobject]$state) $transactionRoot
    Assert-Recovery ($chosen.path -ceq $backup.path) 'Corrupt checkpoint did not fall back to the verified backup.'
    [IO.File]::WriteAllText((Join-Path $backup.path 'content/a.json'), 'corrupt')
    Write-Fixture 'current-A' 'текущее-Б'
    $failed = $false
    try { Restore-OrphanedIndexTransaction $transactionStateRoot $generatedRoot } catch { $failed = $_.Exception.Message -like '*No complete verified*' }
    Assert-Recovery $failed 'Corrupt recovery snapshots were accepted.'
    Assert-Contents 'current-A' 'текущее-Б'
    Assert-Recovery (Test-Path -LiteralPath $transactionRoot) 'Corrupt transaction evidence was discarded.'
    Remove-IndexTransactionDirectory $transactionRoot $transactionStateRoot

    # Legacy in-progress state proves only its original backup completed; its
    # partial replace-in-place checkpoint must never be preferred.
    $transactionRoot = Join-Path $transactionStateRoot ([guid]::NewGuid().ToString('N'))
    $legacyBackup = Join-Path $transactionRoot 'generated'
    $legacyCheckpoint = Join-Path $transactionRoot 'checkpoint'
    $null = New-Item -ItemType Directory -Path $legacyBackup, $legacyCheckpoint -Force
    Write-Fixture
    Get-ChildItem -LiteralPath $generatedRoot | Microsoft.PowerShell.Management\Copy-Item -Destination $legacyBackup -Recurse
    [IO.File]::WriteAllText((Join-Path $legacyCheckpoint 'a.json'), 'partial')
    Write-IndexTransactionState $transactionRoot ([ordered]@{ schemaVersion = 2; status = 'in-progress'; ownerPid = 2147483647 })
    Write-Fixture 'changed' 'изменено'
    Restore-OrphanedIndexTransaction $transactionStateRoot $generatedRoot
    Assert-Contents

    # Exercise the real batch cleanup when starting a later writer fails.
    # Inject only its executable name; the earlier process and finally block
    # are production code, so a surviving writer would race transaction restore.
    $toolsRoot = Join-Path $fixture 'tools'
    $null = New-Item -ItemType Directory -Path $toolsRoot -Force
    $fixturePidPath = Join-Path $fixture 'writer.pid'
    [IO.File]::WriteAllText((Join-Path $toolsRoot 'writer.ps1'), "[IO.File]::WriteAllText('$($fixturePidPath.Replace("'", "''"))', [string]`$PID)`nStart-Sleep -Seconds 120")
    $batchDefinition = $ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-PipelineBatch' }, $true)[0].Extent.Text
    $injection = @'
if ($toolName -eq 'fail-start.ps1') {
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    while (-not [IO.File]::Exists($fixturePidPath) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 50 }
    if (-not [IO.File]::Exists($fixturePidPath)) { throw 'Earlier pipeline writer did not start.' }
    $startInfo.FileName = Join-Path $fixture 'missing-executable'
}
'@
    . ([scriptblock]::Create($batchDefinition.Replace('$startInfo.FileName = $shellPath', ('$startInfo.FileName = $shellPath' + "`n" + $injection))))
    $shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
    $MaxConcurrency = 2; $ToolTimeoutSeconds = 20
    $script:completedToolTimings = [Collections.Generic.List[object]]::new()
    $failed = $false
    try { Invoke-PipelineBatch 'startup-failure' @('writer.ps1', 'fail-start.ps1') $false } catch { $failed = $true }
    Assert-Recovery ($failed -and [IO.File]::Exists($fixturePidPath)) 'Pipeline startup-failure fixture did not run its earlier writer.'
    $writer = $null
    try { $writer = [Diagnostics.Process]::GetProcessById([int]([IO.File]::ReadAllText($fixturePidPath))) } catch [ArgumentException] { }
    if ($writer) {
        try { Assert-Recovery (Wait-LlmWikiProcessTermination $writer) 'Pipeline startup failure left an index writer alive.' }
        finally { if (-not $writer.HasExited) { Stop-LlmWikiProcessTree $writer }; $writer.Dispose() }
    }

    Assert-Recovery ($pipelineText.IndexOf('Restore-OrphanedIndexTransaction -TransactionStateRoot', [StringComparison]::Ordinal) -gt $pipelineText.IndexOf('$lock = Enter-IndexUpdateLock $lockPath', [StringComparison]::Ordinal)) 'Recovery runs before the exclusive writer lock.'
    Assert-Recovery ($pipelineText.IndexOf('Write-PipelineCacheReceipt $finalPipelineCacheState', [StringComparison]::Ordinal) -lt $pipelineText.IndexOf('if ($updateLock) { $updateLock.Dispose() }', [StringComparison]::Ordinal)) 'Cache receipt is published outside the writer lock.'
    Write-Host 'LLM Wiki index checkpoint regression passed: failed initial copy, interrupted checkpoint, corruption, legacy fallback, Unicode and writer-lock publication.'
} finally {
    Remove-IndexTransactionDirectory $fixture $fixtureParent
}
