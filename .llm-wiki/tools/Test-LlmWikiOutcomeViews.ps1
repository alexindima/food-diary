[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiOutcomeViewFixture.ps1')
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repo -Name 'outcome-views'
$tools = Join-Path $fixture '.llm-wiki/tools'
$knowledge = Join-Path $fixture '.llm-wiki/knowledge'
$policies = Join-Path $fixture '.llm-wiki/policies'
$savedModelPath = $env:LLM_WIKI_MODEL_ROUTE_OUTCOME_REGISTRY_PATH
$savedContextPath = $env:LLM_WIKI_CONTEXT_OUTCOME_REGISTRY_PATH
function Assert-EqualJson($Expected, $Actual, [string]$Context) {
    if (($Expected | ConvertTo-Json -Depth 40 -Compress) -cne ($Actual | ConvertTo-Json -Depth 40 -Compress)) { throw "Outcome views changed $Context." }
}
try {
    $null = New-Item -ItemType Directory -Path $tools, $knowledge, $policies -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../policies/workspace-policies.json') -Destination $policies
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LlmWikiJson.ps1') -Destination $tools
    foreach ($kind in @('Model', 'Context', 'Instruction')) {
        $name = switch ($kind) {
            'Model' { 'Manage-LlmWikiModelRoutingOutcome.ps1' }
            'Context' { 'Manage-LlmWikiContextOutcome.ps1' }
            'Instruction' { 'Manage-LlmWikiInstructionOutcome.ps1' }
        }
        $registryName = switch ($kind) {'Model' {'model-routing-outcomes.json'} 'Context' {'context-strategy-outcomes.json'} 'Instruction' {'instruction-outcomes.json'}}
        $path = Join-Path $knowledge $registryName
        $env:LLM_WIKI_MODEL_ROUTE_OUTCOME_REGISTRY_PATH = Join-Path $knowledge 'model-routing-outcomes.json'
        $env:LLM_WIKI_CONTEXT_OUTCOME_REGISTRY_PATH = Join-Path $knowledge 'context-strategy-outcomes.json'
        $source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot $name))
        # Count real validation/profile construction, keeping production logic intact.
        $countPath = Join-Path $fixture "$kind-count.txt"
        $source = $source.Replace('function Test-Registry([object]$Registry) {', "function Test-Registry([object]`$Registry) {`n    [IO.File]::AppendAllText('$($countPath.Replace("'", "''"))', 'validate' + [Environment]::NewLine)")
        $profileAnchor = if ($kind -eq 'Instruction') {'function Get-Profiles([object]$Registry) {'} else {'function Get-Metrics([object]$Registry, [object]$Validation) {'}
        $source = $source.Replace($profileAnchor, $profileAnchor + "`n    [IO.File]::AppendAllText('$($countPath.Replace("'", "''"))', 'profiles' + [Environment]::NewLine)")
        $tool = Join-Path $tools $name
        [IO.File]::WriteAllText($tool, $source)
        $flag = if ($kind -eq 'Instruction') {'IncludeCandidates'} else {'IncludeHealth'}
        $view = if ($kind -eq 'Instruction') {'candidatesView'} else {'healthView'}
        $companionAction = if ($kind -eq 'Instruction') {'candidates'} else {'health'}
        $combinedArgs = @{Action='metrics';Format='Json';$flag=$true}
        foreach ($state in @('missing','empty','valid','tampered')) {
            $registry = if ($state -eq 'empty') {[pscustomobject]@{schemaVersion=$(if($kind -eq 'Instruction'){2}else{1});events=@()}} else {New-LlmWikiOutcomeViewRegistry -ToolsRoot $PSScriptRoot -FixtureRoot $fixture -Kind $kind}
            if ($state -eq 'tampered') { $registry.events[-1].eventHash = '0' * 64 }
            if ($state -ne 'missing') { [IO.File]::WriteAllText($path, ($registry | ConvertTo-Json -Depth 40)) }
            $plain = & $tool metrics -Format Json | ConvertFrom-Json
            $companion = & $tool $companionAction -Format Json | ConvertFrom-Json
            [IO.File]::WriteAllText($countPath, '')
            $combined = & $tool @combinedArgs | ConvertFrom-Json
            Assert-EqualJson $companion $combined.$view "$kind/$state companion"
            $combined.PSObject.Properties.Remove($view)
            Assert-EqualJson $plain $combined "$kind/$state default metrics"
            $calls = @([IO.File]::ReadAllLines($countPath))
            if (@($calls | Where-Object {$_ -eq 'validate'}).Count -ne 1 -or @($calls | Where-Object {$_ -eq 'profiles'}).Count -ne 1) { throw 'Combined views repeated validation or profile construction.' }
            if ($state -eq 'tampered' -and $combined.valid) { throw 'Combined view accepted tampered history.' }
            if ($state -eq 'missing' -and (Test-Path -LiteralPath $path)) { throw 'Reading missing history created a persistent registry.' }
            if ($state -eq 'valid' -and (-not $combined.valid -or $combined.metrics.validEventCount -ne 60)) { throw "Invalid $kind event fixture." }
        }
        $before = [IO.File]::ReadAllText($path)
        $rejected = $false
        $wrongArgs = @{Action='list';Format='Json'}
        $wrongArgs[$flag] = $true
        try { & $tool @wrongArgs | Out-Null } catch { $rejected = $_.Exception.Message -like '*supported only for metrics*' }
        if (-not $rejected -or [IO.File]::ReadAllText($path) -cne $before) { throw 'Companion flag on a non-metrics action was accepted or mutated history.' }
        # Restoration must trigger fresh validation instead of retaining invalid status.
        $restored = New-LlmWikiOutcomeViewRegistry -ToolsRoot $PSScriptRoot -FixtureRoot $fixture -Kind $kind
        [IO.File]::WriteAllText($path, ($restored | ConvertTo-Json -Depth 40))
        $fresh = & $tool @combinedArgs | ConvertFrom-Json
        if (-not $fresh.valid) { throw 'Restored history remained invalid.' }
        if ($kind -eq 'Instruction') {
            $sourcePath = Join-Path $fixture 'AGENTS.md'
            [IO.File]::AppendAllText($sourcePath, ' edited')
            $changed = & $tool @combinedArgs | ConvertFrom-Json
            if (@($changed.candidatesView.candidates).Count -eq 0 -or $changed.candidatesView.eligibleCount -ne 0) { throw 'Instruction candidates retained stale current-source identity.' }
        }
        $policyPath = Join-Path $policies 'workspace-policies.json'
        $policyText = [IO.File]::ReadAllText($policyPath)
        $changedPolicy = $policyText | ConvertFrom-Json
        switch ($kind) {
            'Model' { $changedPolicy.scheduler.verificationPlanner.modelRouting.outcomes.minimumSamples = 1000 }
            'Context' { $changedPolicy.scheduler.contextBundles.strategyOutcomes.minimumSamples = 1000 }
            'Instruction' { $changedPolicy.scheduler.verificationPlanner.instructionOutcomes.minimumSamples = 1000 }
        }
        [IO.File]::WriteAllText($policyPath, ($changedPolicy | ConvertTo-Json -Depth 100))
        $policyFresh = & $tool @combinedArgs | ConvertFrom-Json
        if ($kind -eq 'Instruction') {
            if (@($policyFresh.candidatesView.candidates).Count -ne 0) { throw 'Instruction candidates reused prior policy thresholds.' }
        } elseif ($policyFresh.healthView.minimumSamples -ne 1000) { throw 'Health reused prior policy thresholds.' }
        [IO.File]::WriteAllText($policyPath, $policyText)
    }
    Write-Host 'Outcome views passed: exact default/companion parity, one fresh validation/profile pass, invalid history, restoration and instruction source identity.'
} finally {
    $env:LLM_WIKI_MODEL_ROUTE_OUTCOME_REGISTRY_PATH = $savedModelPath
    $env:LLM_WIKI_CONTEXT_OUTCOME_REGISTRY_PATH = $savedContextPath
    $sandbox = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repo)).TrimEnd('\','/')
    $target = [IO.Path]::GetFullPath($fixture)
    if (-not $target.StartsWith($sandbox + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe outcome fixture cleanup.' }
    Remove-Item -LiteralPath $target -Recurse -Force
}
