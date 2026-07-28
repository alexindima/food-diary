[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [object]$DiffInput,
    [object]$PolicyInput,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$adrRoot = Join-Path $repositoryRoot 'docs/adr'

$common = @{ BaseRef = $BaseRef; Format = 'Json' }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $common.ChangedPath = $ChangedPath }
$diffArguments = @{} + $common
$diffArguments.Limit = 20
$diff = if ($null -ne $DiffInput) { $DiffInput } else {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1') @diffArguments | ConvertFrom-Json
}
$policy = if ($null -ne $PolicyInput) { $PolicyInput } else {
    & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') @common | ConvertFrom-Json
}

$terms = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($module in @($diff.modules.name)) { if ($module) { $null = $terms.Add($module) } }
foreach ($path in @($diff.changedPaths)) {
    if ($path -match 'module-dependencies\.json$') { $null = $terms.Add('dependency graph') }
    foreach ($segment in ($path -split '[/_.-]')) {
        if ($segment.Length -ge 5 -and $segment -notin @(
            'FoodDiary', 'Application', 'Infrastructure', 'Presentation',
            'architecture', 'module', 'modules', 'dependencies'
        )) {
            $null = $terms.Add($segment)
        }
    }
}

$related = [System.Collections.Generic.List[object]]::new()
foreach ($file in Get-ChildItem -LiteralPath $adrRoot -File -Filter '*.md' | Where-Object Name -match '^\d{4}-') {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $matchedTerms = @($terms | Where-Object { $content -match "\b$([regex]::Escape($_))\b" })
    if ($matchedTerms.Count -gt 0) {
        $related.Add([pscustomobject]@{
            path = $file.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
            matchedTerms = $matchedTerms
        })
    }
}

$triggered = @($policy.matchedRules.id) -contains 'architecture-decision'
$review = @($policy.reviewObligations | Where-Object id -eq 'adr-review' | Select-Object -First 1)
$result = [pscustomobject]@{
    reviewRequired = $triggered
    guidance = if ($review.Count -gt 0) { $review[0].description } else { 'No deterministic ADR trigger matched; create one if the change establishes a durable constraint.' }
    decisionDrivers = @(
        @($diff.scopes | ForEach-Object { "Changed scope: $_" }) +
        @($diff.modules.name | Where-Object { $_ } | ForEach-Object { "Affected module: $_" }) +
        @($diff.warnings | ForEach-Object { "Change warning: $_" })
    )
    relatedAdrs = @($related | Sort-Object path)
    template = 'docs/adr/template.md'
    index = 'docs/adr/README.md'
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 7
    exit 0
}

Write-Host "ADR review required: $($result.reviewRequired)"
Write-Host $result.guidance
Write-Host 'Decision context:'
foreach ($driver in $result.decisionDrivers) { Write-Host " - $driver" }
Write-Host 'Related ADRs:'
foreach ($adr in $result.relatedAdrs) { Write-Host " - $($adr.path) [$($adr.matchedTerms -join ', ')]" }
Write-Host "Template: $($result.template)"
