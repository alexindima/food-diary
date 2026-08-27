[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('simulate', 'assess', 'create', 'show', 'verify')]
    [string]$Action = 'assess',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string[]]$ProposedPath,
    [string]$Objective,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite',
    [switch]$IncludeDiagnostics,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$normalizedWorkspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $normalizedWorkspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$workspaceAbsolute = Join-Path $repositoryRoot $normalizedWorkspace
$manifestPath = Join-Path $workspaceAbsolute 'change-manifest.json'
$packetPath = Join-Path $workspaceAbsolute 'change-packet.json'
$receiptPath = Join-Path $workspaceAbsolute 'impact-simulation.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$impactPolicy = $policy.impactSimulation
. (Join-Path $PSScriptRoot 'LlmWikiImplementationBrief.ps1')

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    if ($null -eq $json) { $json = 'null' }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-FileHashValue([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-Signatures([object[]]$Items) {
    @($Items | Where-Object { $null -ne $_ } | ForEach-Object {
        if ($_ -is [string]) { [string]$_ } else { ConvertTo-Json -InputObject $_ -Depth 12 -Compress }
    } | Sort-Object -Unique)
}
function Get-Ids([object[]]$Items) {
    @($Items | ForEach-Object {
        if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id }
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
}
function Get-ImpactSnapshot([object]$Packet) {
    $brief = Normalize-LlmWikiImplementationBrief $Packet.brief
    $runtime = @(
        @($brief.runtimeImpact.hostedServices) +
        @($brief.runtimeImpact.httpClients) +
        @($brief.runtimeImpact.webhooks) +
        @($brief.runtimeImpact.recurringJobs) +
        @($brief.runtimeImpact.composeServices)
    )
    $frontend = @(
        @($brief.frontendContractImpact.components) +
        @($brief.frontendContractImpact.apiCalls) +
        @($brief.frontendContractImpact.translations)
    )
    $frontendConsumers = @(
        @($brief.frontendContractImpact.downstreamConsumers) +
        @($brief.frontendContractImpact.changedConsumers)
    )
    $data = @(
        @($brief.domainDataImpact.types) +
        @($brief.domainDataImpact.invariants) +
        @($brief.domainDataImpact.mappings)
    )
    $contracts = @($brief.backendContractImpact.contracts)
    $consumers = @($brief.backendContractImpact.downstreamConsumers)
    $privacy = @(
        @($brief.privacyImpact.changedCandidates) +
        @($brief.privacyImpact.boundaryFiles)
    )
    $snapshot = [pscustomobject][ordered]@{
        paths = @($Packet.diff.changedPaths | Sort-Object -Unique)
        scopes = @($Packet.diff.scopes | Sort-Object -Unique)
        directModules = @($Packet.ownership.directModules | Sort-Object -Unique)
        downstreamModules = @($Packet.ownership.downstreamModules | Sort-Object -Unique)
        requiredChecks = @(Get-Ids @($Packet.brief.requiredChecks))
        reviewObligations = @(Get-Ids @($Packet.brief.reviewObligations))
        contracts = @(Get-Signatures $contracts)
        consumers = @(Get-Signatures $consumers)
        runtimeBindings = @(Get-Signatures $runtime)
        dataBindings = @(Get-Signatures $data)
        frontendBindings = @(Get-Signatures $frontend)
        frontendConsumers = @(Get-Signatures $frontendConsumers)
        privacyBindings = @(Get-Signatures $privacy)
        risk = [pscustomobject][ordered]@{ level = [string]$brief.risk.level; score = [int]$brief.risk.score; reasons = @($brief.risk.reasons) }
    }
    $weighted = @($snapshot.scopes).Count * 3 +
        @($snapshot.directModules).Count * 4 +
        @($snapshot.downstreamModules).Count * 2 +
        @($snapshot.contracts).Count * 4 +
        @($snapshot.consumers).Count * 2 +
        @($snapshot.runtimeBindings).Count * 4 +
        @($snapshot.dataBindings).Count * 4 +
        @($snapshot.frontendBindings).Count * 3 +
        @($snapshot.frontendConsumers).Count * 2 +
        @($snapshot.privacyBindings).Count * 4
    $snapshot | Add-Member -NotePropertyName blastRadiusScore -NotePropertyValue ([Math]::Min(100, $weighted))
    $snapshot | Add-Member -NotePropertyName blastRadiusLevel -NotePropertyValue $(if ($weighted -ge 60) { 'critical' } elseif ($weighted -ge 35) { 'high' } elseif ($weighted -ge 15) { 'medium' } else { 'low' })
    $snapshot
}
function Get-Unexpected([object[]]$Actual, [object[]]$Forecast) {
    @($Actual | Where-Object { $_ -notin $Forecast } | Sort-Object -Unique)
}
function Get-ScopeAlignment([string]$ObjectiveText, [string[]]$Paths, [object[]]$FrontendFeatures) {
    $normalizedObjective = ([string]$ObjectiveText).ToLowerInvariant()
    $aliases = @{
        'dashboard' = @('dashboard')
        'meal' = @('meal', 'meals', 'meal')
        'food' = @('food', 'meal', 'meals', 'products')
        'photo' = @('photo', 'image', 'vision')
        'annotation' = @('annotation', 'photo', 'image', 'vision')
    }
    $terms = @(
        [regex]::Matches($normalizedObjective, '[\p{L}\p{Nd}]+') |
            ForEach-Object Value |
            Where-Object { $_.Length -ge 4 -and $_ -notin @('add', 'change', 'feature', 'implement', 'with', 'from', 'that', 'this') } |
            Sort-Object -Unique
    )
    $expanded = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($term in $terms) {
        $null = $expanded.Add($term)
        if ($aliases.ContainsKey($term)) {
            foreach ($alias in @($aliases[$term])) { $null = $expanded.Add($alias) }
        }
    }
    $pathText = (@($Paths) -join ' ').ToLowerInvariant()
    $matched = @($expanded | Where-Object { $pathText.Contains($_) } | Sort-Object)
    $expectedFeatures = @(
        $FrontendFeatures |
            Where-Object {
                $feature = $_
                $feature.name -in @($expanded)
            } |
            ForEach-Object name |
            Sort-Object -Unique
    )
    $requiredFeatures = @(
        @(
            foreach ($term in $terms) {
                if ($term -in @($FrontendFeatures.name)) { $term }
                if ($term -eq 'meal') { 'meals' }
            }
        ) | Sort-Object -Unique
    )
    $coveredFeatures = @($expectedFeatures | Where-Object { $pathText -match "/features/$([regex]::Escape($_))/" })
    $suggestedPaths = @(
        @(
            $FrontendFeatures |
                Where-Object name -in $expectedFeatures |
                ForEach-Object root
            if ($normalizedObjective -match 'photo|image|vision|annotation') {
                'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar'
            }
        ) | Sort-Object -Unique
    )
    $missingRequiredFeatures = @($requiredFeatures | Where-Object { $_ -notin $coveredFeatures })
    $aligned = if ($requiredFeatures.Count -gt 0) {
        $missingRequiredFeatures.Count -eq 0
    } else {
        $expectedFeatures.Count -eq 0 -or $coveredFeatures.Count -gt 0
    }
    [pscustomobject][ordered]@{
        status = if ($aligned) { 'aligned' } else { 'mismatch' }
        confidence = if ($expectedFeatures.Count -gt 0) { 'high' } elseif ($matched.Count -gt 0) { 'medium' } else { 'low' }
        objectiveTerms = @($expanded | Sort-Object)
        matchedPathTerms = $matched
        expectedFeatures = $expectedFeatures
        requiredFeatures = $requiredFeatures
        coveredFeatures = $coveredFeatures
        missingRequiredFeatures = $missingRequiredFeatures
        reasons = @(
            if (-not $aligned) { "Proposed paths do not cover objective feature(s) '$($missingRequiredFeatures -join ', ')'." }
        )
        suggestedPaths = $suggestedPaths
    }
}
function Get-Comparison([object]$Forecast, [object]$Actual) {
    $unexpected = [pscustomobject][ordered]@{
        scopes = @(Get-Unexpected $Actual.scopes $Forecast.scopes)
        modules = @(Get-Unexpected @($Actual.directModules + $Actual.downstreamModules) @($Forecast.directModules + $Forecast.downstreamModules))
        contracts = @(Get-Unexpected $Actual.contracts $Forecast.contracts)
        consumers = @(Get-Unexpected @($Actual.consumers + $Actual.frontendConsumers) @($Forecast.consumers + $Forecast.frontendConsumers))
        runtimeBindings = @(Get-Unexpected $Actual.runtimeBindings $Forecast.runtimeBindings)
        dataBindings = @(Get-Unexpected $Actual.dataBindings $Forecast.dataBindings)
        frontendBindings = @(Get-Unexpected $Actual.frontendBindings $Forecast.frontendBindings)
        requiredChecks = @(Get-Unexpected $Actual.requiredChecks $Forecast.requiredChecks)
        reviewObligations = @(Get-Unexpected $Actual.reviewObligations $Forecast.reviewObligations)
    }
    $missing = [pscustomobject][ordered]@{
        scopes = @(Get-Unexpected $Forecast.scopes $Actual.scopes)
        modules = @(Get-Unexpected @($Forecast.directModules + $Forecast.downstreamModules) @($Actual.directModules + $Actual.downstreamModules))
        contracts = @(Get-Unexpected $Forecast.contracts $Actual.contracts)
        consumers = @(Get-Unexpected @($Forecast.consumers + $Forecast.frontendConsumers) @($Actual.consumers + $Actual.frontendConsumers))
        runtimeBindings = @(Get-Unexpected $Forecast.runtimeBindings $Actual.runtimeBindings)
        dataBindings = @(Get-Unexpected $Forecast.dataBindings $Actual.dataBindings)
        frontendBindings = @(Get-Unexpected $Forecast.frontendBindings $Actual.frontendBindings)
    }
    [pscustomobject][ordered]@{
        unexpected = $unexpected
        missingForecastImpacts = $missing
        forecastScore = [int]$Forecast.blastRadiusScore
        actualScore = [int]$Actual.blastRadiusScore
        scoreDelta = [int]$Actual.blastRadiusScore - [int]$Forecast.blastRadiusScore
    }
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = $Receipt.schemaVersion
        workspace = $Receipt.workspace
        simulatedAtUtc = $Receipt.simulatedAtUtc
        manifestHash = $Receipt.manifestHash
        packetHash = $Receipt.packetHash
        packetFingerprint = $Receipt.packetFingerprint
        policyFingerprint = $Receipt.policyFingerprint
        forecastPacketFingerprint = $Receipt.forecastPacketFingerprint
        forecast = $Receipt.forecast
        actual = $Receipt.actual
        comparison = $Receipt.comparison
        findings = @($Receipt.findings)
        valid = $Receipt.valid
    }
}
function Get-Assessment {
    foreach ($requiredPath in @($manifestPath, $packetPath)) {
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) { throw "Required simulation input is absent: $requiredPath" }
    }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $actualPacket = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    $plannedPaths = @($manifest.scope.plannedPaths | Sort-Object -Unique)
    $forecastPacket = & (Join-Path $PSScriptRoot 'Get-LlmWikiChangePacket.ps1') `
        -BaseRef ([string]$manifest.git.base) `
        -Objective ([string]$manifest.objective) `
        -ChangedPath $plannedPaths `
        -Format Json | ConvertFrom-Json
    $forecast = Get-ImpactSnapshot $forecastPacket
    $actual = Get-ImpactSnapshot $actualPacket
    $comparison = Get-Comparison $forecast $actual
    $findings = [Collections.Generic.List[object]]::new()
    $limits = [ordered]@{
        scopes = [int]$impactPolicy.maximumUnexpectedScopes
        modules = [int]$impactPolicy.maximumUnexpectedModules
        contracts = [int]$impactPolicy.maximumUnexpectedContracts
        consumers = [int]$impactPolicy.maximumUnexpectedConsumers
        runtimeBindings = [int]$impactPolicy.maximumUnexpectedRuntimeBindings
        dataBindings = [int]$impactPolicy.maximumUnexpectedDataBindings
        frontendBindings = [int]$impactPolicy.maximumUnexpectedFrontendBindings
    }
    foreach ($entry in $limits.GetEnumerator()) {
        $count = @($comparison.unexpected.($entry.Key)).Count
        if ($count -gt $entry.Value) {
            $findings.Add([pscustomobject][ordered]@{ id = "unexpected-$($entry.Key)"; severity = 'block'; count = $count; maximum = $entry.Value })
        }
    }
    [pscustomobject]@{
        manifest = $manifest
        actualPacket = $actualPacket
        forecastPacket = $forecastPacket
        forecast = $forecast
        actual = $actual
        comparison = $comparison
        findings = @($findings)
        valid = $findings.Count -eq 0
    }
}
function New-Receipt([object]$Assessment) {
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $normalizedWorkspace
        simulatedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        manifestHash = Get-FileHashValue $manifestPath
        packetHash = Get-FileHashValue $packetPath
        packetFingerprint = [string]$Assessment.actualPacket.fingerprint
        policyFingerprint = Get-FileHashValue $policyPath
        forecastPacketFingerprint = [string]$Assessment.forecastPacket.fingerprint
        forecast = $Assessment.forecast
        actual = $Assessment.actual
        comparison = $Assessment.comparison
        findings = @($Assessment.findings)
        valid = [bool]$Assessment.valid
        simulationHash = ''
    }
    $receipt.simulationHash = Get-Hash (Get-Payload $receipt)
    $receipt
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    $current = New-Receipt (Get-Assessment)
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $normalizedWorkspace) { $issues.Add('Workspace does not match.') }
    foreach ($name in @('manifestHash', 'packetHash', 'packetFingerprint', 'policyFingerprint', 'forecastPacketFingerprint')) {
        if ([string]$Receipt.$name -cne [string]$current.$name) { $issues.Add("$name drifted.") }
    }
    foreach ($name in @('forecast', 'actual', 'comparison')) {
        if ((Get-Hash $Receipt.$name) -cne (Get-Hash $current.$name)) { $issues.Add("Impact $name drifted.") }
    }
    if ((Get-Hash @($Receipt.findings)) -cne (Get-Hash @($current.findings))) { $issues.Add('Impact findings drifted.') }
    if ([bool]$Receipt.valid -ne [bool]$current.valid) { $issues.Add('Impact verdict drifted.') }
    if ([string]$Receipt.simulationHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Impact simulation hash is invalid.') }
    [pscustomobject]@{ valid = $issues.Count -eq 0 -and [bool]$Receipt.valid; integrityValid = $issues.Count -eq 0; issues = @($issues) }
}

if ($Action -eq 'simulate') {
    $paths = @($ProposedPath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
    if ($paths.Count -eq 0) { throw 'simulate requires ProposedPath.' }
    $packet = & (Join-Path $PSScriptRoot 'Get-LlmWikiChangePacket.ps1') -Objective $Objective -ChangedPath $paths -Format Json | ConvertFrom-Json
    $snapshot = Get-ImpactSnapshot $packet
    $featureCatalogStopwatch = [Diagnostics.Stopwatch]::StartNew()
    if ($CompiledIndexSource -eq 'Sqlite') {
        $frontendFeatures = @($packet.diff.compiledIndex.frontendFeatures)
        $frontendSourceHash = [string]$packet.diff.compiledIndex.sourceHashes.frontend
        if ([string]$packet.diff.compiledIndex.source -ne 'sqlite-compiled-index' -or
            $frontendFeatures.Count -eq 0 -or [string]::IsNullOrWhiteSpace($frontendSourceHash)) {
            throw 'SQLite frontend feature projection is unavailable in the change packet. Run ./.llm-wiki/wiki.ps1 graph-build and retry.'
        }
        $featureCatalogStopwatch.Stop()
        $featureCatalogJson = $frontendFeatures | ConvertTo-Json -Depth 8 -Compress
        $featureCatalogDiagnostics = [ordered]@{
            source = 'sqlite-compiled-index-reused'
            sourceHash = $frontendSourceHash
            sourceRecords = $frontendFeatures.Count
            sourceBytesVerified = [int64]$packet.diff.compiledIndex.sourceBytesVerified.frontend
            sourceBytesMaterialized = [Text.Encoding]::UTF8.GetByteCount($featureCatalogJson)
            selectionRoundTripDurationMs = [double]$packet.diff.compiledIndex.roundTripDurationMs
            incrementalRoundTripDurationMs = [Math]::Round($featureCatalogStopwatch.Elapsed.TotalMilliseconds, 2)
        }
    } else {
        $frontendIndexRaw = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/frontend-index.json') -Raw
        $frontendIndex = $frontendIndexRaw | ConvertFrom-Json
        $frontendFeatures = @($frontendIndex.features)
        $featureCatalogStopwatch.Stop()
        $sourceBytes = [Text.Encoding]::UTF8.GetByteCount($frontendIndexRaw)
        $featureCatalogDiagnostics = [ordered]@{
            source = 'json-baseline'
            sourceHash = $null
            sourceRecords = $frontendFeatures.Count
            sourceBytesVerified = $sourceBytes
            sourceBytesMaterialized = $sourceBytes
            selectionRoundTripDurationMs = $null
            incrementalRoundTripDurationMs = [Math]::Round($featureCatalogStopwatch.Elapsed.TotalMilliseconds, 2)
        }
    }
    $alignment = Get-ScopeAlignment $Objective $paths $frontendFeatures
    $result = [pscustomobject][ordered]@{ action = 'simulate'; valid = $true; proposedPaths = $paths; packetFingerprint = $packet.fingerprint; alignment = $alignment; impact = $snapshot }
    if ($IncludeDiagnostics) { $result | Add-Member -NotePropertyName _diagnostics -NotePropertyValue ([pscustomobject]@{ frontendFeatures = [pscustomobject]$featureCatalogDiagnostics }) }
} elseif ($Action -in @('assess', 'create')) {
    $receipt = New-Receipt (Get-Assessment)
    if ($Action -eq 'create') {
        $temporaryPath = "$receiptPath.$([guid]::NewGuid().ToString('N')).tmp"
        try {
            [IO.File]::WriteAllText($temporaryPath, (($receipt | ConvertTo-Json -Depth 40) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
            if (Test-Path -LiteralPath $receiptPath) { [IO.File]::Delete($receiptPath) }
            [IO.File]::Move($temporaryPath, $receiptPath)
        } finally {
            if (Test-Path -LiteralPath $temporaryPath) { [IO.File]::Delete($temporaryPath) }
        }
    }
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $receipt.valid; issues = @(); simulation = $receipt; savedPath = $(if ($Action -eq 'create') { "$normalizedWorkspace/impact-simulation.json" } else { $null }) }
} else {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw "Impact simulation is absent: $normalizedWorkspace/impact-simulation.json" }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $validation = Test-Receipt $receipt
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $validation.valid; integrityValid = $validation.integrityValid; issues = @($validation.issues); simulation = $receipt }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 40 } else {
    if ($Action -eq 'simulate') {
        Write-Host "Impact simulation: paths=$(@($result.proposedPaths).Count), blast=$($result.impact.blastRadiusLevel) ($($result.impact.blastRadiusScore)/100)"
        Write-Host "Objective/path alignment: $($result.alignment.status) (confidence=$($result.alignment.confidence))"
        foreach ($reason in @($result.alignment.reasons)) { Write-Host " - $reason" }
        foreach ($path in @($result.alignment.suggestedPaths)) { Write-Host " - Suggested: $path" }
    } else {
        Write-Host "Impact simulation: action=$($result.action), valid=$($result.valid), forecast=$($result.simulation.forecast.blastRadiusScore), actual=$($result.simulation.actual.blastRadiusScore), delta=$($result.simulation.comparison.scoreDelta)"
        foreach ($finding in @($result.simulation.findings)) { Write-Host " - [$($finding.severity)] $($finding.id): $($finding.count)" }
        foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
    }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
