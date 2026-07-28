[CmdletBinding()]
param(
    [switch]$Check,
    [ValidateRange(1, 8)]
    [int]$MaxConcurrency = 4
)

$ErrorActionPreference = 'Stop'
$toolsRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$shellPath = [System.IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $toolsRoot '../..'))

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
    Write-Host "LLM Wiki index stage: $($stage.name)"
    $tools = @($stage.tools)
    for ($offset = 0; $offset -lt $tools.Count; $offset += $MaxConcurrency) {
        $last = [Math]::Min($offset + $MaxConcurrency - 1, $tools.Count - 1)
        Invoke-PipelineBatch -StageName $stage.name -ToolNames @($tools[$offset..$last]) -CheckMode ([bool]$Check)
    }
}
Write-Host "LLM Wiki index pipeline completed in $(if ($Check) { 'check' } else { 'update' }) mode."
