[CmdletBinding()]
param(
    [switch]$Detailed,
    [ValidateRange(0, 15)]
    [int]$ShardIndex = 0,
    [ValidateRange(1, 16)]
    [int]$ShardCount = 1
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$casesPath = Join-Path $wikiRoot 'evals/cases.json'
$cases = Get-Content -LiteralPath $casesPath -Raw | ConvertFrom-Json
$promoted = & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvalPromotion.ps1') list -FailOnInvalid -Format Json | ConvertFrom-Json
$promotedCases = @($promoted.candidates | Where-Object materialization -eq 'applied' | ForEach-Object { $_.case })
$cases.cases = @($cases.cases) + $promotedCases
if ($ShardIndex -ge $ShardCount) { throw 'ShardIndex must be smaller than ShardCount.' }
$policyPath = Join-Path $wikiRoot 'policies/change-policies.json'
$policyDefinition = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$failures = [System.Collections.Generic.List[string]]::new()
$assertions = 0
$passedAssertions = 0

$caseIds = @($cases.cases.id)
$assertions++
if (@($caseIds | Sort-Object -Unique).Count -eq $caseIds.Count) {
    $passedAssertions++
} else {
    $failures.Add('Eval case ids are not unique across static and promoted cases.')
}

function Test-ExpectedSet {
    param(
        [string]$CaseId,
        [string]$Label,
        [object[]]$Expected,
        [object[]]$Actual
    )

    foreach ($expectedItem in @($Expected)) {
        $script:assertions++
        if (@($Actual) -contains $expectedItem) {
            $script:passedAssertions++
        } else {
            $script:failures.Add("${CaseId}: missing ${Label} '$expectedItem'")
        }
    }
}

$ruleIds = @($policyDefinition.rules | ForEach-Object { $_.id })
$assertions++
if (@($ruleIds | Sort-Object -Unique).Count -eq $ruleIds.Count) {
    $passedAssertions++
} else {
    $failures.Add('Policy rule ids are not unique.')
}
foreach ($rule in $policyDefinition.rules) {
    $assertions++
    if (-not [string]::IsNullOrWhiteSpace([string]$rule.id) -and @($rule.pathPatterns).Count -gt 0) {
        $passedAssertions++
    } else {
        $failures.Add('Every policy rule requires an id and at least one path pattern.')
    }
}

$selectedCases = @(
    for ($caseIndex = 0; $caseIndex -lt $cases.cases.Count; $caseIndex++) {
        if (($caseIndex % $ShardCount) -eq $ShardIndex) { $cases.cases[$caseIndex] }
    }
)
foreach ($case in $selectedCases) {
    $expectedPlanPhases = @(if ($case.PSObject.Properties['expectedPlanPhases']) { @($case.expectedPlanPhases) })
    $expectedAdaptiveProfile = if ($case.PSObject.Properties['expectedAdaptiveProfile']) { [string]$case.expectedAdaptiveProfile } else { '' }
    $adaptiveObjective = if ($case.PSObject.Properties['adaptiveObjective']) { [string]$case.adaptiveObjective } else { '' }
    $expectedAdaptiveStages = @(if ($case.PSObject.Properties['expectedAdaptiveStages']) { @($case.expectedAdaptiveStages) })
    $unexpectedAdaptiveStages = @(if ($case.PSObject.Properties['unexpectedAdaptiveStages']) { @($case.unexpectedAdaptiveStages) })
    $traceQuery = if ($case.PSObject.Properties['traceQuery']) { [string]$case.traceQuery } else { '' }
    $expectedTraceRequests = @(if ($case.PSObject.Properties['expectedTraceRequests']) { @($case.expectedTraceRequests) })
    $privacyQuery = if ($case.PSObject.Properties['privacyQuery']) { [string]$case.privacyQuery } else { '' }
    $privacyCategory = if ($case.PSObject.Properties['privacyCategory']) { [string]$case.privacyCategory } else { '' }
    $expectedPrivacyFields = @(if ($case.PSObject.Properties['expectedPrivacyFields']) { @($case.expectedPrivacyFields) })
    $diffJson = & (Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1') `
        -ChangedPath @($case.changedPaths) `
        -Format Json
    $policyJson = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') `
        -ChangedPath @($case.changedPaths) `
        -Format Json
    $diff = $diffJson | ConvertFrom-Json
    $policy = $policyJson | ConvertFrom-Json
    $moduleNames = @(@($diff.modules) | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['name']) { [string]$_.name } })
    $scopeNames = @($diff.scopes)
    $matchedRuleIds = @(@($policy.matchedRules) | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id } })
    $requiredCheckIds = @(@($policy.requiredChecks) | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id } })
    $violationRules = @(
        @($policy.violations) |
            ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['rule']) { [string]$_.rule } } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
    )

    Test-ExpectedSet $case.id 'module' @($case.expectedModules) $moduleNames
    Test-ExpectedSet $case.id 'scope' @($case.expectedScopes) $scopeNames
    Test-ExpectedSet $case.id 'rule' @($case.expectedRules) $matchedRuleIds
    Test-ExpectedSet $case.id 'check' @($case.expectedChecks) $requiredCheckIds
    Test-ExpectedSet $case.id 'violation rule' @($case.expectedViolationRules) $violationRules
    if ($expectedPlanPhases.Count -gt 0) {
        $planJson = & (Join-Path $PSScriptRoot 'Get-LlmWikiImplementationPlan.ps1') `
            -ChangedPath @($case.changedPaths) `
            -Objective "Eval: $($case.id)" `
            -Format Json
        $implementationPlan = $planJson | ConvertFrom-Json
        Test-ExpectedSet $case.id 'implementation phase' $expectedPlanPhases @($implementationPlan.phases.id)
        $assertions++
        $orders = @($implementationPlan.phases.order)
        if (($orders -join ',') -eq ((1..$orders.Count) -join ',')) {
            $passedAssertions++
        } else {
            $failures.Add("$($case.id): implementation phases are not sequentially ordered")
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($expectedAdaptiveProfile)) {
        $adaptiveJson = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') `
            -Objective $adaptiveObjective `
            -ChangedPath @($case.changedPaths) `
            -Format Json
        $adaptive = $adaptiveJson | ConvertFrom-Json
        $assertions++
        if ([string]$adaptive.profile -eq $expectedAdaptiveProfile) {
            $passedAssertions++
        } else {
            $failures.Add("$($case.id): expected adaptive profile '$expectedAdaptiveProfile', got '$($adaptive.profile)'")
        }
        Test-ExpectedSet $case.id 'adaptive stage' $expectedAdaptiveStages @($adaptive.stages.id)
        foreach ($unexpectedStage in $unexpectedAdaptiveStages) {
            $assertions++
            if (@($adaptive.stages.id) -notcontains $unexpectedStage) {
                $passedAssertions++
            } else {
                $failures.Add("$($case.id): unexpected adaptive stage '$unexpectedStage'")
            }
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($traceQuery)) {
        $traceJson = & (Join-Path $PSScriptRoot 'Find-LlmWikiTrace.ps1') `
            -Query $traceQuery `
            -Format Json
        $trace = @($traceJson | ConvertFrom-Json | ForEach-Object { $_ })
        $traceRequests = @($trace | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['request']) { [string]$_.request } })
        Test-ExpectedSet $case.id 'trace request' $expectedTraceRequests $traceRequests
    }
    if (-not [string]::IsNullOrWhiteSpace($privacyQuery)) {
        $privacyArguments = @{
            Query = $privacyQuery
            Format = 'Json'
        }
        if (-not [string]::IsNullOrWhiteSpace($privacyCategory)) {
            $privacyArguments.Category = $privacyCategory
        }
        $privacyJson = & (Join-Path $PSScriptRoot 'Find-LlmWikiSensitiveData.ps1') @privacyArguments
        $privacy = $privacyJson | ConvertFrom-Json
        $privacyFields = @($privacy.items | ForEach-Object { "$($_.category):$($_.name)" })
        Test-ExpectedSet $case.id 'privacy field' $expectedPrivacyFields $privacyFields
    }

    $assertions++
    $unexpectedViolationRules = @($violationRules | Where-Object {
        @($case.expectedViolationRules) -notcontains $_
    })
    if ($unexpectedViolationRules.Count -eq 0) {
        $passedAssertions++
    } else {
        $failures.Add("$($case.id): unexpected violation(s): $($unexpectedViolationRules -join ', ')")
    }

    if ($Detailed) {
        Write-Host " - $($case.id): modules=$($moduleNames -join ','), rules=$($matchedRuleIds -join ','), violations=$($violationRules -join ',')"
    }
}

$score = if ($assertions -gt 0) {
    [Math]::Round(($passedAssertions / $assertions) * 100, 2)
} else {
    0
}

if ($failures.Count -gt 0) {
    Write-Host "LLM Wiki evals failed: $passedAssertions/$assertions assertions passed ($score%)."
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host "LLM Wiki evals passed: $($selectedCases.Count)/$($cases.cases.Count) cases in shard $($ShardIndex + 1)/$ShardCount ($($promotedCases.Count) promoted), $passedAssertions/$assertions assertions ($score%)."
