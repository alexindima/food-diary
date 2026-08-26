[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Command,
    [Parameter(Mandatory)][string]$RequestFile,
    [Parameter(Mandatory)][string]$WikiPath
)

$ErrorActionPreference = 'Stop'
$request = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $RequestFile), [Text.Encoding]::UTF8) | ConvertFrom-Json
if ($request.schemaVersion -ne 1 -or $null -eq $request.arguments) {
    throw 'Unsupported MCP Wiki command request schema.'
}
$arguments = @{}
foreach ($property in $request.arguments.PSObject.Properties) {
    $arguments[$property.Name] = $property.Value
}
function Rename-McpArgument([hashtable]$Values, [string]$From, [string]$To) {
    if ($Values.ContainsKey($From)) {
        $Values[$To] = $Values[$From]
        $Values.Remove($From)
    }
}
function Select-McpArguments([hashtable]$Values, [string[]]$Names) {
    $selected = @{}
    foreach ($name in $Names) {
        if ($Values.ContainsKey($name)) { $selected[$name] = $Values[$name] }
    }
    $selected
}

switch ($Command) {
    'brief' {
        Rename-McpArgument $arguments 'Objective' 'Intent'
        $briefArguments = Select-McpArguments $arguments @(
            'BaseRef', 'HeadRef', 'ChangedPath', 'ProposedPath', 'Intent',
            'CompiledIndexSource', 'SkipQueryCache', 'Format', 'Compact', 'SkipTestPlan', 'Limit'
        )
        & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskBrief.ps1') @briefArguments
    }
    'test-plan' {
        if ([bool]$arguments.Fast) {
            $fastArguments = Select-McpArguments $arguments @('ChangedPath', 'ProposedPath', 'Limit', 'Format')
            & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphTestPlan.ps1') @fastArguments
        } else {
            Rename-McpArgument $arguments 'Objective' 'Intent'
            $arguments.Remove('Fast')
            & (Join-Path $PSScriptRoot 'Get-LlmWikiTestPlan.ps1') @arguments
        }
    }
    default {
        & $WikiPath $Command -RequestFile $RequestFile
    }
}
