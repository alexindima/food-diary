[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$AffectedOnly,
    [switch]$Plan,
    [switch]$DeferPossiblyConcurrentStale,
    [switch]$ReuseUnchangedChecks,
    [switch]$RequiredOnly,
    [ValidateSet('All', 'Backend', 'Frontend')]
    [string]$Area = 'All',
    [string]$BaseRef = 'HEAD',
    [string[]]$ChangedPath,
    [ValidateRange(1, 8)]
    [int]$MaxConcurrency = 4,
    [ValidateRange(30, 3600)]
    [int]$ToolTimeoutSeconds = 600
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiChangeSemantics.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGeneratedArtifacts.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$toolsRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$shellPath = [System.IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $toolsRoot '../..'))
$pipelineCacheState = $null

function Restore-OrphanedIndexTransaction([string]$TransactionStateRoot, [string]$GeneratedRoot) {
    if (-not (Test-Path -LiteralPath $TransactionStateRoot -PathType Container)) { return }
    foreach ($orphan in @(Get-ChildItem -LiteralPath $TransactionStateRoot -Directory -ErrorAction SilentlyContinue)) {
        $statePath = Join-Path $orphan.FullName 'state.json'
        $backupPath = Join-Path $orphan.FullName 'generated'
        if (-not (Test-Path -LiteralPath $statePath -PathType Leaf) -or -not (Test-Path -LiteralPath $backupPath -PathType Container)) { continue }
        try { $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json } catch { continue }
        if ([string]$state.status -ne 'in-progress') { continue }
        $ownerAlive = $false
        try { $ownerAlive = $null -ne (Get-Process -Id ([int]$state.ownerPid) -ErrorAction Stop) } catch { $ownerAlive = $false }
        if ($ownerAlive) { continue }
        Write-Warning "Recovering interrupted LLM Wiki index transaction $($orphan.Name) before running another update."
        foreach ($item in @(Get-ChildItem -LiteralPath $GeneratedRoot -Force)) { Remove-Item -LiteralPath $item.FullName -Recurse -Force }
        foreach ($item in @(Get-ChildItem -LiteralPath $backupPath -Force)) { Copy-Item -LiteralPath $item.FullName -Destination $GeneratedRoot -Recurse -Force }
        Remove-Item -LiteralPath $orphan.FullName -Recurse -Force
    }
}

function Get-StringSha256([string]$Value) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Value))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Get-PipelineCacheState([string[]]$ToolNames) {
    $repositoryInputs = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--cached', '--others', '--exclude-standard') -FailureMessage 'Unable to enumerate pipeline cache inputs.')
    $repositoryInputs = @($repositoryInputs | Where-Object {
        $_ -notmatch '^\.llm-wiki/(?:generated|reviews)/' -and
        $_ -notmatch '^\.artifacts/' -and
        $_ -notmatch '(?:^|/)(?:node_modules|bin|obj|dist|coverage|TestResults)/'
    })
    $generatedOutputs = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--cached', '--others', '--exclude-standard', '--', '.llm-wiki/generated/**') -FailureMessage 'Unable to enumerate pipeline cache outputs.')
    $toolSet = @($ToolNames | Sort-Object -Unique) -join '|'
    $toolSetHash = (Get-StringSha256 $toolSet).Substring(0, 16)
    return [pscustomobject]@{
        receiptPath = Join-Path $repositoryRoot ".artifacts/llm-wiki/index-cache/pipeline-$toolSetHash.json"
        inputFingerprint = Get-LlmWikiIndexInputFingerprint $repositoryRoot $repositoryInputs
        outputFingerprint = Get-LlmWikiIndexInputFingerprint $repositoryRoot $generatedOutputs
        toolSet = $toolSet
    }
}

function Get-WorkspaceChangedPaths {
    $paths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('diff', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--') -FailureMessage 'Unable to collect workspace changes for stale-index diagnostics.')
    $paths += @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'Unable to collect untracked paths for stale-index diagnostics.')
    return @($paths | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique)
}

if ($AffectedOnly -and -not $PSBoundParameters.ContainsKey('ChangedPath')) {
    $ChangedPath = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('diff', '--name-only', '--diff-filter=ACMRD', $BaseRef, '--') -FailureMessage "Unable to collect changed paths from '$BaseRef'.")
    if ($BaseRef -eq 'HEAD') {
        $ChangedPath += @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'Unable to collect untracked paths.')
    }
}

$selectedTools = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$allIndexTools = @(
    'Build-LlmWikiCatalog.ps1', 'Build-LlmWikiSymbolIndex.ps1', 'Build-LlmWikiFrontendIndex.ps1',
    'Build-LlmWikiFrontendContractIndex.ps1', 'Build-LlmWikiDomainDataIndex.ps1',
    'Build-LlmWikiConfigurationIndex.ps1', 'Build-LlmWikiRuntimeTopology.ps1',
    'Build-LlmWikiSensitiveDataIndex.ps1', 'Build-LlmWikiBackendContractIndex.ps1',
    'Build-LlmWikiQualityIndex.ps1', 'Build-LlmWikiModulePages.ps1',
    'Build-LlmWikiArchitectureHealthIndex.ps1'
)
$analyticalIndexTools = @(
    'Build-LlmWikiQualityIndex.ps1',
    'Build-LlmWikiModulePages.ps1',
    'Build-LlmWikiArchitectureHealthIndex.ps1'
)
function Add-IndexTool([string]$Name) {
    $null = $selectedTools.Add($Name)
}
function Add-IndexToolWithDependents([string]$Name) {
    Add-IndexTool $Name
    switch ($Name) {
        'Build-LlmWikiCatalog.ps1' {
            Add-IndexTool 'Build-LlmWikiModulePages.ps1'
            Add-IndexTool 'Build-LlmWikiArchitectureHealthIndex.ps1'
        }
        'Build-LlmWikiSymbolIndex.ps1' {
            Add-IndexTool 'Build-LlmWikiBackendContractIndex.ps1'
            Add-IndexTool 'Build-LlmWikiQualityIndex.ps1'
            Add-IndexTool 'Build-LlmWikiArchitectureHealthIndex.ps1'
        }
        { $_ -in @('Build-LlmWikiFrontendContractIndex.ps1', 'Build-LlmWikiBackendContractIndex.ps1', 'Build-LlmWikiQualityIndex.ps1') } {
            Add-IndexTool 'Build-LlmWikiArchitectureHealthIndex.ps1'
        }
    }
}
if ($AffectedOnly) {
    $normalizedChangedPaths = @(
        $ChangedPath |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_ -split '[\r\n;]+' } |
            ForEach-Object { $_.Replace('\', '/') } |
            Sort-Object -Unique
    )
    if ($normalizedChangedPaths.Count -eq 0) {
        Write-Host 'LLM Wiki affected index pipeline: no changed paths; nothing to do.'
        exit 0
    }
    $requiresAllIndexes = @($normalizedChangedPaths | Where-Object {
        $_ -match '^Directory\.|\.slnx?$|^\.llm-wiki/tools/LlmWikiJson\.ps1$'
    }).Count -gt 0
    if ($requiresAllIndexes) {
        foreach ($tool in $allIndexTools) { Add-IndexTool $tool }
    } else {
        foreach ($wikiToolPath in @($normalizedChangedPaths | Where-Object { $_ -match '^\.llm-wiki/tools/Build-LlmWiki[^/]+\.ps1$' })) {
            $builderName = Split-Path -Leaf $wikiToolPath
            if ($builderName -in $allIndexTools) {
                Add-IndexToolWithDependents $builderName
            } else {
                foreach ($tool in $allIndexTools) { Add-IndexTool $tool }
            }
        }

        $frontendPaths = @($normalizedChangedPaths | Where-Object { $_ -match '^FoodDiary\.Web\.Client/' })
        $csharpPaths = @($normalizedChangedPaths | Where-Object { $_ -match '\.cs$' })
        $csharpTestPaths = @($csharpPaths | Where-Object {
            $_ -match '(?i)(^|/)(tests?|__tests__)/' -or $_ -match '(?i)\.Tests?/' -or $_ -match '(?i)(?:^|/)[^/]*(?:Tests?|Specs?)\.cs$'
        })
        $productionCSharpPaths = @($csharpPaths | Where-Object { $_ -notin $csharpTestPaths })
        $hasCSharpProjectChange = @($normalizedChangedPaths | Where-Object { $_ -match '\.csproj$' }).Count -gt 0
        $frontendTests = @($frontendPaths | Where-Object { $_ -match '(?:^|/)\w[^/]*\.(?:spec|test)\.ts$' })
        $frontendSources = @($frontendPaths | Where-Object { $_ -match '\.ts$' -and $_ -notmatch '(?:^|/)\w[^/]*\.(?:spec|test)\.ts$' })
        $frontendTemplates = @($frontendPaths | Where-Object { $_ -match '\.html$' })
        $productionChangedPaths = @($normalizedChangedPaths | Where-Object { $_ -notin $csharpTestPaths -and $_ -notin $frontendTests })

        if ($frontendTests.Count -gt 0) {
            Add-IndexTool 'Build-LlmWikiQualityIndex.ps1'
        }
        if ($frontendSources.Count -gt 0) {
            Add-IndexTool 'Build-LlmWikiFrontendIndex.ps1'
            Add-IndexToolWithDependents 'Build-LlmWikiFrontendContractIndex.ps1'
            Add-IndexTool 'Build-LlmWikiQualityIndex.ps1'
        }
        foreach ($templatePath in $frontendTemplates) {
            $templateDiff = Get-LlmWikiPathDiff -RepositoryRoot $repositoryRoot -Path $templatePath
            if (-not (Test-LlmWikiPresentationOnlyTemplateDiff -DiffText $templateDiff)) {
                Add-IndexToolWithDependents 'Build-LlmWikiFrontendContractIndex.ps1'
            }
        }
        if ($csharpTestPaths.Count -gt 0) {
            Add-IndexTool 'Build-LlmWikiQualityIndex.ps1'
        }
        if ($productionCSharpPaths.Count -gt 0 -or $hasCSharpProjectChange) {
            Add-IndexToolWithDependents 'Build-LlmWikiCatalog.ps1'
            Add-IndexToolWithDependents 'Build-LlmWikiSymbolIndex.ps1'
            Add-IndexTool 'Build-LlmWikiSensitiveDataIndex.ps1'
        }
        if (@($normalizedChangedPaths | Where-Object { $_ -in @(
            'docs/architecture/module-dependencies.json',
            'docs/architecture/backend-modules.json',
            'docs/backend/BACKEND_MODULE_OWNERSHIP.md'
        ) }).Count -gt 0) {
            Add-IndexToolWithDependents 'Build-LlmWikiCatalog.ps1'
            Add-IndexTool 'Build-LlmWikiModulePages.ps1'
            Add-IndexTool 'Build-LlmWikiArchitectureHealthIndex.ps1'
        }
        if (@($productionChangedPaths | Where-Object {
            $_ -match 'Domain/|Persistence/|Migrations?/|DbContext|Configuration\.cs$'
        }).Count -gt 0) { Add-IndexTool 'Build-LlmWikiDomainDataIndex.ps1' }
        if (@($productionChangedPaths | Where-Object {
            $_ -match 'appsettings|\.env|Options\.cs$|docker-compose|\.github/workflows/'
        }).Count -gt 0) { Add-IndexTool 'Build-LlmWikiConfigurationIndex.ps1' }
        if (@($productionChangedPaths | Where-Object {
            $_ -match 'HostedService|Recurring|Webhook|Integrations/|JobManager/|docker-compose'
        }).Count -gt 0) { Add-IndexTool 'Build-LlmWikiRuntimeTopology.ps1' }
    }
    Write-Host "LLM Wiki affected index pipeline: $($normalizedChangedPaths.Count) changed path(s), $($selectedTools.Count) selected tool(s)."
    if ($RequiredOnly) {
        foreach ($analyticalTool in $analyticalIndexTools) { $null = $selectedTools.Remove($analyticalTool) }
        Write-Host "LLM Wiki required-index mode: deferred analytical indexes to the publication/CI gate; $($selectedTools.Count) required tool(s) remain."
    }
    if ($Plan) {
        Write-Output "Affected path count: $($normalizedChangedPaths.Count)"
        Write-Output "Affected index tools: $(@($selectedTools | Sort-Object) -join ', ')"
        exit 0
    }
}

$selectedToolNames = if ($AffectedOnly) { @($selectedTools | Sort-Object) } else { @($allIndexTools | Sort-Object) }
$coldCostSeconds = @{
    'Build-LlmWikiQualityIndex.ps1' = 30
    'Build-LlmWikiBackendContractIndex.ps1' = 30
    'Build-LlmWikiFrontendContractIndex.ps1' = 30
    'Build-LlmWikiCatalog.ps1' = 15
    'Build-LlmWikiSymbolIndex.ps1' = 15
    'Build-LlmWikiModulePages.ps1' = 15
    'Build-LlmWikiArchitectureHealthIndex.ps1' = 10
}
$estimatedColdSeconds = (@($selectedToolNames | ForEach-Object { if ($coldCostSeconds.ContainsKey($_)) { [int]$coldCostSeconds[$_] } else { 5 } }) | Measure-Object -Sum).Sum
Write-Host "LLM Wiki index forecast: ~$estimatedColdSeconds cold second(s) for $($selectedToolNames.Count) generator(s); cache/no-op suppression can reduce this."
if (-not $RequiredOnly -and 'Build-LlmWikiQualityIndex.ps1' -in $selectedToolNames) {
    Write-Host 'Iteration hint: use update -AffectedOnly -ContractIndexesOnly while editing; run the full affected update once before handoff.'
}
if ($Area -eq 'Backend') {
    $selectedToolNames = @($selectedToolNames | Where-Object { $_ -notin @(
        'Build-LlmWikiFrontendIndex.ps1', 'Build-LlmWikiFrontendContractIndex.ps1',
        'Build-LlmWikiQualityIndex.ps1', 'Build-LlmWikiArchitectureHealthIndex.ps1'
    ) })
    Write-Host "LLM Wiki verification area: Backend ($($selectedToolNames.Count) generator(s)); frontend freshness is intentionally not evaluated."
} elseif ($Area -eq 'Frontend') {
    $selectedToolNames = @($selectedToolNames | Where-Object { $_ -in @(
        'Build-LlmWikiFrontendIndex.ps1', 'Build-LlmWikiFrontendContractIndex.ps1', 'Build-LlmWikiQualityIndex.ps1'
    ) })
    Write-Host "LLM Wiki verification area: Frontend ($($selectedToolNames.Count) generator(s)); backend freshness is intentionally not evaluated."
}
if ($ReuseUnchangedChecks -and $selectedToolNames.Count -gt 0) {
    $pipelineCacheState = Get-PipelineCacheState $selectedToolNames
    if ($Check -and (Test-Path -LiteralPath $pipelineCacheState.receiptPath -PathType Leaf)) {
        try {
            $pipelineReceipt = Get-Content -LiteralPath $pipelineCacheState.receiptPath -Raw | ConvertFrom-Json
            if ([int]$pipelineReceipt.schemaVersion -eq 1 -and
                [string]$pipelineReceipt.toolSet -ceq [string]$pipelineCacheState.toolSet -and
                [string]$pipelineReceipt.inputFingerprint -ceq [string]$pipelineCacheState.inputFingerprint -and
                [string]$pipelineReceipt.outputFingerprint -ceq [string]$pipelineCacheState.outputFingerprint) {
                Write-Host "LLM Wiki affected pipeline cache hit: $($selectedToolNames.Count) generator(s), source and generated hashes unchanged."
                exit 0
            }
        } catch { Write-Verbose "Ignoring invalid affected pipeline receipt: $($_.Exception.Message)" }
    }
}

$stages = @(
    [pscustomobject]@{
        name = 'source indexes'
        tools = @(
            'Build-LlmWikiCatalog.ps1'
            'Build-LlmWikiSymbolIndex.ps1'
            'Build-LlmWikiFrontendIndex.ps1'
            'Build-LlmWikiFrontendContractIndex.ps1'
            'Build-LlmWikiDomainDataIndex.ps1'
            'Build-LlmWikiConfigurationIndex.ps1'
            'Build-LlmWikiRuntimeTopology.ps1'
            'Build-LlmWikiSensitiveDataIndex.ps1'
        )
    }
    [pscustomobject]@{
        name = 'derived indexes'
        tools = @(
            'Build-LlmWikiBackendContractIndex.ps1'
            'Build-LlmWikiQualityIndex.ps1'
            'Build-LlmWikiModulePages.ps1'
        )
    }
    [pscustomobject]@{
        name = 'architecture health'
        tools = @('Build-LlmWikiArchitectureHealthIndex.ps1')
    }
)

function Invoke-PipelineBatch([string]$StageName, [string[]]$ToolNames, [bool]$CheckMode) {
    $workers = [System.Collections.Generic.List[object]]::new()
    foreach ($toolName in $ToolNames) {
        $scriptPath = [System.IO.Path]::GetFullPath((Join-Path $toolsRoot $toolName))
        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = $shellPath
        $startInfo.WorkingDirectory = $repositoryRoot
        $startInfo.UseShellExecute = $false
        $cacheableTools = @('Build-LlmWikiQualityIndex.ps1', 'Build-LlmWikiBackendContractIndex.ps1', 'Build-LlmWikiFrontendIndex.ps1', 'Build-LlmWikiFrontendContractIndex.ps1', 'Build-LlmWikiArchitectureHealthIndex.ps1')
        $reuseArgument = if ($ReuseUnchangedChecks -and $toolName -in $cacheableTools) { ' -ReuseUnchangedCheck' } else { '' }
        $startInfo.Arguments = "-NoLogo -NoProfile -File `"$scriptPath`"$(if ($CheckMode) { ' -Check' } else { '' })$reuseArgument"
        $startInfo.EnvironmentVariables['GIT_CONFIG_COUNT'] = '1'
        $startInfo.EnvironmentVariables['GIT_CONFIG_KEY_0'] = 'core.safecrlf'
        $startInfo.EnvironmentVariables['GIT_CONFIG_VALUE_0'] = 'false'
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo
        if (-not $process.Start()) { throw "Unable to start $toolName." }
        $workers.Add([pscustomobject]@{ tool = $toolName; process = $process; stopwatch = [System.Diagnostics.Stopwatch]::StartNew(); observed = $false; nextHeartbeat = 30 })
    }
    $failed = [System.Collections.Generic.List[string]]::new()
    while (@($workers | Where-Object { -not $_.observed }).Count -gt 0) {
        foreach ($worker in @($workers | Where-Object { -not $_.observed })) {
            if (-not $worker.process.HasExited) {
                if ($worker.stopwatch.Elapsed.TotalSeconds -ge $worker.nextHeartbeat) {
                    Write-Host " - $($worker.tool): still running ($([Math]::Round($worker.stopwatch.Elapsed.TotalSeconds))s)"
                    $worker.nextHeartbeat += 30
                }
                if ($worker.stopwatch.Elapsed.TotalSeconds -ge $ToolTimeoutSeconds) {
                    Stop-LlmWikiProcessTree -Process $worker.process
                    $worker.stopwatch.Stop()
                    $worker.observed = $true
                    $failed.Add("$($worker.tool) (timeout=${ToolTimeoutSeconds}s)")
                }
                continue
            }
            $worker.stopwatch.Stop()
            $worker.observed = $true
        }
        if (@($workers | Where-Object { -not $_.observed }).Count -gt 0) { Start-Sleep -Milliseconds 25 }
    }
    foreach ($worker in $workers) {
        $worker.process.WaitForExit()
        Write-Host " - $($worker.tool): $([Math]::Round($worker.stopwatch.Elapsed.TotalSeconds, 2))s"
        if (-not $worker.process.HasExited -or $worker.process.ExitCode -ne 0) {
            $failed.Add("$($worker.tool) (exit=$($worker.process.ExitCode))")
        }
        $worker.process.Dispose()
    }
    if ($failed.Count -gt 0) {
        $failedToolNames = @($failed | ForEach-Object { ($_ -split ' \(exit=')[0] })
        $workspaceChangedPaths = if ($DeferPossiblyConcurrentStale) { @(Get-WorkspaceChangedPaths) } else { @() }
        $disposition = & (Join-Path $toolsRoot 'Get-LlmWikiStaleDisposition.ps1') `
            -FailedTool $failedToolNames `
            -WorkspaceChangedPath $workspaceChangedPaths
        $canDefer = $CheckMode -and $DeferPossiblyConcurrentStale -and [bool]$disposition.canDefer
        if ($canDefer) {
            Write-Warning "Fast verification deferred $(@($disposition.artifacts).Count) stale index check(s) because every affected generated artifact is already modified in the working tree. This can indicate parallel Wiki work; do not overwrite those artifacts from this session."
            foreach ($artifact in @($disposition.artifacts)) { Write-Host " - deferred: $artifact" }
            Write-Host 'Run strict ./.llm-wiki/wiki.ps1 verify in the integration session before commit, push, or final handoff.'
            Write-Output ([pscustomobject]@{
                deferredStale = $true
                disposition = [string]$disposition.disposition
                artifacts = @($disposition.artifacts)
            })
            return
        }
        if ($CheckMode) {
            Write-Host ''
            Write-Host 'One or more compiled indexes are stale. Regenerate the complete dependency-aware set with:'
            Write-Host '  ./.llm-wiki/wiki.ps1 update'
            Write-Host 'For an iterative scoped refresh, use:'
            Write-Host '  ./.llm-wiki/wiki.ps1 update -AffectedOnly'
        }
        throw "LLM Wiki index pipeline stage '$StageName' failed: $($failed -join ', ')"
    }
}

$pipelineStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$updateLock = $null
$transactionRoot = $null
$generatedRoot = Join-Path $repositoryRoot '.llm-wiki/generated'
try {
    if (-not $Check) {
        $gitDirectory = (& git -C $repositoryRoot rev-parse --absolute-git-dir).Trim()
        if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve Git directory for the index transaction.' }
        $transactionStateRoot = Join-Path $gitDirectory 'llm-wiki/index-transactions'
        $null = New-Item -ItemType Directory -Path $transactionStateRoot -Force
        Restore-OrphanedIndexTransaction -TransactionStateRoot $transactionStateRoot -GeneratedRoot $generatedRoot
        $lockPath = Join-Path $transactionStateRoot 'update.lock'
        try {
            $updateLock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        } catch {
            throw 'Another LLM Wiki index update is already running. Wait for it to finish instead of producing overlapping generated files.'
        }
        $transactionRoot = Join-Path $transactionStateRoot ([guid]::NewGuid().ToString('N'))
        $backupRoot = Join-Path $transactionRoot 'generated'
        $null = New-Item -ItemType Directory -Path $backupRoot -Force
        foreach ($item in @(Get-ChildItem -LiteralPath $generatedRoot -Force)) {
            Copy-Item -LiteralPath $item.FullName -Destination $backupRoot -Recurse -Force
        }
        [IO.File]::WriteAllText(
            (Join-Path $transactionRoot 'state.json'),
            (([ordered]@{ schemaVersion = 1; status = 'in-progress'; ownerPid = $PID; startedAtUtc = [DateTime]::UtcNow.ToString('o') } | ConvertTo-Json) + [Environment]::NewLine),
            [Text.UTF8Encoding]::new($false))
        Write-Host "LLM Wiki index transaction started: rollback snapshot captured."
    }

    foreach ($stage in $stages) {
        $tools = @($stage.tools | Where-Object { $_ -in $selectedToolNames })
        if ($tools.Count -eq 0) { continue }
        Write-Host "LLM Wiki index stage: $($stage.name) ($($tools.Count) tool(s))"
        for ($offset = 0; $offset -lt $tools.Count; $offset += $MaxConcurrency) {
            $last = [Math]::Min($offset + $MaxConcurrency - 1, $tools.Count - 1)
            Invoke-PipelineBatch -StageName $stage.name -ToolNames @($tools[$offset..$last]) -CheckMode ([bool]$Check)
        }
    }
    if (-not $Check -and $transactionRoot) {
        $semanticNoOps = @(Restore-LlmWikiSemanticNoOpArtifacts `
            -GeneratedRoot $generatedRoot `
            -BackupRoot (Join-Path $transactionRoot 'generated'))
        if ($semanticNoOps.Count -gt 0) { Write-Host "LLM Wiki semantic no-op suppression restored $($semanticNoOps.Count) unchanged generated artifact(s)." }
        [IO.File]::WriteAllText(
            (Join-Path $transactionRoot 'state.json'),
            (([ordered]@{ schemaVersion = 1; status = 'committed'; ownerPid = $PID; completedAtUtc = [DateTime]::UtcNow.ToString('o') } | ConvertTo-Json) + [Environment]::NewLine),
            [Text.UTF8Encoding]::new($false))
    }
} catch {
    if ($transactionRoot) {
        Write-Warning 'LLM Wiki index update failed; restoring the generated tree from the transaction snapshot.'
        foreach ($item in @(Get-ChildItem -LiteralPath $generatedRoot -Force)) {
            Remove-Item -LiteralPath $item.FullName -Recurse -Force
        }
        $backupRoot = Join-Path $transactionRoot 'generated'
        foreach ($item in @(Get-ChildItem -LiteralPath $backupRoot -Force)) {
            Copy-Item -LiteralPath $item.FullName -Destination $generatedRoot -Recurse -Force
        }
    }
    throw
} finally {
    if ($updateLock) { $updateLock.Dispose() }
    if ($transactionRoot -and (Test-Path -LiteralPath $transactionRoot)) {
        Remove-Item -LiteralPath $transactionRoot -Recurse -Force
    }
}
$pipelineStopwatch.Stop()
if ($ReuseUnchangedChecks -and $selectedToolNames.Count -gt 0) {
    $finalPipelineCacheState = Get-PipelineCacheState $selectedToolNames
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $finalPipelineCacheState.receiptPath) -Force
    $pipelineReceipt = [ordered]@{
        schemaVersion = 1
        recordedAtUtc = [DateTime]::UtcNow.ToString('o')
        toolSet = [string]$finalPipelineCacheState.toolSet
        inputFingerprint = [string]$finalPipelineCacheState.inputFingerprint
        outputFingerprint = [string]$finalPipelineCacheState.outputFingerprint
    }
    [IO.File]::WriteAllText($finalPipelineCacheState.receiptPath, (($pipelineReceipt | ConvertTo-Json -Depth 4) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}
Write-Host "LLM Wiki index pipeline completed in $(if ($Check) { 'check' } else { 'update' }) mode in $([Math]::Round($pipelineStopwatch.Elapsed.TotalSeconds, 2))s."
if ($AffectedOnly) {
    $summaryTools = @($selectedToolNames | Sort-Object)
    $summaryPaths = @($normalizedChangedPaths)
    $summaryLayers = [Collections.Generic.List[string]]::new()
    if (@($summaryPaths | Where-Object { $_ -match '^FoodDiary\.Web\.Client/' }).Count -gt 0) { $summaryLayers.Add('Frontend') }
    if (@($summaryPaths | Where-Object { $_ -match 'Presentation|Web\.Api|Snapshots/' }).Count -gt 0) { $summaryLayers.Add('API/Presentation') }
    if (@($summaryPaths | Where-Object { $_ -match 'Application' }).Count -gt 0) { $summaryLayers.Add('Application') }
    if (@($summaryPaths | Where-Object { $_ -match 'Infrastructure|Persistence' }).Count -gt 0) { $summaryLayers.Add('Infrastructure') }
    if (@($summaryPaths | Where-Object { $_ -match 'Domain' }).Count -gt 0) { $summaryLayers.Add('Domain') }
    if (@($summaryPaths | Where-Object { $_ -match '(?i)(^|/)(tests?|__tests__)/|\.Tests?/' }).Count -gt 0) { $summaryLayers.Add('Tests') }
    Write-Host 'LLM Wiki index summary:'
    Write-Host " - scope: $(@($normalizedChangedPaths).Count) changed path(s)"
    Write-Host " - tier: $(if ($RequiredOnly) { 'required contracts/navigation' } else { 'all affected, including analytics' })"
    Write-Host " - generators: $(if ($summaryTools.Count -gt 0) { $summaryTools -join ', ' } else { 'none' })"
    Write-Host " - transaction: $(if ($Check) { 'read-only check' } else { 'atomic update with semantic no-op suppression' })"
    Write-Host " - layers: $(if ($summaryLayers.Count -gt 0) { @($summaryLayers | Sort-Object -Unique) -join ', ' } else { 'Wiki/tooling only' })"
    Write-Host " - API snapshot review: $(if (@($summaryPaths | Where-Object { $_ -match 'Presentation|Web\.Api|Snapshots/' }).Count -gt 0) { 'required/relevant' } else { 'not indicated by paths' })"
    Write-Host " - migration review: $(if (@($summaryPaths | Where-Object { $_ -match '(?i)Migrations?/' }).Count -gt 0) { 'required' } else { 'not indicated by paths' })"
    Write-Host " - analytical indexes: $(if ($RequiredOnly) { 'deferred until publication-finalization' } else { 'included when affected' })"
}
