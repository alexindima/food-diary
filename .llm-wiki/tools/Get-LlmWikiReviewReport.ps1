[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string]$Objective,
    [object]$PacketInput,
    [string]$ManifestPath = '.artifacts/llm-wiki/change-manifest.json',
    [string]$AcceptancePath = '.artifacts/llm-wiki/acceptance-matrix.json',
    [string]$EvidencePath = '.artifacts/llm-wiki/evidence.json',
    [switch]$RequireManifest,
    [switch]$RequireAcceptance,
    [switch]$RequireEvidence,
    [ValidateSet('Markdown', 'Json')]
    [string]$Format = 'Markdown',
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path

function ConvertTo-MarkdownCell([object]$Value) {
    if ($null -eq $Value) { return '' }
    return ([string]$Value).Replace('|', '\|').Replace("`r", ' ').Replace("`n", ' ')
}

function Resolve-OutputPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return Join-Path $repositoryRoot $Path
}

$packetArguments = @{
    BaseRef = $BaseRef
    Objective = $Objective
    Format = 'Json'
}
if ($PSBoundParameters.ContainsKey('HeadRef')) { $packetArguments.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $packetArguments.ChangedPath = $ChangedPath }
$packet = if ($null -ne $PacketInput) {
    $PacketInput
} else {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiChangePacket.ps1') @packetArguments | ConvertFrom-Json
}

$readinessArguments = @{
    PacketInput = $packet
    ManifestPath = $ManifestPath
    AcceptancePath = $AcceptancePath
    EvidencePath = $EvidencePath
    RequireManifest = $RequireManifest
    RequireAcceptance = $RequireAcceptance
    RequireEvidence = $RequireEvidence
    Format = 'Json'
}
$readiness = & (Join-Path $PSScriptRoot 'Get-LlmWikiReleaseReadiness.ps1') @readinessArguments | ConvertFrom-Json

$report = [pscustomobject][ordered]@{
    schemaVersion = 1
    packetFingerprint = $packet.fingerprint
    objective = $packet.objective
    verdict = $readiness.verdict
    score = $readiness.score
    maximumScore = $readiness.maximumScore
    risk = $packet.brief.risk
    changedPathCount = @($packet.diff.changedPaths).Count
    scopes = @($packet.diff.scopes)
    modules = @($packet.diff.modules)
    dimensions = @($readiness.dimensions)
    requiredChecks = @($packet.policy.requiredChecks)
    reviewObligations = @($packet.policy.reviewObligations)
    testScenarios = @($packet.testPlan.scenarios)
    blockingDimensions = @($readiness.blockingDimensions)
    unassessedDimensions = @($readiness.unassessedDimensions)
}

if ($Format -eq 'Json') {
    $content = $report | ConvertTo-Json -Depth 12
} else {
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('## LLM Wiki change review')
    $lines.Add('')
    $lines.Add("**READINESS: $($report.verdict.ToUpperInvariant())** | score $($report.score)/$($report.maximumScore) | risk $($report.risk.level)")
    if (-not [string]::IsNullOrWhiteSpace([string]$report.objective)) {
        $lines.Add('')
        $lines.Add("Objective: $(ConvertTo-MarkdownCell $report.objective)")
    }
    $lines.Add('')
    $lines.Add("| Change | Value |")
    $lines.Add('| --- | --- |')
    $lines.Add("| Paths | $($report.changedPathCount) |")
    $lines.Add("| Scopes | $(ConvertTo-MarkdownCell (@($report.scopes) -join ', ')) |")
    $lines.Add("| Modules | $(ConvertTo-MarkdownCell (@($report.modules) -join ', ')) |")
    $lines.Add("| Packet | ``$($report.packetFingerprint)`` |")
    $lines.Add('')
    $lines.Add('### Readiness')
    $lines.Add('')
    $lines.Add('| Dimension | Status | Weight | Summary |')
    $lines.Add('| --- | --- | ---: | --- |')
    foreach ($dimension in $report.dimensions) {
        $lines.Add("| $(ConvertTo-MarkdownCell $dimension.id) | **$(ConvertTo-MarkdownCell $dimension.status)** | $($dimension.weight) | $(ConvertTo-MarkdownCell $dimension.summary) |")
    }

    $issues = @($report.dimensions | ForEach-Object {
        $dimensionId = $_.id
        @($_.issues | ForEach-Object { [pscustomobject]@{ dimension = $dimensionId; issue = $_ } })
    })
    if ($issues.Count -gt 0) {
        $lines.Add('')
        $lines.Add('### Findings')
        $lines.Add('')
        foreach ($issue in $issues) {
            $lines.Add("- **$(ConvertTo-MarkdownCell $issue.dimension):** $(ConvertTo-MarkdownCell $issue.issue)")
        }
    }

    $lines.Add('')
    $lines.Add('### Required checks')
    $lines.Add('')
    if (@($report.requiredChecks).Count -eq 0) {
        $lines.Add('- None inferred.')
    } else {
        foreach ($check in $report.requiredChecks) {
            $lines.Add("- ``$(ConvertTo-MarkdownCell $check.command)`` (policy: $(ConvertTo-MarkdownCell $check.sourceRule))")
        }
    }
    $lines.Add('')
    $lines.Add('### Review obligations')
    $lines.Add('')
    if (@($report.reviewObligations).Count -eq 0) {
        $lines.Add('- None inferred.')
    } else {
        foreach ($review in $report.reviewObligations) {
            $lines.Add("- **$(ConvertTo-MarkdownCell $review.id):** $(ConvertTo-MarkdownCell $review.description)")
        }
    }
    $lines.Add('')
    $lines.Add('### Suggested test scenarios')
    $lines.Add('')
    foreach ($scenario in @($report.testScenarios | Select-Object -First 12)) {
        $lines.Add("- **$(ConvertTo-MarkdownCell $scenario.id):** $(ConvertTo-MarkdownCell $scenario.description) Evidence: $(ConvertTo-MarkdownCell $scenario.evidence).")
    }
    $content = $lines -join [Environment]::NewLine
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $absoluteOutputPath = Resolve-OutputPath $OutputPath
    $outputDirectory = Split-Path -Parent $absoluteOutputPath
    if (-not (Test-Path -LiteralPath $outputDirectory)) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }
    [System.IO.File]::WriteAllText($absoluteOutputPath, $content + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Generated LLM Wiki review report: $OutputPath"
} else {
    $content
}
