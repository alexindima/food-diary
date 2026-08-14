[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiFailures.ps1'
$emptyOutput = @(& $manager search -Query '__llm_wiki_no_known_failure__' *>&1) -join "`n"
if ($emptyOutput -notmatch 'No known failures matched') { throw 'Empty failure search did not return its stable no-match result.' }
$allOutput = @(& $manager search *>&1) -join "`n"
if ([string]::IsNullOrWhiteSpace($allOutput)) { throw 'Failure search without a query returned no entries.' }
Write-Host 'LLM Wiki failure search cardinality regression passed.'
