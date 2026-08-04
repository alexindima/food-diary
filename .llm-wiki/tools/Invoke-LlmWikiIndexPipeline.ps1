[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$AffectedOnly,
    [switch]$Plan,
    [switch]$DeferPossiblyConcurrentStale,
    [switch]$ReuseUnchangedChecks,
    [string]$BaseRef = 'HEAD',
    [string[]]$ChangedPath,
    [ValidateRange(1, 8)]
    [int]$MaxConcurrency = 4
)

$ErrorActionPreference = 'Stop'
$toolsRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$shellPath = [System.IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $toolsRoot '../..'))

function Get-WorkspaceChangedPaths {
    $paths = @(& git -C $repositoryRoot diff --name-only --diff-filter=ACMRD HEAD --)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to collect workspace changes for stale-index diagnostics.' }
    $paths += @(& git -C $repositoryRoot ls-files --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to collect untracked paths for stale-index diagnostics.' }
    return @($paths | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique)
}

if ($AffectedOnly -and -not $PSBoundParameters.ContainsKey('ChangedPath')) {
    $ChangedPath = @(& git -C $repositoryRoot diff --name-only --diff-filter=ACMRD $BaseRef --)
    if ($LASTEXITCODE -ne 0) { throw "Unable to collect changed paths from '$BaseRef'." }
    if ($BaseRef -eq 'HEAD') {
        $ChangedPath += @(& git -C $repositoryRoot ls-files --others --exclude-standard)
        if ($LASTEXITCODE -ne 0) { throw 'Unable to collect untracked paths.' }
    }
}

$selectedTools = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
function Add-IndexTool([string]$Name) {
    $null = $selectedTools.Add($Name)
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
    if (@($normalizedChangedPaths | Where-Object { $_ -match '^\.llm-wiki/(tools|policies)/|^Directory\.|\.slnx?$' }).Count -gt 0) {
        foreach ($tool in @(
            'Build-LlmWikiCatalog.ps1', 'Build-LlmWikiSymbolIndex.ps1', 'Build-LlmWikiFrontendIndex.ps1',
            'Build-LlmWikiFrontendContractIndex.ps1', 'Build-LlmWikiDomainDataIndex.ps1',
            'Build-LlmWikiConfigurationIndex.ps1', 'Build-LlmWikiRuntimeTopology.ps1',
            'Build-LlmWikiSensitiveDataIndex.ps1', 'Build-LlmWikiBackendContractIndex.ps1',
            'Build-LlmWikiQualityIndex.ps1', 'Build-LlmWikiModulePages.ps1',
            'Build-LlmWikiArchitectureHealthIndex.ps1'
        )) { Add-IndexTool $tool }
    } else {
        $frontendPaths = @($normalizedChangedPaths | Where-Object { $_ -match '^FoodDiary\.Web\.Client/' })
        $hasFrontend = $frontendPaths.Count -gt 0
        $frontendTestOnly = $hasFrontend -and @($frontendPaths | Where-Object { $_ -notmatch '(?:^|/)\w[^/]*\.(?:spec|test)\.ts$' }).Count -eq 0
        $frontendStyleOnly = $hasFrontend -and @($frontendPaths | Where-Object { $_ -notmatch '\.(?:scss|css)$' }).Count -eq 0
        $frontendTemplateOnly = $hasFrontend -and @($frontendPaths | Where-Object { $_ -notmatch '\.html$' }).Count -eq 0
        $hasCSharp = @($normalizedChangedPaths | Where-Object { $_ -match '\.(cs|csproj)$' }).Count -gt 0
        if ($frontendStyleOnly) {
            # Current compiled indexes do not read stylesheet contents.
        } elseif ($frontendTestOnly) {
            Add-IndexTool 'Build-LlmWikiQualityIndex.ps1'
        } elseif ($frontendTemplateOnly) {
            Add-IndexTool 'Build-LlmWikiFrontendIndex.ps1'
            Add-IndexTool 'Build-LlmWikiFrontendContractIndex.ps1'
        } elseif ($hasFrontend) {
            Add-IndexTool 'Build-LlmWikiFrontendIndex.ps1'
            Add-IndexTool 'Build-LlmWikiFrontendContractIndex.ps1'
            Add-IndexTool 'Build-LlmWikiQualityIndex.ps1'
            Add-IndexTool 'Build-LlmWikiSensitiveDataIndex.ps1'
        }
        if ($hasCSharp) {
            Add-IndexTool 'Build-LlmWikiCatalog.ps1'
            Add-IndexTool 'Build-LlmWikiSymbolIndex.ps1'
            Add-IndexTool 'Build-LlmWikiBackendContractIndex.ps1'
            Add-IndexTool 'Build-LlmWikiQualityIndex.ps1'
            Add-IndexTool 'Build-LlmWikiSensitiveDataIndex.ps1'
            Add-IndexTool 'Build-LlmWikiModulePages.ps1'
        }
        if (@($normalizedChangedPaths | Where-Object {
            $_ -match 'Domain/|Persistence/|Migrations?/|DbContext|Configuration\.cs$'
        }).Count -gt 0) { Add-IndexTool 'Build-LlmWikiDomainDataIndex.ps1' }
        if (@($normalizedChangedPaths | Where-Object {
            $_ -match 'appsettings|\.env|Options\.cs$|docker-compose|\.github/workflows/'
        }).Count -gt 0) { Add-IndexTool 'Build-LlmWikiConfigurationIndex.ps1' }
        if (@($normalizedChangedPaths | Where-Object {
            $_ -match 'HostedService|Recurring|Webhook|Integrations/|JobManager/|docker-compose'
        }).Count -gt 0) { Add-IndexTool 'Build-LlmWikiRuntimeTopology.ps1' }
        if ($selectedTools.Count -gt 0) {
            Add-IndexTool 'Build-LlmWikiArchitectureHealthIndex.ps1'
        }
    }
    Write-Host "LLM Wiki affected index pipeline: $($normalizedChangedPaths.Count) changed path(s), $($selectedTools.Count) selected tool(s)."
    if ($Plan) {
        Write-Output "Affected index tools: $(@($selectedTools | Sort-Object) -join ', ')"
        exit 0
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
        $cacheableTools = @('Build-LlmWikiQualityIndex.ps1', 'Build-LlmWikiBackendContractIndex.ps1', 'Build-LlmWikiFrontendIndex.ps1', 'Build-LlmWikiFrontendContractIndex.ps1')
        $reuseArgument = if ($CheckMode -and $ReuseUnchangedChecks -and $toolName -in $cacheableTools) { ' -ReuseUnchangedCheck' } else { '' }
        $startInfo.Arguments = "-NoLogo -NoProfile -File `"$scriptPath`"$(if ($CheckMode) { ' -Check' } else { '' })$reuseArgument"
        $startInfo.EnvironmentVariables['GIT_CONFIG_COUNT'] = '1'
        $startInfo.EnvironmentVariables['GIT_CONFIG_KEY_0'] = 'core.safecrlf'
        $startInfo.EnvironmentVariables['GIT_CONFIG_VALUE_0'] = 'false'
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo
        if (-not $process.Start()) { throw "Unable to start $toolName." }
        $workers.Add([pscustomobject]@{ tool = $toolName; process = $process; stopwatch = [System.Diagnostics.Stopwatch]::StartNew(); observed = $false })
    }
    $failed = [System.Collections.Generic.List[string]]::new()
    while (@($workers | Where-Object { -not $_.observed }).Count -gt 0) {
        foreach ($worker in @($workers | Where-Object { -not $_.observed })) {
            if (-not $worker.process.HasExited) { continue }
            $worker.stopwatch.Stop()
            $worker.observed = $true
        }
        if (@($workers | Where-Object { -not $_.observed }).Count -gt 0) { Start-Sleep -Milliseconds 25 }
    }
    foreach ($worker in $workers) {
        $worker.process.WaitForExit()
        Write-Host " - $($worker.tool): $([Math]::Round($worker.stopwatch.Elapsed.TotalSeconds, 2))s"
        if ($worker.process.ExitCode -ne 0) {
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
foreach ($stage in $stages) {
    $tools = @($stage.tools | Where-Object { -not $AffectedOnly -or $selectedTools.Contains($_) })
    if ($tools.Count -eq 0) { continue }
    Write-Host "LLM Wiki index stage: $($stage.name) ($($tools.Count) tool(s))"
    for ($offset = 0; $offset -lt $tools.Count; $offset += $MaxConcurrency) {
        $last = [Math]::Min($offset + $MaxConcurrency - 1, $tools.Count - 1)
        Invoke-PipelineBatch -StageName $stage.name -ToolNames @($tools[$offset..$last]) -CheckMode ([bool]$Check)
    }
}
$pipelineStopwatch.Stop()
Write-Host "LLM Wiki index pipeline completed in $(if ($Check) { 'check' } else { 'update' }) mode in $([Math]::Round($pipelineStopwatch.Elapsed.TotalSeconds, 2))s."
