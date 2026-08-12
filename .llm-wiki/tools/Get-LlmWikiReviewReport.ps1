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
$evidenceAbsolute = Resolve-OutputPath $EvidencePath
$evidence = if (Test-Path -LiteralPath $evidenceAbsolute -PathType Leaf) { Get-Content -LiteralPath $evidenceAbsolute -Raw | ConvertFrom-Json } else { $null }
$evidenceApplicable = $null -ne $evidence -and
    (@($evidence.change.changedPaths | Sort-Object) -join '|') -ceq (@($packet.diff.changedPaths | Sort-Object) -join '|')
$testCommands = @($packet.testPlan.commands)
$packetObjective = if ($packet.PSObject.Properties['objective']) { $packet.objective } else { $Objective }

$report = [pscustomobject][ordered]@{
    schemaVersion = 1
    packetFingerprint = $packet.fingerprint
    objective = $packetObjective
    verdict = $readiness.verdict
    engineeringReadiness = $readiness.engineeringReadiness
    governanceCompleteness = $readiness.governanceCompleteness
    score = $readiness.score
    maximumScore = $readiness.maximumScore
    risk = $packet.brief.risk
    changedPathCount = @($packet.diff.changedPaths).Count
    scopes = @($packet.diff.scopes)
    modules = @($packet.diff.modules | ForEach-Object { if ($_ -is [string]) { $_ } elseif ($null -ne $_.name) { [string]$_.name } else { [string]$_ } } | Where-Object { $_ } | Sort-Object -Unique)
    dimensions = @($readiness.dimensions)
    requiredChecks = @($packet.policy.requiredChecks)
    reviewObligations = @($packet.policy.reviewObligations)
    testScenarios = @($packet.testPlan.scenarios)
    testCommands = [pscustomobject][ordered]@{
        required = @($testCommands | Where-Object priority -eq 'required')
        recommended = @($testCommands | Where-Object priority -eq 'recommended')
        fullRegression = @($testCommands | Where-Object priority -eq 'full-regression')
    }
    executedChecks = $(if (-not $evidenceApplicable) { @() } else { @($evidence.checks) })
    generatedPaths = @($packet.diff.changedPaths | Where-Object { $_ -match '^\.llm-wiki/generated/' })
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
    $lines.Add("Engineering: **$($report.engineeringReadiness.verdict.ToUpperInvariant())** | Governance: **$($report.governanceCompleteness.verdict.ToUpperInvariant())**")
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
    $lines.Add('### Check execution')
    $lines.Add('')
    if (@($report.executedChecks).Count -eq 0) {
        $lines.Add('- No evidence bundle was found; commands below are recommendations, not proof of execution.')
    } else {
        foreach ($check in $report.executedChecks) {
            $duration = if ($null -eq $check.durationSeconds) { '' } else { ", $($check.durationSeconds)s" }
            $lines.Add(('- **{0}**{1} - `{2}`' -f (ConvertTo-MarkdownCell $check.status), $duration, (ConvertTo-MarkdownCell $check.command)))
        }
    }
    $lines.Add('')
    $lines.Add('### Additional verification tiers')
    $lines.Add('')
    foreach ($tier in @('recommended', 'fullRegression')) {
        foreach ($check in @($report.testCommands.$tier)) {
            $lines.Add(('- **{0}:** `{1}` - {2}' -f $tier, (ConvertTo-MarkdownCell $check.command), (ConvertTo-MarkdownCell $check.reason)))
        }
    }
    $lines.Add('')
    $lines.Add('### Generated artifacts')
    $lines.Add('')
    if (@($report.generatedPaths).Count -eq 0) { $lines.Add('- None changed.') }
    else { foreach ($path in $report.generatedPaths) { $lines.Add("- ``$(ConvertTo-MarkdownCell $path)``") } }
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
