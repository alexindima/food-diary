[CmdletBinding()]
param(
    [ValidateRange(20, 300)]
    [int]$Count = 100,
    [string]$OutputPath = '.artifacts/llm-wiki/evals/context-search-unseen-draft.json',
    [switch]$Force,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$absoluteOutput = if ([IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $repositoryRoot $OutputPath }
if ((Test-Path -LiteralPath $absoluteOutput) -and -not $Force) { throw "Unseen corpus draft already exists: $absoluteOutput. Use -Force to replace it." }
$usedPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/evals') -Filter 'context-search*.json' -File) {
    $corpus = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    foreach ($case in @($corpus.cases)) {
        $accepted = if ($null -ne $case.PSObject.Properties['acceptedPaths']) { @($case.acceptedPaths) } else { @() }
        foreach ($path in @($case.expectedPaths) + $accepted) {
            if (-not [string]::IsNullOrWhiteSpace([string]$path)) { $null = $usedPaths.Add(([string]$path).Replace('\\', '/')) }
        }
    }
}
$paths = @(& git -C $repositoryRoot ls-files '*.cs' '*.ts' '*.ps1' '*.mjs' | ForEach-Object { $_.Replace('\\', '/') } |
    Where-Object { -not $usedPaths.Contains($_) -and $_ -notmatch '(^|/)(bin|obj|Migrations|generated)/' })
function Get-Cohort([string]$Path) {
    if ($Path.StartsWith('.llm-wiki/tools/')) { return 'wiki-tooling' }
    if ($Path.StartsWith('tests/')) { return 'behavior-to-test' }
    if ($Path.StartsWith('FoodDiary.Web.Client/')) { return 'frontend' }
    if ($Path -match 'Domain/') { return 'domain-invariants' }
    if ($Path -match 'Infrastructure|Integrations|JobManager') { return 'integrations-persistence' }
    'application-api'
}
$ranked = @($paths | ForEach-Object {
    $bytes = [Text.Encoding]::UTF8.GetBytes($_)
    [pscustomobject]@{ path = $_; hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)); cohort = Get-Cohort $_ }
} | Sort-Object hash)
$cohorts = @('application-api', 'behavior-to-test', 'domain-invariants', 'frontend', 'integrations-persistence', 'wiki-tooling')
$selected = [Collections.Generic.List[object]]::new()
$cursor = 0
while ($selected.Count -lt $Count) {
    $cohort = $cohorts[$cursor % $cohorts.Count]
    $selectedPaths = @($selected | ForEach-Object path)
    $candidate = @($ranked | Where-Object { $_.cohort -eq $cohort -and $_.path -notin $selectedPaths } | Select-Object -First 1)
    if ($candidate.Count -eq 0) { $candidate = @($ranked | Where-Object { $_.path -notin $selectedPaths } | Select-Object -First 1) }
    if ($candidate.Count -eq 0) { break }
    $selected.Add($candidate[0]); $cursor++
}
$cases = @($selected | ForEach-Object -Begin { $index = 0 } -Process {
    $index++
    [pscustomobject][ordered]@{
        id = "unseen-v2-$('{0:d3}' -f $index)"
        cohort = $_.cohort
        query = '<independent-author-query-required>'
        changeType = $(if ($_.cohort -eq 'frontend') { 'Frontend' } elseif ($_.cohort -eq 'behavior-to-test') { 'Tests' } else { 'Any' })
        expectedPaths = @($_.path)
    }
})
$draft = [pscustomobject][ordered]@{
    schemaVersion = 1
    status = 'draft-unseen-not-executable'
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    methodology = 'Targets are deterministic, unused by committed context-search corpora, and require independently authored queries before freezing.'
    targetCount = $cases.Count
    cases = $cases
}
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteOutput) -Force
[IO.File]::WriteAllText($absoluteOutput, (($draft | ConvertTo-Json -Depth 8) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
$result = [pscustomobject]@{ outputPath = $absoluteOutput.Replace('\\', '/'); targetCount = $cases.Count; cohorts = @($cases | Group-Object cohort | ForEach-Object { [pscustomobject]@{ cohort = $_.Name; count = $_.Count } }) }
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 5 } else { Write-Host "Unseen context corpus draft: $($cases.Count) unused target(s), output=$($result.outputPath). Independent query authorship is required before freezing." }
