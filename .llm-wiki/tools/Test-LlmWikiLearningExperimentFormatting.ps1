[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Format-LlmWikiLearningExperimentResult.ps1')

function New-TestExperiment([string]$Id) {
    [pscustomobject]@{
        candidateId = $Id
        shadow = [pscustomobject]@{ verdict = 'pass' }
        canaryState = 'active'
        currentEvaluation = [pscustomobject]@{ verdict = 'continue' }
        successful = $false
    }
}

foreach ($action in @('canary-start', 'canary-record', 'canary-evaluate', 'canary-stop')) {
    $result = [pscustomobject]@{ action = $action; valid = $true; experiment = (New-TestExperiment "singular-$action"); issues = @() }
    $text = (Write-LlmWikiLearningExperimentResult $result 6>&1 | Out-String)
    if ($text -notmatch "singular-$action") { throw "Singular text formatter omitted the experiment for $action." }
}

foreach ($action in @('list', 'show', 'active', 'verify')) {
    $result = [pscustomobject]@{ action = $action; valid = $true; experiments = @((New-TestExperiment "plural-$action")); issues = @() }
    $text = (Write-LlmWikiLearningExperimentResult $result 6>&1 | Out-String)
    if ($text -notmatch "plural-$action") { throw "Plural text formatter omitted experiments for $action." }
}

$emptyText = (Write-LlmWikiLearningExperimentResult ([pscustomobject]@{ action = 'list'; valid = $true }) 6>&1 | Out-String)
if ($emptyText -notmatch 'action=list, valid=True') { throw 'Formatter rejected a valid empty result shape.' }

Write-Host 'LLM Wiki learning-experiment formatter regression passed: singular, plural, and empty result shapes are safe.'
