[CmdletBinding()]
param(
    [string]$Query,
    [string[]]$CandidatePath,
    [ValidateRange(1, 50)]
    [int]$Limit = 5,
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite',
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$contractIndexPath = Join-Path $wikiRoot 'generated/frontend-contract-index.json'
$paths = @($CandidatePath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique)
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$diagnostics = $null
if ($CompiledIndexSource -eq 'Sqlite') {
    $sqlResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') `
        -Action frontend-runtime-owner `
        -Query $Query `
        -ChangedPath $paths `
        -Limit $Limit `
        -SkipRefresh `
        -Format Json | ConvertFrom-Json
    if (-not [bool]$sqlResult.ready) {
        throw "SQLite frontend runtime-owner projection is unavailable ($($sqlResult.unavailableReason)). Run ./.llm-wiki/wiki.ps1 graph-build and retry."
    }
    $result = $sqlResult.runtimeOwner
    $diagnostics = [ordered]@{
        source = [string]$sqlResult.source
        sqlDurationMs = [double]$sqlResult.durationMs
        scannedRecords = [int]$sqlResult.scannedRecords
        candidateRecords = [int]$sqlResult.candidateRecords
        returnedRecords = [int]$sqlResult.returnedRecords
        sourceHash = [string]$sqlResult.sourceHash
    }
} else {
if (-not (Test-Path -LiteralPath $contractIndexPath -PathType Leaf)) { throw "Frontend runtime index is absent: $contractIndexPath" }
$contracts = Get-Content -LiteralPath $contractIndexPath -Raw | ConvertFrom-Json
$ignored = @('change', 'component', 'frontend', 'improve', 'layout', 'result', 'style', 'template', 'visual', 'with')
$tokens = @(
    [regex]::Matches(([string]$Query).ToLowerInvariant(), '[\p{L}\p{Nd}]{3,}') |
        ForEach-Object Value |
        Where-Object { $_ -notin $ignored } |
        Sort-Object -Unique
)
if ([regex]::IsMatch([string]$Query, '(?i)(?<![\p{L}\p{Nd}])ai(?![\p{L}\p{Nd}])')) { $tokens = @($tokens + 'ai' | Sort-Object -Unique) }

$ranked = foreach ($component in @($contracts.components)) {
    $componentDirectory = [IO.Path]::GetDirectoryName([string]$component.path).Replace('\', '/')
    $explicit = @($paths | Where-Object {
        $_ -eq [string]$component.path -or $_ -eq [string]$component.templatePath -or
        [IO.Path]::GetDirectoryName($_).Replace('\', '/') -eq $componentDirectory
    }).Count -gt 0
    $inputNames = @($component.inputs | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['name']) { [string]$_.name } } | Where-Object { $_ })
    $outputNames = @($component.outputs | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['name']) { [string]$_.name } } | Where-Object { $_ })
    $search = "$($component.class) $($component.selector) $($component.path) $($component.templatePath) $($inputNames -join ' ') $($outputNames -join ' ')".ToLowerInvariant()
    $semanticScore = @($tokens | Where-Object { $search -match [regex]::Escape($_) }).Count
    if ($explicit -or $semanticScore -gt 0) {
        [pscustomobject]@{ component = $component; score = $semanticScore + $(if ($explicit) { 100 } else { 0 }); explicit = $explicit }
    }
}
$maximumScore = @($ranked | ForEach-Object { $_.score } | Measure-Object -Maximum).Maximum
$owners = @($ranked | Where-Object score -eq $maximumScore | Sort-Object { $_.component.path } | Select-Object -First $Limit)

function Find-ComponentByTemplate([string]$TemplatePath) {
    $contracts.components | Where-Object templatePath -eq $TemplatePath | Select-Object -First 1
}
function Get-ConsumerChain([object]$Owner) {
    $chain = [Collections.Generic.List[object]]::new()
    $current = $Owner
    $visited = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($depth in 0..5) {
        if ($null -eq $current -or -not $visited.Add([string]$current.path)) { break }
        $currentPath = [string]$current.path
        $edges = @($contracts.consumerEdges | Where-Object { [string]$_.componentPath -ceq $currentPath } | Sort-Object consumerPath)
        if ($edges.Count -eq 0) { break }
        $edge = $edges[0]
        $consumer = Find-ComponentByTemplate ([string]$edge.consumerPath)
        $chain.Add([pscustomobject][ordered]@{
            depth = $depth + 1
            selector = [string]$edge.selector
            renderedBy = [string]$edge.consumerPath
            consumerComponent = $(if ($null -ne $consumer) { [string]$consumer.class } else { '' })
            consumerPath = $(if ($null -ne $consumer) { [string]$consumer.path } else { '' })
        })
        $current = $consumer
    }
    @($chain)
}

$runtimeOwners = foreach ($match in $owners) {
    $component = $match.component
    $directory = [IO.Path]::GetDirectoryName([string]$component.path).Replace('\', '/')
    $baseName = [IO.Path]::GetFileNameWithoutExtension([string]$component.path)
    $stylePath = "$directory/$baseName.scss"
    [pscustomobject][ordered]@{
        class = [string]$component.class
        selector = [string]$component.selector
        componentPath = [string]$component.path
        templatePath = [string]$component.templatePath
        stylePath = $stylePath
        specPath = [string]$component.specPath
        score = [int]$match.score
        explicitPathMatch = [bool]$match.explicit
        renderChain = @(Get-ConsumerChain $component)
        recommendedScope = @([string]$component.path, [string]$component.templatePath, $stylePath, [string]$component.specPath |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    }
}
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    query = [string]$Query
    candidatePaths = $paths
    ownerCount = @($runtimeOwners).Count
    confidence = if (@($runtimeOwners).Count -eq 1 -and ([bool]$runtimeOwners[0].explicitPathMatch -or [int]$runtimeOwners[0].score -ge 2)) { 'high' } elseif (@($runtimeOwners).Count -gt 0) { 'medium' } else { 'low' }
    owners = @($runtimeOwners)
    note = 'Confirm the visible entry action and rendered consumer chain in current templates before editing the recommended scope.'
}
$diagnostics = [ordered]@{
    source = 'json-baseline'
    sqlDurationMs = $null
    scannedRecords = @($contracts.components).Count + @($contracts.consumerEdges).Count + @($contracts.apiCalls).Count + @($contracts.translationUsage).Count
    candidateRecords = @($contracts.components).Count
    returnedRecords = @($contracts.components).Count + @($contracts.consumerEdges).Count
    sourceHash = $null
}
}
$stopwatch.Stop()
$diagnostics['roundTripDurationMs'] = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
$result | Add-Member -NotePropertyName compiledIndex -NotePropertyValue ([pscustomobject]$diagnostics) -Force
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12 } else {
    Write-Host "Frontend runtime owners: $($result.ownerCount) ($($result.confidence) confidence)"
    foreach ($owner in @($result.owners)) {
        Write-Host " - $($owner.class) [$($owner.selector)]: $($owner.templatePath)"
        foreach ($edge in @($owner.renderChain)) { Write-Host "   <- $($edge.renderedBy)" }
    }
    Write-Host $result.note
}
