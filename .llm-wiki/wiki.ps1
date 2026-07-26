[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('help', 'update', 'verify', 'context', 'diff', 'impact', 'catalog', 'symbols', 'frontend', 'modules')]
    [string]$Command = 'help',

    [string]$Module,
    [string]$Query,
    [ValidateSet('Any', 'Api', 'Backend', 'Frontend', 'Database', 'Tests')]
    [string]$ChangeType = 'Any',
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12,
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [switch]$FailOnUnreviewed,
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$toolsRoot = Join-Path $PSScriptRoot 'tools'

function Invoke-WikiTool {
    param(
        [string]$Name,
        [hashtable]$ToolArguments = @{}
    )

    $toolPath = Join-Path $toolsRoot $Name
    $global:LASTEXITCODE = 0
    & $toolPath @ToolArguments
    if (-not $?) {
        exit 1
    }
    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

switch ($Command) {
    'update' {
        Invoke-WikiTool 'Build-LlmWikiCatalog.ps1'
        Invoke-WikiTool 'Build-LlmWikiSymbolIndex.ps1'
        Invoke-WikiTool 'Build-LlmWikiFrontendIndex.ps1'
        Invoke-WikiTool 'Build-LlmWikiModulePages.ps1'
    }
    'verify' {
        Invoke-WikiTool 'Test-LlmWiki.ps1'
        Invoke-WikiTool 'Build-LlmWikiCatalog.ps1' @{ Check = $true }
        Invoke-WikiTool 'Build-LlmWikiSymbolIndex.ps1' @{ Check = $true }
        Invoke-WikiTool 'Build-LlmWikiFrontendIndex.ps1' @{ Check = $true }
        Invoke-WikiTool 'Build-LlmWikiModulePages.ps1' @{ Check = $true }
        Invoke-WikiTool 'Test-LlmWikiTools.ps1'
        Invoke-WikiTool 'Get-LlmWikiImpact.ps1' @{ FailOnUnreviewed = $true }
    }
    'context' {
        Invoke-WikiTool 'Find-LlmWikiContext.ps1' @{
            Module = $Module
            Query = $Query
            ChangeType = $ChangeType
            Format = $Format
            Limit = $Limit
        }
    }
    'diff' {
        $diffArguments = @{
            BaseRef = $BaseRef
            Format = $Format
            Limit = [Math]::Min($Limit, 20)
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) {
            $diffArguments.HeadRef = $HeadRef
        }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) {
            $diffArguments.ChangedPath = $ChangedPath
        }
        Invoke-WikiTool 'Get-LlmWikiDiffContext.ps1' $diffArguments
    }
    'impact' {
        $impactArguments = @{
            BaseRef = $BaseRef
            FailOnUnreviewed = $FailOnUnreviewed
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) {
            $impactArguments.HeadRef = $HeadRef
        }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) {
            $impactArguments.ChangedPath = $ChangedPath
        }
        Invoke-WikiTool 'Get-LlmWikiImpact.ps1' $impactArguments
    }
    'catalog' {
        Invoke-WikiTool 'Build-LlmWikiCatalog.ps1' @{ Check = $Check }
    }
    'symbols' {
        Invoke-WikiTool 'Build-LlmWikiSymbolIndex.ps1' @{ Check = $Check }
    }
    'frontend' {
        Invoke-WikiTool 'Build-LlmWikiFrontendIndex.ps1' @{ Check = $Check }
    }
    'modules' {
        Invoke-WikiTool 'Build-LlmWikiModulePages.ps1' @{ Check = $Check }
    }
    default {
        Write-Host 'FoodDiary LLM Wiki'
        Write-Host ''
        Write-Host 'Usage:'
        Write-Host '  ./.llm-wiki/wiki.ps1 update'
        Write-Host '  ./.llm-wiki/wiki.ps1 verify'
        Write-Host '  ./.llm-wiki/wiki.ps1 context -Module Billing -ChangeType Api'
        Write-Host '  ./.llm-wiki/wiki.ps1 diff'
        Write-Host '  ./.llm-wiki/wiki.ps1 impact -FailOnUnreviewed'
        Write-Host '  ./.llm-wiki/wiki.ps1 catalog [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 symbols [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 frontend [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 modules [-Check]'
    }
}
