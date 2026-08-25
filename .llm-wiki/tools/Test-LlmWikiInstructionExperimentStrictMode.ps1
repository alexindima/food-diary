[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$toolPath = Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionExperiment.ps1'
$source = Get-Content -LiteralPath $toolPath -Raw
$forbiddenPatterns = @(
    '\.PSObject\.Properties\.Count'
    '\$matchedCohorts\.weight'
    '\$cohorts\.remainingCandidateSamples'
)
foreach ($pattern in $forbiddenPatterns) {
    if ($source -match $pattern) {
        throw "Instruction experiment contains a strict-mode-unsafe collection access: $pattern"
    }
}
$outcomeTools = @(
    'Manage-LlmWikiContextOutcome.ps1'
    'Manage-LlmWikiInstructionOutcome.ps1'
)
foreach ($outcomeTool in $outcomeTools) {
    $outcomeSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot $outcomeTool) -Raw
    if ($outcomeSource -match '\$registry\.events\.completionFingerprint' -or
        $outcomeSource -notmatch '\$registry\.events \| ForEach-Object \{ \[string\]\$_\.completionFingerprint \}') {
        throw "$outcomeTool contains a strict-mode-unsafe empty-registry deduplication path."
    }
}
$migrationSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Update-LlmWikiTaskWorkspace.ps1') -Raw
if ($migrationSource -match '\$descriptor\.artifacts\.\(\$artifact\.Key\)' -or
    $migrationSource -notmatch '\$descriptor\.artifacts\.PSObject\.Properties\[\[string\]\$artifact\.Key\]') {
    throw 'Task-workspace migration contains a strict-mode-unsafe legacy artifact lookup.'
}
foreach ($requiredPattern in @(
    '@\(\$Value\.PSObject\.Properties\)\.Count'
    'Get-PropertySum \$matchedCohorts ''weight'''
    'Get-PropertySum \$cohorts ''remainingCandidateSamples'''
)) {
    if ($source -notmatch $requiredPattern) {
        throw "Instruction experiment lost a strict-mode-safe collection guard: $requiredPattern"
    }
}

$value = [pscustomobject]@{ name = 'fixture' }
if (@($value.PSObject.Properties).Count -ne 1) {
    throw 'Strict-mode-safe PSObject property enumeration changed unexpectedly.'
}
$empty = @()
$emptyMeasure = $empty | Measure-Object -Property weight -Sum
$emptySum = if ($null -eq $emptyMeasure -or $null -eq $emptyMeasure.PSObject.Properties['Sum'] -or $null -eq $emptyMeasure.Sum) { 0 } else { $emptyMeasure.Sum }
if ([int]$emptySum -ne 0) {
    throw 'Strict-mode-safe empty cohort aggregation changed unexpectedly.'
}

Write-Host 'LLM Wiki outcome strict-mode regression passed: empty properties, cohorts, and registries are safe.'
