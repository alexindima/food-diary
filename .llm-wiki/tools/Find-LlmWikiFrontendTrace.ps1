[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Query,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 30)]
    [int]$Limit = 10,
    [switch]$Compact,
    [string]$IndexRoot
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($IndexRoot)) { $IndexRoot = Join-Path $wikiRoot 'generated' }
$frontendIndexPath = Join-Path $IndexRoot 'frontend-index.json'
$contractIndexPath = Join-Path $IndexRoot 'frontend-contract-index.json'

function Read-FrontendIndex([string]$Path, [string[]]$RootProperties, [hashtable]$CollectionContracts) {
    try { $index = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json } catch {
        throw "Wiki index '$Path' is unreadable: $($_.Exception.Message). Run ./.llm-wiki/wiki.ps1 update -AffectedOnly."
    }
    $missingRoot = @($RootProperties | Where-Object { -not $index.PSObject.Properties[$_] })
    if ($missingRoot.Count -gt 0) {
        throw "Wiki index '$Path' has an incompatible schema (missing: $($missingRoot -join ', ')). Run ./.llm-wiki/wiki.ps1 update -AffectedOnly."
    }
    if ([int]$index.schemaVersion -ne 1) {
        throw "Wiki index '$Path' has unsupported schemaVersion '$($index.schemaVersion)' (expected 1). Run ./.llm-wiki/wiki.ps1 update -AffectedOnly."
    }
    foreach ($collectionName in $CollectionContracts.Keys) {
        $ordinal = 0
        foreach ($item in @($index.$collectionName)) {
            $missing = @($CollectionContracts[$collectionName] | Where-Object { -not $item.PSObject.Properties[$_] })
            if ($missing.Count -gt 0) {
                throw "Wiki index '$Path' has an incompatible '$collectionName' item at index $ordinal (missing: $($missing -join ', ')). Run ./.llm-wiki/wiki.ps1 update -AffectedOnly."
            }
            $ordinal++
        }
    }
    return $index
}

$frontendIndex = Read-FrontendIndex $frontendIndexPath @('schemaVersion','symbols','routes') @{
    symbols = @('name','role','path','line')
    routes = @('path','source','line')
}
$contractIndex = Read-FrontendIndex $contractIndexPath @('schemaVersion','components','apiCalls','consumerEdges') @{
    components = @('class','path')
    apiCalls = @('path','line')
    consumerEdges = @('component','consumerPath')
}

$queryText = $Query.ToLowerInvariant()
$queryTerms = @(
    [regex]::Matches($queryText, '[a-z0-9]+') |
        ForEach-Object Value |
        Where-Object { $_.Length -ge 3 } |
        Sort-Object -Unique
)
$symbols = @($frontendIndex.symbols)
$matches = @(
    $symbols |
        ForEach-Object {
            $selector = if ($_.PSObject.Properties['selector']) { [string]$_.selector } else { '' }
            $searchable = "$($_.name) $selector $($_.path)".ToLowerInvariant()
            $matchedTerms = @($queryTerms | Where-Object { $searchable.Contains($_) })
            $score = $matchedTerms.Count * 10
            if ($_.name.ToLowerInvariant() -eq $queryText -or $selector -eq $queryText) { $score += 100 }
            if ($score -gt 0) {
                [pscustomobject]@{ symbol = $_; score = $score; matchedTerms = $matchedTerms }
            }
        } |
        Sort-Object @{ Expression = 'score'; Descending = $true }, @{ Expression = { $_.symbol.name } } |
        Select-Object -First $Limit
)

if ($matches.Count -eq 0) {
    if ($Format -eq 'Json') {
        [pscustomobject]@{ matched = $false; query = $Query; traces = @() } | ConvertTo-Json -Depth 5
        exit 0
    }
    Write-Host "No frontend symbols matched '$Query'."
    exit 1
}

$documents = @{}
foreach ($symbol in $symbols) {
    if (-not $documents.ContainsKey($symbol.path)) {
        $absolutePath = Join-Path (Split-Path -Parent $wikiRoot) $symbol.path
        if (Test-Path -LiteralPath $absolutePath) {
            $documents[$symbol.path] = [System.IO.File]::ReadAllText($absolutePath)
        }
    }
}

function Get-Consumers {
    param([string]$SymbolName, [string]$ExcludePath)
    $found = @(
        foreach ($candidate in $symbols) {
            if ($candidate.path -eq $ExcludePath -or -not $documents.ContainsKey($candidate.path)) { continue }
            if ($documents[$candidate.path] -match "\b$([regex]::Escape($SymbolName))\b") {
                [pscustomobject]@{
                    name = $candidate.name
                    role = $candidate.role
                    path = $candidate.path
                    line = $candidate.line
                }
            }
        }
    )
    return @($found | Sort-Object path, name -Unique)
}

$traces = @(
    foreach ($match in $matches) {
        $target = $match.symbol
        $related = [System.Collections.Generic.List[object]]::new()
        $queue = [System.Collections.Generic.Queue[object]]::new()
        $visited = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        $queue.Enqueue([pscustomobject]@{ symbol = $target; depth = 0; relation = 'target' })
        while ($queue.Count -gt 0 -and $related.Count -lt 40) {
            $current = $queue.Dequeue()
            if (-not $visited.Add($current.symbol.name)) { continue }
            if ($current.depth -gt 0) {
                $related.Add([pscustomobject]@{
                    name = $current.symbol.name
                    role = $current.symbol.role
                    path = $current.symbol.path
                    line = $current.symbol.line
                    relation = $current.relation
                    depth = $current.depth
                })
            }
            if ($current.depth -ge 6) { continue }
            foreach ($consumer in @(Get-Consumers $current.symbol.name $current.symbol.path)) {
                $queue.Enqueue([pscustomobject]@{ symbol = $consumer; depth = $current.depth + 1; relation = 'consumer' })
            }
            if ($documents.ContainsKey($current.symbol.path)) {
                $source = $documents[$current.symbol.path]
                foreach ($dependency in $symbols) {
                    if ($dependency.name -eq $current.symbol.name -or
                        $dependency.role -notin @('Component', 'Facade', 'Service', 'ApiClient')) { continue }
                    if ($dependency.name -match '^Ai' -and $source -match "\b$([regex]::Escape($dependency.name))\b") {
                        $queue.Enqueue([pscustomobject]@{ symbol = $dependency; depth = $current.depth + 1; relation = 'dependency' })
                    }
                }
            }
        }
        $upstream = @($related | Where-Object relation -eq 'consumer')
        $componentContract = @($contractIndex.components | Where-Object class -eq $target.name | Select-Object -First 1)
        $selectorConsumers = if ($componentContract.Count -eq 0) {
            @()
        } else {
            @($contractIndex.consumerEdges | Where-Object component -eq $target.name)
        }
        $relatedPaths = @(
            @($target.path) +
            @($related | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['path']) { [string]$_.path } }) +
            @($selectorConsumers | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['consumerPath']) { [string]$_.consumerPath } }) |
                Where-Object { $_ } |
                Sort-Object -Unique
        )
        $apiCalls = @(
            $contractIndex.apiCalls |
                Where-Object { $_.path -in $relatedPaths } |
                Sort-Object path, line -Unique
        )
        $relatedFeatures = @(
            $related |
                ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['path']) { [string]$_.path } } |
                ForEach-Object {
                    $featureMatch = [regex]::Match($_, '/features/(?<feature>[^/]+)/')
                    if ($featureMatch.Success) { $featureMatch.Groups['feature'].Value }
                } |
                Sort-Object -Unique
        )
        $routes = @(
            $frontendIndex.routes |
                Where-Object {
                    $route = $_
                    $route.path -in $relatedFeatures -or
                    @($relatedFeatures | Where-Object { $route.source -match "/features/$([regex]::Escape($_))/" }).Count -gt 0
                } |
                Sort-Object source, line -Unique
        )
        [pscustomobject][ordered]@{
            symbol = $target
            match = [pscustomobject]@{ score = $match.score; queryTerms = $queryTerms; matchedTerms = $match.matchedTerms }
            routes = $routes
            relatedSymbols = @($related | Sort-Object depth, relation, path, name -Unique)
            upstreamConsumers = @($upstream | Sort-Object depth, path, name -Unique)
            selectorConsumers = $selectorConsumers
            contract = if ($componentContract.Count -eq 0) { $null } else { $componentContract[0] }
            apiCalls = $apiCalls
            tests = @(
                @([string]$target.path) + @($related | ForEach-Object {
                    if ($null -ne $_ -and $_.PSObject.Properties['path']) { [string]$_.path }
                }) |
                    ForEach-Object { $_ -replace '\.ts$', '.spec.ts' } |
                    Where-Object { $_ } |
                    Where-Object { Test-Path -LiteralPath (Join-Path (Split-Path -Parent $wikiRoot) $_) } |
                    Sort-Object -Unique
            )
        }
    }
)

$result = [pscustomobject]@{ matched = $true; query = $Query; traces = $traces }
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
    exit 0
}

$displayTraces = if ($Compact) { @($traces | Select-Object -First 1) } else { @($traces) }
foreach ($trace in $displayTraces) {
    Write-Host "$($trace.symbol.name) [$($trace.symbol.role)]"
    Write-Host "  Source: $($trace.symbol.path):$($trace.symbol.line)"
    foreach ($route in @($trace.routes | Select-Object -First $(if ($Compact) { 3 } else { 1000 }))) { Write-Host "  Route: /$($route.path) ($($route.source):$($route.line))" }
    foreach ($consumer in @($trace.upstreamConsumers | Select-Object -First $(if ($Compact) { 5 } else { 1000 }))) { Write-Host "  Upstream: $($consumer.name) ($($consumer.path):$($consumer.line))" }
    foreach ($consumer in @($trace.selectorConsumers | Select-Object -First $(if ($Compact) { 5 } else { 1000 }))) { Write-Host "  Template consumer: $($consumer.consumerPath)" }
    foreach ($apiCall in @($trace.apiCalls | Select-Object -First $(if ($Compact) { 3 } else { 1000 }))) { Write-Host "  API: $($apiCall.method) $($apiCall.resolvedUrlExpression) ($($apiCall.path):$($apiCall.line))" }
    foreach ($test in @($trace.tests | Select-Object -First $(if ($Compact) { 5 } else { 1000 }))) { Write-Host "  Test: $test" }
    if ($Compact) { Write-Host '  Compact trace: use -FullTrace for every match and consumer.' }
    Write-Host ''
}
