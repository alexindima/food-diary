[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$AffectedOnly,
    [switch]$Plan,
    [string]$BaseRef = 'HEAD',
    [string[]]$ChangedPath,
    [ValidateRange(1, 8)]
    [int]$MaxConcurrency = 4
)

$ErrorActionPreference = 'Stop'
$toolsRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$shellPath = [System.IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $toolsRoot '../..'))

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
        $hasFrontend = @($normalizedChangedPaths | Where-Object { $_ -match '^FoodDiary\.Web\.Client/' }).Count -gt 0
        $hasCSharp = @($normalizedChangedPaths | Where-Object { $_ -match '\.(cs|csproj)$' }).Count -gt 0
        if ($hasFrontend) {
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
        $startInfo.Arguments = "-NoLogo -NoProfile -File `"$scriptPath`"$(if ($CheckMode) { ' -Check' } else { '' })"
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo
        if (-not $process.Start()) { throw "Unable to start $toolName." }
        $workers.Add([pscustomobject]@{ tool = $toolName; process = $process })
    }
    $failed = [System.Collections.Generic.List[string]]::new()
    foreach ($worker in $workers) {
        $worker.process.WaitForExit()
        if ($worker.process.ExitCode -ne 0) {
            $failed.Add("$($worker.tool) (exit=$($worker.process.ExitCode))")
        }
        $worker.process.Dispose()
    }
    if ($failed.Count -gt 0) {
        throw "LLM Wiki index pipeline stage '$StageName' failed: $($failed -join ', ')"
    }
}

foreach ($stage in $stages) {
    $tools = @($stage.tools | Where-Object { -not $AffectedOnly -or $selectedTools.Contains($_) })
    if ($tools.Count -eq 0) { continue }
    Write-Host "LLM Wiki index stage: $($stage.name) ($($tools.Count) tool(s))"
    for ($offset = 0; $offset -lt $tools.Count; $offset += $MaxConcurrency) {
        $last = [Math]::Min($offset + $MaxConcurrency - 1, $tools.Count - 1)
        Invoke-PipelineBatch -StageName $stage.name -ToolNames @($tools[$offset..$last]) -CheckMode ([bool]$Check)
    }
}
Write-Host "LLM Wiki index pipeline completed in $(if ($Check) { 'check' } else { 'update' }) mode."
