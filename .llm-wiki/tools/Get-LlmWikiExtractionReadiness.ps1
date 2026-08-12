[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Module,
    [ValidateSet('Text', 'Json')][string]$Format = 'Text',
    [switch]$IncludeTests
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$folderModulePath = Join-Path $repositoryRoot "FoodDiary.Application/$Module"
$extractedModulePath = Join-Path $repositoryRoot "FoodDiary.Application.$Module"
if (-not (Test-Path -LiteralPath $folderModulePath -PathType Container) -and
    -not (Test-Path -LiteralPath $extractedModulePath -PathType Container)) {
    throw "Application module not found: $Module"
}
$aggregateName = if ($Module -eq 'Users') { 'User' } else { $Module.TrimEnd('s') }
$sourcePaths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--cached', '--others', '--exclude-standard', '--', '*.cs') -FailureMessage 'Unable to enumerate C# sources for extraction readiness.')
$sourcePaths = @($sourcePaths |
    Where-Object { Test-Path -LiteralPath (Join-Path $repositoryRoot $_) -PathType Leaf } |
    Where-Object { $IncludeTests -or $_ -notmatch '(^|/)tests?/|\.Tests?/' } |
    Sort-Object -Unique)

$contracts = [Collections.Generic.List[object]]::new()
foreach ($path in $sourcePaths) {
    $absolutePath = Join-Path $repositoryRoot $path
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) { continue }
    $text = [IO.File]::ReadAllText($absolutePath)
    foreach ($match in [regex]::Matches($text, '(?ms)public\s+interface\s+(?<name>I[A-Za-z0-9_]+)(?:\s*:\s*(?<base>[^\{]+))?\s*\{(?<body>.*?)\}')) {
        $body = $match.Groups['body'].Value
        $aggregateMethods = @([regex]::Matches($body, "(?m)^\s*(?<return>[^;\r\n]*\b$([regex]::Escape($aggregateName))\??(?:[>, ])[^;\r\n]*)\s+(?<method>[A-Za-z_]\w*)\s*\(") | ForEach-Object { [pscustomobject]@{ name = $_.Groups['method'].Value; returns = $_.Groups['return'].Value.Trim() } })
        $mutationMethods = @([regex]::Matches($body, '(?m)^\s*(?<return>[^;\r\n]+)\s+(?<method>(?:Update|Create|Delete|Remove|Restore|Set)[A-Za-z0-9_]*)Async\s*\(') | ForEach-Object { [pscustomobject]@{ name = $_.Groups['method'].Value; returns = $_.Groups['return'].Value.Trim() } })
        $baseContracts = @([regex]::Matches($match.Groups['base'].Value, '\bI[A-Z][A-Za-z0-9_]+\b') | ForEach-Object Value | Sort-Object -Unique)
        if ($aggregateMethods.Count -gt 0 -or $mutationMethods.Count -gt 0 -or $baseContracts.Count -gt 0) {
            $contracts.Add([pscustomobject]@{ name = $match.Groups['name'].Value; path = $path; aggregateMethods = $aggregateMethods; mutationMethods = $mutationMethods; baseContracts = $baseContracts })
        }
    }
    foreach ($match in [regex]::Matches($text, '(?m)public\s+interface\s+(?<name>I[A-Za-z0-9_]+)\s*:\s*(?<base>[^;\r\n]+)\s*;')) {
        $name = $match.Groups['name'].Value
        if (@($contracts | Where-Object name -eq $name).Count -gt 0) { continue }
        $baseContracts = @([regex]::Matches($match.Groups['base'].Value, '\bI[A-Z][A-Za-z0-9_]+\b') | ForEach-Object Value | Sort-Object -Unique)
        $contracts.Add([pscustomobject]@{ name = $name; path = $path; aggregateMethods = @(); mutationMethods = @(); baseContracts = $baseContracts })
    }
}

$leakingNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($contract in $contracts | Where-Object { @($_.aggregateMethods).Count -gt 0 }) { $null = $leakingNames.Add($contract.name) }
do {
    $added = $false
    foreach ($contract in $contracts) {
        if (-not $leakingNames.Contains($contract.name) -and @($contract.baseContracts | Where-Object { $leakingNames.Contains($_) }).Count -gt 0) { $added = $leakingNames.Add($contract.name) -or $added }
    }
} while ($added)

function Get-ConsumerModule([string]$Path) {
    if ($Path -match '^FoodDiary\.Application(?:\.Abstractions)?/([^/]+)/') { return $Matches[1] }
    if ($Path -match '^FoodDiary\.Application\.([^/]+)/') { return $Matches[1] }
    return ($Path -split '/')[0]
}
$leaks = [Collections.Generic.List[object]]::new()
foreach ($contract in $contracts | Where-Object { $leakingNames.Contains($_.name) }) {
    $escaped = [regex]::Escape($contract.name)
    foreach ($consumerPath in $sourcePaths) {
        if ($consumerPath -eq $contract.path) { continue }
        $consumerText = [IO.File]::ReadAllText((Join-Path $repositoryRoot $consumerPath))
        if ($consumerText -notmatch "\b$escaped\b") { continue }
        $composition = $consumerPath -match 'DependencyInjection|Initializer|Program\.cs$'
        $properties = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $operations = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($variableMatch in [regex]::Matches($consumerText, '(?m)\b(?:User|var)\??\s+(?<name>[a-zA-Z_]\w*)\s*=\s*await\s+[^;]+;')) {
            $variable = [regex]::Escape($variableMatch.Groups['name'].Value)
            foreach ($memberMatch in [regex]::Matches($consumerText, "\b$variable(?:\?\.)?\.(?<member>[A-Z][A-Za-z0-9_]*)(?<call>\s*\()?") ) {
                if ($memberMatch.Groups['call'].Success) { $null = $operations.Add($memberMatch.Groups['member'].Value) }
                else { $null = $properties.Add($memberMatch.Groups['member'].Value) }
            }
        }
        $inheritsLeak = @($contract.baseContracts | Where-Object { $leakingNames.Contains($_) }).Count -gt 0
        $kind = if ($composition) { 'composition-only' } elseif ($inheritsLeak -and @($contract.aggregateMethods).Count -eq 0) { 'transitive-wrapper' } elseif ($contract.name -match 'Directory|Repository') { 'repository-or-directory' } elseif (@($contract.aggregateMethods).Count -gt 0) { 'direct-or-wrapper-aggregate' } else { 'transitive-wrapper' }
        $leaks.Add([pscustomobject]@{ contract = $contract.name; declarationPath = $contract.path; consumerModule = Get-ConsumerModule $consumerPath; consumerPath = $consumerPath; kind = $kind; usedProperties = @($properties | Sort-Object); usedOperations = @($operations | Sort-Object); compositionOnly = $composition })
    }
}

$context = & (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Contract IUserContextService -Format Json | ConvertFrom-Json
$productionLeaks = @($leaks | Where-Object {
    -not $_.compositionOnly -and
    $_.consumerPath -notmatch '(^|/)tests?/|\.Tests?/' -and
    $_.consumerModule -ne $Module -and
    $_.consumerPath -notmatch "^FoodDiary\.Infrastructure/Persistence/$Module/"
})
$mutationConsumers = @(if ($Module -eq 'Users') { @($context.consumers | Where-Object {
    $_.access -eq 'mutation' -and
    -not $_.compositionRegistration -and
    $_.consumer -ne $Module
}) } else { @() })
$blockers = [Collections.Generic.List[string]]::new()
if ($productionLeaks.Count -gt 0) { $blockers.Add("$($productionLeaks.Count) production path(s) expose the $aggregateName aggregate through direct or transitive contracts.") }
if ($mutationConsumers.Count -gt 0) { $blockers.Add("$($mutationConsumers.Count) IUserContextService mutation consumer(s) still require a narrow mutation capability.") }
$projections = @($productionLeaks | Where-Object { (@($_.usedProperties).Count -gt 0 -or @($_.usedOperations).Count -gt 0) -and $_.consumerPath -match '^FoodDiary\.Application(?:\.Billing)?/' -and $_.consumerModule -ne 'Users' } | Group-Object consumerModule | ForEach-Object { [pscustomobject]@{
    module = $_.Name
    suggestedName = "$(($_.Name -replace '^FoodDiary\.Application\.', ''))UserProjection"
    fields = @($_.Group | ForEach-Object { @($_.usedProperties) } | Sort-Object -Unique)
    operations = @($_.Group | ForEach-Object { @($_.usedOperations) } | Sort-Object -Unique)
    consumers = @($_.Group | ForEach-Object consumerPath | Sort-Object -Unique)
} })
$result = [pscustomobject]@{
    schemaVersion = 1; module = $Module; ownedAggregate = $aggregateName
    contractReadiness = [pscustomobject]@{ contract = 'IUserContextService'; aggregateBlockers = [int]$context.readiness.aggregateConsumers; mutationBlockers = $mutationConsumers.Count; aggregateReady = [int]$context.readiness.aggregateConsumers -eq 0 }
    moduleReadiness = [pscustomobject]@{ ready = $blockers.Count -eq 0; blockers = @($blockers); aggregateLeakPaths = $productionLeaks.Count; leakingContracts = @($leakingNames | Sort-Object) }
    leaks = @($leaks)
    categories = [pscustomobject]@{ directOrWrapper = @($leaks | Where-Object kind -eq 'direct-or-wrapper-aggregate').Count; repositoryOrDirectory = @($leaks | Where-Object kind -eq 'repository-or-directory').Count; transitiveWrapper = @($leaks | Where-Object kind -eq 'transitive-wrapper').Count; test = @($leaks | Where-Object { $_.consumerPath -match '(^|/)tests?/|\.Tests?/' }).Count; compositionOnly = @($leaks | Where-Object compositionOnly).Count }
    suggestedProjections = $projections
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 10; exit 0 }
Write-Host "Extraction readiness: $Module module (owned aggregate: $aggregateName)"
Write-Host "Contract readiness: IUserContextService aggregate blockers=$($result.contractReadiness.aggregateBlockers), mutation blockers=$($result.contractReadiness.mutationBlockers), aggregate ready=$($result.contractReadiness.aggregateReady)"
Write-Host "Module readiness: $(if ($result.moduleReadiness.ready) { 'ready' } else { 'not ready' })"
foreach ($blocker in $result.moduleReadiness.blockers) { Write-Host "BLOCKER: $blocker" }
Write-Host "Leaks: direct/wrapper=$($result.categories.directOrWrapper), repository/directory=$($result.categories.repositoryOrDirectory), transitive=$($result.categories.transitiveWrapper), tests=$($result.categories.test), composition=$($result.categories.compositionOnly)"
foreach ($contractGroup in @($productionLeaks | Group-Object contract | Sort-Object @{ Expression = 'Count'; Descending = $true }, Name)) {
    $modules = @($contractGroup.Group.consumerModule | Sort-Object -Unique)
    Write-Host "- $($contractGroup.Name): $($contractGroup.Count) production path(s), modules=$($modules -join ', ')"
}
foreach ($projection in $result.suggestedProjections) {
    Write-Host "Projection: $($projection.suggestedName) { $($projection.fields -join ', ') }"
    if (@($projection.operations).Count -gt 0) { Write-Host "  Separate mutation/domain operations: $($projection.operations -join ', ')" }
}
