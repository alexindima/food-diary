[CmdletBinding()]
param(
    [switch]$Detailed
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$casesPath = Join-Path $wikiRoot 'evals/cases.json'
$cases = Get-Content -LiteralPath $casesPath -Raw | ConvertFrom-Json
$promoted = & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvalPromotion.ps1') list -FailOnInvalid -Format Json | ConvertFrom-Json
$promotedCases = @($promoted.candidates | Where-Object materialization -eq 'applied' | ForEach-Object { $_.case })
$cases.cases = @($cases.cases) + $promotedCases
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

foreach ($case in $cases.cases) {
    $diffJson = & (Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1') `
        -ChangedPath @($case.changedPaths) `
        -Format Json
    $policyJson = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') `
        -ChangedPath @($case.changedPaths) `
        -Format Json
    $diff = $diffJson | ConvertFrom-Json
    $policy = $policyJson | ConvertFrom-Json

    Test-ExpectedSet $case.id 'module' @($case.expectedModules) @($diff.modules.name)
    Test-ExpectedSet $case.id 'scope' @($case.expectedScopes) @($diff.scopes)
    Test-ExpectedSet $case.id 'rule' @($case.expectedRules) @($policy.matchedRules.id)
    Test-ExpectedSet $case.id 'check' @($case.expectedChecks) @($policy.requiredChecks.id)
    Test-ExpectedSet $case.id 'violation rule' @($case.expectedViolationRules) @($policy.violations.rule)
    if ($null -ne $case.expectedPlanPhases) {
        $planJson = & (Join-Path $PSScriptRoot 'Get-LlmWikiImplementationPlan.ps1') `
            -ChangedPath @($case.changedPaths) `
            -Objective "Eval: $($case.id)" `
            -Format Json
        $implementationPlan = $planJson | ConvertFrom-Json
        Test-ExpectedSet $case.id 'implementation phase' @($case.expectedPlanPhases) @($implementationPlan.phases.id)
        $assertions++
        $orders = @($implementationPlan.phases.order)
        if (($orders -join ',') -eq ((1..$orders.Count) -join ',')) {
            $passedAssertions++
        } else {
            $failures.Add("$($case.id): implementation phases are not sequentially ordered")
        }
    }

    $assertions++
    $unexpectedViolations = @($policy.violations | Where-Object {
        @($case.expectedViolationRules) -notcontains $_.rule
    })
    if ($unexpectedViolations.Count -eq 0) {
        $passedAssertions++
    } else {
        $failures.Add("$($case.id): unexpected violation(s): $($unexpectedViolations.rule -join ', ')")
    }

    if ($Detailed) {
        Write-Host " - $($case.id): modules=$(@($diff.modules.name) -join ','), rules=$(@($policy.matchedRules.id) -join ','), violations=$(@($policy.violations.rule) -join ',')"
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

Write-Host "LLM Wiki evals passed: $($cases.cases.Count) cases ($($promotedCases.Count) promoted), $passedAssertions/$assertions assertions ($score%)."
