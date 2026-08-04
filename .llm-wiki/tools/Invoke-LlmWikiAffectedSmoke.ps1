[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string[]]$ChangedPath,
    [switch]$Plan
)

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$wikiRoot = Split-Path -Parent $toolsRoot
$repositoryRoot = Split-Path -Parent $wikiRoot

if (-not $PSBoundParameters.ContainsKey('ChangedPath')) {
    $ChangedPath = @(& git -C $repositoryRoot diff --name-only --diff-filter=ACMRD $BaseRef --)
    if ($LASTEXITCODE -ne 0) { throw "Unable to collect changed paths from '$BaseRef'." }
    if ($BaseRef -eq 'HEAD') {
        $ChangedPath += @(& git -C $repositoryRoot ls-files --others --exclude-standard)
        if ($LASTEXITCODE -ne 0) { throw 'Unable to collect untracked paths.' }
    }
}
$paths = @($ChangedPath | Where-Object { $_ } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique)
if ($paths.Count -eq 0) {
    Write-Host 'Affected tools smoke: no changed paths; nothing to run.'
    exit 0
}

$groups = [Collections.Generic.List[string]]::new()
function Add-Group([string]$Name) { if (-not $groups.Contains($Name)) { $groups.Add($Name) } }

$hasUnknownToolChange = $false
$wikiRelevantPathCount = 0
foreach ($path in $paths) {
    if ($path -notmatch '^\.llm-wiki/') { continue }
    $wikiRelevantPathCount++
    if ($path -match '^\.llm-wiki/(tools/(Get-LlmWikiAdaptiveWorkflow|Get-LlmWikiSolutionComparison|Get-LlmWikiDesignCheckpoint|Test-LlmWikiAdaptiveWorkflow|Get-LlmWikiIntegrationScan|Test-LlmWikiIntegrationScan|Invoke-LlmWikiAdaptiveVerification)|evals/|policies/experience-policies\.json|workflows/(adaptive-development|developer-experience|integration-scan|evals|learned-regression-evals)\.md)') {
        Add-Group 'adaptive-routing'
    } elseif ($path -match '^\.llm-wiki/(tools/Get-LlmWikiDependencyChanges|workflows/dependency-rollout\.md)') {
        Add-Group 'dependency-analysis'
    } elseif ($path -match '^\.llm-wiki/(tools/(Invoke-LlmWikiAffectedSmoke|Test-LlmWikiStrictAffected)|wiki\.ps1|workflows/index-pipeline\.md)') {
        Add-Group 'facade-contract'
    } elseif ($path -match '^\.llm-wiki/tools/(Find-LlmWikiFrontendTrace|Find-LlmWikiTrace|Test-LlmWikiTraceOutput)\.ps1$') {
        Add-Group 'trace-output'
    } elseif ($path -match '^\.llm-wiki/tools/(Manage-LlmWikiTaskBaseline|Test-LlmWikiTaskBaseline)\.ps1$') {
        Add-Group 'task-baseline'
    } elseif ($path -match '^\.llm-wiki/tools/(Manage-LlmWikiVerificationCache|Test-LlmWikiVerificationCache|Invoke-LlmWikiFullVerification)\.ps1$') {
        Add-Group 'verification-cache'
    } elseif ($path -match '^\.llm-wiki/tools/') {
        $hasUnknownToolChange = $true
    }
}
if ($wikiRelevantPathCount -eq 0) {
    Write-Host 'Affected tools smoke: no LLM Wiki implementation paths changed; nothing to run.'
    exit 0
}
if ($hasUnknownToolChange -or $groups.Count -eq 0) { Add-Group 'full-tools' }

Write-Host "Affected tools smoke: $($paths.Count) changed path(s), groups=$($groups -join ',')."
if ($Plan) { exit 0 }

foreach ($group in $groups) {
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    switch ($group) {
        'adaptive-routing' {
            & (Join-Path $toolsRoot 'Invoke-LlmWikiAdaptiveVerification.ps1')
            if (-not $?) { exit 1 }
        }
        'dependency-analysis' {
            $rootResult = & (Join-Path $toolsRoot 'Get-LlmWikiDependencyChanges.ps1') -BaseRef HEAD -Format Json | ConvertFrom-Json
            Push-Location (Join-Path $repositoryRoot 'FoodDiary.Web.Client')
            try {
                $frontendResult = & (Join-Path $toolsRoot 'Get-LlmWikiDependencyChanges.ps1') -BaseRef HEAD -Format Json | ConvertFrom-Json
            } finally { Pop-Location }
            if ($rootResult.changeCount -ne $frontendResult.changeCount -or
                (@($rootResult.changes | ConvertTo-Json -Depth 7) -join '') -cne (@($frontendResult.changes | ConvertTo-Json -Depth 7) -join '')) {
                throw 'Dependency analysis differs between repository-root and frontend working directories.'
            }
            Write-Host "Dependency analysis smoke passed: $($rootResult.changeCount) current change(s), cwd-independent."
        }
        'facade-contract' {
            & (Join-Path $toolsRoot 'Test-LlmWiki.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiLint.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiStrictAffected.ps1')
            if (-not $?) { exit 1 }
        }
        'trace-output' {
            & (Join-Path $toolsRoot 'Test-LlmWikiTraceOutput.ps1')
            if (-not $?) { exit 1 }
        }
        'task-baseline' {
            & (Join-Path $toolsRoot 'Test-LlmWikiTaskBaseline.ps1')
            if (-not $?) { exit 1 }
        }
        'verification-cache' {
            & (Join-Path $toolsRoot 'Test-LlmWikiVerificationCache.ps1')
            if (-not $?) { exit 1 }
        }
        'full-tools' {
            & (Join-Path $toolsRoot 'Test-LlmWikiTools.ps1')
            if (-not $?) { exit 1 }
        }
    }
    $stopwatch.Stop()
    Write-Host " - ${group}: $([Math]::Round($stopwatch.Elapsed.TotalSeconds, 2))s"
}
