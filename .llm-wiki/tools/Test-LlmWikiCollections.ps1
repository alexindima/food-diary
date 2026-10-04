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

# Both consumers own an item-ID projection. Keep the formerly separate regression
# cases here so strict-shapes checks empty, mixed, legacy, and duplicate values.
Set-StrictMode -Version Latest
foreach ($consumer in @('Manage-LlmWikiModelRouting.ps1', 'Manage-LlmWikiVerificationPlan.ps1')) {
    $parseErrors = $null
    $consumerAst = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $consumer), [ref]$null, [ref]$parseErrors)
    if (@($parseErrors).Count -gt 0) { throw "Item-ID consumer does not parse: $consumer" }
    $helpers = @($consumerAst.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-ItemIds' }, $true))
    if ($helpers.Count -ne 1) { throw "Expected one item-ID projection in $consumer." }
    & {
        param([string]$Definition, [string]$Consumer)
        . ([scriptblock]::Create($Definition))
        if (@(Get-ItemIds @()).Count -ne 0) { throw "Empty item-ID projection returned values: $Consumer" }
        foreach ($expected in @(@('privacy-review', 'security-review'), @('architecture-tests', 'domain-tests'))) {
            $mixed = @([pscustomobject]@{ id = $expected[0] }, [pscustomobject]@{ description = 'legacy item without an id' }, $expected[1], ' ', $null, [pscustomobject]@{ id = $expected[0] })
            $actual = @(Get-ItemIds $mixed)
            if ($actual.Count -ne 2 -or -not (Test-LlmWikiSameSet $actual $expected)) { throw "Mixed item-ID projection lost or duplicated values: $Consumer" }
        }
        $normal = @(Get-ItemIds @([pscustomobject]@{ id = 'adr-review' }, [pscustomobject]@{ id = 'rollout-review' }))
        if ($normal.Count -ne 2 -or -not (Test-LlmWikiSameSet $normal @('adr-review', 'rollout-review'))) { throw "Normal item-ID projection regressed: $Consumer" }
    } $helpers[0].Extent.Text $consumer
}

Write-Host 'LLM Wiki collection regression passed: set comparison and empty/single/many/legacy property shapes are safe.'
