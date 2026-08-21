[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
function Assert-ResearchContract([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$plannedPaths = @(
    '.llm-wiki/workflows/adaptive-development.md'
    '.llm-wiki/workflows/architecture-health-review.md'
)
$research = & (Join-Path $PSScriptRoot 'Get-LlmWikiResearchPacket.ps1') `
    -Objective 'Assess Wiki research planning and review workflow contracts' `
    -Purpose Assessment `
    -ProposedPath $plannedPaths `
    -Compact `
    -SkipHistory `
    -Format Json | ConvertFrom-Json

$explicitPaths = @($research.discovery.implementationFiles | Where-Object provenance -eq 'explicit-planned-path' | Select-Object -ExpandProperty path)
Assert-ResearchContract (@($plannedPaths | Where-Object { $_ -notin $explicitPaths }).Count -eq 0) 'Research did not prioritize every explicit planned path.'
Assert-ResearchContract (@($research.discovery.groundedPaths | Where-Object { $_ -notmatch '^\.llm-wiki/' }).Count -eq 0) 'Planned Wiki research leaked unrelated product paths.'
Assert-ResearchContract (@($research.researchLanes | Where-Object { [int]$_.evidenceCount -eq 0 -and @($_.sources).Count -eq 0 }).Count -eq 0) 'Research emitted an empty lane.'
Assert-ResearchContract (@($research.researchLanes.sources | ForEach-Object { @($_) } | Where-Object { $null -eq $_ -or [string]::IsNullOrWhiteSpace([string]$_) }).Count -eq 0) 'Research emitted a null or blank source reference.'
$groupedFlowAndGuidance = @($research.researchPlan.groups | Where-Object { 'flow' -in @($_.laneIds) -and 'guidance' -in @($_.laneIds) })
Assert-ResearchContract ($groupedFlowAndGuidance.Count -eq 1) 'Research did not group lanes sharing the two planned workflow paths.'
Assert-ResearchContract ([int]$research.researchPlan.duplicateReadSavings -ge 2) 'Research plan did not report avoided duplicate reads.'
Assert-ResearchContract (@($plannedPaths | Where-Object { $_ -notin @($research.researchPlan.readSet) }).Count -eq 0) 'Research plan omitted an explicit planned path from the read set.'

$syntheticResearch = [pscustomobject]@{
    objective = 'Choose a compatibility boundary.'
    openQuestions = @(
        [pscustomobject]@{ id = 'non-blocking'; blocking = $false; question = 'Optional follow-up'; anchorStatus = 'missing'; anchor = $null; whyUserInputIsRequired = 'Optional'; evidenceNeeded = 'Optional'; resolutionCommand = '' }
        [pscustomobject]@{ id = 'blocking'; blocking = $true; question = 'Which compatibility boundary is required?'; anchorStatus = 'line'; anchor = [pscustomobject]@{ path = 'docs/ARCHITECTURE.md'; line = 10; symbol = $null }; whyUserInputIsRequired = 'The tradeoff changes behavior.'; evidenceNeeded = 'A selected boundary.'; resolutionCommand = './.llm-wiki/wiki.ps1 design' }
    )
}
$nextQuestion = & (Join-Path $PSScriptRoot 'Get-LlmWikiNextResearchQuestion.ps1') `
    -ResearchInput $syntheticResearch `
    -Format Json | ConvertFrom-Json
Assert-ResearchContract ($nextQuestion.found -and $nextQuestion.question.id -eq 'blocking') 'Next-question routing did not prioritize the blocking question.'
Assert-ResearchContract ($nextQuestion.question.anchorStatus -eq 'line' -and $nextQuestion.question.anchor.line -eq 10) 'Next-question routing lost the grounded source anchor.'
Assert-ResearchContract ($nextQuestion.remainingQuestionCount -eq 1) 'Next-question routing did not defer the remaining question.'

Write-Host 'LLM Wiki research contracts passed.'
