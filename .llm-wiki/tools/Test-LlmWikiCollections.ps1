[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiCollections.ps1')

if (-not (Test-LlmWikiSameSet @('alpha', 'beta') @('beta', 'alpha'))) { throw 'Equal sets were rejected.' }
if (Test-LlmWikiSameSet @('alpha') @('beta')) { throw 'Different sets were accepted.' }
if (-not (Test-LlmWikiSameSet @() @())) { throw 'Two empty sets were rejected.' }

Write-Host 'LLM Wiki collection regression passed: equal, different, and empty set comparisons are safe.'
