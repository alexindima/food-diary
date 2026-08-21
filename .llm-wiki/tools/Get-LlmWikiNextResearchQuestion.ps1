[CmdletBinding()]
param(
    [Alias('Intent')]
    [string]$Objective,
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [Alias('PlannedPath')]
    [string[]]$ProposedPath,
    [ValidateSet('Auto', 'Assessment', 'Implementation')]
    [string]$Purpose = 'Auto',
    [ValidateRange(1, 30)]
    [int]$Limit = 10,
    [string]$Module,
    [switch]$Compact,
    [switch]$SkipHistory,
    [object]$ResearchInput,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$research = if ($null -ne $ResearchInput) {
    $ResearchInput
} else {
    if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'Objective is required when ResearchInput is not supplied.' }
    $arguments = @{
        Objective = $Objective
        BaseRef = $BaseRef
        Purpose = $Purpose
        Limit = $Limit
        Compact = $Compact
        SkipHistory = $SkipHistory
        Format = 'Json'
    }
    if ($PSBoundParameters.ContainsKey('HeadRef')) { $arguments.HeadRef = $HeadRef }
    if ($PSBoundParameters.ContainsKey('ChangedPath')) { $arguments.ChangedPath = $ChangedPath }
    if ($PSBoundParameters.ContainsKey('ProposedPath')) { $arguments.ProposedPath = $ProposedPath }
    if (-not [string]::IsNullOrWhiteSpace($Module)) { $arguments.Module = $Module }
    & (Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1') @arguments | ConvertFrom-Json
}

$questions = @($research.openQuestions | Where-Object { $null -ne $_ })
$selected = @($questions | Sort-Object @{ Expression = { if ($_.blocking) { 0 } else { 1 } } }, id | Select-Object -First 1)
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    objective = [string]$research.objective
    found = $selected.Count -gt 0
    question = $(if ($selected.Count -gt 0) { $selected[0] } else { $null })
    remainingQuestionCount = [Math]::Max(0, $questions.Count - $selected.Count)
    interactionContract = $(if ($selected.Count -gt 0) {
        'Ask only this grounded question. Do not ask for confirmation when repository evidence can answer it.'
    } else {
        'No developer question is required by the current research packet.'
    })
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 12
    exit 0
}
if (-not $result.found) {
    Write-Host 'Research has no open developer question.'
    exit 0
}
$question = $result.question
$anchor = if ($question.anchorStatus -eq 'line') {
    "$($question.anchor.path):$($question.anchor.line)"
} elseif ($question.anchorStatus -eq 'path') {
    [string]$question.anchor.path
} else {
    'source anchor unavailable'
}
Write-Host "Research question [$($question.id)] ($anchor)"
Write-Host $question.question
Write-Host "Why input is required: $($question.whyUserInputIsRequired)"
Write-Host "Evidence needed: $($question.evidenceNeeded)"
if (-not [string]::IsNullOrWhiteSpace([string]$question.resolutionCommand)) { Write-Host "Resolution: $($question.resolutionCommand)" }
if ($result.remainingQuestionCount -gt 0) { Write-Host "Deferred questions: $($result.remainingQuestionCount)" }
