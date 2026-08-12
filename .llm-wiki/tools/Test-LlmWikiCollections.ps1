[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiCollections.ps1')

if (-not (Test-LlmWikiSameSet @('alpha', 'beta') @('beta', 'alpha'))) { throw 'Equal sets were rejected.' }
if (Test-LlmWikiSameSet @('alpha') @('beta')) { throw 'Different sets were accepted.' }
if (-not (Test-LlmWikiSameSet @() @())) { throw 'Two empty sets were rejected.' }

if (@(Get-LlmWikiPropertyValues @() 'id').Count -ne 0) { throw 'Empty property collection was not preserved.' }
$singleId = @(Get-LlmWikiPropertyValues @([pscustomobject]@{ id = 'one' }) 'id')
if ($singleId.Count -ne 1 -or $singleId[0] -cne 'one') { throw 'Single property value was not preserved as a collection.' }
$manyIds = @(Get-LlmWikiPropertyValues @([pscustomobject]@{ id = 'one' }, [pscustomobject]@{ id = 'two' }) 'id')
if (-not (Test-LlmWikiSameSet $manyIds @('one', 'two'))) { throw 'Multiple property values were not preserved.' }
if (@(Get-LlmWikiPropertyValues @([pscustomobject]@{ legacy = 'value' }) 'id').Count -ne 0) { throw 'Legacy object without the requested property was not ignored.' }

Write-Host 'LLM Wiki collection regression passed: set comparison and empty/single/many/legacy property shapes are safe.'
